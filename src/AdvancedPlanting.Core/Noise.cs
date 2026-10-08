using System;

namespace AdvancedPlanting.Core
{
    /// <summary>
    /// Deterministic noise. Everything is a pure function of (seed, coordinates) so a layout
    /// previewed as ghosts is exactly the layout that gets planted, and re-rolling the seed gives
    /// a new but equally stable variation.
    /// </summary>
    public static class Noise
    {
        /// <summary>Squirrel3-style integer hash. Fast, well distributed, no allocations.</summary>
        public static uint Hash(int position, uint seed)
        {
            const uint bitNoise1 = 0xB5297A4D;
            const uint bitNoise2 = 0x68E31DA4;
            const uint bitNoise3 = 0x1B56C4E9;

            uint mangled = unchecked((uint)position);
            mangled = unchecked(mangled * bitNoise1);
            mangled = unchecked(mangled + seed);
            mangled ^= mangled >> 8;
            mangled = unchecked(mangled + bitNoise2);
            mangled ^= mangled << 8;
            mangled = unchecked(mangled * bitNoise3);
            mangled ^= mangled >> 8;
            return mangled;
        }

        public static uint Hash(int x, int y, uint seed)
        {
            const int primeY = 198491317;
            return Hash(unchecked(x + primeY * y), seed);
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public static float Float01(int position, uint seed)
            => Hash(position, seed) / 4294967296f;

        public static float Float01(int x, int y, uint seed)
            => Hash(x, y, seed) / 4294967296f;

        /// <summary>Uniform float in [-1, 1).</summary>
        public static float FloatSigned(int position, uint seed)
            => Float01(position, seed) * 2f - 1f;

        /// <summary>
        /// Uniform random point inside the unit disk. Uses rejection-free polar sampling with the
        /// sqrt correction so the distribution is uniform by area, not clustered in the centre.
        /// </summary>
        public static Vec2 InUnitDisk(int position, uint seed)
        {
            float r = (float)Math.Sqrt(Float01(position, seed));
            float angle = Float01(position, unchecked(seed + 0x9E3779B9u)) * 2f * (float)Math.PI;
            return new Vec2(r * (float)Math.Cos(angle), r * (float)Math.Sin(angle));
        }

        /// <summary>
        /// Smooth value noise in [-1, 1] over continuous 2D coordinates. Bilinear interpolation of
        /// lattice hashes with a quintic fade; good enough for gently bending planting rows.
        /// </summary>
        public static float Smooth2D(float x, float y, uint seed)
        {
            int x0 = FloorToInt(x);
            int y0 = FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            float ux = Fade(fx);
            float uy = Fade(fy);

            float v00 = Float01(x0, y0, seed);
            float v10 = Float01(x0 + 1, y0, seed);
            float v01 = Float01(x0, y0 + 1, seed);
            float v11 = Float01(x0 + 1, y0 + 1, seed);

            float a = v00 + (v10 - v00) * ux;
            float b = v01 + (v11 - v01) * ux;
            float v = a + (b - a) * uy;
            return v * 2f - 1f;
        }

        /// <summary>2D vector field of smooth noise with independent components.</summary>
        public static Vec2 SmoothVector2D(float x, float y, uint seed)
        {
            return new Vec2(
                Smooth2D(x, y, seed),
                Smooth2D(x, y, unchecked(seed + 0x7F4A7C15u)));
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        private static int FloorToInt(float v)
        {
            int i = (int)v;
            return v < i ? i - 1 : i;
        }
    }
}
