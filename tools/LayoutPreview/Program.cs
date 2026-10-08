using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AdvancedPlanting.Core;

// Renders a few representative layouts to SVG so the geometry can be eyeballed without the game.
// Usage: dotnet run --project tools/LayoutPreview -- <output-dir>
namespace LayoutPreview
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string outDir = args.Length > 0 ? args[0] : "preview";
            Directory.CreateDirectory(outDir);

            var scenes = new List<(string name, LayoutRequest req)>
            {
                ("line_3rows", new LayoutRequest
                {
                    Mode = PathMode.Segment,
                    ControlPoints = new List<Vec2> { new Vec2(2, 10), new Vec2(28, 12) },
                    Spacing = 1.5f, Rows = 3, MinDistance = 1f,
                }),
                ("arc_3rows_jitter", new LayoutRequest
                {
                    Mode = PathMode.Arc,
                    ControlPoints = new List<Vec2> { new Vec2(3, 4), new Vec2(15, 14), new Vec2(27, 4) },
                    Spacing = 1.5f, Rows = 3, MinDistance = 1f, Jitter = 0.7f, Seed = 11,
                }),
                ("spline_4rows_jitter_thin", new LayoutRequest
                {
                    Mode = PathMode.Spline,
                    ControlPoints = new List<Vec2> { new Vec2(2, 6), new Vec2(9, 14), new Vec2(16, 5), new Vec2(23, 15), new Vec2(29, 8) },
                    Spacing = 1.4f, Rows = 4, MinDistance = 0.9f, Jitter = 1f, Thinning = 0.15f, Seed = 5,
                }),
                ("contour_fill", new LayoutRequest
                {
                    Mode = PathMode.Contour,
                    ControlPoints = new List<Vec2> { new Vec2(4, 3), new Vec2(16, 1), new Vec2(27, 7), new Vec2(24, 17), new Vec2(12, 19), new Vec2(3, 12) },
                    Spacing = 1.6f, Rows = 0, MinDistance = 1f, Jitter = 0.5f, Seed = 3,
                }),
                ("circle_fill", new LayoutRequest
                {
                    Mode = PathMode.Circle,
                    ControlPoints = new List<Vec2> { new Vec2(6, 10), new Vec2(15, 19), new Vec2(24, 10) },
                    Spacing = 1.5f, Rows = 3, Side = RowSide.Left, MinDistance = 1f,
                }),
                ("hairpin_trim", new LayoutRequest
                {
                    Mode = PathMode.Spline,
                    ControlPoints = new List<Vec2> { new Vec2(2, 4), new Vec2(20, 4), new Vec2(26, 10), new Vec2(20, 16), new Vec2(2, 16) },
                    Spacing = 1.2f, RowSpacing = 2.5f, Rows = 5, MinDistance = 0.8f,
                }),
            };

            foreach ((string name, LayoutRequest req) in scenes)
            {
                LayoutResult result = PlantingLayout.Generate(req);
                string svg = RenderSvg(req, result);
                string path = Path.Combine(outDir, name + ".svg");
                File.WriteAllText(path, svg);
                Console.WriteLine($"{name}: {result.Points.Count} plants, {result.RowCurves.Count} rows, rejected {result.Rejected}, truncated {result.Truncated} -> {path}");
            }
            return 0;
        }

        private static string RenderSvg(LayoutRequest req, LayoutResult result)
        {
            const float scale = 30f;
            const float pad = 1.5f;
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            void Grow(Vec2 p)
            {
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
            }
            foreach (Vec2 p in req.ControlPoints) Grow(p);
            foreach (LayoutPoint p in result.Points) Grow(p.Position);
            foreach (Polyline pl in result.RowCurves) foreach (Vec2 p in pl.Points) Grow(p);
            if (minX > maxX) { minX = minZ = 0; maxX = maxZ = 10; }

            float w = (maxX - minX + 2 * pad) * scale;
            float h = (maxZ - minZ + 2 * pad) * scale;
            string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
            string SX(float x) => F((x - minX + pad) * scale);
            string SZ(float z) => F(h - (z - minZ + pad) * scale);

            var sb = new StringBuilder();
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(w)}\" height=\"{F(h)}\" viewBox=\"0 0 {F(w)} {F(h)}\">\n");
            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#5a4a3a\"/>\n");

            for (int i = 0; i < result.RowCurves.Count; i++)
            {
                Polyline pl = result.RowCurves[i];
                if (pl.Count < 2) continue;
                sb.Append($"<polyline fill=\"none\" stroke=\"{(i == 0 ? "#ffd84d" : "#9ad98a")}\" stroke-width=\"{(i == 0 ? 2 : 1)}\" stroke-opacity=\"0.8\" points=\"");
                foreach (Vec2 p in pl.Points) sb.Append(SX(p.X)).Append(',').Append(SZ(p.Z)).Append(' ');
                if (pl.IsClosed) sb.Append(SX(pl.Points[0].X)).Append(',').Append(SZ(pl.Points[0].Z));
                sb.Append("\"/>\n");
            }

            float r = req.MinDistance * 0.5f * scale;
            foreach (LayoutPoint p in result.Points)
            {
                sb.Append($"<circle cx=\"{SX(p.Position.X)}\" cy=\"{SZ(p.Position.Z)}\" r=\"{F(r)}\" fill=\"#3fbf4f\" fill-opacity=\"0.35\" stroke=\"#1f7f2f\" stroke-width=\"1\"/>\n");
                sb.Append($"<circle cx=\"{SX(p.Position.X)}\" cy=\"{SZ(p.Position.Z)}\" r=\"3\" fill=\"#1f5f2f\"/>\n");
            }

            foreach (Vec2 c in req.ControlPoints)
            {
                sb.Append($"<circle cx=\"{SX(c.X)}\" cy=\"{SZ(c.Z)}\" r=\"6\" fill=\"#ff9b2f\" stroke=\"#000\" stroke-width=\"1\"/>\n");
            }

            sb.Append($"<text x=\"8\" y=\"18\" fill=\"#fff\" font-family=\"sans-serif\" font-size=\"14\">{req.Mode} spacing={F(req.Spacing)} rows={req.Rows} jitter={F(req.Jitter)} thin={F(req.Thinning)} plants={result.Points.Count}</text>\n");
            sb.Append("</svg>\n");
            return sb.ToString();
        }
    }
}
