using UnityEngine;
using System.Collections.Generic;
using MelonLoader;

namespace EyesSpeedrunToolkit
{
    public static class BagsWallHack
    {
        private static bool _isEnabled = false;
        private static List<GameObject> _foundBags = new List<GameObject>();

        // Расширенный список префиксов для ВСЕХ карт
        private static readonly string[] BagPrefixes = new string[]
        { 
            // School и Mansion
            "sack1", "sack2", "Sack1", "Sack2",
            // Hospital
            "sackHospital1", "sackHospital2", "sackHospital3",
            // Общие для всех карт
            "MoneyBag", "moneybag", "moneyBags",
            "Bag", "bag"
        };

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

        public static void DrawBagIndicators()
        {
            if (!_isEnabled || Camera.main == null) return;

            foreach (GameObject bag in _foundBags)
            {
                if (bag == null) continue;

                // === ИСПРАВЛЕНИЕ: используем bounds.center вместо transform.position ===
                // Это даёт точную позицию визуальной части мешка, а не его родителя
                Vector3 bagPosition = GetBagVisualPosition(bag);

                Vector3 screenPos = Camera.main.WorldToScreenPoint(bagPosition);

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

        // Получаем визуальную позицию мешка через bounds
        private static Vector3 GetBagVisualPosition(GameObject obj)
        {
            // Пробуем найти Renderer (MeshRenderer, SpriteRenderer и т.д.)
            Renderer renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // bounds.center — это центр визуальной части объекта в мировых координатах
                return renderer.bounds.center;
            }

            // Если Renderer не найден, fallback на transform.position
            return obj.transform.position;
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