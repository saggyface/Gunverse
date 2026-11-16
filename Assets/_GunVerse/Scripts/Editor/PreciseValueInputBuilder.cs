using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class PreciseValueInputBuilder
    {
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string LOBBY_SCENE_PATH = "Assets/_GunVerse/Scenes/Lobby.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";

        private const string SENSITIVITY_INPUT_NAME = "SensitivityInput";
        private const string VOLUME_INPUT_NAME = "VolumeInput";
        private const string FOG_INPUT_NAME = "FogInput";
        private const string TEXT_AREA_NAME = "Text Area";
        private const int SENSITIVITY_CHARACTER_LIMIT = 5;
        private const int PERCENT_CHARACTER_LIMIT = 3;
        private const float DEFAULT_INPUT_HEIGHT = 30f;
        private const float TEXT_AREA_HORIZONTAL_PADDING = 8f;
        private const float TEXT_AREA_VERTICAL_PADDING = 2f;

        [MenuItem("Tools/Gun Verse/UI/Add Precise Value Inputs")]
        public static void Apply()
        {
            ApplyScene(MAIN_MENU_SCENE_PATH, PatchMainMenu);
            ApplyScene(LOBBY_SCENE_PATH, PatchLobby);
            ApplyScene(BOPBUY_SCENE_PATH, PatchPauseMenu);
            ApplyScene(GROCERY_STORE_SCENE_PATH, PatchPauseMenu);
            Debug.Log("[PreciseValueInputBuilder] Added precise numeric inputs for sensitivity, volume, and fog.");
        }

        public static string BuildValidationReport()
        {
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("[PreciseValueInputBuilder] Validation");

            string previousScenePath = SceneManager.GetActiveScene().path;
            bool hasPreviousScene = !string.IsNullOrEmpty(previousScenePath);

            ValidateMainMenu(report);
            ValidateLobby(report);
            ValidatePauseMenu(BOPBUY_SCENE_PATH, report);
            ValidatePauseMenu(GROCERY_STORE_SCENE_PATH, report);

            if (hasPreviousScene)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            return report.ToString();
        }

        private static void ValidateMainMenu(System.Text.StringBuilder report)
        {
            Scene scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
            report.AppendLine("Scene: " + scene.name);
            report.AppendLine("  MainMenuController: " + (controller != null));

            if (controller == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            report.AppendLine("  Sensitivity input: " + DescribeInput(GetInput(serializedObject, "sensitivityInput")));
            report.AppendLine("  Volume input: " + DescribeInput(GetInput(serializedObject, "volumeInput")));
            report.AppendLine("  Fog input: " + DescribeInput(GetInput(serializedObject, "lobbyFogInput")));
        }

        private static void ValidateLobby(System.Text.StringBuilder report)
        {
            Scene scene = EditorSceneManager.OpenScene(LOBBY_SCENE_PATH, OpenSceneMode.Single);
            LobbyController controller = Object.FindAnyObjectByType<LobbyController>(FindObjectsInactive.Include);
            report.AppendLine("Scene: " + scene.name);
            report.AppendLine("  LobbyController: " + (controller != null));

            if (controller == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            report.AppendLine("  Fog input: " + DescribeInput(GetInput(serializedObject, "fogInput")));
        }

        private static void ValidatePauseMenu(string scenePath, System.Text.StringBuilder report)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            PauseMenuController controller = Object.FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            report.AppendLine("Scene: " + scene.name);
            report.AppendLine("  PauseMenuController: " + (controller != null));

            if (controller == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            report.AppendLine("  Sensitivity input: " + DescribeInput(GetInput(serializedObject, "sensitivityInput")));
            report.AppendLine("  Volume input: " + DescribeInput(GetInput(serializedObject, "volumeInput")));
        }

        private static TMP_InputField GetInput(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as TMP_InputField : null;
        }

        private static string DescribeInput(TMP_InputField input)
        {
            if (input == null)
            {
                return "MISSING";
            }

            bool hasText = input.textComponent != null;
            bool hasViewport = input.textViewport != null;
            bool hasGraphic = input.targetGraphic != null;
            return $"present text={hasText} viewport={hasViewport} graphic={hasGraphic} type={input.contentType}";
        }

        private static void ApplyScene(string scenePath, System.Action apply)
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            bool hasPreviousScene = !string.IsNullOrEmpty(previousScenePath);

            if (hasPreviousScene)
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            apply();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (hasPreviousScene)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        public static void PatchMainMenu()
        {
            MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogError("[PreciseValueInputBuilder] MainMenuController not found.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            TextMeshProUGUI sensitivityLabel = GetText(serializedObject, "sensitivityValueText");
            TextMeshProUGUI volumeLabel = GetText(serializedObject, "volumeValueText");
            TextMeshProUGUI fogLabel = GetText(serializedObject, "lobbyFogValueText");

            TMP_InputField sensitivityInput = EnsureNumericInput(
                sensitivityLabel,
                SENSITIVITY_INPUT_NAME,
                TMP_InputField.ContentType.DecimalNumber,
                SENSITIVITY_CHARACTER_LIMIT,
                NormalizeSensitivity(sensitivityLabel != null ? sensitivityLabel.text : string.Empty));

            TMP_InputField volumeInput = EnsureNumericInput(
                volumeLabel,
                VOLUME_INPUT_NAME,
                TMP_InputField.ContentType.IntegerNumber,
                PERCENT_CHARACTER_LIMIT,
                NormalizePercent(volumeLabel != null ? volumeLabel.text : string.Empty, 100));

            TMP_InputField fogInput = EnsureNumericInput(
                fogLabel,
                FOG_INPUT_NAME,
                TMP_InputField.ContentType.IntegerNumber,
                PERCENT_CHARACTER_LIMIT,
                NormalizePercent(fogLabel != null ? fogLabel.text : string.Empty, 25));

            Assign(serializedObject, "sensitivityInput", sensitivityInput);
            Assign(serializedObject, "volumeInput", volumeInput);
            Assign(serializedObject, "lobbyFogInput", fogInput);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        public static void PatchLobby()
        {
            LobbyController controller = Object.FindAnyObjectByType<LobbyController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogError("[PreciseValueInputBuilder] LobbyController not found.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            TextMeshProUGUI fogLabel = GetText(serializedObject, "fogValueText");
            TMP_InputField fogInput = EnsureNumericInput(
                fogLabel,
                FOG_INPUT_NAME,
                TMP_InputField.ContentType.IntegerNumber,
                PERCENT_CHARACTER_LIMIT,
                NormalizePercent(fogLabel != null ? fogLabel.text : string.Empty, 25));

            Assign(serializedObject, "fogInput", fogInput);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        public static void PatchPauseMenu()
        {
            PauseMenuController controller = Object.FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogError("[PreciseValueInputBuilder] PauseMenuController not found.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            TextMeshProUGUI sensitivityLabel = GetText(serializedObject, "sensitivityValueText");
            TextMeshProUGUI volumeLabel = GetText(serializedObject, "volumeValueText");

            TMP_InputField sensitivityInput = EnsureNumericInput(
                sensitivityLabel,
                SENSITIVITY_INPUT_NAME,
                TMP_InputField.ContentType.DecimalNumber,
                SENSITIVITY_CHARACTER_LIMIT,
                NormalizeSensitivity(sensitivityLabel != null ? sensitivityLabel.text : string.Empty));

            TMP_InputField volumeInput = EnsureNumericInput(
                volumeLabel,
                VOLUME_INPUT_NAME,
                TMP_InputField.ContentType.IntegerNumber,
                PERCENT_CHARACTER_LIMIT,
                NormalizePercent(volumeLabel != null ? volumeLabel.text : string.Empty, 100));

            Assign(serializedObject, "sensitivityInput", sensitivityInput);
            Assign(serializedObject, "volumeInput", volumeInput);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static TMP_InputField EnsureNumericInput(
            TextMeshProUGUI label,
            string inputName,
            TMP_InputField.ContentType contentType,
            int characterLimit,
            string normalizedText)
        {
            if (label == null)
            {
                Debug.LogWarning("[PreciseValueInputBuilder] Missing value label for " + inputName);
                return null;
            }

            Transform labelTransform = label.transform;
            Transform inputRoot = FindAncestor(labelTransform, inputName);

            if (inputRoot == null)
            {
                inputRoot = CreateInputRoot(labelTransform, inputName);
            }

            if (inputRoot == null)
            {
                return null;
            }

            Image background = inputRoot.GetComponent<Image>();
            if (background == null)
            {
                background = inputRoot.gameObject.AddComponent<Image>();
            }

            background.color = ArtPalette.Ui.Surface;
            background.raycastTarget = true;

            Transform textArea = inputRoot.Find(TEXT_AREA_NAME);
            if (textArea == null)
            {
                textArea = CreateTextArea(inputRoot);
            }

            if (labelTransform.parent != textArea)
            {
                labelTransform.SetParent(textArea, false);
            }

            SetStretch(label.rectTransform, 0f, 0f, 0f, 0f);
            label.alignment = TextAlignmentOptions.Center;
            label.color = ArtPalette.Ui.Cream;
            label.raycastTarget = false;
            label.text = normalizedText;

            TMP_InputField input = inputRoot.GetComponent<TMP_InputField>();
            if (input == null)
            {
                input = inputRoot.gameObject.AddComponent<TMP_InputField>();
            }

            input.textComponent = label;
            input.textViewport = textArea as RectTransform;
            input.targetGraphic = background;
            input.contentType = contentType;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = characterLimit;
            input.caretColor = ArtPalette.Ui.Primary;
            input.selectionColor = ArtPalette.WithAlpha(ArtPalette.Ui.Secondary, 0.35f);
            input.onFocusSelectAll = true;
            input.restoreOriginalTextOnEscape = true;
            input.resetOnDeActivation = true;
            input.SetTextWithoutNotify(normalizedText);

            EditorUtility.SetDirty(input);
            EditorUtility.SetDirty(label);
            EditorUtility.SetDirty(background);
            return input;
        }

        private static Transform CreateInputRoot(Transform labelTransform, string inputName)
        {
            Transform content = labelTransform.parent;
            if (content == null)
            {
                return null;
            }

            float height = ((RectTransform)labelTransform).sizeDelta.y;
            if (height <= 0f)
            {
                height = DEFAULT_INPUT_HEIGHT;
            }

            int siblingIndex = labelTransform.GetSiblingIndex();

            GameObject inputObject = new GameObject(inputName, typeof(RectTransform), typeof(Image));
            inputObject.transform.SetParent(content, false);
            inputObject.transform.SetSiblingIndex(siblingIndex);

            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(0f, height);

            Image background = inputObject.GetComponent<Image>();
            background.color = ArtPalette.Ui.Surface;
            background.raycastTarget = true;

            CreateTextArea(inputObject.transform);
            return inputObject.transform;
        }

        private static Transform CreateTextArea(Transform inputRoot)
        {
            GameObject textAreaObject = new GameObject(TEXT_AREA_NAME, typeof(RectTransform), typeof(RectMask2D));
            textAreaObject.transform.SetParent(inputRoot, false);

            RectTransform textAreaRect = textAreaObject.GetComponent<RectTransform>();
            SetStretch(
                textAreaRect,
                TEXT_AREA_HORIZONTAL_PADDING,
                TEXT_AREA_VERTICAL_PADDING,
                -TEXT_AREA_HORIZONTAL_PADDING,
                -TEXT_AREA_VERTICAL_PADDING);

            return textAreaObject.transform;
        }

        private static TextMeshProUGUI GetText(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as TextMeshProUGUI : null;
        }

        private static void Assign(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static Transform FindAncestor(Transform transform, string name)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == name)
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private static void SetStretch(RectTransform rectTransform, float left, float bottom, float right, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(right, top);
        }

        private static string NormalizeSensitivity(string text)
        {
            if (float.TryParse(text, out float value))
            {
                value = Mathf.Clamp(value, GameSettings.SENSITIVITY_MIN, GameSettings.SENSITIVITY_MAX);
                return value.ToString("0.00");
            }

            return GameSettings.SENSITIVITY_DEFAULT.ToString("0.00");
        }

        private static string NormalizePercent(string text, int fallback)
        {
            string cleaned = text.Replace("%", string.Empty).Replace("Fog:", string.Empty).Trim();
            if (int.TryParse(cleaned, out int percent))
            {
                return Mathf.Clamp(percent, 0, 100).ToString();
            }

            return fallback.ToString();
        }
    }
}
