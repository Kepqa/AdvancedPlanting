namespace AdvancedPlanting.Core
{
    /// <summary>Shape of the guide path the planting grid is built around.</summary>
    public enum PathMode
    {
        /// <summary>Straight line through the first and last control point.</summary>
        Segment = 0,

        /// <summary>Circular arc through three control points (start, mid, end).</summary>
        Arc = 1,

        /// <summary>Full circle through three control points.</summary>
        Circle = 2,

        /// <summary>Open centripetal Catmull-Rom spline through all control points.</summary>
        Spline = 3,

        /// <summary>Closed Catmull-Rom spline; rows are generated inward to fill the shape.</summary>
        Contour = 4,
    }

    public static class PathModeExtensions
    {
        public static int MinControlPoints(this PathMode mode)
        {
            switch (mode)
            {
                case PathMode.Segment: return 2;
                case PathMode.Arc: return 3;
                case PathMode.Circle: return 3;
                case PathMode.Spline: return 2;
                case PathMode.Contour: return 3;
                default: return 2;
            }
        }

        /// <summary>Maximum control points the mode uses; extra points are ignored. 0 means unlimited.</summary>
        public static int MaxControlPoints(this PathMode mode)
        {
            switch (mode)
            {
                case PathMode.Segment: return 2;
                case PathMode.Arc: return 3;
                case PathMode.Circle: return 3;
                default: return 0;
            }
        }

        public static bool IsClosed(this PathMode mode) => mode == PathMode.Circle || mode == PathMode.Contour;

        public static PathMode Next(this PathMode mode) => (PathMode)(((int)mode + 1) % 5);
    }
}
