using System;
using System.Collections.Generic;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Wheel icon artwork as signed-distance shapes, rasterised at 256 px with analytic
    /// anti-aliasing: white glyphs with a soft dark outline, like the vanilla radial icons.
    /// Pure C# with no Unity types, so the same code renders previews outside the game.
    /// </summary>
    public static class IconArt
    {
        public const int Size = 256;

        public static readonly string[] Names =
        {
            "single", "line", "arc", "circle", "curve", "shape", "fill", "side",
            "undo", "clear", "reshuffle", "settings",
        };

        /// <summary>Renders an icon to RGBA bytes, rows top to bottom.</summary>
        public static byte[] Render(string name)
        {
            var c = new Canvas();
            switch (name)
            {
                case "single": DrawSingle(c); break;
                case "line": DrawLine(c); break;
                case "arc": DrawArc(c); break;
                case "circle": DrawCircle(c); break;
                case "curve": DrawCurve(c); break;
                case "shape": DrawShape(c, false); break;
                case "fill": DrawShape(c, true); break;
                case "side": DrawSide(c); break;
                case "undo": DrawUndo(c); break;
                case "clear": DrawClear(c); break;
                case "reshuffle": DrawReshuffle(c); break;
                case "settings": DrawSettings(c); break;
                default: throw new ArgumentException("Unknown icon " + name);
            }
            return c.ToRgba();
        }

        // ------------------------------------------------------------------ artwork
        // Style: thick solid path, plants as large round markers. Few elements, so the icons
        // stay readable at the 60-90 px the wheel shows them at.

        private const float PathWidth = 14f;
        private const float PlantRadius = 19f;

        private static void DrawSingle(Canvas c)
        {
            // Seedling: slightly curved stem, two leaves opening upwards, soil mound.
            var stem = new List<P>();
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                stem.Add(new P(128 + 10 * (float)Math.Sin(t * Math.PI * 0.9), 206 - 104 * t));
            }
            c.Polyline(stem, 16f);
            P top = stem[stem.Count - 1];
            c.Leaf(new P(top.X - 40, top.Y - 22), 46, 20, -2.6f);
            c.Leaf(new P(top.X + 38, top.Y - 34), 48, 21, -0.5f);
            c.Capsule(new P(70, 214), new P(186, 214), 9f);
        }

        private static void DrawLine(Canvas c)
        {
            P a = new P(40, 196), b = new P(216, 60);
            c.Capsule(a, b, PathWidth * 0.5f);
            for (int i = 0; i < 4; i++) c.Disk(Lerp(a, b, i / 3f), PlantRadius);
        }

        private static void DrawArc(Canvas c)
        {
            List<P> arc = ArcPts(128, 200, 100, Math.PI * 0.05, Math.PI * 0.95, 64);
            c.Polyline(arc, PathWidth);
            foreach (float t in new[] { 0f, 0.333f, 0.667f, 1f }) c.Disk(At(arc, t), PlantRadius);
        }

        private static void DrawCircle(Canvas c)
        {
            List<P> ring = ArcPts(128, 128, 88, 0, Math.PI * 2, 96);
            c.Polyline(ring, PathWidth);
            for (int i = 0; i < 6; i++)
            {
                double a = Math.PI / 2 + i * Math.PI / 3;
                c.Disk(new P((float)(128 + 88 * Math.Cos(a)), (float)(128 - 88 * Math.Sin(a))), PlantRadius);
            }
        }

        private static void DrawCurve(Canvas c)
        {
            var pts = new List<P>();
            for (int i = 0; i <= 80; i++)
            {
                float x = 30 + 196f * i / 80;
                float y = 128 - 56f * (float)Math.Sin((x - 30) / 196f * 2 * Math.PI);
                pts.Add(new P(x, y));
            }
            c.Polyline(pts, PathWidth);
            foreach (float t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }) c.Disk(At(pts, t), PlantRadius * 0.9f);
        }

        private static void DrawShape(Canvas c, bool filled)
        {
            List<P> blob = BlobPts(128, 128, 98);
            c.Polyline(blob, PathWidth);
            if (!filled)
            {
                foreach (float t in new[] { 0.04f, 0.24f, 0.44f, 0.64f, 0.84f }) c.Disk(At(blob, t), PlantRadius * 0.9f);
                return;
            }
            // Filled: the inside of the outline is solid.
            c.Polygon(BlobPts(128, 128, 62));
        }

        private static void DrawSide(Canvas c)
        {
            c.Capsule(new P(128, 34), new P(128, 222), PathWidth * 0.5f);
            Arrow(c, new P(106, 128), new P(26, 128));
            Arrow(c, new P(150, 128), new P(230, 128));
        }

        private static void DrawUndo(Canvas c)
        {
            // Counter-clockwise arc ending top-left, with a filled arrow head.
            List<P> arc = ArcPts(132, 140, 76, -Math.PI * 0.75, Math.PI * 0.8, 64);
            c.Polyline(arc, 18f);
            P end = arc[arc.Count - 1];
            P prev = arc[arc.Count - 4];
            float dx = end.X - prev.X, dy = end.Y - prev.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            dx /= len; dy /= len;
            var tip = new P(end.X + dx * 34, end.Y + dy * 34);
            var l = new P(end.X - dy * 30, end.Y + dx * 30);
            var r = new P(end.X + dy * 30, end.Y - dx * 30);
            c.Triangle(tip, l, r);
        }

        private static void DrawClear(Canvas c)
        {
            c.Capsule(new P(62, 62), new P(194, 194), 16f);
            c.Capsule(new P(194, 62), new P(62, 194), 16f);
        }

        private static void DrawReshuffle(Canvas c)
        {
            c.RoundRectOutline(128, 128, 90, 90, 28, 14f);
            foreach (P p in new[] { new P(86, 86), new P(128, 128), new P(170, 170), new P(170, 86), new P(86, 170) })
                c.Disk(p, 16f);
        }

        private static void DrawSettings(Canvas c)
        {
            const int teeth = 8;
            for (int i = 0; i < teeth; i++)
            {
                double a = i * 2 * Math.PI / teeth;
                var inner = new P((float)(128 + 60 * Math.Cos(a)), (float)(128 + 60 * Math.Sin(a)));
                var outer = new P((float)(128 + 92 * Math.Cos(a)), (float)(128 + 92 * Math.Sin(a)));
                c.Capsule(inner, outer, 19f);
            }
            c.Disk(new P(128, 128), 72f);
            c.CutDisk(new P(128, 128), 30f);
        }

        private static void Arrow(Canvas c, P from, P to)
        {
            float dx = to.X - from.X, dy = to.Y - from.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            dx /= len; dy /= len;
            const float head = 40f;
            var baseC = new P(to.X - dx * head, to.Y - dy * head);
            c.Capsule(from, baseC, 8f);
            c.Triangle(to, new P(baseC.X - dy * 30, baseC.Y + dx * 30), new P(baseC.X + dy * 30, baseC.Y - dx * 30));
        }

        // ------------------------------------------------------------------ geometry

        private readonly struct P
        {
            public readonly float X, Y;
            public P(float x, float y) { X = x; Y = y; }
        }

        private static P Lerp(P a, P b, float t) => new P(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        private static List<P> ArcPts(float cx, float cy, float r, double from, double to, int steps)
        {
            var list = new List<P>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                double a = from + (to - from) * i / steps;
                list.Add(new P((float)(cx + r * Math.Cos(a)), (float)(cy - r * Math.Sin(a))));
            }
            return list;
        }

        private static List<P> BlobPts(float cx, float cy, float r)
        {
            var list = new List<P>();
            for (int i = 0; i <= 96; i++)
            {
                double a = 2 * Math.PI * i / 96;
                double rr = r * (0.86 + 0.1 * Math.Sin(3 * a + 0.4) + 0.05 * Math.Cos(2 * a));
                list.Add(new P((float)(cx + rr * Math.Cos(a)), (float)(cy - 0.85 * rr * Math.Sin(a))));
            }
            return list;
        }

        /// <summary>Point at fraction t of the polyline's length.</summary>
        private static P At(List<P> pts, float t)
        {
            float total = 0f;
            for (int i = 1; i < pts.Count; i++) total += Dist(pts[i - 1], pts[i]);
            float target = total * t, acc = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                float seg = Dist(pts[i - 1], pts[i]);
                if (acc + seg >= target) return Lerp(pts[i - 1], pts[i], seg > 0 ? (target - acc) / seg : 0f);
                acc += seg;
            }
            return pts[pts.Count - 1];
        }

        private static float Dist(P a, P b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // ------------------------------------------------------------------ rasteriser

        /// <summary>
        /// Two distance fields: shapes (union) and cut-outs. Final distance is
        /// max(shape, -cut). Coverage = clamp(0.5 - d), outline drawn from the same field.
        /// </summary>
        private sealed class Canvas
        {
            private const float Outline = 7f;
            private readonly float[] _shape = Fill(float.MaxValue);
            private readonly float[] _cut = Fill(float.MaxValue);

            private static float[] Fill(float v)
            {
                var a = new float[Size * Size];
                for (int i = 0; i < a.Length; i++) a[i] = v;
                return a;
            }

            public void Capsule(P a, P b, float r) => Prim(_shape, Bounds(a, b, r), (x, y) => SegDist(x, y, a, b) - r);

            public void Disk(P c, float r) => Prim(_shape, Bounds(c, c, r), (x, y) => Hyp(x - c.X, y - c.Y) - r);

            public void CutDisk(P c, float r) => Prim(_cut, Bounds(c, c, r), (x, y) => Hyp(x - c.X, y - c.Y) - r);

            public void Polyline(List<P> pts, float width)
            {
                for (int i = 1; i < pts.Count; i++) Capsule(pts[i - 1], pts[i], width * 0.5f);
            }

            /// <summary>Dashed polyline: dash length, gap length, stroke width.</summary>
            public void Dashed(List<P> pts, float width, float dash, float gap)
            {
                float period = dash + gap, pos = 0f;
                for (int i = 1; i < pts.Count; i++)
                {
                    P a = pts[i - 1], b = pts[i];
                    float seg = Dist(a, b);
                    float s = 0f;
                    while (s < seg)
                    {
                        float phase = (pos + s) % period;
                        if (phase < dash)
                        {
                            float end = Math.Min(seg, s + (dash - phase));
                            Capsule(Lerp(a, b, s / seg), Lerp(a, b, end / seg), width * 0.5f);
                            s = end;
                        }
                        else
                        {
                            s += period - phase;
                        }
                    }
                    pos += seg;
                }
            }

            /// <summary>Leaf: ellipse with semi-axes (len, wid) rotated by angle (radians).</summary>
            public void Leaf(P c, float len, float wid, float angle)
            {
                float cos = (float)Math.Cos(angle), sin = (float)Math.Sin(angle);
                Prim(_shape, Bounds(c, c, len), (x, y) =>
                {
                    float dx = x - c.X, dy = y - c.Y;
                    float u = dx * cos + dy * sin;
                    float v = -dx * sin + dy * cos;
                    float k = Hyp(u / len, v / wid);
                    return (k - 1f) * Math.Min(len, wid);
                });
            }

            /// <summary>Filled simple polygon (even-odd rule).</summary>
            public void Polygon(List<P> pts)
            {
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (P p in pts)
                {
                    x0 = Math.Min(x0, p.X); y0 = Math.Min(y0, p.Y);
                    x1 = Math.Max(x1, p.X); y1 = Math.Max(y1, p.Y);
                }
                Prim(_shape, (x0, y0, x1, y1), (x, y) =>
                {
                    float d = float.MaxValue;
                    bool inside = false;
                    for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                    {
                        P a = pts[j], b = pts[i];
                        d = Math.Min(d, SegDist(x, y, a, b));
                        if ((b.Y > y) != (a.Y > y) && x < (a.X - b.X) * (y - b.Y) / (a.Y - b.Y) + b.X) inside = !inside;
                    }
                    return inside ? -d : d;
                });
            }

            /// <summary>Filled triangle with slightly rounded corners.</summary>
            public void Triangle(P a, P b, P c)
            {
                var box = (Math.Min(a.X, Math.Min(b.X, c.X)), Math.Min(a.Y, Math.Min(b.Y, c.Y)),
                           Math.Max(a.X, Math.Max(b.X, c.X)), Math.Max(a.Y, Math.Max(b.Y, c.Y)));
                float sign = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X) > 0 ? 1f : -1f;
                Prim(_shape, box, (x, y) =>
                {
                    float d = Math.Min(SegDist(x, y, a, b), Math.Min(SegDist(x, y, b, c), SegDist(x, y, c, a)));
                    bool inside = Side(x, y, a, b) * sign >= 0 && Side(x, y, b, c) * sign >= 0 && Side(x, y, c, a) * sign >= 0;
                    return (inside ? -d : d) - 2f;
                });
            }

            private static float Side(float x, float y, P a, P b) => (b.X - a.X) * (y - a.Y) - (b.Y - a.Y) * (x - a.X);

            public void RoundRectOutline(float cx, float cy, float hw, float hh, float r, float width)
            {
                Prim(_shape, (cx - hw - width, cy - hh - width, cx + hw + width, cy + hh + width), (x, y) =>
                {
                    float qx = Math.Abs(x - cx) - (hw - r), qy = Math.Abs(y - cy) - (hh - r);
                    float outside = Hyp(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f) - r;
                    return Math.Abs(outside) - width * 0.5f;
                });
            }

            private static (float, float, float, float) Bounds(P a, P b, float r) =>
                (Math.Min(a.X, b.X) - r, Math.Min(a.Y, b.Y) - r, Math.Max(a.X, b.X) + r, Math.Max(a.Y, b.Y) + r);

            /// <summary>
            /// Artwork is drawn on a 256 grid and shrunk around the centre by this factor, leaving
            /// a margin so icons sit comfortably inside the wheel slots. Outline width is not scaled.
            /// </summary>
            private const float ArtScale = 0.8f;
            private const float Centre = Size * 0.5f;

            private static void Prim(float[] field, (float x0, float y0, float x1, float y1) artBox, Func<float, float, float> artSdf)
            {
                (float x0, float y0, float x1, float y1) box = (Centre + (artBox.x0 - Centre) * ArtScale, Centre + (artBox.y0 - Centre) * ArtScale,
                           Centre + (artBox.x1 - Centre) * ArtScale, Centre + (artBox.y1 - Centre) * ArtScale);
                Func<float, float, float> sdf = (x, y) =>
                    artSdf(Centre + (x - Centre) / ArtScale, Centre + (y - Centre) / ArtScale) * ArtScale;
                int margin = (int)Outline + 3;
                int x0 = Math.Max(0, (int)box.x0 - margin), y0 = Math.Max(0, (int)box.y0 - margin);
                int x1 = Math.Min(Size - 1, (int)box.x1 + margin), y1 = Math.Min(Size - 1, (int)box.y1 + margin);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float d = sdf(x + 0.5f, y + 0.5f);
                        int i = y * Size + x;
                        if (d < field[i]) field[i] = d;
                    }
                }
            }

            private static float SegDist(float px, float py, P a, P b)
            {
                float abx = b.X - a.X, aby = b.Y - a.Y;
                float len2 = abx * abx + aby * aby;
                float t = len2 > 1e-6f ? ((px - a.X) * abx + (py - a.Y) * aby) / len2 : 0f;
                if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
                return Hyp(px - a.X - abx * t, py - a.Y - aby * t);
            }

            private static float Hyp(float x, float y) => (float)Math.Sqrt(x * x + y * y);

            private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

            public byte[] ToRgba()
            {
                var rgba = new byte[Size * Size * 4];
                for (int i = 0; i < Size * Size; i++)
                {
                    float d = Math.Max(_shape[i], -_cut[i]);
                    float fill = Clamp01(0.5f - d);
                    float edge = Clamp01(0.5f - (d - Outline)) * 0.55f;
                    // Composite white fill over a dark outline.
                    float a = fill + edge * (1f - fill);
                    float lum = a > 0f ? fill / a : 0f;
                    byte v = (byte)(lum * 255f);
                    rgba[i * 4 + 0] = v;
                    rgba[i * 4 + 1] = v;
                    rgba[i * 4 + 2] = v;
                    rgba[i * 4 + 3] = (byte)(a * 255f);
                }
                return rgba;
            }
        }
    }
}
