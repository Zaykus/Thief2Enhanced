using HarmonyLib;

namespace ThiefSimulator2Mod
{
    /// <summary>Main-thread hook on GameManager.Update().</summary>
    [HarmonyPatch(typeof(GameManager), "Update")]
    public static class GameUpdatePatch
    {
        public static void Prefix() { ModRuntime.Tick(); }
        public static void Postfix() { ModRuntime.ApplyCursorAfterGameUpdate(); }
    }

    /// <summary>Backup main-thread hook on the player's per-frame update.</summary>
    [HarmonyPatch(typeof(PlayerScript), "Update")]
    public static class PlayerUpdatePatch
    {
        public static void Prefix() { ModRuntime.Tick(); }
        public static void Postfix() { ModRuntime.ApplyCursorAfterGameUpdate(); }
    }

    /// <summary>Hook that runs on the title screen too.</summary>
    [HarmonyPatch(typeof(MainMenu), "Update")]
    public static class MainMenuUpdatePatch
    {
        public static void Prefix() { ModRuntime.Tick(); }
        public static void Postfix() { ModRuntime.ApplyCursorAfterGameUpdate(); }
    }
}
