using UnityEngine;
using MelonLoader;
using System.Collections.Generic;

namespace EyesSpeedrunToolkit
{
    public static class HUDToggle
    {
        // Чёрный список Canvas'ов, которые отвечают за меню, ввод или важные события.
        // Их трогать нельзя, иначе сломается управление или игра не даст возродиться.
        private static readonly string[] BlacklistedCanvases = new string[]
        {
            "CanvasChat", "CanvasMultiplayer", "Map", "CanvasJournal",
            "CanvasInternatConnection", "CanvasFrequent", "CanvasRare",
            "CanvasMoreEyes", "CanvasTutorial", "CanvasUpgradeSprint",
            "CameraEffects", "Collectible", "CanvasWaiting", "CanvasRevive"
        };

        private static List<Canvas> _hudCanvases = new List<Canvas>();
        private static bool _isInitialized = false;

        public static void Initialize()
        {
            MelonLogger.Msg("HUDToggle: Initialized");
        }

        public static void Update()
        {
            if (!ConfigManager.HideHUDEnabled) return;

            // Инициализация один раз при первом включении
            if (!_isInitialized)
            {
                FindHUDElements();
                _isInitialized = true;
            }

            // Отключаем только компонент Canvas (делает UI невидимым, но не ломает ввод)
            foreach (Canvas canvas in _hudCanvases)
            {
                if (canvas != null && canvas.enabled)
                {
                    canvas.enabled = false;
                }
            }
        }

        private static void FindHUDElements()
        {
            MelonLogger.Msg("HUDToggle: Searching for HUD elements...");

            Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>();
            int hudCount = 0;

            foreach (Canvas canvas in allCanvases)
            {
                if (canvas == null) continue;

                string canvasName = canvas.gameObject.name;

                // 1. Пропускаем чёрный список (интерактивные меню)
                bool isBlacklisted = false;
                foreach (string blacklisted in BlacklistedCanvases)
                {
                    if (canvasName == blacklisted)
                    {
                        isBlacklisted = true;
                        break;
                    }
                }
                if (isBlacklisted) continue;

                // 2. Пропускаем наш мод-меню
                string lowerName = canvasName.ToLower();
                if (lowerName.Contains("speedrun") || lowerName.Contains("toolkit") ||
                    lowerName.Contains("mod") || lowerName.Contains("menu"))
                {
                    continue;
                }

                // 3. Берём только оверлейные Canvas'ы (обычный HUD)
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                    canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    _hudCanvases.Add(canvas);
                    hudCount++;
                    MelonLogger.Msg($"  Found HUD Canvas: {canvasName}");
                }
            }

            MelonLogger.Msg($"HUDToggle: Found {hudCount} safe HUD canvases to hide");
        }

        public static void Enable()
        {
            ConfigManager.HideHUDEnabled = true;
            _isInitialized = false; // Сбросить для повторного поиска
            MelonLogger.Msg("HUDToggle: Enabled");
        }

        public static void Disable()
        {
            ConfigManager.HideHUDEnabled = false;

            // Включаем компонент Canvas обратно
            foreach (Canvas canvas in _hudCanvases)
            {
                if (canvas != null && !canvas.enabled)
                {
                    canvas.enabled = true;
                }
            }

            MelonLogger.Msg("HUDToggle: Disabled, HUD restored");
        }

        public static void Reset()
        {
            _hudCanvases.Clear();
            _isInitialized = false;
        }
    }
}