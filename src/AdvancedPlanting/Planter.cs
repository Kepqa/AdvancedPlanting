using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Plants a whole figure in place of one vanilla placement, with the same costs per plant.
    ///
    /// Vanilla flow (Player.UpdatePlacement): check stamina and requirements once, call
    /// TryPlacePiece, and if it returns true pay once: resources, stamina, tool durability, skill.
    /// We replace TryPlacePiece. The first plant is paid by that vanilla code after we return true;
    /// every additional plant is paid here, immediately, with the same formulas. Auto-continue
    /// runs outside that flow and pays for every plant here.
    /// </summary>
    public static class Planter
    {
        public struct Outcome
        {
            public int Planted;
            public int Blocked;
            public bool OutOfSeeds;
            public bool OutOfStamina;
            public bool ToolBroken;
        }

        /// <summary>
        /// Plants valid slots, nearest to the player first.
        /// </summary>
        /// <param name="vanillaPaysFirst">
        /// True when called from TryPlacePiece: vanilla pays for the first plant after we return.
        /// False for auto-continue, where nothing in vanilla pays, so every plant is paid here.
        /// </param>
        /// <param name="limit">Maximum plants to place in this call.</param>
        /// <param name="onPlanted">Receives the position of every plant placed.</param>
        public static Outcome Plant(Player player, Piece piece, List<PlantSlot> slots, bool vanillaPaysFirst, int limit, Action<Vector3> onPlanted)
        {
            var outcome = new Outcome();
            ItemDrop.ItemData tool = player.GetRightItem();
            if (tool == null) return outcome;

            bool free = player.m_noPlacementCost
                || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()));
            bool cheated = (player.m_inventory.ItemCheated(piece.m_resources) || player.NoCostCheat())
                && !PlayerProfile.s_bypassCheatChecks;
            float stamina = player.GetBuildStamina();

            var order = new List<int>(slots.Count);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsValid) order.Add(i);
                else if (slots[i].IsBlocked) outcome.Blocked++;
            }
            Vector3 origin = player.transform.position;
            order.Sort((a, b) => SqrFlat(slots[a].Position, origin).CompareTo(SqrFlat(slots[b].Position, origin)));

            // One planting sound for the whole batch instead of hundreds stacked on each other.
            EffectList placeEffect = piece.m_placeEffect;
            try
            {
                foreach (int index in order)
                {
                    if (outcome.Planted >= limit) break;
                    PlantSlot slot = slots[index];

                    bool paidByVanilla = vanillaPaysFirst && outcome.Planted == 0;
                    if (!paidByVanilla)
                    {
                        // When vanilla still owes the first plant, keep its seeds and stamina in reserve.
                        int reserve = vanillaPaysFirst ? 1 : 0;
                        if (!free && !HaveResources(player, piece, 1 + reserve))
                        {
                            outcome.OutOfSeeds = true;
                            break;
                        }
                        if (!player.HaveStamina(stamina * (1 + reserve)))
                        {
                            outcome.OutOfStamina = true;
                            break;
                        }
                        if (tool.m_shared.m_useDurability && tool.m_durability <= 0f)
                        {
                            outcome.ToolBroken = true;
                            break;
                        }
                        PayForPlant(player, piece, tool, free, stamina);
                    }

                    piece.m_placeEffect = outcome.Planted == 0 ? placeEffect : EmptyEffects;
                    player.PlacePiece(piece, slot.Position, slot.Rotation, outcome.Planted == 0, cheated);
                    CountStats(piece, cheated);
                    outcome.Planted++;
                    onPlanted?.Invoke(slot.Position);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Planting failed: " + e);
            }
            finally
            {
                piece.m_placeEffect = placeEffect;
            }
            return outcome;
        }

        private static float SqrFlat(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>Whether one more plant can be paid for right now (seeds, stamina, tool).</summary>
        public static bool CanAffordOne(Player player, Piece piece)
        {
            ItemDrop.ItemData tool = player.GetRightItem();
            if (tool == null) return false;
            bool free = player.m_noPlacementCost
                || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()));
            if (!free && !HaveResources(player, piece, 1)) return false;
            if (tool.m_shared.m_useDurability && tool.m_durability <= 0f) return false;
            return player.HaveStamina(player.GetBuildStamina());
        }

        private static readonly EffectList EmptyEffects = new EffectList();

        /// <summary>Requirement check for <paramref name="sets"/> plants at once.</summary>
        private static bool HaveResources(Player player, Piece piece, int sets)
        {
            if (piece.m_resources == null) return true;
            Inventory inv = player.GetInventory();
            foreach (Piece.Requirement req in piece.m_resources)
            {
                if (req == null || req.m_resItem == null) continue;
                int amount = req.GetAmount(0) * sets;
                if (amount <= 0) continue;
                if (inv.CountItems(req.m_resItem.m_itemData.m_shared.m_name) < amount) return false;
            }
            return true;
        }

        /// <summary>Copy of what Player.UpdatePlacement pays after a successful TryPlacePiece.</summary>
        private static void PayForPlant(Player player, Piece piece, ItemDrop.ItemData tool, bool free, float stamina)
        {
            if (!free) player.ConsumeResources(piece.m_resources, 0);
            player.UseStamina(stamina);

            PieceTable table = player.m_buildPieces;
            if (table != null && table.m_skill != Skills.SkillType.None)
            {
                if (table.m_skill == Skills.SkillType.Crafting)
                    Game.instance.IncrementPlayerStat(PlayerStatType.BuiltPieces);
                if (player.m_buildRemoveDebt > 0)
                {
                    player.m_buildRemoveDebt--;
                }
                else
                {
                    player.RaiseSkill(table.m_skill);
                    if (table.m_skill == Skills.SkillType.Crafting)
                        Game.instance.IncrementPlayerStat(PlayerStatType.BuiltPiecesNoDebt);
                }
            }

            if (tool.m_shared.m_useDurability)
                tool.m_durability -= player.GetPlaceDurability(tool) * Game.m_durabilityRate;
        }

        /// <summary>Stats TryPlacePiece records for each placement; we replace it, so we record them.</summary>
        private static void CountStats(Piece piece, bool cheated)
        {
            Game game = Game.instance;
            if (game == null) return;
            game.IncrementPlayerStat(PlayerStatType.Builds, 1f, cheated);
            game.GetPlayerProfile()?.IncrementStatBuildPiecePlaced(piece.m_name, 1f, cheated);
        }
    }
}
