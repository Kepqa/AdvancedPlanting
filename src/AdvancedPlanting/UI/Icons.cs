using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Wheel icons drawn in code (white anti-aliased strokes on transparent background, like the
    /// vanilla radial icons), so the mod ships as plain DLLs without image assets.
    /// </summary>
    internal static class Icons
    {
        private const int Size = 64;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Line => Get("line", c => { c.Stroke(Pts((12, 32), (52, 32)), 3f); c.Dots(Pts((12, 32), (25, 32), (39, 32), (52, 32)), 4.5f); });
        public static Sprite Arc => Get("arc", c => { var p = ArcPts(32, 44, 22, 0, Math.PI, 24); c.Stroke(p, 3f); c.Dots(Every(p, 6), 4.5f); });
        public static Sprite Circle => Get("circle", c => { var p = ArcPts(32, 32, 21, 0, 2 * Math.PI, 40); c.Stroke(p, 3f); c.Dots(Every(p, 5), 4f); });
        public static Sprite Curve => Get("curve", c => { var p = SinePts(); c.Stroke(p, 3f); c.Dots(Every(p, 8), 4.5f); });
        public static Sprite Shape => Get("shape", c =>
        {
            var p = BlobPts();
            c.Stroke(p, 3f);
            c.Dots(Pts((26, 30), (38, 30), (32, 40), (24, 40), (40, 40)), 3.5f);
        });
        public static Sprite Single => Get("single", c => { c.Stroke(Pts((32, 14), (32, 40)), 3f); c.Stroke(Pts((32, 30), (22, 22)), 3f); c.Stroke(Pts((32, 24), (42, 16)), 3f); c.Stroke(Pts((18, 48), (46, 48)), 3f); });
        public static Sprite Side => Get("side", c =>
        {
            c.Stroke(Pts((32, 12), (32, 52)), 3f);
            c.Stroke(Pts((10, 32), (24, 32)), 3f); c.Stroke(Pts((10, 32), (16, 26)), 3f); c.Stroke(Pts((10, 32), (16, 38)), 3f);
            c.Stroke(Pts((40, 32), (54, 32)), 3f); c.Stroke(Pts((54, 32), (48, 26)), 3f); c.Stroke(Pts((54, 32), (48, 38)), 3f);
        });
        public static Sprite Undo => Get("undo", c =>
        {
            c.Stroke(ArcPts(34, 34, 16, -Math.PI * 0.75, Math.PI * 0.9, 24), 3f);
            c.Stroke(Pts((15, 23), (18, 36)), 3f); c.Stroke(Pts((15, 23), (27, 26)), 3f);
        });
        public static Sprite Clear => Get("clear", c => { c.Stroke(Pts((16, 16), (48, 48)), 4f); c.Stroke(Pts((48, 16), (16, 48)), 4f); });
        public static Sprite Finish => Get("finish", c => c.Stroke(Pts((14, 34), (27, 46), (50, 18)), 4.5f));
        public static Sprite Reshuffle => Get("reshuffle", c =>
        {
            c.Stroke(Pts((14, 14), (50, 14), (50, 50), (14, 50), (14, 14)), 3f);
            c.Dots(Pts((23, 23), (32, 32), (41, 41), (41, 23), (23, 41)), 4f);
        });
        public static Sprite Settings => Get("settings", c =>
        {
            c.Stroke(ArcPts(32, 32, 12, 0, 2 * Math.PI, 32), 4f);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                c.Stroke(Pts(((float)(32 + 15 * Math.Cos(a)), (float)(32 + 15 * Math.Sin(a))), ((float)(32 + 22 * Math.Cos(a)), (float)(32 + 22 * Math.Sin(a)))), 5f);
            }
        });
        public static Sprite Auto => Get("auto", c =>
        {
            c.Stroke(ArcPts(32, 32, 18, Math.PI * 0.2, Math.PI * 1.9, 30), 3f);
            c.Stroke(Pts((47, 18), (48, 28)), 3f); c.Stroke(Pts((47, 18), (38, 22)), 3f);
            c.Dots(Pts((32, 32)), 5f);
        });

        private static Sprite Get(string key, Action<Canvas> draw)
        {
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
            try
            {
                var canvas = new Canvas();
                draw(canvas);
                s = canvas.ToSprite("AdvancedPlanting_" + key);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Icon " + key + " failed: " + e.Message);
                s = null;
            }
            Cache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------ geometry helpers
        // Coordinates are in a 64x64 box with y pointing down (like an image); flipped on export.

        private static List<Vector2> Pts(params (float x, float y)[] pts)
        {
            var list = new List<Vector2>(pts.Length);
            foreach (var p in pts) list.Add(new Vector2(p.x, p.y));
            return list;
        }

        private static List<Vector2> ArcPts(float cx, float cy, float r, double from, double to, int steps)
        {
            var list = new List<Vector2>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                double a = from + (to - from) * i / steps;
                list.Add(new Vector2((float)(cx + r * Math.Cos(a)), (float)(cy - r * Math.Sin(a))));
            }
            return list;
        }

        private static List<Vector2> SinePts()
        {
            var list = new List<Vector2>();
            for (int i = 0; i <= 32; i++)
            {
                float x = 10 + 44f * i / 32;
                float y = 32 - 14f * (float)Math.Sin((x - 10) / 44f * 2 * Math.PI);
                list.Add(new Vector2(x, y));
            }
            return list;
        }

        private static List<Vector2> BlobPts()
        {
            var list = new List<Vector2>();
            for (int i = 0; i <= 40; i++)
            {
                double a = 2 * Math.PI * i / 40;
                double r = 21 + 4 * Math.Sin(3 * a) + 2 * Math.Cos(2 * a);
                list.Add(new Vector2((float)(32 + r * Math.Cos(a)), (float)(34 - 0.85 * r * Math.Sin(a))));
            }
            return list;
        }

        private static List<Vector2> Every(List<Vector2> pts, int step)
        {
            var list = new List<Vector2>();
            for (int i = 0; i < pts.Count; i += step) list.Add(pts[i]);
            return list;
        }

        /// <summary>Coverage buffer with signed-distance anti-aliasing.</summary>
        private sealed class Canvas
        {
            private readonly float[] _alpha = new float[Size * Size];

            public void Stroke(List<Vector2> pts, float width)
            {
                float half = width * 0.5f;
                for (int i = 0; i + 1 < pts.Count; i++) Segment(pts[i], pts[i + 1], half);
            }

            public void Dots(List<Vector2> pts, float radius)
            {
                foreach (Vector2 p in pts) Segment(p, p, radius);
            }

            private void Segment(Vector2 a, Vector2 b, float half)
            {
                int x0 = Math.Max(0, (int)(Math.Min(a.x, b.x) - half - 2));
                int x1 = Math.Min(Size - 1, (int)(Math.Max(a.x, b.x) + half + 2));
                int y0 = Math.Max(0, (int)(Math.Min(a.y, b.y) - half - 2));
                int y1 = Math.Min(Size - 1, (int)(Math.Max(a.y, b.y) + half + 2));
                float abx = b.x - a.x, aby = b.y - a.y;
                float len2 = abx * abx + aby * aby;
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float px = x + 0.5f - a.x, py = y + 0.5f - a.y;
                        float t = len2 > 1e-6f ? Mathf.Clamp01((px * abx + py * aby) / len2) : 0f;
                        float dx = px - abx * t, dy = py - aby * t;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        float cover = Mathf.Clamp01(half + 0.5f - d);
                        int idx = y * Size + x;
                        if (cover > _alpha[idx]) _alpha[idx] = cover;
                    }
                }
            }

            public Sprite ToSprite(string name)
            {
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[Size * Size];
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        byte a = (byte)(_alpha[y * Size + x] * 255f);
                        // Texture rows go bottom-up; our drawing is top-down.
                        pixels[(Size - 1 - y) * Size + x] = new Color32(255, 255, 255, a);
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = name;
                return sprite;
            }
        }
    }
}
