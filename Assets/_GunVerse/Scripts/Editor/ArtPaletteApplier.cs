using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class ArtPaletteApplier
    {
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";

        [MenuItem("Tools/Gun Verse/Art/Apply BopBuy Palette")]
        public static void ApplyBopBuy()
        {
            ApplyScene(BOPBUY_SCENE_PATH, ApplyBopBuyScene);
        }

        [MenuItem("Tools/Gun Verse/Art/Apply GroceryStore Palette")]
        public static void ApplyGroceryStore()
        {
            ApplyScene(GROCERY_STORE_SCENE_PATH, ApplyGroceryStoreScene);
        }

        [MenuItem("Tools/Gun Verse/Art/Apply All Map Palettes")]
        public static void ApplyAllMapPalettes()
        {
            ApplyBopBuy();
            ApplyGroceryStore();
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

        private static void ApplyBopBuyScene(Scene scene)
        {
            ApplyBopBuyMaterialPalette();

            int replaced = BopBuyPalettePrefabBuilder.ApplyBopBuyVariants(scene);
            int directReplacements = ReplaceDirectGroceryMaterials(scene);
            ApplyLighting(scene, ArtPalette.BopBuy.Ambient, ArtPalette.BopBuy.KeyLight, ArtPalette.BopBuy.PointLight, 1.1f);
            ApplyCamera(scene, ArtPalette.BopBuy.CameraBackground);
            EnsureCeilingActive(scene);

            int remaining = CountSharedGroceryMaterialReferences(scene);
            Debug.Log(
                $"[ArtPaletteApplier] BopBuy palette applied: {replaced} prefab instances replaced, " +
                $"{directReplacements} direct material overrides, {remaining} remaining shared GroceryStore references.");
        }

        private static void ApplyGroceryStoreScene(Scene scene)
        {
            GroceryStoreSetupBuilder.ApplyPaletteToMaterials();
            ApplyLighting(scene, ArtPalette.GroceryStore.Ambient, ArtPalette.GroceryStore.KeyLight, ArtPalette.GroceryStore.PointLight, 0.6f);
            ApplyCamera(scene, ArtPalette.GroceryStore.CameraBackground);
            EnsureCeilingActive(scene);
            GunPropBakeUtility.AssignSceneProjectileHashes(scene);

            Debug.Log("[ArtPaletteApplier] GroceryStore palette applied.");
        }

        private static void ApplyBopBuyMaterialPalette()
        {
            _ = BopBuyMaterials.Floor;
            _ = BopBuyMaterials.Wall;
            _ = BopBuyMaterials.Ceiling;
            _ = BopBuyMaterials.Blue;
            _ = BopBuyMaterials.Yellow;
            _ = BopBuyMaterials.Counter;
            _ = BopBuyMaterials.Metal;
            _ = BopBuyMaterials.Carpet;
            _ = BopBuyMaterials.WoodFloor;
            _ = BopBuyMaterials.CharcoalCarpet;
            _ = BopBuyMaterials.CoinRubber;
            _ = BopBuyMaterials.BlackWall;
            _ = BopBuyMaterials.WhiteBlock;
            _ = BopBuyMaterials.BlackTruss;
            _ = BopBuyMaterials.Duct;
            _ = BopBuyMaterials.BlueFabric;
            _ = BopBuyMaterials.YellowTree;
            _ = BopBuyMaterials.WhiteCounter;
            _ = BopBuyMaterials.GreyLocker;
            _ = BopBuyMaterials.Chrome;
            _ = BopBuyMaterials.RedAccent;
            _ = BopBuyMaterials.WoodTable;
            AssetDatabase.SaveAssets();
        }

        private static int ReplaceDirectGroceryMaterials(Scene scene)
        {
            int replacements = 0;

            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (renderer.gameObject.scene != scene)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material replacement = ResolveBopBuyReplacement(materials[i]);
                    if (replacement == null || replacement == materials[i])
                    {
                        continue;
                    }

                    materials[i] = replacement;
                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                renderer.sharedMaterials = materials;
                replacements++;
            }

            return replacements;
        }

        private static Material ResolveBopBuyReplacement(Material material)
        {
            if (material == null)
            {
                return null;
            }

            return material.name.Replace(" (Instance)", string.Empty).Trim() switch
            {
                "Checkout" => BopBuyMaterials.Counter,
                "CheckoutTop" => BopBuyMaterials.WhiteCounter,
                "ShelfMetal" => BopBuyMaterials.Metal,
                "ShelfBack" => BopBuyMaterials.WhiteBlock,
                "Floor" => BopBuyMaterials.Floor,
                "StoreWall" => BopBuyMaterials.Wall,
                "StoreCeiling" => BopBuyMaterials.Ceiling,
                "ProduceFloor" => BopBuyMaterials.Carpet,
                _ => null
            };
        }

        private static int CountSharedGroceryMaterialReferences(Scene scene)
        {
            int references = 0;

            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (renderer.gameObject.scene != scene)
                {
                    continue;
                }

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    string materialName = material.name.Replace(" (Instance)", string.Empty).Trim();
                    if (materialName is "Checkout" or "CheckoutTop" or "ShelfMetal" or "ShelfBack")
                    {
                        references++;
                    }
                }
            }

            return references;
        }

        private static void ApplyLighting(Scene scene, Color ambient, Color keyColor, Color pointColor, float keyIntensity)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.4f;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            Light directional = null;

            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.gameObject.scene != scene)
                {
                    continue;
                }

                if (light.type == LightType.Directional)
                {
                    directional = light;
                    light.color = keyColor;
                    light.intensity = keyIntensity;
                    light.shadows = LightShadows.Soft;
                    continue;
                }

                if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    light.color = pointColor;
                }
            }

            RenderSettings.sun = directional;
        }

        private static void ApplyCamera(Scene scene, Color backgroundColor)
        {
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (camera.gameObject.scene != scene)
                {
                    continue;
                }

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = backgroundColor;
                return;
            }
        }

        private static void EnsureCeilingActive(Scene scene)
        {
            GameObject ceiling = FindGameObject(scene, "StoreCeiling");
            if (ceiling == null)
            {
                Debug.LogError("[ArtPaletteApplier] StoreCeiling not found in " + scene.name);
                return;
            }

            if (!ceiling.activeSelf)
            {
                ceiling.SetActive(true);
                Debug.Log("[ArtPaletteApplier] Re-enabled StoreCeiling in " + scene.name);
            }
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
    }
}
