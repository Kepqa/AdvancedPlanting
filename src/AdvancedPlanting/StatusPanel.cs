using System.Text;
using AdvancedPlanting.Core;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>Small IMGUI status panel shown while a planting figure is being edited.</summary>
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

        public void Draw(FigureState state, LayoutService layout, Plant plant, string plantName, int seedsInInventory, float spacing)
        {
            if (!_config.ShowHud.Value) return;
            EnsureStyles();

            _sb.Length = 0;
            _sb.Append("<b>AdvancedPlanting</b>");
            if (!state.Enabled) _sb.Append("  <color=#ff8080>(off)</color>");
            _sb.Append('\n');
            _sb.Append(plantName).Append('\n');
            _sb.Append("Mode: ").Append(ModeName(state.Mode));
            if (state.Mode != PathMode.Contour) _sb.Append("   Side: ").Append(SideName(state.Side));
            _sb.Append('\n');
            _sb.Append("Points: ").Append(state.ControlPoints.Count).Append(" / ").Append(state.Mode.MinControlPoints());
            int max = state.Mode.MaxControlPoints();
            if (max > 0) _sb.Append(" (max ").Append(max).Append(')');
            _sb.Append('\n');
            AppendParam(state, EditParameter.Spacing, spacing.ToString("0.00") + " m");
            AppendParam(state, EditParameter.Rows, state.Mode == PathMode.Contour && state.Rows == 0 ? "fill" : state.Rows.ToString());
            _sb.Append('\n');
            AppendParam(state, EditParameter.Jitter, state.Jitter.ToString("0.00"));
            AppendParam(state, EditParameter.Thinning, state.Thinning.ToString("0.00"));
            _sb.Append('\n');

            if (state.HasEnoughPoints)
            {
                int invalid = layout.Slots.Count - layout.ValidCount;
                _sb.Append("Plants: <color=#80ff80>").Append(layout.ValidCount).Append("</color>");
                if (invalid > 0)
                    _sb.Append(" + <color=#ff8080>").Append(invalid).Append(" blocked: ")
                        .Append(PlacementValidator.Describe(layout.TopBlockReason)).Append("</color>");
                if (layout.Truncated) _sb.Append(" <color=#ffd080>(cap reached)</color>");
                _sb.Append("   Seeds: ");
                if (seedsInInventory < layout.ValidCount) _sb.Append("<color=#ff8080>");
                _sb.Append(seedsInInventory);
                if (seedsInInventory < layout.ValidCount) _sb.Append("</color>");
                _sb.Append('\n');
            }
            else
            {
                _sb.Append("<color=#c0c0c0>Add ").Append(state.Mode.MinControlPoints() - state.ControlPoints.Count)
                    .Append(" more point(s) with [").Append(_config.AddPointKey.Value).Append("]</color>\n");
            }

            _sb.Append("<size=11><color=#b0b0b0>")
                .Append('[').Append(_config.AddPointKey.Value).Append("] point  ")
                .Append('[').Append(_config.RemovePointKey.Value).Append("] undo  ")
                .Append('[').Append(_config.ClearKey.Value).Append("] clear  ")
                .Append('[').Append(_config.CycleModeKey.Value).Append("] path  ")
                .Append('[').Append(_config.CycleSideKey.Value).Append("] side\n")
                .Append('[').Append(_config.NextParameterKey.Value).Append("] next parameter  ")
                .Append(Plugin.ModName(_config.ParameterModifier.Value)).Append("+wheel change  ")
                .Append('[').Append(_config.RerollSeedKey.Value).Append("] reshuffle  ")
                .Append("LMB plant")
                .Append("</color></size>");

            string text = _sb.ToString();
            GUIContent content = new GUIContent(text);
            Vector2 size = _textStyle.CalcSize(content);
            Rect rect = new Rect(_config.HudX.Value, _config.HudY.Value, size.x + 20f, size.y + 16f);
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
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 13,
                wordWrap = false,
            };
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
                case RowSide.Both: return "both";
                case RowSide.Left: return "left";
                case RowSide.Right: return "right";
                default: return side.ToString();
            }
        }

        private void AppendParam(FigureState state, EditParameter p, string value)
        {
            bool selected = state.Selected == p;
            if (selected) _sb.Append("<color=#ffd84d>> ");
            _sb.Append(ParameterName(p)).Append(": ").Append(value);
            if (selected) _sb.Append("</color>");
            _sb.Append("   ");
        }

        public static string ParameterName(EditParameter p)
        {
            switch (p)
            {
                case EditParameter.Spacing: return "Spacing";
                case EditParameter.Rows: return "Rows";
                case EditParameter.Jitter: return "Jitter";
                case EditParameter.Thinning: return "Thinning";
                default: return p.ToString();
            }
        }
    }
}
