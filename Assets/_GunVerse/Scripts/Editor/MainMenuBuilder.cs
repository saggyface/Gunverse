using System.Linq;
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
    public static class MainMenuBuilder
    {
        private const string MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string TMP_SETTINGS_PATH = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string DEFAULT_FONT_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const int CONTENT_WIDTH = 420;
        private const int MAIN_CONTENT_HEIGHT = 600;
        private const int LOBBY_CONTENT_HEIGHT = 1160;
        private const int PLAYER_ROW_COUNT = 8;
        private const float LOBBY_SPACING = 6f;

        private const float BUTTON_HEIGHT = 60f;
        private const float SLIDER_HEIGHT = 24f;
        private const float INPUT_HEIGHT = 60f;
        private const float TITLE_FONT_SIZE = 64f;
        private const float BUTTON_FONT_SIZE = 28f;
        private const float LABEL_FONT_SIZE = 24f;
        private const float VALUE_FONT_SIZE = 20f;
        private const float PLAYER_ROW_FONT_SIZE = 20f;
        private const float STATUS_FONT_SIZE = 22f;

        private static Color buttonNormalColor => ArtPalette.Ui.SurfaceRaised;
        private static Color startButtonColor => ArtPalette.Ui.Primary;
        private static Color accentColor => ArtPalette.Ui.Accent;
        private static Color primaryTextColor => ArtPalette.Ui.Cream;
        private static Color mutedTextColor => ArtPalette.Ui.CreamMuted;
        private static Color inputBackgroundColor => ArtPalette.Ui.Surface;
        private static Color sliderBackgroundColor => ArtPalette.Ui.Ink;
        private static Color cameraBackgroundColor => ArtPalette.Ui.Ink;

        private static TMP_FontAsset defaultFont;

        public static void Build()
        {
            ImportTextMeshProEssentials();
            LoadDefaultFont();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildEventSystem();

            Canvas canvas = BuildCanvas();
            MenuReferences references = BuildPanels(canvas.transform);
            AssignController(canvas, references);
            PreciseValueInputBuilder.PatchMainMenu();

            EditorSceneManager.SaveScene(scene, MENU_SCENE_PATH);
            AddSceneToBuildSettings();

            Debug.Log("[MainMenuBuilder] Built " + MENU_SCENE_PATH);
        }

        private static void ImportTextMeshProEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TMP_SETTINGS_PATH) != null)
            {
                return;
            }

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
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
                Debug.LogError("[MainMenuBuilder] No default TMP font asset found.");
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
            GameObject canvasGameObject = new GameObject("MainMenuCanvas");
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

        private static MenuReferences BuildPanels(Transform canvasTransform)
        {
            MenuReferences references = new MenuReferences();

            references.mainPanel = BuildMainPanel(canvasTransform, references);
            references.usernamePanel = BuildUsernamePanel(canvasTransform, references);
            references.hostJoinPanel = BuildHostJoinPanel(canvasTransform, references);
            references.joinCodePanel = BuildJoinCodePanel(canvasTransform, references);
            references.lobbyPanel = BuildLobbyPanel(canvasTransform, references);
            references.settingsPanel = BuildSettingsPanel(canvasTransform, references);

            return references;
        }

        private static GameObject BuildMainPanel(Transform canvasTransform, MenuReferences references)
        {
            GameObject panel = CreatePanel("MainPanel", canvasTransform);
            RectTransform content = CreateContent(panel.transform, MAIN_CONTENT_HEIGHT);

            CreateLabel("Title", content, "GUNVERSE", TITLE_FONT_SIZE, ArtPalette.Ui.Primary);
            references.playButton = CreateButton("PlayButton", content, "Play", ArtPalette.Ui.Primary);
            references.settingsButton = CreateButton("SettingsButton", content, "Settings", ArtPalette.Ui.Secondary);
            references.quitButton = CreateButton("QuitButton", content, "Quit", ArtPalette.Ui.Danger);

            return panel;
        }

        private static GameObject BuildUsernamePanel(Transform canvasTransform, MenuReferences references)
        {
            GameObject panel = CreatePanel("UsernamePanel", canvasTransform);
            panel.SetActive(false);

            RectTransform content = CreateContent(panel.transform, MAIN_CONTENT_HEIGHT);

            CreateLabel("Title", content, "USERNAME", TITLE_FONT_SIZE, primaryTextColor);
            references.usernameInput = CreateInputField("UsernameInput", content, "Enter username");
            references.usernameInput.characterLimit = GunVerse.GameSettings.PLAYER_NAME_MAX_LENGTH;
            references.usernameErrorText = CreateLabel("UsernameError", content, string.Empty, STATUS_FONT_SIZE, ArtPalette.Ui.Danger);
            references.usernameContinueButton = CreateButton("ContinueButton", content, "Continue", ArtPalette.Ui.Primary);
            references.usernameBackButton = CreateButton("BackButton", content, "Back", ArtPalette.Ui.Secondary);

            return panel;
        }

        private static GameObject BuildHostJoinPanel(Transform canvasTransform, MenuReferences references)
        {
            GameObject panel = CreatePanel("HostJoinPanel", canvasTransform);
            panel.SetActive(false);

            RectTransform content = CreateContent(panel.transform, MAIN_CONTENT_HEIGHT);

            CreateLabel("Title", content, "PLAY", TITLE_FONT_SIZE, primaryTextColor);
            references.hostButton = CreateButton("HostButton", content, "Host", ArtPalette.Ui.Primary);
            references.joinButton = CreateButton("JoinButton", content, "Join", ArtPalette.Ui.Primary);
            references.hostJoinBackButton = CreateButton("BackButton", content, "Back", ArtPalette.Ui.Secondary);
            references.hostJoinStatusText = CreateLabel("StatusText", content, string.Empty, STATUS_FONT_SIZE, mutedTextColor);

            return panel;
        }

        private static GameObject BuildJoinCodePanel(Transform canvasTransform, MenuReferences references)
        {
            GameObject panel = CreatePanel("JoinCodePanel", canvasTransform);
            panel.SetActive(false);

            RectTransform content = CreateContent(panel.transform, MAIN_CONTENT_HEIGHT);

            CreateLabel("Title", content, "JOIN", TITLE_FONT_SIZE, primaryTextColor);
            references.joinCodeInput = CreateInputField("JoinCodeInput", content, "Enter join code");
            references.joinLobbyButton = CreateButton("JoinLobbyButton", content, "Join Lobby", ArtPalette.Ui.Primary);
            references.joinCodeBackButton = CreateButton("BackButton", content, "Back", ArtPalette.Ui.Secondary);
            references.joinCodeStatusText = CreateLabel("StatusText", content, string.Empty, STATUS_FONT_SIZE, mutedTextColor);

            return panel;
        }

        private static GameObject BuildLobbyPanel(Transform canvasTransform, MenuReferences references)
        {
            GameObject panel = CreatePanel("LobbyPanel", canvasTransform);
            panel.SetActive(false);

            RectTransform content = CreateContent(panel.transform, LOBBY_CONTENT_HEIGHT, LOBBY_SPACING);

            CreateLabel("Title", content, "LOBBY", TITLE_FONT_SIZE, primaryTextColor);
            references.lobbyJoinCodeText = CreateLabel("LobbyJoinCodeText", content, "Join Code: ", LABEL_FONT_SIZE, primaryTextColor);
            references.lobbyPlayerCountText = CreateLabel("LobbyPlayerCountText", content, "Players: 0 / 8", LABEL_FONT_SIZE, primaryTextColor);
            references.lobbyModeText = CreateLabel("LobbyModeText", content, "Mode: Free For All", LABEL_FONT_SIZE, primaryTextColor);
            references.lobbyFfaButton = CreateButton("FfaButton", content, "Free For All", buttonNormalColor);
            references.lobbyTdmButton = CreateButton("TdmButton", content, "Team Deathmatch", buttonNormalColor);
            references.lobbyGunGameButton = CreateButton("GunGameButton", content, "Gun Game", buttonNormalColor);
            references.lobbyMapButton = CreateButton("MapButton", content, "Map: Grocery Store", ArtPalette.Ui.Secondary);
            references.lobbyFogButton = CreateButton("FogButton", content, "Store Fog: Off", ArtPalette.Ui.Secondary);
            references.lobbyFogSlider = CreateSlider("FogSlider", content, 0f, 1f, StoreFogController.DEFAULT_INTENSITY);
            references.lobbyFogValueText = CreateLabel("FogValue", content, "Fog: Off", VALUE_FONT_SIZE, mutedTextColor);

            references.lobbyPlayerRows = new TextMeshProUGUI[PLAYER_ROW_COUNT];
            for (int i = 0; i < PLAYER_ROW_COUNT; i++)
            {
                references.lobbyPlayerRows[i] = CreateLabel($"PlayerRow{i + 1}", content, string.Empty, PLAYER_ROW_FONT_SIZE, primaryTextColor);
                references.lobbyPlayerRows[i].gameObject.SetActive(false);
            }

            references.lobbyStartButton = CreateButton("StartButton", content, "Start", startButtonColor);
            references.lobbyLeaveButton = CreateButton("LeaveButton", content, "Leave", ArtPalette.Ui.Danger);
            references.lobbyStatusText = CreateLabel("LobbyStatusText", content, string.Empty, STATUS_FONT_SIZE, mutedTextColor);

            return panel;
        }

        private static GameObject BuildSettingsPanel(Transform canvasTransform, MenuReferences references)
        {
            GameObject panel = CreatePanel("SettingsPanel", canvasTransform);
            panel.SetActive(false);

            RectTransform content = CreateContent(panel.transform, MAIN_CONTENT_HEIGHT);

            CreateLabel("Title", content, "SETTINGS", TITLE_FONT_SIZE, primaryTextColor);

            CreateLabel("SensitivityLabel", content, "Mouse Sensitivity", LABEL_FONT_SIZE, primaryTextColor);
            references.sensitivitySlider = CreateSlider("SensitivitySlider", content, GameSettings.SENSITIVITY_MIN, GameSettings.SENSITIVITY_MAX, GameSettings.SENSITIVITY_DEFAULT);
            references.sensitivityValueText = CreateLabel("SensitivityValue", content, string.Empty, VALUE_FONT_SIZE, mutedTextColor);

            CreateLabel("VolumeLabel", content, "Master Volume", LABEL_FONT_SIZE, primaryTextColor);
            references.volumeSlider = CreateSlider("VolumeSlider", content, GameSettings.MASTER_VOLUME_MIN, GameSettings.MASTER_VOLUME_MAX, GameSettings.MASTER_VOLUME_DEFAULT);
            references.volumeValueText = CreateLabel("VolumeValue", content, string.Empty, VALUE_FONT_SIZE, mutedTextColor);

            references.settingsBackButton = CreateButton("BackButton", content, "Back", ArtPalette.Ui.Secondary);

            return panel;
        }

        private static void AssignController(Canvas canvas, MenuReferences references)
        {
            MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>();

            // SerializedObject.FindProperty requires the serialized field's name as a string; the
            // fields are private so nameof(...) cannot reference them from this class.
            SerializedObject serializedObject = new SerializedObject(controller);

            Assign(serializedObject, "mainPanel", references.mainPanel);
            Assign(serializedObject, "usernamePanel", references.usernamePanel);
            Assign(serializedObject, "hostJoinPanel", references.hostJoinPanel);
            Assign(serializedObject, "joinCodePanel", references.joinCodePanel);
            Assign(serializedObject, "lobbyPanel", references.lobbyPanel);
            Assign(serializedObject, "settingsPanel", references.settingsPanel);

            Assign(serializedObject, "playButton", references.playButton);
            Assign(serializedObject, "settingsButton", references.settingsButton);
            Assign(serializedObject, "quitButton", references.quitButton);

            Assign(serializedObject, "usernameInput", references.usernameInput);
            Assign(serializedObject, "usernameContinueButton", references.usernameContinueButton);
            Assign(serializedObject, "usernameBackButton", references.usernameBackButton);
            Assign(serializedObject, "usernameErrorText", references.usernameErrorText);

            Assign(serializedObject, "hostButton", references.hostButton);
            Assign(serializedObject, "joinButton", references.joinButton);
            Assign(serializedObject, "hostJoinBackButton", references.hostJoinBackButton);
            Assign(serializedObject, "hostJoinStatusText", references.hostJoinStatusText);

            Assign(serializedObject, "joinCodeInput", references.joinCodeInput);
            Assign(serializedObject, "joinLobbyButton", references.joinLobbyButton);
            Assign(serializedObject, "joinCodeBackButton", references.joinCodeBackButton);
            Assign(serializedObject, "joinCodeStatusText", references.joinCodeStatusText);

            Assign(serializedObject, "lobbyJoinCodeText", references.lobbyJoinCodeText);
            Assign(serializedObject, "lobbyPlayerCountText", references.lobbyPlayerCountText);
            Assign(serializedObject, "lobbyModeText", references.lobbyModeText);
            Assign(serializedObject, "lobbyFfaButton", references.lobbyFfaButton);
            Assign(serializedObject, "lobbyTdmButton", references.lobbyTdmButton);
            Assign(serializedObject, "lobbyGunGameButton", references.lobbyGunGameButton);
            Assign(serializedObject, "lobbyMapButton", references.lobbyMapButton);
            Assign(serializedObject, "lobbyFogButton", references.lobbyFogButton);
            Assign(serializedObject, "lobbyFogSlider", references.lobbyFogSlider);
            Assign(serializedObject, "lobbyFogValueText", references.lobbyFogValueText);
            Assign(serializedObject, "lobbyStartButton", references.lobbyStartButton);
            Assign(serializedObject, "lobbyLeaveButton", references.lobbyLeaveButton);
            Assign(serializedObject, "lobbyStatusText", references.lobbyStatusText);
            AssignArray(serializedObject, "lobbyPlayerRows", references.lobbyPlayerRows);

            Assign(serializedObject, "settingsBackButton", references.settingsBackButton);
            Assign(serializedObject, "sensitivitySlider", references.sensitivitySlider);
            Assign(serializedObject, "volumeSlider", references.volumeSlider);
            Assign(serializedObject, "sensitivityValueText", references.sensitivityValueText);
            Assign(serializedObject, "volumeValueText", references.volumeValueText);

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

        private static RectTransform CreateContent(Transform parent, int height, float spacing = 16f)
        {
            GameObject contentGameObject = CreateUiObject("Content", parent);
            RectTransform rectTransform = contentGameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(CONTENT_WIDTH, height);

            VerticalLayoutGroup layout = contentGameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = spacing;
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

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, INPUT_HEIGHT);

            Image background = gameObject.AddComponent<Image>();
            background.color = ArtPalette.Ui.Surface;

            TMP_InputField inputField = gameObject.AddComponent<TMP_InputField>();

            GameObject textAreaGameObject = CreateUiObject("Text Area", gameObject.transform);
            SetAnchors(textAreaGameObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            textAreaGameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI placeholder = CreateLabel("Placeholder", textAreaGameObject.transform, placeholderText, LABEL_FONT_SIZE, ArtPalette.Ui.CreamMuted);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(placeholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI text = CreateLabel("Text", textAreaGameObject.transform, string.Empty, LABEL_FONT_SIZE, ArtPalette.Ui.Cream);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.targetGraphic = background;
            inputField.caretColor = ArtPalette.Ui.Primary;

            return inputField;
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

        private static void AddSceneToBuildSettings()
        {
            bool alreadyPresent = EditorBuildSettings.scenes.Any(scene => scene.path == MENU_SCENE_PATH);
            if (alreadyPresent)
            {
                return;
            }

            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Append(new EditorBuildSettingsScene(MENU_SCENE_PATH, true))
                .ToArray();
        }

        private sealed class MenuReferences
        {
            public GameObject mainPanel;
            public GameObject usernamePanel;
            public GameObject hostJoinPanel;
            public GameObject joinCodePanel;
            public GameObject lobbyPanel;
            public GameObject settingsPanel;

            public Button playButton;
            public Button settingsButton;
            public Button quitButton;

            public TMP_InputField usernameInput;
            public Button usernameContinueButton;
            public Button usernameBackButton;
            public TextMeshProUGUI usernameErrorText;

            public Button hostButton;
            public Button joinButton;
            public Button hostJoinBackButton;
            public TextMeshProUGUI hostJoinStatusText;

            public TMP_InputField joinCodeInput;
            public Button joinLobbyButton;
            public Button joinCodeBackButton;
            public TextMeshProUGUI joinCodeStatusText;

            public TextMeshProUGUI lobbyJoinCodeText;
            public TextMeshProUGUI lobbyPlayerCountText;
            public TextMeshProUGUI lobbyModeText;
            public Button lobbyFfaButton;
            public Button lobbyTdmButton;
            public Button lobbyGunGameButton;
            public Button lobbyMapButton;
            public Button lobbyFogButton;
            public Slider lobbyFogSlider;
            public TextMeshProUGUI lobbyFogValueText;
            public Button lobbyStartButton;
            public Button lobbyLeaveButton;
            public TextMeshProUGUI lobbyStatusText;
            public TextMeshProUGUI[] lobbyPlayerRows;

            public Button settingsBackButton;
            public Slider sensitivitySlider;
            public Slider volumeSlider;
            public TextMeshProUGUI sensitivityValueText;
            public TextMeshProUGUI volumeValueText;
        }
    }
}
