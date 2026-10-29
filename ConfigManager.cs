using UnityEngine;
using MelonLoader;
using System;

namespace EyesSpeedrunToolkit
{
    public static class ConfigManager
    {
        // QoL настройки
        public static bool DisablePostProcessing = false;
        public static bool DisableVSync = false;
        public static int CustomFPS = 0;
        public static float TargetFOV = 60f;
        public static bool TimerEnabled = true;
        public static bool ShowMilliseconds = false;
        public static bool HideHUDEnabled = false;
        public static float TimerX = 10f;
        public static float TimerY = 10f;

        // Cheats (Not for SRC)
        public static bool DisableFalloffShader = false;
        public static float RenderDistance = 11f;
        public static bool ESPEnabled = false;
        public static bool MonsterTrackerEnabled = false;
        public static bool ESP2DBoxEnabled = false;
        public static bool DisableFadeEnabled = false;
        public static bool FreecamEnabled = false;
        public static bool JumpEnabled = false;
        public static float JumpForce = 6f;

        // Practice
        public static bool BagsWallHackEnabled = false;

        // PlayerModifiers

        public static bool InfiniteStaminaEnabled = false;
        public static bool StaminaTimerEnabled = false;


        // Меню
        public static KeyCode MenuKey = KeyCode.F1;

        private const string PREFIX = "EST_";
        private static bool _loaded = false;

        public static void Load()
        {
            if (_loaded) return;
            MelonLogger.Msg("ConfigManager: Loading settings...");
            try
            {
                DisablePostProcessing = PlayerPrefs.GetInt(PREFIX + "DisablePP", 0) == 1;
                DisableVSync = PlayerPrefs.GetInt(PREFIX + "DisableVSync", 0) == 1;
                CustomFPS = PlayerPrefs.GetInt(PREFIX + "CustomFPS", 0);
                TargetFOV = PlayerPrefs.GetFloat(PREFIX + "FOV", 60f);
                TimerEnabled = PlayerPrefs.GetInt(PREFIX + "TimerEnabled", 1) == 1;
                ShowMilliseconds = PlayerPrefs.GetInt(PREFIX + "ShowMilliseconds", 0) == 1;
                HideHUDEnabled = PlayerPrefs.GetInt(PREFIX + "HideHUD", 0) == 1;
                DisableFadeEnabled = PlayerPrefs.GetInt(PREFIX + "DisableFade", 0) == 1;
                InfiniteStaminaEnabled = PlayerPrefs.GetInt(PREFIX + "InfiniteStamina", 0) == 1;
                StaminaTimerEnabled = PlayerPrefs.GetInt(PREFIX + "StaminaTimer", 0) == 1;
                JumpEnabled = PlayerPrefs.GetInt(PREFIX + "Jump", 0) == 1;
                JumpForce = PlayerPrefs.GetFloat(PREFIX + "JumpForce", 6f);
                MonsterTrackerEnabled = PlayerPrefs.GetInt(PREFIX + "MonsterTracker", 0) == 1;
                ESPEnabled = PlayerPrefs.GetInt(PREFIX + "ESP", 0) == 1;
                ESP2DBoxEnabled = PlayerPrefs.GetInt(PREFIX + "ESP2DBox", 0) == 1;
                FreecamEnabled = PlayerPrefs.GetInt(PREFIX + "Freecam", 0) == 1;
                DisableFalloffShader = PlayerPrefs.GetInt(PREFIX + "DisableFalloff", 0) == 1;
                TimerX = PlayerPrefs.GetFloat(PREFIX + "TimerX", 10f);
                TimerY = PlayerPrefs.GetFloat(PREFIX + "TimerY", 10f);
                RenderDistance = PlayerPrefs.GetFloat(PREFIX + "RenderDistance", 11f);
                MenuKey = (KeyCode)PlayerPrefs.GetInt(PREFIX + "MenuKey", (int)KeyCode.F1);
                _loaded = true;
                MelonLogger.Msg($"ConfigManager: Settings loaded. Menu key: {MenuKey}");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"ConfigManager: Failed to load. Error: {e.Message}");
                _loaded = true;
            }
        }

        public static void Save()
        {
            try
            {
                PlayerPrefs.SetInt(PREFIX + "DisablePP", DisablePostProcessing ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "DisableVSync", DisableVSync ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "CustomFPS", CustomFPS);
                PlayerPrefs.SetFloat(PREFIX + "FOV", TargetFOV);
                PlayerPrefs.SetInt(PREFIX + "TimerEnabled", TimerEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "ShowMilliseconds", ShowMilliseconds ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "HideHUD", HideHUDEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "DisableFade", DisableFadeEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "InfiniteStamina", InfiniteStaminaEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "StaminaTimer", StaminaTimerEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "Jump", JumpEnabled ? 1 : 0);
                PlayerPrefs.SetFloat(PREFIX + "JumpForce", JumpForce);
                PlayerPrefs.SetInt(PREFIX + "MonsterTracker", MonsterTrackerEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "ESP", ESPEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "ESP2DBox", ESP2DBoxEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "Freecam", FreecamEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "DisableFalloff", DisableFalloffShader ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "BagsWallHack", BagsWallHackEnabled ? 1 : 0);
                PlayerPrefs.SetFloat(PREFIX + "TimerX", TimerX);
                PlayerPrefs.SetFloat(PREFIX + "TimerY", TimerY);
                PlayerPrefs.SetFloat(PREFIX + "RenderDistance", RenderDistance);
                PlayerPrefs.SetInt(PREFIX + "MenuKey", (int)MenuKey);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"ConfigManager: Failed to save. Error: {e.Message}");
            }
        }

        public static void ResetToDefaults()
        {
            DisablePostProcessing = false;
            DisableVSync = false;
            CustomFPS = 0;
            TargetFOV = 60f;
            TimerEnabled = true;
            ShowMilliseconds = false;
            HideHUDEnabled = false;
            DisableFadeEnabled = false;
            JumpEnabled = false;
            JumpForce = 6f;
            MonsterTrackerEnabled = false;
            ESPEnabled = false;
            ESP2DBoxEnabled = false;
            FreecamEnabled = false;
            DisableFalloffShader = false;
            BagsWallHackEnabled = false;
            TimerX = 10f;
            TimerY = 10f;
            RenderDistance = 11f;
            MenuKey = KeyCode.F1;
            Save();
            MelonLogger.Msg("ConfigManager: Reset to defaults");
        }
    }
}