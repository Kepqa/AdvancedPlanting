using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace AdvancedPlanting
{
    /// <summary>
    /// Translations for every text the mod shows.
    ///
    /// Strings live in "key = value" files, one per game language (English.txt, Russian.txt,
    /// German.txt, ...), named exactly like the language in the game settings. Every bundled
    /// language is embedded in the DLL. Files in the "Translations" folder next to the DLL
    /// override them or add new languages, so translators need no build tools.
    ///
    /// Lookup order: current game language, then English, then the key itself.
    /// </summary>
    public static class Loc
    {
        private const string Fallback = "English";

        private static readonly Dictionary<string, Dictionary<string, string>> Languages =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private static string _language = Fallback;
        private static Dictionary<string, string> _current;
        private static Dictionary<string, string> _fallback;
        private static float _nextLanguageCheck;
        private static bool _loaded;


        /// <summary>Current game language name, e.g. "English" or "Russian".</summary>
        public static string Language
        {
            get
            {
                Refresh();
                return _language;
            }
        }

        /// <summary>Translated text for <paramref name="key"/>, formatted with <paramref name="args"/>.</summary>
        public static string T(string key, params object[] args)
        {
            Refresh();
            string text = null;
            if (_current != null) _current.TryGetValue(key, out text);
            if (text == null && _fallback != null) _fallback.TryGetValue(key, out text);
            if (text == null) text = key;
            if (args == null || args.Length == 0) return text;
            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        /// <summary>Loads embedded and external translation files. Safe to call again to reload.</summary>
        public static void Load()
        {
            Languages.Clear();
            Assembly asm = typeof(Loc).Assembly;
            const string prefix = "AdvancedPlanting.Translations.";
            foreach (string res in asm.GetManifestResourceNames())
            {
                if (!res.StartsWith(prefix) || !res.EndsWith(".txt")) continue;
                string lang = res.Substring(prefix.Length, res.Length - prefix.Length - 4);
                using (Stream s = asm.GetManifestResourceStream(res))
                using (var reader = new StreamReader(s, Encoding.UTF8))
                {
                    Merge(lang, reader.ReadToEnd(), "embedded");
                }
            }

            try
            {
                string dir = Path.Combine(Path.GetDirectoryName(asm.Location) ?? ".", "Translations");
                if (Directory.Exists(dir))
                {
                    foreach (string file in Directory.GetFiles(dir, "*.txt"))
                        Merge(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file, Encoding.UTF8), file);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Could not read translation files: " + e.Message);
            }

            _loaded = true;
            _fallback = Languages.TryGetValue(Fallback, out var en) ? en : null;
            _nextLanguageCheck = 0f;
            ApplyLanguage(DetectLanguage());
        }

        private static void Merge(string language, string content, string source)
        {
            if (!Languages.TryGetValue(language, out var dict))
            {
                dict = new Dictionary<string, string>();
                Languages[language] = dict;
            }
            int lineNo = 0;
            foreach (string raw in content.Split('\n'))
            {
                lineNo++;
                string line = raw.TrimEnd('\r');
                string trimmed = line.TrimStart();
                if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    Plugin.Log?.LogWarning($"{source}:{lineNo}: expected 'key = value'");
                    continue;
                }
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
                dict[key] = value;
            }
        }

        private static void Refresh()
        {
            if (!_loaded) Load();
            if (Time.unscaledTime < _nextLanguageCheck) return;
            _nextLanguageCheck = Time.unscaledTime + 2f;
            string lang = DetectLanguage();
            if (!string.Equals(lang, _language, StringComparison.OrdinalIgnoreCase)) ApplyLanguage(lang);
        }

        private static void ApplyLanguage(string lang)
        {
            _language = string.IsNullOrEmpty(lang) ? Fallback : lang;
            _current = Languages.TryGetValue(_language, out var dict) ? dict : _fallback;
        }

        private static string DetectLanguage()
        {
            try
            {
                string lang = Localization.instance?.GetSelectedLanguage();
                if (!string.IsNullOrEmpty(lang)) return lang;
            }
            catch (Exception)
            {
                // Localization not ready yet; fall through to the stored preference.
            }
            return PlayerPrefs.GetString("language", Fallback);
        }
    }
}
