using UnityEngine;
using MelonLoader;

namespace EyesSpeedrunToolkit
{
    public static class SpeedrunTimer
    {
        private static bool _isRunning = false;
        private static bool _isFinished = false;
        private static float _elapsedTime = 0f;
        private static float _startTime = 0f;

        public static void Start()
        {
            _isRunning = true;
            _isFinished = false;
            _elapsedTime = 0f;
            _startTime = Time.time;
            MelonLogger.Msg("SpeedrunTimer: Started");
        }

        public static void Stop()
        {
            _isRunning = false;
            MelonLogger.Msg("SpeedrunTimer: Stopped");
        }

        public static void Finish()
        {
            _isRunning = false;
            _isFinished = true;
            MelonLogger.Msg($"SpeedrunTimer: Finished at {GetCurrentTimeFormatted()}");
        }

        public static void Reset()
        {
            _isRunning = false;
            _isFinished = false;
            _elapsedTime = 0f;
        }

        public static void Update()
        {
            if (!_isRunning) return;

            // Используем Time.deltaTime, но только когда игра не на паузе
            if (Time.timeScale > 0)
            {
                _elapsedTime += Time.deltaTime;
            }
        }

        public static bool IsRunning() => _isRunning;
        public static bool IsFinished() => _isFinished;

        public static string GetCurrentTimeFormatted()
        {
            float elapsed = _elapsedTime;

            if (ConfigManager.ShowMilliseconds)
            {
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);
                int milliseconds = Mathf.FloorToInt((elapsed % 1f) * 1000f);

                return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
            }
            else
            {
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);

                return $"{minutes:00}:{seconds:00}";
            }
        }
    }
}