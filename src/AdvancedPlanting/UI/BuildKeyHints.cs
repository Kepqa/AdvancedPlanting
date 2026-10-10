using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Adds our rows to the vanilla build key hints (bottom right while building), in the same
    /// "action  key" format, by cloning one of the vanilla rows. Rows only show in figure mode.
    /// </summary>
    internal sealed class BuildKeyHints
    {
        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly List<TMP_Text> _texts = new List<TMP_Text>();
        private readonly string[] _lastText = new string[3];
        private KeyHints _owner;

        /// <summary>Called after KeyHints.UpdateHints. Shows or hides our rows.</summary>
        public void Update(KeyHints hints, Plugin plugin)
        {
            if (hints == null) return;
            if (_owner != hints)
            {
                _rows.Clear();
                _texts.Clear();
                _owner = hints;
            }

            bool show = hints.m_keyHintsEnabled && hints.m_buildHints != null && hints.m_buildHints.activeSelf
                && plugin.ShowsFigureHints && !global::Hud.IsPieceSelectionVisible();
            if (!show)
            {
                SetVisible(0);
                return;
            }
            if (_rows.Count == 0 && !CreateRows(hints)) return;

            string click = GameText.Localize("$KEY_Attack");
            int count = 0;
            SetRow(count++, plugin.NextActionHint(), click);
            string finish = plugin.FinishHint();
            if (finish != null) SetRow(count++, finish, click);
            SetRow(count++, Loc.T("hint.wheel"), plugin.WheelKeyName);
            SetVisible(count);
        }

        public bool Available => _rows.Count > 0;

        private bool CreateRows(KeyHints hints)
        {
            TMP_Text template = hints.m_buildRotateKey;
            Transform container = hints.m_buildHints.transform;
            if (template == null) return false;

            // The row is the closest ancestor of the template whose parent lays out its children
            // (a layout group), still inside the build hints. Cloning that keeps the vanilla look
            // without duplicating a whole keyboard or gamepad group.
            Transform row = null;
            for (Transform t = template.transform; t != null && t != container; t = t.parent)
            {
                if (t.parent != null && t.parent.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
                {
                    row = t;
                    break;
                }
            }
            if (row == null || !row.IsChildOf(container))
            {
                Plugin.Log.LogWarning("Build key hints have an unexpected layout; using the status panel instead.");
                return false;
            }
            Transform parentGroup = row.parent;

            string path = RelativePath(row, template.transform);
            for (int i = 0; i < 3; i++)
            {
                GameObject clone = Object.Instantiate(row.gameObject, parentGroup, false);
                clone.name = "AdvancedPlanting_Hint" + i;
                clone.transform.SetSiblingIndex(i);
                Transform textTransform = path.Length == 0 ? clone.transform : clone.transform.Find(path);
                TMP_Text text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : clone.GetComponentInChildren<TMP_Text>(true);
                if (text == null)
                {
                    Object.Destroy(clone);
                    continue;
                }
                // The game re-localizes cached texts on language change, which would put the
                // template's vanilla text back. Our rows are written by us every frame instead.
                Localization.instance?.RemoveTextFromCache(text);
                _rows.Add(clone);
                _texts.Add(text);
            }
            return _rows.Count > 0;
        }

        private void SetRow(int index, string action, string key)
        {
            if (index >= _texts.Count) return;
            string text = action + " <mspace=0.6em> " + key + "</mspace>";
            if (_lastText[index] == text) return;
            _lastText[index] = text;
            _texts[index].text = text;
        }

        private void SetVisible(int count)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] == null) continue;
                bool on = i < count;
                if (_rows[i].activeSelf != on) _rows[i].SetActive(on);
            }
        }

        private static string RelativePath(Transform root, Transform child)
        {
            var parts = new List<string>();
            for (Transform t = child; t != null && t != root; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
