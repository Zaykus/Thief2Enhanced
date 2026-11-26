using System.Collections.Generic;
using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Applies all menu state to the live game every frame on the Unity main thread.
    /// </summary>
    public sealed class FeatureManager
    {
        private readonly List<IModFeature> _features = new List<IModFeature>();
        private bool _lastStopTime;
        private EnviroTime.TimeProgressMode _savedProgress = EnviroTime.TimeProgressMode.Simulated;

        public T Get<T>() where T : class, IModFeature
        {
            for (int i = 0; i < _features.Count; i++)
                if (_features[i] is T feature)
                    return feature;
            return null;
        }

        public void Register(IModFeature feature)
        {
            if (feature == null) return;
            _features.Add(feature);
        }

        public void Initialize()
        {
            Register(new PlayerFeatures());
            Register(new EconomyFeatures());
            Register(new SecurityFeatures());
            Register(new WorldFeatures());
            Register(new ItemFeatures());
            ModLogger.Info("Registered " + _features.Count + " feature groups.");
        }

        public void OnUpdate()
        {
            GameContext.Refresh();

            for (int i = 0; i < _features.Count; i++)
                _features[i].OnUpdate();

            ApplyStopTime();
        }

        private void ApplyStopTime()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null || ps.GM == null || ps.GM.enviro == null) return;

            if (ModState.StopTime)
            {
                if (!_lastStopTime)
                    _savedProgress = ps.GM.enviro.GameTime.ProgressTime;
                ps.GM.enviro.GameTime.ProgressTime = EnviroTime.TimeProgressMode.None;
            }
            else if (_lastStopTime)
            {
                ps.GM.enviro.GameTime.ProgressTime = _savedProgress;
            }
            _lastStopTime = ModState.StopTime;
        }
    }

    public interface IModFeature
    {
        string Name { get; }
        void OnUpdate();
    }

    // ---------------------------------------------------------------- PLAYER

    internal sealed class PlayerFeatures : IModFeature
    {
        public string Name { get { return "Player"; } }

        private bool _capturedMovement;
        private float _origWalk, _origRun, _origCrouch, _origJump;
        private int _origJumpsInRow;
        private bool _origCanJump;

        private bool _capturedStamina;
        private float _origStamina;
        private bool _origExhausted, _origCanRun;
        private bool _staminaApplied;

        private bool _capturedInvisible;
        private bool _origIsInvisible, _origHitByLight, _origSeenByNPC;
        private float _origDetectDistance, _origConcern;

        public void OnUpdate()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null) return;

            PlayerMotorBehavior motor = GameContext.Motor ?? ps.PMB;
            if (motor != null)
            {
                if (!_capturedMovement)
                {
                    _origWalk = motor.moveSpeed;
                    _origRun = motor.runSpeed;
                    _origCrouch = motor.crouchSpeed;
                    // CalculateMovement() rebuilds jumpForce from originalJumpForce,
                    // so that is the value the slider must scale.
                    _origJump = motor.originalJumpForce;
                    _origJumpsInRow = motor.jumpsInRow;
                    _origCanJump = motor.canJump;
                    _capturedMovement = true;
                }

                if (ModState.NoClip)
                {
                    float ghost = Mathf.Max(0.1f, ModState.NoclipSpeed);
                    motor.moveSpeed = ghost;
                    motor.runSpeed = ghost;
                    motor.crouchSpeed = ghost;
                }
                else
                {
                    motor.moveSpeed = _origWalk * Mathf.Max(0.01f, ModState.WalkSpeed);
                    motor.runSpeed = _origRun * Mathf.Max(0.01f, ModState.RunSpeed);
                    motor.crouchSpeed = _origCrouch * Mathf.Max(0.01f, ModState.CrouchSpeed);
                }

                motor.jumpForce = _origJump * Mathf.Max(0.01f, ModState.JumpHeight);
                motor.originalJumpForce = _origJump * Mathf.Max(0.01f, ModState.JumpHeight);
                motor.canJump = ModState.InfiniteJumps ? true : _origCanJump;
                motor.jumpsInRow = ModState.InfiniteJumps ? 999 : _origJumpsInRow;
                // The slider allows up to 2x; clamp to the same range.
                motor.gravityScale = ModState.NoClip ? 0f : Mathf.Clamp(ModState.Gravity, 0f, 2f);
                motor.ghostMode = ModState.NoClip;
                motor.neverExhaust = ModState.InfiniteStamina;

                if (ModState.GodMode)
                {
                    if (ps.bloodSprite != null) ps.bloodSprite.fillAmount = 1f;
                    ps.isBleeding = false;
                    ps.bltHealth = Mathf.Max(ps.bltHealth, 100f);
                }
            }

            PlayerStaminaBehavior stamina = GameContext.Stamina;
            if (stamina != null)
            {
                if (!_capturedStamina)
                {
                    _origStamina = stamina.currentStamina;
                    _origExhausted = stamina.isExhausted;
                    _origCanRun = motor != null && motor.canRun;
                    _capturedStamina = true;
                }

                bool staminaCheat = ModState.InfiniteStamina || ModState.NoTiredness;
                if (staminaCheat)
                {
                    stamina.currentStamina = stamina.maxStamina;
                    stamina.isExhausted = false;
                    if (motor != null) motor.canRun = true;
                    _staminaApplied = true;
                }
                else if (_staminaApplied)
                {
                    stamina.currentStamina = _origStamina;
                    stamina.isExhausted = _origExhausted;
                    if (motor != null) motor.canRun = _origCanRun;
                    _staminaApplied = false;
                }
            }

            // InvisibleToCameras is handled by SecurityFeatures by disabling
            // CameraTrigger.isOn / SecurityCamera.isDisabled. Do not set the
            // global ps.isInvisible cloak here, because AI and police also read it.
            if (ModState.InvisibleToNpcs)
            {
                if (!_capturedInvisible)
                {
                    _origIsInvisible = ps.isInvisible;
                    _origHitByLight = ps.isHitByLight;
                    _origSeenByNPC = ps.isSeenByNPC;
                    _origDetectDistance = ps.detectDistance;
                    _origConcern = ps.addConcernNPC;
                    _capturedInvisible = true;
                }
                ps.isInvisible = true;
                ps.isHitByLight = false;
                ps.isSeenByNPC = false;
                ps.detectDistance = 0f;
                ps.addConcernNPC = 0f;
            }
            else if (_capturedInvisible)
            {
                ps.isInvisible = _origIsInvisible;
                ps.isHitByLight = _origHitByLight;
                ps.isSeenByNPC = _origSeenByNPC;
                ps.detectDistance = _origDetectDistance;
                ps.addConcernNPC = _origConcern;
                _capturedInvisible = false;
            }
        }
    }

    // --------------------------------------------------------------- ECONOMY

    internal sealed class EconomyFeatures : IModFeature
    {
        public string Name { get { return "Economy"; } }

        private bool _infiniteSaved;

        public void OnUpdate()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null) return;

            if (ModState.InfiniteMoney)
            {
                ps.moneyAm = Mathf.Max(ps.moneyAm, 100000000);
                if (!_infiniteSaved)
                {
                    SaveStats(ps);
                    _infiniteSaved = true;
                }
            }
            else _infiniteSaved = false;

            if (ModState.NoItemWeight)
                ps.currentWeight = 0f;
        }

        private static void SaveStats(PlayerScript ps)
        {
            try { SPrefs.SetInt("Money", ps.moneyAm); }
            catch { }
        }
    }

    // --------------------------------------------------------------- SECURITY

    internal sealed class SecurityFeatures : IModFeature
    {
        public string Name { get { return "Security"; } }
        private float _nextSweep;
        private bool _cameraHidden;

        private readonly Dictionary<SecurityCamera, bool> _cameraStates = new Dictionary<SecurityCamera, bool>();
        private readonly Dictionary<CameraTrigger, bool> _triggerStates = new Dictionary<CameraTrigger, bool>();

        public void OnUpdate()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null) return;

            if (ModState.NoPoliceChase && ps.isPoliceChase)
            {
                // End_Chase() only continues when isPoliceChase is still true.
                if (GameContext.Police != null)
                    GameContext.Police.End_Chase();
                ps.isPoliceChase = false;
                ps.illegal = false;
                ps.wasSeenIllegal = false;
            }

            if (ModState.InvisibleToCameras)
                ApplyCameraInvisibility();
            else if (_cameraHidden)
                RestoreCameras();

            if (Time.unscaledTime < _nextSweep) return;
            _nextSweep = Time.unscaledTime + 2f;

            if (ModState.InvisibleToCameras)
                ApplyCameraInvisibility();
        }

        private void ApplyCameraInvisibility()
        {
            _cameraHidden = true;

            // Do NOT call DisablePermamently(): it sets a 999s permanent disable.
            // Snapshot and toggle only the detection flag so it can be restored.
            SecurityCamera[] cameras = UnityEngine.Object.FindObjectsOfType<SecurityCamera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                SecurityCamera cam = cameras[i];
                if (cam == null) continue;
                if (!_cameraStates.ContainsKey(cam)) _cameraStates[cam] = cam.isDisabled;
                cam.isDisabled = true;
            }

            CameraTrigger[] triggers = UnityEngine.Object.FindObjectsOfType<CameraTrigger>();
            for (int i = 0; i < triggers.Length; i++)
            {
                CameraTrigger trigger = triggers[i];
                if (trigger == null) continue;
                if (!_triggerStates.ContainsKey(trigger)) _triggerStates[trigger] = trigger.isOn;
                trigger.isOn = false;
            }
        }

        private void RestoreCameras()
        {
            foreach (var pair in _cameraStates)
                if (pair.Key != null) pair.Key.isDisabled = pair.Value;
            foreach (var pair in _triggerStates)
                if (pair.Key != null) pair.Key.isOn = pair.Value;

            _cameraStates.Clear();
            _triggerStates.Clear();
            _cameraHidden = false;
        }
    }

    // ------------------------------------------------------------------ WORLD

    internal sealed class WorldFeatures : IModFeature
    {
        public string Name { get { return "World"; } }

        private bool _timeCaptured;
        private float _lastHour;
        private float _lastMinute;
        private float _lastDesiredSpeed = ModState.GameSpeed;
        private bool _pendingSpeed;

        public void OnUpdate()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null || ps.GM == null || ps.GM.enviro == null) return;

            if (!_timeCaptured)
            {
                // Do not overwrite the time loaded from the save on the first frame.
                _lastHour = ModState.TargetHour;
                _lastMinute = ModState.TargetMinute;
                _timeCaptured = true;
            }
            else if (!Mathf.Approximately(_lastHour, ModState.TargetHour) ||
                     !Mathf.Approximately(_lastMinute, ModState.TargetMinute))
            {
                // SetInternalTimeOfDay takes HOURS, not a 0..1 fraction.
                float hours = Mathf.Repeat(ModState.TargetHour + ModState.TargetMinute / 60f, 24f);
                ps.GM.enviro.SetInternalTimeOfDay(hours);
                _lastHour = ModState.TargetHour;
                _lastMinute = ModState.TargetMinute;
            }

            float desired = Mathf.Max(0.01f, ModState.GameSpeed);
            if (!Mathf.Approximately(_lastDesiredSpeed, ModState.GameSpeed))
            {
                _lastDesiredSpeed = ModState.GameSpeed;
                _pendingSpeed = true;
            }
            // Never resume time behind the game's own pause/phone/UI screens.
            if (_pendingSpeed && Time.timeScale > 0f)
            {
                Time.timeScale = desired;
                _pendingSpeed = false;
            }
        }
    }

    // ------------------------------------------------------------------ ITEMS

    internal sealed class ItemFeatures : IModFeature
    {
        public string Name { get { return "Items"; } }

        public void OnUpdate()
        {
            PlayerScript ps = GameContext.PS;
            if (ps == null) return;

            // gasLeft is the sleep-gas tank (SleepingGas.cs), not car fuel.
            if (ModState.UnlimitedFuel)
                ps.gasLeft = 100f;

            if (ModState.UnlimitedItemUses)
            {
                List<ItemID> items = ps.item_IDs;
                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                        if (items[i] != null && items[i].max_amount > 0)
                            items[i].item_amount = items[i].max_amount;
                }
            }
        }
    }
}
