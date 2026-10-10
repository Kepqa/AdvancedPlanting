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
        NeedDirt,
        WrongBiome,
        TooHot,
        TooCold,
        DeepSnow,
        InWater,
        NoSpace,
        /// <summary>Already planted by this figure; not a problem, just done.</summary>
        Planted,
        /// <summary>Not checked yet; validation is spread over several frames.</summary>
        Pending,
    }

    /// <summary>
    /// Mirrors the plant-relevant checks of Player.UpdatePlacementGhost and Plant.HaveGrowSpace
    /// (verified against the decompiled game) for an arbitrary position instead of the cursor.
    /// Growth-time checks (biome, heat, cold) are included too: vanilla lets you plant there,
    /// but the plant would just wither, so the mod refuses to waste the seed.
    /// </summary>
    public static class PlacementValidator
    {
        private static int s_spaceMask;

        /// <summary>Same layers Plant.HaveGrowSpace uses. Plant.m_spaceMask is lazily set, so do not read it.</summary>
        private static int SpaceMask
        {
            get
            {
                if (s_spaceMask == 0)
                    s_spaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
                return s_spaceMask;
            }
        }

        /// <summary>Terrain height under (x, z), same ray and mask as ZoneSystem.GetGroundHeight.</summary>
        public static bool TryGetGroundHeight(float x, float z, out float height)
        {
            bool ok = TryGetGround(x, z, out height, out _);
            return ok;
        }

        /// <summary>Terrain height and surface normal under (x, z) with a single ray.</summary>
        public static bool TryGetGround(float x, float z, out float height, out Vector3 normal)
        {
            height = 0f;
            normal = Vector3.up;
            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null) return false;
            if (!Physics.Raycast(new Vector3(x, 6000f, z), Vector3.down, out RaycastHit hit, 10000f, zs.m_terrainRayMask))
                return false;
            height = hit.point.y;
            normal = hit.normal;
            return true;
        }

        public static PointStatus Check(Vector3 pos, Piece piece, Plant plant, Player player)
        {
            if (Location.IsInsideNoBuildLocation(pos)) return PointStatus.NoBuildZone;
            if (!PrivateArea.CheckAccess(pos, 0f, false, false)) return PointStatus.PrivateZone;

            Heightmap hm = Heightmap.FindHeightmap(pos);
            if (hm == null && (piece.m_groundOnly || piece.m_cultivatedGroundOnly || piece.m_vegetationGroundOnly))
                return PointStatus.NoGround;

            ZoneSystem zs = ZoneSystem.instance;
            if (piece.m_noInWater && zs != null && pos.y < zs.m_waterLevel) return PointStatus.InWater;

            bool needCultivated = piece.m_cultivatedGroundOnly || (plant != null && plant.m_needCultivatedGround);
            if (needCultivated && (hm == null || !hm.IsCultivated(pos))) return PointStatus.NeedCultivated;

            Heightmap.Biome biome = hm != null ? hm.GetBiome(pos) : Heightmap.FindBiome(pos);

            if (piece.m_vegetationGroundOnly && hm != null)
            {
                float veg = hm.GetVegetationMask(pos);
                bool noDirt = biome == Heightmap.Biome.AshLands ? veg > 0.1f : veg < 0.25f;
                if (noDirt) return PointStatus.NeedDirt;
            }

            if (piece.m_onlyInBiome != Heightmap.Biome.None && (biome & piece.m_onlyInBiome) == 0) return PointStatus.WrongBiome;

            if (plant != null)
            {
                if ((biome & plant.m_biome) == 0) return PointStatus.WrongBiome;
                if (!plant.m_tolerateHeat && biome == Heightmap.Biome.AshLands && !ShieldGenerator.IsInsideShield(pos)) return PointStatus.TooHot;
                if (!plant.m_tolerateCold && (biome == Heightmap.Biome.DeepNorth || biome == Heightmap.Biome.Mountain) && !ShieldGenerator.IsInsideShield(pos))
                    return PointStatus.TooCold;
            }

            if (!piece.m_allowedInDeepSnow && biome == Heightmap.Biome.DeepNorth && hm != null && player != null
                && hm.GetCultivationMask(pos) > player.m_deepSnowBuildHeight)
                return PointStatus.DeepSnow;

            if (plant != null && !HaveGrowSpace(pos, plant)) return PointStatus.NoSpace;

            return PointStatus.Valid;
        }

        /// <summary>
        /// Plant.HaveGrowSpace at a position: any collider in the grow radius blocks, except plants
        /// that are not healthy. Our ghosts live on the "ghost" layer and are never hit.
        /// </summary>
        public static bool HaveGrowSpace(Vector3 pos, Plant plant)
        {
            float radius = plant.m_growRadius;
            if (radius > 0f)
            {
                Collider[] hits = Physics.OverlapSphere(pos, radius, SpaceMask);
                for (int i = 0; i < hits.Length; i++)
                {
                    Plant other = hits[i].GetComponent<Plant>();
                    if (other == null || other.GetStatus() == Plant.Status.Healthy) return false;
                }
            }
            if (plant.m_growRadiusVines > 0f)
            {
                Collider[] hits = Physics.OverlapSphere(pos, plant.m_growRadiusVines, SpaceMask);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].GetComponentInParent<Vine>() != null) return false;
                }
            }
            return true;
        }

        public static string Describe(PointStatus status)
        {
            switch (status)
            {
                case PointStatus.Valid: return Loc.T("reason.ok");
                case PointStatus.NoGround: return Loc.T("reason.no_ground");
                case PointStatus.NoBuildZone: return Loc.T("reason.no_build");
                case PointStatus.PrivateZone: return Loc.T("reason.ward");
                case PointStatus.NeedCultivated: return Loc.T("reason.not_cultivated");
                case PointStatus.NeedDirt: return Loc.T("reason.needs_dirt");
                case PointStatus.WrongBiome: return Loc.T("reason.wrong_biome");
                case PointStatus.TooHot: return Loc.T("reason.too_hot");
                case PointStatus.TooCold: return Loc.T("reason.too_cold");
                case PointStatus.DeepSnow: return Loc.T("reason.deep_snow");
                case PointStatus.InWater: return Loc.T("reason.in_water");
                case PointStatus.NoSpace: return Loc.T("reason.no_space");
                case PointStatus.Planted: return Loc.T("reason.planted");
                case PointStatus.Pending: return Loc.T("reason.pending");
                default: return status.ToString();
            }
        }
    }
}
