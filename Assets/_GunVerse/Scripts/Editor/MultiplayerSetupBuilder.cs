using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class MultiplayerSetupBuilder
    {
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MATERIALS_FOLDER = "Assets/_GunVerse/Materials";
        private const string FLOOR_MATERIAL_PATH = "Assets/_GunVerse/Materials/Floor.mat";
        private const string PLAYER_MATERIAL_PATH = "Assets/_GunVerse/Materials/Player.mat";
        private const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";
        private const string DEFAULT_FONT_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        public const int NetworkTickRate = 60;

        private static readonly Color floorColor = new Color(0.42f, 0.44f, 0.48f, 1f);
        private static readonly Color playerColor = new Color(0.95f, 0.62f, 0.20f, 1f);

        public static void Build()
        {
            EnsureMaterialsFolder();

            Material floorMaterial = GetOrCreateMaterial(FLOOR_MATERIAL_PATH, floorColor);
            GetOrCreateMaterial(PLAYER_MATERIAL_PATH, playerColor);

            Scene mainMenuScene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);

            ConfigureMainMenuScene(mainMenuScene);
            EditorSceneManager.SaveScene(mainMenuScene);

            CreateGroceryStoreScene(floorMaterial);
            ConfigureBuildSettings();

            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            Debug.Log("[MultiplayerSetupBuilder] Setup complete.");
        }

        [MenuItem("Tools/Gun Verse/Apply 60 Hz Network Tick")]
        public static void ApplyNetworkTickRate()
        {
            Scene scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            NetworkManager networkManager = Object.FindFirstObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("[MultiplayerSetupBuilder] NetworkManager not found in MainMenu.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(networkManager);
            SerializedProperty configProperty = serializedObject.FindProperty("NetworkConfig");
            SerializedProperty tickRateProperty = configProperty?.FindPropertyRelative("TickRate");
            if (tickRateProperty == null)
            {
                Debug.LogError("[MultiplayerSetupBuilder] Could not find NetworkConfig.TickRate.");
                return;
            }

            tickRateProperty.intValue = NetworkTickRate;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(networkManager);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MultiplayerSetupBuilder] Set network tick rate to {NetworkTickRate} Hz.");
        }

        private static void EnsureMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder(MATERIALS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_GunVerse", "Materials");
            }
        }

        private static Material GetOrCreateMaterial(string assetPath, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(URP_LIT_SHADER);
            if (shader == null)
            {
                Debug.LogError("[MultiplayerSetupBuilder] URP/Lit shader not found.");
                return null;
            }

            material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void ConfigureMainMenuScene(Scene scene)
        {
            GameObject networkManagerGameObject = new GameObject("NetworkManager");
            NetworkManager networkManager = networkManagerGameObject.AddComponent<NetworkManager>();
            UnityTransport transport = networkManagerGameObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.TickRate = NetworkTickRate;

            GameObject sessionManagerGameObject = new GameObject("SessionManager");
            MultiplayerSessionManager sessionManager = sessionManagerGameObject.AddComponent<MultiplayerSessionManager>();

            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller != null)
            {
                SerializedObject serializedObject = new SerializedObject(controller);
                serializedObject.FindProperty("sessionManager").objectReferenceValue = sessionManager;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void CreateGroceryStoreScene(Material floorMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildFloor(floorMaterial);
            BuildLight();
            BuildCamera();
            BuildJoinCodeDisplay();

            EditorSceneManager.SaveScene(scene, GROCERY_STORE_SCENE_PATH);
        }

        private static void BuildFloor(Material floorMaterial)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);

            if (floorMaterial != null)
            {
                floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
            }
        }

        private static void BuildLight()
        {
            GameObject lightGameObject = new GameObject("Directional Light");
            Light light = lightGameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGameObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void BuildCamera()
        {
            GameObject cameraGameObject = new GameObject("Main Camera");
            cameraGameObject.tag = "MainCamera";

            Camera camera = cameraGameObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 8f, -10f);
            camera.transform.LookAt(Vector3.zero);

            cameraGameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraGameObject.AddComponent<AudioListener>();
        }

        private static void BuildJoinCodeDisplay()
        {
            GameObject canvasGameObject = new GameObject("JoinCodeCanvas", typeof(RectTransform));
            Canvas canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGameObject.AddComponent<CanvasScaler>();
            canvasGameObject.AddComponent<GraphicRaycaster>();

            GameObject textGameObject = new GameObject("JoinCodeText", typeof(RectTransform));
            textGameObject.transform.SetParent(canvasGameObject.transform, false);

            RectTransform rectTransform = textGameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -30f);
            rectTransform.sizeDelta = new Vector2(600f, 60f);

            TextMeshProUGUI label = textGameObject.AddComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DEFAULT_FONT_ASSET_PATH);
            label.fontSize = 32f;
            label.color = ArtPalette.Ui.Cream;
            label.alignment = TextAlignmentOptions.Center;
            label.text = string.Empty;

            textGameObject.AddComponent<JoinCodeDisplay>();
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
                new EditorBuildSettingsScene(GROCERY_STORE_SCENE_PATH, true)
            };
        }
    }
}
