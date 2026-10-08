using System.Collections.Generic;
using AdvancedPlanting.Core;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>One plant position in world space with its validity.</summary>
    public struct PlantSlot
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public PointStatus Status;
        public int Row;
        public bool IsValid => Status == PointStatus.Valid;
    }

    /// <summary>
    /// Bridges the pure 2D layout from the core library and the 3D game world: samples terrain
    /// height for every point, validates it, and prepares the row curves for drawing.
    /// </summary>
    public sealed class LayoutService
    {
        private readonly ModConfig _config;

        public readonly List<PlantSlot> Slots = new List<PlantSlot>();
        public readonly List<List<Vector3>> RowLines = new List<List<Vector3>>();
        public bool Truncated { get; private set; }
        public int ValidCount { get; private set; }
        public int RejectedByDistance { get; private set; }

        /// <summary>Plant prefab the current slots were built for.</summary>
        public string BuiltForPrefab { get; private set; }

        public LayoutService(ModConfig config)
        {
            _config = config;
        }

        public float DefaultSpacing(Plant plant)
        {
            float radius = plant != null ? plant.m_growRadius : 1f;
            if (radius <= 0.05f) radius = 0.5f;
            return Mathf.Round(radius * _config.DefaultSpacingFactor.Value * 100f) / 100f;
        }

        public float MinDistance(Plant plant)
        {
            float radius = plant != null ? plant.m_growRadius : 1f;
            if (radius <= 0.05f) radius = 0.5f;
            return radius + _config.MinDistanceMargin.Value;
        }

        public void Clear()
        {
            Slots.Clear();
            RowLines.Clear();
            Truncated = false;
            ValidCount = 0;
            RejectedByDistance = 0;
            BuiltForPrefab = null;
        }

        /// <summary>Full rebuild: geometry, heights and validation.</summary>
        public void Rebuild(FigureState state, Piece piece, Plant plant, string prefabName)
        {
            Clear();
            BuiltForPrefab = prefabName;
            if (!state.HasEnoughPoints) return;

            float spacing = state.GetSpacing(prefabName, DefaultSpacing(plant));
            var request = new LayoutRequest
            {
                Mode = state.Mode,
                Spacing = spacing,
                RowSpacing = spacing,
                Rows = state.Rows,
                Side = state.Side,
                Jitter = state.Jitter,
                Thinning = state.Thinning,
                Seed = state.Seed,
                MinDistance = MinDistance(plant),
                WarpScale = _config.WarpScale.Value,
                MaxPoints = _config.MaxPlants.Value,
            };
            foreach (Vector3 p in state.ControlPoints) request.ControlPoints.Add(new Vec2(p.x, p.z));

            LayoutResult layout = PlantingLayout.Generate(request);
            Truncated = layout.Truncated;
            RejectedByDistance = layout.Rejected;

            float probeY = state.ControlPoints[0].y + 50f;
            foreach (LayoutPoint lp in layout.Points)
            {
                var slot = new PlantSlot { Row = lp.Row };
                if (!PlacementValidator.TryGetGroundHeight(lp.Position.X, lp.Position.Z, probeY, out float h))
                {
                    slot.Position = new Vector3(lp.Position.X, state.ControlPoints[0].y, lp.Position.Z);
                    slot.Status = PointStatus.NoGround;
                }
                else
                {
                    slot.Position = new Vector3(lp.Position.X, h, lp.Position.Z);
                    slot.Status = PlacementValidator.Check(slot.Position, piece, plant);
                }
                slot.Rotation = RotationFor(slot.Position, state.Seed);
                if (slot.IsValid) ValidCount++;
                Slots.Add(slot);
            }

            BuildRowLines(layout, probeY);
        }

        /// <summary>Cheap pass: re-check ground rules without regenerating geometry.</summary>
        public void Revalidate(Piece piece, Plant plant)
        {
            ValidCount = 0;
            for (int i = 0; i < Slots.Count; i++)
            {
                PlantSlot slot = Slots[i];
                if (slot.Status == PointStatus.NoGround) continue;
                slot.Status = PlacementValidator.Check(slot.Position, piece, plant);
                if (slot.IsValid) ValidCount++;
                Slots[i] = slot;
            }
        }

        private Quaternion RotationFor(Vector3 pos, uint seed)
        {
            if (!_config.RandomRotation.Value) return Quaternion.identity;
            int id = unchecked(Mathf.RoundToInt(pos.x * 20f) * 73856093 ^ Mathf.RoundToInt(pos.z * 20f) * 19349663);
            float yaw = Noise.Float01(id, unchecked(seed + 0x2545F491u)) * 360f;
            return Quaternion.Euler(0f, yaw, 0f);
        }

        private void BuildRowLines(LayoutResult layout, float probeY)
        {
            const float drawStep = 0.5f;
            foreach (Polyline curve in layout.RowCurves)
            {
                if (curve.IsDegenerate) continue;
                var line = new List<Vector3>();
                int n = Mathf.Max(2, Mathf.CeilToInt(curve.Length / drawStep));
                int total = curve.IsClosed ? n + 1 : n + 1;
                for (int i = 0; i < total; i++)
                {
                    float d = curve.Length * i / n;
                    Vec2 p = curve.PointAt(d);
                    float y = probeY - 50f;
                    if (PlacementValidator.TryGetGroundHeight(p.X, p.Z, probeY, out float h)) y = h;
                    line.Add(new Vector3(p.X, y + 0.15f, p.Z));
                }
                RowLines.Add(line);
            }
        }
    }
}
