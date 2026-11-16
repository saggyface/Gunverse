using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class LobbySceneBuilder
    {
        private const string LOBBY_SCENE_PATH = "Assets/_GunVerse/Scenes/Lobby.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string DEFAULT_FONT_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const int CONTENT_WIDTH = 420;
        private const int CONTENT_HEIGHT = 1160;
        private const int PLAYER_ROW_COUNT = 8;
        private const float CONTENT_SPACING = 6f;

        private const float BUTTON_HEIGHT = 60f;
        private const float SLIDER_HEIGHT = 24f;
        private const float TITLE_FONT_SIZE = 64f;
        private const float BUTTON_FONT_SIZE = 28f;
        private const float LABEL_FONT_SIZE = 24f;
        private const float VALUE_FONT_SIZE = 20f;
        private const float PLAYER_ROW_FONT_SIZE = 20f;
        private const float STATUS_FONT_SIZE = 22f;

        private static Color buttonNormalColor => ArtPalette.Ui.SurfaceRaised;
        private static Color startButtonColor => ArtPalette.Ui.Primary;
        private static Color primaryTextColor => ArtPalette.Ui.Cream;
        private static Color mutedTextColor => ArtPalette.Ui.CreamMuted;
        private static Color sliderBackgroundColor => ArtPalette.Ui.Ink;
        private static Color cameraBackgroundColor => ArtPalette.Ui.Ink;

        private static TMP_FontAsset defaultFont;

        public static void Build()
        {
            LoadDefaultFont();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildEventSystem();

            Canvas canvas = BuildCanvas();
            LobbyReferences references = BuildPanel(canvas.transform);
            AssignController(canvas, references);
            PreciseValueInputBuilder.PatchLobby();

            EditorSceneManager.SaveScene(scene, LOBBY_SCENE_PATH);
            ConfigureBuildSettings();

            Debug.Log("[LobbySceneBuilder] Built " + LOBBY_SCENE_PATH);
        }

        private static void LoadDefaultFont()
        {
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DEFAULT_FONT_ASSET_PATH);

            if (defaultFont == null)
            {
                defaultFont = TMP_Settings.defaultFontAsset;
            }

            if (defaultFont == null)
            {
                Debug.LogError("[LobbySceneBuilder] No default TMP font asset found.");
            }
        }

        private static void BuildCamera()
        {
            GameObject cameraGameObject = new GameObject("Main Camera");
            cameraGameObject.tag = "MainCamera";

            Camera camera = cameraGameObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cameraBackgroundColor;

            cameraGameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraGameObject.AddComponent<AudioListener>();
        }

        private static void BuildEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas BuildCanvas()
        {
            GameObject canvasGameObject = new GameObject("LobbyCanvas", typeof(RectTransform));
            Canvas canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGameObject.AddComponent<GraphicRaycaster>();
            CreateBackdrop(canvasGameObject.transform);
            return canvas;
        }

        private static void CreateBackdrop(Transform parent)
        {
            GameObject backdrop = CreateUiObject("UiBackdrop", parent);
            backdrop.transform.SetAsFirstSibling();

            RectTransform rectTransform = backdrop.GetComponent<RectTransform>();
            SetAnchors(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image image = backdrop.AddComponent<Image>();
            image.color = ArtPalette.Ui.Ink;
            image.raycastTarget = false;
        }

        private static LobbyReferences BuildPanel(Transform canvasTransform)
        {
            LobbyReferences references = new LobbyReferences();

            GameObject panel = CreatePanel("LobbyPanel", canvasTransform);
            RectTransform content = CreateContent(panel.transform);

            CreateLabel("Title", content, "LOBBY", TITLE_FONT_SIZE, primaryTextColor);
            references.joinCodeText = CreateLabel("JoinCodeText", content, "Join Code: ", LABEL_FONT_SIZE, primaryTextColor);
            references.playerCountText = CreateLabel("PlayerCountText", content, "Players: 0 / 8", LABEL_FONT_SIZE, primaryTextColor);
            references.modeText = CreateLabel("ModeText", content, "Mode: Free For All", LABEL_FONT_SIZE, primaryTextColor);
            references.ffaButton = CreateButton("FfaButton", content, "Free For All", buttonNormalColor);
            references.tdmButton = CreateButton("TdmButton", content, "Team Deathmatch", buttonNormalColor);
            references.gunGameButton = CreateButton("GunGameButton", content, "Gun Game", buttonNormalColor);
            references.mapButton = CreateButton("MapButton", content, "Map: Grocery Store", ArtPalette.Ui.Secondary);
            references.fogButton = CreateButton("FogButton", content, "Store Fog: Off", ArtPalette.Ui.Secondary);
            references.fogSlider = CreateSlider("FogSlider", content, 0f, 1f, StoreFogController.DEFAULT_INTENSITY);
            references.fogValueText = CreateLabel("FogValue", content, "Fog: Off", VALUE_FONT_SIZE, mutedTextColor);

            references.playerRows = new TextMeshProUGUI[PLAYER_ROW_COUNT];
            for (int i = 0; i < PLAYER_ROW_COUNT; i++)
            {
                references.playerRows[i] = CreateLabel($"PlayerRow{i + 1}", content, string.Empty, PLAYER_ROW_FONT_SIZE, primaryTextColor);
                references.playerRows[i].gameObject.SetActive(false);
            }

            references.startButton = CreateButton("StartButton", content, "Start", startButtonColor);
            references.leaveButton = CreateButton("LeaveButton", content, "Leave", ArtPalette.Ui.Danger);
            references.statusText = CreateLabel("StatusText", content, string.Empty, STATUS_FONT_SIZE, mutedTextColor);

            return references;
        }

        private static void AssignController(Canvas canvas, LobbyReferences references)
        {
            LobbyController controller = canvas.gameObject.AddComponent<LobbyController>();
            SerializedObject serializedObject = new SerializedObject(controller);

            Assign(serializedObject, "joinCodeText", references.joinCodeText);
            Assign(serializedObject, "playerCountText", references.playerCountText);
            Assign(serializedObject, "modeText", references.modeText);
            Assign(serializedObject, "ffaButton", references.ffaButton);
            Assign(serializedObject, "tdmButton", references.tdmButton);
            Assign(serializedObject, "gunGameButton", references.gunGameButton);
            Assign(serializedObject, "mapButton", references.mapButton);
            Assign(serializedObject, "fogButton", references.fogButton);
            Assign(serializedObject, "fogSlider", references.fogSlider);
            Assign(serializedObject, "fogValueText", references.fogValueText);
            Assign(serializedObject, "startButton", references.startButton);
            Assign(serializedObject, "leaveButton", references.leaveButton);
            Assign(serializedObject, "statusText", references.statusText);
            AssignArray(serializedObject, "playerRows", references.playerRows);

            serializedObject.ApplyModifiedProperties();
        }

        private static void Assign(SerializedObject serializedObject, string fieldName, Object value)
        {
            serializedObject.FindProperty(fieldName).objectReferenceValue = value;
        }

        private static void AssignArray(SerializedObject serializedObject, string fieldName, TextMeshProUGUI[] values)
        {
            SerializedProperty arrayProperty = serializedObject.FindProperty(fieldName);
            arrayProperty.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = CreateUiObject(name, parent);
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            SetAnchors(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return panel;
        }

        private static RectTransform CreateContent(Transform parent)
        {
            GameObject contentGameObject = CreateUiObject("Content", parent);
            RectTransform rectTransform = contentGameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(CONTENT_WIDTH, CONTENT_HEIGHT);

            VerticalLayoutGroup layout = contentGameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = CONTENT_SPACING;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return rectTransform;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color normalColor)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, BUTTON_HEIGHT);

            Image image = gameObject.AddComponent<Image>();
            image.color = normalColor;

            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = BuildColorBlock();

            TextMeshProUGUI buttonText = CreateLabel("Text", gameObject.transform, label, BUTTON_FONT_SIZE, GetButtonTextColor(normalColor));
            SetAnchors(buttonText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Color GetButtonTextColor(Color background)
        {
            return background == ArtPalette.Ui.SurfaceRaised ? ArtPalette.Ui.Cream : ArtPalette.Ui.Ink;
        }

        private static ColorBlock BuildColorBlock()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            return colors;
        }

        private static Slider CreateSlider(string name, Transform parent, float min, float max, float value)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, SLIDER_HEIGHT);
            Slider slider = gameObject.AddComponent<Slider>();

            GameObject backgroundGameObject = CreateUiObject("Background", gameObject.transform);
            SetAnchors(backgroundGameObject.GetComponent<RectTransform>(), new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            Image background = backgroundGameObject.AddComponent<Image>();
            background.color = sliderBackgroundColor;

            GameObject fillAreaGameObject = CreateUiObject("Fill Area", gameObject.transform);
            SetAnchors(fillAreaGameObject.GetComponent<RectTransform>(), new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(5f, 0f), new Vector2(-15f, 0f));
            GameObject fillGameObject = CreateUiObject("Fill", fillAreaGameObject.transform);
            RectTransform fillRectTransform = fillGameObject.GetComponent<RectTransform>();
            SetAnchors(fillRectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fill = fillGameObject.AddComponent<Image>();
            fill.color = ArtPalette.Ui.Secondary;

            GameObject handleAreaGameObject = CreateUiObject("Handle Slide Area", gameObject.transform);
            SetAnchors(handleAreaGameObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            GameObject handleGameObject = CreateUiObject("Handle", handleAreaGameObject.transform);
            RectTransform handleRectTransform = handleGameObject.GetComponent<RectTransform>();
            handleRectTransform.anchorMin = Vector2.zero;
            handleRectTransform.anchorMax = Vector2.zero;
            handleRectTransform.sizeDelta = new Vector2(20f, 0f);
            Image handle = handleGameObject.AddComponent<Image>();
            handle.color = ArtPalette.Ui.Cream;

            slider.fillRect = fillRectTransform;
            slider.handleRect = handleRectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            return slider;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, string content, float fontSize, Color color)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, fontSize * 1.5f);

            TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
            label.text = content;
            label.font = defaultFont;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
                new EditorBuildSettingsScene(LOBBY_SCENE_PATH, true),
                new EditorBuildSettingsScene(GROCERY_STORE_SCENE_PATH, true),
                new EditorBuildSettingsScene(BOPBUY_SCENE_PATH, true)
            };
        }

        private sealed class LobbyReferences
        {
            public TextMeshProUGUI joinCodeText;
            public TextMeshProUGUI playerCountText;
            public TextMeshProUGUI modeText;
            public Button ffaButton;
            public Button tdmButton;
            public Button gunGameButton;
            public Button mapButton;
            public Button fogButton;
            public Slider fogSlider;
            public TextMeshProUGUI fogValueText;
            public Button startButton;
            public Button leaveButton;
            public TextMeshProUGUI statusText;
            public TextMeshProUGUI[] playerRows;
        }
    }
}
