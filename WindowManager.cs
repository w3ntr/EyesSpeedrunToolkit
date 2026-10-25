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

        public static void Draw(string title, Action<int> windowContent)
        {
            if (!_configLoaded)
            {
                LoadConfig();
                _configLoaded = true;
            }

            WindowRect.width = Mathf.Max(MinWindowWidth, WindowRect.width);
            WindowRect.height = Mathf.Max(MinWindowHeight, WindowRect.height);

            WindowRect = GUI.ModalWindow(0, WindowRect, (id) => WindowCallback(id, windowContent), title);
        }

        private static void WindowCallback(int id, Action<int> contentAction)
        {
            contentAction?.Invoke(id);

            // Область перетаскивания (исключаем правый нижний угол и крестик)
            GUI.DragWindow(new Rect(0, 0, WindowRect.width - 35f, 25f));

            HandleResize();

            // Сохраняем только при отпускании кнопки мыши (чтобы не спамить записью)
            if (Event.current.type == EventType.MouseUp)
            {
                SaveConfig();
            }
        }

        private static void HandleResize()
        {
            Rect resizeHandleRect = new Rect(
                WindowRect.width - ResizeHandleSize,
                WindowRect.height - ResizeHandleSize,
                ResizeHandleSize,
                ResizeHandleSize
            );

            // Рисуем видимый треугольник
            GUI.Label(resizeHandleRect, "◢");

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