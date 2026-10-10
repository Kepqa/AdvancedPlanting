using System.Text;
using AdvancedPlanting.Core;
using AdvancedPlanting.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedPlanting
{
    /// <summary>
    /// Small status panel (top left) in the same style as the settings window: figure type,
    /// plant counts, seeds and stamina. Visible while a seed is selected in figure mode.
    /// </summary>
    public sealed class StatusPanel
    {
        private readonly ModConfig _config;
        private readonly StringBuilder _sb = new StringBuilder(512);
        private RectTransform _root;
        private TextMeshProUGUI _text;
        private string _last;
        private float _nextText;

        /// <summary>The text is rebuilt ten times a second, not every frame.</summary>
        private const float TextInterval = 0.1f;

        public StatusPanel(ModConfig config)
        {
            _config = config;
        }

        public void Hide()
        {
            if (_root != null && _root.gameObject.activeSelf) _root.gameObject.SetActive(false);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
        }

        /// <summary>Called every frame while the panel should be visible.</summary>
        public void Show(Plugin plugin)
        {
            if (!_config.ShowHud.Value)
            {
                Hide();
                return;
            }
            if (_root != null && _root.gameObject.activeSelf && Time.unscaledTime < _nextText) return;
            _nextText = Time.unscaledTime + TextInterval;
            FigureState state = plugin.State;
            LayoutService layout = plugin.Layout;

            _sb.Length = 0;
            _sb.Append("<color=#FFC97A><b>").Append(ModeName(state.Mode)).Append("</b></color>  <color=#C9B48F>").Append(plugin.PlantDisplayName).Append("</color>\n");

            if (layout.TooLarge)
            {
                _sb.Append("<color=#FF8C78>").Append(plugin.TooLargeText(layout.Extent)).Append("</color>\n");
            }
            else if (layout.Slots.Count > 0)
            {
                _sb.Append("<color=#96DC8C>").Append(Loc.T("panel.to_plant", layout.ValidCount)).Append("</color>");
                if (layout.PlantedCount > 0) _sb.Append("   <color=#C9B48F>").Append(Loc.T("panel.planted", layout.PlantedCount)).Append("</color>");
                if (layout.BlockedCount > 0)
                    _sb.Append("   <color=#FF8C78>").Append(Loc.T("panel.blocked", layout.BlockedCount, PlacementValidator.Describe(layout.TopBlockReason))).Append("</color>");
                if (layout.Truncated) _sb.Append("   <color=#FFC97A>").Append(Loc.T("panel.cap", _config.MaxPlants.Value)).Append("</color>");
                _sb.Append('\n');
                _sb.Append(plugin.ResourceLine()).Append('\n');
            }

            if (!plugin.KeyHintsShowing)
            {
                // Fallback when the game's key hints are turned off in its settings.
                foreach (var hint in plugin.CurrentHints())
                    _sb.Append(hint.Key).Append("  <color=#FFC97A>").Append(hint.Value).Append("</color>\n");
            }
            if (state.AutoContinue) _sb.Append("<color=#9CC8E8>").Append(Loc.T("panel.auto")).Append("</color>");
            if (_sb.Length > 0 && _sb[_sb.Length - 1] == '\n') _sb.Length--;

            if (_sb.Length == 0 || !EnsureBuilt())
            {
                Hide();
                return;
            }
            string text = _sb.ToString();
            if (text != _last)
            {
                _last = text;
                _text.text = text;
            }
            _root.anchoredPosition = new Vector2(_config.HudX.Value, -_config.HudY.Value);
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);
        }

        private bool EnsureBuilt()
        {
            if (_root != null) return true;
            Canvas canvas = global::Hud.instance != null ? global::Hud.instance.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return false;
            UnityEngine.UI.Image bg = UiKit.Image(canvas.rootCanvas.transform, "inset", "AdvancedPlanting_Status");
            _root = bg.rectTransform;
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0f, 1f);
            bg.raycastTarget = false;
            UiKit.Column(_root, 0f, new RectOffset(14, 14, 10, 10));
            var fitter = _root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _text = UiKit.Text(_root, 17f, UiKit.Cream, false, TextAlignmentOptions.TopLeft, false);
            _text.lineSpacing = 6f;
            return true;
        }

        public static string ModeName(PathMode mode)
        {
            switch (mode)
            {
                case PathMode.Segment: return Loc.T("mode.line");
                case PathMode.Arc: return Loc.T("mode.arc");
                case PathMode.Circle: return Loc.T("mode.circle");
                case PathMode.Spline: return Loc.T("mode.curve");
                case PathMode.Contour: return Loc.T("mode.shape");
                default: return mode.ToString();
            }
        }

        /// <summary>For closed paths left and right read as inside and outside.</summary>
        public static string SideName(RowSide side, bool closed)
        {
            switch (side)
            {
                case RowSide.Both: return Loc.T("side.both");
                case RowSide.Left: return Loc.T(closed ? "side.inside" : "side.left");
                case RowSide.Right: return Loc.T(closed ? "side.outside" : "side.right");
                default: return side.ToString();
            }
        }
    }
}
