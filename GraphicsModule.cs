using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace EyesSpeedrunToolkit
{
    public static class GraphicsModule
    {
        private static Camera _mainCamera;
        private static Dictionary<Material, Shader> _originalFalloffShaders = new Dictionary<Material, Shader>();
        private static List<ParticleSystem> _disabledParticles = new List<ParticleSystem>();
        private static bool _initialized = false;
        private static bool _lastDisablePP = false;
        private static bool _lastDisableFalloff = false;
        private static float _lastRenderDistance = 11f;

        public static void Initialize()
        {
            MelonLogger.Msg("GraphicsModule: Initialized");
        }

        private static float ConvertFarClipToMeters(float normalizedValue)
        {
            return 11f + (normalizedValue - 0.01f) / (1.00f - 0.01f) * (1000f - 11f);
        }

        private static void ApplyFPSLimit()
        {
            if (ConfigManager.DisableVSync)
            {
                if (QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0;
                int targetFPS = ConfigManager.CustomFPS > 0 ? ConfigManager.CustomFPS : -1;
                if (Application.targetFrameRate != targetFPS) Application.targetFrameRate = targetFPS;
            }
            else
            {
                if (QualitySettings.vSyncCount != 1) QualitySettings.vSyncCount = 1;
                int refreshRate = Screen.currentResolution.refreshRate;
                if (refreshRate <= 0) refreshRate = 60;
                if (Application.targetFrameRate != refreshRate) Application.targetFrameRate = refreshRate;
            }
        }

        public static void Update()
        {
            ApplyFPSLimit();

            string currentScene = SceneManager.GetActiveScene().name;
            bool isGameScene = currentScene == "School" || currentScene == "Hospital" || currentScene == "Mansion";

            // === ЖЁСТКИЙ ФИКС СБРОСА FOV ===
            // Применяем FOV каждый кадр, чтобы игра (например, при спринте или загрузке) не могла его переписать
            Camera activeCam = Camera.main ?? _mainCamera;
            if (activeCam != null && isGameScene)
            {
                activeCam.fieldOfView = ConfigManager.TargetFOV;
                _mainCamera = activeCam; // Обновляем ссылку на всякий случай
            }
            // ==============================

            if (!isGameScene) return;

            if (!_initialized)
            {
                _mainCamera = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
                if (_mainCamera != null)
                {
                    MelonLogger.Msg($"GraphicsModule: Camera found in '{currentScene}' ({_mainCamera.name})");
                    _initialized = true;
                    ApplySettings();
                }
            }

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

            if (_lastDisableFalloff != ConfigManager.DisableFalloffShader)
            {
                _lastDisableFalloff = ConfigManager.DisableFalloffShader;
                ApplyFalloffShaderFix(ConfigManager.DisableFalloffShader);
                settingsChanged = true;
            }

            if (Mathf.Abs(_lastRenderDistance - ConfigManager.RenderDistance) > 0.1f)
            {
                _lastRenderDistance = ConfigManager.RenderDistance;
                ApplyRenderDistance();
                settingsChanged = true;
            }

            if (settingsChanged)
            {
                ApplySettings();
            }
        }

        private static void ApplyRenderDistance()
        {
            if (_mainCamera == null) return;
            _mainCamera.farClipPlane = ConfigManager.RenderDistance;
        }

        private static void ApplySettings()
        {
            if (_mainCamera == null) return;

            _mainCamera.fieldOfView = ConfigManager.TargetFOV;

            TogglePostProcessing(!ConfigManager.DisablePostProcessing);
            ApplyRenderDistance();
        }

        private static void ApplyFalloffShaderFix(bool disable)
        {
            if (disable)
            {
                Renderer[] allRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
                int replaced = 0;
                int skippedPortals = 0;

                foreach (Renderer rend in allRenderers)
                {
                    if (rend == null) continue;
                    if (rend is ParticleSystemRenderer) continue;

                    foreach (Material mat in rend.sharedMaterials)
                    {
                        if (mat == null || mat.shader == null) continue;

                        if (mat.shader.name == "CustomShaders/FalloffPlusLightmap")
                        {
                            // === ПУЛЕНЕПРОБИВАЕМАЯ ПРОВЕРКА НА ПОРТАЛ ===
                            bool isPortal = false;
                            Transform current = rend.transform;

                            // Поднимаемся вверх по иерархии объекта (от ребенка к родителю)
                            while (current != null)
                            {
                                // 1. Проверка тега на любом уровне иерархии
                                if (current.CompareTag("MansionPortal"))
                                {
                                    isPortal = true;
                                    break;
                                }

                                // 2. Проверка имени на любом уровне (добавил "exit" и "gate", так как порталы часто так называются)
                                string objName = current.name.ToLower();
                                if (objName.Contains("portal") || objName.Contains("teleport") ||
                                    objName.Contains("exit") || objName.Contains("gate"))
                                {
                                    isPortal = true;
                                    break;
                                }

                                // 3. Проверка наличия компонента Teleport на любом уровне
                                if (current.GetComponent("Teleport") != null)
                                {
                                    isPortal = true;
                                    break;
                                }

                                current = current.parent; // Переходим к родителю
                            }

                            if (isPortal)
                            {
                                skippedPortals++;
                                continue; // Пропускаем этот материал, оставляем оригинальный шейдер
                            }
                            // ============================================

                            if (!_originalFalloffShaders.ContainsKey(mat))
                            {
                                _originalFalloffShaders[mat] = mat.shader;
                            }

                            Shader newShader = Shader.Find("Unlit/Texture");
                            if (newShader != null)
                            {
                                mat.shader = newShader;
                                replaced++;
                            }
                        }
                    }
                }
                MelonLogger.Msg($"GraphicsModule: Replaced {replaced} FalloffPlusLightmap materials (Skipped {skippedPortals} portal materials)");

                // 2. Отключаем все партиклы (чтобы не было черных квадратов), НО пропускаем порталы!
                ParticleSystem[] allParticles = UnityEngine.Object.FindObjectsOfType<ParticleSystem>();
                int skippedParticlePortals = 0;

                foreach (ParticleSystem ps in allParticles)
                {
                    if (ps == null) continue;

                    // === ПРОВЕРКА: Является ли этот партикл частью портала? ===
                    bool isPortalParticle = false;
                    Transform current = ps.transform;

                    while (current != null)
                    {
                        if (current.CompareTag("MansionPortal") ||
                            current.name.ToLower().Contains("portal") ||
                            current.name.ToLower().Contains("teleport") ||
                            current.name.ToLower().Contains("exit") ||
                            current.name.ToLower().Contains("gate") ||
                            current.GetComponent("Teleport") != null)
                        {
                            isPortalParticle = true;
                            break;
                        }
                        current = current.parent;
                    }

                    if (isPortalParticle)
                    {
                        skippedParticlePortals++;
                        continue; // НЕ отключаем партиклы портала!
                    }
                    // ==========================================================

                    if (!_disabledParticles.Contains(ps))
                    {
                        _disabledParticles.Add(ps);
                        ps.Stop();
                        ps.gameObject.SetActive(false);
                    }
                }
                MelonLogger.Msg($"GraphicsModule: Disabled {_disabledParticles.Count} particle systems (Skipped {skippedParticlePortals} portal particles)");
            }
            else
            {
                // 1. Возвращаем шейдеры
                int restored = 0;
                foreach (var kvp in _originalFalloffShaders)
                {
                    if (kvp.Key != null && kvp.Value != null)
                    {
                        kvp.Key.shader = kvp.Value;
                        restored++;
                    }
                }
                _originalFalloffShaders.Clear();
                MelonLogger.Msg($"GraphicsModule: Restored {restored} original shaders");

                // 2. Включаем партиклы обратно
                foreach (ParticleSystem ps in _disabledParticles)
                {
                    if (ps != null)
                    {
                        ps.gameObject.SetActive(true);
                        ps.Play();
                    }
                }
                MelonLogger.Msg($"GraphicsModule: Restored {_disabledParticles.Count} particle systems");
                _disabledParticles.Clear();
            }
        }

        private static void TogglePostProcessing(bool enable)
        {
            if (_mainCamera == null) return;

            var postProcessLayer = _mainCamera.GetComponent<PostProcessLayer>();
            if (postProcessLayer != null)
            {
                postProcessLayer.enabled = enable;
            }

            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
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
            // Сначала восстанавливаем всё
            if (_originalFalloffShaders.Count > 0)
            {
                foreach (var kvp in _originalFalloffShaders)
                {
                    if (kvp.Key != null && kvp.Value != null)
                    {
                        kvp.Key.shader = kvp.Value;
                    }
                }
                _originalFalloffShaders.Clear();
            }

            foreach (ParticleSystem ps in _disabledParticles)
            {
                if (ps != null)
                {
                    ps.gameObject.SetActive(true);
                    ps.Play();
                }
            }
            _disabledParticles.Clear();

            _initialized = false;
            _mainCamera = null;
            _lastDisablePP = false;
            _lastDisableFalloff = false;
            _lastRenderDistance = 11f;
            MelonLogger.Msg("GraphicsModule: Reset (game settings only)");
        }
    }
}