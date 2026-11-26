using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Shared state between the menu and the feature/patch implementations.
    /// Values are intentionally plain fields so IMGUI can mutate them directly.
    /// </summary>
    public static class ModState
    {
        // Player
        public static bool GodMode;
        public static bool InvisibleToNpcs;
        public static bool InvisibleToCameras;
        public static bool NoClip;
        public static bool NoFallDamage;
        public static bool InfiniteJumps;
        public static bool InfiniteStamina;
        public static bool NoTiredness;
        public static bool SuspendEnemies;
        public static float WalkSpeed = 1f;
        public static float RunSpeed = 1f;
        public static float CrouchSpeed = 1f;
        public static float JumpHeight = 1f;
        public static float Gravity = 1f;
        public static float NoclipSpeed = 12f;

        // Economy / progression
        public static bool InfiniteMoney;
        public static bool NoItemWeight;
        public static bool UnlimitedItemUses;
        public static int Money = 1000000;
        public static int Debt;
        public static int SkillPoints = 99;
        public static int SpecialPoints = 99;
        public static int Level = 1;
        public static int Experience;
        public static int ExperienceNext;

        // Skills / security
        public static bool AutoLockpicking;
        public static bool AutoPickpocketing;
        public static bool AutoSafeCracking;
        public static bool AutoHacking;
        public static bool NoPoliceChase;

        // World
        public static bool StopTime;
        public static float GameSpeed = 1f;
        public static float TargetHour = 12f;
        public static float TargetMinute;

        // Items / vehicles
        public static bool UnlimitedFuel;
        public static bool NoCarDamage;
        public static string ItemSearch = "Lockpick";
        public static int ItemAmount = 1;

        // Teleport slots
        public static readonly Vector3Store[] Slots = new Vector3Store[8];

        static ModState()
        {
            for (int i = 0; i < Slots.Length; i++)
                Slots[i] = new Vector3Store();
        }

        public sealed class Vector3Store
        {
            public float X;
            public float Y;
            public float Z;
            public bool HasValue;
        }

        // ------------------------------------------------------------ presets

        public const float DefaultWalkSpeed = 1f;
        public const float DefaultRunSpeed = 1f;
        public const float DefaultCrouchSpeed = 1f;
        public const float DefaultJumpHeight = 1f;
        public const float DefaultGravity = 1f;
        public const float DefaultNoclipSpeed = 12f;
        public const float DefaultGameSpeed = 1f;
        public const float DefaultTargetHour = 12f;
        public const float DefaultTargetMinute = 0f;
        public const int DefaultMoney = 1000000;
        public const int DefaultSkillPoints = 99;
        public const int DefaultSpecialPoints = 99;

        public static int ChangedCount()
        {
            int n = 0;

            if (GodMode) n++;
            if (NoFallDamage) n++;
            if (InfiniteStamina) n++;
            if (NoTiredness) n++;
            if (InfiniteJumps) n++;
            if (NoClip) n++;
            if (InvisibleToNpcs) n++;
            if (InvisibleToCameras) n++;
            if (SuspendEnemies) n++;
            if (InfiniteMoney) n++;
            if (NoItemWeight) n++;
            if (UnlimitedItemUses) n++;
            if (AutoLockpicking) n++;
            if (AutoPickpocketing) n++;
            if (AutoSafeCracking) n++;
            if (AutoHacking) n++;
            if (NoPoliceChase) n++;
            if (StopTime) n++;
            if (UnlimitedFuel) n++;
            if (NoCarDamage) n++;

            if (!Mathf.Approximately(WalkSpeed, DefaultWalkSpeed)) n++;
            if (!Mathf.Approximately(RunSpeed, DefaultRunSpeed)) n++;
            if (!Mathf.Approximately(CrouchSpeed, DefaultCrouchSpeed)) n++;
            if (!Mathf.Approximately(JumpHeight, DefaultJumpHeight)) n++;
            if (!Mathf.Approximately(Gravity, DefaultGravity)) n++;
            if (!Mathf.Approximately(NoclipSpeed, DefaultNoclipSpeed)) n++;
            if (!Mathf.Approximately(GameSpeed, DefaultGameSpeed)) n++;
            if (!Mathf.Approximately(TargetHour, DefaultTargetHour)) n++;
            if (!Mathf.Approximately(TargetMinute, DefaultTargetMinute)) n++;

            if (Money != DefaultMoney) n++;
            if (Debt != 0) n++;
            if (SkillPoints != DefaultSkillPoints) n++;
            if (SpecialPoints != DefaultSpecialPoints) n++;
            if (Level != 1) n++;
            if (Experience != 0) n++;
            if (ExperienceNext != 0) n++;

            for (int i = 0; i < Slots.Length; i++)
                if (Slots[i] != null && Slots[i].HasValue) n++;

            return n;
        }

        public static void ResetPlayer()
        {
            GodMode = false;
            NoFallDamage = false;
            InfiniteStamina = false;
            NoTiredness = false;
            InfiniteJumps = false;
            NoClip = false;
            WalkSpeed = DefaultWalkSpeed;
            RunSpeed = DefaultRunSpeed;
            CrouchSpeed = DefaultCrouchSpeed;
            JumpHeight = DefaultJumpHeight;
            Gravity = DefaultGravity;
            NoclipSpeed = DefaultNoclipSpeed;
        }

        public static void ResetMoney()
        {
            InfiniteMoney = false;
            NoItemWeight = false;
            UnlimitedItemUses = false;
            Money = DefaultMoney;
            Debt = 0;
            SkillPoints = DefaultSkillPoints;
            SpecialPoints = DefaultSpecialPoints;
            Level = 1;
            Experience = 0;
            ExperienceNext = 0;
        }

        public static void ResetSkills()
        {
            AutoLockpicking = false;
            AutoPickpocketing = false;
            AutoSafeCracking = false;
            AutoHacking = false;
        }

        public static void ResetSecurity()
        {
            InvisibleToNpcs = false;
            InvisibleToCameras = false;
            NoPoliceChase = false;
            SuspendEnemies = false;
        }

        public static void ResetWorld()
        {
            StopTime = false;
            GameSpeed = DefaultGameSpeed;
            TargetHour = DefaultTargetHour;
            TargetMinute = DefaultTargetMinute;
        }

        public static void ResetItems()
        {
            ItemSearch = "Lockpick";
            ItemAmount = 1;
        }

        public static void ResetVehicles()
        {
            UnlimitedFuel = false;
            NoCarDamage = false;
        }

        // ------------------------------------------------ per-section state

        public static bool IsDefenseChanged()
        {
            return GodMode || NoFallDamage || InfiniteStamina || NoTiredness;
        }

        public static bool IsMovementChanged()
        {
            return InfiniteJumps || NoClip ||
                   !Mathf.Approximately(WalkSpeed, DefaultWalkSpeed) ||
                   !Mathf.Approximately(RunSpeed, DefaultRunSpeed) ||
                   !Mathf.Approximately(CrouchSpeed, DefaultCrouchSpeed) ||
                   !Mathf.Approximately(JumpHeight, DefaultJumpHeight) ||
                   !Mathf.Approximately(Gravity, DefaultGravity) ||
                   !Mathf.Approximately(NoclipSpeed, DefaultNoclipSpeed);
        }

        public static bool IsTeleportChanged()
        {
            for (int i = 0; i < Slots.Length; i++)
                if (Slots[i] != null && Slots[i].HasValue) return true;
            return false;
        }

        public static bool IsCashChanged()
        {
            return InfiniteMoney || Money != DefaultMoney || Debt != 0;
        }

        public static bool IsProgressionChanged()
        {
            return SkillPoints != DefaultSkillPoints || SpecialPoints != DefaultSpecialPoints ||
                   Level != 1 || Experience != 0 || ExperienceNext != 0;
        }

        public static bool IsInventoryChanged()
        {
            return NoItemWeight || UnlimitedItemUses;
        }

        public static bool IsAutoChanged()
        {
            return AutoLockpicking || AutoPickpocketing || AutoSafeCracking || AutoHacking;
        }

        public static bool IsSkillPointsChanged()
        {
            return SkillPoints != DefaultSkillPoints || SpecialPoints != DefaultSpecialPoints;
        }

        public static bool IsDetectionChanged()
        {
            return InvisibleToNpcs || InvisibleToCameras || NoPoliceChase;
        }

        public static bool IsDevicesChanged()
        {
            return SuspendEnemies;
        }

        public static bool IsTimeChanged()
        {
            return StopTime ||
                   !Mathf.Approximately(GameSpeed, DefaultGameSpeed) ||
                   !Mathf.Approximately(TargetHour, DefaultTargetHour) ||
                   !Mathf.Approximately(TargetMinute, DefaultTargetMinute);
        }

        public static bool IsSpawnerChanged()
        {
            return (ItemSearch ?? "") != "Lockpick" || ItemAmount != 1;
        }

        public static bool IsVehicleChanged()
        {
            return UnlimitedFuel || NoCarDamage;
        }

        public static void ResetDefense()
        {
            GodMode = false;
            NoFallDamage = false;
            InfiniteStamina = false;
            NoTiredness = false;
        }

        public static void ResetMovement()
        {
            InfiniteJumps = false;
            NoClip = false;
            WalkSpeed = DefaultWalkSpeed;
            RunSpeed = DefaultRunSpeed;
            CrouchSpeed = DefaultCrouchSpeed;
            JumpHeight = DefaultJumpHeight;
            Gravity = DefaultGravity;
            NoclipSpeed = DefaultNoclipSpeed;
        }

        public static void ResetTeleport()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] == null) Slots[i] = new Vector3Store();
                else Slots[i].HasValue = false;
            }
        }

        public static void ResetCash()
        {
            InfiniteMoney = false;
            Money = DefaultMoney;
            Debt = 0;
        }

        public static void ResetProgression()
        {
            SkillPoints = DefaultSkillPoints;
            SpecialPoints = DefaultSpecialPoints;
            Level = 1;
            Experience = 0;
            ExperienceNext = 0;
        }

        public static void ResetInventory()
        {
            NoItemWeight = false;
            UnlimitedItemUses = false;
        }

        public static void ResetAuto()
        {
            AutoLockpicking = false;
            AutoPickpocketing = false;
            AutoSafeCracking = false;
            AutoHacking = false;
        }

        public static void ResetDetection()
        {
            InvisibleToNpcs = false;
            InvisibleToCameras = false;
            NoPoliceChase = false;
        }

        public static void ResetDevices()
        {
            SuspendEnemies = false;
        }

        public static void ResetTime()
        {
            StopTime = false;
            GameSpeed = DefaultGameSpeed;
            TargetHour = DefaultTargetHour;
            TargetMinute = DefaultTargetMinute;
        }

        public static void ResetSpawner()
        {
            ItemSearch = "Lockpick";
            ItemAmount = 1;
        }
    }
}
