using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class StoreFogUiPatch
    {
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string LOBBY_SCENE_PATH = "Assets/_GunVerse/Scenes/Lobby.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";

        private const float BUTTON_HEIGHT = 60f;
        private const float SLIDER_HEIGHT = 24f;
        private const float BUTTON_FONT_SIZE = 28f;
        private const float VALUE_FONT_SIZE = 20f;

        private static Color ButtonColor => ArtPalette.Ui.SurfaceRaised;
        private static Color AccentColor => ArtPalette.Ui.Secondary;
        private static Color SliderBackgroundColor => ArtPalette.Ui.Ink;
        private static Color TextColor => ArtPalette.Ui.Cream;

        [MenuItem("Tools/Gun Verse/Patch Host Fog UI")]
        public static void Apply()
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            PatchMainMenu();
            PatchLobby();

            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static void PatchMainMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
            Transform content = FindContent("MainMenuCanvas", "LobbyPanel");
            if (controller == null || content == null)
            {
                Debug.LogError("[StoreFogUiPatch] MainMenu controller or lobby content not found.");
                return;
            }

            Button button;
            Slider slider;
            TextMeshProUGUI valueText;
            BuildControls(content, out button, out slider, out valueText);
            AdjustContentLayout(content);

            SerializedObject serializedObject = new SerializedObject(controller);
            Assign(serializedObject, "lobbyFogButton", button);
            Assign(serializedObject, "lobbyFogSlider", slider);
            Assign(serializedObject, "lobbyFogValueText", valueText);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[StoreFogUiPatch] Patched MainMenu host lobby fog controls.");
        }

        private static void PatchLobby()
        {
            Scene scene = EditorSceneManager.OpenScene(LOBBY_SCENE_PATH, OpenSceneMode.Single);
            LobbyController controller = Object.FindAnyObjectByType<LobbyController>(FindObjectsInactive.Include);
            Transform content = FindContent("LobbyCanvas", "LobbyPanel");
            if (controller == null || content == null)
            {
                Debug.LogError("[StoreFogUiPatch] Lobby controller or lobby content not found.");
                return;
            }

            Button button;
            Slider slider;
            TextMeshProUGUI valueText;
            BuildControls(content, out button, out slider, out valueText);
            AdjustContentLayout(content);

            SerializedObject serializedObject = new SerializedObject(controller);
            Assign(serializedObject, "fogButton", button);
            Assign(serializedObject, "fogSlider", slider);
            Assign(serializedObject, "fogValueText", valueText);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[StoreFogUiPatch] Patched Lobby fog controls.");
        }

        private static void BuildControls(
            Transform content,
            out Button button,
            out Slider slider,
            out TextMeshProUGUI valueText)
        {
            RemoveExisting(content, "FogButton");
            RemoveExisting(content, "FogSlider");
            RemoveExisting(content, "FogValue");

            int insertIndex = GetInsertIndex(content);
            button = CreateButton("FogButton", content, "Store Fog: Off");
            button.transform.SetSiblingIndex(insertIndex++);
            slider = CreateSlider("FogSlider", content);
            slider.transform.SetSiblingIndex(insertIndex++);
            valueText = CreateLabel("FogValue", content, "Fog: Off");
            valueText.transform.SetSiblingIndex(insertIndex);
        }

        private static void AdjustContentLayout(Transform content)
        {
            RectTransform rectTransform = content as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(420f, 1160f);
            }

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 6f;
            }
        }

        private static Transform FindContent(string canvasName, string panelName)
        {
            GameObject canvas = GameObject.Find(canvasName);
            if (canvas == null)
            {
                return null;
            }

            Transform panel = canvas.transform.Find(panelName);
            return panel != null ? panel.Find("Content") : null;
        }

        private static int GetInsertIndex(Transform content)
        {
            Transform mapButton = content.Find("MapButton");
            return mapButton != null ? mapButton.GetSiblingIndex() + 1 : content.childCount;
        }

        private static void RemoveExisting(Transform content, string name)
        {
            Transform existing = content.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, BUTTON_HEIGHT);

            Image image = gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateLabel("Text", gameObject.transform, label);
            text.fontSize = BUTTON_FONT_SIZE;
            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private static Slider CreateSlider(string name, Transform parent)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, SLIDER_HEIGHT);
            Slider slider = gameObject.AddComponent<Slider>();

            GameObject backgroundObject = CreateUiObject("Background", gameObject.transform);
            SetAnchors(backgroundObject.GetComponent<RectTransform>(), new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            Image background = backgroundObject.AddComponent<Image>();
            background.color = SliderBackgroundColor;

            GameObject fillAreaObject = CreateUiObject("Fill Area", gameObject.transform);
            SetAnchors(fillAreaObject.GetComponent<RectTransform>(), new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(5f, 0f), new Vector2(-15f, 0f));
            GameObject fillObject = CreateUiObject("Fill", fillAreaObject.transform);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            SetAnchors(fillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fill = fillObject.AddComponent<Image>();
            fill.color = AccentColor;

            GameObject handleAreaObject = CreateUiObject("Handle Slide Area", gameObject.transform);
            SetAnchors(handleAreaObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            GameObject handleObject = CreateUiObject("Handle", handleAreaObject.transform);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.sizeDelta = new Vector2(20f, 0f);
            Image handle = handleObject.AddComponent<Image>();
            handle.color = TextColor;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = StoreFogController.DEFAULT_INTENSITY;
            return slider;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, string content)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, VALUE_FONT_SIZE * 1.5f);

            TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
            label.text = content;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = VALUE_FONT_SIZE;
            label.color = TextColor;
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

        private static void Assign(SerializedObject serializedObject, string fieldName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
