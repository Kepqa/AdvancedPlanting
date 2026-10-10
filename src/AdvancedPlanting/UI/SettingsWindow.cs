using System;
using System.Collections.Generic;
using AdvancedPlanting.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Planting settings window built from the game's own UI pieces, so it looks and scales like
    /// any Valheim window: the panel background, craft button, PvP toggle and the stack-split
    /// slider of the inventory screen are cloned as templates and arranged with layout groups.
    ///
    /// While open the game treats it like the build menu (see UiPatches): cursor free, character
    /// and camera ignore input, Esc closes the window instead of opening the game menu.
    /// </summary>
    public sealed class SettingsWindow
    {
        private static readonly Color Highlight = new Color(1f, 0.71f, 0.32f);
        private static readonly Color Normal = new Color(0.93f, 0.9f, 0.82f);
        private static readonly Color Muted = new Color(0.72f, 0.69f, 0.62f);

        private const float Width = 640f;

        private readonly Plugin _plugin;
        private GameObject _root;
        private bool _open;
        private int _closedFrame = -10;
        private bool _syncing;

        private TMP_Text _title;
        private readonly Button[] _modeButtons = new Button[5];
        private readonly TMP_Text[] _modeLabels = new TMP_Text[5];
        private GameObject _fillRow;
        private Toggle _fill;
        private TMP_Text _fillLabel;
        private GameObject _sideHeader, _sideRow;
        private TMP_Text _pathHeader, _sideHeaderText;
        private readonly Button[] _sideButtons = new Button[3];
        private readonly TMP_Text[] _sideLabels = new TMP_Text[3];
        private SliderRow _spacing, _rows, _jitter, _thinning;
        private TMP_Text _noRoom;
        private TMP_Text _reshuffleLabel;
        private Toggle _auto;
        private TMP_Text _autoLabel, _autoDesc, _cost, _closeLabel;

        private TMP_Text _titleStyle, _bodyStyle;

        public SettingsWindow(Plugin plugin)
        {
            _plugin = plugin;
        }

        public bool IsOpen => _open;

        /// <summary>
        /// True while open and for one frame after closing, so the Esc that closed the window
        /// is not seen again by the game menu.
        /// </summary>
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
            if (_root == null || !_plugin.HasPlantContext)
            {
                Close();
                return;
            }
            if (ZInput.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }
            Sync();
        }

        // ------------------------------------------------------------------ sync

        /// <summary>Pushes the current state into the controls without triggering their events.</summary>
        private void Sync()
        {
            FigureState s = _plugin.State;
            _syncing = true;
            try
            {
                SetText(_title, Loc.T("window.title", _plugin.PlantDisplayName));
                SetText(_pathHeader, Loc.T("window.path"));
                PathMode[] modes = { PathMode.Segment, PathMode.Arc, PathMode.Circle, PathMode.Spline, PathMode.Contour };
                for (int i = 0; i < modes.Length; i++)
                {
                    SetText(_modeLabels[i], StatusPanel.ModeName(modes[i]));
                    SetColor(_modeLabels[i], s.Enabled && s.Mode == modes[i] ? Highlight : Normal);
                }

                bool closed = s.IsClosedPath;
                SetActive(_fillRow, closed);
                _fill.SetIsOnWithoutNotify(s.Fill);
                SetText(_fillLabel, Loc.T("window.fill"));

                bool sides = !(closed && s.Fill);
                SetActive(_sideHeader, sides);
                SetActive(_sideRow, sides);
                SetText(_sideHeaderText, Loc.T("window.rows_go"));
                RowSide[] order = { RowSide.Both, RowSide.Left, RowSide.Right };
                for (int i = 0; i < order.Length; i++)
                {
                    SetText(_sideLabels[i], StatusPanel.SideName(order[i], closed));
                    SetColor(_sideLabels[i], s.Side == order[i] ? Highlight : Normal);
                }

                float min = _plugin.MinSpacing;
                float spacing = _plugin.CurrentSpacing;
                _spacing.Set(Loc.T("window.spacing"), min, Mathf.Max(min * 4f, 6f), spacing, Loc.T("window.unit.m", spacing.ToString("0.00")));
                SetActive(_rows.Root, sides);
                _rows.Set(Loc.T("window.rows"), 1, 20, s.Rows, s.Rows.ToString());
                _jitter.Set(Loc.T("window.jitter"), 0f, 1f, s.Jitter, s.Jitter.ToString("0.00"));
                _thinning.Set(Loc.T("window.thinning"), 0f, 0.9f, s.Thinning, Mathf.RoundToInt(s.Thinning * 100f) + " %");

                bool noRoom = s.Jitter > 0f && _plugin.JitterRoom <= 0.01f;
                SetActive(_noRoom.gameObject, noRoom);
                SetText(_noRoom, Loc.T("window.no_room"));

                SetText(_reshuffleLabel, Loc.T("window.reshuffle"));
                _auto.SetIsOnWithoutNotify(s.AutoContinue);
                SetText(_autoLabel, Loc.T("window.auto"));
                SetText(_autoDesc, Loc.T("window.auto.desc"));
                SetText(_cost, _plugin.CostSummary());
                SetText(_closeLabel, Loc.T("window.close"));
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
                InventoryGui inv = InventoryGui.instance;
                if (inv == null) throw new InvalidOperationException("InventoryGui not ready");
                Canvas canvas = inv.GetComponentInParent<Canvas>();
                if (canvas == null) throw new InvalidOperationException("no UI canvas");

                _titleStyle = inv.m_recipeName;
                _bodyStyle = inv.m_recipeDecription != null ? inv.m_recipeDecription : inv.m_recipeName;
                Button buttonTemplate = inv.m_craftButton;
                Toggle toggleTemplate = inv.m_pvp;
                Slider sliderTemplate = inv.m_splitDialog != null ? inv.m_splitDialog.m_splitSlider : null;
                if (buttonTemplate == null || toggleTemplate == null || sliderTemplate == null)
                    throw new InvalidOperationException("UI templates missing");

                _root = new GameObject("AdvancedPlanting_Settings", typeof(RectTransform));
                _root.transform.SetParent(canvas.rootCanvas.transform, false);
                var rt = (RectTransform)_root.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(Width, 600f);

                ApplyPanelBackground(_root, inv);
                var layout = _root.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(34, 34, 26, 26);
                layout.spacing = 8f;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                var fitter = _root.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                _title = Text(_root.transform, _titleStyle, 1.15f, Highlight, TextAlignmentOptions.Center);
                Space(_root.transform, 4f);

                _pathHeader = Text(_root.transform, _bodyStyle, 0.95f, Muted, TextAlignmentOptions.Left);
                Transform modeRow = Row(_root.transform, 6f).transform;
                PathMode[] modes = { PathMode.Segment, PathMode.Arc, PathMode.Circle, PathMode.Spline, PathMode.Contour };
                for (int i = 0; i < modes.Length; i++)
                {
                    PathMode mode = modes[i];
                    _modeButtons[i] = MakeButton(modeRow, buttonTemplate, out _modeLabels[i], () =>
                    {
                        _plugin.SetFigureMode(true);
                        _plugin.State.SetMode(mode);
                    });
                }

                _fillRow = Row(_root.transform, 6f);
                _fill = MakeToggle(_fillRow.transform, toggleTemplate, out _fillLabel, on => _plugin.State.SetFill(on));

                _sideHeader = Text(_root.transform, _bodyStyle, 0.95f, Muted, TextAlignmentOptions.Left).gameObject;
                _sideHeaderText = _sideHeader.GetComponent<TMP_Text>();
                _sideRow = Row(_root.transform, 6f);
                RowSide[] order = { RowSide.Both, RowSide.Left, RowSide.Right };
                for (int i = 0; i < order.Length; i++)
                {
                    RowSide side = order[i];
                    _sideButtons[i] = MakeButton(_sideRow.transform, buttonTemplate, out _sideLabels[i], () => _plugin.State.SetSide(side));
                }

                Space(_root.transform, 4f);
                _spacing = MakeSlider(sliderTemplate, v => _plugin.SetSpacing(v));
                _rows = MakeSlider(sliderTemplate, v => _plugin.State.SetRows(Mathf.RoundToInt(v)), wholeNumbers: true);
                _jitter = MakeSlider(sliderTemplate, v => _plugin.State.SetJitter(v));
                _noRoom = Text(_root.transform, _bodyStyle, 0.8f, Muted, TextAlignmentOptions.Left);
                _thinning = MakeSlider(sliderTemplate, v => _plugin.State.SetThinning(v));

                Space(_root.transform, 4f);
                Transform actionRow = Row(_root.transform, 12f).transform;
                MakeButton(actionRow, buttonTemplate, out _reshuffleLabel, () => _plugin.State.RerollSeed());
                _auto = MakeToggle(actionRow, toggleTemplate, out _autoLabel, on => _plugin.State.AutoContinue = on);
                _autoDesc = Text(_root.transform, _bodyStyle, 0.8f, Muted, TextAlignmentOptions.Left);

                Space(_root.transform, 4f);
                _cost = Text(_root.transform, _bodyStyle, 0.9f, Normal, TextAlignmentOptions.Left);

                Transform bottom = Row(_root.transform, 0f).transform;
                var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
                spacer.transform.SetParent(bottom, false);
                spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;
                Button close = MakeButton(bottom, buttonTemplate, out _closeLabel, Close);
                close.GetComponent<LayoutElement>().flexibleWidth = 0f;
                close.GetComponent<LayoutElement>().preferredWidth = 180f;

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

        /// <summary>Uses the wooden panel sprite of the inventory screen as window background.</summary>
        private static void ApplyPanelBackground(GameObject target, InventoryGui inv)
        {
            Image best = null;
            float bestArea = 0f;
            foreach (RectTransform area in new[] { inv.m_crafting, inv.m_player, inv.m_info })
            {
                if (area == null) continue;
                foreach (Image img in area.GetComponentsInChildren<Image>(true))
                {
                    if (img.sprite == null || img.sprite.border == Vector4.zero) continue;
                    Rect r = img.rectTransform.rect;
                    float a = r.width * r.height;
                    if (a > bestArea)
                    {
                        bestArea = a;
                        best = img;
                    }
                }
            }
            var bg = target.AddComponent<Image>();
            if (best != null)
            {
                bg.sprite = best.sprite;
                bg.type = Image.Type.Sliced;
                bg.color = best.color;
                bg.material = best.material;
                bg.pixelsPerUnitMultiplier = best.pixelsPerUnitMultiplier;
                Plugin.Log.LogDebug("Settings window background: " + best.sprite.name);
            }
            else
            {
                bg.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);
            }
        }

        private GameObject Row(Transform parent, float spacing)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            return go;
        }

        private static void Space(Transform parent, float height)
        {
            var go = new GameObject("Space", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = height;
        }

        private static TMP_Text Text(Transform parent, TMP_Text style, float scale, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (style != null)
            {
                t.font = style.font;
                t.fontSharedMaterial = style.fontSharedMaterial;
                t.fontSize = Mathf.Max(14f, style.fontSize * scale);
            }
            else
            {
                t.fontSize = 18f * scale;
            }
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        private Button MakeButton(Transform parent, Button template, out TMP_Text label, Action onClick)
        {
            GameObject go = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            go.name = "Button";
            go.SetActive(true);
            StripGamepadHints(go);
            Button b = go.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(() =>
            {
                if (!_syncing) onClick();
            });
            b.interactable = true;
            label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null) Localization.instance?.RemoveTextFromCache(label);
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = 38f;
            le.preferredHeight = 42f;
            le.flexibleWidth = 1f;
            return b;
        }

        private Toggle MakeToggle(Transform parent, Toggle template, out TMP_Text label, Action<bool> onChange)
        {
            GameObject go = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            go.name = "Toggle";
            go.SetActive(true);
            StripGamepadHints(go);
            Toggle t = go.GetComponent<Toggle>();
            t.group = null;
            t.interactable = true;
            t.onValueChanged = new Toggle.ToggleEvent();
            t.onValueChanged.AddListener(on =>
            {
                if (!_syncing) onChange(on);
            });
            label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                Localization.instance?.RemoveTextFromCache(label);
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = 34f;
            le.flexibleWidth = 1f;
            return t;
        }

        private SliderRow MakeSlider(Slider template, Action<float> onChange, bool wholeNumbers = false)
        {
            GameObject row = Row(_root.transform, 12f);
            var label = Text(row.transform, _bodyStyle, 1f, Normal, TextAlignmentOptions.Left);
            var labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 150f;
            labelLe.flexibleWidth = 0f;

            GameObject go = UnityEngine.Object.Instantiate(template.gameObject, row.transform, false);
            go.name = "Slider";
            go.SetActive(true);
            StripGamepadHints(go);
            Slider s = go.GetComponent<Slider>();
            s.onValueChanged = new Slider.SliderEvent();
            s.wholeNumbers = wholeNumbers;
            s.interactable = true;
            s.onValueChanged.AddListener(v =>
            {
                if (!_syncing) onChange(v);
            });
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minHeight = 28f;
            le.preferredHeight = Mathf.Max(28f, ((RectTransform)template.transform).rect.height);

            var value = Text(row.transform, _bodyStyle, 1f, Highlight, TextAlignmentOptions.Right);
            var valueLe = value.gameObject.AddComponent<LayoutElement>();
            valueLe.preferredWidth = 90f;
            valueLe.flexibleWidth = 0f;
            return new SliderRow { Root = row, Label = label, Slider = s, Value = value };
        }

        /// <summary>Templates may carry gamepad button bindings; clones must not react to them.</summary>
        private static void StripGamepadHints(GameObject go)
        {
            foreach (UIGamePad pad in go.GetComponentsInChildren<UIGamePad>(true))
            {
                // Only a hint inside the clone is ours; one outside still belongs to the game.
                if (pad.m_hint != null && pad.m_hint.transform.IsChildOf(go.transform)) UnityEngine.Object.Destroy(pad.m_hint);
                UnityEngine.Object.Destroy(pad);
            }
        }

        private static void SetText(TMP_Text t, string text)
        {
            if (t != null && t.text != text) t.text = text;
        }

        private static void SetColor(TMP_Text t, Color c)
        {
            if (t != null && t.color != c) t.color = c;
        }

        private static void SetActive(GameObject go, bool on)
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
                SetText(Label, label);
                if (!Mathf.Approximately(Slider.minValue, min)) Slider.minValue = min;
                if (!Mathf.Approximately(Slider.maxValue, max)) Slider.maxValue = max;
                if (!Mathf.Approximately(Slider.value, value)) Slider.SetValueWithoutNotify(value);
                SetText(Value, shown);
            }
        }
    }
}
