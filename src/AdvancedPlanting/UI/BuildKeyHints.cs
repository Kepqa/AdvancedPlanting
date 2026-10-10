using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Shows the figure hints in the style of the vanilla build key hints (bottom right while
    /// building), stacked directly above them.
    ///
    /// The lines copy the style of a vanilla hint text, so font, size and colour match. They live
    /// in our own container instead of inside the vanilla layout, and the container is moved
    /// every frame to sit on top of whatever vanilla hint texts are visible. That works no matter
    /// how the vanilla hint block is built internally.
    /// </summary>
    internal sealed class BuildKeyHints
    {
        private const int MaxRows = 6;
        private const float Gap = 6f;

        private readonly List<TMP_Text> _lines = new List<TMP_Text>();
        private readonly string[] _lastText = new string[MaxRows];
        private readonly List<TMP_Text> _vanillaTexts = new List<TMP_Text>();
        private readonly Vector3[] _corners = new Vector3[4];
        private KeyHints _owner;
        private RectTransform _container;
        private bool _failed;
        private bool _loggedLayout;
        private float _nextText;

        public bool Available => _container != null && !_failed;

        /// <summary>Called after KeyHints.UpdateHints.</summary>
        public void Update(KeyHints hints, Plugin plugin)
        {
            if (hints == null) return;
            if (_owner != hints)
            {
                _owner = hints;
                _container = null;
                _lines.Clear();
                _failed = false;
            }

            bool show = hints.m_keyHintsEnabled && hints.m_buildHints != null && hints.m_buildHints.activeInHierarchy
                && plugin.ShowsFigureHints && !global::Hud.IsPieceSelectionVisible();
            if (!show)
            {
                if (_container != null && _container.gameObject.activeSelf) _container.gameObject.SetActive(false);
                return;
            }
            if (_container == null && (_failed || !Create(hints))) return;
            if (!_container.gameObject.activeSelf) _container.gameObject.SetActive(true);

            if (Time.unscaledTime >= _nextText)
            {
                // Texts change rarely; rebuilding them ten times a second avoids per-frame garbage.
                _nextText = Time.unscaledTime + 0.1f;
                int count = 0;
                foreach (KeyValuePair<string, string> hint in plugin.CurrentHints())
                {
                    if (count >= _lines.Count) break;
                    SetLine(count++, hint.Key, hint.Value);
                }
                for (int i = 0; i < _lines.Count; i++)
                {
                    bool on = i < count;
                    if (_lines[i].gameObject.activeSelf != on) _lines[i].gameObject.SetActive(on);
                }
            }
            PlaceAboveVanilla(hints);
        }

        private bool Create(KeyHints hints)
        {
            TMP_Text template = hints.m_buildRotateKey;
            if (template == null)
            {
                _failed = true;
                Plugin.Log.LogWarning("Key hints: no vanilla template text; hints stay in the status panel.");
                return false;
            }
            LogLayoutOnce(hints);

            Transform parent = hints.m_buildHints.transform;
            var go = new GameObject("AdvancedPlanting_KeyHints", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().ignoreLayout = true; // never disturb the vanilla layout
            _container = (RectTransform)go.transform;
            _container.anchorMin = _container.anchorMax = new Vector2(0.5f, 0.5f);
            _container.pivot = new Vector2(1f, 0f);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerRight;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.spacing = 2f;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < MaxRows; i++)
            {
                var lineGo = new GameObject("Hint" + i, typeof(RectTransform));
                lineGo.transform.SetParent(_container, false);
                var t = lineGo.AddComponent<TextMeshProUGUI>();
                t.font = template.font;
                t.fontSharedMaterial = template.fontSharedMaterial;
                t.fontSize = template.fontSize;
                t.fontStyle = template.fontStyle;
                t.color = template.color;
                t.alignment = TextAlignmentOptions.Right;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.raycastTarget = false;
                t.richText = true;
                lineGo.SetActive(false);
                _lines.Add(t);
            }
            return true;
        }

        /// <summary>Puts our block right above the highest visible vanilla hint text.</summary>
        private void PlaceAboveVanilla(KeyHints hints)
        {
            var parent = (RectTransform)_container.parent;
            float top = float.MinValue, right = float.MinValue;
            _vanillaTexts.Clear();
            hints.m_buildHints.GetComponentsInChildren(false, _vanillaTexts);
            foreach (TMP_Text t in _vanillaTexts)
            {
                if (t.transform.IsChildOf(_container) || string.IsNullOrEmpty(t.text)) continue;
                t.rectTransform.GetWorldCorners(_corners);
                for (int k = 0; k < 4; k++)
                {
                    Vector3 local = parent.InverseTransformPoint(_corners[k]);
                    if (local.y > top) top = local.y;
                    if (local.x > right) right = local.x;
                }
            }
            if (top == float.MinValue)
            {
                // No vanilla text visible: use the bottom-right of the hint area.
                Rect r = parent.rect;
                top = r.yMin;
                right = r.xMax;
            }
            // Anchors are at the parent's centre, so convert from parent-local to anchored space.
            _container.anchoredPosition = new Vector2(right, top + Gap) - parent.rect.center;
        }

        private void SetLine(int index, string action, string key)
        {
            string text = action + " <mspace=0.6em> " + key + "</mspace>";
            if (_lastText[index] == text) return;
            _lastText[index] = text;
            _lines[index].text = text;
        }

        private void LogLayoutOnce(KeyHints hints)
        {
            if (_loggedLayout) return;
            _loggedLayout = true;
            var sb = new StringBuilder("Key hint layout:");
            Dump(hints.m_buildHints.transform, 0, sb);
            Plugin.Log.LogInfo(sb.ToString());
        }

        private static void Dump(Transform t, int depth, StringBuilder sb)
        {
            if (depth > 4) return;
            sb.Append('\n').Append(' ', depth * 2).Append(t.name);
            if (t.GetComponent<LayoutGroup>() != null) sb.Append(" [layout]");
            var text = t.GetComponent<TMP_Text>();
            if (text != null) sb.Append(" \"").Append(text.text).Append('"');
            foreach (Transform c in t) Dump(c, depth + 1, sb);
        }
    }
}
