using System.Collections.Generic;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// World-space overlay drawn with pooled LineRenderers: the guide path and rows, control point
    /// markers, and a ring per plant showing its grow radius, coloured by status.
    /// </summary>
    public sealed class CurveRenderer
    {
        private static readonly string[] ShaderCandidates =
        {
            "Sprites/Default",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Alpha Blended",
            "Hidden/Internal-Colored",
            "Standard",
        };

        private static readonly Color ValidRing = new Color(0.35f, 0.95f, 0.45f, 0.85f);
        private static readonly Color BlockedRing = new Color(1f, 0.3f, 0.25f, 0.9f);
        private static readonly Color SnapColor = new Color(0.4f, 0.85f, 1f, 1f);

        private const int RingSegments = 20;

        private readonly ModConfig _config;
        private readonly List<LineRenderer> _lines = new List<LineRenderer>();
        private readonly List<LineRenderer> _markers = new List<LineRenderer>();
        private readonly List<LineRenderer> _rings = new List<LineRenderer>();
        private readonly Vector3[] _ringBuffer = new Vector3[RingSegments];
        private Material _material;
        private GameObject _root;
        private bool _warnedShader;

        public CurveRenderer(ModConfig config)
        {
            _config = config;
        }

        /// <param name="snapIndex">Control point a click would snap to (finish/close), or -1.</param>
        public void Apply(List<List<Vector3>> rowLines, List<Vector3> controlPoints, int snapIndex, List<PlantSlot> slots, float growRadius)
        {
            EnsureRoot();
            float width = _config.CurveWidth.Value;

            int lineCount = _config.ShowCurve.Value ? rowLines.Count : 0;
            Color rowColor = _config.CurveColor.Value;
            for (int i = 0; i < lineCount; i++)
            {
                LineRenderer lr = Get(_lines, i, "row", loop: false);
                List<Vector3> pts = rowLines[i];
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());
                lr.startWidth = lr.endWidth = width;
                Color c = i == 0 ? rowColor : new Color(rowColor.r, rowColor.g, rowColor.b, rowColor.a * 0.55f);
                lr.startColor = lr.endColor = c;
                lr.enabled = true;
            }
            Disable(_lines, lineCount);

            Color markerColor = _config.ControlPointColor.Value;
            for (int i = 0; i < controlPoints.Count; i++)
            {
                LineRenderer lr = Get(_markers, i, "cp", loop: false);
                Vector3 p = controlPoints[i];
                bool snap = i == snapIndex;
                float height = snap ? 2.2f : (i == 0 ? 1.6f : 1.2f);
                lr.positionCount = 2;
                lr.SetPosition(0, p + Vector3.up * 0.05f);
                lr.SetPosition(1, p + Vector3.up * height);
                lr.startWidth = width * (snap ? 4f : 2.5f);
                lr.endWidth = width * 0.5f;
                lr.startColor = lr.endColor = snap ? SnapColor : markerColor;
                lr.enabled = true;
            }
            Disable(_markers, controlPoints.Count);

            int ringCount = 0;
            if (_config.ShowGrowRadius.Value && slots != null && growRadius > 0.05f)
            {
                foreach (PlantSlot slot in slots)
                {
                    if (slot.IsPlanted) continue;
                    LineRenderer lr = Get(_rings, ringCount++, "ring", loop: true);
                    // Ring lies in the ground plane at the slot, so it hugs slopes instead of
                    // floating on one side and sinking into the hill on the other.
                    Vector3 n = slot.Normal.sqrMagnitude > 0.01f ? slot.Normal.normalized : Vector3.up;
                    Vector3 t = Vector3.Cross(n, Vector3.forward);
                    if (t.sqrMagnitude < 0.01f) t = Vector3.Cross(n, Vector3.right);
                    t.Normalize();
                    Vector3 b = Vector3.Cross(n, t);
                    Vector3 centre = slot.Position + n * 0.1f;
                    for (int k = 0; k < RingSegments; k++)
                    {
                        float a = k * Mathf.PI * 2f / RingSegments;
                        _ringBuffer[k] = centre + (t * Mathf.Cos(a) + b * Mathf.Sin(a)) * growRadius;
                    }
                    lr.positionCount = RingSegments;
                    lr.SetPositions(_ringBuffer);
                    lr.startWidth = lr.endWidth = width * 0.6f;
                    lr.startColor = lr.endColor = slot.IsValid ? ValidRing : BlockedRing;
                    lr.enabled = true;
                }
            }
            Disable(_rings, ringCount);
        }

        public void HideAll()
        {
            Disable(_lines, 0);
            Disable(_markers, 0);
            Disable(_rings, 0);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
            _lines.Clear();
            _markers.Clear();
            _rings.Clear();
        }

        private static void Disable(List<LineRenderer> pool, int from)
        {
            for (int i = from; i < pool.Count; i++)
            {
                if (pool[i] != null && pool[i].enabled) pool[i].enabled = false;
            }
        }

        private void EnsureRoot()
        {
            if (_root != null) return;
            _root = new GameObject("AdvancedPlanting_Overlay");
            Object.DontDestroyOnLoad(_root);
            _lines.Clear();
            _markers.Clear();
            _rings.Clear();
        }

        private Material GetMaterial()
        {
            if (_material != null) return _material;
            foreach (string name in ShaderCandidates)
            {
                Shader s = Shader.Find(name);
                if (s == null) continue;
                _material = new Material(s);
                Plugin.Log.LogDebug("Overlay shader: " + name);
                return _material;
            }
            if (!_warnedShader)
            {
                _warnedShader = true;
                Plugin.Log.LogWarning("No overlay shader found; lines may render incorrectly.");
            }
            return null;
        }

        private LineRenderer Get(List<LineRenderer> pool, int index, string prefix, bool loop)
        {
            while (pool.Count <= index)
            {
                var go = new GameObject(prefix + "_" + pool.Count);
                go.transform.SetParent(_root.transform, false);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.loop = loop;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.alignment = LineAlignment.View;
                lr.numCapVertices = loop ? 0 : 2;
                lr.numCornerVertices = 2;
                Material m = GetMaterial();
                if (m != null) lr.material = m;
                pool.Add(lr);
            }
            return pool[index];
        }
    }
}
