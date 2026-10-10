using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedPlanting.UI
{
    /// <summary>Wheel icons as Unity sprites, rasterised once from <see cref="IconArt"/>.</summary>
    internal static class Icons
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Single => Get("single");
        public static Sprite Line => Get("line");
        public static Sprite Arc => Get("arc");
        public static Sprite Circle => Get("circle");
        public static Sprite Curve => Get("curve");
        public static Sprite Shape => Get("shape");
        public static Sprite Fill => Get("fill");
        public static Sprite Side => Get("side");
        public static Sprite Undo => Get("undo");
        public static Sprite Clear => Get("clear");
        public static Sprite Reshuffle => Get("reshuffle");
        public static Sprite Settings => Get("settings");

        private static Sprite Get(string name)
        {
            if (Cache.TryGetValue(name, out Sprite cached) && cached != null) return cached;
            Sprite sprite = null;
            try
            {
                byte[] rgba = IconArt.Render(name);
                int size = IconArt.Size;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    int src = y * size * 4;
                    int dst = (size - 1 - y) * size; // texture rows go bottom-up
                    for (int x = 0; x < size; x++, src += 4)
                        pixels[dst + x] = new Color32(rgba[src], rgba[src + 1], rgba[src + 2], rgba[src + 3]);
                }
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
                {
                    name = "AdvancedPlanting_" + name,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 2,
                };
                tex.SetPixels32(pixels);
                tex.Apply(true, true);
                sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = tex.name;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Icon " + name + " failed: " + e.Message);
            }
            Cache[name] = sprite;
            return sprite;
        }
    }
}
