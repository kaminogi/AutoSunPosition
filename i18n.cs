using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ColossalFramework.Globalization;

namespace AutoSunPosition
{
    internal static class I18n
    {
        private static readonly Dictionary<string,string> _table = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        private static string _lang = "en";
        private static bool _inited;

        public static void InitOnce()
        {
            if (_inited) return;
            _inited = true;
            TryLoad(CurrentLang());
            // 言語切替に追従
            LocaleManager.eventLocaleChanged += () => TryLoad(CurrentLang());
        }

        private static string CurrentLang()
        {
            try
            {
                var l = LocaleManager.instance?.language;
                if (!string.IsNullOrEmpty(l)) return l;   // 例: "ja" "en" "fr"
            } catch {}
            return "en";
        }

        private static void TryLoad(string lang)
        {
            _table.Clear();
            _lang = lang ?? "en";
            // 優先: 完全一致 -> 2文字 -> en の順
            if (!LoadFromEmbedded($"i18n.{_lang}.txt"))
            {
                var two = _lang.Length >= 2 ? _lang.Substring(0,2) : _lang;
                if (!LoadFromEmbedded($"i18n.{two}.txt"))
                {
                    LoadFromEmbedded("i18n.en.txt"); // 最後に英語
                }
            }
        }

        private static bool LoadFromEmbedded(string resourceName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                // 名前空間は <AssemblyRootNamespace> を前置（例: AutoSunPosition.i18n.en.txt）
                string full = null;
                foreach (var n in asm.GetManifestResourceNames())
                    if (n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase)) { full = n; break; }
                if (full == null) return false;

                using (var s = asm.GetManifestResourceStream(full))
                using (var r = new StreamReader(s))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        if (line == null || line.Length == 0 || line.Trim().Length == 0) continue;
                        if (line.StartsWith("#")) continue;
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        var k = line.Substring(0, eq).Trim();
                        var v = line.Substring(eq + 1).Trim();
                        _table[k] = v;
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public static string T(string key, string fallback = null)
        {
            InitOnce();
            if (key != null && _table.TryGetValue(key, out var v)) return v;
            return fallback ?? key ?? "";
        }
    }
}
