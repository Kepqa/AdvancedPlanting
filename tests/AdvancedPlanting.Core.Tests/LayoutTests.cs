using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedPlanting.Core;
using Xunit;

namespace AdvancedPlanting.Core.Tests
{
    public class LayoutTests
    {
        private static LayoutRequest Line(float length, float spacing, int rows = 1)
        {
            return new LayoutRequest
            {
                Mode = PathMode.Segment,
                ControlPoints = new List<Vec2> { new Vec2(0, 0), new Vec2(length, 0) },
                Spacing = spacing,
                Rows = rows,
                MinDistance = 1f,
                Seed = 7,
            };
        }

        [Fact]
        public void StraightLine_SingleRow_EvenlySpaced()
        {
            LayoutResult r = PlantingLayout.Generate(Line(10f, 2f));
            Assert.Equal(6, r.Points.Count);
            for (int i = 1; i < r.Points.Count; i++)
            {
                float d = Vec2.Distance(r.Points[i - 1].Position, r.Points[i].Position);
                Assert.InRange(d, 1.99f, 2.01f);
            }
        }

        [Fact]
        public void ThreeRows_Both_AreCentredOnGuide()
        {
            LayoutResult r = PlantingLayout.Generate(Line(10f, 2f, rows: 3));
            var rowIds = r.Points.Select(p => p.Row).Distinct().OrderBy(x => x).ToArray();
            Assert.Equal(new[] { -1, 0, 1 }, rowIds);
            Assert.Equal(18, r.Points.Count);
            // Left normal of +X direction is +Z, so row +1 sits at z = +2.
            Assert.All(r.Points.Where(p => p.Row == 1), p => Assert.InRange(p.Position.Z, 1.99f, 2.01f));
            Assert.All(r.Points.Where(p => p.Row == -1), p => Assert.InRange(p.Position.Z, -2.01f, -1.99f));
        }

        [Fact]
        public void RowSideLeft_PutsAllRowsOnOneSide()
        {
            LayoutRequest req = Line(10f, 2f, rows: 2);
            req.Side = RowSide.Left;
            LayoutResult r = PlantingLayout.Generate(req);
            Assert.All(r.Points, p => Assert.True(p.Position.Z >= -0.01f));
            Assert.Contains(r.Points, p => p.Row == 1);
        }

        [Fact]
        public void Jitter_NeverViolatesMinDistance_AndIsDeterministic()
        {
            LayoutRequest req = Line(30f, 2f, rows: 4);
            req.Jitter = 1f;
            req.MinDistance = 1.2f;
            LayoutResult a = PlantingLayout.Generate(req);
            LayoutResult b = PlantingLayout.Generate(req);

            Assert.True(a.Points.Count > 40);
            Assert.Equal(a.Points.Select(p => p.Position), b.Points.Select(p => p.Position));

            for (int i = 0; i < a.Points.Count; i++)
            {
                for (int j = i + 1; j < a.Points.Count; j++)
                {
                    float d = Vec2.Distance(a.Points[i].Position, a.Points[j].Position);
                    Assert.True(d >= req.MinDistance - 1e-4f, $"points {i} and {j} are {d} apart");
                }
            }

            // Jitter actually moved something.
            LayoutResult plain = PlantingLayout.Generate(Line(30f, 2f, rows: 4));
            bool moved = a.Points.Zip(plain.Points, (x, y) => Vec2.Distance(x.Position, y.Position)).Any(d => d > 0.05f);
            Assert.True(moved);
        }

        [Fact]
        public void DifferentSeeds_GiveDifferentJitter()
        {
            LayoutRequest req = Line(20f, 2f, rows: 2);
            req.Jitter = 0.8f;
            LayoutResult a = PlantingLayout.Generate(req);
            req.Seed = 99;
            LayoutResult b = PlantingLayout.Generate(req);
            Assert.NotEqual(a.Points.Select(p => p.Position), b.Points.Select(p => p.Position));
        }

        [Fact]
        public void Thinning_RemovesRoughlyTheRequestedFraction()
        {
            LayoutRequest req = Line(200f, 1f, rows: 5);
            req.MinDistance = 0.5f;
            req.MaxPoints = 5000;
            int full = PlantingLayout.Generate(req).Points.Count;
            req.Thinning = 0.3f;
            int thinned = PlantingLayout.Generate(req).Points.Count;
            double ratio = (double)thinned / full;
            Assert.InRange(ratio, 0.62, 0.78);
        }

        [Fact]
        public void MaxPoints_TruncatesAndFlags()
        {
            LayoutRequest req = Line(100f, 1f, rows: 3);
            req.MinDistance = 0.5f;
            req.MaxPoints = 50;
            LayoutResult r = PlantingLayout.Generate(req);
            Assert.True(r.Truncated);
            Assert.Equal(50, r.Points.Count);
        }

        [Fact]
        public void Contour_FillsInwardWithConcentricRows()
        {
            var req = new LayoutRequest
            {
                Mode = PathMode.Contour,
                ControlPoints = new List<Vec2> { new Vec2(0, 0), new Vec2(12, 0), new Vec2(12, 12), new Vec2(0, 12) },
                Spacing = 2f,
                Rows = 0,
                MinDistance = 1f,
            };
            LayoutResult r = PlantingLayout.Generate(req);
            Assert.True(r.RowCurves.Count >= 3, $"only {r.RowCurves.Count} rows");
            Assert.True(r.Points.Count > 30);

            // Everything must stay inside the (slightly bulged) square.
            Assert.All(r.Points, p =>
            {
                Assert.InRange(p.Position.X, -1.5f, 13.5f);
                Assert.InRange(p.Position.Z, -1.5f, 13.5f);
            });

            // Inner rows are strictly inside the outer one.
            Vec2 centre = new Vec2(6, 6);
            float outer = r.Points.Where(p => p.Row == 0).Min(p => Vec2.Distance(p.Position, centre));
            float inner = r.Points.Where(p => p.Row == 1).Max(p => Vec2.Distance(p.Position, centre));
            Assert.True(inner < outer + 0.5f);
        }

        [Fact]
        public void Contour_InwardSign_IsOrientationIndependent()
        {
            var ccw = new LayoutRequest
            {
                Mode = PathMode.Contour,
                ControlPoints = new List<Vec2> { new Vec2(0, 0), new Vec2(10, 0), new Vec2(10, 10), new Vec2(0, 10) },
                Spacing = 2f,
                Rows = 2,
                MinDistance = 1f,
            };
            var cw = new LayoutRequest
            {
                Mode = PathMode.Contour,
                ControlPoints = new List<Vec2> { new Vec2(0, 0), new Vec2(0, 10), new Vec2(10, 10), new Vec2(10, 0) },
                Spacing = 2f,
                Rows = 2,
                MinDistance = 1f,
            };
            foreach (LayoutRequest req in new[] { ccw, cw })
            {
                LayoutResult r = PlantingLayout.Generate(req);
                Assert.Contains(r.Points, p => p.Row == 1);
                Assert.All(r.Points.Where(p => p.Row == 1), p =>
                {
                    Assert.InRange(p.Position.X, 0.5f, 9.5f);
                    Assert.InRange(p.Position.Z, 0.5f, 9.5f);
                });
            }
        }

        [Fact]
        public void TightBend_OffsetOnConcaveSide_IsTrimmedNotFolded()
        {
            // Hairpin: rows 3 units inside a bend of radius ~2 must not fold back on themselves.
            var req = new LayoutRequest
            {
                Mode = PathMode.Spline,
                ControlPoints = new List<Vec2> { new Vec2(0, 0), new Vec2(10, 0), new Vec2(12, 2), new Vec2(10, 4), new Vec2(0, 4) },
                Spacing = 1f,
                RowSpacing = 3f,
                Rows = 3,
                MinDistance = 0.8f,
            };
            LayoutResult r = PlantingLayout.Generate(req);
            Assert.True(r.Points.Count > 20);
            for (int i = 0; i < r.Points.Count; i++)
            {
                for (int j = i + 1; j < r.Points.Count; j++)
                {
                    Assert.True(Vec2.Distance(r.Points[i].Position, r.Points[j].Position) >= req.MinDistance - 1e-4f);
                }
            }
        }

        [Fact]
        public void NotEnoughControlPoints_GivesEmptyResult()
        {
            var req = new LayoutRequest { Mode = PathMode.Spline, ControlPoints = new List<Vec2> { new Vec2(1, 1) } };
            Assert.True(PlantingLayout.Generate(req).IsEmpty);
        }

        [Fact]
        public void MaxJitterOffset_IsZeroWhenSpacingEqualsMinDistance()
        {
            var req = new LayoutRequest { Spacing = 1f, MinDistance = 1f };
            Assert.Equal(0f, PlantingLayout.MaxJitterOffset(req));
            req.Spacing = 3f;
            Assert.Equal(1f, PlantingLayout.MaxJitterOffset(req));
        }
    }

    public class NoiseTests
    {
        [Fact]
        public void Hash_IsDeterministicAndSeedSensitive()
        {
            Assert.Equal(Noise.Hash(42, 1), Noise.Hash(42, 1));
            Assert.NotEqual(Noise.Hash(42, 1), Noise.Hash(42, 2));
            Assert.NotEqual(Noise.Hash(42, 1), Noise.Hash(43, 1));
        }

        [Fact]
        public void Float01_StaysInRange_AndIsRoughlyUniform()
        {
            int buckets = 10;
            var counts = new int[buckets];
            const int n = 20000;
            for (int i = 0; i < n; i++)
            {
                float v = Noise.Float01(i, 123);
                Assert.InRange(v, 0f, 0.9999999f);
                counts[(int)(v * buckets)]++;
            }
            foreach (int c in counts) Assert.InRange(c, n / buckets * 0.9, n / buckets * 1.1);
        }

        [Fact]
        public void InUnitDisk_StaysInsideDisk()
        {
            for (int i = 0; i < 5000; i++)
            {
                Assert.True(Noise.InUnitDisk(i, 5).Length <= 1.0001f);
            }
        }

        [Fact]
        public void Smooth2D_IsContinuous()
        {
            float prev = Noise.Smooth2D(0f, 0.3f, 9);
            for (float x = 0.01f; x < 5f; x += 0.01f)
            {
                float v = Noise.Smooth2D(x, 0.3f, 9);
                Assert.InRange(v, -1f, 1f);
                Assert.True(Math.Abs(v - prev) < 0.1f, $"jump at x={x}");
                prev = v;
            }
        }
    }
}
