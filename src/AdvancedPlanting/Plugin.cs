using System;
using System.Collections.Generic;
using AdvancedPlanting.Core;
using AdvancedPlanting.UI;
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
        public const string PluginVersion = "0.2.1";
        public const string PluginAuthor = "kepqa";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        public ModConfig Settings { get; private set; }
        public FigureState State { get; private set; }
        public LayoutService Layout { get; private set; }
        public SettingsWindow Window { get; private set; }
        internal PlantingWheel Wheel { get; private set; }

        private GhostPool _ghosts;
        private CurveRenderer _overlay;
        private StatusPanel _panel;
        private Harmony _harmony;

        private float _nextRevalidate;
        private float _nextRubberBand;
        private Vector3? _builtCursor;
        private bool _previewVisible;
        private float _nextAutoPlant;
        private readonly List<PlantSlot> _ghostSlots = new List<PlantSlot>();

        // Per-frame context, refreshed in Update.
        private Player _player;
        private Piece _piece;
        private Plant _plant;
        private string _prefabName;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Settings = new ModConfig(Config);
            Loc.Load();
            State = new FigureState { Enabled = Settings.EnabledByDefault.Value };
            Layout = new LayoutService(Settings);
            Window = new SettingsWindow(this);
            Wheel = new PlantingWheel(this);
            _ghosts = new GhostPool();
            _overlay = new CurveRenderer(Settings);
            _panel = new StatusPanel(Settings);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{PluginName} {PluginVersion} by {PluginAuthor} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _ghosts?.DestroyRoots();
            _overlay?.Destroy();
            Instance = null;
        }

        // ------------------------------------------------------------------ context

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

        /// <summary>Local player is in build mode with a seed or sapling selected.</summary>
        public bool HasPlantContext => _piece != null && _player != null;

        /// <summary>Figure mode is on and a seed is selected.</summary>
        private bool FigureModeActive => State.Enabled && HasPlantContext;

        /// <summary>True only during our own call to open the radial menu.</summary>
        public bool OpeningWheel { get; private set; }

        /// <summary>Frame the wheel was opened on, so the opening key press does not close it.</summary>
        public int WheelOpenedFrame { get; private set; } = -10;

        /// <summary>Time the wheel key went down, for hold-and-release selection.</summary>
        public float WheelKeyDownTime { get; private set; }

        public bool WindowBlocksInput => Window != null && Window.IsBlockingInput;

        public string PlantDisplayName => _piece != null ? GameText.Localize(_piece.m_name) : "";

        public float MinSpacing => Layout.MinDistance(_plant);

        public float CurrentSpacing => State.GetSpacing(_prefabName, Layout.DefaultSpacing(_plant));

        public float JitterRoom => Mathf.Max(0f, (CurrentSpacing - MinSpacing) * 0.5f);

        public void SetSpacing(float value) => State.SetSpacing(_prefabName, value, MinSpacing);

        public string WheelKeyName => Settings.WheelKey.Value.ToString();

        public void SetFigureMode(bool on)
        {
            if (State.Enabled == on) return;
            State.Enabled = on;
            State.MarkDirty();
            if (!on)
            {
                Window.Close();
                HidePreview();
            }
            _player?.Message(MessageHud.MessageType.Center, Loc.T(on ? "msg.figure_mode" : "msg.single_mode"));
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            if (!RefreshContext())
            {
                Window.Close();
                HidePreview();
                return;
            }
            HandleWheelKey();
            if (!State.Enabled)
            {
                HidePreview();
                return;
            }
            UpdateAutoContinue();
        }

        private void HandleWheelKey()
        {
            KeyCode key = Settings.WheelKey.Value;
            if (key == KeyCode.None || !ZInput.GetKeyDown(key)) return;
            WheelKeyDownTime = Time.unscaledTime;
            if (global::Hud.InRadial() || Window.IsBlockingInput || global::Hud.IsPieceSelectionVisible()) return;
            if (!_player.TakeInput()) return;
            OpenWheel();
        }

        /// <summary>
        /// Opens the vanilla radial menu the same way the radial key does, and routes it to the
        /// planting wheel (see UiPatches).
        /// </summary>
        private void OpenWheel()
        {
            global::Hud hud = global::Hud.instance;
            if (hud == null || hud.m_radialMenu == null || hud.m_config == null) return;
            OpeningWheel = true;
            try
            {
                WheelOpenedFrame = Time.frameCount;
                hud.m_radialMenu.CanOpen = true;
                hud.m_radialMenu.Open(hud.m_config);
            }
            catch (Exception e)
            {
                Log.LogError("Could not open the planting wheel: " + e);
            }
            finally
            {
                OpeningWheel = false;
            }
        }

        // ------------------------------------------------------------------ clicks

        /// <summary>
        /// Called from the UpdatePlacement prefix. Returns true when the click was used to draw
        /// (add, finish or restart the figure) and must not reach vanilla planting.
        /// </summary>
        public bool HandlePlacementClick(Player player)
        {
            if (player != _player || !FigureModeActive) return false;
            if (Window.IsBlockingInput || global::Hud.InRadial() || global::Hud.IsPieceSelectionVisible()) return false;
            if (!(ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace"))) return false;

            if (State.Finished)
            {
                RebuildNow();
                if (Layout.ValidCount > 0)
                {
                    // Vanilla will plant through TryPlacePiece, unless it has no stamina for the
                    // first plant, in which case it only flashes the bar. Say why.
                    ItemDrop.ItemData tool = player.GetRightItem();
                    if (tool != null && !player.HaveStamina(tool.m_shared.m_attack.m_attackStamina))
                    {
                        player.Message(MessageHud.MessageType.Center, Loc.T("msg.no_stamina")
                            + (State.AutoContinue ? "" : Loc.T("msg.no_stamina.auto_hint")));
                    }
                    return false;
                }
                // Nothing left to plant in this figure: the click starts a new one.
                State.Clear();
            }

            GameObject ghost = player.m_placementGhost;
            if (ghost == null || !ghost.activeSelf)
            {
                player.Message(MessageHud.MessageType.Center, Loc.T("msg.aim"));
                return true;
            }
            Vector3 cursor = ghost.transform.position;

            int snap = SnapTarget(cursor);
            if (snap >= 0)
            {
                State.Finish();
                player.Message(MessageHud.MessageType.Center, Loc.T("msg.ready", StatusPanel.ModeName(State.Mode)));
                return true;
            }
            if (State.ControlPoints.Count > 0 && FlatDistance(State.ControlPoints[State.ControlPoints.Count - 1], cursor) < 0.2f)
                return true; // double click on the same spot of a fixed-size path: ignore

            State.AddPoint(cursor);
            if (State.Finished) player.Message(MessageHud.MessageType.Center, Loc.T("msg.ready", StatusPanel.ModeName(State.Mode)));
            return true;
        }

        /// <summary>
        /// Index of the control point a click at <paramref name="cursor"/> would finish on:
        /// the last point of a curve, or the first (or last) point of a shape. -1 if none.
        /// </summary>
        private int SnapTarget(Vector3 cursor)
        {
            if (!State.CanFinish) return -1;
            float r = Settings.SnapRadius.Value;
            int last = State.ControlPoints.Count - 1;
            if (FlatDistance(State.ControlPoints[last], cursor) <= r) return last;
            if (State.Mode == PathMode.Contour && FlatDistance(State.ControlPoints[0], cursor) <= r) return 0;
            return -1;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ------------------------------------------------------------------ preview

        private Vector3? CursorPoint()
        {
            GameObject ghost = _player != null ? _player.m_placementGhost : null;
            if (ghost == null || !ghost.activeSelf) return null;
            return ghost.transform.position;
        }

        private void RebuildNow()
        {
            Vector3? cursor = State.Finished ? (Vector3?)null : CursorPoint();
            try
            {
                Layout.Rebuild(State, cursor, _piece, _plant, _prefabName, _player);
            }
            catch (Exception e)
            {
                Log.LogError("Layout rebuild failed: " + e);
                Layout.Clear();
            }
            _builtCursor = cursor;
            State.ClearDirty();
            _nextRevalidate = Time.time + Settings.RevalidateInterval.Value;
        }

        /// <summary>Called by the UpdatePlacementGhost postfix, after vanilla moved its ghost.</summary>
        public void OnPlacementGhostUpdated(Player player)
        {
            if (player != Player.m_localPlayer) return;
            if (!FigureModeActive)
            {
                HidePreview();
                return;
            }

            bool prefabChanged = Layout.BuiltForPrefab != _prefabName;
            if (State.Dirty || prefabChanged)
            {
                RebuildNow();
            }
            else if (!State.Finished)
            {
                // Rubber band: the cursor is the next control point.
                Vector3? cursor = CursorPoint();
                bool moved = cursor.HasValue != _builtCursor.HasValue
                    || (cursor.HasValue && FlatDistance(cursor.Value, _builtCursor.Value) > 0.1f);
                if (moved && Time.time >= _nextRubberBand)
                {
                    RebuildNow();
                    _nextRubberBand = Time.time + 0.05f;
                }
            }
            else if (Time.time >= _nextRevalidate)
            {
                Layout.Revalidate(State, _piece, _plant, _player);
                _nextRevalidate = Time.time + Settings.RevalidateInterval.Value;
            }

            _ghostSlots.Clear();
            foreach (PlantSlot s in Layout.Slots)
            {
                if (s.IsValid) _ghostSlots.Add(s);
            }
            _ghosts.Apply(player.m_placementGhost, _ghostSlots);

            int snap = -1;
            Vector3? c = CursorPoint();
            if (c.HasValue) snap = SnapTarget(c.Value);
            _overlay.Apply(Layout.RowLines, State.ControlPoints, snap, Layout.Slots, _plant != null ? _plant.m_growRadius : 0f);
            _previewVisible = true;
        }

        private void HidePreview()
        {
            if (!_previewVisible) return;
            _ghosts.HideAll();
            _overlay.HideAll();
            _previewVisible = false;
        }

        // ------------------------------------------------------------------ planting

        /// <summary>
        /// Called by the TryPlacePiece prefix. Returns true when the figure was handled here and
        /// the vanilla single placement must be skipped.
        /// </summary>
        public bool TryPlantFigure(Player player, Piece piece, out bool placedAny)
        {
            placedAny = false;
            if (!FigureModeActive || player != _player || piece == null || !State.Finished) return false;
            if (piece.GetComponent<Plant>() == null) return false;

            if (State.Dirty || Layout.BuiltForPrefab != _prefabName) RebuildNow();
            else Layout.Revalidate(State, _piece, _plant, _player);

            if (Layout.ValidCount == 0)
            {
                player.Message(MessageHud.MessageType.Center, Loc.T("msg.nothing", PlacementValidator.Describe(Layout.TopBlockReason)));
                return true;
            }

            Planter.Outcome result = Planter.Plant(player, piece, Layout.Slots, true, int.MaxValue, State.MarkPlanted);
            placedAny = result.Planted > 0;
            Layout.Revalidate(State, _piece, _plant, _player);

            string msg = Loc.T("msg.planted", result.Planted);
            if (Layout.ValidCount > 0)
            {
                msg += Loc.T("msg.left", Layout.ValidCount);
                if (result.OutOfStamina) msg += " " + Loc.T(State.AutoContinue ? "msg.left.auto" : "msg.left.stamina");
                else if (result.OutOfSeeds) msg += " " + Loc.T("msg.left.seeds");
                else if (result.ToolBroken) msg += " " + Loc.T("msg.left.tool");
            }
            if (result.Blocked > 0) msg += Loc.T("msg.blocked", result.Blocked, PlacementValidator.Describe(Layout.TopBlockReason));
            player.Message(MessageHud.MessageType.TopLeft, msg);

            CompleteIfDone();
            return true;
        }

        private void UpdateAutoContinue()
        {
            if (!State.AutoContinue || !State.Finished || Window.IsBlockingInput) return;
            if (Time.time < _nextAutoPlant || Layout.ValidCount == 0) return;
            if (global::Hud.InRadial() || !_player.TakeInput()) return;
            _nextAutoPlant = Time.time + Settings.AutoContinueInterval.Value;

            if (State.Dirty) RebuildNow();
            if (Layout.ValidCount == 0 || !Planter.CanAffordOne(_player, _piece)) return;

            Planter.Outcome result = Planter.Plant(_player, _piece, Layout.Slots, false, 1, State.MarkPlanted);
            if (result.Planted == 0) return;
            Layout.Revalidate(State, _piece, _plant, _player);
            CompleteIfDone();
        }

        /// <summary>A figure with nothing left (no plantable and no blocked slots) is cleared.</summary>
        private void CompleteIfDone()
        {
            if (Layout.ValidCount > 0 || Layout.BlockedCount > 0 || Layout.PlantedCount == 0) return;
            _player?.Message(MessageHud.MessageType.Center, Loc.T("msg.complete", Layout.PlantedCount));
            State.Clear();
        }

        // ------------------------------------------------------------------ texts

        /// <summary>What the next left click does, for the panel and the key hints.</summary>
        public string NextActionHint()
        {
            if (State.Finished)
                return Layout.ValidCount > 0 ? Loc.T("hint.plant", Layout.ValidCount) : Loc.T("hint.new_figure");
            int have = State.ControlPoints.Count;
            switch (State.Mode)
            {
                case PathMode.Segment:
                    return Loc.T(have == 0 ? "hint.line.start" : "hint.line.end");
                case PathMode.Arc:
                    return Loc.T(have == 0 ? "hint.arc.start" : have == 1 ? "hint.arc.middle" : "hint.arc.end");
                case PathMode.Circle:
                    return Loc.T("hint.circle.point", have + 1);
                case PathMode.Spline:
                    return have < 2 ? Loc.T("hint.curve.point", have + 1) : Loc.T("hint.curve.next");
                case PathMode.Contour:
                    return have < 3 ? Loc.T("hint.shape.point", have + 1) : Loc.T("hint.shape.next");
                default:
                    return "";
            }
        }

        /// <summary>Second hint while drawing: how to finish a curve or close a shape, or null.</summary>
        public string FinishHint()
        {
            if (State.Finished || !State.CanFinish) return null;
            return Loc.T(State.Mode == PathMode.Contour ? "hint.shape.close" : "hint.curve.finish");
        }

        public string ResourceLine()
        {
            if (_player == null || _piece == null) return "";
            int toPlant = Layout.ValidCount;
            int seeds = SeedsAvailable();
            float perPlant = _player.GetBuildStamina();
            float stamina = _player.GetStamina();
            int byStamina = perPlant > 0.01f ? Mathf.FloorToInt(stamina / perPlant) : int.MaxValue;

            string seedText = seeds >= toPlant ? seeds.ToString() : "<color=#ff8080>" + seeds + "</color>";
            string line = Loc.T("panel.seeds", seedText, toPlant);
            if (perPlant > 0.01f)
            {
                int now = Mathf.Min(toPlant, byStamina);
                line += "   " + Loc.T("panel.stamina", now >= toPlant ? now.ToString() : "<color=#ffd080>" + now + "</color>");
            }
            return line;
        }

        public string CostSummary()
        {
            if (Layout.Slots.Count == 0) return Loc.T("window.cost.empty");
            float perPlant = _player != null ? _player.GetBuildStamina() : 0f;
            int toPlant = Layout.ValidCount;
            string s = Loc.T("window.cost.counts", toPlant, Layout.PlantedCount, Layout.BlockedCount) + "\n";
            s += Loc.T("window.cost.seeds", toPlant, SeedsAvailable());
            if (perPlant > 0.01f && _player != null)
                s += " " + Loc.T("window.cost.stamina", Mathf.CeilToInt(toPlant * perPlant), Mathf.FloorToInt(_player.GetStamina()),
                    Mathf.FloorToInt(_player.GetMaxStamina()));
            return s;
        }

        private int SeedsAvailable()
        {
            if (_player == null || _piece == null || _piece.m_resources == null) return 0;
            int seeds = int.MaxValue;
            Inventory inv = _player.GetInventory();
            foreach (Piece.Requirement req in _piece.m_resources)
            {
                int amount = req?.m_resItem != null ? req.GetAmount(0) : 0;
                if (amount <= 0) continue;
                seeds = Mathf.Min(seeds, inv.CountItems(req.m_resItem.m_itemData.m_shared.m_name) / amount);
            }
            return seeds == int.MaxValue ? 0 : seeds;
        }

        // ------------------------------------------------------------------ gui

        private void OnGUI()
        {
            if (!HasPlantContext) return;
            Window.OnGUI();
            if (Window.IsOpen || !State.Enabled) return;
            if (global::Hud.InRadial()) return;
            if (global::Hud.IsPieceSelectionVisible()) return;
            _panel.Draw(this);
        }
    }
}
