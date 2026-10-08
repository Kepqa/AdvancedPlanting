using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Thin reflection layer over the one vanilla method whose signature has changed between
    /// game versions: Player.PlacePiece. Older builds take only the piece and read the position
    /// from the placement ghost; newer builds take an explicit position and rotation.
    /// </summary>
    public static class VanillaBridge
    {
        private static MethodInfo _placePiece;
        private static int _placePieceArity;
        private static bool _resolved;

        public static MethodInfo ResolvePlacePiece()
        {
            if (_resolved) return _placePiece;
            _resolved = true;

            MethodInfo best = null;
            foreach (MethodInfo m in typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name != "PlacePiece") continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length == 0 || ps[0].ParameterType != typeof(Piece)) continue;
                // Prefer the overload with an explicit position.
                if (best == null || ps.Length > best.GetParameters().Length) best = m;
            }

            _placePiece = best;
            _placePieceArity = best != null ? best.GetParameters().Length : 0;
            if (best == null) Plugin.Log.LogError("Player.PlacePiece not found; planting figures will not work.");
            else Plugin.Log.LogDebug($"Resolved Player.PlacePiece with {_placePieceArity} parameters.");
            return best;
        }

        /// <summary>Calls the vanilla PlacePiece for one position. Returns the vanilla result.</summary>
        public static bool Place(Player player, Piece piece, Vector3 position, Quaternion rotation, bool doAttack)
        {
            MethodInfo m = ResolvePlacePiece();
            if (m == null) return false;

            object result;
            if (_placePieceArity >= 4)
            {
                object[] args = new object[_placePieceArity];
                args[0] = piece;
                args[1] = position;
                args[2] = rotation;
                args[3] = doAttack;
                for (int i = 4; i < _placePieceArity; i++) args[i] = DefaultFor(m.GetParameters()[i]);
                result = m.Invoke(player, args);
            }
            else
            {
                // Legacy signature: position is taken from the ghost, so move it for the call.
                GameObject ghost = player.m_placementGhost;
                Vector3 oldPos = ghost != null ? ghost.transform.position : Vector3.zero;
                Quaternion oldRot = ghost != null ? ghost.transform.rotation : Quaternion.identity;
                if (ghost != null)
                {
                    ghost.transform.position = position;
                    ghost.transform.rotation = rotation;
                }
                try
                {
                    object[] args = new object[_placePieceArity];
                    args[0] = piece;
                    for (int i = 1; i < _placePieceArity; i++) args[i] = DefaultFor(m.GetParameters()[i]);
                    result = m.Invoke(player, args);
                }
                finally
                {
                    if (ghost != null)
                    {
                        ghost.transform.position = oldPos;
                        ghost.transform.rotation = oldRot;
                    }
                }
            }

            return result is bool b ? b : true;
        }

        private static object DefaultFor(ParameterInfo p)
        {
            if (p.HasDefaultValue) return p.DefaultValue;
            Type t = p.ParameterType;
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        /// <summary>Player.TakeInput is private in vanilla; publicized at build time, but keep a reflective fallback.</summary>
        public static bool TakeInput(Player player)
        {
            try
            {
                return player.TakeInput();
            }
            catch (MissingMethodException)
            {
                MethodInfo m = AccessTools.Method(typeof(Player), "TakeInput");
                return m == null || (bool)m.Invoke(player, null);
            }
        }
    }
}
