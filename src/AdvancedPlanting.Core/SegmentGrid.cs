using System;
using System.Collections.Generic;

namespace AdvancedPlanting.Core
{
    /// <summary>
    /// Uniform grid over the edges of a polyline for fast "is any edge closer than r" queries.
    /// Replaces a scan over every edge, which made offsetting quadratic in curve length.
    /// </summary>
    public sealed class SegmentGrid
    {
        private readonly IReadOnlyList<Vec2> _pts;
        private readonly int _edgeCount;
        private readonly float _cell;
        private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();

        public SegmentGrid(Polyline polyline, float cellSize)
        {
            _pts = polyline.Points;
            _cell = Math.Max(0.05f, cellSize);
            int n = _pts.Count;
            _edgeCount = polyline.IsClosed ? n : Math.Max(0, n - 1);
            for (int e = 0; e < _edgeCount; e++)
            {
                Vec2 a = _pts[e], b = _pts[(e + 1) % n];
                int x0 = Cell(Math.Min(a.X, b.X)), x1 = Cell(Math.Max(a.X, b.X));
                int z0 = Cell(Math.Min(a.Z, b.Z)), z1 = Cell(Math.Max(a.Z, b.Z));
                for (int x = x0; x <= x1; x++)
                    for (int z = z0; z <= z1; z++)
                        Bucket(x, z).Add(e);
            }
        }

        /// <summary>True when some edge lies strictly closer than <paramref name="radius"/> to p.</summary>
        public bool AnyCloserThan(Vec2 p, float radius)
        {
            if (radius <= 0f) return false;
            int reach = (int)Math.Ceiling(radius / _cell);
            int cx = Cell(p.X), cz = Cell(p.Z);
            int n = _pts.Count;
            for (int x = cx - reach; x <= cx + reach; x++)
            {
                for (int z = cz - reach; z <= cz + reach; z++)
                {
                    if (!_cells.TryGetValue(Key(x, z), out List<int> edges)) continue;
                    foreach (int e in edges)
                    {
                        if (Polyline.DistancePointSegment(p, _pts[e], _pts[(e + 1) % n]) < radius) return true;
                    }
                }
            }
            return false;
        }

        private List<int> Bucket(int x, int z)
        {
            long key = Key(x, z);
            if (!_cells.TryGetValue(key, out List<int> list))
            {
                list = new List<int>(4);
                _cells[key] = list;
            }
            return list;
        }

        private int Cell(float v) => (int)Math.Floor(v / _cell);

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
    }
}
