using System;
using AdvancedPlanting.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Planting settings window. Own artwork (<see cref="UiArt"/>) in a dark-wood and bronze
    /// palette with the game's Averia font, laid out with uGUI layout groups on the game's UI
    /// canvas, so it scales with the game's UI setting. Layout constants match the mock-up
    /// produced by tools/IconPreview + the mock script.
    ///
    /// While open the game treats it like the build menu (see UiPatches): cursor free, character
    /// and camera ignore input, Esc closes the window instead of opening the game menu.
    /// </summary>
    public sealed class SettingsWindow
    {
        private const float Width = 600f;
        private static readonly PathMode[] Modes = { PathMode.Segment, PathMode.Arc, PathMode.Circle, PathMode.Spline, PathMode.Contour };
        private static readonly RowSide[] Sides = { RowSide.Both, RowSide.Left, RowSide.Right };

        private readonly Plugin _plugin;
        private GameObject _root;
        private bool _open;
        private int _closedFrame = -10;
        private bool _syncing;

        private TextMeshProUGUI _title, _subtitle, _pathHeader, _sidesHeader, _noRoom, _autoDesc, _cost;
        private readonly Button[] _modeButtons = new Button[5];
        private readonly TextMeshProUGUI[] _modeLabels = new TextMeshProUGUI[5];
        private readonly Button[] _sideButtons = new Button[3];
        private readonly TextMeshProUGUI[] _sideLabels = new TextMeshProUGUI[3];
        private Toggle _fill, _auto;
        private TextMeshProUGUI _fillLabel, _autoLabel, _reshuffleLabel, _closeLabel;
        private GameObject _fillRow, _sidesBlock, _rowsRow;
        private SliderRow _spacing, _rows, _jitter, _thinning;

        public SettingsWindow(Plugin plugin)
        {
            _plugin = plugin;
        }

        public bool IsOpen => _open;

        /// <summary>True while open and one frame after, so the closing Esc does not open the game menu.</summary>
        public bool IsBlockingInput => _open || Time.frameCount <= _closedFrame + 1;

        public void Open()
        {
            if (_open) return;
            if (_root == null && !Build())
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, Loc.T("msg.window_unavailable"));
                return;
            }
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _open = true;
            Sync();
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _closedFrame = Time.frameCount;
            if (_root != null) _root.SetActive(false);
        }

        public void Destroy()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _open = false;
        }

        /// <summary>Called every frame from the plugin.</summary>
        public void Update()
        {
            if (!_open) return;
            if (_root == null || !_plugin.HasPlantContext || ZInput.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }
            Sync();
        }

        // ------------------------------------------------------------------ sync

        private void Sync()
        {
            FigureState s = _plugin.State;
            _syncing = true;
            try
            {
                Set(_title, Loc.T("window.heading"));
                Set(_subtitle, _plugin.PlantDisplayName);
                Set(_pathHeader, Loc.T("window.path").ToUpperInvariant());
                for (int i = 0; i < Modes.Length; i++)
                {
                    Set(_modeLabels[i], StatusPanel.ModeName(Modes[i]));
                    UiKit.SetSelected(_modeButtons[i], _modeLabels[i], s.Enabled && s.Mode == Modes[i]);
                }

                bool closed = s.IsClosedPath;
                Show(_fillRow, closed);
                _fill.SetIsOnWithoutNotify(s.Fill);
                Set(_fillLabel, Loc.T("window.fill"));

                bool rowsMatter = !(closed && s.Fill);
                Show(_sidesBlock, rowsMatter);
                Show(_rowsRow, rowsMatter);
                Set(_sidesHeader, Loc.T("window.rows_go").ToUpperInvariant());
                for (int i = 0; i < Sides.Length; i++)
                {
                    Set(_sideLabels[i], Capitalize(StatusPanel.SideName(Sides[i], closed)));
                    UiKit.SetSelected(_sideButtons[i], _sideLabels[i], s.Side == Sides[i]);
                }

                float min = _plugin.MinSpacing;
                float spacing = _plugin.CurrentSpacing;
                _spacing.Set(Loc.T("window.spacing"), min, Mathf.Max(min * 4f, 6f), spacing, Loc.T("window.unit.m", spacing.ToString("0.00")));
                _rows.Set(Loc.T("window.rows"), 1, 20, s.Rows, s.Rows.ToString());
                _jitter.Set(Loc.T("window.jitter"), 0f, 1f, s.Jitter, s.Jitter.ToString("0.00"));
                _thinning.Set(Loc.T("window.thinning"), 0f, 0.9f, s.Thinning, Mathf.RoundToInt(s.Thinning * 100f) + " %");

                Show(_noRoom.gameObject, s.Jitter > 0f && _plugin.JitterRoom <= 0.01f);
                Set(_noRoom, Loc.T("window.no_room"));
                Set(_reshuffleLabel, Loc.T("window.reshuffle"));
                _auto.SetIsOnWithoutNotify(s.AutoContinue);
                Set(_autoLabel, Loc.T("window.auto"));
                Set(_autoDesc, Loc.T("window.auto.desc"));
                Set(_cost, _plugin.CostSummary());
                Set(_closeLabel, Loc.T("window.close"));
            }
            finally
            {
                _syncing = false;
            }
        }

        // ------------------------------------------------------------------ build

        private bool Build()
        {
            try
            {
                Canvas canvas = FindCanvas();
                if (canvas == null) throw new InvalidOperationException("no game UI canvas");

                Image panel = UiKit.Image(canvas.rootCanvas.transform, "panel", "AdvancedPlanting_Settings");
                _root = panel.gameObject;
                RectTransform rt = panel.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(Width, 700f);
                UiKit.Column(rt, 0f, new RectOffset(30, 30, 26, 26));
                _root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Transform body = rt;

                _title = UiKit.Text(body, 30f, UiKit.GoldText, true, TextAlignmentOptions.Center);
                _subtitle = UiKit.Text(body, 17f, UiKit.Muted, false, TextAlignmentOptions.Center);
                UiKit.Space(body, 6f);
                UiKit.Divider(body);
                UiKit.Space(body, 10f);

                _pathHeader = Header(body);
                HorizontalLayoutGroup modeRow = UiKit.Row(body, 8f);
                modeRow.childForceExpandWidth = true;
                Sprite[] icons = { Icons.Line, Icons.Arc, Icons.Circle, Icons.Curve, Icons.Shape };
                for (int i = 0; i < Modes.Length; i++)
                {
                    PathMode mode = Modes[i];
                    _modeButtons[i] = UiKit.Button(modeRow.transform, out _modeLabels[i], () => Do(() =>
                    {
                        _plugin.SetFigureMode(true);
                        _plugin.State.SetMode(mode);
                    }), 15f, icons[i]);
                    UiKit.Layout(_modeButtons[i], 80f, 0f, 1f, 80f);
                }
                UiKit.Space(body, 12f);

                _fill = UiKit.Toggle(body, out _fillLabel, on => Do(() => _plugin.State.SetFill(on)));
                _fillRow = _fill.gameObject;
                UiKit.Space(body, 12f);

                RectTransform sides = UiKit.Rect("Sides", body);
                UiKit.Column(sides, 0f, new RectOffset(0, 0, 0, 12));
                _sidesBlock = sides.gameObject;
                _sidesHeader = Header(sides);
                HorizontalLayoutGroup sideRow = UiKit.Row(sides, 8f);
                sideRow.childForceExpandWidth = true;
                for (int i = 0; i < Sides.Length; i++)
                {
                    RowSide side = Sides[i];
                    _sideButtons[i] = UiKit.Button(sideRow.transform, out _sideLabels[i], () => Do(() => _plugin.State.SetSide(side)));
                    UiKit.Layout(_sideButtons[i], 40f, 0f, 1f, 40f);
                }

                UiKit.Divider(body);
                UiKit.Space(body, 8f);
                _spacing = MakeSliderRow(body, v => _plugin.SetSpacing(v));
                _rows = MakeSliderRow(body, v => _plugin.State.SetRows(Mathf.RoundToInt(v)), whole: true);
                _rowsRow = _rows.Root;
                _jitter = MakeSliderRow(body, v => _plugin.State.SetJitter(v));
                _noRoom = UiKit.Text(body, 15f, UiKit.Muted);
                _noRoom.fontStyle |= FontStyles.Italic;
                _thinning = MakeSliderRow(body, v => _plugin.State.SetThinning(v));
                UiKit.Space(body, 6f);
                UiKit.Divider(body);
                UiKit.Space(body, 10f);

                HorizontalLayoutGroup actions = UiKit.Row(body, 20f);
                Button reshuffle = UiKit.Button(actions.transform, out _reshuffleLabel, () => Do(() => _plugin.State.RerollSeed()));
                UiKit.Layout(reshuffle, 40f, 230f, 0f, 40f);
                _auto = UiKit.Toggle(actions.transform, out _autoLabel, on => Do(() => _plugin.State.AutoContinue = on));
                UiKit.Space(body, 8f);
                _autoDesc = UiKit.Text(body, 15f, UiKit.Muted);
                UiKit.Space(body, 10f);

                Image inset = UiKit.Image(body, "inset", "Cost");
                UiKit.Column(inset.rectTransform, 0f, new RectOffset(14, 14, 11, 11));
                _cost = UiKit.Text(inset.transform, 16f, UiKit.Cream);
                UiKit.Space(body, 14f);

                HorizontalLayoutGroup footer = UiKit.Row(body, 0f, TextAnchor.MiddleRight);
                footer.childForceExpandWidth = false;
                Button close = UiKit.Button(footer.transform, out _closeLabel, Close, 18f);
                UiKit.Layout(close, 42f, 170f, 0f, 42f);

                _root.SetActive(false);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not build the settings window: " + e);
                if (_root != null) UnityEngine.Object.Destroy(_root);
                _root = null;
                return false;
            }
        }

        private static Canvas FindCanvas()
        {
            if (InventoryGui.instance != null)
            {
                Canvas c = InventoryGui.instance.GetComponentInParent<Canvas>();
                if (c != null) return c;
            }
            return global::Hud.instance != null ? global::Hud.instance.GetComponentInParent<Canvas>() : null;
        }

        private static TextMeshProUGUI Header(Transform parent)
        {
            TextMeshProUGUI t = UiKit.Text(parent, 14f, UiKit.Muted, true);
            t.characterSpacing = 4f;
            UiKit.Layout(t, 22f);
            return t;
        }

        private SliderRow MakeSliderRow(Transform parent, Action<float> onChange, bool whole = false)
        {
            HorizontalLayoutGroup row = UiKit.Row(parent, 10f);
            UiKit.Layout(row, 36f);
            TextMeshProUGUI label = UiKit.Text(row.transform, 18f, UiKit.Cream, false, TextAlignmentOptions.Left, false);
            UiKit.Layout(label, -1f, 150f, 0f);
            Slider slider = UiKit.Slider(row.transform, v => Do(() => onChange(v)));
            slider.wholeNumbers = whole;
            TextMeshProUGUI value = UiKit.Text(row.transform, 18f, UiKit.GoldText, false, TextAlignmentOptions.Right, false);
            UiKit.Layout(value, -1f, 90f, 0f);
            return new SliderRow { Root = row.gameObject, Label = label, Slider = slider, Value = value };
        }

        /// <summary>Control callbacks are ignored while the window itself writes values into controls.</summary>
        private void Do(Action action)
        {
            if (!_syncing) action();
        }

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        private static void Set(TMP_Text t, string text)
        {
            if (t != null && t.text != text) t.text = text;
        }

        private static void Show(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        private sealed class SliderRow
        {
            public GameObject Root;
            public TMP_Text Label;
            public Slider Slider;
            public TMP_Text Value;

            public void Set(string label, float min, float max, float value, string shown)
            {
                SettingsWindow.Set(Label, label);
                if (!Mathf.Approximately(Slider.minValue, min)) Slider.minValue = min;
                if (!Mathf.Approximately(Slider.maxValue, max)) Slider.maxValue = max;
                if (!Mathf.Approximately(Slider.value, value)) Slider.SetValueWithoutNotify(value);
                SettingsWindow.Set(Value, shown);
            }
        }
    }
}
