using HarmonyLib;
using UnityEngine;

namespace AdvancedPlanting.Patches
{
    /// <summary>After vanilla positioned its own ghost, refresh the figure preview.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    internal static class Player_UpdatePlacementGhost_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            Plugin.Instance?.OnPlacementGhostUpdated(__instance);
        }
    }

    /// <summary>
    /// Stamina per placement. Vanilla pays the first plant of a figure with it and the mod pays
    /// every other plant with it too, so scaling it here covers the whole figure and auto-continue.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetBuildStamina))]
    internal static class Player_GetBuildStamina_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, ref float __result)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin != null) __result = plugin.ScaleBuildStamina(__instance, __result);
        }
    }

    /// <summary>
    /// The click handler in Player.UpdatePlacement calls TryPlacePiece and, when it returns
    /// true, pays for one placement. With a finished figure we plant the whole figure instead
    /// (paying for every plant beyond the first ourselves) and skip the vanilla single placement.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    internal static class Player_TryPlacePiece_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null) return true;
            if (!plugin.TryPlantFigure(__instance, piece, out bool placed)) return true;
            __result = placed;
            return false;
        }
    }

    /// <summary>
    /// While a figure is being drawn, a left click adds a control point instead of planting.
    /// In figure mode the mouse wheel changes the row count instead of rotating the ghost.
    /// The click is consumed by making vanilla believe the tool was just used, then the
    /// pending press is discarded so it cannot fire a frame later.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    internal static class Player_UpdatePlacement_Patch
    {
        internal struct ClickState
        {
            public bool Consumed;
            public float LastToolUseTime;
            public int PlaceRotation;
        }

        [HarmonyPrefix]
        private static void Prefix(Player __instance, bool takeInput, out ClickState __state)
        {
            __state = default;
            __state.PlaceRotation = __instance.m_placeRotation;
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !takeInput) return;
            if (!plugin.HandlePlacementClick(__instance)) return;
            __state.Consumed = true;
            __state.LastToolUseTime = __instance.m_lastToolUseTime;
            __instance.m_lastToolUseTime = Time.time;
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool takeInput, ClickState __state)
        {
            Plugin plugin = Plugin.Instance;
            // In figure mode the wheel changes rows; undo the rotation vanilla applied.
            if (plugin != null && takeInput && plugin.HandleMouseWheel(__instance))
                __instance.m_placeRotation = __state.PlaceRotation;
            if (!__state.Consumed) return;
            __instance.m_lastToolUseTime = __state.LastToolUseTime;
            __instance.m_placePressedTime = -9999f;
        }
    }
}
