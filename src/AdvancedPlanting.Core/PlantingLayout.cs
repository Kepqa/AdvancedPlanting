using System;
using System.Collections.Generic;

namespace AdvancedPlanting.Core
{
    /// <summary>Which side of the guide path the extra rows are placed on.</summary>
    public enum RowSide
    {
        Both = 0,
        Left = 1,
        Right = 2,
    }

    public sealed class LayoutRequest
    {
        public PathMode Mode = PathMode.Spline;
        public List<Vec2> ControlPoints = new List<Vec2>();

        /// <summary>Distance between plants along a row, world units.</summary>
        public float Spacing = 2f;

        /// <summary>Distance between rows. Zero or less means "same as Spacing".</summary>
        public float RowSpacing = 0f;

        /// <summary>Number of rows, including the guide path itself. Ignored when <see cref="Fill"/> is set.</summary>
        public int Rows = 1;

        /// <summary>
        /// For closed paths (circle, shape): fill the whole interior with concentric rows until
        /// it collapses. Ignored for open paths.
        /// </summary>
        public bool Fill = false;

        public RowSide Side = RowSide.Both;

        /// <summary>0 = perfect grid, 1 = maximum displacement that still keeps <see cref="MinDistance"/>.</summary>
        public float Jitter = 0f;

        /// <summary>Fraction of points dropped at random, 0..1.</summary>
        public float Thinning = 0f;

        public uint Seed = 1;

        /// <summary>Hard lower bound on the distance between any two plants (twice the grow radius).</summary>
        public float MinDistance = 1f;

        /// <summary>Period in world units of the low-frequency warp that bends whole rows.</summary>
        public float WarpScale = 6f;

        /// <summary>Safety cap on generated plants.</summary>
        public int MaxPoints = 400;

        public float Resolution = CurveBuilder.DefaultResolution;

        public float EffectiveRowSpacing => RowSpacing > 0f ? RowSpacing : Spacing;
    }

    public readonly struct LayoutPoint
    {
        public readonly Vec2 Position;
        public readonly int Row;
        public readonly int IndexInRow;

        public LayoutPoint(Vec2 position, int row, int indexInRow)
        {
            Position = position;
            Row = row;
            IndexInRow = indexInRow;
        }
    }

    public sealed class LayoutResult
    {
        public List<LayoutPoint> Points { get; } = new List<LayoutPoint>();

        /// <summary>Row polylines, for drawing the preview curve. Index 0 is always the guide path.</summary>
        public List<Polyline> RowCurves { get; } = new List<Polyline>();

        /// <summary>True when generation stopped at <see cref="LayoutRequest.MaxPoints"/>.</summary>
        public bool Truncated { get; set; }

        /// <summary>Number of points removed because they were too close to an accepted point.</summary>
        public int Rejected { get; set; }

        public bool IsEmpty => Points.Count == 0;
    }

    /// <summary>
    /// The whole pipeline: control points → guide path → rows → even samples → jitter → thinning
    /// → min-distance filter. Pure and deterministic.
    /// </summary>
    public static class PlantingLayout
    {
        private const int MaxContourRows = 200;
        private const int JitterRetries = 5;

        public static LayoutResult Generate(LayoutRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var result = new LayoutResult();

            if (request.ControlPoints == null || request.ControlPoints.Count < request.Mode.MinControlPoints())
                return result;
            if (request.Spacing <= 0.05f) return result;

            Polyline guide = CurveBuilder.Build(request.Mode, request.ControlPoints, request.Resolution);
            if (guide.IsDegenerate)
            {
                // A single valid control point still produces one plant, so a 2-point "segment"
                // of zero length behaves sensibly.
                if (guide.Count == 1)
                {
                    result.RowCurves.Add(guide);
                    result.Points.Add(new LayoutPoint(guide.Points[0], 0, 0));
                }
                return result;
            }

            List<RowCurve> rows = BuildRows(request, guide);
            foreach (RowCurve row in rows) result.RowCurves.Add(row.Curve);

            float maxJitter = MaxJitterOffset(request);
            var accepted = new SpatialHash(Math.Max(0.5f, request.MinDistance));

            foreach (RowCurve row in rows)
            {
                List<float> distances = row.Curve.EvenDistances(request.Spacing);
                for (int i = 0; i < distances.Count; i++)
                {
                    if (result.Points.Count >= request.MaxPoints)
                    {
                        result.Truncated = true;
                        return result;
                    }

                    Vec2 gridPoint = row.Curve.PointAt(distances[i]);
                    int cell = QuantizedId(gridPoint);

                    if (request.Thinning > 0f && Noise.Float01(cell, unchecked(request.Seed + 0x51A7u)) < request.Thinning)
                        continue;

                    Vec2 final;
                    if (!TryPlace(request, gridPoint, cell, maxJitter, accepted, out final))
                    {
                        result.Rejected++;
                        continue;
                    }

                    accepted.Add(final);
                    result.Points.Add(new LayoutPoint(final, row.Index, i));
                }
            }

            return result;
        }

        /// <summary>Largest displacement that cannot bring two neighbouring grid points below MinDistance.</summary>
        public static float MaxJitterOffset(LayoutRequest request)
        {
            float smallestSpacing = Math.Min(request.Spacing, request.EffectiveRowSpacing);
            float room = (smallestSpacing - request.MinDistance) * 0.5f;
            return room > 0f ? room : 0f;
        }

        private static bool TryPlace(LayoutRequest request, Vec2 gridPoint, int cell, float maxJitter, SpatialHash accepted, out Vec2 final)
        {
            if (request.Jitter <= 0f || maxJitter <= 0f)
            {
                final = gridPoint;
                return accepted.IsFree(final, request.MinDistance);
            }

            float amplitude = request.Jitter * maxJitter;
            float warpScale = request.WarpScale > 0.1f ? request.WarpScale : 6f;

            // Low-frequency component: bends whole rows in the same direction (60 percent of the budget).
            Vec2 warp = Noise.SmoothVector2D(gridPoint.X / warpScale, gridPoint.Z / warpScale, request.Seed) * 0.6f;

            for (int attempt = 0; attempt < JitterRetries; attempt++)
            {
                uint salt = unchecked(request.Seed + (uint)attempt * 0x9E3779B9u);
                // High-frequency component: independent per-plant scatter (40 percent of the budget).
                Vec2 scatter = Noise.InUnitDisk(cell, salt) * 0.4f;
                Vec2 candidate = gridPoint + (warp + scatter) * amplitude;
                if (accepted.IsFree(candidate, request.MinDistance))
                {
                    final = candidate;
                    return true;
                }
            }

            final = gridPoint;
            return accepted.IsFree(final, request.MinDistance);
        }

        /// <summary>
        /// Stable integer id of a grid point. Hashing the quantized position (instead of a running
        /// index) means adding a row or changing thinning does not reshuffle every other plant.
        /// </summary>
        private static int QuantizedId(Vec2 p)
        {
            int qx = (int)Math.Round(p.X * 20f);
            int qz = (int)Math.Round(p.Z * 20f);
            return unchecked(qx * 73856093 ^ qz * 19349663);
        }

        // ------------------------------------------------------------------ rows

        private struct RowCurve
        {
            public Polyline Curve;
            public int Index;
        }

        private static List<RowCurve> BuildRows(LayoutRequest request, Polyline guide)
        {
            var rows = new List<RowCurve>();
            float rowSpacing = request.EffectiveRowSpacing;

            if (guide.IsClosed && request.Fill)
            {
                float inward = Offset.InwardSign(guide);
                rows.Add(new RowCurve { Curve = guide, Index = 0 });
                for (int k = 1; k < MaxContourRows; k++)
                {
                    List<Polyline> pieces = Offset.OffsetPolyline(guide, inward * rowSpacing * k, request.Resolution);
                    bool any = false;
                    foreach (Polyline piece in pieces)
                    {
                        if (piece.IsDegenerate) continue;
                        rows.Add(new RowCurve { Curve = piece, Index = k });
                        any = true;
                    }
                    if (!any) break;
                }
                return rows;
            }

            // For closed paths "left" means inside and "right" outside, whatever the winding.
            float sideSign = guide.IsClosed ? Offset.InwardSign(guide) : 1f;
            int count = Math.Max(1, request.Rows);
            for (int i = 0; i < count; i++)
            {
                int k;
                switch (request.Side)
                {
                    case RowSide.Left: k = i; break;
                    case RowSide.Right: k = -i; break;
                    default: k = i - (count - 1) / 2; break;
                }

                if (k == 0)
                {
                    rows.Add(new RowCurve { Curve = guide, Index = 0 });
                    continue;
                }

                foreach (Polyline piece in Offset.OffsetPolyline(guide, sideSign * rowSpacing * k, request.Resolution))
                {
                    if (!piece.IsDegenerate) rows.Add(new RowCurve { Curve = piece, Index = k });
                }
            }
            return rows;
        }

        // ------------------------------------------------------------------ spatial hash

        private sealed class SpatialHash
        {
            private readonly float _cell;
            private readonly Dictionary<long, List<Vec2>> _buckets = new Dictionary<long, List<Vec2>>();

            public SpatialHash(float cellSize)
            {
                _cell = cellSize;
            }

            private long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

            public void Add(Vec2 p)
            {
                long key = Key(Cell(p.X), Cell(p.Z));
                if (!_buckets.TryGetValue(key, out List<Vec2> list))
                {
                    list = new List<Vec2>();
                    _buckets[key] = list;
                }
                list.Add(p);
            }

            public bool IsFree(Vec2 p, float minDistance)
            {
                if (minDistance <= 0f) return true;
                float min2 = minDistance * minDistance;
                int reach = (int)Math.Ceiling(minDistance / _cell);
                int cx = Cell(p.X);
                int cz = Cell(p.Z);
                for (int dx = -reach; dx <= reach; dx++)
                {
                    for (int dz = -reach; dz <= reach; dz++)
                    {
                        if (!_buckets.TryGetValue(Key(cx + dx, cz + dz), out List<Vec2> list)) continue;
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (Vec2.SqrDistance(list[i], p) < min2) return false;
                        }
                    }
                }
                return true;
            }

            private int Cell(float v)
            {
                int i = (int)(v / _cell);
                return v < 0f && i * _cell != v ? i - 1 : i;
            }
        }
    }
}
