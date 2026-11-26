using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Called by the native UnityPlayer.dll proxy from an attached Mono thread.
    /// Only the three Update hooks are installed here because they are the
    /// bootstrap. Gameplay patches are deferred to ModRuntime on the Unity main
    /// thread: patching live methods from a worker thread can crash Unity.
    /// </summary>
    public static class NativeEntry
    {
        private static bool _initialized;
        private static Harmony _harmony;
        private static bool _gameplayPatched;
        private static readonly HashSet<string> _gameplayClasses = new HashSet<string>();

        public static bool GameplayPatched { get { return _gameplayPatched; } }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            ModLogger.Init();
            ModLogger.Info("Native loader attached to Mono runtime.");

            try
            {
                InstallBootstrapPatches();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Patch installation failed: " + ex);
            }
        }

        private static void InstallBootstrapPatches()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "Assembly-CSharp")
                    ModLogger.Info("Assembly-CSharp instance: " + asm.GetHashCode() + " location='" + asm.Location + "'");
            }

            _harmony = new Harmony(PluginInfo.Guid);
            PatchClass(typeof(GameUpdatePatch));
            PatchClass(typeof(PlayerUpdatePatch));
            PatchClass(typeof(MainMenuUpdatePatch));
            LogPatched("Harmony bootstrap patches installed: ");
        }

        /// <summary>Installed from ModRuntime on the Unity main thread.</summary>
        public static void InstallGameplayPatches()
        {
            if (_gameplayPatched) return;

            if (_harmony == null)
            {
                ModLogger.Error("Cannot install gameplay patches: Harmony instance is missing.");
                return;
            }

            Type[] patchClasses =
            {
                typeof(GodModePatch),
                typeof(NoFallDamagePatch),
                typeof(AutoLockpickPatch),
                typeof(AutoSafeCrackingPatch),
                typeof(AutoPickpocketPatch),
                typeof(AutoHackingPatch),
                typeof(NoCarDamagePatch),
                typeof(FreezeGuardsPatch)
            };

            // Patch each class at most once so a retry after a partial failure
            // cannot double-apply postfixes.
            for (int i = 0; i < patchClasses.Length; i++)
            {
                Type patchClass = patchClasses[i];
                if (_gameplayClasses.Contains(patchClass.FullName)) continue;
                try
                {
                    PatchClass(patchClass);
                    _gameplayClasses.Add(patchClass.FullName);
                }
                catch (Exception ex)
                {
                    ModLogger.Error("Gameplay patch failed for " + patchClass.Name + ": " + ex);
                }
            }

            if (_gameplayClasses.Count == patchClasses.Length)
            {
                _gameplayPatched = true;
                ModLogger.Info("Harmony gameplay patches installed: " + _gameplayClasses.Count + " classes.");
            }
        }

        private static void PatchClass(Type patchClass)
        {
            _harmony.CreateClassProcessor(patchClass).Patch();
        }

        private static void LogPatched(string prefix)
        {
            int patched = 0;
            foreach (var m in _harmony.GetPatchedMethods())
            {
                ModLogger.Info("Patched method: " + m.DeclaringType.FullName + "." + m.Name);
                patched++;
            }
            ModLogger.Info(prefix + patched + " methods.");
        }
    }
}
