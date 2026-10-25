using UnityEngine;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EyesSpeedrunToolkit
{
    public static class MonsterTracker
    {
        private static List<Vector3> _pathHistory = new List<Vector3>();
        private static float _logInterval = 0.5f;
        private static float _lastLogTime;
        private static Transform _currentMonster;
        private static int _searchAttempts = 0;
        private static float _searchStartTime = 0f;
        private static bool _searching = false;
        private static bool _diagnosticLogged = false;
        private static string _lastSceneName = "";
        private static bool _wasInGameScene = false;  // <-- НОВОЕ: отслеживаем выход из игры

        private static readonly string[] SchoolMonsters = { "Enemy_Dog", "Dog" };
        private static readonly string[] HospitalMonsters = { "Enemy_Charlie" };
        private static readonly string[] MansionMonsters = { "Enemy_Krasue", "Enemy_Witch" };

        public static void Initialize()
        {
            MelonLogger.Msg("MonsterTracker: Initialized");
            Reset();
        }

        public static void Update()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            bool isGameScene = currentScene == "School" || currentScene == "Hospital" || currentScene == "Mansion";

            if (!isGameScene)
            {
                // Мы вышли из игровой сцены (смерть/меню) — запоминаем это
                _wasInGameScene = false;
                return;
            }

            // 1. СБРОС ПРИ ВОЗВРАТЕ В ИГРУ ПОСЛЕ СМЕРТИ
            if (!_wasInGameScene)
            {
                _wasInGameScene = true;
                MelonLogger.Msg($"MonsterTracker: Returned to game scene '{currentScene}' after death/menu, resetting tracker");
                Reset();
            }

            // 2. Сброс при смене игровой сцены (School → Hospital)
            if (_lastSceneName != currentScene)
            {
                _lastSceneName = currentScene;
                Reset();
                MelonLogger.Msg($"MonsterTracker: Scene changed to {currentScene}, resetting tracker");
            }

            // 3. Сброс если монстр стал неактивным (умер/исчез)
            if (_currentMonster != null && !_currentMonster.gameObject.activeInHierarchy)
            {
                MelonLogger.Msg($"MonsterTracker: Monster {_currentMonster.name} became inactive, rescanning...");
                Reset();
            }

            // 4. Диагностика через 2 секунды
            if (!_diagnosticLogged && Time.time - _searchStartTime > 2f)
            {
                _diagnosticLogged = true;
                LogSceneDiagnostics();

                if (_currentMonster != null)
                {
                    _searching = false;
                    MelonLogger.Msg($"MonsterTracker: Successfully tracked {_currentMonster.name}!");
                    if (ConfigManager.ESPEnabled) EnableESP();
                }
            }

            // 5. Ищем монстра (первые 120 секунд)
            if (_searching && Time.time - _searchStartTime < 120f)
            {
                _searchAttempts++;
                if (Time.time - _lastLogTime >= _logInterval)
                {
                    _lastLogTime = Time.time;

                    string[] targetMonsters = GetMonstersForScene(currentScene);
                    GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

                    foreach (GameObject obj in allObjects)
                    {
                        if (obj == null) continue;

                        foreach (string monsterName in targetMonsters)
                        {
                            if (obj.name == monsterName || obj.name.StartsWith(monsterName + "_"))
                            {
                                _currentMonster = obj.transform;
                                _searching = false;
                                MelonLogger.Msg($"MonsterTracker: Found {monsterName} '{obj.name}' at {obj.transform.position}");
                                if (ConfigManager.ESPEnabled) EnableESP();
                                return;
                            }
                        }
                    }

                    if (_searchAttempts % 40 == 0)
                    {
                        MelonLogger.Msg($"MonsterTracker: Search attempt {_searchAttempts} - no monsters found yet");
                    }
                }
            }

            // 6. Логируем позицию
            if (_currentMonster != null && _currentMonster.gameObject.activeInHierarchy)
            {
                _pathHistory.Add(_currentMonster.position);
                if (_pathHistory.Count > 100) _pathHistory.RemoveAt(0);
            }
        }

        private static string[] GetMonstersForScene(string sceneName)
        {
            switch (sceneName)
            {
                case "School": return SchoolMonsters;
                case "Hospital": return HospitalMonsters;
                case "Mansion": return MansionMonsters;
                default: return new string[] { "Enemy_" };
            }
        }

        private static void LogSceneDiagnostics()
        {
            MelonLogger.Msg("=== SCENE DIAGNOSTICS ===");
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            MelonLogger.Msg($"Total GameObjects in scene: {allObjects.Length}");

            int monsterCount = 0;
            int bagCount = 0;
            int sackCount = 0;
            int lootCount = 0;
            int itemCount = 0;
            int pickupCount = 0;

            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;

                // Логируем монстров
                if (obj.name.StartsWith("Enemy_"))
                {
                    MelonLogger.Msg($"  Monster: '{obj.name}' (Active: {obj.activeInHierarchy}, Pos: {obj.transform.position})");
                    monsterCount++;
                }

                string name = obj.name.ToLower();

                // Ищем мешки по разным возможным названиям
                if (name.Contains("bag"))
                {
                    MelonLogger.Msg($"  BAG: '{obj.name}' (Active: {obj.activeInHierarchy}, Pos: {obj.transform.position})");
                    bagCount++;
                }
                else if (name.Contains("sack"))
                {
                    MelonLogger.Msg($"  SACK: '{obj.name}' (Active: {obj.activeInHierarchy}, Pos: {obj.transform.position})");
                    sackCount++;
                }
                else if (name.Contains("loot"))
                {
                    MelonLogger.Msg($"  LOOT: '{obj.name}' (Active: {obj.activeInHierarchy}, Pos: {obj.transform.position})");
                    lootCount++;
                }
                else if (name.Contains("item") && !name.Contains("item_"))
                {
                    MelonLogger.Msg($"  ITEM: '{obj.name}' (Active: {obj.activeInHierarchy}, Pos: {obj.transform.position})");
                    itemCount++;
                }
                else if (name.Contains("pickup") || name.Contains("collect"))
                {
                    MelonLogger.Msg($"  PICKUP: '{obj.name}' (Active: {obj.activeInHierarchy}, Pos: {obj.transform.position})");
                    pickupCount++;
                }
            }

            MelonLogger.Msg($"Total monsters found: {monsterCount}");
            MelonLogger.Msg($"--- Summary: Bags={bagCount}, Sacks={sackCount}, Loot={lootCount}, Items={itemCount}, Pickups={pickupCount} ---");
            MelonLogger.Msg("=== END DIAGNOSTICS ===");
        }

        public static Vector3 GetCurrentPosition()
        {
            if (_currentMonster != null && _currentMonster.gameObject.activeInHierarchy)
                return _currentMonster.position;
            return Vector3.zero;
        }

        public static float GetDistanceToPlayer()
        {
            if (_currentMonster == null || !_currentMonster.gameObject.activeInHierarchy)
                return -1f;

            Transform playerTransform = Camera.main != null ? Camera.main.transform : null;

            if (playerTransform == null)
            {
                var player = CustomSingleton<PlayerManager>.Instance?.Heart;
                if (player == null) return -1f;
                playerTransform = player.transform;
            }

            return Vector3.Distance(_currentMonster.position, playerTransform.position);
        }

        public static int GetPathHistoryCount() => _pathHistory.Count;

        public static void Reset()
        {
            _pathHistory.Clear();
            _currentMonster = null;
            _searchAttempts = 0;
            _searching = true;
            _searchStartTime = Time.time;
            _diagnosticLogged = false;
            _lastSceneName = SceneManager.GetActiveScene().name;
            MelonLogger.Msg("MonsterTracker: Reset");
        }

        // ESP логика
        private static Material _espMaterial;
        private static Dictionary<GameObject, Material[]> _originalMaterials = new Dictionary<GameObject, Material[]>();

        public static void EnableESP()
        {
            if (_currentMonster == null) return;

            if (_espMaterial == null)
            {
                Shader espShader = Shader.Find("Hidden/Internal-Colored");
                if (espShader == null)
                {
                    MelonLogger.Msg("MonsterTracker: ERROR - Hidden/Internal-Colored not found!");
                    return;
                }

                _espMaterial = new Material(espShader);
                _espMaterial.color = new Color(1f, 0f, 0f, 1f);
                _espMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                _espMaterial.SetInt("_ZWrite", 0);
                _espMaterial.renderQueue = 5000;
                MelonLogger.Msg($"MonsterTracker: ESP using {espShader.name}");
            }

            Renderer[] renderers = _currentMonster.gameObject.GetComponentsInChildren<Renderer>();
            int count = 0;

            foreach (Renderer rend in renderers)
            {
                if (rend == null) continue;

                if (!_originalMaterials.ContainsKey(rend.gameObject))
                {
                    _originalMaterials[rend.gameObject] = rend.sharedMaterials;
                }

                rend.sharedMaterials = new Material[] { _espMaterial };
                count++;
            }
            MelonLogger.Msg($"MonsterTracker: ESP applied to {count} renderers");
        }

        public static void DisableESP()
        {
            foreach (var kvp in _originalMaterials)
            {
                if (kvp.Key != null)
                {
                    var renderer = kvp.Key.GetComponent<Renderer>();
                    if (renderer != null) renderer.sharedMaterials = kvp.Value;
                }
            }
            _originalMaterials.Clear();
            MelonLogger.Msg("MonsterTracker: ESP disabled, materials restored");
        }
    }
}