using System.Collections.Generic;
using AdvancedPlanting.Core;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Everything the player has set up for the current figure: control points, path mode,
    /// spacing, rows, jitter. Mutations set <see cref="Dirty"/> so the preview is rebuilt lazily.
    /// </summary>
    public sealed class FigureState
    {
        public readonly List<Vector3> ControlPoints = new List<Vector3>();
        public PathMode Mode = PathMode.Spline;
        public RowSide Side = RowSide.Both;
        public int Rows = 1;
        public float Jitter = 0f;
        public float Thinning = 0f;
        public uint Seed = 1;
        public bool Enabled = true;

        /// <summary>Spacing remembered per plant prefab so switching seeds keeps sensible values.</summary>
        private readonly Dictionary<string, float> _spacingByPrefab = new Dictionary<string, float>();

        public bool Dirty { get; private set; } = true;

        public void MarkDirty() => Dirty = true;
        public void ClearDirty() => Dirty = false;

        public bool HasEnoughPoints => ControlPoints.Count >= Mode.MinControlPoints();

        public void AddPoint(Vector3 p)
        {
            ControlPoints.Add(p);
            Dirty = true;
        }

        public void RemoveLastPoint()
        {
            if (ControlPoints.Count == 0) return;
            ControlPoints.RemoveAt(ControlPoints.Count - 1);
            Dirty = true;
        }

        public void Clear()
        {
            if (ControlPoints.Count == 0) return;
            ControlPoints.Clear();
            Dirty = true;
        }

        public void CycleMode()
        {
            Mode = Mode.Next();
            Dirty = true;
        }

        public void CycleSide()
        {
            Side = (RowSide)(((int)Side + 1) % 3);
            Dirty = true;
        }

        public void RerollSeed()
        {
            Seed = unchecked(Seed * 1664525u + 1013904223u);
            if (Seed == 0) Seed = 1;
            Dirty = true;
        }

        public void AdjustRows(int delta)
        {
            int next = Mathf.Clamp(Rows + delta, Mode == PathMode.Contour ? 0 : 1, 50);
            if (next == Rows) return;
            Rows = next;
            Dirty = true;
        }

        public void AdjustJitter(float delta)
        {
            float next = Mathf.Clamp01(Mathf.Round((Jitter + delta) * 100f) / 100f);
            if (Mathf.Approximately(next, Jitter)) return;
            Jitter = next;
            Dirty = true;
        }

        public void AdjustThinning(float delta)
        {
            float next = Mathf.Clamp(Mathf.Round((Thinning + delta) * 100f) / 100f, 0f, 0.9f);
            if (Mathf.Approximately(next, Thinning)) return;
            Thinning = next;
            Dirty = true;
        }

        public float GetSpacing(string prefabName, float fallback)
        {
            if (string.IsNullOrEmpty(prefabName)) return fallback;
            if (_spacingByPrefab.TryGetValue(prefabName, out float s)) return s;
            _spacingByPrefab[prefabName] = fallback;
            return fallback;
        }

        public void AdjustSpacing(string prefabName, float fallback, float delta, float minimum)
        {
            float current = GetSpacing(prefabName, fallback);
            float next = Mathf.Max(minimum, Mathf.Round((current + delta) * 100f) / 100f);
            if (Mathf.Approximately(next, current)) return;
            _spacingByPrefab[prefabName] = next;
            Dirty = true;
        }
    }
}
