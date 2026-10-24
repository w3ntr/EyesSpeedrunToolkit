using UnityEngine;
using UnityEngine.SceneManagement;
using MelonLoader;
using UnityEngine.Rendering.PostProcessing;

namespace EyesSpeedrunToolkit
{
    public static class GraphicsModule
    {
        private static Camera _mainCamera;
        private static bool _initialized = false;
        private static bool _lastDisablePP = false;
        private static bool _lastDisableVSync = false;
        private static int _lastCustomFPS = 0;
        private static float _lastFarClip = 0.01f;

        public static void Initialize()
        {
            MelonLogger.Msg("GraphicsModule: Initialized");
            ApplyFPSLimit(); // Применяем сразу
        }

        // Этот метод вызывается КАЖДЫЙ КАДР для гарантии
        private static void ApplyFPSLimit()
        {
            // ВСЕГДА используем VSync + targetFrameRate для стабильности
            QualitySettings.vSyncCount = 1;

            int targetFPS = ConfigManager.CustomFPS > 0 ? ConfigManager.CustomFPS : Screen.currentResolution.refreshRate;
            if (targetFPS <= 0) targetFPS = 60;

            Application.targetFrameRate = targetFPS;

            // Логируем только при изменении
            if (_lastCustomFPS != ConfigManager.CustomFPS)
            {
                _lastCustomFPS = ConfigManager.CustomFPS;
                MelonLogger.Msg($"GraphicsModule: FPS limited to {targetFPS} (VSync enabled)");
            }
        }

        private static float ConvertFarClipToMeters(float normalizedValue)
        {
            return 11f + (normalizedValue - 0.01f) / (1.00f - 0.01f) * (1000f - 11f);
        }

        public static void Update()
        {
            // ВСЕГДА применяем FPS лимит (каждый кадр!)
            ApplyFPSLimit();

            // Проверяем изменения настроек (для логирования)
            if (_lastDisableVSync != ConfigManager.DisableVSync || _lastCustomFPS != ConfigManager.CustomFPS)
            {
                _lastDisableVSync = ConfigManager.DisableVSync;
                _lastCustomFPS = ConfigManager.CustomFPS;
                MelonLogger.Msg($"GraphicsModule: FPS settings updated (VSync: {!ConfigManager.DisableVSync}, Limit: {ConfigManager.CustomFPS}, Actual: {Application.targetFrameRate})");
            }

            // === Игровые настройки только в игровых сценах ===
            string currentScene = SceneManager.GetActiveScene().name;
            bool isGameScene = currentScene == "School" || currentScene == "Hospital" || currentScene == "Mansion";

            if (!isGameScene) return;

            // FOV
            if (_mainCamera != null && Mathf.Abs(_mainCamera.fieldOfView - ConfigManager.TargetFOV) > 0.1f)
            {
                _mainCamera.fieldOfView = ConfigManager.TargetFOV;
            }

            // Инициализация камеры (только один раз)
            if (!_initialized)
            {
                _mainCamera = Camera.main ?? Object.FindObjectOfType<Camera>();

                if (_mainCamera != null)
                {
                    MelonLogger.Msg($"GraphicsModule: Camera found in '{currentScene}' ({_mainCamera.name})");
                    _initialized = true;
                    ApplySettings();
                }
            }
            // Временная диагностика - убрать после проверки
            if (Time.frameCount % 60 == 0) // Логируем каждые 60 кадров (примерно раз в секунду)
            {
                MelonLogger.Msg($"[DIAG] Frame {Time.frameCount}: targetFrameRate={Application.targetFrameRate}, vSyncCount={QualitySettings.vSyncCount}, actualFPS={1f / Time.deltaTime:F0}");
            }

            // Проверяем изменения игровых настроек
            CheckAndApplyGameChanges();
        }

        private static void CheckAndApplyGameChanges()
        {
            bool settingsChanged = false;

            if (_lastDisablePP != ConfigManager.DisablePostProcessing)
            {
                _lastDisablePP = ConfigManager.DisablePostProcessing;
                settingsChanged = true;
            }

            if (Mathf.Abs(_lastFarClip - ConfigManager.FarClipPlane) > 0.001f)
            {
                _lastFarClip = ConfigManager.FarClipPlane;
                settingsChanged = true;
            }

            if (settingsChanged)
            {
                ApplySettings();
                MelonLogger.Msg("GraphicsModule: Game settings changed, reapplying...");
            }
        }

        private static void ApplySettings()
        {
            if (_mainCamera == null) return;

            _mainCamera.fieldOfView = ConfigManager.TargetFOV;

            float farClipMeters = ConvertFarClipToMeters(ConfigManager.FarClipPlane);
            _mainCamera.farClipPlane = farClipMeters;

            MelonLogger.Msg($"GraphicsModule: Far Clip Plane set to {farClipMeters:F1}m (normalized: {ConfigManager.FarClipPlane:F2})");

            TogglePostProcessing(!ConfigManager.DisablePostProcessing);
        }

        private static void TogglePostProcessing(bool enable)
        {
            if (_mainCamera == null) return;

            var postProcessLayer = _mainCamera.GetComponent<PostProcessLayer>();
            if (postProcessLayer != null)
            {
                postProcessLayer.enabled = enable;
            }

            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                var volume = obj.GetComponent<PostProcessVolume>();
                if (volume != null)
                {
                    volume.enabled = enable;
                }
            }
        }

        public static void Reset()
        {
            _initialized = false;
            _mainCamera = null;
            _lastDisablePP = false;
            _lastFarClip = 0.01f;
            MelonLogger.Msg("GraphicsModule: Reset (game settings only)");
        }
    }
}