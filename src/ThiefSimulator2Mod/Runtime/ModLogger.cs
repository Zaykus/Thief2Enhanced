using System;
using System.IO;

namespace ThiefSimulator2Mod
{
    /// <summary>Simple file logger (no console available in a Unity player build).</summary>
    internal static class ModLogger
    {
        private static string _path;
        private static readonly object Lock = new object();

        public static string LogDirectory
        {
            get
            {
                // The native loader sets this to the game executable's directory.
                string gameRoot = Environment.GetEnvironmentVariable("THIEF_ENHANCED_ROOT");
                if (!string.IsNullOrEmpty(gameRoot)) return Path.Combine(gameRoot, "Mod", "logs");

                // Fallback: derive from this assembly's location (may be empty in Unity Mono).
                string assemblyPath = typeof(ModLogger).Assembly.Location;
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    string managedDir = Path.GetDirectoryName(assemblyPath);
                    string dataDir = managedDir != null ? Directory.GetParent(managedDir)?.FullName : null;
                    if (dataDir != null)
                    {
                        string root = Directory.GetParent(dataDir)?.FullName;
                        if (!string.IsNullOrEmpty(root)) return Path.Combine(root, "Mod", "logs");
                    }
                }

                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mod", "logs");
            }
        }

        public static void Init()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                _path = Path.Combine(LogDirectory, "mod.log");
                File.WriteAllText(_path,
                    "=== " + PluginInfo.Name + " v" + PluginInfo.Version + " ===\n" +
                    "Started: " + DateTime.Now + "\n");
            }
            catch
            {
                _path = null;
            }
        }

        public static void Info(string message) { Write("INFO", message); }
        public static void Warning(string message) { Write("WARN", message); }
        public static void Error(string message) { Write("ERROR", message); }

        private static void Write(string level, string message)
        {
            lock (Lock)
            {
                try
                {
                    if (_path == null) Init();
                    if (_path == null) return;
                    File.AppendAllText(_path, "[" + DateTime.Now.ToString("HH:mm:ss") + "] [" + level + "] " + message + "\n");
                }
                catch
                {
                    // Logging must never break the game.
                }
            }
        }
    }
}
