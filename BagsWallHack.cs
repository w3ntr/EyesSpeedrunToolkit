using UnityEngine;
using System.Collections.Generic;
using MelonLoader;

namespace EyesSpeedrunToolkit
{
    public static class BagsWallHack
    {
        private static bool _isEnabled = false;
        private static List<GameObject> _foundBags = new List<GameObject>();

        private static readonly string[] BagPrefixes = new string[] { "sack1", "sack2" };

        public static void Update()
        {
            if (!_isEnabled) return;

            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isGameScene = currentScene == "School" || currentScene == "Hospital" || currentScene == "Mansion";

            if (!isGameScene) return;

            // Очищаем список и ищем все мешки
            _foundBags.Clear();
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;

                foreach (string prefix in BagPrefixes)
                {
                    if (obj.name.StartsWith(prefix))
                    {
                        _foundBags.Add(obj);
                        break;
                    }
                }
            }
        }

        // Этот метод вызывается из HUDManager.OnGUI() для рисования 2D-индикаторов
        public static void DrawBagIndicators()
        {
            if (!_isEnabled || Camera.main == null) return;

            foreach (GameObject bag in _foundBags)
            {
                if (bag == null) continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(bag.transform.position);

                // Проверяем, что мешок перед камерой
                if (screenPos.z > 0)
                {
                    float guiX = screenPos.x - 15f;
                    float guiY = Screen.height - screenPos.y - 15f;

                    // Рисуем зелёный квадрат 30x30
                    GUI.color = new Color(0f, 1f, 0f, 0.8f);
                    GUI.DrawTexture(new Rect(guiX, guiY, 30f, 30f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        public static void Enable()
        {
            _isEnabled = true;
            MelonLogger.Msg("BagsWallHack: Enabled (2D indicators mode)");
        }

        public static void Disable()
        {
            _isEnabled = false;
            _foundBags.Clear();
            MelonLogger.Msg("BagsWallHack: Disabled");
        }
    }
}