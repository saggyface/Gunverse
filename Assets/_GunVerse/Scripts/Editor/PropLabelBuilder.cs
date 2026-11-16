using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunVerse.EditorTools
{
    public static class PropLabelBuilder
    {
        private const string LABEL_TEXTURES_FOLDER = "Assets/_GunVerse/Textures/ItemLabels";
        private const string LABEL_MATERIALS_FOLDER = "Assets/_GunVerse/Materials/ItemLabels";
        private const string URP_UNLIT_SHADER = "Universal Render Pipeline/Unlit";
        private const float LABEL_ALPHA = 0.7f;

        public static void AddFrontBackLabels(Transform root, string itemName, string textureFile, Vector3 center, float width, float height, float zOffset)
        {
            Material material = GetOrCreateMaterial(itemName, textureFile);
            if (material == null)
            {
                return;
            }

            AddLabelQuad(root, "LabelFront", material, center + new Vector3(0f, 0f, zOffset), Quaternion.Euler(0f, 180f, 0f), width, height);
            AddLabelQuad(root, "LabelBack", material, center + new Vector3(0f, 0f, -zOffset), Quaternion.identity, width, height);
        }

        public static void AddSideLabels(Transform root, string itemName, string textureFile, Vector3 center, float width, float height, float xOffset)
        {
            Material material = GetOrCreateMaterial(itemName, textureFile);
            if (material == null)
            {
                return;
            }

            AddLabelQuad(root, "LabelRight", material, center + new Vector3(xOffset, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), width, height);
            AddLabelQuad(root, "LabelLeft", material, center + new Vector3(-xOffset, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), width, height);
        }

        public static void AddTopLabel(Transform root, string itemName, string textureFile, Vector3 center, float width, float height, float yOffset)
        {
            Material material = GetOrCreateMaterial(itemName, textureFile);
            if (material == null)
            {
                return;
            }

            AddLabelQuad(root, "LabelTop", material, center + new Vector3(0f, yOffset, 0f), Quaternion.Euler(90f, 0f, 0f), width, height);
        }

        public static void AddTopAndBottomLabels(Transform root, string itemName, string textureFile, Vector3 center, float width, float height, float yOffset)
        {
            Material material = GetOrCreateMaterial(itemName, textureFile);
            if (material == null)
            {
                return;
            }

            AddLabelQuad(root, "LabelTop", material, center + new Vector3(0f, yOffset, 0f), Quaternion.Euler(90f, 0f, 0f), width, height);
            AddLabelQuad(root, "LabelBottom", material, center + new Vector3(0f, -yOffset, 0f), Quaternion.Euler(-90f, 0f, 0f), width, height);
        }

        private static void AddLabelQuad(Transform parent, string name, Material material, Vector3 localPosition, Quaternion localRotation, float width, float height)
        {
            GameObject label = GameObject.CreatePrimitive(PrimitiveType.Quad);
            label.name = name;
            Object.DestroyImmediate(label.GetComponent<Collider>());
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = localRotation;
            label.transform.localScale = new Vector3(width, height, 1f);

            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public static void ApplyLabelOpacity(Material material)
        {
            if (material == null)
            {
                return;
            }

            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
            color.a = LABEL_ALPHA;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
        }

        private static Material GetOrCreateMaterial(string itemName, string textureFile)
        {
            string texturePath = LABEL_TEXTURES_FOLDER + "/" + textureFile;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogError("[PropLabelBuilder] Label texture not found: " + texturePath);
                return null;
            }

            ConfigureLabelTexture(texturePath);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                return null;
            }

            EnsureFolder(LABEL_MATERIALS_FOLDER);
            string assetPath = LABEL_MATERIALS_FOLDER + "/" + itemName + "Label.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find(URP_UNLIT_SHADER);
                if (shader == null)
                {
                    Debug.LogError("[PropLabelBuilder] URP Unlit shader not found.");
                    return null;
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            material.enableInstancing = true;
            ApplyLabelOpacity(material);
            return material;
        }

        private static void ConfigureLabelTexture(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool requiresReimport =
                importer.textureType != TextureImporterType.Default ||
                !importer.sRGBTexture ||
                importer.wrapMode != TextureWrapMode.Repeat ||
                importer.filterMode != FilterMode.Bilinear ||
                !importer.mipmapEnabled ||
                importer.alphaIsTransparency;

            if (!requiresReimport)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
