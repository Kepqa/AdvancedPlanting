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
        }

        [HarmonyPrefix]
        private static void Prefix(Player __instance, bool takeInput, out ClickState __state)
        {
            __state = default;
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !takeInput) return;
            if (!plugin.HandlePlacementClick(__instance)) return;
            __state.Consumed = true;
            __state.LastToolUseTime = __instance.m_lastToolUseTime;
            __instance.m_lastToolUseTime = Time.time;
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance, ClickState __state)
        {
            if (!__state.Consumed) return;
            __instance.m_lastToolUseTime = __state.LastToolUseTime;
            __instance.m_placePressedTime = -9999f;
        }
    }
}
