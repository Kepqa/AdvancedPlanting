using System.Collections.Generic;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Pool of preview ghosts, cloned from the player's own placement ghost so they look exactly
    /// like it (same model, ghost layer, "_GhostOnly" parts).
    ///
    /// Clones are instantiated under an inactive parent, so no Awake runs, and every script is
    /// stripped before they are shown. Without that, each clone's Piece.Awake would register in
    /// the game's global piece list and its ZNetView would try to create a networked object.
    /// What remains is transforms, meshes and renderers.
    /// </summary>
    public sealed class GhostPool
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private readonly List<GameObject> _ghosts = new List<GameObject>();
        private readonly List<bool> _invalidState = new List<bool>();
        private string _sourceName;
        private GameObject _activeRoot;
        private GameObject _staging;

        /// <summary>Clones are expensive (instantiate + strip); spread pool growth over frames.</summary>
        private const int MaxClonesPerCall = 24;

        /// <summary>Places ghosts on the slots. Returns false while the pool is still growing.</summary>
        public bool Apply(GameObject source, List<PlantSlot> slots)
        {
            if (source == null)
            {
                HideAll();
                return true;
            }
            EnsureRoots();

            if (_sourceName != source.name)
            {
                DestroyAll();
                EnsureRoots();
                _sourceName = source.name;
            }

            int budget = MaxClonesPerCall;
            while (_ghosts.Count < slots.Count && budget-- > 0)
            {
                GameObject ghost = Clone(source);
                if (ghost == null) break;
                _ghosts.Add(ghost);
                _invalidState.Add(false);
            }

            for (int i = 0; i < _ghosts.Count; i++)
            {
                GameObject ghost = _ghosts[i];
                if (ghost == null) continue;
                if (i >= slots.Count)
                {
                    if (ghost.activeSelf) ghost.SetActive(false);
                    continue;
                }
                PlantSlot slot = slots[i];
                ghost.transform.position = slot.Position;
                ghost.transform.rotation = slot.Rotation;
                if (!ghost.activeSelf) ghost.SetActive(true);
                SetInvalid(i, slot.IsBlocked);
            }
            return _ghosts.Count >= slots.Count;
        }

        public void HideAll()
        {
            foreach (GameObject g in _ghosts)
            {
                if (g != null && g.activeSelf) g.SetActive(false);
            }
        }

        public void DestroyAll()
        {
            foreach (GameObject g in _ghosts)
            {
                if (g != null) Object.Destroy(g);
            }
            _ghosts.Clear();
            _invalidState.Clear();
            _sourceName = null;
        }

        public void DestroyRoots()
        {
            DestroyAll();
            if (_activeRoot != null) Object.Destroy(_activeRoot);
            if (_staging != null) Object.Destroy(_staging);
            _activeRoot = null;
            _staging = null;
        }

        private void EnsureRoots()
        {
            if (_activeRoot == null)
            {
                _activeRoot = new GameObject("AdvancedPlanting_Ghosts");
                Object.DontDestroyOnLoad(_activeRoot);
            }
            if (_staging == null)
            {
                _staging = new GameObject("AdvancedPlanting_GhostStaging");
                _staging.SetActive(false);
                Object.DontDestroyOnLoad(_staging);
            }
        }

        private void SetInvalid(int index, bool invalid)
        {
            if (_invalidState[index] == invalid) return;
            _invalidState[index] = invalid;
            MaterialMan mm = MaterialMan.instance;
            if (mm == null) return;
            GameObject go = _ghosts[index];
            // Same calls as Piece.SetInvalidPlacementHeightlight, without needing a Piece component.
            if (invalid)
            {
                mm.SetValue(go, ColorId, Color.red);
                mm.SetValue(go, EmissionId, Color.red * 0.7f);
            }
            else
            {
                mm.ResetValue(go, ColorId);
                mm.ResetValue(go, EmissionId);
            }
        }

        private GameObject Clone(GameObject source)
        {
            GameObject clone = Object.Instantiate(source, _staging.transform, false);
            if (clone == null) return null;

            StripScripts(clone);
            foreach (Collider c in clone.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            clone.name = "AdvancedPlantingGhost_" + source.name;
            clone.SetActive(false);
            clone.transform.SetParent(_activeRoot.transform, false);
            return clone;
        }

        /// <summary>
        /// Destroys every MonoBehaviour. Several passes because Unity refuses to destroy a
        /// component another one still declares as required.
        /// </summary>
        private static void StripScripts(GameObject go)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                MonoBehaviour[] scripts = go.GetComponentsInChildren<MonoBehaviour>(true);
                if (scripts.Length == 0) return;
                for (int i = scripts.Length - 1; i >= 0; i--)
                {
                    if (scripts[i] != null) Object.DestroyImmediate(scripts[i]);
                }
            }
        }
    }
}
