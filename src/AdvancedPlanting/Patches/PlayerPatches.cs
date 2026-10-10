using HarmonyLib;

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
    /// true, pays for one placement. With a figure active we plant the whole figure instead
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
    /// Vanilla rotates the ghost with the mouse wheel. While our modifier is held the wheel
    /// edits a figure parameter, so the rotation change is undone.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    internal static class Player_UpdatePlacement_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, out int __state)
        {
            __state = __instance.m_placeRotation;
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool takeInput, int __state)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !takeInput) return;
            if (plugin.HandleScroll(__instance)) __instance.m_placeRotation = __state;
        }
    }
}
