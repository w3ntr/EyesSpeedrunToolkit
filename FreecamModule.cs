using MelonLoader;
using UnityEngine;

namespace EyesSpeedrunToolkit
{
    public static class FreecamModule
    {
        public static bool IsEnabled = false;
        public static float FlySpeed = 12f;
        public static float FastFlyMultiplier = 3f;
        public static float MouseSensitivity = 2f;

        private static KeyCode toggleKey = KeyCode.F3;
        private static GameObject freecamObj;
        private static Camera freecamComp;
        private static Camera originalMainCam;

        private static float savedTimeScale = 1f;
        private static float rotX = 0f;
        private static float rotY = 0f;
        private static bool _logged = false;

        public static void Initialize()
        {
            MelonLogger.Msg("FreecamModule: Initialized (Toggle: F3)");
        }

        public static void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleFreecam();
            }

            if (!IsEnabled) return;

            // Авто-отключение, если камера пропала
            if (freecamObj == null || originalMainCam == null)
            {
                DisableFreecam();
                return;
            }

            // 1. Поворот камеры мышью
            rotX += Input.GetAxis("Mouse X") * MouseSensitivity * 2f;
            rotY -= Input.GetAxis("Mouse Y") * MouseSensitivity * 2f;
            rotY = Mathf.Clamp(rotY, -89f, 89f);

            freecamObj.transform.rotation = Quaternion.Euler(rotY, rotX, 0f);

            // 2. Полёт (unscaledDeltaTime работает при timeScale = 0)
            float speed = FlySpeed * (Input.GetKey(KeyCode.LeftShift) ? FastFlyMultiplier : 1f) * Time.unscaledDeltaTime;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += freecamObj.transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= freecamObj.transform.forward;
            if (Input.GetKey(KeyCode.D)) move += freecamObj.transform.right;
            if (Input.GetKey(KeyCode.A)) move -= freecamObj.transform.right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;

            freecamObj.transform.position += move * speed;
        }

        public static void ToggleFreecam()
        {
            if (!IsEnabled)
            {
                EnableFreecam();
            }
            else
            {
                DisableFreecam();
            }
        }

        private static void EnableFreecam()
        {
            originalMainCam = Camera.main;
            if (originalMainCam == null)
            {
                MelonLogger.Msg("FreecamModule: ERROR - No main camera found!");
                return;
            }

            IsEnabled = true;

            // Замораживаем время
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Создаём виртуальную камеру
            freecamObj = new GameObject("EyesFreecam");
            freecamObj.tag = "MainCamera";

            freecamComp = freecamObj.AddComponent<Camera>();
            freecamComp.CopyFrom(originalMainCam);

            if (originalMainCam.clearFlags == CameraClearFlags.Depth ||
                originalMainCam.clearFlags == CameraClearFlags.Nothing)
            {
                freecamComp.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                freecamComp.clearFlags = originalMainCam.clearFlags;
            }

            freecamComp.backgroundColor = originalMainCam.backgroundColor;
            freecamComp.cullingMask = originalMainCam.cullingMask;

            // Копируем позицию и вращение
            freecamObj.transform.position = originalMainCam.transform.position;
            freecamObj.transform.rotation = originalMainCam.transform.rotation;

            rotX = freecamObj.transform.eulerAngles.y;
            rotY = freecamObj.transform.eulerAngles.x;

            // Отключаем оригинальную камеру
            originalMainCam.enabled = false;

            // Блокируем курсор
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (!_logged)
            {
                MelonLogger.Msg("FreecamModule: Enabled (Time frozen, use WASD+Mouse to fly)");
                _logged = true;
            }
        }

        public static void DisableFreecam()
        {
            if (!IsEnabled) return;

            IsEnabled = false;

            // Возвращаем время
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;

            if (originalMainCam != null)
            {
                originalMainCam.enabled = true;
            }

            if (freecamObj != null)
            {
                Object.Destroy(freecamObj);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            MelonLogger.Msg("FreecamModule: Disabled");
        }

        public static void Reset()
        {
            if (IsEnabled)
            {
                DisableFreecam();
            }
            _logged = false;
        }
    }
}