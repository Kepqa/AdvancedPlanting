using BepInEx.Configuration;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// All user-facing settings. Written to BepInEx/config/kepqa.AdvancedPlanting.cfg.
    ///
    /// Left click draws and plants, the mouse wheel changes the row count, and a few keys that
    /// the game does not use in build mode give quick access to the most common actions.
    /// Everything else is in the planting wheel or the settings window.
    /// </summary>
    public sealed class ModConfig
    {
        // Keys
        public ConfigEntry<KeyCode> WheelKey;
        public ConfigEntry<KeyCode> FinishKey;
        public ConfigEntry<KeyCode> FillKey;
        public ConfigEntry<KeyCode> ClearKey;
        public ConfigEntry<bool> RowsOnMouseWheel;

        // Behaviour
        public ConfigEntry<bool> EnabledByDefault;
        public ConfigEntry<float> DefaultSpacingFactor;
        public ConfigEntry<float> MinDistanceMargin;
        public ConfigEntry<int> MaxPlants;
        public ConfigEntry<float> MaxFigureSize;
        public ConfigEntry<bool> RandomRotation;
        public ConfigEntry<float> WarpScale;
        public ConfigEntry<float> RevalidateInterval;
        public ConfigEntry<float> SnapRadius;
        public ConfigEntry<float> AutoContinueInterval;

        // Display
        public ConfigEntry<bool> ShowHud;
        public ConfigEntry<int> HudX;
        public ConfigEntry<int> HudY;
        public ConfigEntry<bool> ShowCurve;
        public ConfigEntry<bool> ShowGrowRadius;
        public ConfigEntry<float> CurveWidth;
        public ConfigEntry<Color> CurveColor;
        public ConfigEntry<Color> ControlPointColor;

        public ModConfig(ConfigFile file)
        {
            WheelKey = file.Bind("0. Keys", "PlantingWheel", KeyCode.B,
                "Opens the planting wheel while a seed is selected in build mode. Press again or hold and release to choose. B is unused by the game.");
            FinishKey = file.Bind("0. Keys", "FinishFigure", KeyCode.Z,
                "Finishes a curve or closes a shape (same as clicking its last or first point). None to disable.");
            FillKey = file.Bind("0. Keys", "ToggleFill", KeyCode.G,
                "Turns filling of circles and shapes on or off. The radial-menu key does nothing in build mode, so G is free there. None to disable.");
            ClearKey = file.Bind("0. Keys", "ClearFigure", KeyCode.Backspace,
                "Removes the whole figure. None to disable.");
            RowsOnMouseWheel = file.Bind("0. Keys", "RowsOnMouseWheel", true,
                "In figure mode the mouse wheel changes the number of rows instead of rotating (plants get a random rotation anyway).");

            const string behaviour = "1. Behaviour";
            EnabledByDefault = file.Bind(behaviour, "EnabledByDefault", true,
                "Start in figure mode. Switch to ordinary planting from the planting wheel.");
            DefaultSpacingFactor = file.Bind(behaviour, "DefaultSpacingFactor", 2.0f,
                new ConfigDescription("Initial spacing = plant grow radius × this factor.", new AcceptableValueRange<float>(1f, 6f)));
            MinDistanceMargin = file.Bind(behaviour, "MinDistanceMargin", 0.35f,
                new ConfigDescription("Minimum distance between plants = grow radius + this margin. Jitter never goes below it.", new AcceptableValueRange<float>(0f, 2f)));
            MaxPlants = file.Bind(behaviour, "MaxPlants", 300,
                new ConfigDescription("Hard cap on plants per figure, protects frame rate and the server.", new AcceptableValueRange<int>(10, 2000)));
            MaxFigureSize = file.Bind(behaviour, "MaxFigureSize", 80f,
                new ConfigDescription("Largest figure in meters (longer side; diameter for a circle). Bigger figures are not previewed or planted, so a stray click far away cannot freeze the game.", new AcceptableValueRange<float>(10f, 500f)));
            RandomRotation = file.Bind(behaviour, "RandomRotation", true, "Give every plant a random yaw instead of the cursor rotation.");
            WarpScale = file.Bind(behaviour, "WarpScale", 6f,
                new ConfigDescription("Size in meters of the slow waves that bend whole rows when jitter is on.", new AcceptableValueRange<float>(1f, 30f)));
            RevalidateInterval = file.Bind(behaviour, "RevalidateInterval", 0.5f, "Seconds between re-checks of ground validity for the preview.");
            SnapRadius = file.Bind(behaviour, "SnapRadius", 0.8f,
                new ConfigDescription("Click this close to the last point to finish a curve, or to the first point to close a shape.", new AcceptableValueRange<float>(0.2f, 3f)));
            AutoContinueInterval = file.Bind(behaviour, "AutoContinueInterval", 0.6f,
                new ConfigDescription("Seconds between plants when auto-continue is on.", new AcceptableValueRange<float>(0.2f, 5f)));

            const string display = "2. Display";
            ShowHud = file.Bind(display, "ShowHud", true, "Show the status panel while a seed is selected in figure mode.");
            HudX = file.Bind(display, "HudX", 20, "Panel X position in pixels from the left.");
            HudY = file.Bind(display, "HudY", 200, "Panel Y position in pixels from the top.");
            ShowCurve = file.Bind(display, "ShowCurve", true, "Draw the guide path and rows as lines.");
            ShowGrowRadius = file.Bind(display, "ShowGrowRadius", true, "Draw each plant's grow radius as a ring on the ground.");
            CurveWidth = file.Bind(display, "CurveWidth", 0.08f, "Line width in meters.");
            CurveColor = file.Bind(display, "CurveColor", new Color(0.3f, 0.9f, 0.4f, 0.8f), "Row line colour.");
            ControlPointColor = file.Bind(display, "ControlPointColor", new Color(1f, 0.85f, 0.2f, 0.9f), "Control point marker colour.");
        }
    }
}
