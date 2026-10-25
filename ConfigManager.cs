using UnityEngine;
using MelonLoader;
using System;

namespace EyesSpeedrunToolkit
{
    public static class ConfigManager
    {
        // QoL настройки
        public static bool DisablePostProcessing = false;
        public static bool DisableVSync = false;       // <-- Вернули
        public static int CustomFPS = 0;               // <-- Вернули
        public static float TargetFOV = 60f;
        public static bool TimerEnabled = true;
        public static float TimerX = 10f;
        public static float TimerY = 10f;

        // Cheats (Not for SRC)
        public static float FarClipPlane = 0.01f;
        public static bool DisableFalloffShader = false;
        public static float RenderDistance = 11f; // В метрах (11-1000)
        public static bool ESPEnabled = false;
        public static bool MonsterTrackerEnabled = false;
        public static bool ESP2DBoxEnabled = false;

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
                MonsterTrackerEnabled = PlayerPrefs.GetInt(PREFIX + "MonsterTracker", 0) == 1;
                ESPEnabled = PlayerPrefs.GetInt(PREFIX + "ESP", 0) == 1;
                ESP2DBoxEnabled = PlayerPrefs.GetInt(PREFIX + "ESP2DBox", 0) == 1;
                DisableFalloffShader = PlayerPrefs.GetInt(PREFIX + "DisableFalloff", 0) == 1;
                TimerX = PlayerPrefs.GetFloat(PREFIX + "TimerX", 10f);
                TimerY = PlayerPrefs.GetFloat(PREFIX + "TimerY", 10f);
                FarClipPlane = PlayerPrefs.GetFloat(PREFIX + "FarClip", 0.01f);
                RenderDistance = PlayerPrefs.GetFloat(PREFIX + "RenderDistance", 11f);
                int keyInt = PlayerPrefs.GetInt(PREFIX + "MenuKey", (int)KeyCode.F1);
                MenuKey = (KeyCode)keyInt;
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
                PlayerPrefs.SetInt(PREFIX + "MonsterTracker", MonsterTrackerEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "ESP", ESPEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "ESP2DBox", ESP2DBoxEnabled ? 1 : 0);
                PlayerPrefs.SetInt(PREFIX + "DisableFalloff", DisableFalloffShader ? 1 : 0);
                PlayerPrefs.SetFloat(PREFIX + "TimerX", TimerX);
                PlayerPrefs.SetFloat(PREFIX + "TimerY", TimerY);
                PlayerPrefs.SetFloat(PREFIX + "FarClip", FarClipPlane);
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
            MonsterTrackerEnabled = false;
            ESPEnabled = false;
            ESP2DBoxEnabled = false;
            DisableFalloffShader = false;
            TimerX = 10f;
            TimerY = 10f;
            FarClipPlane = 0.01f;
            RenderDistance = 11f;
            MenuKey = KeyCode.F1;
            Save();
            MelonLogger.Msg("ConfigManager: Reset to defaults");
        }
    }
}