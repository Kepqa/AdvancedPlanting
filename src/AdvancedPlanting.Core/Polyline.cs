using System;
using System.Collections.Generic;

namespace AdvancedPlanting.Core
{
    /// <summary>
    /// Dense polyline approximation of a curve with a cumulative arc-length table.
    /// Every path type (segment, arc, spline, contour) is reduced to this so the rest of the
    /// pipeline (resampling, offsets, filtering) has a single representation to work with.
    /// </summary>
    public sealed class Polyline
    {
        private readonly List<Vec2> _points;
        private readonly float[] _cumulative;

        public bool IsClosed { get; }
        public IReadOnlyList<Vec2> Points => _points;
        public int Count => _points.Count;

        /// <summary>Total arc length. For a closed polyline this includes the closing edge.</summary>
        public float Length { get; }

        public Polyline(IEnumerable<Vec2> points, bool closed)
        {
            _points = RemoveDuplicates(points);
            IsClosed = closed && _points.Count >= 3;

            int edgeCount = IsClosed ? _points.Count : Math.Max(0, _points.Count - 1);
            _cumulative = new float[edgeCount + 1];
            float acc = 0f;
            for (int i = 0; i < edgeCount; i++)
            {
                Vec2 a = _points[i];
                Vec2 b = _points[(i + 1) % _points.Count];
                acc += Vec2.Distance(a, b);
                _cumulative[i + 1] = acc;
            }
            Length = acc;
        }

        private static List<Vec2> RemoveDuplicates(IEnumerable<Vec2> points)
        {
            var result = new List<Vec2>();
            foreach (Vec2 p in points)
            {
                if (result.Count == 0 || Vec2.SqrDistance(result[result.Count - 1], p) > 1e-8f)
                    result.Add(p);
            }
            // A closed shape must not repeat its first point at the end.
            if (result.Count > 2 && Vec2.SqrDistance(result[0], result[result.Count - 1]) <= 1e-8f)
                result.RemoveAt(result.Count - 1);
            return result;
        }

        public bool IsDegenerate => _points.Count < 2 || Length < 1e-5f;

        /// <summary>Point at the given arc-length distance from the start. Clamped for open, wrapped for closed.</summary>
        public Vec2 PointAt(float distance)
        {
            if (_points.Count == 0) return Vec2.Zero;
            if (_points.Count == 1 || Length <= 0f) return _points[0];

            distance = NormalizeDistance(distance);
            int edge = FindEdge(distance);
            float segStart = _cumulative[edge];
            float segLen = _cumulative[edge + 1] - segStart;
            float t = segLen <= 0f ? 0f : (distance - segStart) / segLen;
            Vec2 a = _points[edge];
            Vec2 b = _points[(edge + 1) % _points.Count];
            return Vec2.Lerp(a, b, t);
        }

        /// <summary>
        /// Unit tangent at the given arc-length distance. On polyline vertices the tangent is
        /// averaged between the two adjacent edges so offset curves do not get kinks.
        /// </summary>
        public Vec2 TangentAt(float distance)
        {
            if (_points.Count < 2 || Length <= 0f) return new Vec2(1f, 0f);

            distance = NormalizeDistance(distance);
            int edge = FindEdge(distance);
            Vec2 tangent = EdgeDirection(edge);

            // Smooth across the vertex if we are very close to it.
            const float blendDistance = 1e-3f;
            float segStart = _cumulative[edge];
            float segEnd = _cumulative[edge + 1];
            if (distance - segStart < blendDistance && (IsClosed || edge > 0))
            {
                int prev = (edge - 1 + EdgeCount) % EdgeCount;
                tangent = (tangent + EdgeDirection(prev)).Normalized;
            }
            else if (segEnd - distance < blendDistance && (IsClosed || edge < EdgeCount - 1))
            {
                int next = (edge + 1) % EdgeCount;
                tangent = (tangent + EdgeDirection(next)).Normalized;
            }
            return tangent.SqrLength < 1e-8f ? EdgeDirection(edge) : tangent;
        }

        /// <summary>Left-hand unit normal at the given arc-length distance.</summary>
        public Vec2 NormalAt(float distance) => TangentAt(distance).PerpLeft;

        private int EdgeCount => _cumulative.Length - 1;

        private Vec2 EdgeDirection(int edge)
        {
            Vec2 a = _points[edge];
            Vec2 b = _points[(edge + 1) % _points.Count];
            return (b - a).Normalized;
        }

        private float NormalizeDistance(float distance)
        {
            if (IsClosed)
            {
                distance %= Length;
                if (distance < 0f) distance += Length;
                return distance;
            }
            if (distance < 0f) return 0f;
            if (distance > Length) return Length;
            return distance;
        }

        private int FindEdge(float distance)
        {
            // Binary search for the last cumulative entry <= distance.
            int lo = 0;
            int hi = _cumulative.Length - 2;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (_cumulative[mid] <= distance) lo = mid; else hi = mid - 1;
            }
            return lo;
        }

        /// <summary>
        /// Signed area of the closed polygon. Positive means counter-clockwise when looking down
        /// at the plane with X to the right and Z up, which in our convention means the left normal
        /// points inward.
        /// </summary>
        public float SignedArea()
        {
            if (_points.Count < 3) return 0f;
            float area = 0f;
            for (int i = 0; i < _points.Count; i++)
            {
                Vec2 a = _points[i];
                Vec2 b = _points[(i + 1) % _points.Count];
                area += Vec2.Cross(a, b);
            }
            return area * 0.5f;
        }

        /// <summary>Shortest distance from a point to the polyline.</summary>
        public float DistanceTo(Vec2 p)
        {
            if (_points.Count == 0) return float.PositiveInfinity;
            if (_points.Count == 1) return Vec2.Distance(p, _points[0]);

            float best = float.PositiveInfinity;
            for (int i = 0; i < EdgeCount; i++)
            {
                Vec2 a = _points[i];
                Vec2 b = _points[(i + 1) % _points.Count];
                float d = DistancePointSegment(p, a, b);
                if (d < best) best = d;
            }
            return best;
        }

        public static float DistancePointSegment(Vec2 p, Vec2 a, Vec2 b)
        {
            Vec2 ab = b - a;
            float len2 = ab.SqrLength;
            if (len2 <= 1e-12f) return Vec2.Distance(p, a);
            float t = Vec2.Dot(p - a, ab) / len2;
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            return Vec2.Distance(p, a + ab * t);
        }

        /// <summary>
        /// Points spaced evenly along the arc length. For an open polyline both endpoints are
        /// included and the spacing is stretched slightly so the last point lands exactly on the end.
        /// For a closed polyline the spacing is adjusted so the points divide the loop evenly.
        /// </summary>
        public List<float> EvenDistances(float spacing)
        {
            var result = new List<float>();
            if (IsDegenerate || spacing <= 0f)
            {
                if (_points.Count > 0) result.Add(0f);
                return result;
            }

            if (IsClosed)
            {
                int n = Math.Max(1, (int)Math.Floor(Length / spacing + 1e-4f));
                float step = Length / n;
                for (int i = 0; i < n; i++) result.Add(i * step);
            }
            else
            {
                int segments = Math.Max(1, (int)Math.Floor(Length / spacing + 1e-4f));
                float step = Length / segments;
                for (int i = 0; i <= segments; i++) result.Add(Math.Min(Length, i * step));
            }
            return result;
        }

        /// <summary>Densify so that no edge is longer than maxEdge; used before offsetting.</summary>
        public Polyline Densify(float maxEdge)
        {
            if (IsDegenerate || maxEdge <= 0f) return this;
            var pts = new List<Vec2>();
            for (int i = 0; i < EdgeCount; i++)
            {
                Vec2 a = _points[i];
                Vec2 b = _points[(i + 1) % _points.Count];
                float len = Vec2.Distance(a, b);
                int sub = Math.Max(1, (int)Math.Ceiling(len / maxEdge));
                for (int k = 0; k < sub; k++) pts.Add(Vec2.Lerp(a, b, (float)k / sub));
            }
            if (!IsClosed) pts.Add(_points[_points.Count - 1]);
            return new Polyline(pts, IsClosed);
        }
    }
}
