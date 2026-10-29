using UnityEngine;
using MelonLoader;
using System.Reflection;

namespace EyesSpeedrunToolkit
{
    public static class JumpModule
    {
        public static bool IsEnabled = false;
        public static float JumpForce = 0.14f; // Ставим большое значение для теста

        private static Component _fpController;
        private static FieldInfo _motorJumpForceField;
        private static object _jumpActivity;
        private static MethodInfo _tryStartMethod;

        private static bool _initDone = false;
        private static int _attempts = 0;

        public static void Initialize() { }

        public static void Update()
        {

            if (!IsEnabled)
            {
                _initDone = false;
                _attempts = 0;
                return;
            }

            // Инициализация
            if (!_initDone)
            {
                _attempts++;
                if (_attempts % 60 == 1) // Раз в секунду
                {
                    MelonLogger.Msg($"JumpModule: Попытка инициализации #{_attempts / 60}");
                    FindComponents();
                }
                if (_attempts > 300)
                {
                    MelonLogger.Warning("JumpModule: Не удалось инициализировать");
                }
                return;
            }

            // Реакция на Space
            if (Input.GetKeyDown(KeyCode.Space))
            {
                MelonLogger.Msg("JumpModule: Space pressed!");
                PerformJump();
            }
        }

        public static void Enable()
        {
            IsEnabled = true;
            _initDone = false;  // Сбрасываем инициализацию
            _attempts = 0;       // Сбрасываем счётчик попыток
            MelonLogger.Msg("JumpModule: Enabled");
        }

        public static void Disable()
        {
            IsEnabled = false;

            // Сбрасываем MotorJumpForce обратно в 0
            if (_fpController != null && _motorJumpForceField != null)
            {
                try
                {
                    _motorJumpForceField.SetValue(_fpController, 0f);
                    MelonLogger.Msg("JumpModule: MotorJumpForce reset to 0");
                }
                catch { }
            }
        }

        private static void FindComponents()
        {
            var pm = CustomSingleton<PlayerManager>.Instance;
            if (pm == null || pm.Heart == null) return;

            GameObject target = pm.Heart.transform.parent != null
                ? pm.Heart.transform.parent.gameObject
                : pm.Heart.gameObject;

            // Ищем vp_FPController
            var controllerType = System.Type.GetType("vp_FPController, Assembly-CSharp");
            if (controllerType != null)
            {
                _fpController = target.GetComponent(controllerType);
                if (_fpController != null)
                {
                    _motorJumpForceField = controllerType.GetField("MotorJumpForce",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (_motorJumpForceField != null)
                    {
                        float currentValue = (float)_motorJumpForceField.GetValue(_fpController);
                        MelonLogger.Msg($"JumpModule: Found MotorJumpForce = {currentValue}");

                        // Устанавливаем большое значение
                        _motorJumpForceField.SetValue(_fpController, JumpForce);
                        MelonLogger.Msg($"JumpModule: Set MotorJumpForce to {JumpForce}");
                    }
                }
            }

            // Ищем активность Jump
            var handlerType = System.Type.GetType("vp_FPPlayerEventHandler, Assembly-CSharp");
            if (handlerType != null)
            {
                var handler = target.GetComponent(handlerType);
                if (handler != null)
                {
                    var jumpField = handlerType.GetField("Jump",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (jumpField != null)
                    {
                        _jumpActivity = jumpField.GetValue(handler);
                        if (_jumpActivity != null)
                        {
                            _tryStartMethod = _jumpActivity.GetType().GetMethod("TryStart",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                null, new System.Type[] { typeof(bool) }, null);

                            if (_tryStartMethod == null)
                            {
                                _tryStartMethod = _jumpActivity.GetType().GetMethod("TryStart",
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            }
                        }
                    }
                }
            }

            if (_fpController != null && _jumpActivity != null)
            {
                _initDone = true;
                MelonLogger.Msg("JumpModule: Ready! Press Space to jump");
            }
        }

        private static void PerformJump()
        {
            // Перед прыжком снова устанавливаем силу
            if (_fpController != null && _motorJumpForceField != null)
            {
                _motorJumpForceField.SetValue(_fpController, JumpForce);
                MelonLogger.Msg($"JumpModule: Set MotorJumpForce to {JumpForce} before jump");
            }

            // Вызываем прыжок
            if (_jumpActivity != null && _tryStartMethod != null)
            {
                try
                {
                    _tryStartMethod.Invoke(_jumpActivity, new object[] { true });
                    MelonLogger.Msg("JumpModule: Jump invoked!");
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Error($"JumpModule: Error: {ex.Message}");
                }
            }
        }
    }
}