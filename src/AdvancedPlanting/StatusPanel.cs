using System.Text;
using AdvancedPlanting.Core;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Small IMGUI status panel. Visible as soon as a seed is selected in figure mode, so the
    /// player always sees what the next click will do.
    /// </summary>
    public sealed class StatusPanel
    {
        private readonly ModConfig _config;
        private GUIStyle _boxStyle;
        private GUIStyle _textStyle;
        private readonly StringBuilder _sb = new StringBuilder(512);

        public StatusPanel(ModConfig config)
        {
            _config = config;
        }

        public void Draw(Plugin plugin)
        {
            if (!_config.ShowHud.Value) return;
            EnsureStyles();
            FigureState state = plugin.State;
            LayoutService layout = plugin.Layout;

            _sb.Length = 0;
            _sb.Append("<b>").Append(ModeName(state.Mode)).Append("</b>  ·  ").Append(plugin.PlantDisplayName).Append('\n');

            if (layout.TooLarge)
            {
                _sb.Append("<color=#ff8080>").Append(plugin.TooLargeText(layout.Extent)).Append("</color>\n");
            }
            else if (layout.Slots.Count > 0)
            {
                _sb.Append("<color=#90ee90>").Append(Loc.T("panel.to_plant", layout.ValidCount)).Append("</color>");
                if (layout.PlantedCount > 0) _sb.Append("   <color=#c8c8c8>").Append(Loc.T("panel.planted", layout.PlantedCount)).Append("</color>");
                if (layout.BlockedCount > 0)
                    _sb.Append("   <color=#ff8080>").Append(Loc.T("panel.blocked", layout.BlockedCount, PlacementValidator.Describe(layout.TopBlockReason))).Append("</color>");
                if (layout.Truncated) _sb.Append("   <color=#ffd080>").Append(Loc.T("panel.cap", _config.MaxPlants.Value)).Append("</color>");
                _sb.Append('\n');
                _sb.Append(plugin.ResourceLine()).Append('\n');
            }

            if (!plugin.KeyHintsShowing)
            {
                // Fallback when the game's key hints are turned off in its settings.
                _sb.Append("<color=#ffe08a>").Append(plugin.NextActionHint()).Append("</color>\n");
                string finish = plugin.FinishHint();
                if (finish != null) _sb.Append("<color=#ffe08a>").Append(finish).Append("</color>\n");
                _sb.Append("<size=11><color=#a0a0a0>[").Append(plugin.WheelKeyName).Append("] ").Append(Loc.T("hint.wheel")).Append("</color></size>\n");
            }
            if (state.AutoContinue) _sb.Append("<color=#a0d0ff>").Append(Loc.T("panel.auto")).Append("</color>");
            if (_sb.Length > 0 && _sb[_sb.Length - 1] == '\n') _sb.Length--;

            if (_sb.Length == 0) return;
            var content = new GUIContent(_sb.ToString());
            Vector2 size = _textStyle.CalcSize(content);
            var rect = new Rect(_config.HudX.Value, _config.HudY.Value, size.x + 20f, size.y + 16f);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, size.x, size.y), content, _textStyle);
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null) return;
            _boxStyle = new GUIStyle(GUI.skin.box);
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            tex.Apply();
            _boxStyle.normal.background = tex;
            _textStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14, wordWrap = false };
            _textStyle.normal.textColor = Color.white;
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
