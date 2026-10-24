using UnityEngine;
using MelonLoader;
using System;

namespace EyesSpeedrunToolkit
{
    public static class SpeedrunTimer
    {
        private static float _startTime;
        private static bool _isRunning = false;
        private static bool _isFinished = false;

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
            _startTime = Time.time;
            _isRunning = true;
            _isFinished = false;
            MelonLogger.Msg("SpeedrunTimer: Started");
        }

        // Остановить таймер
        public static void Stop()
        {
            _isRunning = false;
            _isFinished = true;
            MelonLogger.Msg($"SpeedrunTimer: Stopped at {GetCurrentTimeFormatted()}");
        }

        // Сбросить таймер
        public static void Reset()
        {
            _startTime = 0f;
            _isRunning = false;
            _isFinished = false;
            MelonLogger.Msg("SpeedrunTimer: Reset");
        }

        // Получить текущее время
        public static float GetCurrentTime()
        {
            if (!_isRunning) return 0f;
            return Time.time - _startTime;
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
    }
}