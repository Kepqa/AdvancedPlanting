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

        public string LocalizedName => Loc.T("wheel.title");

        public Sprite Sprite => Icons.Curve;

        public void InitRadialConfig(RadialBase radial)
        {
            FigureState s = _plugin.State;
            var list = new List<RadialMenuElement>();

            string single = Loc.T("wheel.single");
            list.Add(Element(s.Enabled ? single : Loc.T("wheel.current", single), Loc.T("wheel.single.desc"), Icons.Single, () =>
            {
                _plugin.SetFigureMode(false);
                return true;
            }, close: true));

            AddMode(list, PathMode.Segment, "wheel.line.desc", Icons.Line);
            AddMode(list, PathMode.Arc, "wheel.arc.desc", Icons.Arc);
            AddMode(list, PathMode.Circle, "wheel.circle.desc", Icons.Circle);
            AddMode(list, PathMode.Spline, "wheel.curve.desc", Icons.Curve);
            AddMode(list, PathMode.Contour, "wheel.shape.desc", Icons.Shape);

            list.Add(Element(Loc.T("wheel.side", StatusPanel.SideName(s.Side, s.IsClosedPath)), Loc.T("wheel.side.desc"), Icons.Side, () =>
            {
                s.CycleSide();
                radial.Refresh();
                return true;
            }, close: false));

            list.Add(Element(Loc.T(s.Fill ? "wheel.fill.on" : "wheel.fill.off"),
                Loc.T(s.IsClosedPath ? "wheel.fill.desc" : "wheel.fill.na"), Icons.Fill, () =>
                {
                    s.SetFill(!s.Fill);
                    radial.Refresh();
                    return true;
                }, close: false));

            list.Add(Element(Loc.T("wheel.undo"),
                s.IsEmpty ? Loc.T("wheel.undo.none") : Loc.T("wheel.undo.count", s.ControlPoints.Count), Icons.Undo, () =>
                {
                    s.RemoveLastPoint();
                    return true;
                }, close: true));

            list.Add(Element(Loc.T("wheel.clear"), Loc.T("wheel.clear.desc"), Icons.Clear, () =>
            {
                s.Clear();
                return true;
            }, close: true));

            list.Add(Element(Loc.T("wheel.reshuffle"), Loc.T("wheel.reshuffle.desc"), Icons.Reshuffle, () =>
            {
                s.RerollSeed();
                return true;
            }, close: false));

            list.Add(Element(Loc.T("wheel.settings"), Loc.T("wheel.settings.desc"), Icons.Settings, () =>
            {
                _plugin.Window.Open();
                return true;
            }, close: true));

            radial.ConstructRadial(list);
        }

        private void AddMode(List<RadialMenuElement> list, PathMode mode, string descKey, Sprite icon)
        {
            FigureState s = _plugin.State;
            string name = StatusPanel.ModeName(mode);
            bool current = s.Enabled && s.Mode == mode;
            list.Add(Element(current ? Loc.T("wheel.current", name) : name, Loc.T(descKey), icon, () =>
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
