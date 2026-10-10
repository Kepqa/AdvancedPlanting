using System;
using System.Collections.Generic;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Artwork for the settings window and status panel: dark wood panels with a bronze trim,
    /// buttons, slider parts, a checkbox. Rendered as RGBA bytes in pure C# (signed distance
    /// fields with analytic anti-aliasing), so it can be prepared on a worker thread and
    /// previewed outside the game. Colours are baked in; Unity images use white tint.
    ///
    /// Palette, chosen for contrast on the panel background (#1E1712):
    /// text cream #F1E6D0 (~14:1), muted parchment #C9B48F (~9:1), accent gold #FFC97A (~11:1).
    /// </summary>
    public static class UiArt
    {
        public struct Image
        {
            public string Name;
            public int Width, Height;
            /// <summary>RGBA, rows top to bottom.</summary>
            public byte[] Rgba;
            /// <summary>9-slice border in pixels: left, bottom, right, top (Unity order).</summary>
            public float BorderL, BorderB, BorderR, BorderT;
        }

        public static readonly string[] Names =
        {
            "panel", "inset", "button", "button_hover", "button_pressed", "button_selected",
            "track", "track_fill", "knob", "check_box", "check_mark", "divider",
        };

        // Palette (linear 0..1 sRGB values, used as-is).
        public static readonly float[] PanelFill = Hex("1E1712", 0.97f);
        public static readonly float[] PanelEdge = Hex("0B0806");
        public static readonly float[] Bronze = Hex("8A6A3F");
        public static readonly float[] BronzeBright = Hex("C89A5A");
        public static readonly float[] Gold = Hex("FFB347");
        public static readonly float[] InsetFill = Hex("130E0A", 0.92f);
        public static readonly float[] InsetEdge = Hex("3D3024");
        public static readonly float[] ButtonFill = Hex("2E241B");
        public static readonly float[] ButtonEdge = Hex("6B5539");
        public static readonly float[] HoverFill = Hex("3B2E22");
        public static readonly float[] HoverEdge = Hex("A27C49");
        public static readonly float[] PressedFill = Hex("22180F");
        public static readonly float[] SelectedFill = Hex("5B3C18");
        public static readonly float[] TrackFill = Hex("120D0A");
        public static readonly float[] TrackEdge = Hex("5E4A33");
        public static readonly float[] FillFrom = Hex("B97A30");
        public static readonly float[] FillTo = Hex("E8AC55");
        public static readonly float[] KnobFill = Hex("FFC97A");
        public static readonly float[] KnobEdge = Hex("3A2A18");

        public static Image Render(string name)
        {
            switch (name)
            {
                case "panel": return Panel();
                case "inset": return Box(name, 48, 6, 2, InsetFill, InsetEdge, null, 0f);
                case "button": return Button(name, ButtonFill, ButtonEdge, 2f);
                case "button_hover": return Button(name, HoverFill, HoverEdge, 2f);
                case "button_pressed": return Button(name, PressedFill, Gold, 2f);
                case "button_selected": return Button(name, SelectedFill, Gold, 2.5f);
                case "track": return Box(name, 32, 5, 1.5f, TrackFill, TrackEdge, null, 0f);
                case "track_fill": return TrackFillArt();
                case "knob": return Knob();
                case "check_box": return Box(name, 40, 6, 2.5f, TrackFill, Bronze, null, 0f);
                case "check_mark": return CheckMark();
                case "divider": return Divider();
                default: throw new ArgumentException("Unknown UI art " + name);
            }
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>Window background: grain, dark rim, inner bronze line, soft inner shadow.</summary>
        private static Image Panel()
        {
            const int size = 128;
            const float radius = 12f;
            var px = new float[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundRect(x + 0.5f, y + 0.5f, size, size, radius);
                    float cover = Cov(-d);
                    if (cover <= 0f) continue;

                    // Wood grain: low-contrast vertical streaks.
                    float grain = 1f + 0.05f * (float)Math.Sin(x * 0.55 + Math.Sin(y * 0.09) * 2.2)
                                     + 0.03f * Hash(x / 3, y / 17);
                    float[] c = Scale(PanelFill, grain);

                    // Inner shadow near the rim gives depth.
                    float depth = Clamp01(-d / 14f);
                    c = Lerp(Scale(PanelFill, 0.7f), c, 0.35f + 0.65f * depth);

                    c = Over(c, Bronze, Cov(1.4f - Math.Abs(-d - 6f)));   // inner bronze line
                    c = Over(c, PanelEdge, Cov(3f + d + 0.5f));           // dark rim (outer 3 px)
                    Put(px, size, x, y, c, cover);
                }
            }
            return Make("panel", size, size, px, 24f);
        }

        private static Image Box(string name, int size, float radius, float edgeWidth, float[] fill, float[] edge, float[] line, float lineInset)
        {
            var px = new float[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundRect(x + 0.5f, y + 0.5f, size, size, radius);
                    float cover = Cov(-d);
                    if (cover <= 0f) continue;
                    float[] c = fill;
                    if (line != null) c = Over(c, line, Cov(1f - Math.Abs(-d - lineInset)));
                    c = Over(c, edge, Cov(edgeWidth + d + 0.5f));
                    Put(px, size, x, y, c, cover);
                }
            }
            return Make(name, size, size, px, radius + edgeWidth + 2f);
        }

        /// <summary>Button: vertical sheen on the fill, crisp edge, slightly lighter top line.</summary>
        private static Image Button(string name, float[] fill, float[] edge, float edgeWidth)
        {
            const int size = 48;
            const float radius = 7f;
            var px = new float[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundRect(x + 0.5f, y + 0.5f, size, size, radius);
                    float cover = Cov(-d);
                    if (cover <= 0f) continue;
                    float t = y / (float)size; // 0 top, 1 bottom
                    float[] c = Scale(fill, 1.18f - 0.3f * t);
                    c = Over(c, Scale(edge, 1.25f), Cov(1f - Math.Abs(-d - edgeWidth - 1f)) * (t < 0.5f ? 0.35f : 0f));
                    c = Over(c, edge, Cov(edgeWidth + d + 0.5f));
                    Put(px, size, x, y, c, cover);
                }
            }
            return Make(name, size, size, px, radius + edgeWidth + 2f);
        }

        private static Image TrackFillArt()
        {
            const int w = 32, h = 32;
            var px = new float[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundRect(x + 0.5f, y + 0.5f, w, h, 5f);
                    float cover = Cov(-d);
                    if (cover <= 0f) continue;
                    float t = y / (float)h;
                    float[] c = Lerp(FillTo, FillFrom, t);
                    Put(px, w, x, y, c, cover);
                }
            }
            return Make("track_fill", w, h, px, 8f);
        }

        private static Image Knob()
        {
            const int size = 48;
            const float c0 = size * 0.5f, r = 20f;
            var px = new float[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c0, dy = y + 0.5f - c0;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    float cover = Cov(r - dist);
                    if (cover <= 0f) continue;
                    float light = Clamp01(1f - (dy + r) / (2f * r)); // top brighter
                    float[] c = Scale(KnobFill, 0.85f + 0.25f * light);
                    c = Over(c, Scale(KnobFill, 1.2f), Cov(5f - Hyp(dx + 5f, dy + 7f)) * 0.6f); // highlight
                    c = Over(c, KnobEdge, Cov(3f - (r - dist) + 0.5f));
                    Put(px, size, x, y, c, cover);
                }
            }
            return Make("knob", size, size, px, 0f);
        }

        private static Image CheckMark()
        {
            const int size = 40;
            var px = new float[size * size * 4];
            float ax = 9, ay = 21, bx = 17, by = 29, cx = 31, cy = 11;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Math.Min(Seg(x + 0.5f, y + 0.5f, ax, ay, bx, by), Seg(x + 0.5f, y + 0.5f, bx, by, cx, cy)) - 3.2f;
                    float cover = Cov(-d);
                    if (cover <= 0f) continue;
                    Put(px, size, x, y, Gold, cover);
                }
            }
            return Make("check_mark", size, size, px, 0f);
        }

        /// <summary>Thin bronze line fading at both ends with a small diamond in the middle.</summary>
        private static Image Divider()
        {
            const int w = 256, h = 16;
            var px = new float[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float fade = Clamp01(Math.Min(fx, w - fx) / 70f);
                    float line = Cov(1.1f - Math.Abs(fy - h * 0.5f)) * fade;
                    float diamond = Cov(5.5f - (Math.Abs(fx - w * 0.5f) + Math.Abs(fy - h * 0.5f)));
                    float a = Math.Max(line, diamond);
                    if (a <= 0f) continue;
                    Put(px, w, x, y, diamond > line ? BronzeBright : Bronze, a);
                }
            }
            return Make("divider", w, h, px, 0f);
        }

        // ------------------------------------------------------------------ helpers

        private static Image Make(string name, int w, int h, float[] px, float border)
        {
            var rgba = new byte[w * h * 4];
            for (int i = 0; i < rgba.Length; i++) rgba[i] = (byte)(Clamp01(px[i]) * 255f + 0.5f);
            return new Image { Name = name, Width = w, Height = h, Rgba = rgba, BorderL = border, BorderB = border, BorderR = border, BorderT = border };
        }

        private static void Put(float[] px, int w, int x, int y, float[] c, float alpha)
        {
            int i = (y * w + x) * 4;
            px[i] = c[0];
            px[i + 1] = c[1];
            px[i + 2] = c[2];
            px[i + 3] = alpha * (c.Length > 3 ? c[3] : 1f);
        }

        /// <summary>Signed distance to a rounded rectangle filling a w x h box (negative inside).</summary>
        private static float RoundRect(float x, float y, float w, float h, float r)
        {
            float qx = Math.Abs(x - w * 0.5f) - (w * 0.5f - r);
            float qy = Math.Abs(y - h * 0.5f) - (h * 0.5f - r);
            return Hyp(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f) - r;
        }

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay;
            float t = Clamp01(((px - ax) * abx + (py - ay) * aby) / (abx * abx + aby * aby));
            return Hyp(px - ax - abx * t, py - ay - aby * t);
        }

        private static float Cov(float v) => Clamp01(v + 0.5f);
        private static float Hyp(float x, float y) => (float)Math.Sqrt(x * x + y * y);
        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float[] Over(float[] under, float[] over, float a)
        {
            if (a <= 0f) return under;
            var r = new float[4];
            for (int i = 0; i < 3; i++) r[i] = under[i] + (over[i] - under[i]) * a;
            r[3] = under.Length > 3 ? under[3] : 1f;
            return r;
        }

        private static float[] Lerp(float[] a, float[] b, float t) => Over(a, b, Clamp01(t));

        private static float[] Scale(float[] c, float k)
        {
            var r = new float[4];
            for (int i = 0; i < 3; i++) r[i] = Clamp01(c[i] * k);
            r[3] = c.Length > 3 ? c[3] : 1f;
            return r;
        }

        private static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f - 0.5f;
        }

        public static float[] Hex(string hex, float alpha = 1f)
        {
            return new[]
            {
                Convert.ToInt32(hex.Substring(0, 2), 16) / 255f,
                Convert.ToInt32(hex.Substring(2, 2), 16) / 255f,
                Convert.ToInt32(hex.Substring(4, 2), 16) / 255f,
                alpha,
            };
        }
    }
}
