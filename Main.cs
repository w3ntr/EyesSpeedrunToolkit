using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using System.Reflection;

[assembly: MelonInfo(typeof(EyesSpeedrunToolkit.Main), "EyesSpeedrunToolkit", "1.6.0", "w3ntr")]
[assembly: MelonGame("", "Eyes")]

namespace EyesSpeedrunToolkit
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=== Eyes Speedrun Toolkit v1.6.0 Loaded ===");

            // 1. Инициализация Harmony (без этого патчи не работают)
            var harmony = new HarmonyLib.Harmony("com.w3ntr.eyesspeedruntoolkit");
            harmony.PatchAll(typeof(Main).Assembly);

            // 2. Инициализация модулей
            ConfigManager.Load();
            HUDToggle.Initialize();
            GraphicsModule.Initialize();
            FadeModule.Initialize();
            MonsterTracker.Initialize();
            PlayerModifiers.Initialize();
            FreecamModule.Initialize();
            HUDManager.Initialize(); // <-- Это критически важно для сохранения биндов!

            MelonLogger.Msg("All modules initialized successfully!");
        }

        public override void OnUpdate()
        {
            SpeedrunTimer.Update();
            HUDToggle.Update();
            GraphicsModule.Update();
            FadeModule.Update();
            MonsterTracker.Update();
            PlayerModifiers.Update();
            FreecamModule.Update();
            BagsWallHack.Update();
            HUDManager.Update();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"Scene is loaded: {sceneName}");

            bool isWinScene = sceneName == "PostGameAd" || sceneName == "Win" || sceneName == "Outro";

            if (!isWinScene)
            {
                SpeedrunTimer.Reset();
                HUDToggle.Reset();
                MonsterTracker.Reset();
                PlayerModifiers.Reset();
                FreecamModule.Reset();
                FadeModule.Reset();
                MelonLogger.Msg("SpeedrunTimer: Auto-started in game scene");
            }
            else
            {
                // На экране победы — останавливаем таймер, если он ещё идёт
                if (SpeedrunTimer.IsRunning())
                {
                    SpeedrunTimer.Finish();
                    MelonLogger.Msg($"SpeedrunTimer: Finished on {sceneName} at {SpeedrunTimer.GetCurrentTimeFormatted()}");
                }
            }

            if (sceneName == "School" || sceneName == "Hospital" || sceneName == "Mansion")
            {
                    SpeedrunTimer.Start();
                    HUDToggle.Reset();
                    FadeModule.Reset();
                    MonsterTracker.Reset();
                    PlayerModifiers.Reset();
                    FreecamModule.Reset();
                    MelonLogger.Msg("SpeedrunTimer: Auto-started in game scene");
                }
            }

        public override void OnGUI()
        {
            HUDManager.OnGUI();
        }
    }
}