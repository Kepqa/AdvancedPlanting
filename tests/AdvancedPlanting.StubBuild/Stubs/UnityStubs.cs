// Minimal stand-ins for the UnityEngine API surface used by the plugin. Compile-only.
#pragma warning disable IDE0060, CS1591
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
    }

    public struct Quaternion
    {
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => default;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
    }

    public struct LayerMask
    {
        public int value;
        public static implicit operator int(LayerMask m) => m.value;
    }

    public static class Mathf
    {
        public static float Abs(float v) => Math.Abs(v);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Round(float v) => (float)Math.Round(v);
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 1e-6f;
    }

    public static class Time
    {
        public static float time => 0f;
    }

    public class Object
    {
        public string name { get; set; }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => 0;
    }

    public class Component : Object
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public T GetComponent<T>() => default;
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) => new T[0];
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour { }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Transform parent { get; set; }
        public void SetParent(Transform parent, bool worldPositionStays) { }
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        public Transform transform { get; }
        public int layer { get; set; }
        public bool activeSelf { get; }
        public void SetActive(bool value) { }
        public T GetComponent<T>() => default;
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) => new T[0];
        public T AddComponent<T>() where T : Component, new() => new T();
    }

    public class Collider : Component
    {
        public bool enabled { get; set; }
    }

    public static class Physics
    {
        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask) => new Collider[0];
    }

    public class Shader : Object
    {
        public static Shader Find(string name) => null;
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
    }

    public class Texture2D : Object
    {
        public Texture2D(int w, int h) { }
        public void SetPixel(int x, int y, Color c) { }
        public void Apply() { }
    }

    public enum LineAlignment { View, TransformZ }

    namespace Rendering
    {
        public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    }

    public class Renderer : Component
    {
        public bool enabled { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
        public Material material { get; set; }
    }

    public class LineRenderer : Renderer
    {
        public int positionCount { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public bool useWorldSpace { get; set; }
        public LineAlignment alignment { get; set; }
        public int numCapVertices { get; set; }
        public int numCornerVertices { get; set; }
        public void SetPositions(Vector3[] positions) { }
        public void SetPosition(int index, Vector3 position) { }
    }

    public enum KeyCode
    {
        None, F, Z, X, G, H, R, F7,
        LeftAlt, RightAlt, LeftControl, RightControl, LeftShift, RightShift,
    }

    public class GUIContent
    {
        public static GUIContent none => new GUIContent("");
        public GUIContent(string text) { }
    }

    public class GUIStyleState
    {
        public Texture2D background { get; set; }
        public Color textColor { get; set; }
    }

    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public GUIStyleState normal { get; } = new GUIStyleState();
        public bool richText { get; set; }
        public int fontSize { get; set; }
        public bool wordWrap { get; set; }
        public Vector2 CalcSize(GUIContent content) => default;
    }

    public class GUISkin
    {
        public GUIStyle box { get; } = new GUIStyle();
        public GUIStyle label { get; } = new GUIStyle();
    }

    public static class GUI
    {
        public static GUISkin skin { get; } = new GUISkin();
        public static void Box(Rect r, GUIContent c, GUIStyle s) { }
        public static void Label(Rect r, GUIContent c, GUIStyle s) { }
    }
}
