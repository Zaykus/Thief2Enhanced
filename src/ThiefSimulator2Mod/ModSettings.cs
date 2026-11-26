using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Persists movement preferences and teleport slots. Teleport positions are
    /// included because they are part of player progress. Cheats, money,
    /// skills, time and world state are deliberately never stored here so a
    /// restored session cannot surprise the player or damage a save.
    /// </summary>
    public static class ModSettings
    {
        private const string FileName = "settings.cfg";
        private const float SaveDelay = 1.5f;
        private const int SlotCount = 8;
        private const float CoordinateLimit = 100000f;

        // index, file key, default, minimum, maximum
        private static readonly string[] Keys =
        {
            "walkspeed", "runspeed", "crouchspeed", "jumpheight", "gravity", "ghostspeed"
        };
        private static readonly float[] Defaults =
        {
            ModState.DefaultWalkSpeed, ModState.DefaultRunSpeed, ModState.DefaultCrouchSpeed,
            ModState.DefaultJumpHeight, ModState.DefaultGravity, ModState.DefaultNoclipSpeed
        };
        private static readonly float[] Minimums =
        {
            0.1f, 0.1f, 0.1f, 0.1f, 0f, 1f
        };
        private static readonly float[] Maximums =
        {
            5f, 5f, 5f, 4f, 2f, 30f
        };

        private static readonly float[] SavedMovement = new float[Keys.Length];
        private static readonly float[] SavedSlotX = new float[SlotCount];
        private static readonly float[] SavedSlotY = new float[SlotCount];
        private static readonly float[] SavedSlotZ = new float[SlotCount];
        private static readonly bool[] SavedSlotHas = new bool[SlotCount];

        private static bool _loaded;
        private static float _changedAt;

        public static string FilePath
        {
            get { return Path.Combine(ModLogger.LogDirectory, "..", "config", FileName); }
        }

        public static void Load()
        {
            _loaded = true;
            for (int i = 0; i < SavedMovement.Length; i++) SavedMovement[i] = Defaults[i];
            Array.Clear(SavedSlotX, 0, SavedSlotX.Length);
            Array.Clear(SavedSlotY, 0, SavedSlotY.Length);
            Array.Clear(SavedSlotZ, 0, SavedSlotZ.Length);
            Array.Clear(SavedSlotHas, 0, SavedSlotHas.Length);

            try
            {
                string path = FilePath;
                if (File.Exists(path))
                {
                    foreach (string raw in File.ReadAllLines(path))
                    {
                        string line = raw == null ? string.Empty : raw.Trim();
                        if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;

                        int split = line.IndexOf('=');
                        if (split < 0) continue;
                        string key = line.Substring(0, split).Trim().ToLowerInvariant();
                        string value = line.Substring(split + 1).Trim();

                        int movementIndex = Array.IndexOf(Keys, key);
                        if (movementIndex >= 0)
                        {
                            float parsed;
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                                SavedMovement[movementIndex] = ClampMovement(parsed, movementIndex);
                            continue;
                        }

                        if (key.StartsWith("slot", StringComparison.Ordinal))
                        {
                            int slotNumber;
                            if (int.TryParse(key.Substring(4), out slotNumber) && slotNumber >= 1 && slotNumber <= SlotCount)
                            {
                                float x, y, z;
                                if (TryParseSlot(value, out x, out y, out z))
                                {
                                    int index = slotNumber - 1;
                                    SavedSlotX[index] = x;
                                    SavedSlotY[index] = y;
                                    SavedSlotZ[index] = z;
                                    SavedSlotHas[index] = true;
                                }
                            }
                        }
                    }
                }
                ApplyMovement(SavedMovement);
                ApplySlots();
                ModLogger.Info("Loaded saved movement preferences and teleport slots.");
            }
            catch (Exception ex)
            {
                ApplyMovement(Defaults);
                ClearSlots();
                ModLogger.Warning("Could not read saved settings: " + ex.Message);
            }
        }

        /// <summary>Called once per frame; writes at most once after edits settle.</summary>
        public static void Tick()
        {
            if (!_loaded) return;

            if (!IsDirty())
            {
                _changedAt = 0f;
                return;
            }

            float now = Time.unscaledTime;
            if (_changedAt <= 0f)
            {
                _changedAt = now;
                return;
            }
            if (now - _changedAt < SaveDelay) return;

            Save();
            _changedAt = 0f;
        }

        private static void Save()
        {
            try
            {
                string path = FilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var lines = new List<string>
                {
                    "# Thief2Enhanced saved settings",
                    "# Only movement preferences and teleport slots are stored.",
                    "# Cheats, money, skills, time and world state are never saved here.",
                    "# Edit while the game is closed; invalid values are clamped on load."
                };

                float[] movement = CurrentMovement();
                for (int i = 0; i < Keys.Length; i++)
                    lines.Add(Keys[i] + "=" + movement[i].ToString("0.###", CultureInfo.InvariantCulture));

                for (int i = 0; i < SlotCount; i++)
                {
                    ModState.Vector3Store slot = GetSlot(i);
                    if (slot == null || !slot.HasValue)
                        lines.Add("slot" + (i + 1) + "=empty");
                    else
                        lines.Add("slot" + (i + 1) + "=" +
                            ClampCoordinate(slot.X).ToString("0.###", CultureInfo.InvariantCulture) + "," +
                            ClampCoordinate(slot.Y).ToString("0.###", CultureInfo.InvariantCulture) + "," +
                            ClampCoordinate(slot.Z).ToString("0.###", CultureInfo.InvariantCulture));
                }

                File.WriteAllLines(path, lines.ToArray());

                Array.Copy(movement, SavedMovement, SavedMovement.Length);
                for (int i = 0; i < SlotCount; i++)
                {
                    ModState.Vector3Store slot = GetSlot(i);
                    SavedSlotHas[i] = slot != null && slot.HasValue;
                    if (SavedSlotHas[i])
                    {
                        SavedSlotX[i] = ClampCoordinate(slot.X);
                        SavedSlotY[i] = ClampCoordinate(slot.Y);
                        SavedSlotZ[i] = ClampCoordinate(slot.Z);
                    }
                    else
                    {
                        SavedSlotX[i] = 0f;
                        SavedSlotY[i] = 0f;
                        SavedSlotZ[i] = 0f;
                    }
                }
                ModLogger.Info("Saved settings to Mod\\config\\" + FileName);
            }
            catch (Exception ex)
            {
                ModLogger.Warning("Could not save settings: " + ex.Message);
            }
        }

        private static bool IsDirty()
        {
            float[] movement = CurrentMovement();
            for (int i = 0; i < SavedMovement.Length; i++)
                if (!Approximately(movement[i], SavedMovement[i])) return true;

            for (int i = 0; i < SlotCount; i++)
            {
                ModState.Vector3Store slot = GetSlot(i);
                bool has = slot != null && slot.HasValue;
                if (has != SavedSlotHas[i]) return true;
                if (has)
                {
                    if (!Approximately(ClampCoordinate(slot.X), SavedSlotX[i]) ||
                        !Approximately(ClampCoordinate(slot.Y), SavedSlotY[i]) ||
                        !Approximately(ClampCoordinate(slot.Z), SavedSlotZ[i])) return true;
                }
            }
            return false;
        }

        private static float[] CurrentMovement()
        {
            return new[]
            {
                ModState.WalkSpeed, ModState.RunSpeed, ModState.CrouchSpeed,
                ModState.JumpHeight, ModState.Gravity, ModState.NoclipSpeed
            };
        }

        private static void ApplyMovement(float[] values)
        {
            ModState.WalkSpeed = values[0];
            ModState.RunSpeed = values[1];
            ModState.CrouchSpeed = values[2];
            ModState.JumpHeight = values[3];
            ModState.Gravity = values[4];
            ModState.NoclipSpeed = values[5];
        }

        private static ModState.Vector3Store GetSlot(int index)
        {
            return ModState.Slots != null && index < ModState.Slots.Length ? ModState.Slots[index] : null;
        }

        private static void ApplySlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (ModState.Slots == null || i >= ModState.Slots.Length) break;
                if (ModState.Slots[i] == null) ModState.Slots[i] = new ModState.Vector3Store();

                ModState.Vector3Store slot = ModState.Slots[i];
                slot.HasValue = SavedSlotHas[i];
                slot.X = SavedSlotX[i];
                slot.Y = SavedSlotY[i];
                slot.Z = SavedSlotZ[i];
            }
        }

        private static void ClearSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                SavedSlotHas[i] = false;
                SavedSlotX[i] = 0f;
                SavedSlotY[i] = 0f;
                SavedSlotZ[i] = 0f;
                ModState.Vector3Store slot = GetSlot(i);
                if (slot != null) slot.HasValue = false;
            }
        }

        private static bool TryParseSlot(string value, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            if (string.IsNullOrEmpty(value) ||
                value.Equals("empty", StringComparison.OrdinalIgnoreCase)) return false;

            string[] parts = value.Split(',');
            if (parts.Length != 3) return false;
            return float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                   float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                   float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out z);
        }

        private static float ClampMovement(float value, int index)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return Defaults[index];
            return Clamp(value, Minimums[index], Maximums[index]);
        }

        private static float ClampCoordinate(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
            return Clamp(value, -CoordinateLimit, CoordinateLimit);
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        private static bool Approximately(float a, float b)
        {
            return Math.Abs(a - b) <= 0.0001f;
        }
    }
}
