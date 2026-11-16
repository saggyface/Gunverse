using System;
using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class BopBuyApplianceVariantBuilder
    {
        private const string BASE_COLOR_PROPERTY = "_BaseColor";
        private const string COLOR_PROPERTY = "_Color";
        private const string INSTANCE_SUFFIX = " (Instance)";
        private const float LUMINANCE_RED = 0.2126f;
        private const float LUMINANCE_GREEN = 0.7152f;
        private const float LUMINANCE_BLUE = 0.0722f;
        private const float DARK_MATERIAL_LUMINANCE = 0.25f;

        [MenuItem("Tools/Gun Verse/Build BopBuy Appliance Variants")]
        public static void BuildVariants()
        {
            EnsureFolder(BopBuyApplianceVarietyCatalog.VARIANT_MATERIAL_FOLDER);
            EnsureFolder(BopBuyApplianceVarietyCatalog.VARIANT_PREFAB_FOLDER);

            int builtCount = 0;
            foreach (ApplianceCategory category in BopBuyApplianceVarietyCatalog.GetCategories())
            {
                foreach (ApplianceVariantSpec spec in BopBuyApplianceVarietyCatalog.GetVariants(category))
                {
                    if (spec.IsBase)
                    {
                        continue;
                    }

                    if (BuildVariant(spec))
                    {
                        builtCount++;
                    }
                }
            }

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log($"[BopBuyApplianceVariantBuilder] Built or updated {builtCount} appliance variants.");
        }

        private static bool BuildVariant(ApplianceVariantSpec spec)
        {
            string basePath = BopBuyApplianceVarietyCatalog.GetBasePrefabPath(spec.Category);
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null)
            {
                Debug.LogError("[BopBuyApplianceVariantBuilder] Base prefab not found: " + basePath);
                return false;
            }

            Material sourceMaterial = FindSourceMaterial(basePrefab, spec);
            if (sourceMaterial == null)
            {
                Debug.LogWarning("[BopBuyApplianceVariantBuilder] No source material found for " + spec.VariantName + ".");
                return false;
            }

            string sourceMaterialName = NormalizeMaterialName(sourceMaterial.name);
            Material variantMaterial = EnsureVariantMaterial(spec, sourceMaterialName, sourceMaterial);
            if (variantMaterial == null)
            {
                return false;
            }

            bool variantExists = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath) != null;
            string loadPath = variantExists ? spec.PrefabPath : basePath;
            GameObject root = PrefabUtility.LoadPrefabContents(loadPath);
            if (root == null)
            {
                Debug.LogError("[BopBuyApplianceVariantBuilder] Could not load prefab contents for " + spec.VariantName + ".");
                return false;
            }

            try
            {
                root.name = spec.VariantName;
                if (!ApplyVariantMaterial(root, spec, sourceMaterialName, variantMaterial))
                {
                    Debug.LogWarning("[BopBuyApplianceVariantBuilder] No renderer used " + sourceMaterialName + " on " + spec.VariantName + ".");
                }

                ClearInScenePlacedFlag(root);
                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.ImportAsset(spec.PrefabPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static Material FindSourceMaterial(GameObject prefab, ApplianceVariantSpec spec)
        {
            if (!string.IsNullOrEmpty(spec.TargetRendererName))
            {
                Renderer renderer = FindRenderer(prefab.transform, spec.TargetRendererName);
                if (renderer != null)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            return material;
                        }
                    }
                }
            }

            foreach (string targetMaterialName in spec.TargetMaterialNames)
            {
                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && string.Equals(NormalizeMaterialName(material.name), targetMaterialName, StringComparison.Ordinal))
                        {
                            return material;
                        }
                    }
                }
            }

            return null;
        }

        private static Renderer FindRenderer(Transform root, string rendererName)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (string.Equals(renderer.name, rendererName, StringComparison.Ordinal))
                {
                    return renderer;
                }
            }

            return null;
        }

        private static Material EnsureVariantMaterial(ApplianceVariantSpec spec, string sourceMaterialName, Material sourceMaterial)
        {
            string materialPath = GetVariantMaterialPath(spec, sourceMaterialName);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(sourceMaterial);
                material.name = Path.GetFileNameWithoutExtension(materialPath);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = sourceMaterial.shader;
                material.CopyPropertiesFromMaterial(sourceMaterial);
                material.name = Path.GetFileNameWithoutExtension(materialPath);
            }

            ApplyTint(material, spec.Tint);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyTint(Material material, Color tint)
        {
            if (material.HasProperty(BASE_COLOR_PROPERTY))
            {
                material.SetColor(BASE_COLOR_PROPERTY, TintColor(material.GetColor(BASE_COLOR_PROPERTY), tint));
            }

            if (material.HasProperty(COLOR_PROPERTY))
            {
                material.SetColor(COLOR_PROPERTY, TintColor(material.GetColor(COLOR_PROPERTY), tint));
            }
        }

        private static Color TintColor(Color source, Color tint)
        {
            float luminance = source.r * LUMINANCE_RED + source.g * LUMINANCE_GREEN + source.b * LUMINANCE_BLUE;
            if (luminance < DARK_MATERIAL_LUMINANCE)
            {
                return new Color(tint.r, tint.g, tint.b, source.a);
            }

            return new Color(source.r * tint.r, source.g * tint.g, source.b * tint.b, source.a);
        }

        private static bool ApplyVariantMaterial(
            GameObject root,
            ApplianceVariantSpec spec,
            string sourceMaterialName,
            Material variantMaterial)
        {
            bool changed = false;
            string variantMaterialName = NormalizeMaterialName(variantMaterial.name);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!string.IsNullOrEmpty(spec.TargetRendererName) &&
                    !string.Equals(renderer.name, spec.TargetRendererName, StringComparison.Ordinal))
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                bool rendererChanged = false;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material material = materials[index];
                    if (material == null)
                    {
                        continue;
                    }

                    bool isTargetRenderer = !string.IsNullOrEmpty(spec.TargetRendererName);
                    string materialName = NormalizeMaterialName(material.name);
                    bool isTargetMaterial = string.Equals(materialName, sourceMaterialName, StringComparison.Ordinal) ||
                                            string.Equals(materialName, variantMaterialName, StringComparison.Ordinal);
                    if (!isTargetRenderer && !isTargetMaterial)
                    {
                        continue;
                    }

                    materials[index] = variantMaterial;
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

        private static void ClearInScenePlacedFlag(GameObject root)
        {
            NetworkObject networkObject = root.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(networkObject);
            SerializedProperty inScenePlaced = serializedObject.FindProperty("m_InScenePlaced");
            if (inScenePlaced != null)
            {
                inScenePlaced.boolValue = false;
            }

            SerializedProperty sceneMigration = serializedObject.FindProperty("SceneMigrationSynchronization");
            if (sceneMigration != null)
            {
                sceneMigration.boolValue = true;
            }

            SerializedProperty sourceHash = serializedObject.FindProperty("InScenePlacedSourceGlobalObjectIdHash");
            if (sourceHash != null)
            {
                sourceHash.longValue = 0;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetVariantMaterialPath(ApplianceVariantSpec spec, string sourceMaterialName)
        {
            string fileName = SanitizeFileName(spec.VariantName + "_" + sourceMaterialName) + ".mat";
            return BopBuyApplianceVarietyCatalog.VARIANT_MATERIAL_FOLDER + "/" + fileName;
        }

        private static string NormalizeMaterialName(string materialName)
        {
            return materialName.Replace(INSTANCE_SUFFIX, string.Empty).Trim();
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            char[] characters = value.ToCharArray();
            for (int index = 0; index < characters.Length; index++)
            {
                if (Array.IndexOf(invalidCharacters, characters[index]) >= 0)
                {
                    characters[index] = '_';
                }
            }

            return new string(characters);
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
