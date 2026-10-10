using System;
using System.Reflection;

namespace AdvancedPlanting
{
    /// <summary>
    /// Access to the game's Localization class, which lives in assembly_guiutils. Resolved by
    /// reflection so the mod does not need that assembly at compile time.
    /// </summary>
    internal static class GameText
    {
        private static bool _resolved;
        private static PropertyInfo _instance;
        private static MethodInfo _localize;

        public static string Localize(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('$') < 0) return text;
            Resolve();
            if (_instance == null || _localize == null) return text;
            try
            {
                object loc = _instance.GetValue(null, null);
                if (loc == null) return text;
                return _localize.Invoke(loc, new object[] { text }) as string ?? text;
            }
            catch (Exception)
            {
                return text;
            }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType("Localization", false);
                if (t == null) continue;
                _instance = t.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                _localize = t.GetMethod("Localize", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                if (_instance != null && _localize != null) return;
            }
        }
    }
}
