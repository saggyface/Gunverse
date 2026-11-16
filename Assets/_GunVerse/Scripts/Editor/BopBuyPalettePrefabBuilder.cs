using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class BopBuyPalettePrefabBuilder
    {
        private const string PREFAB_FOLDER = "Assets/_GunVerse/Prefabs/BopBuy";
        private const string BAKED_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps/Baked";
        private const string VARIANT_SUFFIX = "_BopBuy";

        private static readonly string[] TransformPropertyPrefixes =
        {
            "m_LocalPosition.",
            "m_LocalRotation.",
            "m_LocalScale.",
            "m_LocalEulerAnglesHint."
        };

        private static readonly string[] TransformPropertyNames =
        {
            "m_RootOrder",
            "m_ConstrainProportionsScale",
            "m_Name"
        };

        private static readonly string[] SharedFixturePaths =
        {
            "Assets/_GunVerse/Prefabs/LargeProps/CheckoutCounterGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/EntranceMatGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/ShelfRunGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/CashRegisterGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/ConveyorBeltGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/LaneMarkerGun.prefab"
        };

        public static int ApplyBopBuyVariants(Scene scene)
        {
            EnsureFolder(PREFAB_FOLDER);

            int replaced = 0;
            foreach (string sourcePath in SharedFixturePaths)
            {
                GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (sourcePrefab == null)
                {
                    continue;
                }

                GameObject variantPrefab = EnsureVariant(sourcePath);
                if (variantPrefab == null)
                {
                    continue;
                }

                replaced += ReplaceInstances(scene, sourcePath, variantPrefab);
            }

            UpdateBakedFixtures();
            GunItemPrefabRegistry.Rebuild();
            GunPropBakeUtility.AssignSceneProjectileHashes(scene);
            AssetDatabase.SaveAssets();
            return replaced;
        }

        private static GameObject EnsureVariant(string sourcePath)
        {
            string variantPath = GetVariantPath(sourcePath);
            GameObject root = PrefabUtility.LoadPrefabContents(sourcePath);
            if (root == null)
            {
                return null;
            }

            RecolorRenderers(root, sourcePath);
            root.name = Path.GetFileNameWithoutExtension(variantPath);
            GameObject variant = PrefabUtility.SaveAsPrefabAsset(root, variantPath);
            PrefabUtility.UnloadPrefabContents(root);
            return variant;
        }

        private static int ReplaceInstances(Scene scene, string sourcePath, GameObject variantPrefab)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            int replaced = 0;

            foreach (GameObject root in roots)
            {
                replaced += ReplaceInstancesRecursive(root.transform, sourcePath, variantPrefab);
            }

            return replaced;
        }

        private static int ReplaceInstancesRecursive(Transform parent, string sourcePath, GameObject variantPrefab)
        {
            int replaced = 0;

            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                GunItem gunItem = child.GetComponent<GunItem>();
                if (gunItem != null && IsSourcePrefab(child.gameObject, sourcePath))
                {
                    ReplaceInstance(child.gameObject, variantPrefab);
                    replaced++;
                    continue;
                }

                replaced += ReplaceInstancesRecursive(child, sourcePath, variantPrefab);
            }

            return replaced;
        }

        private static bool IsSourcePrefab(GameObject instance, string sourcePath)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (source == null)
            {
                return false;
            }

            return AssetDatabase.GetAssetPath(source) == sourcePath;
        }

        private static void ReplaceInstance(GameObject oldRoot, GameObject variantPrefab)
        {
            Transform parent = oldRoot.transform.parent;
            Vector3 position = oldRoot.transform.position;
            Quaternion rotation = oldRoot.transform.rotation;
            Vector3 localScale = oldRoot.transform.localScale;
            string name = oldRoot.name;
            int siblingIndex = oldRoot.transform.GetSiblingIndex();
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(oldRoot);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(variantPrefab, parent);
            if (instance == null)
            {
                return;
            }

            if (modifications != null)
            {
                PropertyModification[] filtered = modifications
                    .Where(modification => modification.propertyPath != "projectilePrefabHash" &&
                                           !IsTransformModification(modification.propertyPath))
                    .ToArray();
                PrefabUtility.SetPropertyModifications(instance, filtered);
            }

            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = localScale;
            instance.transform.SetSiblingIndex(siblingIndex);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            UnityEngine.Object.DestroyImmediate(oldRoot);
        }

        private static bool IsTransformModification(string propertyPath)
        {
            int separatorIndex = propertyPath.LastIndexOf('/');
            string propertyName = separatorIndex >= 0 ? propertyPath.Substring(separatorIndex + 1) : propertyPath;
            return TransformPropertyPrefixes.Any(prefix => propertyName.StartsWith(prefix, StringComparison.Ordinal)) ||
                   TransformPropertyNames.Contains(propertyName, StringComparer.Ordinal);
        }

        private static void UpdateBakedFixtures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { BAKED_FOLDER });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    continue;
                }

                bool changed = RecolorRenderers(root, path);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool RecolorRenderers(GameObject root, string sourcePath)
        {
            bool changed = false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool rendererChanged = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material replacement = ResolveReplacement(sourcePath, materials[i]);
                    if (replacement == null || replacement == materials[i])
                    {
                        continue;
                    }

                    materials[i] = replacement;
                    rendererChanged = true;
                }

                if (!rendererChanged)
                {
                    continue;
                }

                renderer.sharedMaterials = materials;
                changed = true;
            }

            return changed;
        }

        private static Material ResolveReplacement(string sourcePath, Material material)
        {
            if (material == null)
            {
                return null;
            }

            string materialName = material.name.Replace(" (Instance)", string.Empty).Trim();

            if (sourcePath.EndsWith("LaneMarkerGun.prefab") && materialName == "ShelfMetal")
            {
                return BopBuyMaterials.Yellow;
            }

            if (sourcePath.EndsWith("EntranceMatGun.prefab") && materialName == "Checkout")
            {
                return BopBuyMaterials.BlueFabric;
            }

            return materialName switch
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

        private static string GetVariantPath(string sourcePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            return PREFAB_FOLDER + "/" + fileName + VARIANT_SUFFIX + ".prefab";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
