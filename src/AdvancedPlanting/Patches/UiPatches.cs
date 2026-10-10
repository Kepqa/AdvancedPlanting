using HarmonyLib;
using UnityEngine;
using Valheim.UI;

namespace AdvancedPlanting.Patches
{
    /// <summary>
    /// Vanilla blocks the radial menu in build mode, so the radial key does nothing while a seed
    /// is selected. We lift that block only in that situation and show the planting wheel.
    /// </summary>
    [HarmonyPatch(typeof(RadialBase), nameof(RadialBase.InRadialBlockingPlaceMode), MethodType.Getter)]
    internal static class RadialBase_InRadialBlockingPlaceMode_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (__result && Plugin.Instance != null && Plugin.Instance.WantsPlantingWheel) __result = false;
        }
    }

    /// <summary>
    /// The radial key opens OpenRadialConfig, which then picks the wheel to show (main, emotes,
    /// hover menus). With a seed selected we pick the planting wheel. The emote key keeps
    /// opening emotes.
    /// </summary>
    [HarmonyPatch(typeof(OpenRadialConfig), nameof(OpenRadialConfig.InitRadialConfig))]
    internal static class OpenRadialConfig_InitRadialConfig_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(RadialBase radial)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !plugin.WantsPlantingWheel || ZInput.GetButton("OpenEmote")) return true;

            // Same setup OpenRadialConfig does before opening a group.
            radial.OnInteractionDelay = delay => PlayerController.SetTakeInputDelay(delay);
            radial.ShouldAnimateIn = true;
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
