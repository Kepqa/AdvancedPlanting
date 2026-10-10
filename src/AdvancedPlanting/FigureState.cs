using System.Collections.Generic;
using AdvancedPlanting.Core;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Everything the player has set up for the current figure.
    ///
    /// Lifecycle: while <see cref="Finished"/> is false the player is drawing and the cursor acts
    /// as the next control point (rubber band). Fixed-size paths (line, arc, circle) finish by
    /// themselves when their last point is placed; a curve finishes by clicking its last point
    /// again, a shape by clicking its first point. Once finished, a click plants.
    /// Plants that were placed are remembered so a figure can be completed over several clicks.
    /// </summary>
    public sealed class FigureState
    {
        public readonly List<Vector3> ControlPoints = new List<Vector3>();
        public readonly List<Vector3> Planted = new List<Vector3>();

        public PathMode Mode = PathMode.Spline;
        public RowSide Side = RowSide.Both;
        public int Rows = 1;
        public float Jitter = 0f;
        public float Thinning = 0f;
        public uint Seed = 1;

        /// <summary>Fill closed paths (circle, shape) with concentric rows to the centre.</summary>
        public bool Fill = false;

        public bool IsClosedPath => Mode == PathMode.Circle || Mode == PathMode.Contour;

        public void SetFill(bool fill)
        {
            if (fill == Fill) return;
            Fill = fill;
            Dirty = true;
        }

        /// <summary>Figure mode. When false the cultivator behaves exactly like vanilla.</summary>
        public bool Enabled = true;

        /// <summary>Keep planting the rest of the figure on its own as stamina and seeds allow.</summary>
        public bool AutoContinue = false;

        public bool Finished { get; private set; }

        private readonly Dictionary<string, float> _spacingByPrefab = new Dictionary<string, float>();

        public bool Dirty { get; private set; } = true;
        public void MarkDirty() => Dirty = true;
        public void ClearDirty() => Dirty = false;

        public bool IsEmpty => ControlPoints.Count == 0;
        public bool HasEnoughPoints => ControlPoints.Count >= Mode.MinControlPoints();
        public bool IsFixedSize => Mode.MaxControlPoints() > 0;

        /// <summary>A curve or shape with enough points that can be finished manually.</summary>
        public bool CanFinish => !Finished && !IsFixedSize && HasEnoughPoints;

        public void AddPoint(Vector3 p)
        {
            if (Finished) return;
            ControlPoints.Add(p);
            int max = Mode.MaxControlPoints();
            if (max > 0 && ControlPoints.Count >= max) Finished = true;
            Dirty = true;
        }

        public void Finish()
        {
            if (!CanFinish) return;
            Finished = true;
            Dirty = true;
        }

        public void RemoveLastPoint()
        {
            if (ControlPoints.Count == 0) return;
            ControlPoints.RemoveAt(ControlPoints.Count - 1);
            Finished = false;
            Dirty = true;
        }

        public void Clear()
        {
            ControlPoints.Clear();
            Planted.Clear();
            Finished = false;
            Dirty = true;
        }

        public void SetMode(PathMode mode)
        {
            if (mode == Mode) return;
            Mode = mode;
            int max = mode.MaxControlPoints();
            if (max > 0 && ControlPoints.Count > max) ControlPoints.RemoveRange(max, ControlPoints.Count - max);
            Finished = max > 0 && ControlPoints.Count >= max;
            Dirty = true;
        }

        public void SetSide(RowSide side)
        {
            if (side == Side) return;
            Side = side;
            Dirty = true;
        }

        public void CycleSide() => SetSide((RowSide)(((int)Side + 1) % 3));

        public void RerollSeed()
        {
            Seed = unchecked(Seed * 1664525u + 1013904223u);
            if (Seed == 0) Seed = 1;
            Dirty = true;
        }

        public void SetRows(int rows)
        {
            rows = Mathf.Clamp(rows, 1, 50);
            if (rows == Rows) return;
            Rows = rows;
            Dirty = true;
        }

        public void SetJitter(float value)
        {
            value = Mathf.Clamp01(Mathf.Round(value * 100f) / 100f);
            if (Mathf.Approximately(value, Jitter)) return;
            Jitter = value;
            Dirty = true;
        }

        public void SetThinning(float value)
        {
            value = Mathf.Clamp(Mathf.Round(value * 100f) / 100f, 0f, 0.9f);
            if (Mathf.Approximately(value, Thinning)) return;
            Thinning = value;
            Dirty = true;
        }

        public float GetSpacing(string prefabName, float fallback)
        {
            if (string.IsNullOrEmpty(prefabName)) return fallback;
            if (_spacingByPrefab.TryGetValue(prefabName, out float s)) return s;
            _spacingByPrefab[prefabName] = fallback;
            return fallback;
        }

        public void SetSpacing(string prefabName, float value, float minimum)
        {
            if (string.IsNullOrEmpty(prefabName)) return;
            value = Mathf.Max(minimum, Mathf.Round(value * 20f) / 20f);
            if (_spacingByPrefab.TryGetValue(prefabName, out float current) && Mathf.Approximately(current, value)) return;
            _spacingByPrefab[prefabName] = value;
            Dirty = true;
        }

        public void MarkPlanted(Vector3 p) => Planted.Add(p);

        public bool IsPlanted(Vector3 p, float tolerance)
        {
            float t2 = tolerance * tolerance;
            for (int i = 0; i < Planted.Count; i++)
            {
                float dx = Planted[i].x - p.x;
                float dz = Planted[i].z - p.z;
                if (dx * dx + dz * dz <= t2) return true;
            }
            return false;
        }
    }
}
