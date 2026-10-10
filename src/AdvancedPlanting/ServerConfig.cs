using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace AdvancedPlanting
{
    /// <summary>
    /// Server-enforced settings.
    ///
    /// When the mod is installed on the server (dedicated or the host), every client that has it
    /// asks the server for its values once the player has spawned, and uses them instead of its
    /// own for the entries in <see cref="ModConfig.Synced"/> until it leaves the world. The
    /// client's config file is never touched. A server without the mod ignores the request, so
    /// the client keeps its own values. Built on the game's routed RPCs, no extra dependency.
    /// </summary>
    internal static class ServerConfig
    {
        private const string RequestRpc = "AdvancedPlanting_RequestConfig";
        private const string ValuesRpc = "AdvancedPlanting_Config";

        private static ModConfig _config;
        private static ZRoutedRpc _registeredOn;
        private static ZNet _requestedFrom;
        private static ZNet _receivedFrom;
        private static readonly Dictionary<string, object> Overrides = new Dictionary<string, object>();

        /// <summary>Server values are in use for the current connection.</summary>
        public static bool Active => ZNet.instance != null && _receivedFrom == ZNet.instance;

        /// <summary>Value of <paramref name="entry"/>, or the server's value while connected to one that sent it.</summary>
        public static T Get<T>(ConfigEntry<T> entry)
        {
            if (Active && Overrides.TryGetValue(entry.Definition.Key, out object value) && value is T typed) return typed;
            return entry.Value;
        }

        public static void Init(ModConfig config)
        {
            _config = config;
            config.File.SettingChanged += OnSettingChanged;
        }

        /// <summary>Registers the RPCs on each new connection and, on a client, asks the server once.</summary>
        public static void Update()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            ZNet net = ZNet.instance;
            if (rpc == null || net == null) return;
            if (_registeredOn != rpc)
            {
                rpc.Register<ZPackage>(RequestRpc, OnRequest);
                rpc.Register<ZPackage>(ValuesRpc, OnValues);
                _registeredOn = rpc;
            }
            if (!net.IsServer() && _requestedFrom != net && Player.m_localPlayer != null)
            {
                _requestedFrom = net;
                rpc.InvokeRoutedRPC(rpc.GetServerPeerID(), RequestRpc, new ZPackage());
            }
        }

        private static void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            // An admin edited the server's config while players are online: push the new values.
            if (ZNet.instance == null || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null) return;
            if (!_config.Synced.Contains(e.ChangedSetting) && e.ChangedSetting != _config.EnforceOnClients) return;
            if (_config.EnforceOnClients.Value)
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, ValuesRpc, Pack());
        }

        private static void OnRequest(long sender, ZPackage request)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || !_config.EnforceOnClients.Value) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ValuesRpc, Pack());
        }

        private static ZPackage Pack()
        {
            var pkg = new ZPackage();
            pkg.Write(_config.Synced.Count);
            foreach (ConfigEntryBase entry in _config.Synced)
            {
                pkg.Write(entry.Definition.Key);
                pkg.Write(entry.GetSerializedValue());
            }
            return pkg;
        }

        private static void OnValues(long sender, ZPackage pkg)
        {
            ZNet net = ZNet.instance;
            // Only the server may set these; ignore copies of our own broadcast and other peers.
            if (net == null || net.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
            try
            {
                Overrides.Clear();
                int count = pkg.ReadInt();
                var applied = new List<string>(count);
                for (int i = 0; i < count; i++)
                {
                    string key = pkg.ReadString();
                    string text = pkg.ReadString();
                    ConfigEntryBase entry = _config.Synced.Find(x => x.Definition.Key == key);
                    if (entry == null) continue; // a newer server may sync more settings
                    object value = TomlTypeConverter.ConvertToValue(text, entry.SettingType);
                    if (entry.Description.AcceptableValues != null)
                        value = entry.Description.AcceptableValues.Clamp(value);
                    Overrides[key] = value;
                    applied.Add(key + " = " + text);
                }
                _receivedFrom = net;
                Plugin.Instance?.State.MarkDirty();
                Plugin.Log.LogInfo("Using the server's settings: " + string.Join(", ", applied));
            }
            catch (Exception e)
            {
                Overrides.Clear();
                Plugin.Log.LogWarning("Could not read the server's settings, keeping local ones: " + e.Message);
            }
        }
    }
}
