using System;
using System.Collections.Generic;

namespace AdvancedPlanting.Core
{
    /// <summary>
    /// Turns a list of control points into a dense polyline for the chosen path mode.
    /// </summary>
    public static class CurveBuilder
    {
        /// <summary>Target edge length of the generated polyline, in world units.</summary>
        public const float DefaultResolution = 0.1f;

        public static Polyline Build(PathMode mode, IReadOnlyList<Vec2> controlPoints, float resolution = DefaultResolution)
        {
            if (controlPoints == null) throw new ArgumentNullException(nameof(controlPoints));
            if (resolution <= 0f) resolution = DefaultResolution;

            var pts = TrimToMode(mode, controlPoints);
            if (pts.Count < mode.MinControlPoints())
                return new Polyline(new Vec2[0], false);

            switch (mode)
            {
                case PathMode.Segment:
                    return new Polyline(new[] { pts[0], pts[pts.Count - 1] }, false).Densify(resolution);
                case PathMode.Arc:
                    return BuildArc(pts[0], pts[1], pts[2], false, resolution);
                case PathMode.Circle:
                    return BuildCircle(pts[0], pts[1], resolution);
                case PathMode.Spline:
                    return BuildCatmullRom(pts, false, resolution);
                case PathMode.Contour:
                    return BuildCatmullRom(pts, true, resolution);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private static List<Vec2> TrimToMode(PathMode mode, IReadOnlyList<Vec2> controlPoints)
        {
            int max = mode.MaxControlPoints();
            var list = new List<Vec2>(controlPoints.Count);
            for (int i = 0; i < controlPoints.Count; i++)
            {
                if (max > 0 && list.Count >= max) break;
                list.Add(controlPoints[i]);
            }
            return list;
        }

        // ---------------------------------------------------------------- arcs

        /// <summary>
        /// Circle through three points. Returns false when the points are (nearly) collinear.
        /// </summary>
        public static bool TryCircumcircle(Vec2 a, Vec2 b, Vec2 c, out Vec2 center, out float radius)
        {
            float d = 2f * (a.X * (b.Z - c.Z) + b.X * (c.Z - a.Z) + c.X * (a.Z - b.Z));
            if (Math.Abs(d) < 1e-6f)
            {
                center = Vec2.Zero;
                radius = 0f;
                return false;
            }
            float a2 = a.SqrLength, b2 = b.SqrLength, c2 = c.SqrLength;
            float ux = (a2 * (b.Z - c.Z) + b2 * (c.Z - a.Z) + c2 * (a.Z - b.Z)) / d;
            float uz = (a2 * (c.X - b.X) + b2 * (a.X - c.X) + c2 * (b.X - a.X)) / d;
            center = new Vec2(ux, uz);
            radius = Vec2.Distance(center, a);
            return radius > 1e-4f && radius < 1e5f;
        }

        private static Polyline BuildArc(Vec2 start, Vec2 mid, Vec2 end, bool fullCircle, float resolution)
        {
            if (!TryCircumcircle(start, mid, end, out Vec2 center, out float radius))
            {
                // Collinear: fall back to a straight segment through the three points.
                return new Polyline(new[] { start, mid, end }, false).Densify(resolution);
            }

            float a0 = (float)Math.Atan2(start.Z - center.Z, start.X - center.X);
            float a1 = (float)Math.Atan2(mid.Z - center.Z, mid.X - center.X);
            float a2 = (float)Math.Atan2(end.Z - center.Z, end.X - center.X);

            // Choose the sweep direction so the arc passes through the mid point.
            float sweepCcw = Wrap(a2 - a0);           // in [0, 2pi)
            float midCcw = Wrap(a1 - a0);
            bool ccw = midCcw <= sweepCcw;
            float sweep = ccw ? sweepCcw : -(2f * (float)Math.PI - sweepCcw);
            if (fullCircle) sweep = ccw ? 2f * (float)Math.PI : -2f * (float)Math.PI;

            float arcLength = Math.Abs(sweep) * radius;
            int segments = Math.Max(8, (int)Math.Ceiling(arcLength / resolution));
            var pts = new List<Vec2>(segments + 1);
            int count = fullCircle ? segments : segments + 1;
            for (int i = 0; i < count; i++)
            {
                float ang = a0 + sweep * i / segments;
                pts.Add(new Vec2(center.X + radius * (float)Math.Cos(ang), center.Z + radius * (float)Math.Sin(ang)));
            }
            return new Polyline(pts, fullCircle);
        }

        /// <summary>Circle around <paramref name="center"/> through <paramref name="edge"/>, starting at the edge point.</summary>
        public static Polyline BuildCircle(Vec2 center, Vec2 edge, float resolution)
        {
            float radius = Vec2.Distance(center, edge);
            if (radius < 1e-3f) return new Polyline(new[] { center }, false);
            float a0 = (float)Math.Atan2(edge.Z - center.Z, edge.X - center.X);
            int segments = Math.Max(12, (int)Math.Ceiling(2f * (float)Math.PI * radius / resolution));
            var pts = new List<Vec2>(segments);
            for (int i = 0; i < segments; i++)
            {
                float ang = a0 + 2f * (float)Math.PI * i / segments;
                pts.Add(new Vec2(center.X + radius * (float)Math.Cos(ang), center.Z + radius * (float)Math.Sin(ang)));
            }
            return new Polyline(pts, true);
        }

        private static float Wrap(float angle)
        {
            const float twoPi = 2f * (float)Math.PI;
            angle %= twoPi;
            if (angle < 0f) angle += twoPi;
            return angle;
        }

        // ---------------------------------------------------------------- splines

        /// <summary>
        /// Centripetal Catmull-Rom spline (alpha = 0.5). Passes through every control point,
        /// never forms cusps or self-loops within a segment, which is what we want for planting rows.
        /// </summary>
        public static Polyline BuildCatmullRom(IReadOnlyList<Vec2> control, bool closed, float resolution)
        {
            var pts = new List<Vec2>(control.Count);
            foreach (Vec2 p in control)
            {
                if (pts.Count == 0 || Vec2.SqrDistance(pts[pts.Count - 1], p) > 1e-8f) pts.Add(p);
            }
            if (closed && pts.Count > 2 && Vec2.SqrDistance(pts[0], pts[pts.Count - 1]) <= 1e-8f)
                pts.RemoveAt(pts.Count - 1);

            if (pts.Count < 2) return new Polyline(pts, false);
            if (pts.Count == 2 && !closed) return new Polyline(pts, false).Densify(resolution);
            if (closed && pts.Count < 3) return new Polyline(pts, false).Densify(resolution);

            var output = new List<Vec2>();
            int segCount = closed ? pts.Count : pts.Count - 1;
            for (int i = 0; i < segCount; i++)
            {
                Vec2 p0 = GetControl(pts, i - 1, closed);
                Vec2 p1 = GetControl(pts, i, closed);
                Vec2 p2 = GetControl(pts, i + 1, closed);
                Vec2 p3 = GetControl(pts, i + 2, closed);

                float chord = Vec2.Distance(p1, p2);
                int steps = Math.Max(4, (int)Math.Ceiling(chord / resolution));
                for (int s = 0; s < steps; s++)
                {
                    float t = (float)s / steps;
                    output.Add(EvaluateCentripetal(p0, p1, p2, p3, t));
                }
            }
            if (!closed) output.Add(pts[pts.Count - 1]);
            return new Polyline(output, closed);
        }

        private static Vec2 GetControl(List<Vec2> pts, int index, bool closed)
        {
            int n = pts.Count;
            if (closed)
            {
                index %= n;
                if (index < 0) index += n;
                return pts[index];
            }
            if (index < 0)
            {
                // Phantom point mirrored over the first point so the curve starts straight.
                return pts[0] + (pts[0] - pts[1]);
            }
            if (index >= n)
            {
                return pts[n - 1] + (pts[n - 1] - pts[n - 2]);
            }
            return pts[index];
        }

        private static Vec2 EvaluateCentripetal(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, float t)
        {
            const float alpha = 0.5f;
            float t0 = 0f;
            float t1 = t0 + Knot(p0, p1, alpha);
            float t2 = t1 + Knot(p1, p2, alpha);
            float t3 = t2 + Knot(p2, p3, alpha);

            // Degenerate knots (duplicate points) collapse to linear interpolation.
            if (t2 - t1 < 1e-6f) return Vec2.Lerp(p1, p2, t);

            float tt = t1 + (t2 - t1) * t;
            Vec2 a1 = Blend(p0, p1, t0, t1, tt);
            Vec2 a2 = Blend(p1, p2, t1, t2, tt);
            Vec2 a3 = Blend(p2, p3, t2, t3, tt);
            Vec2 b1 = Blend(a1, a2, t0, t2, tt);
            Vec2 b2 = Blend(a2, a3, t1, t3, tt);
            return Blend(b1, b2, t1, t2, tt);
        }

        private static float Knot(Vec2 a, Vec2 b, float alpha)
        {
            float d = Vec2.Distance(a, b);
            float k = (float)Math.Pow(Math.Max(d, 1e-5f), alpha);
            return k;
        }

        private static Vec2 Blend(Vec2 a, Vec2 b, float ta, float tb, float t)
        {
            float span = tb - ta;
            if (span < 1e-6f) return a;
            return a * ((tb - t) / span) + b * ((t - ta) / span);
        }
    }
}
