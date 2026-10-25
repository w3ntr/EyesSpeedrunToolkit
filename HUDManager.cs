using UnityEngine;
using MelonLoader;

namespace EyesSpeedrunToolkit
{
    public static class HUDManager
    {
        private static bool _menuOpen = false;
        private static int _selectedTab = 0;

        private static bool _lastDisablePP;
        private static bool _lastDisableVSync;
        private static float _lastFOV;
        private static bool _lastTimerEnabled;
        private static bool _lastMonsterTrackerEnabled;
        private static float _lastFarClip;

        private static bool _isDraggingTimer = false;
        private static Vector2 _timerDragOffset;

        // Для скроллбара в Cheats
        private static Vector2 _cheatsScrollPosition;

        private static Color _warningColor = new Color(1f, 0.3f, 0.3f, 1f);
        public static bool IsMenuOpen => _menuOpen;

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

            // ТАЙМЕР с перетаскиванием
            if (ConfigManager.TimerEnabled)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                bool isGameScene = currentScene == "School" || currentScene == "Hospital" || currentScene == "Mansion";
                if (isGameScene)
                {
                    string timeText = "00:00";
                    if (SpeedrunTimer.IsRunning()) timeText = SpeedrunTimer.GetCurrentTimeFormatted();
                    else if (SpeedrunTimer.IsFinished()) timeText = SpeedrunTimer.GetCurrentTimeFormatted() + " (FINISHED)";

                    Rect timerRect = new Rect(ConfigManager.TimerX, ConfigManager.TimerY, 200f, 30f);

                    if (Event.current.type == EventType.MouseDown && timerRect.Contains(Event.current.mousePosition))
                    {
                        _isDraggingTimer = true;
                        _timerDragOffset = new Vector2(Event.current.mousePosition.x - ConfigManager.TimerX, Event.current.mousePosition.y - ConfigManager.TimerY);
                    }

                    if (_isDraggingTimer && Event.current.type == EventType.MouseDrag)
                    {
                        ConfigManager.TimerX = Event.current.mousePosition.x - _timerDragOffset.x;
                        ConfigManager.TimerY = Event.current.mousePosition.y - _timerDragOffset.y;
                    }

                    if (Event.current.type == EventType.MouseUp)
                    {
                        if (_isDraggingTimer)
                        {
                            _isDraggingTimer = false;
                            ConfigManager.Save();
                        }
                    }

                    GUI.Label(timerRect, timeText);
                }
            }

            // 2D ESP (Стрелка на монстра)
            if (ConfigManager.MonsterTrackerEnabled)
            {
                Vector3 monsterPos = MonsterTracker.GetCurrentPosition();
                if (monsterPos != Vector3.zero) DrawMonsterESP(monsterPos);
            }

            // МЕНЮ (делегировано WindowManager)
            if (_menuOpen)
            {
                WindowManager.Draw("Eyes Speedrun Toolkit", DrawWindowContent);

                // БЛОКИРОВКА КЛИКОВ СКВОЗЬ МЕНЮ
                if (WindowManager.WindowRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown ||
                        Event.current.type == EventType.MouseUp ||
                        Event.current.type == EventType.MouseDrag)
                    {
                        Event.current.Use();
                    }
                }
            }

            // 2D ESP BOX
            if (ConfigManager.ESP2DBoxEnabled)
            {
                Vector3 monsterPos = MonsterTracker.GetCurrentPosition();
                if (monsterPos != Vector3.zero && Camera.main != null)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(monsterPos);
                    if (screenPos.z > 0)
                    {
                        float guiX = screenPos.x - 20f;
                        float guiY = Screen.height - screenPos.y - 20f;
                        GUI.color = Color.red;
                        GUI.DrawTexture(new Rect(guiX, guiY, 40f, 40f), Texture2D.whiteTexture);

                        float dist = MonsterTracker.GetDistanceToPlayer();
                        if (dist > 0) GUI.Label(new Rect(guiX - 10f, guiY + 42f, 60f, 20f), $"{dist:F1}m");

                        GUI.color = Color.white;
                    }
                    // BAGS WALLHACK INDICATORS
                    BagsWallHack.DrawBagIndicators();
                }
            }
        }

        private static void DrawWindowContent(int windowID)
        {
            GUILayout.BeginHorizontal();
            // Теперь 4 вкладки: QoL (0), Practice (1), Cheats (2), Settings (3)
            _selectedTab = GUILayout.SelectionGrid(_selectedTab, new string[] { "QoL", "Practice", "Cheats", "Settings" }, 4, GUILayout.Height(30));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0: DrawQoLTab(); break;
                case 1: DrawPracticeTab(); break; // <-- НОВОЕ
                case 2: DrawCheatsTab(); break;
                case 3: DrawSettingsTab(); break;
            }

            if (GUI.Button(new Rect(WindowManager.WindowRect.width - 30, 5, 25, 20), "X"))
            {
                _menuOpen = false;
            }
        }

        private static void DrawQoLTab()
        {
            GUILayout.Label("Quality of Life (Allowed on SRC)", GUILayout.Height(20));
            GUILayout.Space(10);

            bool newDisablePP = GUILayout.Toggle(ConfigManager.DisablePostProcessing, "Disable Post-Processing");
            if (newDisablePP != ConfigManager.DisablePostProcessing) ConfigManager.DisablePostProcessing = newDisablePP;

            bool newDisableVSync = GUILayout.Toggle(ConfigManager.DisableVSync, "Disable V-Sync");
            if (newDisableVSync != ConfigManager.DisableVSync) { ConfigManager.DisableVSync = newDisableVSync; GraphicsModule.Reset(); }

            if (ConfigManager.DisableVSync)
            {
                GUILayout.Space(10);
                string fpsLabel = ConfigManager.CustomFPS > 0 ? $"FPS Limit: {ConfigManager.CustomFPS}" : "FPS Limit: Unlimited";
                GUILayout.Label(fpsLabel, GUILayout.Height(20));

                float newFpsFloat = GUILayout.HorizontalSlider(ConfigManager.CustomFPS, 0f, 720f);
                int newFps = Mathf.RoundToInt(newFpsFloat);
                if (newFps != ConfigManager.CustomFPS)
                {
                    ConfigManager.CustomFPS = newFps;
                    ConfigManager.Save();
                    GraphicsModule.Reset();
                }
                GUILayout.Label("(0 = Unlimited, 30-300 = FPS cap)", GUILayout.Height(20));
            }

            bool newTimerEnabled = GUILayout.Toggle(ConfigManager.TimerEnabled, "Show Speedrun Timer");
            if (newTimerEnabled != ConfigManager.TimerEnabled) ConfigManager.TimerEnabled = newTimerEnabled;

            GUILayout.Space(10);
            GUILayout.Label("FOV: " + ConfigManager.TargetFOV.ToString("F0"));
            float newFov = GUILayout.HorizontalSlider(ConfigManager.TargetFOV, 60f, 140f);
            if (Mathf.Abs(newFov - ConfigManager.TargetFOV) > 0.1f)
            {
                ConfigManager.TargetFOV = newFov;
                ConfigManager.Save();
                GraphicsModule.Reset();
            }
        }

        private static void DrawPracticeTab()
        {
            Color originalColor = GUI.color;
            GUI.color = _warningColor;
            GUILayout.Label("⚠️ PRACTICE ONLY - BANNED ON SPEEDRUN.COM! ⚠️", GUILayout.Height(25));
            GUILayout.Label("These features are for route learning and practice.", GUILayout.Height(20));
            GUILayout.Label("Using them invalidates any run for leaderboard submission.", GUILayout.Height(20));
            GUI.color = originalColor;
            GUILayout.Space(15);

            bool newBagsWH = GUILayout.Toggle(ConfigManager.BagsWallHackEnabled, "Bags Wallhack (See sacks through walls)");
            if (newBagsWH != ConfigManager.BagsWallHackEnabled)
            {
                ConfigManager.BagsWallHackEnabled = newBagsWH;
                ConfigManager.Save();

                if (ConfigManager.BagsWallHackEnabled)
                {
                    BagsWallHack.Enable();
                }
                else
                {
                    BagsWallHack.Disable();
                }
            }
            GUILayout.Label("(Highlights all sacks in green through walls)", GUILayout.Height(20));

            GUILayout.Space(20);
            GUILayout.Label("More practice tools coming soon...", GUILayout.Height(20));
        }

        private static void DrawCheatsTab()
        {
            Color originalColor = GUI.color;
            GUI.color = _warningColor;
            GUILayout.Label("⚠️ BANNED ON SPEEDRUN.COM! ⚠️", GUILayout.Height(25));
            GUILayout.Label("Use ONLY for practice or casual play.", GUILayout.Height(20));
            GUI.color = originalColor;
            GUILayout.Space(10);

            // === УМНЫЙ СКРОЛЛБАР ===
            // Вычисляем доступную высоту для скролла
            float availableHeight = WindowManager.WindowRect.height - 130f;

            // Если окно очень маленькое (< 400px), делаем скролл на всю доступную высоту
            // Если окно нормальное, ограничиваем скролл разумным максимумом
            float scrollHeight = Mathf.Clamp(availableHeight, 150f, 600f);

            _cheatsScrollPosition = GUILayout.BeginScrollView(_cheatsScrollPosition, GUILayout.Height(scrollHeight));

            bool newESPEnabled = GUILayout.Toggle(ConfigManager.ESPEnabled, "3D ESP (Wallhack)");
            if (newESPEnabled != ConfigManager.ESPEnabled)
            {
                ConfigManager.ESPEnabled = newESPEnabled;
                ConfigManager.Save();
                if (ConfigManager.ESPEnabled) MonsterTracker.EnableESP();
                else MonsterTracker.DisableESP();
            }

            GUILayout.Space(10);
            bool newDisableFalloff = GUILayout.Toggle(ConfigManager.DisableFalloffShader, "Disable Distance Fog");
            if (newDisableFalloff != ConfigManager.DisableFalloffShader)
            {
                ConfigManager.DisableFalloffShader = newDisableFalloff;
                ConfigManager.Save();
                GraphicsModule.Reset();
            }
            GUILayout.Label("(Replaces fog shader with standard texture)", GUILayout.Height(20));

            GUILayout.Space(10);
            GUILayout.Label($"Render Distance: {ConfigManager.RenderDistance:F0}m");
            float newRenderDist = GUILayout.HorizontalSlider(ConfigManager.RenderDistance, 11f, 1000f);
            if (Mathf.Abs(newRenderDist - ConfigManager.RenderDistance) > 0.1f)
            {
                ConfigManager.RenderDistance = newRenderDist;
                ConfigManager.Save();
                GraphicsModule.Reset();
            }

            GUILayout.Space(10);
            bool new2DBox = GUILayout.Toggle(ConfigManager.ESP2DBoxEnabled, "2D ESP Box (Fallback)");
            if (new2DBox != ConfigManager.ESP2DBoxEnabled) { ConfigManager.ESP2DBoxEnabled = new2DBox; ConfigManager.Save(); }

            GUILayout.Space(10);
            float farClipMeters = 11f + (ConfigManager.FarClipPlane - 0.01f) / (1.00f - 0.01f) * (1000f - 11f);
            GUILayout.Label($"Far Clip Plane: {farClipMeters:F1}m");
            float newFarClip = GUILayout.HorizontalSlider(ConfigManager.FarClipPlane, 0.01f, 1.00f);
            if (Mathf.Abs(newFarClip - ConfigManager.FarClipPlane) > 0.001f) ConfigManager.FarClipPlane = newFarClip;

            GUILayout.Space(20);
            GUILayout.Label("--- Monster Tracker Details ---", GUILayout.Height(20));

            // === ВОЗВРАЩЁННЫЙ ТОГГЛ MONSTER TRACKER ===
            bool newMonsterTracker = GUILayout.Toggle(ConfigManager.MonsterTrackerEnabled, "Monster Tracker (Bottom-right info)");
            if (newMonsterTracker != ConfigManager.MonsterTrackerEnabled)
            {
                ConfigManager.MonsterTrackerEnabled = newMonsterTracker;
                ConfigManager.Save();
            }
            GUILayout.Label("(Shows distance and direction to monster)", GUILayout.Height(20));
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
            GUILayout.Label("Path history: " + MonsterTracker.GetPathHistoryCount() + " points", GUILayout.Height(20));

            GUILayout.EndScrollView();
            // ==============================
        }

        private static void DrawSettingsTab()
        {
            GUILayout.Label("Menu Settings", GUILayout.Height(20));
            GUILayout.Space(10);
            GUILayout.Label("Current Menu Key: " + ConfigManager.MenuKey.ToString(), GUILayout.Height(20));
            GUILayout.Space(10);

            if (_isRebinding)
            {
                Color originalColor = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label(">>> Press any key... (Esc to cancel) <<<", GUILayout.Height(25));
                GUI.color = originalColor;
            }
            else
            {
                if (GUILayout.Button("Change Menu Key", GUILayout.Height(30))) _isRebinding = true;
            }

            GUILayout.Space(30);
            GUILayout.Label("Data Management", GUILayout.Height(20));
            GUILayout.Space(5);

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                ConfigManager.ResetToDefaults();
                WindowManager.ResetPosition();
                GraphicsModule.Reset();
                MelonLogger.Msg("HUDManager: Settings reset to defaults");
            }
        }

        public static bool _isRebinding = false;

        private static void DrawMonsterESP(Vector3 worldPosition)
        {
            var player = CustomSingleton<PlayerManager>.Instance?.Heart;
            if (player == null) return;

            Vector3 playerPos = player.transform.position;
            float distance = Vector3.Distance(playerPos, worldPosition);
            Vector3 directionToMonster = (worldPosition - playerPos).normalized;
            Vector3 playerForward = player.transform.forward;
            float angle = Vector3.Angle(playerForward, directionToMonster);

            string relativeDirection;
            if (angle < 45f) relativeDirection = "FRONT";
            else if (angle < 135f)
            {
                Vector3 cross = Vector3.Cross(playerForward, directionToMonster);
                relativeDirection = cross.y > 0 ? "RIGHT" : "LEFT";
            }
            else relativeDirection = "BACK";

            float boxWidth = 150f;
            float boxHeight = 60f;
            float padding = 10f;
            Rect espRect = new Rect(Screen.width - boxWidth - padding, Screen.height - boxHeight - padding, boxWidth, boxHeight);

            GUI.Box(espRect, "");
            Color originalColor = GUI.color;
            GUI.color = distance < 15f ? Color.red : Color.white;

            GUI.Label(new Rect(espRect.x + 5f, espRect.y + 5f, boxWidth - 10f, 20f), $"Distance: {distance:F1}m");
            GUI.Label(new Rect(espRect.x + 5f, espRect.y + 25f, boxWidth - 10f, 20f), $"Direction: {relativeDirection}");

            GUI.color = originalColor;
        }
    }
}