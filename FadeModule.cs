using UnityEngine;
using MelonLoader;
using System.Collections.Generic;

namespace EyesSpeedrunToolkit
{
    public static class FadeModule
    {
        private static List<GameObject> _fadeObjects = new List<GameObject>();
        private static bool _searched = false;

        public static void Initialize()
        {
            MelonLogger.Msg("FadeModule: Initialized");
        }

        public static void Update()
        {
            if (!ConfigManager.DisableFadeEnabled) return;

            // Ищем объекты один раз при включении тоггла
            if (!_searched)
            {
                FindFadeObjects();
                _searched = true;
            }

            // Отключаем их
            foreach (GameObject obj in _fadeObjects)
            {
                if (obj != null && obj.activeSelf)
                {
                    obj.SetActive(false);
                }
            }
        }

        private static void FindFadeObjects()
        {
            MelonLogger.Msg("FadeModule: Searching for fade objects...");
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;

                // Ищем Blackout и Fader
                if (obj.name == "Blackout" || obj.name == "Fader" || obj.name.Contains("Fade"))
                {
                    if (!_fadeObjects.Contains(obj))
                    {
                        _fadeObjects.Add(obj);
                        MelonLogger.Msg($"  Found fade object: {obj.name}");
                    }
                }
            }
            MelonLogger.Msg($"FadeModule: Total fade objects found: {_fadeObjects.Count}");
        }

        public static void Reset()
        {
            // При смене сцены сбрасываем поиск, чтобы найти объекты заново
            _fadeObjects.Clear();
            _searched = false;

            // Если тоггл выключен, включаем объекты обратно (на всякий случай)
            if (!ConfigManager.DisableFadeEnabled)
            {
                // Можно добавить логику включения, но обычно игра сама их включает при спавне
            }
        }
    }
}