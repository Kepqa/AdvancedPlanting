using System;
using AdvancedPlanting.Core;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AdvancedPlanting
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "kepqa.AdvancedPlanting";
        public const string PluginName = "AdvancedPlanting";
        public const string PluginVersion = "0.1.0";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        public ModConfig Settings { get; private set; }
        public FigureState State { get; private set; }
        public LayoutService Layout { get; private set; }

        private GhostPool _ghosts;
        private CurveRenderer _curves;
        private StatusPanel _hud;
        private Harmony _harmony;

        private float _nextRevalidate;
        private bool _previewVisible;

        // Cached per-frame context, refreshed in Update.
        private Player _player;
        private Piece _piece;
        private Plant _plant;
        private string _prefabName;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Settings = new ModConfig(Config);
            State = new FigureState { Enabled = Settings.EnabledByDefault.Value };
            Layout = new LayoutService(Settings);
            _ghosts = new GhostPool();
            _curves = new CurveRenderer(Settings);
            _hud = new StatusPanel(Settings);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _ghosts?.DestroyRoots();
            _curves?.Destroy();
            Instance = null;
        }

        // ------------------------------------------------------------------ context

        /// <summary>True when the local player is in build mode holding a plantable piece.</summary>
        private bool RefreshContext()
        {
            _player = Player.m_localPlayer;
            _piece = null;
            _plant = null;
            _prefabName = null;
            if (_player == null || !_player.InPlaceMode()) return false;

            Piece piece = _player.GetSelectedPiece();
            if (piece == null) return false;
            Plant plant = piece.GetComponent<Plant>();
            if (plant == null) return false;

            _piece = piece;
            _plant = plant;
            _prefabName = piece.gameObject.name;
            return true;
        }

        private bool FigureActive => State.Enabled && _piece != null && State.HasEnoughPoints;

        private float CurrentSpacing => State.GetSpacing(_prefabName, Layout.DefaultSpacing(_plant));

        // ------------------------------------------------------------------ input

        private void Update()
        {
            if (ZInput.GetKeyDown(Settings.ToggleModKey.Value))
            {
                State.Enabled = !State.Enabled;
                State.MarkDirty();
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, State.Enabled ? "AdvancedPlanting: on" : "AdvancedPlanting: off");
            }

            if (!RefreshContext() || !State.Enabled)
            {
                HidePreview();
                return;
            }
            if (!_player.TakeInput() || global::Hud.IsPieceSelectionVisible()) return;

            if (ZInput.GetKeyDown(Settings.AddPointKey.Value)) AddControlPoint();
            if (ZInput.GetKeyDown(Settings.RemovePointKey.Value)) State.RemoveLastPoint();
            if (ZInput.GetKeyDown(Settings.ClearKey.Value)) State.Clear();
            if (ZInput.GetKeyDown(Settings.CycleModeKey.Value))
            {
                State.CycleMode();
                _player.Message(MessageHud.MessageType.Center, "Path: " + StatusPanel.ModeName(State.Mode));
            }
            if (ZInput.GetKeyDown(Settings.CycleSideKey.Value))
            {
                State.CycleSide();
                _player.Message(MessageHud.MessageType.Center, "Rows: " + StatusPanel.SideName(State.Side));
            }
            if (ZInput.GetKeyDown(Settings.RerollSeedKey.Value)) State.RerollSeed();
            if (ZInput.GetKeyDown(Settings.NextParameterKey.Value))
            {
                State.NextParameter();
                _player.Message(MessageHud.MessageType.Center, ModName(Settings.ParameterModifier.Value) + " + wheel: " + StatusPanel.ParameterName(State.Selected));
            }
        }

        internal static string ModName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt: return "Alt";
                case KeyCode.LeftControl:
                case KeyCode.RightControl: return "Ctrl";
                case KeyCode.LeftShift:
                case KeyCode.RightShift: return "Shift";
                default: return key.ToString();
            }
        }

        private void AddControlPoint()
        {
            GameObject ghost = _player.m_placementGhost;
            if (ghost == null || !ghost.activeSelf)
            {
                _player.Message(MessageHud.MessageType.Center, "Aim at the ground first");
                return;
            }
            int max = State.Mode.MaxControlPoints();
            if (max > 0 && State.ControlPoints.Count >= max)
            {
                _player.Message(MessageHud.MessageType.Center, StatusPanel.ModeName(State.Mode) + " uses " + max + " points");
                return;
            }
            State.AddPoint(ghost.transform.position);
        }

        /// <summary>
        /// Called from the UpdatePlacement patch every frame in build mode. Returns true when the
        /// wheel was consumed so the patch restores the vanilla rotation.
        /// </summary>
        public bool HandleScroll(Player player)
        {
            if (!State.Enabled || _piece == null || player != _player) return false;

            if (!ZInput.GetKey(Settings.ParameterModifier.Value)) return false;

            float wheel = ZInput.GetMouseScrollWheel();
            if (Mathf.Abs(wheel) < 0.001f) return true;
            int dir = wheel > 0f ? 1 : -1;

            switch (State.Selected)
            {
                case EditParameter.Spacing:
                    State.AdjustSpacing(_prefabName, Layout.DefaultSpacing(_plant), dir * Settings.SpacingStep.Value, Layout.MinDistance(_plant));
                    break;
                case EditParameter.Rows:
                    State.AdjustRows(dir);
                    break;
                case EditParameter.Jitter:
                    State.AdjustJitter(dir * Settings.JitterStep.Value);
                    break;
                case EditParameter.Thinning:
                    State.AdjustThinning(dir * Settings.ThinningStep.Value);
                    break;
            }
            return true;
        }

        // ------------------------------------------------------------------ preview

        /// <summary>Called by the UpdatePlacementGhost postfix.</summary>
        public void OnPlacementGhostUpdated(Player player)
        {
            if (player != Player.m_localPlayer) return;
            if (_piece == null || !State.Enabled)
            {
                HidePreview();
                return;
            }

            if (!State.HasEnoughPoints)
            {
                _ghosts.HideAll();
                if (State.ControlPoints.Count > 0)
                {
                    Layout.Clear();
                    _curves.Apply(Layout.RowLines, State.ControlPoints);
                    _previewVisible = true;
                }
                else
                {
                    HidePreview();
                }
                State.ClearDirty();
                return;
            }

            bool prefabChanged = Layout.BuiltForPrefab != _prefabName;
            if (State.Dirty || prefabChanged)
            {
                try
                {
                    Layout.Rebuild(State, _piece, _plant, _prefabName, _player);
                }
                catch (Exception e)
                {
                    Log.LogError("Layout rebuild failed: " + e);
                    Layout.Clear();
                }
                State.ClearDirty();
                _nextRevalidate = Time.time + Settings.RevalidateInterval.Value;
            }
            else if (Time.time >= _nextRevalidate)
            {
                Layout.Revalidate(_piece, _plant, _player);
                _nextRevalidate = Time.time + Settings.RevalidateInterval.Value;
            }

            _ghosts.Apply(player.m_placementGhost, Layout.Slots);
            _curves.Apply(Layout.RowLines, State.ControlPoints);
            _previewVisible = true;
        }

        private void HidePreview()
        {
            if (!_previewVisible) return;
            _ghosts.HideAll();
            _curves.HideAll();
            _previewVisible = false;
        }

        // ------------------------------------------------------------------ planting

        /// <summary>
        /// Called by the PlacePiece prefix. Returns true when the figure was handled here and the
        /// vanilla single placement must be skipped.
        /// </summary>
        public bool TryPlantFigure(Player player, Piece piece, out bool placedAny)
        {
            placedAny = false;
            if (!FigureActive || player != _player || piece == null) return false;
            if (piece.GetComponent<Plant>() == null) return false;

            if (State.Dirty || Layout.BuiltForPrefab != _prefabName) Layout.Rebuild(State, _piece, _plant, _prefabName, _player);
            else Layout.Revalidate(_piece, _plant, _player);

            if (Layout.ValidCount == 0)
            {
                string reason = Layout.Slots.Count == 0 ? "figure is empty" : PlacementValidator.Describe(Layout.TopBlockReason);
                player.Message(MessageHud.MessageType.Center, "Nothing to plant: " + reason);
                return true;
            }

            Planter.Outcome result = Planter.Plant(player, piece, Layout.Slots);
            placedAny = result.Planted > 0;

            string msg = "Planted " + result.Planted;
            if (result.Blocked > 0) msg += ", " + result.Blocked + " blocked (" + PlacementValidator.Describe(Layout.TopBlockReason) + ")";
            if (result.OutOfSeeds) msg += ", out of seeds";
            if (result.OutOfStamina) msg += ", out of stamina";
            if (result.ToolBroken) msg += ", tool broken";
            player.Message(MessageHud.MessageType.TopLeft, msg);

            if (placedAny && !Settings.KeepFigureAfterPlanting.Value) State.Clear();
            else State.MarkDirty();
            return true;
        }

        // ------------------------------------------------------------------ hud

        private void OnGUI()
        {
            if (_piece == null || !State.Enabled || !Settings.ShowHud.Value) return;
            if (State.ControlPoints.Count == 0 && !State.HasEnoughPoints) return;
            if (global::Hud.IsPieceSelectionVisible()) return;

            int seeds = int.MaxValue;
            if (_player != null && _piece.m_resources != null)
            {
                Inventory inv = _player.GetInventory();
                foreach (Piece.Requirement req in _piece.m_resources)
                {
                    int amount = req?.m_resItem != null ? req.GetAmount(0) : 0;
                    if (amount <= 0) continue;
                    seeds = Mathf.Min(seeds, inv.CountItems(req.m_resItem.m_itemData.m_shared.m_name) / amount);
                }
            }
            if (seeds == int.MaxValue) seeds = 0;

            string name = GameText.Localize(_piece.m_name);
            _hud.Draw(State, Layout, _plant, name, seeds, CurrentSpacing);
        }
    }
}
