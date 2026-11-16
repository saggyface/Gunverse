using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class PauseMenuBuilder
    {
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string DEFAULT_FONT_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const int CONTENT_WIDTH = 420;
        private const int PANEL_CONTENT_HEIGHT = 600;
        private const int CONFIRM_CONTENT_HEIGHT = 400;

        private const float BUTTON_HEIGHT = 60f;
        private const float SLIDER_HEIGHT = 24f;
        private const float TITLE_FONT_SIZE = 64f;
        private const float BUTTON_FONT_SIZE = 28f;
        private const float LABEL_FONT_SIZE = 24f;
        private const float VALUE_FONT_SIZE = 20f;

        private static Color buttonNormalColor => ArtPalette.Ui.SurfaceRaised;
        private static Color confirmButtonColor => ArtPalette.Ui.Danger;
        private static Color primaryTextColor => ArtPalette.Ui.Cream;
        private static Color mutedTextColor => ArtPalette.Ui.CreamMuted;
        private static Color sliderBackgroundColor => ArtPalette.Ui.Ink;

        private static TMP_FontAsset defaultFont;

        public static void Build()
        {
            LoadDefaultFont();

            Scene scene = EditorSceneManager.OpenScene(GROCERY_STORE_SCENE_PATH, OpenSceneMode.Single);
            BuildEventSystem();
            BuildPauseMenu();

            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);

            Debug.Log("[PauseMenuBuilder] Built pause menu in " + GROCERY_STORE_SCENE_PATH);
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
                Debug.LogError("[PauseMenuBuilder] No default TMP font asset found.");
            }
        }

        private static void BuildEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static void BuildPauseMenu()
        {
            GameObject canvasGameObject = new GameObject("PauseMenuCanvas", typeof(RectTransform));
            Canvas canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGameObject.AddComponent<GraphicRaycaster>();

            PauseReferences references = BuildPanels(canvasGameObject.transform);
            AssignController(canvasGameObject, references);
            PreciseValueInputBuilder.PatchPauseMenu();
        }

        private static PauseReferences BuildPanels(Transform canvasTransform)
        {
            PauseReferences references = new PauseReferences();

            references.pausePanel = BuildPausePanel(canvasTransform, references);
            references.settingsPanel = BuildSettingsPanel(canvasTransform, references);
            references.confirmPanel = BuildConfirmPanel(canvasTransform, references);

            return references;
        }

        private static GameObject BuildPausePanel(Transform canvasTransform, PauseReferences references)
        {
            GameObject panel = CreatePanel("PausePanel", canvasTransform);
            panel.SetActive(false);

            RectTransform content = CreateContent(panel.transform, PANEL_CONTENT_HEIGHT);

            CreateLabel("Title", content, "PAUSED", TITLE_FONT_SIZE, ArtPalette.Ui.Primary);
            references.resumeButton = CreateButton("ResumeButton", content, "Resume", ArtPalette.Ui.Primary);
            references.settingsButton = CreateButton("SettingsButton", content, "Settings", ArtPalette.Ui.Secondary);
            references.leaveGameButton = CreateButton("LeaveGameButton", content, "Leave Game", ArtPalette.Ui.Danger);

            return panel;
        }

        private static GameObject BuildSettingsPanel(Transform canvasTransform, PauseReferences references)
        {
            GameObject panel = CreatePanel("SettingsPanel", canvasTransform);
            panel.SetActive(false);

            RectTransform content = CreateContent(panel.transform, PANEL_CONTENT_HEIGHT);

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

        private static GameObject BuildConfirmPanel(Transform canvasTransform, PauseReferences references)
        {
            GameObject panel = CreatePanel("ConfirmPanel", canvasTransform);
            panel.SetActive(false);

            RectTransform content = CreateContent(panel.transform, CONFIRM_CONTENT_HEIGHT);

            references.confirmText = CreateLabel("ConfirmText", content, "Leave the game?", LABEL_FONT_SIZE, primaryTextColor);
            references.confirmButton = CreateButton("ConfirmButton", content, "Leave Game", confirmButtonColor);
            references.cancelButton = CreateButton("CancelButton", content, "Cancel", ArtPalette.Ui.Secondary);

            return panel;
        }

        private static void AssignController(GameObject canvasGameObject, PauseReferences references)
        {
            PauseMenuController controller = canvasGameObject.AddComponent<PauseMenuController>();
            SerializedObject serializedObject = new SerializedObject(controller);

            Assign(serializedObject, "pausePanel", references.pausePanel);
            Assign(serializedObject, "settingsPanel", references.settingsPanel);
            Assign(serializedObject, "confirmPanel", references.confirmPanel);

            Assign(serializedObject, "resumeButton", references.resumeButton);
            Assign(serializedObject, "settingsButton", references.settingsButton);
            Assign(serializedObject, "leaveGameButton", references.leaveGameButton);

            Assign(serializedObject, "settingsBackButton", references.settingsBackButton);
            Assign(serializedObject, "sensitivitySlider", references.sensitivitySlider);
            Assign(serializedObject, "volumeSlider", references.volumeSlider);
            Assign(serializedObject, "sensitivityValueText", references.sensitivityValueText);
            Assign(serializedObject, "volumeValueText", references.volumeValueText);

            Assign(serializedObject, "confirmText", references.confirmText);
            Assign(serializedObject, "confirmButton", references.confirmButton);
            Assign(serializedObject, "cancelButton", references.cancelButton);

            serializedObject.ApplyModifiedProperties();
        }

        private static void Assign(SerializedObject serializedObject, string fieldName, Object value)
        {
            serializedObject.FindProperty(fieldName).objectReferenceValue = value;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = CreateUiObject(name, parent);
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            SetAnchors(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image image = panel.AddComponent<Image>();
            image.color = ArtPalette.Ui.PauseBackdrop;
            image.raycastTarget = false;
            return panel;
        }

        private static RectTransform CreateContent(Transform parent, int height)
        {
            GameObject contentGameObject = CreateUiObject("Content", parent);
            RectTransform rectTransform = contentGameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(CONTENT_WIDTH, height);

            VerticalLayoutGroup layout = contentGameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 16f;
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

        private sealed class PauseReferences
        {
            public GameObject pausePanel;
            public GameObject settingsPanel;
            public GameObject confirmPanel;

            public Button resumeButton;
            public Button settingsButton;
            public Button leaveGameButton;

            public Button settingsBackButton;
            public Slider sensitivitySlider;
            public Slider volumeSlider;
            public TextMeshProUGUI sensitivityValueText;
            public TextMeshProUGUI volumeValueText;

            public TextMeshProUGUI confirmText;
            public Button confirmButton;
            public Button cancelButton;
        }
    }
}
