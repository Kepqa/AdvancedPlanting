using System.Collections.Generic;
using AdvancedPlanting.Core;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>One plant position in world space with its validity.</summary>
    public struct PlantSlot
    {
        public Vector3 Position;
        /// <summary>Ground normal at the position, for drawing the grow-radius ring on slopes.</summary>
        public Vector3 Normal;
        public Quaternion Rotation;
        public PointStatus Status;
        public int Row;
        public bool IsValid => Status == PointStatus.Valid;
        public bool IsPlanted => Status == PointStatus.Planted;
        public bool IsBlocked => Status != PointStatus.Valid && Status != PointStatus.Planted;
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
        /// <summary>Slots that can be planted right now.</summary>
        public int ValidCount { get; private set; }
        public int PlantedCount { get; private set; }
        public int BlockedCount => Slots.Count - ValidCount - PlantedCount;

        /// <summary>Control points the current slots were built from, including the cursor while drawing.</summary>
        public readonly List<Vector3> PreviewPoints = new List<Vector3>();

        private const float PlantedTolerance = 0.2f;
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
            PreviewPoints.Clear();
            PlantedCount = 0;
            Truncated = false;
            ValidCount = 0;
            RejectedByDistance = 0;
            BuiltForPrefab = null;
        }

        /// <summary>
        /// Full rebuild: geometry, heights and validation. While the figure is still being drawn,
        /// <paramref name="cursor"/> is used as the next control point, so the player sees the
        /// shape before committing to the click.
        /// </summary>
        public void Rebuild(FigureState state, Vector3? cursor, Piece piece, Plant plant, string prefabName, Player player)
        {
            Clear();
            BuiltForPrefab = prefabName;
            PreviewPoints.AddRange(state.ControlPoints);
            int max = state.Mode.MaxControlPoints();
            if (!state.Finished && cursor.HasValue && (max == 0 || PreviewPoints.Count < max))
                PreviewPoints.Add(cursor.Value);
            if (PreviewPoints.Count < state.Mode.MinControlPoints())
            {
                // Not a figure yet (e.g. the first two points of an arc): draw a plain guide line
                // through the points and the cursor so the player still sees where it goes.
                if (PreviewPoints.Count >= 2)
                {
                    var helper = new List<Vector3>();
                    foreach (Vector3 p in PreviewPoints) helper.Add(p + Vector3.up * 0.15f);
                    RowLines.Add(helper);
                }
                return;
            }

            float spacing = state.GetSpacing(prefabName, DefaultSpacing(plant));
            var request = new LayoutRequest
            {
                Mode = state.Mode,
                Spacing = spacing,
                RowSpacing = spacing,
                Rows = state.Rows,
                Fill = state.Fill && state.IsClosedPath,
                Side = state.Side,
                Jitter = state.Jitter,
                Thinning = state.Thinning,
                Seed = state.Seed,
                MinDistance = MinDistance(plant),
                WarpScale = _config.WarpScale.Value,
                MaxPoints = _config.MaxPlants.Value,
            };
            foreach (Vector3 p in PreviewPoints) request.ControlPoints.Add(new Vec2(p.x, p.z));

            LayoutResult layout = PlantingLayout.Generate(request);
            Truncated = layout.Truncated;
            RejectedByDistance = layout.Rejected;

            float fallbackY = PreviewPoints[0].y;
            foreach (LayoutPoint lp in layout.Points)
            {
                var slot = new PlantSlot { Row = lp.Row, Normal = Vector3.up };
                var flat = new Vector3(lp.Position.X, fallbackY, lp.Position.Z);
                if (state.IsPlanted(flat, PlantedTolerance))
                {
                    slot.Position = PlacementValidator.TryGetGround(flat.x, flat.z, out float ph, out Vector3 pn) ? new Vector3(flat.x, ph, flat.z) : flat;
                    slot.Status = PointStatus.Planted;
                }
                else if (!PlacementValidator.TryGetGround(lp.Position.X, lp.Position.Z, out float h, out Vector3 n))
                {
                    slot.Position = new Vector3(lp.Position.X, fallbackY, lp.Position.Z);
                    slot.Status = PointStatus.NoGround;
                }
                else
                {
                    slot.Position = new Vector3(lp.Position.X, h, lp.Position.Z);
                    slot.Normal = n;
                    slot.Status = PlacementValidator.Check(slot.Position, piece, plant, player);
                }
                slot.Rotation = RotationFor(slot.Position, state.Seed);
                Slots.Add(slot);
            }
            CountStatuses();

            BuildRowLines(layout, fallbackY);
        }

        /// <summary>Cheap pass: re-check ground rules without regenerating geometry.</summary>
        public void Revalidate(FigureState state, Piece piece, Plant plant, Player player)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                PlantSlot slot = Slots[i];
                if (slot.Status == PointStatus.NoGround || slot.Status == PointStatus.Planted) continue;
                if (state.IsPlanted(slot.Position, PlantedTolerance))
                {
                    slot.Status = PointStatus.Planted;
                    Slots[i] = slot;
                    continue;
                }
                slot.Status = PlacementValidator.Check(slot.Position, piece, plant, player);
                Slots[i] = slot;
            }
            CountStatuses();
        }

        /// <summary>Most frequent reason a slot is blocked, for the status panel.</summary>
        public PointStatus TopBlockReason { get; private set; }

        private readonly int[] _statusCounts = new int[32];

        private void CountStatuses()
        {
            System.Array.Clear(_statusCounts, 0, _statusCounts.Length);
            ValidCount = 0;
            PlantedCount = 0;
            foreach (PlantSlot s in Slots)
            {
                if (s.IsValid) ValidCount++;
                else if (s.IsPlanted) PlantedCount++;
                else _statusCounts[(int)s.Status]++;
            }
            int best = 0;
            TopBlockReason = PointStatus.Valid;
            for (int i = 1; i < _statusCounts.Length; i++)
            {
                if (_statusCounts[i] > best)
                {
                    best = _statusCounts[i];
                    TopBlockReason = (PointStatus)i;
                }
            }
        }

        private Quaternion RotationFor(Vector3 pos, uint seed)
        {
            if (!_config.RandomRotation.Value) return Quaternion.identity;
            int id = unchecked(Mathf.RoundToInt(pos.x * 20f) * 73856093 ^ Mathf.RoundToInt(pos.z * 20f) * 19349663);
            float yaw = Noise.Float01(id, unchecked(seed + 0x2545F491u)) * 360f;
            return Quaternion.Euler(0f, yaw, 0f);
        }

        private void BuildRowLines(LayoutResult layout, float fallbackY)
        {
            const float drawStep = 0.5f;
            foreach (Polyline curve in layout.RowCurves)
            {
                if (curve.IsDegenerate) continue;
                var line = new List<Vector3>();
                int n = Mathf.Max(2, Mathf.CeilToInt(curve.Length / drawStep));
                int total = n + 1;
                for (int i = 0; i < total; i++)
                {
                    float d = curve.Length * i / n;
                    Vec2 p = curve.PointAt(d);
                    float y = fallbackY;
                    if (PlacementValidator.TryGetGroundHeight(p.X, p.Z, out float h)) y = h;
                    line.Add(new Vector3(p.X, y + 0.15f, p.Z));
                }
                RowLines.Add(line);
            }
        }
    }
}
