using UnityEngine;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace EyesSpeedrunToolkit
{
    public static class SpeedrunTimer
    {
        private static float _elapsedTime = 0f;
        private static bool _isRunning = false;
        private static bool _isFinished = false;

        public static void Update()
        {
            // 1. ЖЁСТКАЯ ПРОВЕРКА СЦЕНЫ
            string currentScene = SceneManager.GetActiveScene().name;
            bool isGameScene = currentScene == "School" || currentScene == "Hospital" || currentScene == "Mansion";

            // Если мы в меню, заставке или уже вышли, время НЕ прибавляется
            if (!isGameScene)
            {
                return;
            }

            // 2. Логика тикания времени
            if (_isRunning && !_isFinished)
            {
                _elapsedTime += Time.deltaTime;
            }
        }

        // Форматирование времени
        public static string GetFormattedTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int secs = totalSeconds % 60;

            if (hours > 0)
            {
                return $"{hours:D2}:{minutes:D2}:{secs:D2}";
            }
            else
            {
                return $"{minutes:D2}:{secs:D2}";
            }
        }

        // Запустить таймер
        public static void Start()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "School" || currentScene == "Hospital" || currentScene == "Mansion")
            {
                _isRunning = true;
                _isFinished = false;
                _elapsedTime = 0f; // Сбрасываем накопленное время при новом старте
            }
        }

        // Остановить таймер (например, при смерти или выходе)
        public static void Stop()
        {
            _isRunning = false;
            _isFinished = true;
            MelonLogger.Msg($"SpeedrunTimer: Stopped at {GetCurrentTimeFormatted()}");
        }

        // Сбросить таймер
        public static void Reset()
        {
            _elapsedTime = 0f;
            _isRunning = false;
            _isFinished = false;
            MelonLogger.Msg("SpeedrunTimer: Reset");
        }

        // Получить текущее время (просто возвращаем накопленное)
        public static float GetCurrentTime()
        {
            return _elapsedTime;
        }

        // Получить текущее время в формате строки
        public static string GetCurrentTimeFormatted()
        {
            return GetFormattedTime(GetCurrentTime());
        }

        // Проверить, запущен ли таймер
        public static bool IsRunning()
        {
            return _isRunning;
        }

        // Проверить, завершён ли таймер
        public static bool IsFinished()
        {
            return _isFinished;
        }

        // Завершить таймер (при сборе последнего мешка)
        public static void Finish()
        {
            _isFinished = true;
            _isRunning = false;
            MelonLogger.Msg($"SpeedrunTimer: Finished at {GetCurrentTimeFormatted()}");
        }
    }
}