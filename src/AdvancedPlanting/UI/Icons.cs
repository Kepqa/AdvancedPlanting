using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Wheel icons as Unity sprites.
    ///
    /// Rasterising the artwork is the slow part, and it is pure C#, so it runs on a worker
    /// thread right after the game starts (<see cref="Prewarm"/>). Uploading a finished image
    /// into a texture must happen on the main thread; <see cref="UploadStep"/> does one per frame
    /// while a seed is selected, so the first wheel opening finds everything ready.
    /// </summary>
    internal static class Icons
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, byte[]> Raw = new Dictionary<string, byte[]>();
        private static readonly object RawLock = new object();
        private static Thread _worker;
        private static bool _allUploaded;

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

        /// <summary>Starts rendering every icon on a background thread.</summary>
        public static void Prewarm()
        {
            if (_worker != null) return;
            _worker = new Thread(() =>
            {
                foreach (string name in IconArt.Names)
                {
                    try
                    {
                        byte[] rgba = IconArt.Render(name);
                        lock (RawLock) Raw[name] = rgba;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogWarning("Icon " + name + " failed: " + e.Message);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "AdvancedPlanting icons",
                Priority = System.Threading.ThreadPriority.BelowNormal,
            };
            _worker.Start();
        }

        /// <summary>Turns at most one finished image into a sprite. Call once per frame.</summary>
        public static void UploadStep()
        {
            if (_allUploaded) return;
            foreach (string name in IconArt.Names)
            {
                if (Sprites.TryGetValue(name, out Sprite existing) && existing != null) continue;
                byte[] rgba;
                lock (RawLock) Raw.TryGetValue(name, out rgba);
                if (rgba == null) return; // not rendered yet, try next frame
                Sprites[name] = Upload(name, rgba);
                return;
            }
            _allUploaded = true;
        }

        private static Sprite Get(string name)
        {
            // A sprite can be destroyed by Unity (e.g. unloading assets between worlds); remake it then.
            if (Sprites.TryGetValue(name, out Sprite s) && s != null) return s;
            byte[] rgba;
            lock (RawLock) Raw.TryGetValue(name, out rgba);
            if (rgba == null)
            {
                // Wheel opened before the worker finished (only right after start-up):
                // render this one here rather than show a blank slot.
                rgba = IconArt.Render(name);
                lock (RawLock) Raw[name] = rgba;
            }
            s = Upload(name, rgba);
            Sprites[name] = s;
            return s;
        }

        private static Sprite Upload(string name, byte[] rgba)
        {
            try
            {
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
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = tex.name;
                return sprite;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Icon " + name + " upload failed: " + e.Message);
                return null;
            }
        }
    }
}
