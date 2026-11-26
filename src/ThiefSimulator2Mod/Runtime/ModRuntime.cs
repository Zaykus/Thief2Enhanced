using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Main-thread bridge. Called every frame from Harmony prefixes on
    /// GameManager.Update(), PlayerScript.Update() and MainMenu.Update().
    /// All Unity API calls live here (never in NativeEntry, which runs on a worker thread).
    /// </summary>
    public static class ModRuntime
    {
        private static bool _ready;
        private static bool _failed;
        private static GameObject _host;
        private static int _lastTickFrame = -1;
        private static bool _tickLogged;
        private static float _nextPatchRetry;

        public static ModConfig Config { get; private set; }
        public static FeatureManager Features { get; private set; }
        public static ModMenu Menu { get; private set; }
        public static bool Ready { get { return _ready; } }

        public static void Tick()
        {
            if (!_tickLogged)
            {
                _tickLogged = true;
                ModLogger.Info("Update hook fired on main thread.");
            }

            // All three hooks call Tick(); run menu/features only once per frame.
            if (_lastTickFrame == Time.frameCount) return;
            _lastTickFrame = Time.frameCount;

            if (!_ready && !_failed) Initialize();
            if (!_ready) return;

            if (!NativeEntry.GameplayPatched && Time.unscaledTime >= _nextPatchRetry)
            {
                _nextPatchRetry = Time.unscaledTime + 2f;
                NativeEntry.InstallGameplayPatches();
            }

            Features.OnUpdate();
            ModSettings.Tick();

            if (Config != null && Input.GetKeyDown(Config.MenuKey))
                Menu.Toggle();
            else if (Menu.IsOpen && Config != null && Config.MenuKey != KeyCode.Escape &&
                     Input.GetKeyDown(KeyCode.Escape))
                Menu.Toggle();
        }

        /// <summary>
        /// Called from Update postfixes. The game rewrites Cursor state every
        /// frame, so the menu must re-apply its own state after those updates.
        /// </summary>
        public static void ApplyCursorAfterGameUpdate()
        {
            if (_ready && Menu != null && Menu.IsOpen)
                Menu.ApplyCursorState();
        }

        private static void Initialize()
        {
            try
            {
                Config = ModConfig.Load();
                ModSettings.Load();
                Features = new FeatureManager();
                Features.Initialize();
                Menu = new ModMenu();

                _host = new GameObject("Thief2Enhanced_MenuHost");
                Object.DontDestroyOnLoad(_host);
                _host.AddComponent<ModGuiHost>();

                // These patches touch live gameplay methods, so install them only
                // after we are running on Unity's main thread.
                NativeEntry.InstallGameplayPatches();

                _ready = true;
                ModLogger.Info("Runtime initialized on main thread. Press " + Config.MenuKey + " for the menu.");
            }
            catch (System.Exception ex)
            {
                _failed = true;
                _ready = false;
                ModLogger.Error("Runtime initialization failed: " + ex);
            }
        }

        public static void DrawGui()
        {
            if (_ready && Menu != null) Menu.OnGUI();
        }
    }

    /// <summary>Tiny MonoBehaviour whose OnGUI draws the menu.</summary>
    internal sealed class ModGuiHost : MonoBehaviour
    {
        private void OnGUI()
        {
            ModRuntime.DrawGui();
        }
    }
}
