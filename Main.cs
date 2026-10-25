using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(EyesSpeedrunToolkit.Main), "EyesSpeedrunToolkit", "1.2.0", "w3ntr")]
[assembly: MelonGame("", "Eyes")]

namespace EyesSpeedrunToolkit
{
    public class Main : MelonMod
    {

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=== Eyes Speedrun Toolkit v1.2.0 Loaded ===");
            ConfigManager.Load();
            GraphicsModule.Initialize();
            MonsterTracker.Initialize();
            MelonLogger.Msg("All modules initialized!");
        }

        public override void OnUpdate()
        {
            SpeedrunTimer.Update();
            GraphicsModule.Update();
            MonsterTracker.Update();
            HUDManager.Update();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"Scene is loaded: {sceneName}");

            // Автозапуск таймера в игровых сценах
            if (sceneName == "School" || sceneName == "Hospital" || sceneName == "Mansion")
            {
                SpeedrunTimer.Start();
                MelonLogger.Msg("SpeedrunTimer: Auto-started in game scene");
            }
        }

        public override void OnGUI()
        {
            HUDManager.OnGUI();
        }
    }
}