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

            if (layout.Slots.Count > 0)
            {
                _sb.Append("<color=#90ee90>").Append(layout.ValidCount).Append(" to plant</color>");
                if (layout.PlantedCount > 0) _sb.Append("   <color=#c8c8c8>").Append(layout.PlantedCount).Append(" planted</color>");
                if (layout.BlockedCount > 0)
                    _sb.Append("   <color=#ff8080>").Append(layout.BlockedCount).Append(" blocked: ")
                        .Append(PlacementValidator.Describe(layout.TopBlockReason)).Append("</color>");
                if (layout.Truncated) _sb.Append("   <color=#ffd080>cap ").Append(_config.MaxPlants.Value).Append("</color>");
                _sb.Append('\n');
                _sb.Append(plugin.ResourceLine()).Append('\n');
            }

            _sb.Append("<color=#ffe08a>").Append(plugin.NextActionHint()).Append("</color>");
            if (state.AutoContinue) _sb.Append("\n<color=#a0d0ff>Auto-continue is on</color>");
            _sb.Append("\n<size=11><color=#a0a0a0>[").Append(plugin.RadialKeyName).Append("] planting wheel</color></size>");

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
                case PathMode.Segment: return "Line";
                case PathMode.Arc: return "Arc";
                case PathMode.Circle: return "Circle";
                case PathMode.Spline: return "Curve";
                case PathMode.Contour: return "Shape fill";
                default: return mode.ToString();
            }
        }

        public static string SideName(RowSide side)
        {
            switch (side)
            {
                case RowSide.Both: return "both sides";
                case RowSide.Left: return "left";
                case RowSide.Right: return "right";
                default: return side.ToString();
            }
        }
    }
}
