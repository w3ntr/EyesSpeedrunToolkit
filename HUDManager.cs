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

        private static bool _isDraggingTimer = false;
        private static Vector2 _timerDragOffset;

        private static Vector2 _cheatsScrollPosition;
        private static Vector2 _practiceScrollPosition;

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
        }

        public static void Update()
        {
            // === ОБРАБОТКА РЕБИНДА КЛАВИШ ===
            if (_isRebinding)
            {
                // Проверяем все клавиши
                foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kcode))
                    {
                        // Esc для отмены
                        if (kcode == KeyCode.Escape)
                        {
                            _isRebinding = false;
                            MelonLogger.Msg("HUDManager: Key rebinding cancelled");
                            return;
                        }

                        // Игнорируем служебные клавиши
                        if (kcode == KeyCode.None || kcode == KeyCode.Escape ||
                            kcode == KeyCode.LeftShift || kcode == KeyCode.RightShift ||
                            kcode == KeyCode.LeftControl || kcode == KeyCode.RightControl ||
                            kcode == KeyCode.LeftAlt || kcode == KeyCode.RightAlt)
                        {
                            return;
                        }

                        // Применяем новую клавишу
                        ConfigManager.MenuKey = kcode;
                        ConfigManager.Save();
                        _isRebinding = false;
                        MelonLogger.Msg($"HUDManager: Menu key changed to {kcode}");
                        return;
                    }
                }
                // Если в режиме ребинда — дальше не идём
                return;
            }
            // ================================

            // Открываем/закрываем меню (только если НЕ в режиме ребинда)
            if (Input.GetKeyDown(ConfigManager.MenuKey))
            {
                _menuOpen = !_menuOpen;
                MelonLogger.Msg("HUDManager: Menu " + (_menuOpen ? "opened" : "closed"));
            }

            // Применяем настройки (КАЖДЫЙ КАДР!)
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

            if (settingsChanged)
            {
                GraphicsModule.Reset();
                MelonLogger.Msg("HUDManager: Settings changed, applying...");
            }
        }

        public static void OnGUI()
        {
            UITheme.EnsureInitialized();

            // 1. Управление курсором
            if (_menuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // ТАЙМЕР (рисуем ТОЛЬКО в игровых сценах)
            if (ConfigManager.TimerEnabled)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                bool isGameScene = currentScene == "School" || currentScene == "Hospital" || currentScene == "Mansion";
                bool isWinScene = currentScene == "PostGameAd" || currentScene == "Win" || currentScene == "Outro";

                if (isGameScene || isWinScene)
                {
                    string timeText = "00:00";
                    if (SpeedrunTimer.IsRunning())
                    {
                        timeText = SpeedrunTimer.GetCurrentTimeFormatted();
                    }
                    else if (SpeedrunTimer.IsFinished())
                    {
                        timeText = SpeedrunTimer.GetCurrentTimeFormatted() + " (FINISHED)";
                    }

                    Rect timerRect = new Rect(ConfigManager.TimerX, ConfigManager.TimerY, 200f, 40f); // Увеличили высоту до 40

                    // Перетаскивание таймера
                    if (Event.current.type == EventType.MouseDown && timerRect.Contains(Event.current.mousePosition))
                    {
                        _isDraggingTimer = true;
                        _timerDragOffset = new Vector2(Event.current.mousePosition.x - ConfigManager.TimerX,
                                                       Event.current.mousePosition.y - ConfigManager.TimerY);
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

                    // === СТИЛЬНАЯ ОТРИСОВКА ТАЙМЕРА (плашка + моноширинный текст) ===
                    float tw = 190f, th = 42f;
                    Rect tbg = new Rect(ConfigManager.TimerX, ConfigManager.TimerY, tw, th);

                    UITheme.Solid(tbg, new Color(0f, 0f, 0f, 0.55f));                      // подложка
                    UITheme.Line(new Rect(tbg.x, tbg.y, 3f, th), UITheme.AccentColor);      // акцентная полоска слева

                    var timeStyle = TimerTextStyle();
                    timeStyle.normal.textColor = SpeedrunTimer.IsFinished() ? new Color(0.5f, 1f, 0.6f) : Color.white;

                    GUI.Label(tbg, timeText, timeStyle);

                    if (_isDraggingTimer)
                    {
                        // рамка-подсветка при перетаскивании
                        UITheme.Line(new Rect(tbg.x, tbg.y, tw, 1f), UITheme.AccentColor);
                        UITheme.Line(new Rect(tbg.x, tbg.yMax - 1f, tw, 1f), UITheme.AccentColor);
                    }
                }
            }

            // 2D ESP (Стрелка на монстра)
            if (ConfigManager.MonsterTrackerEnabled)
            {
                Vector3 monsterPos = MonsterTracker.GetCurrentPosition();
                if (monsterPos != Vector3.zero)
                {
                    DrawMonsterESP(monsterPos);
                }
            }

            // МЕНЮ (делегировано WindowManager)
            if (_menuOpen)
            {
                WindowManager.Draw("Eyes Speedrun Toolkit", DrawWindowContent);
            }

            // 2D ESP BOX (Рисуется поверх всего)
            if (ConfigManager.ESP2DBoxEnabled)
            {
                Vector3 monsterPos = MonsterTracker.GetCurrentPosition();
                if (monsterPos != Vector3.zero && Camera.main != null)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(monsterPos);
                    if (screenPos.z > 0)
                    {
                        float boxSize = 40f;
                        float guiX = screenPos.x - boxSize / 2f;
                        float guiY = Screen.height - screenPos.y - boxSize / 2f;

                        // Полупрозрачный красный заливной квадрат + рамка
                        UITheme.Solid(new Rect(guiX, guiY, boxSize, boxSize), new Color(1f, 0.25f, 0.25f, 0.35f));
                        UITheme.Line(new Rect(guiX, guiY, boxSize, 2f), Color.red);
                        UITheme.Line(new Rect(guiX, guiY + boxSize - 2f, boxSize, 2f), Color.red);

                        float dist = MonsterTracker.GetDistanceToPlayer();
                        if (dist > 0)
                        {
                            var distStyle = TimerTextStyle();
                            distStyle.fontSize = 14;
                            distStyle.normal.textColor = Color.red;
                            GUI.Label(new Rect(guiX - 20f, guiY + boxSize + 2f, 80f, 18f), $"{dist:F1}m", distStyle);
                        }
                    }
                }
            }

            // BAGS WALLHACK INDICATORS (отдельно, не внутри ESP2DBox)
            BagsWallHack.DrawBagIndicators();

            // ==========================================
            // ТАЙМЕР РЕГЕНЕРАЦИИ СТАМИНЫ (QoL)
            // ВЫНЕСЕН ОТДЕЛЬНО, НЕ ЗАВИСИТ ОТ ESP2DBox!
            // ==========================================
            if (ConfigManager.StaminaTimerEnabled && !string.IsNullOrEmpty(PlayerModifiers.StaminaTimerText))
            {
                var stStyle = TimerTextStyle();
                stStyle.fontSize = 16;
                float textW = stStyle.CalcSize(new GUIContent(PlayerModifiers.StaminaTimerText)).x + 24f;
                float height = 30f;
                float x = (Screen.width - textW) / 2f;
                float y = Screen.height - 150f;

                // скруглённая-looking плашка: фон + тонкая верхняя линия
                UITheme.Solid(new Rect(x, y, textW, height), new Color(0f, 0f, 0f, 0.6f));
                UITheme.Line(new Rect(x, y, textW, 1f), UITheme.AccentColor);

                GUI.Label(new Rect(x, y, textW, height), PlayerModifiers.StaminaTimerText, stStyle);
            }
            // === ВОТЕРМАРК CHEATS ACTIVE (реагирует на состояние любого чита из Cheats/Practice) ===
            if (IsAnyCheatEnabled())
            {
                string activeList = GetActiveCheatsList();

                var wmTitle = new GUIStyle(GUI.skin.label);
                wmTitle.fontSize = 14;
                wmTitle.fontStyle = FontStyle.Bold;
                wmTitle.normal.textColor = new Color(1f, 0.45f, 0.45f, 1f);

                var wmList = new GUIStyle(GUI.skin.label);
                wmList.fontSize = 12;
                wmList.normal.textColor = new Color(1f, 0.75f, 0.6f, 0.95f);

                string titleText = "CHEATS ACTIVE (Practice Run)";
                string listText = string.IsNullOrEmpty(activeList) ? "" : activeList;

                float w1 = wmTitle.CalcSize(new GUIContent(titleText)).x;
                float w2 = listText.Length > 0 ? wmList.CalcSize(new GUIContent(listText)).x : 0f;
                float panelW = Mathf.Max(w1, w2) + 26f;
                // Высота по реальному размеру текста (с запасом), чтобы нижняя строка не обрезалась
                float titleH = wmTitle.CalcSize(new GUIContent(titleText)).y;
                float listH = listText.Length > 0 ? wmList.CalcSize(new GUIContent(listText)).y : 0f;
                float panelH = 8f + titleH + listH;
                // Чуть выше нижнего края экрана (было 10px — половина второй строки уходила за край)
                Rect panel = new Rect(10f, Screen.height - panelH - 30f, panelW, panelH);

                UITheme.Solid(panel, new Color(0.15f, 0.03f, 0.03f, 0.75f));                                  // тёмно-красная плашка
                UITheme.Line(new Rect(panel.x, panel.y, 3f, panel.height), new Color(1f, 0.35f, 0.35f, 1f));   // красная акцентная полоска

                GUI.Label(new Rect(panel.x + 12f, panel.y + 4f, panelW - 12f, titleH), titleText, wmTitle);
                if (listText.Length > 0)
                    GUI.Label(new Rect(panel.x + 12f, panel.y + 4f + titleH, panelW - 12f, listH), listText, wmList);
            }
            // ==========================================
        }

        private static void DrawWindowContent(int windowID)
        {
            // === ТАБЫ: рисуем через GUILayout ПОСЛЕ отступа под заголовок окна,
            // иначе GUI.Window titlebar перекрывает первую строку контента. ===
            GUILayout.Space(6);

            string[] tabs = new string[] { "QoL", "Practice", "Cheats", "Settings" };

            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabs.Length; i++)
            {
                if (i > 0) GUILayout.Space(4);
                GUIStyle st = (_selectedTab == i) ? UITheme.TabActiveStyle : UITheme.TabStyle;
                if (GUILayout.Button(tabs[i], st, GUILayout.Height(26f), GUILayout.ExpandWidth(true)))
                {
                    _selectedTab = i;
                    _cheatsScrollPosition = Vector2.zero;
                    _practiceScrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            switch (_selectedTab)
            {
                case 0: DrawQoLTab(); break;
                case 1: DrawPracticeTab(); break;
                case 2: DrawCheatsTab(); break;
                case 3: DrawSettingsTab(); break;
            }
        }

        public static void CloseMenu() => _menuOpen = false;

        // === ХЕЛПЕРЫ ОФОРМЛЕНИЯ ===

        /// Заголовок секции. Полоска слева рисуется плашкой (Solid), а не
        /// GUILayout.Box — Box без background-текстуры рисует чёрный квадрат-заглушку.
        /// Rect увеличен до 20px + padding сверху/снизу, чтобы жирный шрифт 14px
        /// не обрезался снизу и заголовки дышали.
        private static void SectionHeader(string title)
        {
            GUILayout.Space(10);
            Rect r = GUILayoutUtility.GetRect(0, 22f, GUILayout.ExpandWidth(true));
            UITheme.Solid(new Rect(r.x, r.y + 4f, 3f, 14f), UITheme.AccentColor);
            GUI.Label(new Rect(r.x + 9f, r.y, r.width - 9f, 22f), title, UITheme.SectionStyle);
            GUILayout.Space(14);
        }

        /// Чекбокс-строка: подсказка мелким шрифтом под ним. (используется в новых вкладках)
        private static bool ToggleRow(string label, bool value, string hint = null)
        {
            bool newVal = GUILayout.Toggle(value, "  " + label, UITheme.ToggleFor(value), GUILayout.MinHeight(22));
            if (!string.IsNullOrEmpty(hint))
                GUILayout.Label(hint, UITheme.HintStyle);
            return newVal;
        }

        /// Моноширинный стиль для цифр (таймеры, ESP). Кэшируется.
        private static GUIStyle _timerStyleCache;
        private static Font _monoFontCache;

        /// Пытается найти системный моноширинный шрифт (Consolas / Courier New / Liberation Mono).
        /// В Unity 2019/2020 (MelonLoader) у Font НЕТ GetDefault(), поэтому ищем через OS шрифты.
        private static Font MonoFont()
        {
            if (_monoFontCache != null) return _monoFontCache;

            try
            {
                string[] candidates = { "Consolas", "Courier New", "Liberation Mono", "DejaVu Sans Mono", "Lucida Console" };
                var available = Font.CreateDynamicFontFromOSFont(candidates, 22);
                if (available != null)
                {
                    _monoFontCache = available;
                    return _monoFontCache;
                }
            }
            catch { /* если ни один не найден — используем дефолтный шрифт GUI */ }

            _monoFontCache = null; // null => Unity возьмёт стандартный шрифт скина
            return _monoFontCache;
        }

        private static GUIStyle TimerTextStyle()
        {
            if (_timerStyleCache == null)
            {
                _timerStyleCache = new GUIStyle(GUI.skin.label);
                var f = MonoFont();
                if (f != null) _timerStyleCache.font = f; // иначе останется Aileron из GUI.skin
                _timerStyleCache.fontSize = 22;
                _timerStyleCache.fontStyle = FontStyle.Bold;
                _timerStyleCache.alignment = TextAnchor.MiddleCenter;
                _timerStyleCache.normal.textColor = Color.white;
            }
            return _timerStyleCache;
        }

        private static void DrawQoLTab()
        {
            SectionHeader("QUALITY OF LIFE  |  Allowed on Speedrun.com");
            GUILayout.Space(2);

            bool newDisablePP = GUILayout.Toggle(ConfigManager.DisablePostProcessing, "  Disable Post-Processing (Vignette, Bloom, etc.)", UITheme.ToggleFor(ConfigManager.DisablePostProcessing));
            if (newDisablePP != ConfigManager.DisablePostProcessing) { ConfigManager.DisablePostProcessing = newDisablePP; }

            GUILayout.Space(4);
            bool newDisableVSync = GUILayout.Toggle(ConfigManager.DisableVSync, "  Disable V-Sync", UITheme.ToggleFor(ConfigManager.DisableVSync));
            if (newDisableVSync != ConfigManager.DisableVSync)
            {
                ConfigManager.DisableVSync = newDisableVSync;
                GraphicsModule.Reset();
            }

            if (ConfigManager.DisableVSync)
            {
                GUILayout.Space(6);
                string fpsLabel = ConfigManager.CustomFPS > 0 ? $"FPS Limit: {ConfigManager.CustomFPS}" : "FPS Limit: Unlimited";
                GUILayout.Label(fpsLabel, UITheme.LabelStyle);

                float fpsFloat = ConfigManager.CustomFPS;
                float newFpsFloat = GUILayout.HorizontalSlider(fpsFloat, 0f, 300f, GUILayout.Height(14));
                int newFps = Mathf.RoundToInt(newFpsFloat);

                if (newFps != ConfigManager.CustomFPS)
                {
                    ConfigManager.CustomFPS = newFps;
                    ConfigManager.Save();
                    GraphicsModule.Reset();
                    MelonLogger.Msg($"HUDManager: FPS limit changed to {newFps}");
                }

                GUILayout.Label("(0 = Unlimited, 30-300 = FPS cap)", UITheme.HintStyle);
            }

            GUILayout.Space(6);
            bool newTimerEnabled = GUILayout.Toggle(ConfigManager.TimerEnabled, "  Show Speedrun Timer", UITheme.ToggleFor(ConfigManager.TimerEnabled));
            if (newTimerEnabled != ConfigManager.TimerEnabled) { ConfigManager.TimerEnabled = newTimerEnabled; }

            // === Тоггл миллисекунд ===
            if (ConfigManager.TimerEnabled)
            {
                GUILayout.Space(4);
                bool newShowMs = GUILayout.Toggle(ConfigManager.ShowMilliseconds, "      Show Milliseconds", UITheme.ToggleFor(ConfigManager.ShowMilliseconds));
                if (newShowMs != ConfigManager.ShowMilliseconds)
                {
                    ConfigManager.ShowMilliseconds = newShowMs;
                    ConfigManager.Save();
                }
                GUILayout.Label("      (Format: MM:SS.mmm)", UITheme.HintStyle);
            }

            GUILayout.Space(10);
            SectionHeader("FOV");
            GUILayout.Label($"Target FOV: {ConfigManager.TargetFOV:F0}", UITheme.LabelStyle);
            float newFov = GUILayout.HorizontalSlider(ConfigManager.TargetFOV, 60f, 140f, GUILayout.Height(14));

            if (Mathf.Abs(newFov - ConfigManager.TargetFOV) > 0.1f)
            {
                ConfigManager.TargetFOV = newFov;
                ConfigManager.Save();
                GraphicsModule.Reset();
            }

            // === Таймер регенерации стамины ===
            GUILayout.Space(10);
            SectionHeader("DISPLAY");
            bool newStaminaTimer = GUILayout.Toggle(ConfigManager.StaminaTimerEnabled, "  Show Stamina Regen Timer", UITheme.ToggleFor(ConfigManager.StaminaTimerEnabled));
            if (newStaminaTimer != ConfigManager.StaminaTimerEnabled)
            {
                ConfigManager.StaminaTimerEnabled = newStaminaTimer;
                ConfigManager.Save();
            }
            GUILayout.Label("(Shows time until stamina is full)", UITheme.HintStyle);

            GUILayout.Space(4);
            bool newHideHUD = GUILayout.Toggle(ConfigManager.HideHUDEnabled, "  Hide Game HUD", UITheme.ToggleFor(ConfigManager.HideHUDEnabled));
            if (newHideHUD != ConfigManager.HideHUDEnabled)
            {
                ConfigManager.HideHUDEnabled = newHideHUD;
                ConfigManager.Save();
                if (ConfigManager.HideHUDEnabled) HUDToggle.Enable();
                else HUDToggle.Disable();
            }
            GUILayout.Label("(Removes eye icon, bag counter, and other UI elements)", UITheme.HintStyle);
        }

        private static void DrawPracticeTab()
        {
            GUILayout.Label("[!] PRACTICE ONLY - BANNED ON SPEEDRUN.COM!", UITheme.WarningStyle);
            GUILayout.Label("For route learning only. Using these invalidates leaderboard runs.", UITheme.HintStyle);
            GUILayout.Space(10);

            // Умный скроллбар
            float availableHeight = WindowManager.WindowRect.height - 150f;
            float scrollHeight = Mathf.Clamp(availableHeight, 150f, 600f);
            _practiceScrollPosition = GUILayout.BeginScrollView(_practiceScrollPosition, GUILayout.Height(scrollHeight));

            // Bags Wallhack
            bool newBagsWH = GUILayout.Toggle(ConfigManager.BagsWallHackEnabled, "  Bags Wallhack (See sacks through walls)", UITheme.ToggleFor(ConfigManager.BagsWallHackEnabled));
            if (newBagsWH != ConfigManager.BagsWallHackEnabled)
            {
                ConfigManager.BagsWallHackEnabled = newBagsWH;
                ConfigManager.Save();
                if (ConfigManager.BagsWallHackEnabled) BagsWallHack.Enable();
                else BagsWallHack.Disable();
            }
            GUILayout.Label("(Highlights all sacks in green through walls)", UITheme.HintStyle);

            GUILayout.Space(8);

            // Infinite Stamina
            bool newInfStamina = GUILayout.Toggle(ConfigManager.InfiniteStaminaEnabled, "  Infinite Stamina", UITheme.ToggleFor(ConfigManager.InfiniteStaminaEnabled));
            if (newInfStamina != ConfigManager.InfiniteStaminaEnabled)
            {
                ConfigManager.InfiniteStaminaEnabled = newInfStamina;
                ConfigManager.Save();
            }
            GUILayout.Label("(Stamina never drains)", UITheme.HintStyle);

            GUILayout.Space(8);
            bool newFreecam = GUILayout.Toggle(ConfigManager.FreecamEnabled, "  FreeCam (Detach camera)", UITheme.ToggleFor(ConfigManager.FreecamEnabled));
            if (newFreecam != ConfigManager.FreecamEnabled)
            {
                ConfigManager.FreecamEnabled = newFreecam;
                ConfigManager.Save();
                if (ConfigManager.FreecamEnabled) FreecamModule.IsEnabled = true;
                else FreecamModule.IsEnabled = false;
            }
            GUILayout.Label("(WASD to fly, Mouse to look, Shift for speed boost)", UITheme.HintStyle);

            bool newJump = GUILayout.Toggle(ConfigManager.JumpEnabled, "Enable Jump (Space)");
            if (newJump != ConfigManager.JumpEnabled)
            {
                ConfigManager.JumpEnabled = newJump;
                ConfigManager.Save();

                // Синхронизируем с модулем!
                if (newJump)
                    JumpModule.Enable();
                else
                    JumpModule.Disable();
            }
            GUILayout.Label("(Press Space to jump when on ground)", GUILayout.Height(20));

            GUILayout.EndScrollView();
        }

        private static void DrawCheatsTab()
        {
            GUILayout.Label("[!] BANNED ON SPEEDRUN.COM!", UITheme.WarningStyle);
            GUILayout.Label("Unfair advantage. Use ONLY for practice or casual play.", UITheme.HintStyle);
            GUILayout.Space(10);

            // Умный скроллбар
            float availableHeight = WindowManager.WindowRect.height - 150f;
            float scrollHeight = Mathf.Clamp(availableHeight, 150f, 600f);
            _cheatsScrollPosition = GUILayout.BeginScrollView(_cheatsScrollPosition, GUILayout.Height(scrollHeight));

            SectionHeader("VISUAL");

            bool newESPEnabled = GUILayout.Toggle(ConfigManager.ESPEnabled, "  3D ESP (Wallhack)", UITheme.ToggleFor(ConfigManager.ESPEnabled));
            if (newESPEnabled != ConfigManager.ESPEnabled)
            {
                ConfigManager.ESPEnabled = newESPEnabled;
                ConfigManager.Save();
                if (ConfigManager.ESPEnabled) MonsterTracker.EnableESP();
                else MonsterTracker.DisableESP();
            }
            GUILayout.Label("(Monster visible through walls)", UITheme.HintStyle);

            GUILayout.Space(4);
            bool new2DBox = GUILayout.Toggle(ConfigManager.ESP2DBoxEnabled, "  2D ESP Box (Fallback)", UITheme.ToggleFor(ConfigManager.ESP2DBoxEnabled));
            if (new2DBox != ConfigManager.ESP2DBoxEnabled)
            {
                ConfigManager.ESP2DBoxEnabled = new2DBox;
                ConfigManager.Save();
            }

            GUILayout.Space(8);
            bool newDisableFalloff = GUILayout.Toggle(ConfigManager.DisableFalloffShader, "  Disable Distance Fog (Falloff Shader)", UITheme.ToggleFor(ConfigManager.DisableFalloffShader));
            if (newDisableFalloff != ConfigManager.DisableFalloffShader)
            {
                ConfigManager.DisableFalloffShader = newDisableFalloff;
                ConfigManager.Save();
                GraphicsModule.Reset();
            }
            GUILayout.Label("(Replaces fog shader with standard texture)", UITheme.HintStyle);

            GUILayout.Space(4);
            bool newDisableFade = GUILayout.Toggle(ConfigManager.DisableFadeEnabled, "  Disable Spawn Fade (Blackout)", UITheme.ToggleFor(ConfigManager.DisableFadeEnabled));
            if (newDisableFade != ConfigManager.DisableFadeEnabled)
            {
                ConfigManager.DisableFadeEnabled = newDisableFade;
                ConfigManager.Save();
                FadeModule.Reset(); // Сбросить поиск, чтобы применить сразу
            }
            GUILayout.Label("(Removes black screen when spawning/dying)", UITheme.HintStyle);

            GUILayout.Space(8);
            GUILayout.Label($"Render Distance: {ConfigManager.RenderDistance:F0}m", UITheme.LabelStyle);
            float newRenderDist = GUILayout.HorizontalSlider(ConfigManager.RenderDistance, 11f, 1000f, GUILayout.Height(14));
            if (Mathf.Abs(newRenderDist - ConfigManager.RenderDistance) > 0.1f)
            {
                ConfigManager.RenderDistance = newRenderDist;
                ConfigManager.Save();
                GraphicsModule.Reset();
            }
            GUILayout.Label("(11m = original, 1000m = maximum)", UITheme.HintStyle);

            GUILayout.Space(12);
            SectionHeader("MONSTER TRACKER");

            bool newMonsterTracker = GUILayout.Toggle(ConfigManager.MonsterTrackerEnabled, "  Monster Tracker (Bottom-right info)", UITheme.ToggleFor(ConfigManager.MonsterTrackerEnabled));
            if (newMonsterTracker != ConfigManager.MonsterTrackerEnabled)
            {
                ConfigManager.MonsterTrackerEnabled = newMonsterTracker;
                ConfigManager.Save();
            }
            GUILayout.Label("(Shows distance and direction to monster)", UITheme.HintStyle);

            float dist = MonsterTracker.GetDistanceToPlayer();
            var mono = new GUIStyle(UITheme.LabelStyle);
            if (dist >= 0)
            {
                GUILayout.Label($"Distance: {dist:F1}m", mono);
                GUILayout.Label($"Position: {MonsterTracker.GetCurrentPosition().ToString("F1")}", mono);
            }
            else
            {
                GUILayout.Label("Status: Searching or not found", UITheme.HintStyle);
            }

            GUILayout.Label($"Path history: {MonsterTracker.GetPathHistoryCount()} points", UITheme.HintStyle);

            GUILayout.EndScrollView();
        }

        private static void DrawSettingsTab()
        {
            SectionHeader("MENU");
            GUILayout.Space(2);

            // Карточка с текущей клавишей
            var keyStyle = new GUIStyle(UITheme.LabelStyle);
            keyStyle.alignment = TextAnchor.MiddleCenter;
            Rect cardRect = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
            UITheme.Solid(cardRect, UITheme.PanelColor);
            GUI.Label(new Rect(cardRect.x, cardRect.y + 4, cardRect.width, 18), "Current Menu Key", UITheme.HintStyle);
            var bigKey = TimerTextStyle();
            bigKey.fontSize = 18;
            bigKey.normal.textColor = UITheme.AccentColor;
            GUI.Label(new Rect(cardRect.x, cardRect.y + 22, cardRect.width, 22), ConfigManager.MenuKey.ToString(), bigKey);

            GUILayout.Space(6);
            if (_isRebinding)
            {
                GUILayout.Label(">>> Press any key to bind... (Esc to cancel)", UITheme.WarningStyle);
            }
            else
            {
                if (GUILayout.Button("Change Menu Key", UITheme.ButtonStyle, GUILayout.Height(28)))
                {
                    _isRebinding = true;
                }
            }

            GUILayout.Space(14);
            SectionHeader("DATA");

            if (GUILayout.Button("Reset to Defaults", UITheme.ButtonStyle, GUILayout.Height(28)))
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
            if (angle < 45f)
            {
                relativeDirection = "FRONT";
            }
            else if (angle < 135f)
            {
                Vector3 cross = Vector3.Cross(playerForward, directionToMonster);
                if (cross.y > 0)
                {
                    relativeDirection = "RIGHT";
                }
                else
                {
                    relativeDirection = "LEFT";
                }
            }
            else
            {
                relativeDirection = "BACK";
            }

            float boxWidth = 170f;
            float boxHeight = 58f;
            float padding = 12f;
            Rect espRect = new Rect(
                Screen.width - boxWidth - padding,
                Screen.height - boxHeight - padding,
                boxWidth,
                boxHeight
            );

            // Плашка + акцентная полоска слева
            UITheme.Solid(espRect, new Color(0f, 0f, 0f, 0.6f));
            UITheme.Line(new Rect(espRect.x, espRect.y, 3f, boxHeight), distance < 15f ? Color.red : UITheme.AccentColor);

            var lblStyle = TimerTextStyle();
            lblStyle.fontSize = 14;
            lblStyle.alignment = TextAnchor.MiddleLeft;
            lblStyle.normal.textColor = distance < 15f ? new Color(1f, 0.4f, 0.4f) : Color.white;

            float textY = espRect.y + 6f;
            GUI.Label(new Rect(espRect.x + 12f, textY, boxWidth - 16f, 22f), $"DIST   {distance:F1} m", lblStyle);
            GUI.Label(new Rect(espRect.x + 12f, textY + 22f, boxWidth - 16f, 22f), $"DIR      {relativeDirection}", lblStyle);
        }
        private static bool IsAnyCheatEnabled()
        {
            // Проверяем все функции из вкладок Cheats и Practice
            return ConfigManager.ESPEnabled ||
                   ConfigManager.FreecamEnabled ||
                   ConfigManager.InfiniteStaminaEnabled ||
                   ConfigManager.BagsWallHackEnabled ||
                   ConfigManager.DisableFalloffShader;
        }

        // Человекочитаемый список активных читов для ватермарки
        private static string GetActiveCheatsList()
        {
            var list = new System.Collections.Generic.List<string>();
            if (ConfigManager.ESPEnabled)              list.Add("3D ESP");
            if (ConfigManager.ESP2DBoxEnabled)         list.Add("2D ESP Box");
            if (ConfigManager.BagsWallHackEnabled)     list.Add("Bags Wallhack");
            if (ConfigManager.InfiniteStaminaEnabled)  list.Add("Inf. Stamina");
            if (ConfigManager.FreecamEnabled)          list.Add("Freecam");
            if (ConfigManager.MonsterTrackerEnabled)   list.Add("Monster Tracker");
            if (ConfigManager.DisableFalloffShader)    list.Add("No Fog");
            if (ConfigManager.DisableFadeEnabled)      list.Add("No Fade");
            return string.Join(", ", list);
        }
    }
}