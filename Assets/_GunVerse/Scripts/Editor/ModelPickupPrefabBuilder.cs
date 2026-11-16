using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunVerse.EditorTools
{
    public static class ModelPickupPrefabBuilder
    {
        private const string CONFIG_PATH = "Tools/AssetIntake/item-models.json";
        private const string MODEL_VISUAL_NAME = "ModelVisual";
        private const string LABEL_VISUAL_NAME = "LabelVisual";
        private const string LABEL_MATERIALS_FOLDER = "Assets/_GunVerse/Materials/ItemLabels";
        private const string LABEL_MESHES_FOLDER = "Assets/_GunVerse/Meshes/ItemLabels";
        private const string URP_UNLIT_SHADER = "Universal Render Pipeline/Unlit";
        private const int LABEL_SLEEVE_SEGMENTS = 24;
        private const float LABEL_SLEEVE_U_REPEAT = 2f;

        [Serializable]
        private sealed class ItemModelCatalog
        {
            public ItemModelEntry[] items;
        }

        [Serializable]
        private sealed class ItemMaterialColor
        {
            public string material;
            public string color;
        }

        [Serializable]
        private sealed class ItemModelEntry
        {
            public string itemPrefab;
            public string modelPath;
            public float targetHeight;
            public Vector3 targetSize;
            public ItemMaterialColor[] materialColors;
            public string labelTexture;
            public string labelKind;
            public float labelWidthFactor;
            public float labelHeightFactor;
            public Vector3 pickupScale;
            public Vector3 pickupPositionOffset;
            public Vector3 pickupRotationEuler;
            public Vector3 muzzleOffset;
            public string physicsMaterial;
        }

        public static void ApplyConfiguredModels()
        {
            string configPath = Path.Combine(Application.dataPath, "..", CONFIG_PATH);
            if (!File.Exists(configPath))
            {
                Debug.LogWarning("[ModelPickupPrefabBuilder] Model catalog not found: " + configPath);
                return;
            }

            ItemModelCatalog catalog = JsonUtility.FromJson<ItemModelCatalog>(File.ReadAllText(configPath));
            if (catalog == null || catalog.items == null)
            {
                Debug.LogError("[ModelPickupPrefabBuilder] Model catalog is empty or invalid.");
                return;
            }

            EnsureFolder(LABEL_MATERIALS_FOLDER);
            EnsureFolder(LABEL_MESHES_FOLDER);

            foreach (ItemModelEntry entry in catalog.items)
            {
                ApplyModel(entry);
            }

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
        }

        private static void ApplyModel(ItemModelEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.itemPrefab) || string.IsNullOrWhiteSpace(entry.modelPath))
            {
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(entry.itemPrefab);
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(entry.modelPath);
            if (prefabAsset == null || modelAsset == null)
            {
                Debug.LogError($"[ModelPickupPrefabBuilder] Missing asset for {entry.itemPrefab} or {entry.modelPath}.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(entry.itemPrefab);
            GunItem gunItem = root.GetComponent<GunItem>();
            if (gunItem == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("[ModelPickupPrefabBuilder] GunItem not found on " + entry.itemPrefab);
                return;
            }

            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            DestroyChild(root.transform, MODEL_VISUAL_NAME);
            DestroyChild(root.transform, LABEL_VISUAL_NAME);

            foreach (MeshFilter meshFilter in root.GetComponents<MeshFilter>())
            {
                UnityEngine.Object.DestroyImmediate(meshFilter);
            }

            foreach (MeshRenderer meshRenderer in root.GetComponents<MeshRenderer>())
            {
                UnityEngine.Object.DestroyImmediate(meshRenderer);
            }

            foreach (Collider collider in root.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            GameObject visual = UnityEngine.Object.Instantiate(modelAsset, root.transform, false);
            visual.name = MODEL_VISUAL_NAME;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one;

            RemoveGameplayComponents(visual);
            ApplyMaterialColors(visual, entry.materialColors);

            Bounds bounds = GetRendererBounds(visual);
            if (!HasVolume(bounds.size))
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("[ModelPickupPrefabBuilder] Model has no usable renderer bounds: " + entry.modelPath);
                return;
            }

            Vector3 targetSize = ResolveTargetSize(entry, bounds.size);
            Vector3 scale = new Vector3(
                targetSize.x / bounds.size.x,
                targetSize.y / bounds.size.y,
                targetSize.z / bounds.size.z);
            visual.transform.localScale = scale;

            bounds = GetRendererBounds(visual);
            visual.transform.localPosition -= bounds.center;

            BoxCollider boxCollider = root.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = targetSize;

            SerializedObject serializedObject = new SerializedObject(gunItem);
            if (entry.pickupScale.sqrMagnitude > 0f)
            {
                serializedObject.FindProperty("pickupScale").vector3Value = entry.pickupScale;
            }

            if (entry.pickupPositionOffset.sqrMagnitude > 0f || entry.pickupRotationEuler.sqrMagnitude > 0f)
            {
                serializedObject.FindProperty("pickupPositionOffset").vector3Value = entry.pickupPositionOffset;
                serializedObject.FindProperty("pickupRotationEuler").vector3Value = entry.pickupRotationEuler;
            }

            if (entry.muzzleOffset.sqrMagnitude > 0f)
            {
                serializedObject.FindProperty("muzzleOffset").vector3Value = entry.muzzleOffset;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            GunPropPhysicsAssets.ApplyForWeightClass(root);

            AddLabel(root.transform, entry, targetSize, Path.GetFileNameWithoutExtension(entry.itemPrefab));
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);

            PrefabUtility.SaveAsPrefabAsset(root, entry.itemPrefab);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void RemoveGameplayComponents(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            foreach (Rigidbody rigidBody in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                UnityEngine.Object.DestroyImmediate(rigidBody);
            }

            foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
            {
                UnityEngine.Object.DestroyImmediate(animator);
            }

            foreach (MonoBehaviour behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))
            {
                UnityEngine.Object.DestroyImmediate(behaviour);
            }
        }

        private static void ApplyMaterialColors(GameObject visual, ItemMaterialColor[] materialColors)
        {
            if (materialColors == null || materialColors.Length == 0)
            {
                return;
            }

            Dictionary<string, Color> colors = new Dictionary<string, Color>();
            foreach (ItemMaterialColor entry in materialColors)
            {
                if (!string.IsNullOrWhiteSpace(entry.material) &&
                    ColorUtility.TryParseHtmlString(entry.color, out Color color))
                {
                    colors[entry.material] = color;
                }
            }

            HashSet<Material> materials = new HashSet<Material>();
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            foreach (Material material in materials)
            {
                if (!colors.TryGetValue(material.name, out Color color))
                {
                    continue;
                }

                if (material.HasProperty("baseColorFactor"))
                {
                    material.SetColor("baseColorFactor", color);
                }
                else if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                EditorUtility.SetDirty(material);
            }
        }

        private static void AddLabel(Transform root, ItemModelEntry entry, Vector3 targetSize, string itemName)
        {
            if (string.IsNullOrWhiteSpace(entry.labelTexture))
            {
                return;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.labelTexture);
            if (texture == null)
            {
                Debug.LogError("[ModelPickupPrefabBuilder] Label texture not found: " + entry.labelTexture);
                return;
            }

            ConfigureLabelTexture(entry.labelTexture);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.labelTexture);
            if (texture == null)
            {
                return;
            }

            Material material = GetOrCreateLabelMaterial(itemName, texture);
            GameObject labelRoot = new GameObject(LABEL_VISUAL_NAME);
            labelRoot.transform.SetParent(root, false);

            if (entry.labelKind == "Sleeve")
            {
                AddSleeveLabel(labelRoot.transform, targetSize, itemName, material);
            }
            else if (entry.labelKind == "Top")
            {
                AddTopLabel(labelRoot.transform, targetSize, material, entry.labelWidthFactor, entry.labelHeightFactor);
            }
            else
            {
                float widthFactor = entry.labelWidthFactor > 0f ? entry.labelWidthFactor : 0.82f;
                float heightFactor = entry.labelHeightFactor > 0f ? entry.labelHeightFactor : 0.55f;
                AddFrontBackLabels(labelRoot.transform, targetSize, material, widthFactor, heightFactor);
            }
        }

        private static void AddTopLabel(Transform labelRoot, Vector3 targetSize, Material material, float widthFactor, float heightFactor)
        {
            float width = targetSize.x * (widthFactor > 0f ? widthFactor : 0.82f);
            float depth = targetSize.z * (heightFactor > 0f ? heightFactor : 0.82f);
            float yOffset = targetSize.y * 0.5f + 0.004f;

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Quad);
            top.name = "LabelTop";
            UnityEngine.Object.DestroyImmediate(top.GetComponent<Collider>());
            top.transform.SetParent(labelRoot, false);
            top.transform.localPosition = new Vector3(0f, yOffset, 0f);
            top.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            top.transform.localScale = new Vector3(width, depth, 1f);
            ConfigureLabelRenderer(top, material);
        }

        private static void AddFrontBackLabels(Transform labelRoot, Vector3 targetSize, Material material, float widthFactor, float heightFactor)
        {
            float width = targetSize.x * widthFactor;
            float height = targetSize.y * heightFactor;
            float zOffset = targetSize.z * 0.5f + 0.004f;

            GameObject front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            front.name = "LabelFront";
            UnityEngine.Object.DestroyImmediate(front.GetComponent<Collider>());
            front.transform.SetParent(labelRoot, false);
            front.transform.localPosition = new Vector3(0f, 0f, zOffset);
            front.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            front.transform.localScale = new Vector3(width, height, 1f);
            ConfigureLabelRenderer(front, material);

            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "LabelBack";
            UnityEngine.Object.DestroyImmediate(back.GetComponent<Collider>());
            back.transform.SetParent(labelRoot, false);
            back.transform.localPosition = new Vector3(0f, 0f, -zOffset);
            back.transform.localRotation = Quaternion.identity;
            back.transform.localScale = new Vector3(width, height, 1f);
            ConfigureLabelRenderer(back, material);
        }

        private static void AddSleeveLabel(Transform labelRoot, Vector3 targetSize, string itemName, Material material)
        {
            float radius = Mathf.Max(targetSize.x, targetSize.z) * 0.5f + 0.004f;
            float height = targetSize.y * 0.55f;
            Mesh mesh = GetOrCreateSleeveMesh(itemName, radius, height);

            GameObject sleeve = new GameObject("LabelSleeve");
            sleeve.transform.SetParent(labelRoot, false);
            MeshFilter meshFilter = sleeve.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            MeshRenderer meshRenderer = sleeve.AddComponent<MeshRenderer>();
            ConfigureLabelRenderer(meshRenderer, material);
        }

        private static void ConfigureLabelRenderer(GameObject label, Material material)
        {
            ConfigureLabelRenderer(label.GetComponent<MeshRenderer>(), material);
        }

        private static void ConfigureLabelRenderer(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Material GetOrCreateLabelMaterial(string itemName, Texture2D texture)
        {
            string assetPath = $"{LABEL_MATERIALS_FOLDER}/{itemName}Label.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                Shader shader = Shader.Find(URP_UNLIT_SHADER);
                if (shader == null)
                {
                    Debug.LogError("[ModelPickupPrefabBuilder] URP Unlit shader not found.");
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
            PropLabelBuilder.ApplyLabelOpacity(material);
            return material;
        }

        private static Mesh GetOrCreateSleeveMesh(string itemName, float radius, float height)
        {
            string assetPath = $"{LABEL_MESHES_FOLDER}/{itemName}Sleeve.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = itemName + "Sleeve" };
                AssetDatabase.CreateAsset(mesh, assetPath);
            }

            mesh.Clear();

            int vertexCount = (LABEL_SLEEVE_SEGMENTS + 1) * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[LABEL_SLEEVE_SEGMENTS * 6];
            float halfHeight = height * 0.5f;

            for (int i = 0; i <= LABEL_SLEEVE_SEGMENTS; i++)
            {
                float angle = Mathf.PI * 2f * i / LABEL_SLEEVE_SEGMENTS;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                Vector3 normal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                int bottom = i * 2;
                int top = bottom + 1;

                vertices[bottom] = new Vector3(x, -halfHeight, z);
                vertices[top] = new Vector3(x, halfHeight, z);
                normals[bottom] = normal;
                normals[top] = normal;
                uv[bottom] = new Vector2((float)i / LABEL_SLEEVE_SEGMENTS * LABEL_SLEEVE_U_REPEAT, 0f);
                uv[top] = new Vector2((float)i / LABEL_SLEEVE_SEGMENTS * LABEL_SLEEVE_U_REPEAT, 1f);
            }

            for (int i = 0; i < LABEL_SLEEVE_SEGMENTS; i++)
            {
                int bottom = i * 2;
                int top = bottom + 1;
                int nextBottom = bottom + 2;
                int nextTop = bottom + 3;
                int triangle = i * 6;

                triangles[triangle] = bottom;
                triangles[triangle + 1] = top;
                triangles[triangle + 2] = nextBottom;
                triangles[triangle + 3] = nextBottom;
                triangles[triangle + 4] = top;
                triangles[triangle + 5] = nextTop;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
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

        private static void DestroyChild(Transform root, string childName)
        {
            Transform child = root.Find(childName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Vector3 ResolveTargetSize(ItemModelEntry entry, Vector3 boundsSize)
        {
            if (entry.targetSize != Vector3.zero)
            {
                return entry.targetSize;
            }

            if (entry.targetHeight > 0f && boundsSize.y > 0f)
            {
                return boundsSize * (entry.targetHeight / boundsSize.y);
            }

            return boundsSize;
        }

        private static bool HasVolume(Vector3 size)
        {
            return size.x > 0.0001f && size.y > 0.0001f && size.z > 0.0001f;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
