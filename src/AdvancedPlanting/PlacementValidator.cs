using UnityEngine;

namespace AdvancedPlanting
{
    public enum PointStatus
    {
        Valid,
        NoGround,
        NoBuildZone,
        PrivateZone,
        NeedCultivated,
        WrongBiome,
        InWater,
        NoSpace,
    }

    /// <summary>
    /// Mirrors the subset of Player.UpdatePlacementGhost checks that matter for plants, for an
    /// arbitrary world position instead of the cursor.
    /// </summary>
    public static class PlacementValidator
    {
        /// <summary>Samples terrain height under (x, z). Returns false when there is no ground.</summary>
        public static bool TryGetGroundHeight(float x, float z, float probeY, out float height)
        {
            height = 0f;
            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null) return false;
            return zs.GetGroundHeight(new Vector3(x, probeY, z), out height);
        }

        public static PointStatus Check(Vector3 pos, Piece piece, Plant plant)
        {
            if (Location.IsInsideNoBuildLocation(pos)) return PointStatus.NoBuildZone;
            if (!PrivateArea.CheckAccess(pos, 0f, false)) return PointStatus.PrivateZone;

            ZoneSystem zs = ZoneSystem.instance;
            if (piece != null && piece.m_noInWater && zs != null && pos.y < zs.m_waterLevel) return PointStatus.InWater;

            bool needCultivated = (piece != null && piece.m_cultivatedGroundOnly) || (plant != null && plant.m_needCultivatedGround);
            if (needCultivated)
            {
                Heightmap hm = Heightmap.FindHeightmap(pos);
                if (hm == null || !hm.IsCultivated(pos)) return PointStatus.NeedCultivated;
            }

            if (plant != null && plant.m_biome != Heightmap.Biome.None)
            {
                Heightmap.Biome here = Heightmap.FindBiome(pos);
                if ((plant.m_biome & here) == 0) return PointStatus.WrongBiome;
            }

            if (plant != null && !HaveGrowSpace(pos, plant)) return PointStatus.NoSpace;

            return PointStatus.Valid;
        }

        /// <summary>Same test Plant.HaveGrowSpace does at grow time, applied before planting.</summary>
        public static bool HaveGrowSpace(Vector3 pos, Plant plant)
        {
            float radius = plant.m_growRadius;
            if (radius <= 0f) return true;
            Collider[] hits = Physics.OverlapSphere(pos, radius, plant.m_spaceMask);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c == null || !c.enabled) continue;
                if (GhostPool.IsGhost(c.gameObject)) continue;
                return false;
            }
            return true;
        }

        public static string Describe(PointStatus status)
        {
            switch (status)
            {
                case PointStatus.Valid: return "ok";
                case PointStatus.NoGround: return "no ground";
                case PointStatus.NoBuildZone: return "no-build zone";
                case PointStatus.PrivateZone: return "ward";
                case PointStatus.NeedCultivated: return "needs cultivated ground";
                case PointStatus.WrongBiome: return "wrong biome";
                case PointStatus.InWater: return "in water";
                case PointStatus.NoSpace: return "no space";
                default: return status.ToString();
            }
        }
    }
}
