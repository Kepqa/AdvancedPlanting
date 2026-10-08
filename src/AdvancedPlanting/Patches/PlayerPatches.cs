using System.Reflection;
using HarmonyLib;

namespace AdvancedPlanting.Patches
{
    /// <summary>After vanilla moved its own ghost, refresh our figure preview.</summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    internal static class Player_UpdatePlacementGhost_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null) return;
            plugin.OnPlacementGhostUpdated(__instance);
        }
    }

    /// <summary>
    /// Replaces the single vanilla placement with the whole figure when one is active.
    /// The method is resolved at runtime because its signature differs between game versions.
    /// </summary>
    [HarmonyPatch]
    internal static class Player_PlacePiece_Patch
    {
        private static MethodBase TargetMethod() => VanillaBridge.ResolvePlacePiece();

        private static bool Prepare() => VanillaBridge.ResolvePlacePiece() != null;

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
    /// Vanilla rotates the piece on mouse wheel. When one of our modifier keys is held we
    /// consume the wheel for our own parameters and restore the rotation afterwards.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacement")]
    internal static class Player_UpdatePlacement_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, out int __state)
        {
            __state = __instance.m_placeRotation;
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance, int __state)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null) return;
            if (plugin.HandleScroll(__instance)) __instance.m_placeRotation = __state;
        }
    }
}
