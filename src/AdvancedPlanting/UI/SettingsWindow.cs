using AdvancedPlanting.Core;
using UnityEngine;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Planting settings window, opened from the planting wheel. Every change marks the figure
    /// dirty, so the ghosts in the world update while the slider moves.
    ///
    /// While open, the game treats it like the build menu (see InputPatches): the cursor is
    /// free, the character and camera ignore input, and Esc closes the window instead of
    /// opening the game menu.
    /// </summary>
    public sealed class SettingsWindow
    {
        private const int WindowId = 0x41504C54; // "APLT"
        private const float Width = 440f;

        private readonly Plugin _plugin;
        private Rect _rect;
        private bool _open;
        private int _closedFrame = -10;
        private GUIStyle _label, _value, _hint, _header;


        public SettingsWindow(Plugin plugin)
        {
            _plugin = plugin;
        }

        public bool IsOpen => _open;

        /// <summary>
        /// True while open and for one frame after closing, so the Esc that closed the window
        /// is not seen again by the game menu in the same or next frame.
        /// </summary>
        public bool IsBlockingInput => _open || Time.frameCount <= _closedFrame + 1;

        public void Open()
        {
            if (_open) return;
            _open = true;
            _rect = new Rect((Screen.width - Width) * 0.5f, Screen.height * 0.18f, Width, 10f);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _closedFrame = Time.frameCount;
        }

        public void OnGUI()
        {
            if (!_open) return;
            if (!_plugin.HasPlantContext)
            {
                Close();
                return;
            }

            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                Close();
                return;
            }

            EnsureStyles();
            GUI.backgroundColor = new Color(0.15f, 0.12f, 0.09f, 0.97f);
            _rect = GUILayout.Window(WindowId, _rect, Draw, Loc.T("window.title", _plugin.PlantDisplayName), GUILayout.Width(Width));
        }

        private void Draw(int id)
        {
            FigureState s = _plugin.State;
            GUI.backgroundColor = Color.white;

            GUILayout.Space(4);
            GUILayout.Label(Loc.T("window.path"), _header);
            int mode = GUILayout.Toolbar((int)s.Mode, new[] { StatusPanel.ModeName(PathMode.Segment), StatusPanel.ModeName(PathMode.Arc), StatusPanel.ModeName(PathMode.Circle), StatusPanel.ModeName(PathMode.Spline), StatusPanel.ModeName(PathMode.Contour) });
            if (mode != (int)s.Mode)
            {
                _plugin.SetFigureMode(true);
                s.SetMode((PathMode)mode);
            }

            GUILayout.Space(6);
            bool closed = s.IsClosedPath;
            if (closed) s.SetFill(GUILayout.Toggle(s.Fill, " " + Loc.T("window.fill")));
            if (!(closed && s.Fill))
            {
                GUILayout.Label(Loc.T("window.rows_go"), _header);
                int side = GUILayout.Toolbar((int)s.Side, new[] { StatusPanel.SideName(RowSide.Both, closed), StatusPanel.SideName(RowSide.Left, closed), StatusPanel.SideName(RowSide.Right, closed) });
                if (side != (int)s.Side) s.SetSide((RowSide)side);
            }

            GUILayout.Space(8);
            float min = _plugin.MinSpacing;
            float spacing = _plugin.CurrentSpacing;
            float max = Mathf.Max(min * 4f, 6f);
            float newSpacing = Slider(Loc.T("window.spacing"), spacing, min, max, Loc.T("window.unit.m", spacing.ToString("0.00")));
            if (!Mathf.Approximately(newSpacing, spacing)) _plugin.SetSpacing(newSpacing);

            if (!(closed && s.Fill))
            {
                float rows = Slider(Loc.T("window.rows"), s.Rows, 1, 20, s.Rows.ToString());
                s.SetRows(Mathf.RoundToInt(rows));
            }

            float jitter = Slider(Loc.T("window.jitter"), s.Jitter, 0f, 1f, s.Jitter.ToString("0.00"));
            s.SetJitter(jitter);
            if (s.Jitter > 0f && _plugin.JitterRoom <= 0.01f)
                GUILayout.Label(Loc.T("window.no_room"), _hint);

            float thinning = Slider(Loc.T("window.thinning"), s.Thinning, 0f, 0.9f, Mathf.RoundToInt(s.Thinning * 100f) + " %");
            s.SetThinning(thinning);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Loc.T("window.reshuffle"), GUILayout.Width(200))) s.RerollSeed();
            GUILayout.FlexibleSpace();
            s.AutoContinue = GUILayout.Toggle(s.AutoContinue, " " + Loc.T("window.auto"));
            GUILayout.EndHorizontal();
            GUILayout.Label(Loc.T("window.auto.desc"), _hint);

            GUILayout.Space(8);
            GUILayout.Label(_plugin.CostSummary(), _label);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Loc.T("window.close") + " (Esc)", GUILayout.Width(140))) Close();
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private float Slider(string name, float value, float min, float max, string shown)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, _label, GUILayout.Width(80));
            float v = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            GUILayout.Label(shown, _value, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            return v;
        }

        private void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            _label.normal.textColor = new Color(0.95f, 0.92f, 0.85f);
            _value = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
            _value.normal.textColor = new Color(1f, 0.85f, 0.35f);
            _header = new GUIStyle(_label) { fontStyle = FontStyle.Bold, fontSize = 13 };
            _header.normal.textColor = new Color(0.85f, 0.78f, 0.6f);
            _hint = new GUIStyle(_label) { fontSize = 11, wordWrap = true };
            _hint.normal.textColor = new Color(0.7f, 0.68f, 0.62f);
        }
    }
}
