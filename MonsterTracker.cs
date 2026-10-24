using UnityEngine;
using System.Collections.Generic;
using MelonLoader;

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

        // Имена объектов-монстров (префикс "Enemy_")
        private static readonly string[] MonsterPrefixes = new string[]
        {
            "Enemy_Krasue", "Enemy_Charlie", "Enemy_Dog", "Enemy_Witch"
        };

        public static void Initialize()
        {
            MelonLogger.Msg("MonsterTracker: Initialized");
            _pathHistory.Clear();
            _currentMonster = null;
            _searchAttempts = 0;
            _searching = true;
            _searchStartTime = Time.time;
            _diagnosticLogged = false;
        }

        public static void Update()
        {
            // Диагностический лог при первом запуске
            if (!_diagnosticLogged && Time.time - _searchStartTime > 1f)
            {
                _diagnosticLogged = true;
                LogSceneDiagnostics();
            }

            // Ищем монстра в течение первых 60 секунд
            if (_searching && Time.time - _searchStartTime < 60f)
            {
                _searchAttempts++;

                if (Time.time - _lastLogTime >= _logInterval)
                {
                    _lastLogTime = Time.time;

                    // Способ 1: Ищем объекты с префиксом "Enemy_"
                    GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

                    foreach (GameObject obj in allObjects)
                    {
                        if (obj == null || !obj.activeInHierarchy) continue;

                        foreach (string prefix in MonsterPrefixes)
                        {
                            if (obj.name.StartsWith(prefix))
                            {
                                _currentMonster = obj.transform;
                                _searching = false;
                                MelonLogger.Msg($"MonsterTracker: Found monster '{obj.name}' at position {obj.transform.position}");
                                return;
                            }
                        }
                    }

                    // Логируем каждые 10 секунд
                    if (_searchAttempts % 20 == 0)
                    {
                        MelonLogger.Msg($"MonsterTracker: Search attempt {_searchAttempts} - no monsters found yet");
                    }
                }
            }

            // Если монстр найден - логируем позицию
            if (_currentMonster != null && _currentMonster.gameObject.activeInHierarchy)
            {
                _pathHistory.Add(_currentMonster.position);

                if (_pathHistory.Count > 100)
                {
                    _pathHistory.RemoveAt(0);
                }
            }
        }

        // Диагностический лог всех объектов на сцене
        private static void LogSceneDiagnostics()
        {
            MelonLogger.Msg("=== SCENE DIAGNOSTICS ===");

            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            MelonLogger.Msg($"Total GameObjects in scene: {allObjects.Length}");

            int monsterCount = 0;
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;

                // Логируем объекты с префиксом Enemy_
                if (obj.name.StartsWith("Enemy_"))
                {
                    MelonLogger.Msg($"  Monster: '{obj.name}' (Active: {obj.activeInHierarchy})");
                    monsterCount++;
                }
            }

            MelonLogger.Msg($"Total monsters found: {monsterCount}");
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

            var player = CustomSingleton<PlayerManager>.Instance?.Heart;
            if (player == null) return -1f;

            return Vector3.Distance(_currentMonster.position, player.transform.position);
        }

        public static int GetPathHistoryCount()
        {
            return _pathHistory.Count;
        }

        public static void Reset()
        {
            _pathHistory.Clear();
            _currentMonster = null;
            _searchAttempts = 0;
            _searching = true;
            _searchStartTime = Time.time;
            _diagnosticLogged = false;
            MelonLogger.Msg("MonsterTracker: Reset");
        }
    }
}