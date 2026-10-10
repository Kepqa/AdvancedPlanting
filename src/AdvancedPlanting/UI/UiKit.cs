using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Shared look for the mod's uGUI: sprites from <see cref="UiArt"/> (rendered on a worker
    /// thread at start-up), the game's TextMeshPro font, colours, and small factory helpers.
    /// </summary>
    internal static class UiKit
    {
        public static readonly Color Cream = new Color32(241, 230, 208, 255);
        public static readonly Color Muted = new Color32(201, 180, 143, 255);
        public static readonly Color GoldText = new Color32(255, 201, 122, 255);
        public static readonly Color SelectedText = new Color32(255, 226, 176, 255);
        public static readonly Color Good = new Color32(150, 220, 140, 255);
        public static readonly Color Bad = new Color32(255, 140, 120, 255);

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, UiArt.Image> Raw = new Dictionary<string, UiArt.Image>();
        private static readonly object RawLock = new object();
        private static Thread _worker;
        private static TMP_FontAsset _font;
        private static Material _fontMaterial;

        public static void Prewarm()
        {
            if (_worker != null) return;
            _worker = new Thread(() =>
            {
                foreach (string name in UiArt.Names)
                {
                    try
                    {
                        UiArt.Image img = UiArt.Render(name);
                        lock (RawLock) Raw[name] = img;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogWarning("UI art " + name + " failed: " + e.Message);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "AdvancedPlanting UI art",
                Priority = System.Threading.ThreadPriority.BelowNormal,
            };
            _worker.Start();
        }

        public static Sprite Sprite(string name)
        {
            if (Sprites.TryGetValue(name, out Sprite s) && s != null) return s;
            UiArt.Image img;
            bool have;
            lock (RawLock) have = Raw.TryGetValue(name, out img);
            if (!have)
            {
                img = UiArt.Render(name);
                lock (RawLock) Raw[name] = img;
            }
            var pixels = new Color32[img.Width * img.Height];
            for (int y = 0; y < img.Height; y++)
            {
                int src = y * img.Width * 4;
                int dst = (img.Height - 1 - y) * img.Width;
                for (int x = 0; x < img.Width; x++, src += 4)
                    pixels[dst + x] = new Color32(img.Rgba[src], img.Rgba[src + 1], img.Rgba[src + 2], img.Rgba[src + 3]);
            }
            var tex = new Texture2D(img.Width, img.Height, TextureFormat.RGBA32, false)
            {
                name = "AdvancedPlanting_ui_" + name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            var border = new Vector4(img.BorderL, img.BorderB, img.BorderR, img.BorderT);
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, img.Width, img.Height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            s.name = tex.name;
            Sprites[name] = s;
            return s;
        }

        /// <summary>True when the bold Averia asset was found; otherwise bold is applied as a style.</summary>
        private static bool _fontIsBold;
        private static bool _fontResolved;

        /// <summary>
        /// The game's UI font. Valheim's interface uses Averia Serif Libre Bold, so the bold asset
        /// is preferred; when only the regular one is around, text gets the bold style instead.
        /// The asset keeps the game's fallback fonts, so Cyrillic and other scripts still render.
        /// </summary>
        public static void ApplyFont(TMP_Text t)
        {
            if (!_fontResolved) ResolveFont();
            if (_font != null)
            {
                t.font = _font;
                if (_fontMaterial != null) t.fontSharedMaterial = _fontMaterial;
            }
            if (!_fontIsBold) t.fontStyle |= FontStyles.Bold;
        }

        private static void ResolveFont()
        {
            TMP_Text source = InventoryGui.instance != null ? InventoryGui.instance.m_recipeName : null;
            if (source == null && KeyHints.instance != null) source = KeyHints.instance.m_buildRotateKey;
            if (source == null) return; // UI not loaded yet; try again next time
            _fontResolved = true;
            _font = source.font;
            _fontMaterial = source.fontSharedMaterial;

            foreach (TMP_FontAsset asset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                string n = asset.name.ToLowerInvariant();
                if (n.Contains("averia") && n.Contains("bold") && !n.Contains("italic"))
                {
                    _font = asset;
                    _fontMaterial = asset.material;
                    break;
                }
            }
            _fontIsBold = _font != null && _font.name.ToLowerInvariant().Contains("bold");
            Plugin.Log.LogInfo("UI font: " + (_font != null ? _font.name : "TMP default") + (_fontIsBold ? "" : " (bold style)"));
        }

        // ------------------------------------------------------------------ factories

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Image(Transform parent, string sprite, string name = "Image")
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Sprite(sprite);
            img.type = img.sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.color = Color.white;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, float size, Color color, bool bold = false,
            TextAlignmentOptions align = TextAlignmentOptions.Left, bool wrap = true)
        {
            RectTransform rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            ApplyFont(t);
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        public static LayoutElement Layout(Component c, float minHeight = -1f, float preferredWidth = -1f, float flexibleWidth = -1f, float preferredHeight = -1f)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            le.preferredHeight = preferredHeight;
            return le;
        }

        public static HorizontalLayoutGroup Row(Transform parent, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            RectTransform rt = Rect("Row", parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing, RectOffset padding)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        /// <summary>Button using sprite swap for hover and press, with a TMP label and optional icon.</summary>
        public static Button Button(Transform parent, out TextMeshProUGUI label, Action onClick, float fontSize = 17f, Sprite icon = null)
        {
            Image bg = Image(parent, "button", "Button");
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = Sprite("button_hover"),
                selectedSprite = Sprite("button"),
                pressedSprite = Sprite("button_pressed"),
                disabledSprite = Sprite("button"),
            };
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            b.onClick.AddListener(() => onClick());

            var col = Column((RectTransform)bg.transform, 2f, new RectOffset(8, 8, icon != null ? 8 : 6, 6));
            col.childAlignment = TextAnchor.MiddleCenter;
            col.childForceExpandWidth = false;
            if (icon != null)
            {
                RectTransform iconRt = Rect("Icon", bg.transform);
                var img = iconRt.gameObject.AddComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                Layout(img, 40f, 40f, -1f, 40f);
            }
            label = Text(bg.transform, fontSize, Cream, false, TextAlignmentOptions.Center, false);
            return b;
        }

        /// <summary>Marks a button as the current choice: gold frame and warm label.</summary>
        public static void SetSelected(Button b, TextMeshProUGUI label, bool selected)
        {
            var img = (Image)b.targetGraphic;
            Sprite want = Sprite(selected ? "button_selected" : "button");
            if (img.sprite != want) img.sprite = want;
            var state = b.spriteState;
            Sprite hover = Sprite(selected ? "button_selected" : "button_hover");
            if (state.highlightedSprite != hover)
            {
                state.highlightedSprite = hover;
                state.selectedSprite = want;
                b.spriteState = state;
            }
            Color c = selected ? SelectedText : Cream;
            if (label.color != c) label.color = c;
            // Selection is shown by frame and colour; the weight stays the same as other text.
        }

        /// <summary>Checkbox with a label to its right.</summary>
        public static Toggle Toggle(Transform parent, out TextMeshProUGUI label, Action<bool> onChange)
        {
            HorizontalLayoutGroup row = Row(parent, 10f);
            Image box = Image(row.transform, "check_box", "Box");
            Layout(box, 26f, 26f, 0f, 26f);
            Image mark = Image(box.transform, "check_mark", "Mark");
            var markRt = mark.rectTransform;
            markRt.anchorMin = Vector2.zero;
            markRt.anchorMax = Vector2.one;
            markRt.offsetMin = markRt.offsetMax = Vector2.zero;
            mark.raycastTarget = false;

            var t = row.gameObject.AddComponent<Toggle>();
            t.targetGraphic = box;
            t.graphic = mark;
            t.transition = Selectable.Transition.ColorTint;
            var colors = t.colors;
            colors.highlightedColor = new Color(1.15f, 1.1f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.8f, 0.75f, 1f);
            t.colors = colors;
            var nav = t.navigation;
            nav.mode = Navigation.Mode.None;
            t.navigation = nav;
            t.onValueChanged.AddListener(v => onChange(v));

            label = Text(row.transform, 18f, Cream, false, TextAlignmentOptions.Left, false);
            // Clicks on the label toggle too: the row image is invisible but catches raycasts.
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            return t;
        }

        /// <summary>Slider with the mod's track, gold fill and knob.</summary>
        public static Slider Slider(Transform parent, Action<float> onChange)
        {
            RectTransform root = Rect("Slider", parent);
            Layout(root, 26f, -1f, 1f, 26f);
            var s = root.gameObject.AddComponent<Slider>();

            Image track = Image(root, "track", "Track");
            Stretch(track.rectTransform, 0f, 7f, 0f, 7f);

            RectTransform fillArea = Rect("FillArea", root);
            Stretch(fillArea, 0f, 7f, 0f, 7f);
            Image fill = Image(fillArea, "track_fill", "Fill");
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            RectTransform handleArea = Rect("HandleArea", root);
            Stretch(handleArea, 11f, 0f, 11f, 0f);
            Image knob = Image(handleArea, "knob", "Knob");
            knob.rectTransform.sizeDelta = new Vector2(26f, 0f);

            s.fillRect = fill.rectTransform;
            s.handleRect = knob.rectTransform;
            s.targetGraphic = knob;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.transition = Selectable.Transition.ColorTint;
            var colors = s.colors;
            colors.highlightedColor = new Color(1.12f, 1.08f, 1f, 1f);
            s.colors = colors;
            var nav = s.navigation;
            nav.mode = Navigation.Mode.None;
            s.navigation = nav;
            s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }

        public static void Divider(Transform parent)
        {
            Image img = Image(parent, "divider", "Divider");
            img.raycastTarget = false;
            Layout(img, 16f, -1f, 1f, 16f);
        }

        public static void Space(Transform parent, float height)
        {
            RectTransform rt = Rect("Space", parent);
            Layout(rt, height, -1f, -1f, height);
        }

        public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
