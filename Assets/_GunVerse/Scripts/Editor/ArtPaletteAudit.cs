using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class ArtPaletteAudit
    {
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string LOBBY_SCENE_PATH = "Assets/_GunVerse/Scenes/Lobby.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MATERIALS_FOLDER = "Assets/_GunVerse/Materials";
        private const float COLOR_TOLERANCE = 0.04f;

        [MenuItem("Tools/Gun Verse/Art/Audit Palettes")]
        public static void Audit()
        {
            Debug.Log(BuildReport());
        }

        public static string BuildReport()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("[ArtPaletteAudit] Palette audit");

            AuditScene(MAIN_MENU_SCENE_PATH, false, report);
            AuditScene(LOBBY_SCENE_PATH, false, report);
            AuditScene(BOPBUY_SCENE_PATH, true, report);
            AuditScene(GROCERY_STORE_SCENE_PATH, true, report);

            AuditMaterialSet("BopBuy", GetBopBuyMaterialExpectations(), report);
            AuditMaterialSet("GroceryStore", GetGroceryStoreMaterialExpectations(), report);

            return report.ToString();
        }

        public static string BuildValidationReport()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("[ArtPaletteAudit] Validation");

            string previousScenePath = SceneManager.GetActiveScene().path;
            bool hasPreviousScene = !string.IsNullOrEmpty(previousScenePath);

            ValidateGameplayScene(BOPBUY_SCENE_PATH, report);
            ValidateGameplayScene(GROCERY_STORE_SCENE_PATH, report);
            ValidateMenuScene(MAIN_MENU_SCENE_PATH, true, report);
            ValidateMenuScene(LOBBY_SCENE_PATH, false, report);

            if (hasPreviousScene)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            return report.ToString();
        }

        private static void ValidateGameplayScene(string scenePath, StringBuilder report)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            report.AppendLine();
            report.AppendLine("Scene: " + scene.name);

            int gunCount = 0;
            foreach (GunItem item in Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (item.gameObject.scene == scene)
                {
                    gunCount++;
                }
            }

            int spawnCount = 0;
            foreach (PlayerSpawnPoint spawn in Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include))
            {
                if (spawn.gameObject.scene == scene)
                {
                    spawnCount++;
                }
            }

            GameHudController hud = Object.FindObjectsByType<GameHudController>(FindObjectsInactive.Include)
                .FirstOrDefault(controller => controller.gameObject.scene == scene);

            bool hasHitMarkerRoot = false;
            bool hasHitMarkerGroup = false;
            int hitMarkerTickCount = 0;

            if (hud != null)
            {
                SerializedObject serializedObject = new SerializedObject(hud);
                hasHitMarkerRoot = serializedObject.FindProperty("hitMarkerRoot").objectReferenceValue != null;
                hasHitMarkerGroup = serializedObject.FindProperty("hitMarkerCanvasGroup").objectReferenceValue != null;
                hitMarkerTickCount = serializedObject.FindProperty("hitMarkerTicks").arraySize;
            }

            bool hasResults = Object.FindObjectsByType<MatchResultsController>(FindObjectsInactive.Include)
                .Any(controller => controller.gameObject.scene == scene);
            bool hasPause = Object.FindObjectsByType<PauseMenuController>(FindObjectsInactive.Include)
                .Any(controller => controller.gameObject.scene == scene);
            bool hasScoreboard = Object.FindObjectsByType<ScoreboardController>(FindObjectsInactive.Include)
                .Any(controller => controller.gameObject.scene == scene);
            bool hasFog = Object.FindObjectsByType<StoreFogController>(FindObjectsInactive.Include)
                .Any(controller => controller.gameObject.scene == scene);
            GameObject ceiling = FindGameObject(scene, "StoreCeiling");

            report.AppendLine("  GunItems: " + gunCount);
            report.AppendLine("  Spawns: " + spawnCount);
            report.AppendLine("  Bake offenders: " + GunPropBakeUtility.AuditScene(scene));
            report.AppendLine("  StoreCeiling active: " + (ceiling != null && ceiling.activeInHierarchy));
            report.AppendLine("  StoreFogController: " + hasFog);
            report.AppendLine("  HUD hitmarker root/group/ticks: " +
                              hasHitMarkerRoot + "/" + hasHitMarkerGroup + "/" + hitMarkerTickCount);
            report.AppendLine("  Results/Pause/Scoreboard: " + hasResults + "/" + hasPause + "/" + hasScoreboard);
        }

        private static void ValidateMenuScene(string scenePath, bool requiresNetwork, StringBuilder report)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            report.AppendLine();
            report.AppendLine("Scene: " + scene.name);

            GameObject canvas = FindGameObject(scene, "MainMenuCanvas");
            if (canvas == null)
            {
                canvas = FindGameObject(scene, "LobbyCanvas");
            }

            bool hasBackdrop = canvas != null && canvas.transform.Find("UiBackdrop") != null;
            bool hasNetworkManager = Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include)
                .Any(manager => manager.gameObject.scene == scene);
            bool hasSessionManager = Object.FindObjectsByType<MultiplayerSessionManager>(FindObjectsInactive.Include)
                .Any(manager => manager.gameObject.scene == scene);

            bool hasFogControls = false;

            if (requiresNetwork)
            {
                MainMenuController controller = Object.FindObjectsByType<MainMenuController>(FindObjectsInactive.Include)
                    .FirstOrDefault(menu => menu.gameObject.scene == scene);
                if (controller != null)
                {
                    SerializedObject serializedObject = new SerializedObject(controller);
                    hasFogControls = serializedObject.FindProperty("lobbyFogButton").objectReferenceValue != null &&
                                     serializedObject.FindProperty("lobbyFogSlider").objectReferenceValue != null &&
                                     serializedObject.FindProperty("lobbyFogValueText").objectReferenceValue != null;
                }
            }
            else
            {
                LobbyController controller = Object.FindObjectsByType<LobbyController>(FindObjectsInactive.Include)
                    .FirstOrDefault(lobby => lobby.gameObject.scene == scene);
                if (controller != null)
                {
                    SerializedObject serializedObject = new SerializedObject(controller);
                    hasFogControls = serializedObject.FindProperty("fogButton").objectReferenceValue != null &&
                                     serializedObject.FindProperty("fogSlider").objectReferenceValue != null &&
                                     serializedObject.FindProperty("fogValueText").objectReferenceValue != null;
                }
            }

            report.AppendLine("  Canvas/Backdrop: " + (canvas != null) + "/" + hasBackdrop);
            report.AppendLine("  NetworkManager: " + hasNetworkManager + " (required=" + requiresNetwork + ")");
            report.AppendLine("  MultiplayerSessionManager: " + hasSessionManager + " (required=" + requiresNetwork + ")");
            report.AppendLine("  Fog controls: " + hasFogControls);
        }

        private static void AuditScene(string scenePath, bool isGameplayScene, StringBuilder report)
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            bool hasPreviousScene = !string.IsNullOrEmpty(previousScenePath);

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            report.AppendLine();
            report.AppendLine("Scene: " + scene.name);

            int rendererCount = 0;
            int defaultMaterialCount = 0;
            int sharedGroceryCount = 0;
            List<string> uiColorOffenders = new List<string>();

            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (renderer.gameObject.scene != scene)
                {
                    continue;
                }

                rendererCount++;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    string materialName = material.name.Replace(" (Instance)", string.Empty).Trim();

                    if (materialName == "Default-Material")
                    {
                        defaultMaterialCount++;
                    }

                    if (isGameplayScene && scene.name == "BopBuy" &&
                        materialName is "Checkout" or "CheckoutTop" or "ShelfMetal" or "ShelfBack")
                    {
                        sharedGroceryCount++;
                    }
                }
            }

            foreach (Graphic graphic in Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include))
            {
                if (graphic.gameObject.scene != scene ||
                    graphic.gameObject.name == "TeamText" ||
                    IsPaletteColor(graphic.color))
                {
                    continue;
                }

                uiColorOffenders.Add(graphic.gameObject.name + " = " + graphic.color);
            }

            report.AppendLine("  Renderers: " + rendererCount + "; default-material entries: " + defaultMaterialCount);

            if (isGameplayScene && scene.name == "BopBuy")
            {
                report.AppendLine("  Shared GroceryStore material references: " + sharedGroceryCount);
            }

            string skybox = RenderSettings.skybox == null
                ? "NULL"
                : RenderSettings.skybox.name + " / " + RenderSettings.skybox.shader.name;
            report.AppendLine("  Skybox: " + skybox);
            report.AppendLine("  Ambient: " + RenderSettings.ambientLight + " mode=" + RenderSettings.ambientMode);

            bool hasCeiling = FindGameObject(scene, "StoreCeiling") != null;
            bool hasFog = Object.FindObjectsByType<StoreFogController>(FindObjectsInactive.Include)
                .Any(controller => controller.gameObject.scene == scene);
            bool hasCamera = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)
                .Any(camera => camera.gameObject.scene == scene);

            report.AppendLine("  StoreCeiling: " + (hasCeiling ? "present" : "MISSING"));
            report.AppendLine("  StoreFogController: " + (hasFog ? "present" : "MISSING"));
            report.AppendLine("  Camera: " + (hasCamera ? "present" : "MISSING"));

            if (uiColorOffenders.Count == 0)
            {
                report.AppendLine("  UI graphics outside palette: 0");
            }
            else
            {
                report.AppendLine("  UI graphics outside palette: " + uiColorOffenders.Count);
                foreach (string offender in uiColorOffenders.Take(20))
                {
                    report.AppendLine("    - " + offender);
                }
            }

            if (hasPreviousScene)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static void AuditMaterialSet(string label, IReadOnlyList<MaterialExpectation> expectations, StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("Materials: " + label);
            int mismatches = 0;

            foreach (MaterialExpectation expectation in expectations)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(expectation.Path);
                if (material == null)
                {
                    report.AppendLine("  MISSING " + expectation.Path);
                    mismatches++;
                    continue;
                }

                Color color = material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : material.color;

                if (!IsClose(color, expectation.Color))
                {
                    report.AppendLine($"  MISMATCH {material.name}: {color} expected {expectation.Color}");
                    mismatches++;
                }
            }

            report.AppendLine("  Mismatches: " + mismatches);
        }

        private static IReadOnlyList<MaterialExpectation> GetBopBuyMaterialExpectations()
        {
            return new[]
            {
                new MaterialExpectation(MATERIALS_FOLDER + "/BopBuyFloor.mat", ArtPalette.BopBuy.Floor),
                new MaterialExpectation(MATERIALS_FOLDER + "/BopBuyWall.mat", ArtPalette.BopBuy.Wall),
                new MaterialExpectation(MATERIALS_FOLDER + "/BopBuyCeiling.mat", ArtPalette.BopBuy.Ceiling),
                new MaterialExpectation(MATERIALS_FOLDER + "/BopBuyBlue.mat", ArtPalette.BopBuy.Blue),
                new MaterialExpectation(MATERIALS_FOLDER + "/BopBuyYellow.mat", ArtPalette.BopBuy.Yellow),
                new MaterialExpectation(MATERIALS_FOLDER + "/BopBuyMetal.mat", ArtPalette.BopBuy.Metal),
                new MaterialExpectation(MATERIALS_FOLDER + "/BopBuyCounter.mat", ArtPalette.BopBuy.Counter),
                new MaterialExpectation(MATERIALS_FOLDER + "/BopBuyRedAccent.mat", ArtPalette.BopBuy.Accent)
            };
        }

        private static IReadOnlyList<MaterialExpectation> GetGroceryStoreMaterialExpectations()
        {
            return new[]
            {
                new MaterialExpectation(MATERIALS_FOLDER + "/Floor.mat", ArtPalette.GroceryStore.Floor),
                new MaterialExpectation(MATERIALS_FOLDER + "/StoreWall.mat", ArtPalette.GroceryStore.Wall),
                new MaterialExpectation(MATERIALS_FOLDER + "/StoreCeiling.mat", ArtPalette.GroceryStore.Ceiling),
                new MaterialExpectation(MATERIALS_FOLDER + "/ShelfMetal.mat", ArtPalette.GroceryStore.ShelfMetal),
                new MaterialExpectation(MATERIALS_FOLDER + "/ShelfBack.mat", ArtPalette.GroceryStore.ShelfBack),
                new MaterialExpectation(MATERIALS_FOLDER + "/Checkout.mat", ArtPalette.GroceryStore.Checkout),
                new MaterialExpectation(MATERIALS_FOLDER + "/CheckoutTop.mat", ArtPalette.GroceryStore.CheckoutTop),
                new MaterialExpectation(MATERIALS_FOLDER + "/ProduceFloor.mat", ArtPalette.GroceryStore.ProduceFloor)
            };
        }

        private static bool IsPaletteColor(Color color)
        {
            return IsClose(color, ArtPalette.Ui.Ink) ||
                   IsClose(color, ArtPalette.Ui.Surface) ||
                   IsClose(color, ArtPalette.Ui.Cream) ||
                   IsClose(color, ArtPalette.Ui.Primary) ||
                   IsClose(color, ArtPalette.Ui.Secondary) ||
                   IsClose(color, ArtPalette.Ui.Danger) ||
                   IsClose(color, ArtPalette.Ui.Accent) ||
                   IsClose(color, ArtPalette.Ui.SurfaceRaised) ||
                   IsClose(color, ArtPalette.Ui.CreamMuted) ||
                   IsClose(color, ArtPalette.Ui.InkPanel) ||
                   IsClose(color, ArtPalette.Ui.SurfacePanel) ||
                   IsClose(color, ArtPalette.Ui.PauseBackdrop);
        }

        private static bool IsClose(Color left, Color right)
        {
            return Mathf.Abs(left.r - right.r) <= COLOR_TOLERANCE &&
                   Mathf.Abs(left.g - right.g) <= COLOR_TOLERANCE &&
                   Mathf.Abs(left.b - right.b) <= COLOR_TOLERANCE;
        }

        private static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject found = FindGameObjectRecursive(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindGameObjectRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent.gameObject;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject found = FindGameObjectRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private sealed class MaterialExpectation
        {
            public MaterialExpectation(string path, Color color)
            {
                Path = path;
                Color = color;
            }

            public string Path { get; }
            public Color Color { get; }
        }
    }
}
