using UnityEngine;
using MelonLoader;
using System;
using System.Reflection;

namespace EyesSpeedrunToolkit
{
    public static class PlayerModifiers
    {
        private static Component _staminaComponent;
        private static Type _staminaType;

        private static FieldInfo _fieldStaminaCounter;
        private static FieldInfo _fieldMaxStamina;
        private static FieldInfo _fieldCooldownTime;
        private static FieldInfo _fieldRecoverSpeed;
        private static FieldInfo _fieldLastRun;
        private static FieldInfo _fieldIsStaminaInfinity;

        public static string StaminaTimerText = "";

        // Флаги для логирования (чтобы не спамить)
        private static bool _loggedSearchStarted = false;
        private static bool _loggedFound = false;
        private static bool _loggedNotFound = false;

        public static void Initialize()
        {
            MelonLogger.Msg("PlayerModifiers: Initialized");
        }

        public static void Update()
        {
            // 1. Инициализация ссылки на Стамину (делаем один раз)
            if (_staminaComponent == null)
            {
                // Логируем только один раз, что начали поиск
                if (!_loggedSearchStarted)
                {
                    MelonLogger.Msg("PlayerModifiers: Searching for Stamina component...");
                    _loggedSearchStarted = true;
                }

                // Способ 1: Прямой поиск по типу компонента
                _staminaComponent = UnityEngine.Object.FindObjectOfType<Stamina>();

                if (_staminaComponent != null)
                {
                    _staminaType = _staminaComponent.GetType();
                    CacheFields();
                    if (!_loggedFound)
                    {
                        MelonLogger.Msg($"PlayerModifiers: Stamina found via FindObjectOfType! Type: {_staminaType.Name}");
                        _loggedFound = true;
                    }
                }
                else
                {
                    // Логируем только один раз, что не нашли
                    if (!_loggedNotFound)
                    {
                        MelonLogger.Msg("PlayerModifiers: Stamina not found yet (waiting for player to spawn...)");
                        _loggedNotFound = true;
                    }
                }
            }

            if (_staminaComponent == null) return;

            // 2. Логика Бесконечной Стамины
            if (ConfigManager.InfiniteStaminaEnabled)
            {
                ApplyInfiniteStamina();
            }
            else
            {
                if (_fieldIsStaminaInfinity != null)
                {
                    _fieldIsStaminaInfinity.SetValue(_staminaComponent, false);
                }
            }

            // 3. Логика Таймера Регенерации
            if (ConfigManager.StaminaTimerEnabled)
            {
                UpdateStaminaTimer();
            }
            else
            {
                StaminaTimerText = "";
            }
        }

        private static void CacheFields()
        {
            if (_staminaType == null) return;

            _fieldStaminaCounter = _staminaType.GetField("staminaCounter", BindingFlags.NonPublic | BindingFlags.Instance);
            _fieldMaxStamina = _staminaType.GetField("maxStamina", BindingFlags.NonPublic | BindingFlags.Instance);
            _fieldCooldownTime = _staminaType.GetField("cooldownTime", BindingFlags.NonPublic | BindingFlags.Instance);
            _fieldRecoverSpeed = _staminaType.GetField("recoverSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            _fieldLastRun = _staminaType.GetField("lastRun", BindingFlags.NonPublic | BindingFlags.Instance);
            _fieldIsStaminaInfinity = _staminaType.GetField("isStaminaInfinity", BindingFlags.NonPublic | BindingFlags.Instance);

            MelonLogger.Msg($"PlayerModifiers: Cached fields - Counter: {_fieldStaminaCounter != null}, Max: {_fieldMaxStamina != null}, Infinity: {_fieldIsStaminaInfinity != null}");
        }

        private static void ApplyInfiniteStamina()
        {
            if (_fieldIsStaminaInfinity != null)
            {
                _fieldIsStaminaInfinity.SetValue(_staminaComponent, true);
            }
            else
            {
                if (_fieldStaminaCounter != null && _fieldMaxStamina != null)
                {
                    float maxStamina = (float)_fieldMaxStamina.GetValue(_staminaComponent);
                    _fieldStaminaCounter.SetValue(_staminaComponent, maxStamina);
                }
            }
        }

        private static void UpdateStaminaTimer()
        {
            if (_fieldStaminaCounter == null || _fieldMaxStamina == null ||
                _fieldCooldownTime == null || _fieldRecoverSpeed == null || _fieldLastRun == null)
            {
                StaminaTimerText = "Error: Fields not cached";
                return;
            }

            float currentStamina = (float)_fieldStaminaCounter.GetValue(_staminaComponent);
            float maxStamina = (float)_fieldMaxStamina.GetValue(_staminaComponent);
            float cooldown = (float)_fieldCooldownTime.GetValue(_staminaComponent);
            float recoverSpeed = (float)_fieldRecoverSpeed.GetValue(_staminaComponent);
            float lastRun = (float)_fieldLastRun.GetValue(_staminaComponent);

            if (currentStamina >= maxStamina)
            {
                StaminaTimerText = $"Stamina: {currentStamina:F0}/{maxStamina:F0} (Full)";
                return;
            }

            float timeSinceLastRun = Time.time - lastRun;
            float timeUntilRegenStart = Mathf.Max(0f, cooldown - timeSinceLastRun);

            float missingStamina = maxStamina - currentStamina;
            float timeToRefill = missingStamina / recoverSpeed;

            float totalTime = timeUntilRegenStart + timeToRefill;

            StaminaTimerText = $"Stamina: {currentStamina:F0}/{maxStamina:F0} | Full in: {totalTime:F1}s";
        }

        public static void Reset()
        {
            _staminaComponent = null;
            _staminaType = null;
            StaminaTimerText = "";

            // Сбрасываем флаги логирования, чтобы при следующем запуске снова логируем
            _loggedSearchStarted = false;
            _loggedFound = false;
            _loggedNotFound = false;
        }
    }
}