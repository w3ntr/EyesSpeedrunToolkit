using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using System.Reflection;

[assembly: MelonInfo(typeof(EyesSpeedrunToolkit.Main), "EyesSpeedrunToolkit", "1.2.0", "w3ntr")]
[assembly: MelonGame("", "Eyes")]

namespace EyesSpeedrunToolkit
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=== Eyes Speedrun Toolkit v1.2.0 Loaded ===");

            // 1. Инициализация Harmony (без этого патчи не работают)
            var harmony = new HarmonyLib.Harmony("com.w3ntr.eyesspeedruntoolkit");
            harmony.PatchAll(typeof(Main).Assembly);

            // 2. Инициализация модулей
            ConfigManager.Load();
            GraphicsModule.Initialize();
            MonsterTracker.Initialize();
            HUDManager.Initialize(); // <-- Это критически важно для сохранения биндов!

            MelonLogger.Msg("All modules initialized successfully!");
        }

        public override void OnUpdate()
        {
            SpeedrunTimer.Update();
            GraphicsModule.Update();
            MonsterTracker.Update();
            BagsWallHack.Update();
            HUDManager.Update();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"Scene is loaded: {sceneName}");

            if (sceneName == "School" || sceneName == "Hospital" || sceneName == "Mansion")
            {
                SpeedrunTimer.Start();
                MonsterTracker.Reset();
                MelonLogger.Msg("SpeedrunTimer: Auto-started in game scene");
            }
        }

        public override void OnGUI()
        {
            HUDManager.OnGUI();
        }
    }
}