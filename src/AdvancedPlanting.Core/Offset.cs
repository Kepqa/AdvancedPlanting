using System;
using System.Collections.Generic;

namespace AdvancedPlanting.Core
{
    /// <summary>
    /// Parallel (offset) curves. Each row of the planting grid is the guide path shifted
    /// along its normal by a multiple of the row spacing.
    /// </summary>
    public static class Offset
    {
        /// <summary>
        /// Offsets the polyline by <paramref name="distance"/> along its left normal
        /// (negative values go right). Points of the raw offset that end up closer to the
        /// original curve than the requested distance lie on the concave side of a tight bend
        /// and would self-intersect; they are trimmed away. The result may therefore be split
        /// into several pieces, returned as separate polylines.
        /// </summary>
        public static List<Polyline> OffsetPolyline(Polyline source, float distance, float resolution = CurveBuilder.DefaultResolution)
        {
            var result = new List<Polyline>();
            if (source == null || source.IsDegenerate) return result;
            if (Math.Abs(distance) < 1e-5f)
            {
                result.Add(source);
                return result;
            }

            Polyline dense = source.Densify(resolution);
            float tolerance = Math.Abs(distance) * 0.98f - resolution;
            if (tolerance < 0f) tolerance = 0f;

            float length = dense.Length;
            int sampleCount = Math.Max(2, (int)Math.Ceiling(length / resolution));
            if (dense.IsClosed) sampleCount = Math.Max(3, sampleCount);

            // The trim test only needs the curve roughly: a copy with edges ~15% of the offset
            // changes distances by far less than the 2% tolerance margin, and is much cheaper
            // for wide offsets (inner rings of a fill). Cells about half the tolerance keep each
            // query to a few cells around the point.
            float coarseStep = Math.Min(2f, Math.Max(resolution, Math.Abs(distance) * 0.15f));
            Polyline coarse = coarseStep > resolution * 1.5f ? dense.Resample(coarseStep) : dense;
            var grid = new SegmentGrid(coarse, Math.Max(resolution * 2f, tolerance * 0.5f));

            var current = new List<Vec2>();
            var pieces = new List<List<Vec2>>();
            int total = dense.IsClosed ? sampleCount : sampleCount + 1;
            for (int i = 0; i < total; i++)
            {
                float d = dense.IsClosed ? length * i / sampleCount : Math.Min(length, length * i / sampleCount);
                Vec2 p = dense.PointAt(d) + dense.NormalAt(d) * distance;
                bool valid = !grid.AnyCloserThan(p, tolerance);
                if (valid)
                {
                    current.Add(p);
                }
                else if (current.Count > 0)
                {
                    pieces.Add(current);
                    current = new List<Vec2>();
                }
            }
            if (current.Count > 0) pieces.Add(current);

            if (pieces.Count == 0) return result;

            if (dense.IsClosed)
            {
                if (pieces.Count == 1 && pieces[0].Count == total)
                {
                    // Nothing was trimmed: the offset is still a closed loop.
                    result.Add(new Polyline(pieces[0], true));
                    return result;
                }
                // Merge the wrap-around pieces (last piece continues into the first).
                if (pieces.Count > 1)
                {
                    List<Vec2> last = pieces[pieces.Count - 1];
                    last.AddRange(pieces[0]);
                    pieces[0] = last;
                    pieces.RemoveAt(pieces.Count - 1);
                }
            }

            foreach (List<Vec2> piece in pieces)
            {
                var pl = new Polyline(piece, false);
                if (!pl.IsDegenerate) result.Add(pl);
            }
            return result;
        }

        /// <summary>
        /// Direction sign that points to the inside of a closed polyline, in terms of its left normal.
        /// Returns +1 when the left normal points inward, -1 when it points outward.
        /// </summary>
        public static float InwardSign(Polyline closed)
        {
            if (closed == null || !closed.IsClosed) return 1f;
            return closed.SignedArea() >= 0f ? 1f : -1f;
        }
    }
}
