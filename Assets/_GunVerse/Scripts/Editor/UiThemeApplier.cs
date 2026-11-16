using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class UiThemeApplier
    {
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string LOBBY_SCENE_PATH = "Assets/_GunVerse/Scenes/Lobby.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";

        private enum ThemeContext
        {
            Menu,
            Hud,
            Results,
            Pause
        }

        private enum ButtonRole
        {
            Primary,
            Secondary,
            Danger,
            Neutral
        }

        [MenuItem("Tools/Gun Verse/UI/Apply GunVerse UI Theme")]
        public static void Apply()
        {
            ApplyScene(MAIN_MENU_SCENE_PATH, ApplyMenuScene);
            ApplyScene(LOBBY_SCENE_PATH, ApplyMenuScene);
            ApplyScene(BOPBUY_SCENE_PATH, ApplyGameplayScene);
            ApplyScene(GROCERY_STORE_SCENE_PATH, ApplyGameplayScene);
            Debug.Log("[UiThemeApplier] Applied the GunVerse UI theme to all scenes.");
        }

        private static void ApplyScene(string scenePath, System.Action<Scene> apply)
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            bool hasPreviousScene = !string.IsNullOrEmpty(previousScenePath);

            if (hasPreviousScene)
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (hasPreviousScene)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static void ApplyMenuScene(Scene scene)
        {
            ApplyMenuBackground(scene);

            GameObject canvas = FindRoot(scene, "MainMenuCanvas");
            if (canvas == null)
            {
                canvas = FindRoot(scene, "LobbyCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[UiThemeApplier] Menu canvas not found in " + scene.name);
                return;
            }

            EnsureBackdrop(canvas.transform);
            ApplyCanvasTheme(canvas.transform, ThemeContext.Menu);
        }

        private static void ApplyGameplayScene(Scene scene)
        {
            GameObject hudCanvas = FindRoot(scene, "GameHudCanvas");
            if (hudCanvas != null)
            {
                ApplyCanvasTheme(hudCanvas.transform, ThemeContext.Hud);

                GameHudController hud = hudCanvas.GetComponent<GameHudController>();
                if (hud != null)
                {
                    InteractionSetupBuilder.EnsureHitMarkerWidget(hud);
                }
            }

            GameObject joinCodeCanvas = FindRoot(scene, "JoinCodeCanvas");
            if (joinCodeCanvas != null)
            {
                ApplyCanvasTheme(joinCodeCanvas.transform, ThemeContext.Hud);
            }

            GameObject resultsCanvas = FindRoot(scene, "MatchResultsCanvas");
            if (resultsCanvas != null)
            {
                ApplyCanvasTheme(resultsCanvas.transform, ThemeContext.Results);
            }

            GameObject pauseCanvas = FindRoot(scene, "PauseMenuCanvas");
            if (pauseCanvas != null)
            {
                ApplyPauseBackdrops(pauseCanvas.transform);
                ApplyCanvasTheme(pauseCanvas.transform, ThemeContext.Pause);
            }
        }

        private static void ApplyCanvasTheme(Transform root, ThemeContext context)
        {
            PatchPanels(root, context);
            PatchLabels(root, context);
            PatchButtons(root);
            PatchSliders(root);
            PatchInputFields(root);
        }

        private static void ApplyMenuBackground(Scene scene)
        {
            RenderSettings.skybox = null;

            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (camera.gameObject.scene != scene)
                {
                    continue;
                }

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = ArtPalette.Ui.Ink;
                return;
            }
        }

        private static void EnsureBackdrop(Transform parent)
        {
            Transform existing = parent.Find("UiBackdrop");
            GameObject backdrop = existing != null
                ? existing.gameObject
                : CreateUiObject("UiBackdrop", parent);

            backdrop.transform.SetAsFirstSibling();

            RectTransform rectTransform = backdrop.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = backdrop.GetComponent<Image>();
            if (image == null)
            {
                image = backdrop.AddComponent<Image>();
            }

            image.color = ArtPalette.Ui.Ink;
            image.raycastTarget = false;
        }

        private static void ApplyPauseBackdrops(Transform canvasTransform)
        {
            string[] panelNames = { "PausePanel", "SettingsPanel", "ConfirmPanel" };
            foreach (string panelName in panelNames)
            {
                Transform panel = canvasTransform.Find(panelName);
                if (panel == null)
                {
                    continue;
                }

                Image image = panel.GetComponent<Image>();
                if (image == null)
                {
                    image = panel.gameObject.AddComponent<Image>();
                }

                image.color = ArtPalette.Ui.PauseBackdrop;
                image.raycastTarget = false;
            }
        }

        private static void PatchPanels(Transform root, ThemeContext context)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                switch (image.gameObject.name)
                {
                    case "ResultsPanel":
                        image.color = ArtPalette.Ui.InkPanel;
                        image.raycastTarget = false;
                        break;
                    case "ResultsContent":
                        image.color = ArtPalette.Ui.SurfacePanel;
                        break;
                    case "ScoreboardPanel":
                        image.color = ArtPalette.Ui.InkPanel;
                        break;
                    case "ScreenFlash":
                        image.color = ArtPalette.WithAlpha(ArtPalette.Ui.Danger, 0f);
                        break;
                    case "UiBackdrop":
                        image.color = ArtPalette.Ui.Ink;
                        image.raycastTarget = false;
                        break;
                }
            }

            if (context == ThemeContext.Pause)
            {
                ApplyPauseBackdrops(root);
            }
        }

        private static void PatchLabels(Transform root, ThemeContext context)
        {
            foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                label.color = GetLabelColor(label, context);
            }
        }

        private static Color GetLabelColor(TextMeshProUGUI label, ThemeContext context)
        {
            string name = label.gameObject.name;

            if (name.StartsWith("ScoreboardName", System.StringComparison.Ordinal) &&
                !name.Contains("Header"))
            {
                return ArtPalette.Ui.Cream;
            }

            if (name.StartsWith("ScoreboardKills", System.StringComparison.Ordinal))
            {
                return ArtPalette.Ui.Primary;
            }

            if (name.StartsWith("ScoreboardDeaths", System.StringComparison.Ordinal))
            {
                return ArtPalette.Ui.Danger;
            }

            switch (name)
            {
                case "Crosshair":
                case "PromptText":
                case "AmmoText":
                case "KillFeedText":
                case "ModeText":
                case "TeamText":
                case "ResultsWinner":
                case "ResultsScore":
                    return ArtPalette.Ui.Cream;
                case "HeldItemText":
                case "GunGameProgressText":
                case "ScoreboardTitle":
                case "ResultsTitle":
                    return ArtPalette.Ui.Primary;
                case "HealthText":
                case "UsernameError":
                case "ScoreboardDeaths":
                    return ArtPalette.Ui.Danger;
                case "HitMarkerText":
                    return ArtPalette.Ui.Secondary;
                case "CountdownText":
                case "MatchTimeText":
                case "GunGameNotificationText":
                    return ArtPalette.Ui.Accent;
                case "StatusText":
                    return context == ThemeContext.Hud ? ArtPalette.Ui.Accent : ArtPalette.Ui.CreamMuted;
                case "ScoreboardNameHeader":
                case "ScoreboardKillHeader":
                case "ScoreboardDeathHeader":
                case "ResultsReason":
                case "ResultsWaitingText":
                case "ResultsStatusText":
                case "FogValue":
                case "SensitivityValue":
                case "VolumeValue":
                case "Placeholder":
                case "LobbyStatusText":
                    return ArtPalette.Ui.CreamMuted;
                case "Title":
                    return label.text is "GUNVERSE" or "PAUSED"
                        ? ArtPalette.Ui.Primary
                        : ArtPalette.Ui.Cream;
                default:
                    return ArtPalette.Ui.Cream;
            }
        }

        private static void PatchButtons(Transform root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                ButtonRole role = ClassifyButton(button.gameObject.name);
                Color background = GetButtonBackground(role);
                Color textColor = GetButtonTextColor(role);

                Image image = button.targetGraphic as Image;
                if (image == null)
                {
                    image = button.GetComponent<Image>();
                }

                if (image != null)
                {
                    image.color = background;
                }

                button.colors = BuildColorBlock();

                foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.color = textColor;
                }
            }
        }

        private static ButtonRole ClassifyButton(string name)
        {
            if (name.Contains("Quit") || name.Contains("Leave") || name.Contains("Confirm"))
            {
                return ButtonRole.Danger;
            }

            if (name is "PlayButton" or "ContinueButton" or "HostButton" or "JoinButton" or
                "JoinLobbyButton" or "StartButton" or "ResumeButton" or "RematchButton")
            {
                return ButtonRole.Primary;
            }

            if (name is "SettingsButton" or "BackButton" or "CancelButton" or "MapButton" or
                "FogButton" or "TestModeButton" or "ReturnToLobbyButton")
            {
                return ButtonRole.Secondary;
            }

            return ButtonRole.Neutral;
        }

        private static Color GetButtonBackground(ButtonRole role)
        {
            return role switch
            {
                ButtonRole.Primary => ArtPalette.Ui.Primary,
                ButtonRole.Secondary => ArtPalette.Ui.Secondary,
                ButtonRole.Danger => ArtPalette.Ui.Danger,
                _ => ArtPalette.Ui.SurfaceRaised
            };
        }

        private static Color GetButtonTextColor(ButtonRole role)
        {
            return role == ButtonRole.Neutral ? ArtPalette.Ui.Cream : ArtPalette.Ui.Ink;
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

        private static void PatchSliders(Transform root)
        {
            foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
            {
                Transform backgroundTransform = slider.transform.Find("Background");
                if (backgroundTransform != null && backgroundTransform.TryGetComponent(out Image background))
                {
                    background.color = ArtPalette.Ui.Ink;
                }

                if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill))
                {
                    fill.color = ArtPalette.Ui.Secondary;
                }

                if (slider.handleRect != null && slider.handleRect.TryGetComponent(out Image handle))
                {
                    handle.color = ArtPalette.Ui.Cream;
                }
            }
        }

        private static void PatchInputFields(Transform root)
        {
            foreach (TMP_InputField inputField in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (inputField.targetGraphic is Image background)
                {
                    background.color = ArtPalette.Ui.Surface;
                }

                if (inputField.textComponent != null)
                {
                    inputField.textComponent.color = ArtPalette.Ui.Cream;
                }

                if (inputField.placeholder is TextMeshProUGUI placeholder)
                {
                    placeholder.color = ArtPalette.Ui.CreamMuted;
                }

                inputField.caretColor = ArtPalette.Ui.Primary;
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }
    }
}
