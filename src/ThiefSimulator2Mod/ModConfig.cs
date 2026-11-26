using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Plain-text options loaded from Mod/config/mod.cfg.
    /// No loader-specific configuration system is used.
    /// </summary>
    public sealed class ModConfig
    {
        public KeyCode MenuKey = KeyCode.F1;
        public bool ShowHint = true;
        public int WindowWidth = 900;
        public int DefaultMoney = 1000000;
        public int DefaultSkillPoints = 99;
        public float DefaultWalkSpeed = 1f;
        public float DefaultRunSpeed = 6f;
        public float DefaultJumpHeight = 1f;

        private static readonly Dictionary<string, Action<ModConfig, string>> Setters =
            new Dictionary<string, Action<ModConfig, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "menukey", (c, v) => { KeyCode k; if (Enum.TryParse(v, true, out k)) c.MenuKey = k; } },
                { "showhint", (c, v) => c.ShowHint = ParseBool(v, c.ShowHint) },
                { "windowwidth", (c, v) => c.WindowWidth = ParseInt(v, c.WindowWidth) },
                { "defaultmoney", (c, v) => c.DefaultMoney = ParseInt(v, c.DefaultMoney) },
                { "defaultskillpoints", (c, v) => c.DefaultSkillPoints = ParseInt(v, c.DefaultSkillPoints) },
                { "walk_speed", (c, v) => c.DefaultWalkSpeed = ParseFloat(v, c.DefaultWalkSpeed) },
                { "run_speed", (c, v) => c.DefaultRunSpeed = ParseFloat(v, c.DefaultRunSpeed) },
                { "jump_height", (c, v) => c.DefaultJumpHeight = ParseFloat(v, c.DefaultJumpHeight) }
            };

        public static ModConfig Load()
        {
            var cfg = new ModConfig();
            try
            {
                string path = Path.Combine(ModLogger.LogDirectory, "..", "config", "mod.cfg");
                path = Path.GetFullPath(path);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (!File.Exists(path))
                {
                    File.WriteAllText(path,
                        "# Thief Simulator 2 Mod Menu configuration\n" +
                        "menukey=F1\n" +
                        "showhint=true\n" +
                        "windowwidth=900\n" +
                        "defaultmoney=1000000\n" +
                        "defaultskillpoints=99\n");
                    return cfg;
                }

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int idx = line.IndexOf('=');
                    if (idx < 1) continue;
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
                    Action<ModConfig, string> setter;
                    if (Setters.TryGetValue(key, out setter)) setter(cfg, value);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning("Could not load config: " + ex.Message);
            }
            return cfg;
        }

        private static bool ParseBool(string s, bool fallback)
        {
            bool v; return bool.TryParse(s, out v) ? v : fallback;
        }

        private static int ParseInt(string s, int fallback)
        {
            int v; return int.TryParse(s, out v) ? v : fallback;
        }

        private static float ParseFloat(string s, float fallback)
        {
            float v; return float.TryParse(s, out v) ? v : fallback;
        }
    }
}
