// Minimal stand-ins for BepInEx 5 and HarmonyX surface used by the plugin. Compile-only.
#pragma warning disable IDE0060, CS1591
using System;
using System.Reflection;
using UnityEngine;

namespace BepInEx
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BepInPlugin : Attribute
    {
        public BepInPlugin(string guid, string name, string version) { }
    }

    public abstract class BaseUnityPlugin : MonoBehaviour
    {
        protected Configuration.ConfigFile Config { get; } = new Configuration.ConfigFile();
        protected Logging.ManualLogSource Logger { get; } = new Logging.ManualLogSource();
    }

    namespace Logging
    {
        public class ManualLogSource
        {
            public void LogInfo(object data) { }
            public void LogDebug(object data) { }
            public void LogWarning(object data) { }
            public void LogError(object data) { }
        }
    }

    namespace Configuration
    {
        public class ConfigDescription
        {
            public ConfigDescription(string description, object acceptableValues = null) { }
        }

        public class AcceptableValueRange<T>
        {
            public AcceptableValueRange(T min, T max) { }
        }

        public class ConfigEntry<T>
        {
            public T Value { get; set; }
        }

        public class ConfigFile
        {
            public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
                => new ConfigEntry<T> { Value = defaultValue };

            public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription description)
                => new ConfigEntry<T> { Value = defaultValue };
        }
    }
}

namespace HarmonyLib
{
    public class Harmony
    {
        public Harmony(string id) { }
        public void PatchAll(Assembly assembly) { }
        public void UnpatchSelf() { }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type type, string methodName) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyPrefix : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyPostfix : Attribute { }

    public static class AccessTools
    {
        public static MethodInfo Method(Type type, string name) => null;
    }
}
