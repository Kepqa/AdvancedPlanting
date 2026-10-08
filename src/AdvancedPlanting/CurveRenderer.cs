using System.Collections.Generic;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>Draws the guide path, rows and control point markers with pooled LineRenderers.</summary>
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

        private readonly ModConfig _config;
        private readonly List<LineRenderer> _lines = new List<LineRenderer>();
        private readonly List<LineRenderer> _markers = new List<LineRenderer>();
        private Material _material;
        private GameObject _root;

        public CurveRenderer(ModConfig config)
        {
            _config = config;
        }

        public void Apply(List<List<Vector3>> rowLines, List<Vector3> controlPoints)
        {
            if (!_config.ShowCurve.Value)
            {
                HideAll();
                return;
            }
            EnsureRoot();

            Color rowColor = _config.CurveColor.Value;
            float width = _config.CurveWidth.Value;

            for (int i = 0; i < rowLines.Count; i++)
            {
                LineRenderer lr = GetLine(_lines, i, "row");
                List<Vector3> pts = rowLines[i];
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());
                lr.startWidth = width;
                lr.endWidth = width;
                Color c = i == 0 ? rowColor : new Color(rowColor.r, rowColor.g, rowColor.b, rowColor.a * 0.6f);
                lr.startColor = c;
                lr.endColor = c;
                lr.enabled = true;
            }
            for (int i = rowLines.Count; i < _lines.Count; i++) _lines[i].enabled = false;

            Color markerColor = _config.ControlPointColor.Value;
            for (int i = 0; i < controlPoints.Count; i++)
            {
                LineRenderer lr = GetLine(_markers, i, "cp");
                Vector3 p = controlPoints[i];
                lr.positionCount = 2;
                lr.SetPosition(0, p + Vector3.up * 0.05f);
                lr.SetPosition(1, p + Vector3.up * 1.2f);
                lr.startWidth = width * 2.5f;
                lr.endWidth = width * 0.5f;
                lr.startColor = markerColor;
                lr.endColor = markerColor;
                lr.enabled = true;
            }
            for (int i = controlPoints.Count; i < _markers.Count; i++) _markers[i].enabled = false;
        }

        public void HideAll()
        {
            foreach (LineRenderer l in _lines) if (l != null) l.enabled = false;
            foreach (LineRenderer l in _markers) if (l != null) l.enabled = false;
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
            _lines.Clear();
            _markers.Clear();
        }

        private void EnsureRoot()
        {
            if (_root != null) return;
            _root = new GameObject("AdvancedPlanting_Curves");
            Object.DontDestroyOnLoad(_root);
            _lines.Clear();
            _markers.Clear();
        }

        private Material GetMaterial()
        {
            if (_material != null) return _material;
            foreach (string name in ShaderCandidates)
            {
                Shader s = Shader.Find(name);
                if (s == null) continue;
                _material = new Material(s);
                break;
            }
            return _material;
        }

        private LineRenderer GetLine(List<LineRenderer> pool, int index, string prefix)
        {
            while (pool.Count <= index)
            {
                var go = new GameObject(prefix + "_" + pool.Count);
                go.transform.SetParent(_root.transform, false);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.alignment = LineAlignment.View;
                lr.numCapVertices = 2;
                lr.numCornerVertices = 2;
                Material m = GetMaterial();
                if (m != null) lr.material = m;
                pool.Add(lr);
            }
            return pool[index];
        }
    }
}
