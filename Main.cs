using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(EyesSpeedrunToolkit.Main), "EyesSpeedrunToolkit", "1.0.1", "w3ntr")]
[assembly: MelonGame("", "Eyes")]

namespace EyesSpeedrunToolkit
{
    public class Main : MelonMod
    {
        private static string _lastScene = "";

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=== Eyes Speedrun Toolkit v1.0.1 Loaded ===");

            ConfigManager.Load();

            GraphicsModule.Initialize();
            MonsterTracker.Initialize();

            MelonLogger.Msg("All modules initialized!");
        }

        public override void OnUpdate()
        {
            GraphicsModule.Update();
            MonsterTracker.Update();
            HUDManager.Update();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"Scene is loaded: {sceneName}");

            if (sceneName == _lastScene) return;
            _lastScene = sceneName;

            bool isGameScene = sceneName == "School" || sceneName == "Hospital" || sceneName == "Mansion";

            if (isGameScene)
            {
                GraphicsModule.Reset();
                MonsterTracker.Reset();
                SpeedrunTimer.Start();
            }
        }

        public override void OnGUI()
        {
            HUDManager.OnGUI();
        }
    }
}