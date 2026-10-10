using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Area harvest: hold the modifier and pick a ripe crop to pick every crop of the same kind
    /// within the radius around it.
    ///
    /// Each extra pick goes through Pickable.Interact exactly as if the player had pressed the
    /// use key on it, so drops, skill, stats and server ownership all stay vanilla.
    /// </summary>
    internal static class Harvest
    {
        private static readonly Collider[] Hits = new Collider[4096];
        private static readonly HashSet<Pickable> Seen = new HashSet<Pickable>();
        private static bool _running;

        private static ModConfig Config => Plugin.Instance?.Settings;

        /// <summary>Area harvest is on and the modifier is held.</summary>
        private static bool Armed
        {
            get
            {
                ModConfig c = Config;
                if (c == null || !c.HarvestOn) return false;
                KeyCode key = c.HarvestModifier.Value;
                return key == KeyCode.None || ZInput.GetKey(key, logWarning: false);
            }
        }

        /// <summary>Picks the matching crops around <paramref name="target"/> after the player picked it.</summary>
        private static void PickAround(Pickable target, Player player)
        {
            ModConfig c = Config;
            float radius = c.HarvestRadiusLimit;
            Vector3 center = target.transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, radius, Hits, player.m_interactMask, QueryTriggerInteraction.Collide);
            Seen.Clear();
            Seen.Add(target);
            int picked = 0;
            for (int i = 0; i < count; i++)
            {
                Pickable p = Hits[i] != null ? Hits[i].GetComponentInParent<Pickable>() : null;
                if (p == null || !Seen.Add(p) || !p.CanBePicked()) continue;
                if (c.HarvestSameType.Value && p.m_itemPrefab != target.m_itemPrefab) continue;
                if ((p.transform.position - center).sqrMagnitude > radius * radius) continue;
                p.Interact(player, false, false);
                picked++;
            }
            Seen.Clear();
            if (picked > 0) Plugin.Log.LogDebug($"Area harvest: {picked + 1} picked within {radius} m.");
        }

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static class Pickable_Interact_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Pickable __instance, out bool __state)
            {
                // Remember ripeness before vanilla picks it; afterwards it reads as picked.
                __state = !_running && __instance.CanBePicked();
            }

            [HarmonyPostfix]
            private static void Postfix(Pickable __instance, Humanoid character, bool repeat, bool __state)
            {
                if (!__state || repeat || _running) return;
                if (!(character is Player player) || player != Player.m_localPlayer || !Armed) return;
                _running = true;
                try
                {
                    PickAround(__instance, player);
                }
                finally
                {
                    _running = false;
                }
            }
        }

        /// <summary>Adds the area-harvest line under vanilla's "[E] Pick up".</summary>
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
        private static class Pickable_GetHoverText_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(ref string __result)
            {
                ModConfig c = Config;
                if (string.IsNullOrEmpty(__result) || c == null || !c.HarvestOn || !c.HarvestHint.Value) return;
                string use = GameText.Localize("$KEY_Use");
                KeyCode mod = c.HarvestModifier.Value;
                string keys = mod == KeyCode.None ? use : KeyName(mod) + " + " + use;
                __result += "\n[<color=yellow><b>" + keys + "</b></color>] " + Loc.T("harvest.hover", Mathf.RoundToInt(c.HarvestRadiusLimit));
            }
        }

        private static string KeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftShift:
                case KeyCode.RightShift: return "Shift";
                case KeyCode.LeftControl:
                case KeyCode.RightControl: return "Ctrl";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt: return "Alt";
                default: return key.ToString();
            }
        }
    }
}
