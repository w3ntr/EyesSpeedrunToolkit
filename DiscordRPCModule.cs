using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EyesSpeedrunToolkit
{
    public static class DiscordRPCModule
    {
        private static DiscordRpc _rpc;
        private static float _lastUpdate;
        private static long _sessionStartTime;

        public static void Initialize()
        {
            _rpc = new DiscordRpc();
            _rpc.TryConnect();
            _sessionStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            MelonLoader.MelonLogger.Msg("DiscordRPC: Initialized");
        }

        public static void Update()
        {
            if (_rpc == null) return;

            _rpc.TryConnect();

            // Обновляем статус не чаще чем раз в 3 секунды, чтобы не спамить Discord
            if (Time.time - _lastUpdate >= 3.0f)
            {
                _lastUpdate = Time.time;
                UpdatePresence();
            }

            _rpc.Pump();
        }

        private static void UpdatePresence()
        {
            string scene = SceneManager.GetActiveScene().name;

            // 1. Проверка на главное меню или загрузку
            if (string.IsNullOrEmpty(scene) || scene.ToLower().Contains("menu") || scene.ToLower().Contains("init") || scene.ToLower().Contains("postgame"))
            {
                _sessionStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // Сброс таймера сессии

                _rpc.SetActivity(new DiscordActivity
                {
                    Details = "In Main Menu",
                    State = "Browsing or practicing",
                    // Оставляем пустым, чтобы Discord показал стандартную иконку приложения
                    LargeImage = "",
                    LargeText = "Eyes: The Horror Game"
                });
                return;
            }

            // 2. Мы в игре (School, Hospital, Mansion)
            string mapName = GetFriendlyMapName(scene);
            string currentTime = SpeedrunTimer.IsRunning() ? SpeedrunTimer.GetCurrentTimeFormatted() : "00:00";

            _rpc.SetActivity(new DiscordActivity
            {
                Details = $"Map: {mapName}",
                State = $"Time: {currentTime}",
                StartTimestamp = _sessionStartTime,
                LargeImage = "", // Стандартная иконка
                LargeText = "Eyes: The Horror Game",
                SmallImage = "", // Можно добавить "playing" если загрузишь картинку в Dev Portal
                SmallText = "Speedrunning"
            });
        }

        private static string GetFriendlyMapName(string sceneName)
        {
            switch (sceneName.ToLower())
            {
                case "school": return "School";
                case "hospital": return "Hospital";
                case "mansion": return "Mansion";
                default: return sceneName;
            }
        }

        public static void Dispose()
        {
            _rpc?.Dispose();
            MelonLoader.MelonLogger.Msg("DiscordRPC: Disposed");
        }
    }
}