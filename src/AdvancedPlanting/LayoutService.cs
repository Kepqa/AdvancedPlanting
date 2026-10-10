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
        public bool IsPending => Status == PointStatus.Pending;
        public bool IsBlocked => !IsValid && !IsPlanted && !IsPending;
    }

    /// <summary>
    /// Bridges the pure 2D layout from the core library and the 3D game world.
    ///
    /// Performance: <see cref="Rebuild"/> only does geometry and one ground ray per plant; the
    /// game-rule checks (biome, wards, cultivated ground, grow space) are spread over frames by
    /// <see cref="ValidateStep"/>. <see cref="Version"/> changes whenever anything visible
    /// changes, so the preview is only re-applied when needed.
    /// </summary>
    public sealed class LayoutService
    {
        private const float PlantedTolerance = 0.2f;
        private const float RowLineStep = 1f;

        private readonly ModConfig _config;
        private readonly int[] _statusCounts = new int[32];
        private int _validateCursor;
        private bool _passComplete = true;

        public readonly List<PlantSlot> Slots = new List<PlantSlot>();
        public readonly List<List<Vector3>> RowLines = new List<List<Vector3>>();

        /// <summary>Control points the current slots were built from, including the cursor while drawing.</summary>
        public readonly List<Vector3> PreviewPoints = new List<Vector3>();

        public bool Truncated { get; private set; }

        /// <summary>The figure exceeds the size limit; nothing is laid out, the guide is drawn as a warning.</summary>
        public bool TooLarge { get; private set; }
        public float Extent { get; private set; }
        public int ValidCount { get; private set; }
        public int PlantedCount { get; private set; }
        public int PendingCount { get; private set; }
        public int BlockedCount => Slots.Count - ValidCount - PlantedCount - PendingCount;
        public PointStatus TopBlockReason { get; private set; }

        /// <summary>Incremented whenever slots, statuses or lines change.</summary>
        public int Version { get; private set; }

        /// <summary>True when every slot has been checked at least once since the last rebuild.</summary>
        public bool FullyValidated => PendingCount == 0;

        /// <summary>True between the end of one validation pass and the start of the next.</summary>
        public bool PassComplete => _passComplete;

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
            Truncated = false;
            TooLarge = false;
            Extent = 0f;
            ValidCount = PlantedCount = PendingCount = 0;
            TopBlockReason = PointStatus.Valid;
            BuiltForPrefab = null;
            _validateCursor = 0;
            _passComplete = true;
            Version++;
        }

        /// <summary>
        /// Geometry rebuild. While the figure is still being drawn, <paramref name="cursor"/> is used
        /// as the next control point (rubber band). New slots start as Pending until validated.
        /// </summary>
        public void Rebuild(FigureState state, Vector3? cursor, Plant plant, string prefabName)
        {
            Clear();
            BuiltForPrefab = prefabName;
            PreviewPoints.AddRange(state.ControlPoints);
            int max = state.Mode.MaxControlPoints();
            if (!state.Finished && cursor.HasValue && (max == 0 || PreviewPoints.Count < max))
                PreviewPoints.Add(cursor.Value);

            if (PreviewPoints.Count < state.Mode.MinControlPoints())
            {
                // Not a figure yet (e.g. the first two points of an arc): a plain guide line
                // through the points and the cursor still shows where it goes.
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
                // A quarter of the spacing is plenty for placing plants and keeps wide figures cheap.
                Resolution = Mathf.Clamp(spacing / 4f, 0.1f, 0.5f),
                MaxExtent = _config.MaxFigureSize.Value,
            };
            foreach (Vector3 p in PreviewPoints) request.ControlPoints.Add(new Vec2(p.x, p.z));

            LayoutResult layout = PlantingLayout.Generate(request);
            Truncated = layout.Truncated;
            TooLarge = layout.TooLarge;
            Extent = layout.Extent;
            if (TooLarge)
            {
                var warning = new List<Vector3>();
                foreach (Vector3 p in PreviewPoints) warning.Add(p + Vector3.up * 0.15f);
                if (state.IsClosedPath && warning.Count > 2) warning.Add(warning[0]);
                RowLines.Add(warning);
                Version++;
                return;
            }

            float fallbackY = PreviewPoints[0].y;
            foreach (LayoutPoint lp in layout.Points)
            {
                var slot = new PlantSlot { Row = lp.Row, Normal = Vector3.up, Status = PointStatus.Pending };
                if (PlacementValidator.TryGetGround(lp.Position.X, lp.Position.Z, out float h, out Vector3 n))
                {
                    slot.Position = new Vector3(lp.Position.X, h, lp.Position.Z);
                    slot.Normal = n;
                }
                else
                {
                    slot.Position = new Vector3(lp.Position.X, fallbackY, lp.Position.Z);
                    slot.Status = PointStatus.NoGround;
                }
                if (state.IsPlanted(slot.Position, PlantedTolerance)) slot.Status = PointStatus.Planted;
                slot.Rotation = RotationFor(slot.Position, state.Seed);
                Slots.Add(slot);
            }

            BuildRowLines(layout, fallbackY);
            _validateCursor = 0;
            _passComplete = false;
            CountStatuses();
        }

        /// <summary>
        /// Validates up to <paramref name="budget"/> slots, continuing where the last call stopped.
        /// Pending slots are always processed first in order. Returns true when a pass finished.
        /// </summary>
        public bool ValidateStep(FigureState state, Piece piece, Plant plant, Player player, int budget)
        {
            if (_passComplete || Slots.Count == 0)
            {
                _passComplete = true;
                return false;
            }
            bool changed = false;
            int end = Mathf.Min(Slots.Count, _validateCursor + budget);
            for (; _validateCursor < end; _validateCursor++)
            {
                PlantSlot slot = Slots[_validateCursor];
                if (slot.Status == PointStatus.NoGround || slot.Status == PointStatus.Planted) continue;
                PointStatus status = state.IsPlanted(slot.Position, PlantedTolerance)
                    ? PointStatus.Planted
                    : PlacementValidator.Check(slot.Position, piece, plant, player);
                if (status == slot.Status) continue;
                slot.Status = status;
                Slots[_validateCursor] = slot;
                changed = true;
            }
            if (_validateCursor >= Slots.Count) _passComplete = true;
            if (changed || _passComplete) CountStatuses();
            return _passComplete;
        }

        /// <summary>Starts a new validation pass over all slots, keeping current statuses visible.</summary>
        public void BeginRevalidation()
        {
            if (Slots.Count == 0) return;
            _validateCursor = 0;
            _passComplete = false;
        }

        /// <summary>Finishes validation right now. Used before planting.</summary>
        public void ValidateAll(FigureState state, Piece piece, Plant plant, Player player)
        {
            if (_passComplete && PendingCount == 0) BeginRevalidation();
            ValidateStep(state, piece, plant, player, int.MaxValue);
        }

        /// <summary>Marks slots planted by this figure without a full validation pass.</summary>
        public void RefreshPlanted(FigureState state)
        {
            bool changed = false;
            for (int i = 0; i < Slots.Count; i++)
            {
                PlantSlot s = Slots[i];
                if (s.IsPlanted || !state.IsPlanted(s.Position, PlantedTolerance)) continue;
                s.Status = PointStatus.Planted;
                Slots[i] = s;
                changed = true;
            }
            if (changed) CountStatuses();
        }

        private void CountStatuses()
        {
            System.Array.Clear(_statusCounts, 0, _statusCounts.Length);
            ValidCount = PlantedCount = PendingCount = 0;
            foreach (PlantSlot s in Slots)
            {
                if (s.IsValid) ValidCount++;
                else if (s.IsPlanted) PlantedCount++;
                else if (s.IsPending) PendingCount++;
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
            Version++;
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
            foreach (Polyline curve in layout.RowCurves)
            {
                if (curve.IsDegenerate) continue;
                int n = Mathf.Max(2, Mathf.CeilToInt(curve.Length / RowLineStep));
                var line = new List<Vector3>(n + 1);
                for (int i = 0; i <= n; i++)
                {
                    Vec2 p = curve.PointAt(curve.Length * i / n);
                    float y = PlacementValidator.TryGetGroundHeight(p.X, p.Z, out float h) ? h : fallbackY;
                    line.Add(new Vector3(p.X, y + 0.15f, p.Z));
                }
                RowLines.Add(line);
            }
        }
    }
}
