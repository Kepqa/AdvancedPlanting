using HarmonyLib;
using UnityEngine;
using Valheim.UI;

namespace AdvancedPlanting.Patches
{
    /// <summary>
    /// Vanilla blocks the radial menu in build mode. We lift that block only while opening the
    /// planting wheel from its own key; the vanilla radial key keeps its vanilla behaviour.
    /// </summary>
    [HarmonyPatch(typeof(RadialBase), nameof(RadialBase.InRadialBlockingPlaceMode), MethodType.Getter)]
    internal static class RadialBase_InRadialBlockingPlaceMode_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (__result && Plugin.Instance != null && Plugin.Instance.OpeningWheel) __result = false;
        }
    }

    /// <summary>
    /// Opening the radial menu goes through OpenRadialConfig, which picks the wheel to show.
    /// When we opened it, we pick the planting wheel and teach the close and release-to-use
    /// checks about our key (vanilla only knows its own radial and emote keys).
    /// </summary>
    [HarmonyPatch(typeof(OpenRadialConfig), nameof(OpenRadialConfig.InitRadialConfig))]
    internal static class OpenRadialConfig_InitRadialConfig_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(RadialBase radial)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !plugin.OpeningWheel) return true;

            // Same setup OpenRadialConfig does before opening a group.
            radial.OnInteractionDelay = delay => PlayerController.SetTakeInputDelay(delay);
            radial.ShouldAnimateIn = true;

            KeyCode key = plugin.Settings.WheelKey.Value;
            System.Func<bool> close = radial.GetClose;
            radial.GetClose = () =>
                (close != null && close())
                || (Time.frameCount != plugin.WheelOpenedFrame && ZInput.GetKeyDown(key));
            System.Func<bool> release = radial.GetReleaseToUse;
            radial.GetReleaseToUse = () =>
                (release != null && release())
                || (ZInput.GetKeyUp(key) && Time.unscaledTime - plugin.WheelKeyDownTime > RadialData.SO.HoldCloseDelay);

            radial.Open(plugin.Wheel);
            return false;
        }
    }

    /// <summary>
    /// The settings window counts as the build menu for the game: cursor is freed, placement
    /// and the Esc menu are suppressed. Our own code never relies on this method.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.IsPieceSelectionVisible))]
    internal static class Hud_IsPieceSelectionVisible_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (!__result && Plugin.Instance != null && Plugin.Instance.WindowBlocksInput) __result = true;
        }
    }

    /// <summary>No character actions while the settings window is open.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
    internal static class Player_TakeInput_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (__result && Plugin.Instance != null && Plugin.Instance.WindowBlocksInput) __result = false;
        }
    }

    /// <summary>No movement or camera look while the settings window is open.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
    internal static class PlayerController_TakeInput_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (__result && Plugin.Instance != null && Plugin.Instance.WindowBlocksInput) __result = false;
        }
    }

    /// <summary>Show the cursor for the settings window.</summary>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class GameCamera_UpdateMouseCapture_Patch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (Plugin.Instance == null || !Plugin.Instance.WindowBlocksInput) return;
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
        }
    }
}

namespace AdvancedPlanting.Patches
{
    /// <summary>Adds the figure hints to the vanilla build key hints every time they refresh.</summary>
    [HarmonyLib.HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
    internal static class KeyHints_UpdateHints_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix(KeyHints __instance)
        {
            Plugin.Instance?.KeyHintsUpdated(__instance);
        }
    }
}
