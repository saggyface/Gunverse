using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunVerse.EditorTools
{
    public static class PlayerSkinSetupBuilder
    {
        private const string MENU_PATH = "Tools/Gun Verse/Apply Player Skin Textures";
        private const string LOG_PREFIX = "[PlayerSkinSetupBuilder] ";

        private const string TEXTURE_FOLDER_PATH = "Assets/_GunVerse/Models/Player/textures";
        private const string DIFFUSE_TEXTURE_PATH = TEXTURE_FOLDER_PATH + "/Ch36_1001_Diffuse.png";
        private const string NORMAL_TEXTURE_PATH = TEXTURE_FOLDER_PATH + "/Ch36_1001_Normal.png";
        private const string GLOSSINESS_TEXTURE_PATH = TEXTURE_FOLDER_PATH + "/Ch36_1001_Glossiness.png";
        private const string SPECULAR_TEXTURE_PATH = TEXTURE_FOLDER_PATH + "/Ch36_1001_Specular.png";
        private const string SPEC_GLOSS_TEXTURE_PATH = TEXTURE_FOLDER_PATH + "/PlayerSkin_SpecGloss.asset";

        private const string MATERIAL_FOLDER_PATH = "Assets/_GunVerse/Materials";
        private const string PLAYER_SKIN_MATERIAL_PATH = MATERIAL_FOLDER_PATH + "/PlayerSkin.mat";
        private const string EXISTING_PLAYER_MATERIAL_PATH = MATERIAL_FOLDER_PATH + "/Player.mat";

        private const string BODY_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/MannequinBody.prefab";
        private const string VIEWMODEL_ARMS_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/MannequinViewModelArms.prefab";
        private const string NETWORK_PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";

        private const string PLAYER_SKIN_MATERIAL_NAME = "PlayerSkin";
        private const string SPEC_GLOSS_TEXTURE_NAME = "PlayerSkin_SpecGloss";
        private const string LIT_SHADER_NAME = "Universal Render Pipeline/Lit";
        private const string OLD_FBX_MATERIAL_GUID = "ea8bae5822b7e43468ba208bc85e69d9";
        private const string MANNEQUIN_MESH_NAME_FRAGMENT = "Mannequin";

        private const int TEXTURE_MAX_SIZE = 2048;
        private const int SPEC_GLOSS_TEXTURE_SIZE = 1024;
        private const bool SPEC_GLOSS_HAS_MIPMAPS = true;
        private const bool SPEC_GLOSS_IS_LINEAR = true;
        private const float HALF_PIXEL_OFFSET = 0.5f;
        private const float SPECULAR_WORKFLOW_MODE = 1f;
        private const float FULL_INTENSITY = 1f;
        private const float NO_METALLIC = 0f;
        private const float OPAQUE_SURFACE = 0f;
        private const float RECEIVE_SHADOWS_ENABLED = 1f;

        private const string BASE_MAP_PROPERTY = "_BaseMap";
        private const string BASE_COLOR_PROPERTY = "_BaseColor";
        private const string BUMP_MAP_PROPERTY = "_BumpMap";
        private const string BUMP_SCALE_PROPERTY = "_BumpScale";
        private const string WORKFLOW_MODE_PROPERTY = "_WorkflowMode";
        private const string SPEC_GLOSS_MAP_PROPERTY = "_SpecGlossMap";
        private const string SPEC_COLOR_PROPERTY = "_SpecColor";
        private const string GLOSS_MAP_SCALE_PROPERTY = "_GlossMapScale";
        private const string SMOOTHNESS_PROPERTY = "_Smoothness";
        private const string METALLIC_PROPERTY = "_Metallic";
        private const string SURFACE_PROPERTY = "_Surface";
        private const string CULL_PROPERTY = "_Cull";
        private const string RECEIVE_SHADOWS_PROPERTY = "_ReceiveShadows";

        private const string NORMAL_MAP_KEYWORD = "_NORMALMAP";
        private const string SPECULAR_SETUP_KEYWORD = "_SPECULAR_SETUP";
        private const string METALLIC_SPEC_GLOSS_MAP_KEYWORD = "_METALLICSPECGLOSSMAP";

        [MenuItem(MENU_PATH)]
        public static void Apply()
        {
            if (!ConfigureBaseTextureImporters())
            {
                return;
            }

            Texture2D specGlossTexture = null;
            try
            {
                if (ConfigureReadableLinearTexture(SPECULAR_TEXTURE_PATH) &&
                    ConfigureReadableLinearTexture(GLOSSINESS_TEXTURE_PATH))
                {
                    specGlossTexture = BuildSpecGlossTexture();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                SetTextureReadability(SPECULAR_TEXTURE_PATH, false);
                SetTextureReadability(GLOSSINESS_TEXTURE_PATH, false);
            }

            if (specGlossTexture == null)
            {
                Debug.LogError(LOG_PREFIX + "Could not build the combined spec/gloss texture.");
                return;
            }

            Texture2D diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DIFFUSE_TEXTURE_PATH);
            Texture2D normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(NORMAL_TEXTURE_PATH);
            if (diffuseTexture == null || normalTexture == null)
            {
                Debug.LogError(LOG_PREFIX + "Diffuse or normal texture could not be loaded.");
                return;
            }

            Material skinMaterial = CreateOrReplaceSkinMaterial(diffuseTexture, normalTexture, specGlossTexture);
            if (skinMaterial == null)
            {
                return;
            }

            int bodyRendererCount = AssignMaterialToAllRenderers(BODY_PREFAB_PATH, skinMaterial);
            int viewModelRendererCount = AssignMaterialToAllRenderers(VIEWMODEL_ARMS_PREFAB_PATH, skinMaterial);
            int networkPlayerRendererCount = ApplyMaterialOverrides(NETWORK_PLAYER_PREFAB_PATH, skinMaterial);

            AssetDatabase.SaveAssets();

            int totalRendererCount = bodyRendererCount + viewModelRendererCount + networkPlayerRendererCount;
            Debug.Log(LOG_PREFIX + "Material: " + PLAYER_SKIN_MATERIAL_PATH);
            Debug.Log(LOG_PREFIX + "Textures: " + DIFFUSE_TEXTURE_PATH + ", " + NORMAL_TEXTURE_PATH + ", " + SPEC_GLOSS_TEXTURE_PATH);
            Debug.Log(LOG_PREFIX + "Renderers updated: MannequinBody=" + bodyRendererCount +
                      ", MannequinViewModelArms=" + viewModelRendererCount +
                      ", NetworkPlayer=" + networkPlayerRendererCount +
                      ", total=" + totalRendererCount);
        }

        private static bool ConfigureBaseTextureImporters()
        {
            return ConfigureDiffuseTexture() && ConfigureNormalTexture();
        }

        private static bool ConfigureDiffuseTexture()
        {
            TextureImporter importer = GetTextureImporter(DIFFUSE_TEXTURE_PATH);
            if (importer == null)
            {
                return false;
            }

            ApplyCommonTextureImportSettings(importer, TextureImporterType.Default, true, false);
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
            LogTextureImporter(DIFFUSE_TEXTURE_PATH);
            return true;
        }

        private static bool ConfigureNormalTexture()
        {
            TextureImporter importer = GetTextureImporter(NORMAL_TEXTURE_PATH);
            if (importer == null)
            {
                return false;
            }

            ApplyCommonTextureImportSettings(importer, TextureImporterType.NormalMap, false, false);
            importer.normalmapFilter = TextureImporterNormalFilter.Standard;
            importer.SaveAndReimport();
            LogTextureImporter(NORMAL_TEXTURE_PATH);
            return true;
        }

        private static bool ConfigureReadableLinearTexture(string assetPath)
        {
            TextureImporter importer = GetTextureImporter(assetPath);
            if (importer == null)
            {
                return false;
            }

            ApplyCommonTextureImportSettings(importer, TextureImporterType.Default, false, true);
            importer.SaveAndReimport();
            LogTextureImporter(assetPath);
            return true;
        }

        private static void ApplyCommonTextureImportSettings(
            TextureImporter importer,
            TextureImporterType textureType,
            bool usesSrgb,
            bool isReadable)
        {
            importer.textureType = textureType;
            importer.sRGBTexture = usesSrgb;
            importer.maxTextureSize = TEXTURE_MAX_SIZE;
            importer.isReadable = isReadable;
        }

        private static TextureImporter GetTextureImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError(LOG_PREFIX + "Texture importer not found: " + assetPath);
            }

            return importer;
        }

        private static void LogTextureImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            Debug.Log(LOG_PREFIX + "Importer " + assetPath +
                      " | type=" + importer.textureType +
                      " sRGB=" + importer.sRGBTexture +
                      " alphaIsTransparency=" + importer.alphaIsTransparency +
                      " isReadable=" + importer.isReadable +
                      " maxTextureSize=" + importer.maxTextureSize +
                      " normalmapFilter=" + importer.normalmapFilter);
        }

        private static void SetTextureReadability(string assetPath, bool isReadable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning(LOG_PREFIX + "Could not change readability for " + assetPath);
                return;
            }

            importer.isReadable = isReadable;
            importer.SaveAndReimport();
            LogTextureImporter(assetPath);
        }

        private static Texture2D BuildSpecGlossTexture()
        {
            Texture2D specularTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SPECULAR_TEXTURE_PATH);
            Texture2D glossinessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GLOSSINESS_TEXTURE_PATH);
            if (specularTexture == null || glossinessTexture == null)
            {
                Debug.LogError(LOG_PREFIX + "Specular or glossiness texture could not be loaded.");
                return null;
            }

            Texture2D combinedTexture = new Texture2D(
                SPEC_GLOSS_TEXTURE_SIZE,
                SPEC_GLOSS_TEXTURE_SIZE,
                TextureFormat.RGBA32,
                SPEC_GLOSS_HAS_MIPMAPS,
                SPEC_GLOSS_IS_LINEAR);
            combinedTexture.name = SPEC_GLOSS_TEXTURE_NAME;

            Color[] combinedPixels = new Color[SPEC_GLOSS_TEXTURE_SIZE * SPEC_GLOSS_TEXTURE_SIZE];
            int pixelIndex = 0;
            for (int y = 0; y < SPEC_GLOSS_TEXTURE_SIZE; y++)
            {
                float normalizedY = (y + HALF_PIXEL_OFFSET) / SPEC_GLOSS_TEXTURE_SIZE;
                for (int x = 0; x < SPEC_GLOSS_TEXTURE_SIZE; x++)
                {
                    float normalizedX = (x + HALF_PIXEL_OFFSET) / SPEC_GLOSS_TEXTURE_SIZE;
                    Color specularSample = specularTexture.GetPixelBilinear(normalizedX, normalizedY);
                    Color glossinessSample = glossinessTexture.GetPixelBilinear(normalizedX, normalizedY);
                    combinedPixels[pixelIndex] = new Color(
                        specularSample.r,
                        specularSample.g,
                        specularSample.b,
                        glossinessSample.grayscale);
                    pixelIndex++;
                }
            }

            combinedTexture.SetPixels(combinedPixels);
            combinedTexture.Apply(SPEC_GLOSS_HAS_MIPMAPS, false);

            EnsureFolderExists(SPEC_GLOSS_TEXTURE_PATH);
            AssetDatabase.DeleteAsset(SPEC_GLOSS_TEXTURE_PATH);
            AssetDatabase.CreateAsset(combinedTexture, SPEC_GLOSS_TEXTURE_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log(LOG_PREFIX + "Created " + SPEC_GLOSS_TEXTURE_PATH +
                      " (" + SPEC_GLOSS_TEXTURE_SIZE + "x" + SPEC_GLOSS_TEXTURE_SIZE +
                      ", RGBA32, linear, mipmaps)");
            return combinedTexture;
        }

        private static Material CreateOrReplaceSkinMaterial(
            Texture2D diffuseTexture,
            Texture2D normalTexture,
            Texture2D specGlossTexture)
        {
            Shader shader = FindSkinShader();
            if (shader == null)
            {
                Debug.LogError(LOG_PREFIX + "Could not find the URP Lit shader.");
                return null;
            }

            Material materialTemplate = new Material(shader);
            materialTemplate.name = PLAYER_SKIN_MATERIAL_NAME;
            Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(PLAYER_SKIN_MATERIAL_PATH);

            if (existingMaterial == null)
            {
                ConfigureSkinMaterial(materialTemplate, diffuseTexture, normalTexture, specGlossTexture);
                EnsureFolderExists(PLAYER_SKIN_MATERIAL_PATH);
                AssetDatabase.CreateAsset(materialTemplate, PLAYER_SKIN_MATERIAL_PATH);
                AssetDatabase.SaveAssets();
                Debug.Log(LOG_PREFIX + "Created " + PLAYER_SKIN_MATERIAL_PATH);
                return materialTemplate;
            }

            EditorUtility.CopySerialized(materialTemplate, existingMaterial);
            UnityEngine.Object.DestroyImmediate(materialTemplate);
            ConfigureSkinMaterial(existingMaterial, diffuseTexture, normalTexture, specGlossTexture);
            existingMaterial.name = PLAYER_SKIN_MATERIAL_NAME;
            EditorUtility.SetDirty(existingMaterial);
            AssetDatabase.SaveAssets();
            Debug.Log(LOG_PREFIX + "Replaced " + PLAYER_SKIN_MATERIAL_PATH);
            return existingMaterial;
        }

        private static Shader FindSkinShader()
        {
            Shader shader = Shader.Find(LIT_SHADER_NAME);
            if (shader != null)
            {
                return shader;
            }

            Material existingPlayerMaterial = AssetDatabase.LoadAssetAtPath<Material>(EXISTING_PLAYER_MATERIAL_PATH);
            return existingPlayerMaterial != null ? existingPlayerMaterial.shader : null;
        }

        private static void ConfigureSkinMaterial(
            Material material,
            Texture2D diffuseTexture,
            Texture2D normalTexture,
            Texture2D specGlossTexture)
        {
            material.SetTexture(BASE_MAP_PROPERTY, diffuseTexture);
            material.SetColor(BASE_COLOR_PROPERTY, Color.white);
            material.SetTexture(BUMP_MAP_PROPERTY, normalTexture);
            material.SetFloat(BUMP_SCALE_PROPERTY, FULL_INTENSITY);
            material.SetFloat(WORKFLOW_MODE_PROPERTY, SPECULAR_WORKFLOW_MODE);
            material.SetTexture(SPEC_GLOSS_MAP_PROPERTY, specGlossTexture);
            material.SetColor(SPEC_COLOR_PROPERTY, Color.white);
            material.SetFloat(GLOSS_MAP_SCALE_PROPERTY, FULL_INTENSITY);
            material.SetFloat(SMOOTHNESS_PROPERTY, FULL_INTENSITY);
            material.SetFloat(METALLIC_PROPERTY, NO_METALLIC);
            material.SetFloat(SURFACE_PROPERTY, OPAQUE_SURFACE);
            material.SetFloat(CULL_PROPERTY, (float)CullMode.Back);
            material.SetFloat(RECEIVE_SHADOWS_PROPERTY, RECEIVE_SHADOWS_ENABLED);
            material.doubleSidedGI = false;
            material.renderQueue = (int)RenderQueue.Geometry;

            material.EnableKeyword(NORMAL_MAP_KEYWORD);
            material.EnableKeyword(SPECULAR_SETUP_KEYWORD);
            material.EnableKeyword(METALLIC_SPEC_GLOSS_MAP_KEYWORD);
        }

        private static int AssignMaterialToAllRenderers(string prefabPath, Material material)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null)
            {
                Debug.LogError(LOG_PREFIX + "Could not load prefab: " + prefabPath);
                return 0;
            }

            try
            {
                int updatedCount = 0;
                foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                {
                    if (!IsSupportedRenderer(renderer))
                    {
                        continue;
                    }

                    renderer.sharedMaterials = new[] { material };
                    RecordPrefabOverride(renderer);
                    updatedCount++;
                }

                if (updatedCount == 0)
                {
                    Debug.LogWarning(LOG_PREFIX + "No supported renderers found in " + prefabPath);
                    return 0;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log(LOG_PREFIX + "Assigned material to " + updatedCount + " renderer(s) in " + prefabPath);
                return updatedCount;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static int ApplyMaterialOverrides(string prefabPath, Material material)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null)
            {
                Debug.LogError(LOG_PREFIX + "Could not load prefab: " + prefabPath);
                return 0;
            }

            try
            {
                int updatedCount = 0;
                foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                {
                    if (!ShouldApplyMaterialOverride(renderer))
                    {
                        continue;
                    }

                    renderer.sharedMaterials = new[] { material };
                    RecordPrefabOverride(renderer);
                    updatedCount++;
                }

                if (updatedCount == 0)
                {
                    Debug.Log(LOG_PREFIX + "No matching renderers found in " + prefabPath);
                    return 0;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log(LOG_PREFIX + "Set material override on " + updatedCount + " renderer(s) in " + prefabPath);
                return updatedCount;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static bool ShouldApplyMaterialOverride(Renderer renderer)
        {
            if (!IsSupportedRenderer(renderer))
            {
                return false;
            }

            Mesh sharedMesh = GetSharedMesh(renderer);
            if (sharedMesh != null &&
                sharedMesh.name.IndexOf(MANNEQUIN_MESH_NAME_FRAGMENT, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            foreach (Material material in renderer.sharedMaterials)
            {
                if (IsOldFbxMaterial(material))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOldFbxMaterial(Material material)
        {
            if (material == null)
            {
                return false;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string assetGuid, out long _))
            {
                return false;
            }

            return assetGuid == OLD_FBX_MATERIAL_GUID;
        }

        private static Mesh GetSharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static bool IsSupportedRenderer(Renderer renderer)
        {
            return renderer is SkinnedMeshRenderer || renderer is MeshRenderer;
        }

        private static void RecordPrefabOverride(UnityEngine.Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        private static void EnsureFolderExists(string assetPath)
        {
            string folderPath = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            folderPath = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] folderSegments = folderPath.Split('/');
            string currentPath = folderSegments[0];
            for (int index = 1; index < folderSegments.Length; index++)
            {
                string nextPath = currentPath + "/" + folderSegments[index];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folderSegments[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
