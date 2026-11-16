using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class InteractionSetupBuilder
    {
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string SOUP_CAN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/SoupCanGun.prefab";
        private const string CEREAL_BOX_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/CerealBoxGun.prefab";
        private const string DEFAULT_FONT_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const float CROSSHAIR_FONT_SIZE = 40f;
        private const float PROMPT_FONT_SIZE = 28f;
        private const float HELD_ITEM_FONT_SIZE = 24f;
        private const float AMMO_FONT_SIZE = 30f;
        private const float HEALTH_FONT_SIZE = 26f;
        private const float HIT_MARKER_FONT_SIZE = 36f;
        private const float STATUS_FONT_SIZE = 48f;
        private const float KILL_FEED_FONT_SIZE = 22f;
        private const float MATCH_TIME_FONT_SIZE = 30f;
        private const float GUN_GAME_PROGRESS_FONT_SIZE = 26f;
        private const float GUN_GAME_NOTIFICATION_FONT_SIZE = 30f;
        private const float RESULTS_TITLE_FONT_SIZE = 48f;
        private const float RESULTS_REASON_FONT_SIZE = 22f;
        private const float RESULTS_WINNER_FONT_SIZE = 36f;
        private const float RESULTS_SCORE_FONT_SIZE = 24f;
        private const float RESULTS_STATUS_FONT_SIZE = 20f;
        private const float RESULTS_BUTTON_HEIGHT = 56f;
        private const float RESULTS_BUTTON_WIDTH = 360f;
        private const float SCOREBOARD_TITLE_FONT_SIZE = 30f;
        private const float SCOREBOARD_HEADER_FONT_SIZE = 18f;
        private const float SCOREBOARD_ROW_FONT_SIZE = 20f;
        private const int SCOREBOARD_ROW_COUNT = 8;
        private const float SCOREBOARD_WIDTH = 780f;
        private const float SCOREBOARD_HEIGHT = 420f;
        private const float SCOREBOARD_TOP_OFFSET = 36f;
        private const float SCOREBOARD_TITLE_Y = -18f;
        private const float SCOREBOARD_HEADER_Y = -58f;
        private const float SCOREBOARD_FIRST_ROW_Y = -94f;
        private const float SCOREBOARD_ROW_HEIGHT = 36f;
        private const string SCOREBOARD_PANEL_NAME = "ScoreboardPanel";
        private const string HIT_MARKER_ROOT_NAME = "HitMarkerRoot";
        private const int HIT_MARKER_TICK_COUNT = 4;
        private const float HIT_MARKER_ROOT_SIZE = 72f;
        private const float HIT_MARKER_TICK_WIDTH = 6f;
        private const float HIT_MARKER_TICK_HEIGHT = 28f;
        private const float HIT_MARKER_TICK_OFFSET = 16f;
        private const float HIT_MARKER_TICK_ANGLE = 45f;

        private static Color crosshairColor => ArtPalette.Ui.Cream;
        private static Color promptColor => ArtPalette.Ui.Cream;
        private static Color heldItemColor => ArtPalette.Ui.Primary;
        private static Color ammoColor => ArtPalette.Ui.Cream;
        private static Color healthColor => ArtPalette.Ui.Danger;
        private static Color hitMarkerColor => ArtPalette.Ui.Secondary;
        private static Color statusColor => ArtPalette.Ui.Accent;
        private static Color killFeedColor => ArtPalette.Ui.Cream;
        private static Color scoreboardBackgroundColor => ArtPalette.Ui.InkPanel;
        private static Color scoreboardTitleColor => ArtPalette.Ui.Primary;
        private static Color scoreboardHeaderColor => ArtPalette.Ui.CreamMuted;
        private static Color scoreboardNameColor => ArtPalette.Ui.Cream;
        private static Color scoreboardKillColor => ArtPalette.Ui.Primary;
        private static Color scoreboardDeathColor => ArtPalette.Ui.Danger;
        private static Color gunGameProgressColor => ArtPalette.Ui.Primary;
        private static Color gunGameNotificationColor => ArtPalette.Ui.Accent;
        private static Color resultsBackdropColor => ArtPalette.Ui.InkPanel;
        private static Color resultsPanelColor => ArtPalette.Ui.SurfacePanel;
        private static Color resultsTitleColor => ArtPalette.Ui.Primary;
        private static Color resultsMutedColor => ArtPalette.Ui.CreamMuted;
        private static Color resultsRematchColor => ArtPalette.Ui.Primary;
        private static Color resultsLobbyColor => ArtPalette.Ui.Secondary;
        private static Color resultsLeaveColor => ArtPalette.Ui.Danger;

        private static TMP_FontAsset defaultFont;

        public static void Build()
        {
            LoadDefaultFont();

            Scene scene = EditorSceneManager.OpenScene(GROCERY_STORE_SCENE_PATH, OpenSceneMode.Single);
            ClearExisting(scene);
            BuildHud();
            PlaceItems();

            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);

            Debug.Log("[InteractionSetupBuilder] Built HUD and placed pickup items in " + GROCERY_STORE_SCENE_PATH);
        }

        private static void ClearExisting(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "GameHudCanvas" || root.name == "MatchResultsCanvas" || root.GetComponent<GunItem>() != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void LoadDefaultFont()
        {
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DEFAULT_FONT_ASSET_PATH);
            if (defaultFont == null)
            {
                defaultFont = TMP_Settings.defaultFontAsset;
            }
        }

        private static void BuildHud()
        {
            GameObject canvasGameObject = new GameObject("GameHudCanvas", typeof(RectTransform));
            Canvas canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGameObject.AddComponent<GraphicRaycaster>();

            TextMeshProUGUI crosshair = CreateCenteredLabel("Crosshair", canvasGameObject.transform, "+", CROSSHAIR_FONT_SIZE, crosshairColor);

            TextMeshProUGUI prompt = CreateAnchoredLabel("PromptText", canvasGameObject.transform, string.Empty, PROMPT_FONT_SIZE, promptColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(0.5f, 0.5f), 1600f, TextAlignmentOptions.Center);
            prompt.gameObject.SetActive(false);

            TextMeshProUGUI heldItem = CreateAnchoredLabel("HeldItemText", canvasGameObject.transform, string.Empty, HELD_ITEM_FONT_SIZE, heldItemColor,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(0f, 0f), 800f, TextAlignmentOptions.Left);
            heldItem.gameObject.SetActive(false);

            TextMeshProUGUI ammo = CreateAnchoredLabel("AmmoText", canvasGameObject.transform, string.Empty, AMMO_FONT_SIZE, ammoColor,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(1f, 0f), 400f, TextAlignmentOptions.Right);
            ammo.gameObject.SetActive(false);

            GameHudController controller = canvasGameObject.AddComponent<GameHudController>();
            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("promptText").objectReferenceValue = prompt;
            serializedObject.FindProperty("heldItemText").objectReferenceValue = heldItem;
            serializedObject.FindProperty("ammoText").objectReferenceValue = ammo;
            serializedObject.ApplyModifiedProperties();

            AddHealthHudElements(controller);
            AddFfaHudElements(controller);
        }

        public static void AddHealthHudElements()
        {
            LoadDefaultFont();

            GameHudController controller = Object.FindAnyObjectByType<GameHudController>();
            if (controller == null)
            {
                Debug.LogError("[InteractionSetupBuilder] GameHudController not found.");
                return;
            }

            AddHealthHudElements(controller);
            EditorUtility.SetDirty(controller);
        }

        private static void AddHealthHudElements(GameHudController controller)
        {
            Transform parent = controller.transform;

            TextMeshProUGUI health = GetOrCreateAnchoredLabel(parent, "HealthText", "100 / 100", HEALTH_FONT_SIZE, healthColor,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 90f), new Vector2(0f, 0f), 500f, TextAlignmentOptions.Left);
            health.gameObject.SetActive(false);

            TextMeshProUGUI hitMarker = GetOrCreateCenteredLabel(parent, "HitMarkerText", "X", HIT_MARKER_FONT_SIZE, hitMarkerColor);
            hitMarker.gameObject.SetActive(false);
            EnsureHitMarkerWidget(controller);

            TextMeshProUGUI status = GetOrCreateAnchoredLabel(parent, "StatusText", string.Empty, STATUS_FONT_SIZE, statusColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(0.5f, 0.5f), 1200f, TextAlignmentOptions.Center);
            status.gameObject.SetActive(false);

            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("healthText").objectReferenceValue = health;
            serializedObject.FindProperty("hitMarkerText").objectReferenceValue = hitMarker;
            serializedObject.FindProperty("statusText").objectReferenceValue = status;
            serializedObject.ApplyModifiedProperties();
        }

        public static void EnsureHitMarkerWidget(GameHudController controller)
        {
            if (controller == null)
            {
                return;
            }

            Transform parent = controller.transform;
            Transform existing = parent.Find(HIT_MARKER_ROOT_NAME);
            RectTransform root = existing as RectTransform;
            CanvasGroup canvasGroup = existing != null ? existing.GetComponent<CanvasGroup>() : null;
            Image[] ticks = existing != null ? existing.GetComponentsInChildren<Image>(true) : null;

            bool canReuse = root != null &&
                            canvasGroup != null &&
                            ticks != null &&
                            ticks.Length == HIT_MARKER_TICK_COUNT;

            if (!canReuse)
            {
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                CreateHitMarkerRoot(parent, out root, out canvasGroup, out ticks);
            }
            else
            {
                ConfigureHitMarkerTicks(ticks);
            }

            root.gameObject.SetActive(false);

            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("hitMarkerRoot").objectReferenceValue = root;
            serializedObject.FindProperty("hitMarkerCanvasGroup").objectReferenceValue = canvasGroup;
            AssignImageArray(serializedObject.FindProperty("hitMarkerTicks"), ticks);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
        }

        private static void CreateHitMarkerRoot(
            Transform parent,
            out RectTransform root,
            out CanvasGroup canvasGroup,
            out Image[] ticks)
        {
            GameObject rootObject = new GameObject(HIT_MARKER_ROOT_NAME, typeof(RectTransform), typeof(CanvasGroup));
            rootObject.transform.SetParent(parent, false);

            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(HIT_MARKER_ROOT_SIZE, HIT_MARKER_ROOT_SIZE);
            root.anchoredPosition = Vector2.zero;

            canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            ticks = new Image[HIT_MARKER_TICK_COUNT];
            Vector2[] positions =
            {
                new Vector2(-HIT_MARKER_TICK_OFFSET, HIT_MARKER_TICK_OFFSET),
                new Vector2(HIT_MARKER_TICK_OFFSET, HIT_MARKER_TICK_OFFSET),
                new Vector2(-HIT_MARKER_TICK_OFFSET, -HIT_MARKER_TICK_OFFSET),
                new Vector2(HIT_MARKER_TICK_OFFSET, -HIT_MARKER_TICK_OFFSET)
            };
            float[] angles =
            {
                HIT_MARKER_TICK_ANGLE,
                -HIT_MARKER_TICK_ANGLE,
                -HIT_MARKER_TICK_ANGLE,
                HIT_MARKER_TICK_ANGLE
            };

            for (int i = 0; i < HIT_MARKER_TICK_COUNT; i++)
            {
                ticks[i] = CreateHitMarkerTick(root, i, positions[i], angles[i]);
            }
        }

        private static Image CreateHitMarkerTick(Transform parent, int index, Vector2 position, float angle)
        {
            GameObject tickObject = new GameObject("Tick" + index, typeof(RectTransform), typeof(Image));
            tickObject.transform.SetParent(parent, false);

            RectTransform rectTransform = tickObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(HIT_MARKER_TICK_WIDTH, HIT_MARKER_TICK_HEIGHT);
            rectTransform.anchoredPosition = position;
            rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);

            Image image = tickObject.GetComponent<Image>();
            image.color = ArtPalette.Ui.Secondary;
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureHitMarkerTicks(Image[] ticks)
        {
            foreach (Image tick in ticks)
            {
                if (tick == null)
                {
                    continue;
                }

                tick.color = ArtPalette.Ui.Secondary;
                tick.raycastTarget = false;
            }
        }

        private static void AssignImageArray(SerializedProperty property, Image[] images)
        {
            if (property == null)
            {
                return;
            }

            property.arraySize = images.Length;
            for (int i = 0; i < images.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            }
        }

        public static void AddFfaHudElements()
        {
            LoadDefaultFont();

            GameHudController controller = Object.FindAnyObjectByType<GameHudController>();
            if (controller == null)
            {
                Debug.LogError("[InteractionSetupBuilder] GameHudController not found.");
                return;
            }

            AddFfaHudElements(controller);
            EditorUtility.SetDirty(controller);
        }

        private static void AddFfaHudElements(GameHudController controller)
        {
            Transform parent = controller.transform;

            TextMeshProUGUI killFeed = GetOrCreateAnchoredLabel(parent, "KillFeedText", string.Empty, KILL_FEED_FONT_SIZE, killFeedColor,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(1f, 1f), 700f, TextAlignmentOptions.Right);
            killFeed.richText = true;
            killFeed.gameObject.SetActive(false);

            TextMeshProUGUI modeText = GetOrCreateAnchoredLabel(parent, "ModeText", "FREE FOR ALL", 20f, ArtPalette.Ui.Cream,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(0.5f, 1f), 600f, TextAlignmentOptions.Center);
            modeText.gameObject.SetActive(false);

            TextMeshProUGUI teamText = GetOrCreateAnchoredLabel(parent, "TeamText", string.Empty, 22f, ArtPalette.Ui.Cream,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(0.5f, 1f), 600f, TextAlignmentOptions.Center);
            teamText.gameObject.SetActive(false);

            TextMeshProUGUI countdownText = GetOrCreateAnchoredLabel(parent, "CountdownText", string.Empty, 42f, ArtPalette.Ui.Accent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -220f), new Vector2(0.5f, 0.5f), 1200f, TextAlignmentOptions.Center);
            countdownText.gameObject.SetActive(false);

            TextMeshProUGUI matchTimeText = GetOrCreateAnchoredLabel(parent, "MatchTimeText", "5:00", MATCH_TIME_FONT_SIZE, ArtPalette.Ui.Accent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(0.5f, 1f), 600f, TextAlignmentOptions.Center);
            matchTimeText.gameObject.SetActive(false);

            TextMeshProUGUI gunGameProgress = GetOrCreateAnchoredLabel(parent, "GunGameProgressText", string.Empty, GUN_GAME_PROGRESS_FONT_SIZE, gunGameProgressColor,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(0.5f, 1f), 900f, TextAlignmentOptions.Center);
            gunGameProgress.gameObject.SetActive(false);

            TextMeshProUGUI gunGameNotification = GetOrCreateAnchoredLabel(parent, "GunGameNotificationText", string.Empty, GUN_GAME_NOTIFICATION_FONT_SIZE, gunGameNotificationColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(0.5f, 0.5f), 1100f, TextAlignmentOptions.Center);
            gunGameNotification.gameObject.SetActive(false);

            GameObject panel = CreateScoreboardPanel(parent);
            TextMeshProUGUI[] nameTexts = new TextMeshProUGUI[SCOREBOARD_ROW_COUNT];
            TextMeshProUGUI[] killTexts = new TextMeshProUGUI[SCOREBOARD_ROW_COUNT];
            TextMeshProUGUI[] deathTexts = new TextMeshProUGUI[SCOREBOARD_ROW_COUNT];

            for (int i = 0; i < SCOREBOARD_ROW_COUNT; i++)
            {
                float rowY = SCOREBOARD_FIRST_ROW_Y - i * SCOREBOARD_ROW_HEIGHT;
                nameTexts[i] = CreateAnchoredLabel($"ScoreboardName{i + 1}", panel.transform, string.Empty, SCOREBOARD_ROW_FONT_SIZE, scoreboardNameColor,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, rowY), new Vector2(0f, 1f), 520f, TextAlignmentOptions.Left);
                nameTexts[i].richText = true;
                nameTexts[i].overflowMode = TextOverflowModes.Ellipsis;
                killTexts[i] = CreateAnchoredLabel($"ScoreboardKills{i + 1}", panel.transform, string.Empty, SCOREBOARD_ROW_FONT_SIZE, scoreboardKillColor,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, rowY), new Vector2(1f, 1f), 80f, TextAlignmentOptions.Center);
                deathTexts[i] = CreateAnchoredLabel($"ScoreboardDeaths{i + 1}", panel.transform, string.Empty, SCOREBOARD_ROW_FONT_SIZE, scoreboardDeathColor,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, rowY), new Vector2(1f, 1f), 80f, TextAlignmentOptions.Center);
            }

            TextMeshProUGUI teamScoreText = GetOrCreateAnchoredLabel(panel.transform, "ScoreboardTeamScore", string.Empty, 24f, scoreboardTitleColor,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, SCOREBOARD_TITLE_Y), new Vector2(1f, 1f), 360f, TextAlignmentOptions.Right);
            teamScoreText.richText = true;
            teamScoreText.gameObject.SetActive(false);

            panel.SetActive(false);

            ScoreboardController scoreboard = controller.GetComponent<ScoreboardController>();
            if (scoreboard == null)
            {
                scoreboard = controller.gameObject.AddComponent<ScoreboardController>();
            }

            SerializedObject scoreboardObject = new SerializedObject(scoreboard);
            scoreboardObject.FindProperty("scoreboardPanel").objectReferenceValue = panel;
            AssignTextArray(scoreboardObject.FindProperty("nameTexts"), nameTexts);
            AssignTextArray(scoreboardObject.FindProperty("killTexts"), killTexts);
            AssignTextArray(scoreboardObject.FindProperty("deathTexts"), deathTexts);
            scoreboardObject.FindProperty("teamScoreText").objectReferenceValue = teamScoreText;
            scoreboardObject.ApplyModifiedProperties();

            SerializedObject hudObject = new SerializedObject(controller);
            hudObject.FindProperty("killFeedText").objectReferenceValue = killFeed;
            hudObject.FindProperty("modeText").objectReferenceValue = modeText;
            hudObject.FindProperty("teamText").objectReferenceValue = teamText;
            hudObject.FindProperty("countdownText").objectReferenceValue = countdownText;
            hudObject.FindProperty("matchTimeText").objectReferenceValue = matchTimeText;
            hudObject.FindProperty("gunGameProgressText").objectReferenceValue = gunGameProgress;
            hudObject.FindProperty("gunGameNotificationText").objectReferenceValue = gunGameNotification;
            hudObject.ApplyModifiedProperties();

            AddMatchResultsElements();

            EditorUtility.SetDirty(scoreboard);
        }

        private static void AddMatchResultsElements()
        {
            MatchResultsController existingController = Object.FindAnyObjectByType<MatchResultsController>();
            if (existingController != null)
            {
                Object.DestroyImmediate(existingController.gameObject);
            }

            GameObject canvasGameObject = new GameObject("MatchResultsCanvas", typeof(RectTransform));
            Canvas canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasGameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGameObject.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("ResultsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGameObject.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = resultsBackdropColor;

            GameObject content = new GameObject("ResultsContent", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(panel.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(900f, 640f);
            contentRect.anchoredPosition = Vector2.zero;
            content.GetComponent<Image>().color = resultsPanelColor;

            TextMeshProUGUI title = CreateAnchoredLabel("ResultsTitle", content.transform, "MATCH OVER", RESULTS_TITLE_FONT_SIZE, resultsTitleColor,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(0.5f, 1f), 820f, TextAlignmentOptions.Center);
            TextMeshProUGUI reason = CreateAnchoredLabel("ResultsReason", content.transform, string.Empty, RESULTS_REASON_FONT_SIZE, resultsMutedColor,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(0.5f, 1f), 820f, TextAlignmentOptions.Center);
            TextMeshProUGUI winner = CreateAnchoredLabel("ResultsWinner", content.transform, string.Empty, RESULTS_WINNER_FONT_SIZE, ArtPalette.Ui.Cream,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(0.5f, 1f), 820f, TextAlignmentOptions.Center);
            winner.richText = true;

            TextMeshProUGUI score = CreateAnchoredLabel("ResultsScore", content.transform, string.Empty, RESULTS_SCORE_FONT_SIZE, ArtPalette.Ui.Cream,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(0.5f, 1f), 820f, TextAlignmentOptions.Top);
            score.richText = true;
            score.rectTransform.sizeDelta = new Vector2(820f, 130f);

            Button rematchButton = CreateResultsButton("RematchButton", content.transform, "REMATCH", resultsRematchColor, 170f);
            Button returnToLobbyButton = CreateResultsButton("ReturnToLobbyButton", content.transform, "RETURN TO LOBBY", resultsLobbyColor, 90f);
            Button leaveButton = CreateResultsButton("LeaveButton", content.transform, "LEAVE TO MENU", resultsLeaveColor, 10f);

            TextMeshProUGUI waitingText = CreateAnchoredLabel("ResultsWaitingText", content.transform, "Waiting for the host...", 24f, resultsMutedColor,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(0.5f, 0f), 820f, TextAlignmentOptions.Center);

            TextMeshProUGUI statusText = CreateAnchoredLabel("ResultsStatusText", content.transform, string.Empty, RESULTS_STATUS_FONT_SIZE, resultsMutedColor,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 245f), new Vector2(0.5f, 0f), 820f, TextAlignmentOptions.Center);

            MatchResultsController controller = canvasGameObject.AddComponent<MatchResultsController>();
            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("resultsPanel").objectReferenceValue = panel;
            serializedObject.FindProperty("resultTitleText").objectReferenceValue = title;
            serializedObject.FindProperty("resultReasonText").objectReferenceValue = reason;
            serializedObject.FindProperty("winnerText").objectReferenceValue = winner;
            serializedObject.FindProperty("scoreText").objectReferenceValue = score;
            serializedObject.FindProperty("rematchButton").objectReferenceValue = rematchButton;
            serializedObject.FindProperty("returnToLobbyButton").objectReferenceValue = returnToLobbyButton;
            serializedObject.FindProperty("leaveButton").objectReferenceValue = leaveButton;
            serializedObject.FindProperty("waitingText").objectReferenceValue = waitingText;
            serializedObject.FindProperty("statusText").objectReferenceValue = statusText;
            serializedObject.ApplyModifiedProperties();

            panel.SetActive(false);
            EditorUtility.SetDirty(controller);
        }

        private static Button CreateResultsButton(string name, Transform parent, string label, Color normalColor, float bottomOffset)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.sizeDelta = new Vector2(RESULTS_BUTTON_WIDTH, RESULTS_BUTTON_HEIGHT);
            rectTransform.anchoredPosition = new Vector2(0f, bottomOffset);

            Image image = gameObject.AddComponent<Image>();
            image.color = normalColor;

            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            Color textColor = normalColor == ArtPalette.Ui.SurfaceRaised ? ArtPalette.Ui.Cream : ArtPalette.Ui.Ink;
            TextMeshProUGUI buttonLabel = CreateLabel("Text", gameObject.transform, label, 26f, textColor, TextAlignmentOptions.Center);
            SetAnchors(buttonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void SetAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static GameObject CreateScoreboardPanel(Transform parent)
        {
            Transform existing = parent.Find(SCOREBOARD_PANEL_NAME);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject(SCOREBOARD_PANEL_NAME, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = new Vector2(SCOREBOARD_WIDTH, SCOREBOARD_HEIGHT);
            rectTransform.anchoredPosition = new Vector2(0f, -SCOREBOARD_TOP_OFFSET);

            Image background = panel.GetComponent<Image>();
            background.color = scoreboardBackgroundColor;
            background.raycastTarget = false;

            CreateAnchoredLabel("ScoreboardTitle", panel.transform, "SCOREBOARD", SCOREBOARD_TITLE_FONT_SIZE, scoreboardTitleColor,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, SCOREBOARD_TITLE_Y), new Vector2(0f, 1f), 400f, TextAlignmentOptions.Left);

            CreateAnchoredLabel("ScoreboardNameHeader", panel.transform, "PLAYER", SCOREBOARD_HEADER_FONT_SIZE, scoreboardHeaderColor,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, SCOREBOARD_HEADER_Y), new Vector2(0f, 1f), 520f, TextAlignmentOptions.Left);
            CreateAnchoredLabel("ScoreboardKillHeader", panel.transform, "KILLS", SCOREBOARD_HEADER_FONT_SIZE, scoreboardHeaderColor,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, SCOREBOARD_HEADER_Y), new Vector2(1f, 1f), 80f, TextAlignmentOptions.Center);
            CreateAnchoredLabel("ScoreboardDeathHeader", panel.transform, "DEATHS", SCOREBOARD_HEADER_FONT_SIZE, scoreboardHeaderColor,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, SCOREBOARD_HEADER_Y), new Vector2(1f, 1f), 80f, TextAlignmentOptions.Center);

            return panel;
        }

        private static void AssignTextArray(SerializedProperty property, TextMeshProUGUI[] texts)
        {
            property.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = texts[i];
            }
        }

        private static TextMeshProUGUI GetOrCreateAnchoredLabel(Transform parent, string name, string content, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 pivot, float width, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingLabel))
            {
                return existingLabel;
            }

            return CreateAnchoredLabel(name, parent, content, fontSize, color, anchorMin, anchorMax, anchoredPosition, pivot, width, alignment);
        }

        private static TextMeshProUGUI GetOrCreateCenteredLabel(Transform parent, string name, string content, float fontSize, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingLabel))
            {
                return existingLabel;
            }

            return CreateCenteredLabel(name, parent, content, fontSize, color);
        }

        private static void PlaceItems()
        {
            PlaceItem(SOUP_CAN_PREFAB_PATH, new Vector3(2f, 0.2f, 2f), new Vector3(1f, 1f, 1f), Quaternion.identity);
            PlaceItem(SOUP_CAN_PREFAB_PATH, new Vector3(-2f, 0.2f, 3f), new Vector3(1.8f, 1.8f, 1.8f), Quaternion.Euler(0f, 90f, 0f));
            PlaceItem(CEREAL_BOX_PREFAB_PATH, new Vector3(2f, 0.4f, -2f), new Vector3(1f, 1f, 1f), Quaternion.identity);
            PlaceItem(CEREAL_BOX_PREFAB_PATH, new Vector3(-3f, 0.4f, -2f), new Vector3(0.6f, 0.6f, 0.6f), Quaternion.Euler(0f, 45f, 0f));
        }

        private static void PlaceItem(string prefabPath, Vector3 position, Vector3 localScale, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[InteractionSetupBuilder] Prefab not found: " + prefabPath);
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = localScale;
        }

        private static TextMeshProUGUI CreateCenteredLabel(string name, Transform parent, string content, float fontSize, Color color)
        {
            TextMeshProUGUI label = CreateLabel(name, parent, content, fontSize, color, TextAlignmentOptions.Center);
            RectTransform rectTransform = label.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(fontSize * 1.5f, fontSize * 1.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            return label;
        }

        private static TextMeshProUGUI CreateAnchoredLabel(string name, Transform parent, string content, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 pivot, float width, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = CreateLabel(name, parent, content, fontSize, color, alignment);
            RectTransform rectTransform = label.rectTransform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = new Vector2(width, fontSize * 1.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, string content, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
            label.text = content;
            label.font = defaultFont;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            return label;
        }
    }
}
