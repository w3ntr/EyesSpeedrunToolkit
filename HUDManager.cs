using UnityEngine;
using MelonLoader;

namespace EyesSpeedrunToolkit
{
    public static class HUDManager
    {
        private static bool _menuOpen = false;
        private static Rect _windowRect = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 250, 600, 500);
        private static int _selectedTab = 0;

        private static bool _lastDisablePP;
        private static bool _lastDisableVSync;
        private static float _lastFOV;
        private static bool _lastTimerEnabled;
        private static bool _lastMonsterTrackerEnabled;
        private static float _lastFarClip;
        private static bool _isDraggingTimer = false;
        private static Vector2 _dragOffset;
        private static Rect _timerRect;

        private static Color _warningColor = new Color(1f, 0.3f, 0.3f, 1f);

        public static void Initialize()
        {
            MelonLogger.Msg("HUDManager: Initialized (IMGUI mode)");

            _lastDisablePP = ConfigManager.DisablePostProcessing;
            _lastDisableVSync = ConfigManager.DisableVSync;
            _lastFOV = ConfigManager.TargetFOV;
            _lastTimerEnabled = ConfigManager.TimerEnabled;
            _lastMonsterTrackerEnabled = ConfigManager.MonsterTrackerEnabled;
            _lastFarClip = ConfigManager.FarClipPlane;
        }

        public static void Update()
        {
            if (Input.GetKeyDown(ConfigManager.MenuKey))
            {
                _menuOpen = !_menuOpen;
                MelonLogger.Msg("HUDManager: Menu " + (_menuOpen ? "opened" : "closed"));
            }

            CheckAndApplySettings();
        }

        private static void CheckAndApplySettings()
        {
            bool settingsChanged = false;

            if (_lastDisablePP != ConfigManager.DisablePostProcessing) { _lastDisablePP = ConfigManager.DisablePostProcessing; settingsChanged = true; }
            if (_lastDisableVSync != ConfigManager.DisableVSync) { _lastDisableVSync = ConfigManager.DisableVSync; settingsChanged = true; }
            if (Mathf.Abs(_lastFOV - ConfigManager.TargetFOV) > 0.1f) { _lastFOV = ConfigManager.TargetFOV; settingsChanged = true; }
            if (_lastTimerEnabled != ConfigManager.TimerEnabled) { _lastTimerEnabled = ConfigManager.TimerEnabled; }
            if (_lastMonsterTrackerEnabled != ConfigManager.MonsterTrackerEnabled) { _lastMonsterTrackerEnabled = ConfigManager.MonsterTrackerEnabled; }
            if (Mathf.Abs(_lastFarClip - ConfigManager.FarClipPlane) > 0.001f) { _lastFarClip = ConfigManager.FarClipPlane; settingsChanged = true; }

            if (settingsChanged)
            {
                GraphicsModule.Reset();
                MelonLogger.Msg("HUDManager: Settings changed, applying...");
            }
        }

        public static void OnGUI()
        {
            if (_menuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (ConfigManager.TimerEnabled)
            {
                string timeText = "00:00";
                if (SpeedrunTimer.IsRunning()) timeText = SpeedrunTimer.GetCurrentTimeFormatted();
                else if (SpeedrunTimer.IsFinished()) timeText = SpeedrunTimer.GetCurrentTimeFormatted() + " (FINISHED)";

                float timerWidth = 200f;
                float timerHeight = 30f;

                // Создаём прямоугольник таймера
                _timerRect = new Rect(ConfigManager.TimerX, ConfigManager.TimerY, timerWidth, timerHeight);

                // Логика перетаскивания (только если меню закрыто)
                if (!_menuOpen)
                {
                    // Переводим координаты мыши в систему координат IMGUI (0,0 сверху слева)
                    Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

                    // Проверяем, наведена ли мышь на таймер
                    if (_timerRect.Contains(mousePos))
                    {
                        // Если нажали кнопку мыши - начинаем тащить
                        if (Input.GetMouseButtonDown(0))
                        {
                            _isDraggingTimer = true;
                            // Считаем смещение, чтобы таймер не "прыгал" к курсору
                            _dragOffset = mousePos - new Vector2(ConfigManager.TimerX, ConfigManager.TimerY);
                        }
                    }

                    // Если тащим - обновляем координаты
                    if (_isDraggingTimer)
                    {
                        if (Input.GetMouseButton(0))
                        {
                            ConfigManager.TimerX = mousePos.x - _dragOffset.x;
                            ConfigManager.TimerY = mousePos.y - _dragOffset.y;

                            // Ограничиваем, чтобы не утащить слишком далеко за экран (но разрешаем выходить за края)
                            float edgeMargin = 200f; // Запас в 200 пикселей за край экрана
                            ConfigManager.TimerX = Mathf.Clamp(ConfigManager.TimerX, -edgeMargin, Screen.width - timerWidth + edgeMargin);
                            ConfigManager.TimerY = Mathf.Clamp(ConfigManager.TimerY, -edgeMargin, Screen.height - timerHeight + edgeMargin);

                            // Обновляем прямоугольник для плавности
                            _timerRect.x = ConfigManager.TimerX;
                            _timerRect.y = ConfigManager.TimerY;
                        }
                        else
                        {
                            // Отпустили мышь - сохраняем позицию
                            _isDraggingTimer = false;
                            ConfigManager.Save();
                            MelonLogger.Msg($"HUDManager: Timer position saved to ({ConfigManager.TimerX:F0}, {ConfigManager.TimerY:F0})");
                        }
                    }
                }

                // Рисуем сам таймер
                GUI.Label(_timerRect, timeText);
            }

            // МОНСТР-ТРЕКЕР (нижний правый угол)
            if (ConfigManager.MonsterTrackerEnabled)
            {
                float dist = MonsterTracker.GetDistanceToPlayer();
                string monsterText = "Monster: --";
                Color monsterColor = Color.white;

                if (dist >= 0)
                {
                    monsterText = $"Monster: {dist:F1}m";
                    monsterColor = dist < 15f ? Color.red : Color.white;
                }

                float textWidth = 200f;
                float textHeight = 30f;
                float padding = 10f;
                Rect monsterRect = new Rect(
                    Screen.width - textWidth - padding,
                    Screen.height - textHeight - padding,
                    textWidth,
                    textHeight
                );

                Color originalColor = GUI.color;
                GUI.color = monsterColor;
                GUI.Label(monsterRect, monsterText);
                GUI.color = originalColor;
            }

            // МЕНЮ
            if (_menuOpen)
            {
                _windowRect = GUI.Window(0, _windowRect, WindowFunction, "Eyes Speedrun Toolkit");
            }
        }

        private static void WindowFunction(int windowID)
        {
            GUILayout.BeginHorizontal();
            _selectedTab = GUILayout.SelectionGrid(_selectedTab, new string[] { "QoL", "Cheats", "Settings" }, 3, GUILayout.Height(30));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0: DrawQoLTab(); break;
                case 1: DrawCheatsTab(); break;
                case 2: DrawSettingsTab(); break;
            }

            if (GUI.Button(new Rect(_windowRect.width - 30, 5, 25, 20), "X"))
            {
                _menuOpen = false;
            }

            GUI.DragWindow();
        }

        private static void DrawQoLTab()
        {
            GUILayout.Label("Quality of Life (Allowed on SRC)", GUILayout.Height(20));
            GUILayout.Space(10);

            bool newDisablePP = GUILayout.Toggle(ConfigManager.DisablePostProcessing, "Disable Post-Processing (Vignette, Bloom, etc.)");
            if (newDisablePP != ConfigManager.DisablePostProcessing) { ConfigManager.DisablePostProcessing = newDisablePP; }

            bool newDisableVSync = GUILayout.Toggle(ConfigManager.DisableVSync, "Disable V-Sync");
            if (newDisableVSync != ConfigManager.DisableVSync)
            {
                ConfigManager.DisableVSync = newDisableVSync;
                GraphicsModule.Reset(); // Сразу применяем
            }

            // Слайдер FPS (активен только если V-Sync отключен)
            if (ConfigManager.DisableVSync)
            {
                GUILayout.Space(10);

                string fpsLabel = ConfigManager.CustomFPS > 0
                    ? $"FPS Limit: {ConfigManager.CustomFPS}"
                    : "FPS Limit: Unlimited";
                GUILayout.Label(fpsLabel, GUILayout.Height(20));

                // Слайдер: 0 = unlimited, 30-300 = лимит
                float fpsFloat = ConfigManager.CustomFPS;
                float newFpsFloat = GUILayout.HorizontalSlider(fpsFloat, 0f, 300f);
                int newFps = Mathf.RoundToInt(newFpsFloat);

                if (newFps != ConfigManager.CustomFPS)
                {
                    ConfigManager.CustomFPS = newFps;
                    GraphicsModule.Reset(); // Применяем сразу
                }

                GUILayout.Space(5);
                GUILayout.Label("(0 = Unlimited, 30-300 = FPS cap)", GUILayout.Height(20));
                GUILayout.Label("(Possibly helps fix coil whine at high FPS)", GUILayout.Height(20));
            }

            bool newTimerEnabled = GUILayout.Toggle(ConfigManager.TimerEnabled, "Show Speedrun Timer");
            if (newTimerEnabled != ConfigManager.TimerEnabled) { ConfigManager.TimerEnabled = newTimerEnabled; }

            GUILayout.Space(10);
            GUILayout.Label("FOV: " + ConfigManager.TargetFOV.ToString("F0"));
            float newFov = GUILayout.HorizontalSlider(ConfigManager.TargetFOV, 60f, 120f);
            if (Mathf.Abs(newFov - ConfigManager.TargetFOV) > 0.1f) { ConfigManager.TargetFOV = newFov; }

            GUILayout.Space(20);
            if (GUILayout.Button("Save Settings"))
            {
                ConfigManager.Save();
                GraphicsModule.Reset();
                MelonLogger.Msg("HUDManager: Settings saved");
            }
        }

        private static void DrawCheatsTab()
        {
            Color originalColor = GUI.color;
            GUI.color = _warningColor;

            GUILayout.Label("⚠️ BANNED ON SPEEDRUN.COM! ⚠️", GUILayout.Height(25));
            GUILayout.Label("These features give an unfair advantage.", GUILayout.Height(20));
            GUILayout.Label("Use ONLY for practice or casual play.", GUILayout.Height(20));

            GUI.color = originalColor;
            GUILayout.Space(15);

            // <-- НОВЫЙ ТОГГЛ: Monster Tracker
            bool newMonsterTracker = GUILayout.Toggle(ConfigManager.MonsterTrackerEnabled, "Show Monster Distance (bottom-right)");
            if (newMonsterTracker != ConfigManager.MonsterTrackerEnabled)
            {
                ConfigManager.MonsterTrackerEnabled = newMonsterTracker;
                ConfigManager.Save();
            }

            GUILayout.Space(5);
            GUILayout.Label("(Shows distance to monster in real-time)", GUILayout.Height(20));

            GUILayout.Space(20);

            // Far Clip Plane (нормализованный)
            float farClipMeters = 11f + (ConfigManager.FarClipPlane - 0.01f) / (1.00f - 0.01f) * (1000f - 11f);
            GUILayout.Label($"Far Clip Plane: {farClipMeters:F1}m (value: {ConfigManager.FarClipPlane:F2})", GUILayout.Height(20));
            float newFarClip = GUILayout.HorizontalSlider(ConfigManager.FarClipPlane, 0.01f, 1.00f);
            if (Mathf.Abs(newFarClip - ConfigManager.FarClipPlane) > 0.001f)
            {
                ConfigManager.FarClipPlane = newFarClip;
            }

            GUILayout.Space(5);
            GUILayout.Label("(0.01 = 11m original, 1.00 = 1000m max)", GUILayout.Height(20));

            GUILayout.Space(20);

            // Monster Tracker детали (в меню)
            GUILayout.Label("--- Monster Tracker Details ---", GUILayout.Height(20));
            GUILayout.Space(5);

            float dist = MonsterTracker.GetDistanceToPlayer();
            if (dist >= 0)
            {
                GUILayout.Label("Distance: " + dist.ToString("F1") + "m", GUILayout.Height(20));
                GUILayout.Label("Position: " + MonsterTracker.GetCurrentPosition().ToString("F1"), GUILayout.Height(20));
            }
            else
            {
                GUILayout.Label("Status: Searching or not found", GUILayout.Height(20));
            }

            GUILayout.Space(5);
            GUILayout.Label("Path history: " + MonsterTracker.GetPathHistoryCount() + " points", GUILayout.Height(20));
        }

        private static void DrawSettingsTab()
        {
            GUILayout.Label("Menu Keybind: " + ConfigManager.MenuKey.ToString());
            GUILayout.Space(10);

            if (GUILayout.Button("Reset to Defaults"))
            {
                ConfigManager.ResetToDefaults();
                GraphicsModule.Reset();
                MelonLogger.Msg("HUDManager: Settings reset");
            }
        }
    }
}