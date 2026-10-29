using UnityEngine;
using System;

namespace EyesSpeedrunToolkit
{
    public static class WindowManager
    {
        // Публичное поле для HUDManager
        public static Rect WindowRect = new Rect(Screen.width / 2f - 300f, Screen.height / 2f - 250f, 600f, 500f);

        private static bool _isResizing = false;
        private static Vector2 _resizeStartMouse;
        private static Vector2 _resizeStartSize;
        private const float ResizeHandleSize = 20f;
        private const float MinWindowWidth = 500f;
        private const float MinWindowHeight = 400f;
        private static bool _configLoaded = false;

        private const string PREF_X = "EST_WinX";
        private const string PREF_Y = "EST_WinY";
        private const string PREF_W = "EST_WinW";
        private const string PREF_H = "EST_WinH";

        // Кешированные стили (создаются один раз)
        private static GUIStyle _titleStyle;
        private static GUIStyle _hintStyle;
        private static GUIStyle _closeStyle;

        public static void Draw(string title, Action<int> windowContent)
        {
            UITheme.EnsureInitialized();

            if (!_configLoaded)
            {
                LoadConfig();
                _configLoaded = true;
            }

            WindowRect.width = Mathf.Max(MinWindowWidth, WindowRect.width);
            WindowRect.height = Mathf.Max(MinWindowHeight, WindowRect.height);

            // Пока зажата ЛКМ после клика по полосе заголовка — двигнем окно каждый кадр.
            HandleDrag(title);

            // Стили окна правим ДО вызова GUI.Window: border=0, иначе Unity смещает
            // систему координат колбэка внутрь окна и всё рисование уезжает.
            UITheme.WindowStyle.padding = new RectOffset(0, 0, 0, 0);
            UITheme.WindowStyle.margin = new RectOffset(0, 0, 0, 0);
            UITheme.WindowStyle.border = new RectOffset(0, 0, 0, 0);

            GUI.Window(0, WindowRect, (id) => WindowCallback(id, windowContent, title), GUIContent.none, UITheme.WindowStyle);
        }

        private const float TitleBarHeight = 28f;

        // === Ручной drag (100% рабочий вариант) ===
        // Начало: EventType.MouseDown внутри полосы заголовка (проверяем вручную,
        // без GUI.DragWindow — тот ломался из-за clipControlRect/GUILayout).
        // Движение: каждый кадр в OnGUI пока зажата ЛКМ (Input.GetMouseButton).
        // Завершение: MouseUp — сохраняем позицию.
        private static bool _dragging = false;
        private static Vector2 _dragOffset; // смещение курсора (экран) относительно левого верхнего угла окна

        private static void HandleDrag(string title)
        {
            Vector2 mouseScreen = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            if (_dragging)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    _dragging = false;
                    SaveConfig();
                }
                else
                {
                    Vector2 newPos = mouseScreen - _dragOffset;
                    // Не выпускаем окно за пределы экрана (чтобы полоса заголовка всегда была доступна)
                    newPos.x = Mathf.Clamp(newPos.x, -WindowRect.width + 80f, Screen.width - 80f);
                    newPos.y = Mathf.Clamp(newPos.y, 0f, Screen.height - TitleBarHeight);
                    WindowRect.x = newPos.x;
                    WindowRect.y = newPos.y;
                }
            }
        }

        private static void WindowCallback(int id, Action<int> contentAction, string title)
        {
            float w = WindowRect.width;

            // === Полоса заголовка (рисуем вручную, без контролов — чтобы ничего
            // не перехватывало события). Solid/Line internally рисуют через GUI.DrawTexture,
            // они НЕ блокируют клики. ===
            UITheme.Solid(new Rect(0f, 0f, w, TitleBarHeight), UITheme.Lift(UITheme.BgColor, 0.10f));
            UITheme.Line(new Rect(0f, TitleBarHeight - 1f, w, 1f), UITheme.PanelColor);

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label);
                _titleStyle.fontStyle = FontStyle.Bold;
                _titleStyle.alignment = TextAnchor.MiddleLeft;
                _titleStyle.fontSize = 13;
                _titleStyle.padding = new RectOffset(0, 0, 0, 0);
                _titleStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            _titleStyle.normal.textColor = _dragging ? Color.white : UITheme.AccentColor;
            GUI.Label(new Rect(10f, 0f, w - 200f, TitleBarHeight), title, _titleStyle);

            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label);
                _hintStyle.fontSize = 11;
                _hintStyle.alignment = TextAnchor.MiddleRight;
                _hintStyle.padding = new RectOffset(0, 0, 0, 0);
                _hintStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            _hintStyle.normal.textColor = UITheme.DimColor;
            GUI.Label(new Rect(w - 165f, 0f, 130f, TitleBarHeight), "[ drag here ]", _hintStyle);

            // Кнопка закрытия
            Rect closeRect = new Rect(w - 30f, 4f, 24f, TitleBarHeight - 8f);
            bool hovering = closeRect.Contains(Event.current.mousePosition);
            if (hovering && Event.current.type == EventType.Repaint)
                UITheme.Solid(closeRect, UITheme.HoverColor);
            if (_closeStyle == null)
            {
                _closeStyle = new GUIStyle(GUI.skin.label);
                _closeStyle.alignment = TextAnchor.MiddleCenter;
                _closeStyle.fontStyle = FontStyle.Bold;
                _closeStyle.fontSize = 13;
                _closeStyle.padding = new RectOffset(0, 0, 0, 0);
                _closeStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            _closeStyle.normal.textColor = hovering ? UITheme.TextColor : UITheme.DimColor;
            GUI.Label(closeRect, "X", _closeStyle);
            if (Event.current.type == EventType.MouseDown && hovering)
            {
                HUDManager.CloseMenu();
                Event.current.Use();
            }

            // === Перетаскивание окна: ВРУЧНУЮ, без GUI.DragWindow ===
            // Работает по тому же принципу, что в твоём моде для другой игры
            // (клик по верхней полосе -> тянем окно), но зона drag проверяется
            // вручную на EventType.MouseDown. Это 100% надёжно: не зависит от
            // clipControlRect/GUILayout и от того, кто из контролов "Use()" событие раньше.
            if (Event.current.type == EventType.MouseDown)
            {
                Vector2 em = Event.current.mousePosition; // уже в координатах окна
                Rect dragZone = new Rect(0f, 0f, w - 34f, TitleBarHeight);
                Rect closeZone = new Rect(w - 30f, 4f, 24f, TitleBarHeight - 8f);
                if (dragZone.Contains(em) && !closeZone.Contains(em))
                {
                    _dragging = true;
                    // Смещение курсора (экранные координаты) относительно левого верхнего угла окна.
                    // em в координатах окна == экран минус позиция окна, поэтому offset = (em.x, em.y).
                    _dragOffset = em;
                    Event.current.Use();
                }
            }

            GUILayout.BeginArea(new Rect(12f, TitleBarHeight + 4f, w - 24f, WindowRect.height - TitleBarHeight - 30f));
            contentAction?.Invoke(id);
            GUILayout.EndArea();

            HandleResize();
        }

        private static void HandleResize()
        {
            Rect resizeHandleRect = new Rect(
                WindowRect.width - ResizeHandleSize,
                WindowRect.height - ResizeHandleSize,
                ResizeHandleSize,
                ResizeHandleSize
            );

            Color oldc = GUI.color;
            GUI.color = UITheme.DimColor;
            GUI.Label(resizeHandleRect, "//", UITheme.HintStyle);
            GUI.color = oldc;

            Vector2 mouseScreenPosition = new Vector2(
                Input.mousePosition.x,
                Screen.height - Input.mousePosition.y
            );

            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown && resizeHandleRect.Contains(currentEvent.mousePosition))
            {
                _isResizing = true;
                _resizeStartMouse = mouseScreenPosition;
                _resizeStartSize = new Vector2(WindowRect.width, WindowRect.height);
                currentEvent.Use();
            }

            if (_isResizing)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    _isResizing = false;
                    SaveConfig();
                }
                else
                {
                    Vector2 delta = mouseScreenPosition - _resizeStartMouse;
                    WindowRect.width = Mathf.Max(MinWindowWidth, _resizeStartSize.x + delta.x);
                    WindowRect.height = Mathf.Max(MinWindowHeight, _resizeStartSize.y + delta.y);
                }
            }
        }

        private static void LoadConfig()
        {
            float x = PlayerPrefs.GetFloat(PREF_X, Screen.width / 2f - 300f);
            float y = PlayerPrefs.GetFloat(PREF_Y, Screen.height / 2f - 250f);
            float w = PlayerPrefs.GetFloat(PREF_W, 600f);
            float h = PlayerPrefs.GetFloat(PREF_H, 500f);
            WindowRect = new Rect(x, y, w, h);
        }

        private static void SaveConfig()
        {
            PlayerPrefs.SetFloat(PREF_X, WindowRect.x);
            PlayerPrefs.SetFloat(PREF_Y, WindowRect.y);
            PlayerPrefs.SetFloat(PREF_W, WindowRect.width);
            PlayerPrefs.SetFloat(PREF_H, WindowRect.height);
            PlayerPrefs.Save();
        }

        public static void ResetPosition()
        {
            WindowRect = new Rect(Screen.width / 2f - 300f, Screen.height / 2f - 250f, 600f, 500f);
            SaveConfig();
        }
    }
}
