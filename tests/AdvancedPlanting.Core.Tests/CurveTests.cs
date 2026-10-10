using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedPlanting.Core;
using Xunit;

namespace AdvancedPlanting.Core.Tests
{
    public class CurveTests
    {
        [Fact]
        public void Segment_HasExactLength()
        {
            var pl = CurveBuilder.Build(PathMode.Segment, new[] { new Vec2(0, 0), new Vec2(3, 4) });
            Assert.False(pl.IsClosed);
            Assert.InRange(pl.Length, 4.999f, 5.001f);
            AssertClose(pl.PointAt(2.5f), new Vec2(1.5f, 2f));
        }

        [Fact]
        public void Arc_PassesThroughMidPointAndHasCircularLength()
        {
            // Half circle of radius 5 centred at origin, going through the top.
            var pl = CurveBuilder.Build(PathMode.Arc, new[] { new Vec2(-5, 0), new Vec2(0, 5), new Vec2(5, 0) });
            Assert.False(pl.IsClosed);
            Assert.InRange(pl.Length, Math.PI * 5 - 0.05, Math.PI * 5 + 0.05);
            AssertClose(pl.PointAt(pl.Length / 2f), new Vec2(0, 5), 0.02f);
        }

        [Fact]
        public void Arc_ChoosesTheLongWayWhenMidPointDemandsIt()
        {
            // Same endpoints, but mid point below: the arc must go through the bottom, so 3/4 of
            // the circle is not needed, still a half circle but mirrored.
            var pl = CurveBuilder.Build(PathMode.Arc, new[] { new Vec2(-5, 0), new Vec2(0, -5), new Vec2(5, 0) });
            AssertClose(pl.PointAt(pl.Length / 2f), new Vec2(0, -5), 0.02f);

            // Quarter-turn points with mid point on the far side force a 270 degree arc.
            var big = CurveBuilder.Build(PathMode.Arc, new[] { new Vec2(5, 0), new Vec2(-5, 0), new Vec2(0, 5) });
            Assert.InRange(big.Length, 1.5 * Math.PI * 5 - 0.05, 1.5 * Math.PI * 5 + 0.05);
        }

        [Fact]
        public void Circle_IsClosedWithFullCircumference()
        {
            var pl = CurveBuilder.Build(PathMode.Circle, new[] { new Vec2(0, 0), new Vec2(5, 0) });
            Assert.True(pl.IsClosed);
            AssertClose(pl.PointAt(0), new Vec2(5, 0), 0.01f);
            Assert.All(pl.Points, p => Assert.InRange(p.Length, 4.99f, 5.01f));
            Assert.InRange(pl.Length, 2 * Math.PI * 5 - 0.05, 2 * Math.PI * 5 + 0.05);
        }

        [Fact]
        public void Arc_CollinearPointsFallBackToSegment()
        {
            var pl = CurveBuilder.Build(PathMode.Arc, new[] { new Vec2(0, 0), new Vec2(1, 0), new Vec2(2, 0) });
            Assert.InRange(pl.Length, 1.999f, 2.001f);
        }

        [Fact]
        public void Spline_PassesThroughControlPoints()
        {
            var control = new[] { new Vec2(0, 0), new Vec2(4, 3), new Vec2(8, -2), new Vec2(12, 1) };
            var pl = CurveBuilder.Build(PathMode.Spline, control);
            foreach (Vec2 c in control)
            {
                Assert.True(pl.DistanceTo(c) < 0.02f, $"spline misses control point {c}");
            }
            AssertClose(pl.PointAt(0), control[0]);
            AssertClose(pl.PointAt(pl.Length), control[3]);
        }

        [Fact]
        public void Contour_IsClosedAndPassesThroughControlPoints()
        {
            var control = new[] { new Vec2(0, 0), new Vec2(10, 0), new Vec2(10, 8), new Vec2(0, 8) };
            var pl = CurveBuilder.Build(PathMode.Contour, control);
            Assert.True(pl.IsClosed);
            foreach (Vec2 c in control) Assert.True(pl.DistanceTo(c) < 0.02f);
            // Closed curve: distance L wraps back to the start.
            AssertClose(pl.PointAt(pl.Length), pl.PointAt(0), 0.01f);
        }

        [Fact]
        public void TooFewControlPoints_GiveDegenerateCurve()
        {
            var pl = CurveBuilder.Build(PathMode.Arc, new[] { new Vec2(0, 0), new Vec2(1, 0) });
            Assert.True(pl.IsDegenerate);
        }

        [Fact]
        public void EvenDistances_OpenIncludesBothEnds()
        {
            var pl = new Polyline(new[] { new Vec2(0, 0), new Vec2(10, 0) }, false);
            List<float> d = pl.EvenDistances(3f);
            Assert.Equal(4, d.Count); // 0, 3.33, 6.66, 10
            Assert.Equal(0f, d[0]);
            Assert.InRange(d[3], 9.999f, 10.001f);
        }

        [Fact]
        public void EvenDistances_ClosedDividesLoopEvenly()
        {
            var square = new Polyline(new[] { new Vec2(0, 0), new Vec2(10, 0), new Vec2(10, 10), new Vec2(0, 10) }, true);
            Assert.InRange(square.Length, 39.99f, 40.01f);
            List<float> d = square.EvenDistances(4f);
            Assert.Equal(10, d.Count);
            Assert.InRange(d[1] - d[0], 3.99f, 4.01f);
        }

        internal static void AssertClose(Vec2 actual, Vec2 expected, float tolerance = 1e-3f)
        {
            Assert.True(Vec2.Distance(actual, expected) <= tolerance, $"expected {expected}, got {actual}");
        }
    }
}
