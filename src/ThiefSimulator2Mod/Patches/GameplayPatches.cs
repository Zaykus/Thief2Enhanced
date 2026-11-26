using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Gameplay patches whose targets are verified in Mod/docs/FEATURE_MAP.md and
    /// the decompiled Assembly-CSharp source. Each patch stays inert unless its
    /// matching ModState toggle is enabled.
    /// </summary>

    [HarmonyPatch(typeof(PlayerScript), "GotShot")]
    public static class GodModePatch
    {
        public static bool Prefix()
        {
            return !ModState.GodMode;
        }
    }

    [HarmonyPatch(typeof(HeadTilt), "FeelDamage")]
    public static class NoFallDamagePatch
    {
        public static bool Prefix()
        {
            return !(ModState.NoFallDamage || ModState.GodMode);
        }
    }

    [HarmonyPatch(typeof(Lockpick), "StartLockpicking")]
    public static class AutoLockpickPatch
    {
        public static bool Prefix(Lockpick __instance)
        {
            if (!ModState.AutoLockpicking || __instance == null) return true;

            try
            {
                Skill_Manager skills = __instance.PS != null ? __instance.PS.skill_manager : null;
                int level = 0;
                if (skills != null)
                {
                    if (skills.Check_If_Skill_Learned("lockpickinglevel4")) level = 4;
                    else if (skills.Check_If_Skill_Learned("lockpickinglevel3")) level = 3;
                    else if (skills.Check_If_Skill_Learned("lockpickinglevel2")) level = 2;
                    else if (skills.Check_If_Skill_Learned("lockpickinglevel1")) level = 1;
                }

                if (level < __instance.LockpickDifficulty + 2) return true;

                __instance.DoorUnlock();
                Door[] doors = __instance.unlockDoor;
                if (doors != null)
                    for (int i = 0; i < doors.Length; i++)
                        if (doors[i] != null) doors[i].Resett();
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Auto lockpick failed: " + ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(SafeMaster), "StartCracking")]
    public static class AutoSafeCrackingPatch
    {
        public static void Postfix(SafeMaster __instance)
        {
            if (!ModState.AutoSafeCracking || __instance == null) return;
            try { __instance.Success(); }
            catch (Exception ex) { ModLogger.Error("Auto safe cracking failed: " + ex); }
        }
    }

    [HarmonyPatch(typeof(Pickpocket_Manager), "Begin_Pickpocket")]
    public static class AutoPickpocketPatch
    {
        public static void Postfix(Pickpocket_Manager __instance)
        {
            if (!ModState.AutoPickpocketing || __instance == null) return;
            try { __instance.Success(); }
            catch (Exception ex) { ModLogger.Error("Auto pickpocket failed: " + ex); }
        }
    }

    [HarmonyPatch(typeof(PlayerScript), "HackingPuzzleEnable")]
    public static class AutoHackingPatch
    {
        public static bool Prefix(PlayerScript __instance)
        {
            if (!ModState.AutoHacking) return true;
            if (__instance == null) return true;

            try
            {
                __instance.Hacking_Complete();
                if (__instance.invScript != null) __instance.invScript.electronicHack = false;
                if (__instance.PMB != null) __instance.PMB.enabled = true;
                if (__instance.PCB != null) __instance.PCB.enabled = true;
                if (__instance.blur != null) __instance.blur.enabled = false;
                if (__instance.brightnessSc != null) __instance.brightnessSc.enabled = false;
                __instance.cursorLock = true;
                __instance.isLockpicking = false;
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Auto hacking failed: " + ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CarController), "OnCollisionEnter")]
    public static class NoCarDamagePatch
    {
        public static bool Prefix()
        {
            return !ModState.NoCarDamage;
        }
    }

    [HarmonyPatch(typeof(GuardAI), "Update")]
    public static class FreezeGuardsPatch
    {
        private static FieldInfo _agentField;

        public static void Postfix(GuardAI __instance)
        {
            if (!ModState.SuspendEnemies || __instance == null) return;

            try
            {
                if (_agentField == null)
                    _agentField = AccessTools.Field(typeof(GuardAI), "agent");
                object agent = _agentField != null ? _agentField.GetValue(__instance) : null;
                if (agent == null) return;

                PropertyInfo speed = agent.GetType().GetProperty("speed",
                    BindingFlags.Instance | BindingFlags.Public);
                if (speed != null && speed.CanWrite)
                    speed.SetValue(agent, 0f, null);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Freeze guards failed: " + ex);
            }
        }
    }
}
