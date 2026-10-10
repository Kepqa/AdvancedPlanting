using System;
using System.Collections.Generic;
using System.Reflection;
using AdvancedPlanting.Core;
using UnityEngine;
using Valheim.UI;

namespace AdvancedPlanting.UI
{
    /// <summary>
    /// Planting wheel shown in the vanilla radial menu (G by default) while a seed is selected.
    /// Built the same way the game builds its emote wheel: instances of the emote element prefab
    /// with our own name, icon and action.
    /// </summary>
    internal sealed class PlantingWheel : IRadialConfig
    {
        private static readonly FieldInfo IconField =
            typeof(RadialMenuElement).GetField("m_icon", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private readonly Plugin _plugin;

        public PlantingWheel(Plugin plugin)
        {
            _plugin = plugin;
        }

        public string LocalizedName => "Planting";

        public Sprite Sprite => Icons.Curve;

        public void InitRadialConfig(RadialBase radial)
        {
            FigureState s = _plugin.State;
            var list = new List<RadialMenuElement>();

            list.Add(Element(s.Enabled ? "Single plant" : "Single plant (current)", "Ordinary vanilla planting", Icons.Single, () =>
            {
                _plugin.SetFigureMode(false);
                return true;
            }, close: true));

            AddMode(list, PathMode.Segment, "Line", "2 points", Icons.Line);
            AddMode(list, PathMode.Arc, "Arc", "Start, middle, end", Icons.Arc);
            AddMode(list, PathMode.Circle, "Circle", "3 points on the circle", Icons.Circle);
            AddMode(list, PathMode.Spline, "Curve", "Click the last point again to finish", Icons.Curve);
            AddMode(list, PathMode.Contour, "Shape fill", "Click the first point to close", Icons.Shape);

            list.Add(Element("Rows: " + StatusPanel.SideName(s.Side), "Which side of the path extra rows go", Icons.Side, () =>
            {
                s.CycleSide();
                radial.Refresh();
                return true;
            }, close: false));

            list.Add(Element("Undo point", s.IsEmpty ? "No points yet" : s.ControlPoints.Count + " point(s)", Icons.Undo, () =>
            {
                s.RemoveLastPoint();
                return true;
            }, close: true));

            list.Add(Element("Clear figure", "Start over", Icons.Clear, () =>
            {
                s.Clear();
                return true;
            }, close: true));

            list.Add(Element(s.CanFinish ? "Finish figure" : "Finish figure (n/a)",
                "Same as clicking the last point again", Icons.Finish, () =>
                {
                    s.Finish();
                    return true;
                }, close: true));

            list.Add(Element("Reshuffle", "New random pattern for jitter and thinning", Icons.Reshuffle, () =>
            {
                s.RerollSeed();
                return true;
            }, close: false));

            list.Add(Element("Settings...", "Spacing, rows, jitter, thinning, auto-continue", Icons.Settings, () =>
            {
                _plugin.Window.Open();
                return true;
            }, close: true));

            radial.ConstructRadial(list);
        }

        private void AddMode(List<RadialMenuElement> list, PathMode mode, string name, string hint, Sprite icon)
        {
            FigureState s = _plugin.State;
            bool current = s.Enabled && s.Mode == mode;
            list.Add(Element(current ? name + " (current)" : name, hint, icon, () =>
            {
                _plugin.SetFigureMode(true);
                s.SetMode(mode);
                return true;
            }, close: true));
        }

        private static RadialMenuElement Element(string name, string subtitle, Sprite icon, Func<bool> action, bool close)
        {
            RadialMenuElement e = UnityEngine.Object.Instantiate(RadialData.SO.EmoteElement);
            e.Name = name;
            e.SubTitle = subtitle;
            e.Description = "";
            e.Interact = action;
            e.CloseOnInteract = () => close;
            SetIcon(e, icon);
            return e;
        }

        /// <summary>
        /// m_icon is a UnityEngine.UI.Image. Reached by reflection so the mod does not need
        /// UnityEngine.UI.dll to compile.
        /// </summary>
        private static void SetIcon(RadialMenuElement e, Sprite icon)
        {
            try
            {
                if (IconField == null) return;
                if (!(IconField.GetValue(e) is Component image) || image == null) return;
                image.gameObject.SetActive(icon != null);
                PropertyInfo spriteProp = image.GetType().GetProperty("sprite");
                spriteProp?.SetValue(image, icon, null);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not set wheel icon: " + ex.Message);
            }
        }
    }
}
