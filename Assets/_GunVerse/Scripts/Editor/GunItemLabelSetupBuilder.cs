using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class GunItemLabelSetupBuilder
    {
        private const string PREFABS_ROOT = "Assets/_GunVerse/Prefabs";
        private const string LABEL_MATERIALS_FOLDER = "Assets/_GunVerse/Materials/ItemLabels";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string ALWAYS_SHOW_LABEL_PROPERTY = "alwaysShowLabel";
        private const string LABEL_VISUAL_NAME = "LabelVisual";
        private const string LABEL_PREFIX = "Label";

        private static readonly HashSet<string> AlwaysVisiblePrefabNames = new HashSet<string>
        {
            "EntranceMatGun",
            "BopBuySignGun",
            "GuysquadSignGun",
            "MobileSignGun",
            "ComputersSignGun",
            "HomeTheatreSignGun",
            "AppliancesSignGun",
            "CheckoutSignGun",
            "TvWallSignGun",
            "SmartHomeSignGun",
            "CellPhonesSignGun",
            "MobileAccessoriesSignGun",
            "PrintersInkSignGun",
            "TabletsSignGun",
            "LaptopsSignGun",
            "ComputingSignGun",
            "OrderPickupSignGun",
            "CustomerServiceSignGun",
            "HolidaySignGun",
            "GiftsSignGun",
            "SmartHomeBannerGun",
            "CellPhonesBannerGun",
            "TabletsBannerGun",
            "ComputingBannerGun",
            "HolidayBannerGun",
            "GiftsBannerGun",
            "GuysquadWallSignGun",
            "GuysquadHangingSignGun",
            "HangingMobileSignGun",
            "HangingComputersSignGun",
            "HangingAppliancesSignGun",
            "HangingHomeTheatreSignGun",
            "HangingCheckoutSignGun",
            "BopBuyBannerGun",
            "WelcomeDirectorySignGun",
            "NoBreaksSignGun",
            "BehindYouSignGun",
            "MenSignGun",
            "WomenSignGun",
            "ClearanceSignGun",
            "WarehouseSignGun"
        };

        [MenuItem("Tools/Gun Verse/Apply Hover-Only Labels")]
        public static void Apply()
        {
            ApplyLabelOpacity();
            ApplyToPrefabs();
            ApplyToScene(GROCERY_STORE_SCENE_PATH);
            ApplyToScene(BOPBUY_SCENE_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[GunItemLabelSetupBuilder] Applied 70% opacity and hover-only label visibility.");
        }

        public static void ConfigurePrefab(GameObject root, bool alwaysShowLabel)
        {
            GunItem gunItem = root.GetComponent<GunItem>();
            if (gunItem == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(gunItem);
            SerializedProperty alwaysShowProperty = serializedObject.FindProperty(ALWAYS_SHOW_LABEL_PROPERTY);
            if (alwaysShowProperty != null)
            {
                alwaysShowProperty.boolValue = alwaysShowLabel;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (GunItemLabelVisual labelVisual in CollectLabelVisuals(root.transform))
            {
                labelVisual.gameObject.SetActive(alwaysShowLabel);
            }
        }

        private static void ApplyLabelOpacity()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { LABEL_MATERIALS_FOLDER });
            foreach (string guid in guids)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                PropLabelBuilder.ApplyLabelOpacity(material);
            }
        }

        private static void ApplyToPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFABS_ROOT });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<GunItem>() == null)
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool alwaysShowLabel = AlwaysVisiblePrefabNames.Contains(root.name);
                ConfigurePrefab(root, alwaysShowLabel);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToScene(string scenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool changed = false;
            foreach (GunItem gunItem in Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                GunItemLabelVisual[] labelVisuals = gunItem.GetComponentsInChildren<GunItemLabelVisual>(true);
                foreach (GunItemLabelVisual labelVisual in labelVisuals)
                {
                    bool shouldShow = gunItem.AlwaysShowLabel;
                    if (labelVisual.gameObject.activeSelf != shouldShow)
                    {
                        labelVisual.gameObject.SetActive(shouldShow);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static List<GunItemLabelVisual> CollectLabelVisuals(Transform root)
        {
            List<GunItemLabelVisual> labelVisuals = new List<GunItemLabelVisual>();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root || child.name != LABEL_VISUAL_NAME)
                {
                    continue;
                }

                GunItemLabelVisual labelVisual = child.GetComponent<GunItemLabelVisual>();
                if (labelVisual == null)
                {
                    labelVisual = child.gameObject.AddComponent<GunItemLabelVisual>();
                }

                labelVisuals.Add(labelVisual);
                return labelVisuals;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root || !child.name.StartsWith(LABEL_PREFIX, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (child.GetComponent<Renderer>() == null)
                {
                    continue;
                }

                GunItemLabelVisual labelVisual = child.GetComponent<GunItemLabelVisual>();
                if (labelVisual == null)
                {
                    labelVisual = child.gameObject.AddComponent<GunItemLabelVisual>();
                }

                labelVisuals.Add(labelVisual);
            }

            return labelVisuals;
        }
    }
}
