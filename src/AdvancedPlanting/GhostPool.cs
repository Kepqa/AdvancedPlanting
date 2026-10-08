using System.Collections.Generic;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Pool of preview ghosts, cloned from the player's own placement ghost so they carry the
    /// vanilla ghost materials and stripped components. One ghost per plant slot.
    /// </summary>
    public sealed class GhostPool
    {
        private const string GhostTag = "AdvancedPlantingGhost";

        private readonly List<GameObject> _ghosts = new List<GameObject>();
        private string _sourceName;

        public static bool IsGhost(GameObject go)
        {
            if (go == null) return false;
            Transform t = go.transform;
            while (t != null)
            {
                if (t.name.StartsWith(GhostTag)) return true;
                t = t.parent;
            }
            return false;
        }

        public void Apply(GameObject source, List<PlantSlot> slots)
        {
            if (source == null)
            {
                HideAll();
                return;
            }

            if (_sourceName != source.name)
            {
                DestroyAll();
                _sourceName = source.name;
            }

            while (_ghosts.Count < slots.Count)
            {
                GameObject ghost = Clone(source);
                if (ghost == null) break;
                _ghosts.Add(ghost);
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
                Piece piece = ghost.GetComponent<Piece>();
                if (piece != null) piece.SetInvalidPlacementHeightlight(!slot.IsValid);
            }
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
            _sourceName = null;
        }

        private static GameObject Clone(GameObject source)
        {
            // The vanilla ghost was instantiated with ZNetView initialisation disabled; cloning it
            // must use the same guard or the copy would register a networked object.
            bool previous = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            GameObject clone;
            try
            {
                clone = Object.Instantiate(source);
            }
            finally
            {
                ZNetView.m_forceDisableInit = previous;
            }
            if (clone == null) return null;

            clone.name = GhostTag + "_" + source.name;
            foreach (Collider c in clone.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            clone.SetActive(false);
            return clone;
        }
    }
}
