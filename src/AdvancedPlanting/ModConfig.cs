using BepInEx.Configuration;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>All user-facing settings. Written to BepInEx/config/kepqa.AdvancedPlanting.cfg.</summary>
    public sealed class ModConfig
    {
        // Keys
        public ConfigEntry<KeyCode> AddPointKey;
        public ConfigEntry<KeyCode> RemovePointKey;
        public ConfigEntry<KeyCode> ClearKey;
        public ConfigEntry<KeyCode> CycleModeKey;
        public ConfigEntry<KeyCode> CycleSideKey;
        public ConfigEntry<KeyCode> RerollSeedKey;
        public ConfigEntry<KeyCode> ToggleModKey;

        // Scroll modifiers
        public ConfigEntry<KeyCode> SpacingModifier;
        public ConfigEntry<KeyCode> RowsModifier;
        public ConfigEntry<KeyCode> JitterModifier;
        public ConfigEntry<KeyCode> ThinningModifier;

        // Behaviour
        public ConfigEntry<bool> EnabledByDefault;
        public ConfigEntry<float> DefaultSpacingFactor;
        public ConfigEntry<float> MinDistanceMargin;
        public ConfigEntry<float> SpacingStep;
        public ConfigEntry<float> JitterStep;
        public ConfigEntry<float> ThinningStep;
        public ConfigEntry<int> MaxPlants;
        public ConfigEntry<bool> RandomRotation;
        public ConfigEntry<bool> KeepFigureAfterPlanting;
        public ConfigEntry<float> WarpScale;
        public ConfigEntry<float> RevalidateInterval;

        // Display
        public ConfigEntry<bool> ShowHud;
        public ConfigEntry<int> HudX;
        public ConfigEntry<int> HudY;
        public ConfigEntry<bool> ShowCurve;
        public ConfigEntry<float> CurveWidth;
        public ConfigEntry<Color> CurveColor;
        public ConfigEntry<Color> ControlPointColor;

        public ModConfig(ConfigFile file)
        {
            const string keys = "1. Keys";
            AddPointKey = file.Bind(keys, "AddPoint", KeyCode.F, "Add a control point at the placement cursor.");
            RemovePointKey = file.Bind(keys, "RemoveLastPoint", KeyCode.Z, "Remove the last control point.");
            ClearKey = file.Bind(keys, "ClearFigure", KeyCode.X, "Remove all control points.");
            CycleModeKey = file.Bind(keys, "CycleMode", KeyCode.G, "Cycle path mode: segment, arc, circle, spline, contour.");
            CycleSideKey = file.Bind(keys, "CycleRowSide", KeyCode.H, "Cycle which side of the path the rows go to (both, left, right).");
            RerollSeedKey = file.Bind(keys, "RerollSeed", KeyCode.R, "Re-roll the jitter/thinning seed.");
            ToggleModKey = file.Bind(keys, "ToggleMod", KeyCode.F7, "Enable or disable AdvancedPlanting without unloading it.");

            const string scroll = "2. Scroll modifiers";
            SpacingModifier = file.Bind(scroll, "Spacing", KeyCode.LeftAlt, "Hold and scroll to change spacing between plants.");
            RowsModifier = file.Bind(scroll, "Rows", KeyCode.LeftControl, "Hold and scroll to change the number of rows.");
            JitterModifier = file.Bind(scroll, "Jitter", KeyCode.LeftShift, "Hold and scroll to change jitter (0..1).");
            ThinningModifier = file.Bind(scroll, "ThinningSecondModifier", KeyCode.LeftAlt, "Hold together with the Jitter modifier and scroll to change thinning (0..1).");

            const string behaviour = "3. Behaviour";
            EnabledByDefault = file.Bind(behaviour, "EnabledByDefault", true, "Whether the mod is active when the game starts.");
            DefaultSpacingFactor = file.Bind(behaviour, "DefaultSpacingFactor", 2.0f,
                new ConfigDescription("Initial spacing = plant grow radius × this factor.", new AcceptableValueRange<float>(1f, 6f)));
            MinDistanceMargin = file.Bind(behaviour, "MinDistanceMargin", 0.35f,
                new ConfigDescription("Minimum distance between plants = grow radius + this margin. Jitter never goes below it.", new AcceptableValueRange<float>(0f, 2f)));
            SpacingStep = file.Bind(behaviour, "SpacingStep", 0.1f, "Spacing change per scroll notch, in meters.");
            JitterStep = file.Bind(behaviour, "JitterStep", 0.1f, "Jitter change per scroll notch.");
            ThinningStep = file.Bind(behaviour, "ThinningStep", 0.05f, "Thinning change per scroll notch.");
            MaxPlants = file.Bind(behaviour, "MaxPlants", 300,
                new ConfigDescription("Hard cap on plants per figure, protects frame rate and the server.", new AcceptableValueRange<int>(10, 2000)));
            RandomRotation = file.Bind(behaviour, "RandomRotation", true, "Give every plant a random yaw instead of the cursor rotation.");
            KeepFigureAfterPlanting = file.Bind(behaviour, "KeepFigureAfterPlanting", false, "Keep control points after planting instead of clearing them.");
            WarpScale = file.Bind(behaviour, "WarpScale", 6f,
                new ConfigDescription("Size in meters of the slow waves that bend whole rows when jitter is on.", new AcceptableValueRange<float>(1f, 30f)));
            RevalidateInterval = file.Bind(behaviour, "RevalidateInterval", 0.5f, "Seconds between re-checks of ground validity for the preview.");

            const string display = "4. Display";
            ShowHud = file.Bind(display, "ShowHud", true, "Show the status panel while a figure is being edited.");
            HudX = file.Bind(display, "HudX", 20, "Panel X position in pixels from the left.");
            HudY = file.Bind(display, "HudY", 200, "Panel Y position in pixels from the top.");
            ShowCurve = file.Bind(display, "ShowCurve", true, "Draw the guide path and rows as lines.");
            CurveWidth = file.Bind(display, "CurveWidth", 0.08f, "Line width in meters.");
            CurveColor = file.Bind(display, "CurveColor", new Color(0.3f, 0.9f, 0.4f, 0.8f), "Row line colour.");
            ControlPointColor = file.Bind(display, "ControlPointColor", new Color(1f, 0.85f, 0.2f, 0.9f), "Control point marker colour.");
        }
    }
}
