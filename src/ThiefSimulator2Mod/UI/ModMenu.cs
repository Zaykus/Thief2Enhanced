using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Thief2Enhanced IMGUI menu: top tab bar, two-column card grid, flat
    /// heist-tech skin. Drawn from ModRuntime.DrawGui().
    /// </summary>
    public sealed class ModMenu
    {
        private const float DefaultWidth = 900f;
        private const float DefaultHeight = 700f;
        private const float MinWidth = 820f;
        private const float MinHeight = 540f;

        private const string SizeKeyWidth = "Thief2Enhanced.MenuWidth";
        private const string SizeKeyHeight = "Thief2Enhanced.MenuHeight";

        private static readonly string[] Tabs =
        {
            "Player", "Money", "Skills", "Security", "World", "Items", "Vehicles", "Misc"
        };

        private bool _open;
        private int _tab;
        private Vector2 _scroll;
        private Rect _window = new Rect(40f, 40f, DefaultWidth, DefaultHeight);
        private bool _sizeInitialized;
        private bool _resizing;
        private Vector2 _resizeStartMouse;
        private Vector2 _resizeStartSize;
        private float _pendingWidth;
        private float _pendingHeight;
        private bool _recenter;
        private bool _saveSize;
        private string _status = "";
        private float _statusUntil;

        private bool _cursorCaptured;
        private bool _savedCursorVisible;
        private CursorLockMode _savedCursorLock;
        private PlayerScript _savedPlayer;
        private bool _savedPlayerCursorLock;
        private bool _savedPlayerCursorLockCaptured;

        public void Toggle() { SetOpen(!_open); }
        public bool IsOpen { get { return _open; } }

        private void SetOpen(bool open)
        {
            if (_open == open) return;
            _open = open;
            if (_open) CaptureCursor();
            else RestoreCursor();
        }

        private void CaptureCursor()
        {
            _savedCursorVisible = Cursor.visible;
            _savedCursorLock = Cursor.lockState;
            _cursorCaptured = true;

            PlayerScript ps = GameContext.PS;
            if (ps != null)
            {
                _savedPlayer = ps;
                _savedPlayerCursorLock = ps.cursorLock;
                _savedPlayerCursorLockCaptured = true;
            }
            ApplyCursorState();
        }

        /// <summary>Re-applied after the game's Update so the game cannot hide/lock it again.</summary>
        public void ApplyCursorState()
        {
            if (!_open) return;
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;

            PlayerScript ps = GameContext.PS;
            if (ps != null && ps.cursorLock)
                ps.cursorLock = false;
        }

        private void RestoreCursor()
        {
            if (_cursorCaptured)
            {
                Cursor.visible = _savedCursorVisible;
                Cursor.lockState = _savedCursorLock;
                _cursorCaptured = false;
            }
            if (_savedPlayerCursorLockCaptured && _savedPlayer != null)
            {
                _savedPlayer.cursorLock = _savedPlayerCursorLock;
                _savedPlayerCursorLockCaptured = false;
                _savedPlayer = null;
            }
        }

        public void OnGUI()
        {
            ModConfig cfg = ModRuntime.Config;
            string key = cfg != null ? cfg.MenuKey.ToString() : "F1";

            // The scrim would dim the chip while the menu is open, so only
            // draw it in the closed state where it is actually useful.
            if (cfg != null && cfg.ShowHint && !_open)
                GuiHelper.HintChip(PluginInfo.Name + "   |   " + key + " = menu");

            if (!_open) return;

            ApplyCursorState();
            InitializeWindowSize(cfg);
            ClampWindow();

            Color oldColor = GUI.color;
            Color oldBackground = GUI.backgroundColor;
            int oldDepth = GUI.depth;
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.depth = -100;

            GuiHelper.DrawScrim();
            _window = GUI.Window(0x7A11, _window, DrawWindow, GUIContent.none, GuiHelper.WindowStyle);

            // GUI.Window owns the rect while it draws, so the new size from the
            // resize grip must be applied after the callback returns.
            if (_pendingWidth > 0f || _pendingHeight > 0f)
            {
                if (_pendingWidth > 0f) _window.width = _pendingWidth;
                if (_pendingHeight > 0f) _window.height = _pendingHeight;
                _pendingWidth = 0f;
                _pendingHeight = 0f;
                ClampWindow();
            }

            if (_recenter)
            {
                _window.x = Mathf.Clamp((Screen.width - _window.width) * 0.5f, 0f,
                    Mathf.Max(0f, Screen.width - _window.width));
                _window.y = Mathf.Clamp((Screen.height - _window.height) * 0.5f, 0f,
                    Mathf.Max(0f, Screen.height - _window.height));
                _recenter = false;
            }
            if (_saveSize)
            {
                _saveSize = false;
                SaveWindowSize();
            }

            GUI.depth = oldDepth;
            GUI.backgroundColor = oldBackground;
            GUI.color = oldColor;
        }

        private void InitializeWindowSize(ModConfig cfg)
        {
            if (_sizeInitialized) return;
            float defaultWidth = cfg != null ? cfg.WindowWidth : DefaultWidth;
            float width = PlayerPrefs.GetFloat(SizeKeyWidth, defaultWidth);
            float height = PlayerPrefs.GetFloat(SizeKeyHeight, DefaultHeight);
            _window.width = Mathf.Clamp(width, MinWidth, Mathf.Max(MinWidth, Screen.width - 40f));
            _window.height = Mathf.Clamp(height, MinHeight, Mathf.Max(MinHeight, Screen.height - 40f));
            _window.x = Mathf.Clamp((Screen.width - _window.width) * 0.5f, 0f,
                Mathf.Max(0f, Screen.width - _window.width));
            _window.y = Mathf.Clamp((Screen.height - _window.height) * 0.5f, 0f,
                Mathf.Max(0f, Screen.height - _window.height));
            _sizeInitialized = true;
        }

        private void ClampWindow()
        {
            float maxWidth = Mathf.Max(MinWidth, Screen.width - 40f);
            float maxHeight = Mathf.Max(MinHeight, Screen.height - 40f);
            _window.width = Mathf.Clamp(_window.width, MinWidth, maxWidth);
            _window.height = Mathf.Clamp(_window.height, MinHeight, maxHeight);
            _window.x = Mathf.Clamp(_window.x, 0f, Mathf.Max(0f, Screen.width - _window.width));
            _window.y = Mathf.Clamp(_window.y, 0f, Mathf.Max(0f, Screen.height - _window.height));
        }

        private void ResetWindowSize()
        {
            _pendingWidth = Mathf.Clamp(DefaultWidth, MinWidth, Mathf.Max(MinWidth, Screen.width - 40f));
            _pendingHeight = Mathf.Clamp(DefaultHeight, MinHeight, Mathf.Max(MinHeight, Screen.height - 40f));
            _recenter = true;
            _saveSize = true;
            SetStatus("Window size reset");
        }

        private void SaveWindowSize()
        {
            PlayerPrefs.SetFloat(SizeKeyWidth, _window.width);
            PlayerPrefs.SetFloat(SizeKeyHeight, _window.height);
            PlayerPrefs.Save();
        }

        private void HandleResize(Rect grip)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && grip.Contains(e.mousePosition))
            {
                _resizing = true;
                _resizeStartMouse = e.mousePosition;
                _resizeStartSize = new Vector2(_window.width, _window.height);
                e.Use();
            }
            else if (_resizing && e.type == EventType.MouseDrag)
            {
                _pendingWidth = Mathf.Clamp(
                    _resizeStartSize.x + e.mousePosition.x - _resizeStartMouse.x,
                    MinWidth, Mathf.Max(MinWidth, Screen.width - 40f));
                _pendingHeight = Mathf.Clamp(
                    _resizeStartSize.y + e.mousePosition.y - _resizeStartMouse.y,
                    MinHeight, Mathf.Max(MinHeight, Screen.height - 40f));
                e.Use();
            }
            else if (_resizing && e.type == EventType.MouseUp)
            {
                _resizing = false;
                _saveSize = true;
                e.Use();
            }
        }

        private void SetStatus(string text)
        {
            _status = text;
            _statusUntil = Time.realtimeSinceStartup + 3.5f;
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical(GUIStyle.none, GUILayout.ExpandHeight(true));

            if (GuiHelper.Header("THIEF 2", "ENHANCED",
                    "THIEF SIMULATOR 2   //   EXTERNAL MOD MENU"))
                Toggle();

            GUILayout.BeginHorizontal(GUIStyle.none);
            for (int i = 0; i < Tabs.Length; i++)
            {
                bool active = i == _tab;
                if (GuiHelper.TabButton(Tabs[i], active) && !active)
                {
                    _tab = i;
                    _scroll = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GuiHelper.Divider();

            GUILayout.Space(14f);
            GUILayout.BeginHorizontal(GUIStyle.none);
            GUILayout.Space(20f);

            GUIStyle oldBar = GUI.skin.verticalScrollbar;
            GUIStyle oldThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbar = GuiHelper.ScrollbarStyle;
            GUI.skin.verticalScrollbarThumb = GuiHelper.ScrollbarThumbStyle;

            // Always reserve the 6px scrollbar so both columns keep identical
            // left/right margins whether or not the content overflows.
            _scroll = GUILayout.BeginScrollView(_scroll, false, true,
                GUILayout.ExpandHeight(true));

            float columnWidth = (_window.width - 2f - 40f - 16f - 6f) * 0.5f;
            GUILayout.BeginHorizontal(GUIStyle.none);
            GUILayout.BeginVertical(GUIStyle.none, GUILayout.Width(columnWidth));
            DrawLeftColumn();
            GUILayout.EndVertical();
            GUILayout.Space(16f);
            GUILayout.BeginVertical(GUIStyle.none, GUILayout.Width(columnWidth));
            DrawRightColumn();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);

            GUILayout.EndScrollView();
            GUI.skin.verticalScrollbar = oldBar;
            GUI.skin.verticalScrollbarThumb = oldThumb;

            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            string left;
            bool highlight;
            if (!string.IsNullOrEmpty(_status) && Time.realtimeSinceStartup < _statusUntil)
            {
                left = _status.ToUpperInvariant();
                highlight = true;
            }
            else
            {
                int changes = ModState.ChangedCount();
                left = changes == 0
                    ? "NO CHANGES ACTIVE"
                    : changes + (changes == 1 ? " CHANGE ACTIVE" : " CHANGES ACTIVE");
                highlight = changes > 0;
            }
            string key = ModRuntime.Config != null ? ModRuntime.Config.MenuKey.ToString() : "F1";
            GuiHelper.Footer(left,
                PluginInfo.Name + " v" + PluginInfo.Version + "   |   " +
                key.ToUpperInvariant() + " TO CLOSE", highlight);

            Rect grip = new Rect(_window.width - 18f, _window.height - 18f, 14f, 14f);
            GuiHelper.DrawResizeGrip(grip, _resizing);
            HandleResize(grip);

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, _window.width, 64f));
        }

        // ------------------------------------------------------------ columns

        private void DrawLeftColumn()
        {
            switch (_tab)
            {
                case 0: PlayerLeft(); break;
                case 1: MoneyLeft(); break;
                case 2: SkillsLeft(); break;
                case 3: SecurityLeft(); break;
                case 4: WorldLeft(); break;
                case 5: ItemsLeft(); break;
                case 6: VehiclesLeft(); break;
                default: MiscLeft(); break;
            }
        }

        private void DrawRightColumn()
        {
            switch (_tab)
            {
                case 0: PlayerRight(); break;
                case 1: MoneyRight(); break;
                case 2: SkillsRight(); break;
                case 3: SecurityRight(); break;
                case 4: WorldRight(); break;
                case 5: ItemsRight(); break;
                case 6: VehiclesRight(); break;
                default: MiscRight(); break;
            }
        }

        // ------------------------------------------------------------ player

        private void PlayerLeft()
        {
            bool defense = ModState.IsDefenseChanged();
            GuiHelper.BeginCard(defense);
            if (GuiHelper.CardHeader("Defense", defense, true))
            {
                ModState.ResetDefense();
                SetStatus("Defense reset");
            }
            ModState.GodMode = GuiHelper.Toggle("God mode (no damage)", ModState.GodMode);
            ModState.NoFallDamage = GuiHelper.Toggle("No fall damage", ModState.NoFallDamage);
            ModState.InfiniteStamina = GuiHelper.Toggle("Infinite stamina", ModState.InfiniteStamina);
            ModState.NoTiredness = GuiHelper.Toggle("Never tired", ModState.NoTiredness);
            GuiHelper.EndCard();

            bool teleport = ModState.IsTeleportChanged();
            GuiHelper.BeginCard(teleport);
            if (GuiHelper.CardHeader("Teleport", teleport, true))
            {
                ModState.ResetTeleport();
                SetStatus("Teleport slots cleared");
            }

            PlayerScript ps = GameContext.PS;
            if (ps == null)
                GuiHelper.Hint("Enter the game to use teleport slots.");

            // Always draw all slots: the menu keeps its shape before the player
            // spawns, and the controls simply stay disabled until then.
            bool wasEnabled = GUI.enabled;
            GUI.enabled = ps != null;
            for (int i = 0; i < ModState.Slots.Length; i++)
            {
                ModState.Vector3Store slot = ModState.Slots[i];
                if (slot == null)
                {
                    slot = new ModState.Vector3Store();
                    ModState.Slots[i] = slot;
                }

                GUILayout.BeginHorizontal(GUILayout.Height(26f));
                GuiHelper.Muted("SLOT " + (i + 1), GUILayout.Width(46f), GUILayout.Height(26f));
                GuiHelper.Muted(slot.HasValue
                        ? string.Format("({0:0}, {1:0}, {2:0})", slot.X, slot.Y, slot.Z)
                        : "empty",
                    GUILayout.ExpandWidth(true), GUILayout.Height(26f));
                if (GuiHelper.SmallButton("SAVE", GUILayout.Width(48f)))
                {
                    Vector3 p = ps.transform.position;
                    slot.X = p.x; slot.Y = p.y; slot.Z = p.z; slot.HasValue = true;
                    SetStatus("Saved position to slot " + (i + 1));
                }
                if (GuiHelper.SmallButton("LOAD", GUILayout.Width(48f)) && slot.HasValue)
                {
                    ps.transform.position = new Vector3(slot.X, slot.Y, slot.Z);
                    SetStatus("Teleported to slot " + (i + 1));
                }
                GUILayout.EndHorizontal();
            }
            GUI.enabled = wasEnabled;
            GuiHelper.EndCard();
        }

        private void PlayerRight()
        {
            bool movement = ModState.IsMovementChanged();
            GuiHelper.BeginCard(movement);
            if (GuiHelper.CardHeader("Movement", movement, true))
            {
                ModState.ResetMovement();
                SetStatus("Movement reset");
            }
            ModState.WalkSpeed = GuiHelper.Slider("Walk speed", ModState.WalkSpeed, 0.1f, 5f, 1f, "0.0×");
            ModState.RunSpeed = GuiHelper.Slider("Run speed", ModState.RunSpeed, 0.1f, 5f, 1f, "0.0×");
            ModState.CrouchSpeed = GuiHelper.Slider("Crouch speed", ModState.CrouchSpeed, 0.1f, 5f, 1f, "0.0×");
            ModState.JumpHeight = GuiHelper.Slider("Jump height", ModState.JumpHeight, 0.1f, 4f, 1f, "0.0×");
            ModState.Gravity = GuiHelper.Slider("Gravity", ModState.Gravity, 0f, 2f, 1f, "0.0×");
            ModState.NoclipSpeed = GuiHelper.Slider("Ghost speed", ModState.NoclipSpeed, 1f, 30f, 12f, "0 m/s");
            ModState.InfiniteJumps = GuiHelper.Toggle("Unlimited jumps", ModState.InfiniteJumps);
            ModState.NoClip = GuiHelper.Toggle("No-clip / ghost", ModState.NoClip);
            GuiHelper.EndCard();

            // Read-only readouts keep the right column visually balanced with
            // the teleport slots on the left.
            GuiHelper.BeginCard(false);
            GuiHelper.CardHeader("Live status", false, false);
            PlayerScript ps = GameContext.PS;
            if (ps == null)
            {
                GuiHelper.Hint("Health: --   Stamina: --");
                GuiHelper.Hint("Money: --   Debt: --");
                GuiHelper.Hint("Level: --   Skill points: --");
                GuiHelper.Hint("Carrying: --");
                GuiHelper.Hint("Items: --   Sleep gas: --");
                GuiHelper.Hint("Enter the game to see live values.");
            }
            else
            {
                GuiHelper.Hint("Health: " + ps.bltHealth.ToString("0") + "%" +
                    "   Stamina: " + (GameContext.Stamina != null
                        ? GameContext.Stamina.currentStamina.ToString("0") : "--"));
                GuiHelper.Hint("Money: " + ps.moneyAm + "   Debt: " + ps.debtAm);
                GuiHelper.Hint("Level: " + ps.currentLvl + "   Skill points: " + ps.skillXP);
                GuiHelper.Hint("Carrying: " + ps.currentWeight.ToString("0.##") + " / " +
                    ps.maxWeight.ToString("0.##"));
                GuiHelper.Hint("Items: " + (ps.item_IDs != null ? ps.item_IDs.Count.ToString() : "0") +
                    "   Sleep gas: " + ps.gasLeft.ToString("0") + "%");
                GuiHelper.Hint("Values update each frame from the live player.");
            }
            GuiHelper.EndCard();
        }

        // ------------------------------------------------------------ money

        private void MoneyLeft()
        {
            bool cash = ModState.IsCashChanged();
            GuiHelper.BeginCard(cash);
            if (GuiHelper.CardHeader("Cash", cash, true))
            {
                ModState.ResetCash();
                SetStatus("Cash reset");
            }
            ModState.InfiniteMoney = GuiHelper.Toggle("Infinite money", ModState.InfiniteMoney);
            ModState.Money = GuiHelper.IntField("Money", ModState.Money, 0, int.MaxValue);
            if (GuiHelper.Button("APPLY MONEY")) ApplyMoneyAmount();
            ModState.Debt = GuiHelper.IntField("Debt", ModState.Debt, 0, int.MaxValue);
            GUILayout.BeginHorizontal();
            if (GuiHelper.Button("APPLY DEBT", GUILayout.ExpandWidth(true))) ApplyDebtAmount();
            if (GuiHelper.Button("CLEAR DEBT", GUILayout.ExpandWidth(true)))
            {
                ModState.Debt = 0;
                ApplyDebtAmount();
            }
            GUILayout.EndHorizontal();
            GuiHelper.EndCard();

            bool inventory = ModState.IsInventoryChanged();
            GuiHelper.BeginCard(inventory);
            if (GuiHelper.CardHeader("Inventory", inventory, true))
            {
                ModState.ResetInventory();
                SetStatus("Inventory reset");
            }
            ModState.NoItemWeight = GuiHelper.Toggle("No item weight", ModState.NoItemWeight);
            ModState.UnlimitedItemUses = GuiHelper.Toggle("Unlimited item uses", ModState.UnlimitedItemUses);
            GuiHelper.EndCard();
        }

        private void MoneyRight()
        {
            bool progression = ModState.IsProgressionChanged();
            GuiHelper.BeginCard(progression);
            if (GuiHelper.CardHeader("Progression", progression, true))
            {
                ModState.ResetProgression();
                SetStatus("Progression reset");
            }
            ModState.SkillPoints = GuiHelper.IntField("Skill points", ModState.SkillPoints, 0, 9999);
            ModState.SpecialPoints = GuiHelper.IntField("Special points", ModState.SpecialPoints, 0, 9999);
            ModState.Level = GuiHelper.IntField("Level", ModState.Level, 1, 100);
            ModState.Experience = GuiHelper.IntField("Current XP", ModState.Experience, 0, int.MaxValue);
            ModState.ExperienceNext = GuiHelper.IntField("XP to next level", ModState.ExperienceNext, 0, int.MaxValue);
            if (GuiHelper.Button("APPLY PROGRESSION")) ApplyProgression();
            GuiHelper.Hint("Values are written straight to the save game.");
            GuiHelper.EndCard();
        }

        // ------------------------------------------------------------ skills

        private void SkillsLeft()
        {
            bool auto = ModState.IsAutoChanged();
            GuiHelper.BeginCard(auto);
            if (GuiHelper.CardHeader("Auto complete", auto, true))
            {
                ModState.ResetAuto();
                SetStatus("Auto complete reset");
            }
            ModState.AutoLockpicking = GuiHelper.Toggle("Auto lockpick", ModState.AutoLockpicking);
            ModState.AutoPickpocketing = GuiHelper.Toggle("Auto pickpocket", ModState.AutoPickpocketing);
            ModState.AutoSafeCracking = GuiHelper.Toggle("Auto safe cracking", ModState.AutoSafeCracking);
            ModState.AutoHacking = GuiHelper.Toggle("Auto hacking (panel / ATM)", ModState.AutoHacking);
            GuiHelper.Hint("Triggers when the matching minigame starts.");
            GuiHelper.EndCard();
        }

        private void SkillsRight()
        {
            bool points = ModState.IsSkillPointsChanged();
            GuiHelper.BeginCard(points);
            if (GuiHelper.CardHeader("Skill points", points, true))
            {
                ModState.ResetSkillPoints();
                SetStatus("Skill points reset");
            }
            if (GuiHelper.Button("+10 SKILL POINTS"))
            {
                ModState.SkillPoints += 10;
                ApplyProgression();
                SetStatus("Added 10 skill points");
            }
            if (GuiHelper.Button("+10 SPECIAL POINTS"))
            {
                ModState.SpecialPoints += 10;
                ApplyProgression();
                SetStatus("Added 10 special points");
            }
            if (GuiHelper.Button("MAX ALL POINTS"))
            {
                ModState.SkillPoints = 99;
                ModState.SpecialPoints = 99;
                ApplyProgression();
                SetStatus("Maxed all points");
            }
            GuiHelper.Hint("Progression is applied immediately.");
            GuiHelper.EndCard();
        }

        // ------------------------------------------------------------ security

        private void SecurityLeft()
        {
            bool detection = ModState.IsDetectionChanged();
            GuiHelper.BeginCard(detection);
            if (GuiHelper.CardHeader("Detection", detection, true))
            {
                ModState.ResetDetection();
                SetStatus("Detection reset");
            }
            ModState.InvisibleToNpcs = GuiHelper.Toggle("Invisible to NPCs", ModState.InvisibleToNpcs);
            ModState.InvisibleToCameras = GuiHelper.Toggle("Invisible to cameras", ModState.InvisibleToCameras);
            ModState.NoPoliceChase = GuiHelper.Toggle("No police chase", ModState.NoPoliceChase);
            if (GuiHelper.Button("END CURRENT CHASE"))
            {
                PlayerScript ps = GameContext.PS;
                if (ps != null && ps.isPoliceChase)
                {
                    // End_Chase() only runs while isPoliceChase is still true.
                    if (GameContext.Police != null) GameContext.Police.End_Chase();
                    ps.isPoliceChase = false;
                    ps.illegal = false;
                    ps.wasSeenIllegal = false;
                    SetStatus("Chase ended");
                }
            }
            GuiHelper.EndCard();
        }

        private void SecurityRight()
        {
            bool devices = ModState.IsDevicesChanged();
            GuiHelper.BeginCard(devices);
            if (GuiHelper.CardHeader("Security devices", devices, true))
            {
                ModState.ResetDevices();
                SetStatus("Devices reset");
            }
            if (GuiHelper.Button("DISABLE ALL CAMERAS"))
            {
                SecurityCamera[] cams = UnityEngine.Object.FindObjectsOfType<SecurityCamera>();
                for (int i = 0; i < cams.Length; i++) cams[i].DisablePermamently();
                SetStatus("Disabled " + cams.Length + " cameras");
            }
            if (GuiHelper.Button("DISABLE ALL ALARMS"))
            {
                Alarm[] alarms = UnityEngine.Object.FindObjectsOfType<Alarm>();
                for (int i = 0; i < alarms.Length; i++) alarms[i].gameObject.SetActive(false);
                SetStatus("Disabled " + alarms.Length + " alarms");
            }
            ModState.SuspendEnemies = GuiHelper.Toggle("Freeze guards", ModState.SuspendEnemies);
            GuiHelper.Hint("Camera and alarm actions are one-shot.");
            GuiHelper.EndCard();
        }

        // ------------------------------------------------------------ world

        private void WorldLeft()
        {
            bool time = ModState.IsTimeChanged();
            GuiHelper.BeginCard(time);
            if (GuiHelper.CardHeader("Time", time, true))
            {
                ModState.ResetTime();
                SetStatus("Time reset");
            }
            ModState.StopTime = GuiHelper.Toggle("Stop time", ModState.StopTime);
            ModState.GameSpeed = GuiHelper.Slider("Game speed", ModState.GameSpeed, 0.1f, 5f, 1f, "0.0×");
            ModState.TargetHour = GuiHelper.Slider("Hour", ModState.TargetHour, 0f, 23f, 12f, "0");
            ModState.TargetMinute = GuiHelper.Slider("Minute", ModState.TargetMinute, 0f, 59f, 0f, "0");
            GUILayout.BeginHorizontal();
            if (GuiHelper.Button("NIGHT 22:00", GUILayout.ExpandWidth(true)))
            {
                ModState.TargetHour = 22f;
                ModState.TargetMinute = 0f;
            }
            if (GuiHelper.Button("DAY 12:00", GUILayout.ExpandWidth(true)))
            {
                ModState.TargetHour = 12f;
                ModState.TargetMinute = 0f;
            }
            GUILayout.EndHorizontal();
            GuiHelper.EndCard();
        }

        private void WorldRight()
        {
            GuiHelper.BeginCard(false);
            GuiHelper.CardHeader("Weather", false, false);
            if (GuiHelper.Button("CLEAR WEATHER")) SetWeather("Clear");
            if (GuiHelper.Button("RAIN")) SetWeather("Rain");
            if (GuiHelper.Button("FOG")) SetWeather("Fog");
            GuiHelper.Hint("Weather presets come from the game's Enviro sky system.");
            GuiHelper.EndCard();
        }

        // ------------------------------------------------------------ items

        private void ItemsLeft()
        {
            bool spawner = ModState.IsSpawnerChanged();
            GuiHelper.BeginCard(spawner);
            if (GuiHelper.CardHeader("Item spawner", spawner, true))
            {
                ModState.ResetSpawner();
                SetStatus("Spawner reset");
            }
            ModState.ItemSearch = GuiHelper.TextField("Item name / ID", ModState.ItemSearch);
            ModState.ItemAmount = GuiHelper.IntField("Amount", ModState.ItemAmount, 1, 999);
            if (GuiHelper.Button("GIVE ITEM")) GiveItem();
            GuiHelper.Hint("Matches carried stacks by name or numeric ID.");
            GuiHelper.EndCard();

            GuiHelper.BeginCard(false);
            GuiHelper.CardHeader("Item stats", false, false);
            PlayerScript ps = GameContext.PS;
            if (ps != null)
            {
                GuiHelper.Hint("Items carried: " + (ps.item_IDs != null ? ps.item_IDs.Count.ToString() : "0"));
                GuiHelper.Hint("Weight: " + ps.currentWeight.ToString("0.##") + " / " + ps.maxWeight.ToString("0.##"));
                GuiHelper.Hint("Sleep gas: " + ps.gasLeft.ToString("0.##") + "%");
            }
            else GuiHelper.Hint("Enter the game to use item features.");
            GuiHelper.EndCard();
        }

        private void ItemsRight()
        {
            GuiHelper.BeginCard(false);
            GuiHelper.CardHeader("Quick give", false, false);
            if (GuiHelper.Button("ALL TOOLS")) GiveTools();
            if (GuiHelper.Button("LOCKPICKS x10")) GiveItemById(1, 10);
            if (GuiHelper.Button("FULL HEALTH")) HealPlayer();
            GuiHelper.Hint("Full health restores health and sleep gas resistance.");
            GuiHelper.EndCard();
        }

        // ------------------------------------------------------------ vehicles

        private void VehiclesLeft()
        {
            bool vehicle = ModState.IsVehicleChanged();
            GuiHelper.BeginCard(vehicle);
            if (GuiHelper.CardHeader("Vehicle", vehicle, true))
            {
                ModState.ResetVehicles();
                SetStatus("Vehicle settings reset");
            }
            ModState.NoCarDamage = GuiHelper.Toggle("Skip car collisions", ModState.NoCarDamage);
            ModState.UnlimitedFuel = GuiHelper.Toggle("Infinite sleep gas", ModState.UnlimitedFuel);
            if (GuiHelper.Button("REFILL SLEEP GAS"))
            {
                PlayerScript ps = GameContext.PS;
                if (ps != null)
                {
                    ps.gasLeft = 100f;
                    SetStatus("Sleep gas refilled");
                }
            }
            if (GuiHelper.Button("CLEAR DAMAGE BILL"))
            {
                ClearDamageBill();
                SetStatus("Damage bill cleared");
            }
            GuiHelper.EndCard();
        }

        private void VehiclesRight()
        {
            GuiHelper.BeginCard(false);
            GuiHelper.CardHeader("Notes", false, false);
            GuiHelper.Hint("Sleep gas is the player's gas tank, not car fuel.");
            GuiHelper.Hint("Vehicle unlocks and upgrades stay in the in-game car menu.");
            GuiHelper.Hint("Collision skip only affects the car you are driving.");
            GuiHelper.EndCard();
        }

        // ------------------------------------------------------------ misc

        private void MiscLeft()
        {
            GuiHelper.BeginCard(false);
            GuiHelper.CardHeader("Menu", false, false);
            GuiHelper.Hint("Menu key: " +
                (ModRuntime.Config != null ? ModRuntime.Config.MenuKey.ToString() : "F1") +
                "   (edit Mod\\config\\mod.cfg)");
            if (GuiHelper.Button("RESET WINDOW SIZE")) ResetWindowSize();
            if (GuiHelper.Button("CLOSE MENU")) Toggle();
            GuiHelper.EndCard();
        }

        private void MiscRight()
        {
            GuiHelper.BeginCard(false);
            GuiHelper.CardHeader("Logs", false, false);
            if (GuiHelper.Button("OPEN LOG FOLDER"))
            {
                try { System.Diagnostics.Process.Start(ModLogger.LogDirectory); }
                catch { SetStatus("Could not open log folder"); }
            }
            GuiHelper.Hint("Runtime log: Mod\\logs\\mod.log");
            GuiHelper.EndCard();

            GuiHelper.BeginCard(false);
            GuiHelper.CardHeader("About", false, false);
            GuiHelper.Hint(PluginInfo.Name + " v" + PluginInfo.Version);
            GuiHelper.Hint("Native UnityPlayer proxy + Mono attach. No BepInEx loader.");
            GuiHelper.Hint("Single-player use only.");
            GuiHelper.EndCard();
        }

        // ------------------------------------------------------------ actions

        private static void ApplyMoneyAmount()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null) return;
            ps.moneyAm = ModState.Money;
            ps.SaveMoneyAmount();
            try { SPrefs.SetInt("Money", ps.moneyAm); }
            catch { }
        }

        private static void ApplyDebtAmount()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null) return;
            ps.debtAm = ModState.Debt;
            ps.SaveMoneyAmount();
            try { SPrefs.SetInt("Money", ps.moneyAm); }
            catch { }
        }

        private static void ApplyProgression()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null) return;
            ps.skillXP = ModState.SkillPoints;
            ps.specialSkillXP = ModState.SpecialPoints;
            ps.currentLvl = ModState.Level;
            ps.currentXPPoints = ModState.Experience;
            ps.nextLevelPoints = Mathf.Max(1f, ModState.ExperienceNext);
            // Do not call RefreshXP(): it can grant unintended level-ups.
            if (ps.saveGameScript != null) ps.saveGameScript.Save_XP_And_Level();
        }

        private void SetWeather(string name)
        {
            try
            {
                EnviroSkyMgr mgr = EnviroSkyMgr.instance;
                if (mgr == null) { SetStatus("Weather system not ready"); return; }

                System.Collections.Generic.List<EnviroWeatherPreset> presets = mgr.GetCurrentWeatherPresetList();
                bool found = false;
                if (presets != null)
                {
                    for (int i = 0; i < presets.Count; i++)
                        if (presets[i] != null && presets[i].Name == name) { found = true; break; }
                }
                if (!found) { SetStatus("Weather preset '" + name + "' not available"); return; }

                mgr.ChangeWeather(name);
                SetStatus("Weather: " + name);
            }
            catch (System.Exception ex)
            {
                SetStatus("Weather change failed");
                ModLogger.Error("Weather change failed: " + ex);
            }
        }

        private static void ClearDamageBill()
        {
            GameManager gm = GameContext.GM;
            if (gm != null)
            {
                gm.carDamagesValue = 0;
                if (gm.CarDamagesObj != null) gm.CarDamagesObj.SetActive(false);
                if (gm.carDamagesTxt != null) gm.carDamagesTxt.text = "";
            }
            CarController[] cars = UnityEngine.Object.FindObjectsOfType<CarController>();
            for (int i = 0; i < cars.Length; i++) cars[i].DisableAlarm();
        }

        private void GiveItem()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null || ps.item_IDs == null) return;
            string query = (ModState.ItemSearch ?? "").ToLowerInvariant();
            if (query.Length == 0)
            {
                SetStatus("Enter an item name or ID");
                return;
            }
            int found = 0;
            for (int i = 0; i < ps.item_IDs.Count; i++)
            {
                ItemID item = ps.item_IDs[i];
                if (item == null) continue;
                if (item.item_name != null && item.item_name.ToLowerInvariant().Contains(query))
                {
                    item.item_amount += ModState.ItemAmount;
                    found++;
                }
                else if (query == item.item_ID.ToString())
                {
                    item.item_amount += ModState.ItemAmount;
                    found++;
                }
            }
            SetStatus("Updated " + found + " item stack(s)");
        }

        private void GiveItemById(int id, int amount)
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null || ps.item_IDs == null) return;
            for (int i = 0; i < ps.item_IDs.Count; i++)
                if (ps.item_IDs[i] != null && ps.item_IDs[i].item_ID == id)
                    ps.item_IDs[i].item_amount += amount;
            SetStatus("Gave " + amount + " of item " + id);
        }

        private void GiveTools()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null || ps.item_IDs == null) return;
            for (int i = 0; i < ps.item_IDs.Count; i++)
            {
                ItemID item = ps.item_IDs[i];
                if (item == null) continue;
                if (item.isTool && item.max_amount > 0)
                    item.item_amount = item.max_amount;
            }
            SetStatus("Maxed all tool stacks");
        }

        private static void HealPlayer()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null) return;
            ps.Heal();
            ps.bltHealth = 100f;
        }
    }
}
