using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class GunPropBakeUtility
    {
        private const string BAKED_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps/Baked";
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";
        private const string MONITOR_PREFAB = "Assets/_GunVerse/Prefabs/Items/MonitorGun.prefab";
        private const string DESIGN_TABLE_PREFAB = "Assets/_GunVerse/Prefabs/Items/DesignTableGun.prefab";
        private const string CHECKOUT_COUNTER_PREFAB = LARGE_PROPS_FOLDER + "/CheckoutCounterGun.prefab";
        private const string SHELF_RUN_PREFAB = LARGE_PROPS_FOLDER + "/ShelfRunGun.prefab";

        private const float EPSILON = 0.001f;

        private sealed class VariantSpec
        {
            public string SourcePath;
            public string VariantName;
            public Vector3 Scale;
        }

        private static readonly VariantSpec[] CommonVariants =
        {
            new VariantSpec { SourcePath = SHELF_RUN_PREFAB, VariantName = "ShelfRunLongGun", Scale = new Vector3(1f, 1f, 0.937f) },
            new VariantSpec { SourcePath = SHELF_RUN_PREFAB, VariantName = "ShelfRunShortGun", Scale = new Vector3(1f, 1f, 0.261f) },
            new VariantSpec { SourcePath = SHELF_RUN_PREFAB, VariantName = "HalfShelfRunGun", Scale = new Vector3(1f, 0.5f, 0.937f) },
            new VariantSpec { SourcePath = CHECKOUT_COUNTER_PREFAB, VariantName = "GuysquadCheckoutCounterGun", Scale = new Vector3(1f, 1f, 0.86f) },
            new VariantSpec { SourcePath = CHECKOUT_COUNTER_PREFAB, VariantName = "PCCheckoutCounterGun", Scale = new Vector3(1f, 1f, 0.75f) },
            new VariantSpec { SourcePath = CHECKOUT_COUNTER_PREFAB, VariantName = "MobileStaffCounterGun", Scale = new Vector3(1f, 1f, 5.101f / 3.2f) },
            new VariantSpec { SourcePath = DESIGN_TABLE_PREFAB, VariantName = "MobileCustomerTableGun", Scale = new Vector3(0.6f, 1f, 0.45f) },
            new VariantSpec { SourcePath = MONITOR_PREFAB, VariantName = "LargeMonitorGun", Scale = new Vector3(1.45f, 1.75f, 1f) }
        };

        [MenuItem("Tools/Gun Verse/Repair Empty Guysquad Shelf")]
        public static void RepairEmptyGuysquadShelf()
        {
            Scene scene = SceneManager.GetActiveScene();
            GunItem shelf = UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include)
                .FirstOrDefault(item => item != null && item.name == "Guysquad Shelf (5)" && item.transform.childCount == 0);

            if (shelf == null)
            {
                Debug.LogWarning("[GunPropBakeUtility] No empty Guysquad Shelf (5) was found.");
                return;
            }

            AddTallShelfGeometry(shelf.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            BakeAndFinalizeSceneProps();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Gun Verse/Add Solid Collision Proxies To Large Props")]
        public static void AddCollisionProxiesToLargeGunPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int changed = 0;
            string[] largePropTerms = { "Shelf", "Table", "Counter", "Workbench", "Podium" };

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<GunItem>() == null ||
                    prefab.GetComponent<Collider>() != null ||
                    !largePropTerms.Any(term => prefab.name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                AddSolidCollisionProxy(root);
                GunPropPhysicsAssets.ApplyForWeightClass(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                ClearInScenePlacedFlags(path);
                changed++;
            }

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log($"[GunPropBakeUtility] Added solid collision proxies to {changed} large gun prefabs.");
        }

        [MenuItem("Tools/Gun Verse/Mark Shelves As Self Projectiles")]
        public static void MarkShelvesAsSelfProjectiles()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<GunItem>() == null ||
                    !prefab.name.Contains("Shelf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(prefab.GetComponent<GunItem>());
                SerializedProperty selfProjectile = serializedObject.FindProperty("fireAsSelfProjectile");
                SerializedProperty magazineSize = serializedObject.FindProperty("magazineSize");
                if (selfProjectile == null)
                {
                    continue;
                }

                selfProjectile.boolValue = true;
                if (magazineSize != null)
                {
                    magazineSize.intValue = 1;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
                changed++;
            }

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log($"[GunPropBakeUtility] Marked {changed} shelf prefabs as self-projectiles.");
        }

        [MenuItem("Tools/Gun Verse/Add Solid Collision Proxies To Baked Props")]
        public static void AddCollisionProxiesToBakedPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { BAKED_FOLDER });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                AddSolidCollisionProxy(root);
                GunPropPhysicsAssets.ApplyForWeightClass(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                ClearInScenePlacedFlags(path);
            }

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log($"[GunPropBakeUtility] Added solid collision proxies to {guids.Length} baked props.");
        }

        [MenuItem("Tools/Gun Verse/Bake And Audit BopBuy Gun Props")]
        public static void BakeAndAuditActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[GunPropBakeUtility] No active scene is loaded.");
                return;
            }

            int replaced = BakeAndReplaceSceneProps(scene);
            GunItemPrefabRegistry.Rebuild();
            AssignSceneProjectileHashes(scene);
            int remaining = AuditScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[GunPropBakeUtility] Baked/replaced {replaced} props in {scene.name}; {remaining} offenders remain.");
        }

        public static void EnsureBakedVariants()
        {
            EnsureBakedFolder();
            EnsureCommonVariants();
        }

        public static int BakeAndReplaceSceneProps()
        {
            return BakeAndReplaceSceneProps(SceneManager.GetActiveScene());
        }

        public static int BakeAndFinalizeSceneProps()
        {
            Scene scene = SceneManager.GetActiveScene();
            int replaced = BakeAndReplaceSceneProps(scene);
            GunItemPrefabRegistry.Rebuild();
            AssignSceneProjectileHashes(scene);
            int remaining = AuditScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (remaining > 0)
            {
                Debug.LogWarning($"[GunPropBakeUtility] {remaining} GunItem offenders remain in {scene.name}.");
            }

            return replaced;
        }

        public static int BakeAndReplaceSceneProps(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            EnsureBakedFolder();
            EnsureCommonVariants();

            List<GameObject> gunRoots = UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include)
                .Where(item => item != null && item.gameObject.scene == scene && item.GetComponent<NetworkObject>() != null)
                .Select(item => item.gameObject)
                .OrderBy(item => item.name)
                .ThenBy(item => item.transform.position.x)
                .ThenBy(item => item.transform.position.z)
                .ToList();

            int replaced = 0;
            foreach (GameObject oldRoot in gunRoots)
            {
                if (oldRoot == null)
                {
                    continue;
                }

                GameObject replacement = HasAddedSceneChildren(oldRoot)
                    ? ReplaceWithBakedCustom(oldRoot)
                    : TryReplaceScaledInstance(oldRoot);

                if (replacement != null)
                {
                    replaced++;
                }
            }

            return replaced;
        }

        public static int AuditScene(Scene scene)
        {
            int offenders = 0;
            foreach (GunItem item in UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (item == null || item.gameObject.scene != scene || item.GetComponent<NetworkObject>() == null)
                {
                    continue;
                }

                Vector3 scale = item.transform.localScale;
                bool isScaled = Mathf.Abs(scale.x - 1f) > EPSILON ||
                                Mathf.Abs(scale.y - 1f) > EPSILON ||
                                Mathf.Abs(scale.z - 1f) > EPSILON;
                bool hasAddedChildren = HasAddedSceneChildren(item.gameObject);
                if (!isScaled && !hasAddedChildren)
                {
                    continue;
                }

                offenders++;
                Debug.LogWarning(
                    $"[GunPropBakeUtility] Unbaked GunItem '{item.name}' at {item.transform.position}: " +
                    $"scale={scale}, addedChildren={hasAddedChildren}", item);
            }

            return offenders;
        }

        private static void EnsureCommonVariants()
        {
            foreach (VariantSpec spec in CommonVariants)
            {
                string path = BAKED_FOLDER + "/" + spec.VariantName + ".prefab";
                BakeScaledVariant(spec.SourcePath, path, spec.Scale);
            }
        }

        private static GameObject TryReplaceScaledInstance(GameObject oldRoot)
        {
            Vector3 scale = oldRoot.transform.localScale;
            if (Mathf.Abs(scale.x - 1f) <= EPSILON &&
                Mathf.Abs(scale.y - 1f) <= EPSILON &&
                Mathf.Abs(scale.z - 1f) <= EPSILON)
            {
                return null;
            }

            string sourcePath = GetSourcePrefabPath(oldRoot);
            if (string.IsNullOrEmpty(sourcePath) || IsBakedPath(sourcePath))
            {
                return null;
            }

            VariantSpec spec = CommonVariants.FirstOrDefault(candidate =>
                candidate.SourcePath == sourcePath && NearlyEqual(candidate.Scale, scale));
            string variantPath = spec != null
                ? BAKED_FOLDER + "/" + spec.VariantName + ".prefab"
                : BAKED_FOLDER + "/" + BuildGenericVariantName(sourcePath, scale) + ".prefab";

            if (spec == null)
            {
                BakeScaledVariant(sourcePath, variantPath, scale);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            return prefab != null ? ReplaceSceneObject(oldRoot, prefab) : null;
        }

        private static GameObject ReplaceWithBakedCustom(GameObject oldRoot)
        {
            string path = BAKED_FOLDER + "/" + BuildCustomVariantName(oldRoot) + ".prefab";
            Debug.Log($"[GunPropBakeUtility] Baking custom prop '{oldRoot.name}' to {path}.");
            BakeCustomVariant(oldRoot, path);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log($"[GunPropBakeUtility] Loaded custom prop prefab: {(prefab != null ? prefab.name : "null")}.");
            return prefab != null ? ReplaceSceneObject(oldRoot, prefab) : null;
        }

        private static GameObject ReplaceSceneObject(GameObject oldRoot, GameObject prefab)
        {
            Transform parent = oldRoot.transform.parent;
            Vector3 position = oldRoot.transform.position;
            Quaternion rotation = oldRoot.transform.rotation;
            string name = oldRoot.name;
            int siblingIndex = oldRoot.transform.GetSiblingIndex();

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = Vector3.one;
            instance.transform.SetSiblingIndex(siblingIndex);
            UnityEngine.Object.DestroyImmediate(oldRoot);
            return instance;
        }

        private static void BakeScaledVariant(string sourcePath, string destinationPath, Vector3 targetScale)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogError("[GunPropBakeUtility] Missing source prefab: " + sourcePath);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(sourcePath);
            Vector3 effectiveScale = Vector3.Scale(root.transform.localScale, targetScale);
            FlattenRootScale(root, effectiveScale);
            root.name = Path.GetFileNameWithoutExtension(destinationPath);
            AddSolidCollisionProxy(root);
            GunPropPhysicsAssets.ApplyForWeightClass(root);
            PrefabUtility.SaveAsPrefabAsset(root, destinationPath);
            PrefabUtility.UnloadPrefabContents(root);
            ClearInScenePlacedFlags(destinationPath);
        }

        private static void BakeCustomVariant(GameObject source, string destinationPath)
        {
            try
            {
                GameObject clone = UnityEngine.Object.Instantiate(source);
                clone.transform.SetParent(null);
                clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                if (PrefabUtility.IsPartOfPrefabInstance(clone))
                {
                    PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                Vector3 effectiveScale = clone.transform.localScale;
                FlattenRootScale(clone, effectiveScale);
                clone.name = Path.GetFileNameWithoutExtension(destinationPath);
                AddSolidCollisionProxy(clone);
                GunPropPhysicsAssets.ApplyForWeightClass(clone);
                PrefabUtility.SaveAsPrefabAsset(clone, destinationPath);
                UnityEngine.Object.DestroyImmediate(clone);
                ClearInScenePlacedFlags(destinationPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("[GunPropBakeUtility] Custom bake failed: " + exception);
            }
        }

        private static void FlattenRootScale(GameObject root, Vector3 scale)
        {
            root.transform.localScale = Vector3.one;
            if (scale == Vector3.one)
            {
                return;
            }

            foreach (Transform child in root.transform)
            {
                child.localPosition = Vector3.Scale(child.localPosition, scale);
                child.localScale = Vector3.Scale(child.localScale, scale);
            }

            foreach (Collider collider in root.GetComponents<Collider>())
            {
                if (collider is BoxCollider box)
                {
                    box.center = Vector3.Scale(box.center, scale);
                    box.size = Vector3.Scale(box.size, scale);
                }
                else if (collider is SphereCollider sphere)
                {
                    sphere.center = Vector3.Scale(sphere.center, scale);
                    sphere.radius *= MaxAbs(scale);
                }
                else if (collider is CapsuleCollider capsule)
                {
                    capsule.center = Vector3.Scale(capsule.center, scale);
                    capsule.radius *= MaxAbs(new Vector3(scale.x, 1f, scale.z));
                    capsule.height *= Mathf.Abs(scale.y);
                }
            }

            GunItem gunItem = root.GetComponent<GunItem>();
            if (gunItem != null)
            {
                SerializedObject serializedObject = new SerializedObject(gunItem);
                ScaleVectorProperty(serializedObject, "muzzleOffset", scale);
                ScaleVectorProperty(serializedObject, "pickupPositionOffset", scale);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ScaleVectorProperty(SerializedObject serializedObject, string propertyName, Vector3 scale)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = Vector3.Scale(property.vector3Value, scale);
            }
        }

        public static void AssignSceneProjectileHashes(Scene scene)
        {
            foreach (GunItem item in UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (item == null || item.gameObject.scene != scene)
                {
                    continue;
                }

                string prefabPath = GetSourcePrefabPath(item.gameObject);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                NetworkObject networkObject = prefab != null ? prefab.GetComponent<NetworkObject>() : null;
                if (networkObject == null || networkObject.PrefabIdHash == 0)
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(item);
                SerializedProperty hashProperty = serializedObject.FindProperty("projectilePrefabHash");
                if (hashProperty == null)
                {
                    continue;
                }

                hashProperty.longValue = networkObject.PrefabIdHash;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }
        }

        private static void ClearInScenePlacedFlags(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            NetworkObject networkObject = prefab != null ? prefab.GetComponent<NetworkObject>() : null;
            if (networkObject == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(networkObject);
            serializedObject.FindProperty("m_InScenePlaced").boolValue = false;
            serializedObject.FindProperty("SceneMigrationSynchronization").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
        }

        private static bool HasAddedSceneChildren(GameObject root)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(root))
            {
                return false;
            }

            try
            {
                return PrefabUtility.GetAddedGameObjects(root).Count > 0 ||
                       PrefabUtility.GetAddedComponents(root).Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetSourcePrefabPath(GameObject root)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(root))
            {
                return string.Empty;
            }

            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
        }

        private static bool IsBakedPath(string path)
        {
            return path.Replace("\\", "/").StartsWith(BAKED_FOLDER, StringComparison.Ordinal);
        }

        private static bool NearlyEqual(Vector3 left, Vector3 right)
        {
            return Mathf.Abs(left.x - right.x) <= EPSILON &&
                   Mathf.Abs(left.y - right.y) <= EPSILON &&
                   Mathf.Abs(left.z - right.z) <= EPSILON;
        }

        private static string BuildGenericVariantName(string sourcePath, Vector3 scale)
        {
            string sourceName = Path.GetFileNameWithoutExtension(sourcePath);
            return Sanitize(sourceName) + "_" + FormatScale(scale);
        }

        private static string BuildCustomVariantName(GameObject root)
        {
            Vector3 position = root.transform.position;
            return Sanitize(root.name) + "_" +
                   Mathf.RoundToInt(position.x * 100f) + "_" +
                   Mathf.RoundToInt(position.y * 100f) + "_" +
                   Mathf.RoundToInt(position.z * 100f);
        }

        private static string FormatScale(Vector3 scale)
        {
            return Mathf.RoundToInt(scale.x * 1000f) + "_" +
                   Mathf.RoundToInt(scale.y * 1000f) + "_" +
                   Mathf.RoundToInt(scale.z * 1000f);
        }

        private static string Sanitize(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        }

        private static float MaxAbs(Vector3 value)
        {
            return Mathf.Max(Mathf.Abs(value.x), Mathf.Max(Mathf.Abs(value.y), Mathf.Abs(value.z)));
        }

        private static void AddTallShelfGeometry(Transform root)
        {
            Vector3 size = new Vector3(9.78f, 3.62f, 1.01f);
            AddShelfPart(root, "Shelf Side Left", new Vector3(-size.x * 0.5f + 0.03f, 0f, 0f), new Vector3(0.06f, size.y, size.z));
            AddShelfPart(root, "Shelf Side Right", new Vector3(size.x * 0.5f - 0.03f, 0f, 0f), new Vector3(0.06f, size.y, size.z));
            AddShelfPart(root, "Shelf Top", new Vector3(0f, size.y * 0.5f - 0.03f, 0f), new Vector3(size.x, 0.06f, size.z));
            AddShelfPart(root, "Shelf Bottom", new Vector3(0f, -size.y * 0.5f + 0.03f, 0f), new Vector3(size.x, 0.06f, size.z));

            float[] boardHeights = { -1.56f, -0.98f, -0.33f, 1.23f };
            foreach (float localY in boardHeights)
            {
                AddShelfPart(root, "Shelf Board", new Vector3(0f, localY, 0f), new Vector3(size.x - 0.08f, 0.17f, size.z));
            }

            float legHeight = 0.78f - 0.14f;
            if (legHeight > 0.01f)
            {
                float legY = -size.y * 0.5f - legHeight * 0.5f;
                float legX = size.x * 0.5f - 0.05f;
                float legZ = size.z * 0.5f - 0.05f;
                AddShelfPart(root, "Shelf Leg", new Vector3(-legX, legY, -legZ), new Vector3(0.08f, legHeight, 0.08f));
                AddShelfPart(root, "Shelf Leg", new Vector3(legX, legY, -legZ), new Vector3(0.08f, legHeight, 0.08f));
                AddShelfPart(root, "Shelf Leg", new Vector3(-legX, legY, legZ), new Vector3(0.08f, legHeight, 0.08f));
                AddShelfPart(root, "Shelf Leg", new Vector3(legX, legY, legZ), new Vector3(0.08f, legHeight, 0.08f));
            }
        }

        private static void AddShelfPart(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<MeshRenderer>().sharedMaterial = BopBuyMaterials.Metal;
        }

        private static void AddSolidCollisionProxy(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            if (root.GetComponent<Collider>() != null)
            {
                MarkShelfHull(root);
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            BoxCollider proxy = root.AddComponent<BoxCollider>();
            proxy.center = root.transform.InverseTransformPoint(bounds.center);
            proxy.size = Vector3.Max(bounds.size, new Vector3(0.05f, 0.05f, 0.05f));
            proxy.isTrigger = false;
            MarkShelfHull(root);
        }

        private static void MarkShelfHull(GameObject root)
        {
            if (!HasShelfName(root.name))
            {
                return;
            }

            if (root.GetComponent<GunItemHullCollider>() == null)
            {
                root.AddComponent<GunItemHullCollider>();
            }
        }

        private static bool HasShelfName(string objectName)
        {
            return !string.IsNullOrEmpty(objectName) &&
                   objectName.IndexOf("Shelf", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void EnsureBakedFolder()
        {
            if (AssetDatabase.IsValidFolder(BAKED_FOLDER))
            {
                return;
            }

            string parent = Path.GetDirectoryName(BAKED_FOLDER).Replace("\\", "/");
            string name = Path.GetFileName(BAKED_FOLDER);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
