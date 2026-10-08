using System;

namespace AdvancedPlanting.Core
{
    /// <summary>
    /// Point or vector on the horizontal (XZ) plane. The core never deals with height:
    /// the plugin samples terrain height for every final point separately.
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly float X;
        public readonly float Z;

        public Vec2(float x, float z)
        {
            X = x;
            Z = z;
        }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        public float Length => (float)Math.Sqrt(X * X + Z * Z);
        public float SqrLength => X * X + Z * Z;

        public Vec2 Normalized
        {
            get
            {
                float len = Length;
                return len < 1e-6f ? Zero : new Vec2(X / len, Z / len);
            }
        }

        /// <summary>Vector rotated 90 degrees counter-clockwise when looking down at the plane (left normal).</summary>
        public Vec2 PerpLeft => new Vec2(-Z, X);

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Z * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.X * s, a.Z * s);
        public static Vec2 operator /(Vec2 a, float s) => new Vec2(a.X / s, a.Z / s);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Z);

        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Z * b.Z;

        /// <summary>2D cross product (signed area of the parallelogram).</summary>
        public static float Cross(Vec2 a, Vec2 b) => a.X * b.Z - a.Z * b.X;

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public static float SqrDistance(Vec2 a, Vec2 b) => (a - b).SqrLength;

        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;

        public bool Equals(Vec2 other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => unchecked(X.GetHashCode() * 397 ^ Z.GetHashCode());
        public override string ToString() => $"({X:0.###}, {Z:0.###})";
    }
}
