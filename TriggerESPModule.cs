using UnityEngine;
using System.Collections.Generic;
using MelonLoader;

namespace EyesSpeedrunToolkit
{
    public static class TriggerESPModule
    {
        public static bool IsEnabled = false;
        public static bool ShowThroughWalls = true; // X-Ray по умолчанию
        public static bool ShowLabels = true;
        public static bool WireframeMode = false;
        public static float MinSize = 1.5f; // Отсекаем мелкие триггеры, оставляем только комнаты/двери

        private static List<TriggerInfo> triggers = new List<TriggerInfo>();
        public static int TriggerCount = 0;
        private static List<GameObject> overlayObjects = new List<GameObject>();
        private static Material overlayMaterial;
        private static Mesh wireframeMesh;

        private class TriggerInfo
        {
            public GameObject gameObject;
            public Collider collider;
            public Vector3 center;
            public Vector3 size;
            public float maxSize;
            public string type;
        }

        public static void Initialize()
        {
            MelonLogger.Msg("[TriggerESP] Initialized");
            CreateMaterial();
            CreateWireframeMesh();
        }

        private static void CreateMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("GUI/Text Shader");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            overlayMaterial = new Material(shader);
            UpdateMaterialProperties();
        }

        private static void UpdateMaterialProperties()
        {
            if (overlayMaterial == null) return;
            overlayMaterial.SetInt("_SrcBlend", 5);
            overlayMaterial.SetInt("_DstBlend", 10);
            overlayMaterial.SetInt("_Cull", 0);
            overlayMaterial.SetInt("_ZWrite", 0);
            overlayMaterial.SetInt("_ZTest", ShowThroughWalls ? 8 : 4); // 8 = Always (X-Ray)
            overlayMaterial.color = new Color(1f, 0.1f, 0.1f, 0.2f); // Полупрозрачный красный
        }

        private static void CreateWireframeMesh()
        {
            if (wireframeMesh != null) return;
            wireframeMesh = new Mesh();
            Vector3[] verts = new Vector3[8] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f)
            };
            int[] lines = new int[24] {
                0,1, 1,2, 2,3, 3,0, 4,5, 5,6, 6,7, 7,4, 0,4, 1,5, 2,6, 3,7
            };
            wireframeMesh.vertices = verts;
            wireframeMesh.SetIndices(lines, MeshTopology.Lines, 0);
        }

        public static void OnSceneLoaded()
        {
            if (IsEnabled) FindTriggers();
        }

        public static void FindTriggers()
        {
            ClearOverlays();
            triggers.Clear();
            MelonLogger.Msg("[TriggerESP] Searching for triggers...");

            Collider[] colliders = Object.FindObjectsOfType<Collider>();
            int count = 0;

            foreach (Collider col in colliders)
            {
                if (col == null || !col.isTrigger) continue;
                float maxSize = Mathf.Max(col.bounds.size.x, col.bounds.size.y, col.bounds.size.z);
                if (maxSize < MinSize) continue;
                if (col.gameObject.CompareTag("Player")) continue; // Игнорируем триггеры игрока

                triggers.Add(new TriggerInfo
                {
                    gameObject = col.gameObject,
                    collider = col,
                    center = col.bounds.center,
                    size = col.bounds.size,
                    maxSize = maxSize,
                    type = col.GetType().Name
                });
                count++;
            }

            TriggerCount = count;
            MelonLogger.Msg($"[TriggerESP] Found {count} large triggers (min size: {MinSize}m)");
            CreateOverlays();
        }

        private static void CreateOverlays()
        {
            foreach (TriggerInfo trigger in triggers)
            {
                if (trigger.gameObject == null || trigger.collider == null) continue;

                GameObject overlayObj;
                if (WireframeMode)
                {
                    overlayObj = new GameObject("[TriggerWire]");
                    overlayObj.AddComponent<MeshFilter>().sharedMesh = wireframeMesh;
                    MeshRenderer mr = overlayObj.AddComponent<MeshRenderer>();
                    mr.material = overlayMaterial;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
                else
                {
                    overlayObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(overlayObj.GetComponent<Collider>());
                    Renderer ren = overlayObj.GetComponent<Renderer>();
                    if (ren != null)
                    {
                        ren.material = overlayMaterial;
                        ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        ren.receiveShadows = false;
                    }
                }

                Bounds bounds = trigger.collider.bounds;
                overlayObj.transform.position = bounds.center;
                overlayObj.transform.localScale = bounds.size + new Vector3(0.05f, 0.05f, 0.05f);
                overlayObjects.Add(overlayObj);
            }
        }

        public static void ClearOverlays()
        {
            foreach (GameObject obj in overlayObjects)
            {
                if (obj != null) Object.Destroy(obj);
            }
            overlayObjects.Clear();
        }

        public static void OnGUI()
        {
            if (!IsEnabled || Camera.main == null) return;

            if (ShowLabels)
            {
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };

                foreach (TriggerInfo trigger in triggers)
                {
                    if (trigger.gameObject == null) continue;
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(trigger.center);

                    if (screenPos.z > 0)
                    {
                        Color labelColor = trigger.maxSize > 3f ? Color.red : (trigger.maxSize > 1.5f ? Color.yellow : Color.cyan);
                        GUI.color = labelColor;
                        string label = $"[{trigger.type}]\n{trigger.maxSize:F1}m";
                        GUI.Label(new Rect(screenPos.x - 40, Screen.height - screenPos.y + 20, 80, 40), label, labelStyle);
                    }
                }
                GUI.color = Color.white;
            }

            // Счётчик (можно убрать, если он дублирует вотермарку, но для отладки полезен)
            GUIStyle counterStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.3f, 0.3f) } };
            GUI.Label(new Rect(15, Screen.height - 30, 200, 20), $"Triggers: {TriggerCount}", counterStyle);
        }
    }
}