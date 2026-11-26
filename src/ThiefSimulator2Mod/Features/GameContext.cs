using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Caches references to the live game objects. Refreshed periodically because
    /// the player and managers can be recreated when a new level is loaded.
    /// </summary>
    internal static class GameContext
    {
        public static PlayerScript PS;
        public static GameManager GM;
        public static PlayerMotorBehavior Motor;
        public static PlayerStaminaBehavior Stamina;
        public static Inventory Inv;
        public static Police_Manager Police;
        public static Skill_Manager Skills;
        public static EnviroSky Sky;

        private static float _nextRefresh;

        public static void Refresh(bool force = false)
        {
            float now = Time.unscaledTime;
            if (!force && now < _nextRefresh) return;
            _nextRefresh = now + 1f;

            if (PS == null || PS.transform == null)
                PS = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerScript>();
            if (GM == null)
                GM = GameObject.FindGameObjectWithTag("GameManager")?.GetComponent<GameManager>();

            if (PS != null)
            {
                Motor = PS.PMB;
                Stamina = UnityEngine.Object.FindObjectOfType<PlayerStaminaBehavior>();
                Inv = PS.invScript;
                Police = PS.PoliceManager != null ? PS.PoliceManager : UnityEngine.Object.FindObjectOfType<Police_Manager>();
                Skills = PS.skill_manager;
                Sky = GM != null ? GM.enviro : UnityEngine.Object.FindObjectOfType<EnviroSky>();
            }
            else
            {
                if (Stamina == null) Stamina = UnityEngine.Object.FindObjectOfType<PlayerStaminaBehavior>();
                if (Police == null) Police = UnityEngine.Object.FindObjectOfType<Police_Manager>();
                if (Skills == null) Skills = GameObject.FindGameObjectWithTag("SkillManager")?.GetComponent<Skill_Manager>();
                if (Sky == null) Sky = UnityEngine.Object.FindObjectOfType<EnviroSky>();
            }
        }

        public static PlayerScript Player { get { Refresh(); return PS; } }
        public static bool HasPlayer { get { Refresh(); return PS != null; } }
    }
}
