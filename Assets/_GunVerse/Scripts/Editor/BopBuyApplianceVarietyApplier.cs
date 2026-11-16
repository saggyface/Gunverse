using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class BopBuyApplianceVarietyApplier
    {
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string PROPS_ROOT_NAME = "BopBuyProps";
        private const string TOP_ITEM_NAME_PREFIX = "Appliance Top ";
        private const string NAME_PROPERTY = "m_Name";
        private const string PROJECTILE_PREFAB_HASH_PROPERTY = "projectilePrefabHash";
        private const string IN_SCENE_PLACED_PROPERTY = "m_InScenePlaced";
        private const string GLOBAL_OBJECT_ID_HASH_PROPERTY = "GlobalObjectIdHash";
        private const string IN_SCENE_PLACED_SOURCE_HASH_PROPERTY = "InScenePlacedSourceGlobalObjectIdHash";
        private const string SCENE_MIGRATION_SYNCHRONIZATION_PROPERTY = "SceneMigrationSynchronization";
        private const string TICK_SYNC_CHILDREN_PROPERTY = "TickSyncChildren";
        private const float TOP_ITEM_CHANCE = 0.35f;
        private const int MAX_TOP_ITEMS = 18;
        private const float TOP_ITEM_GAP = 0.01f;
        private const float TOP_ITEM_VOLUME_SCALE = 0.95f;
        private const float MIN_OVERLAP_EXTENT = 0.01f;
        private const float ROTATION_TOLERANCE = 0.5f;
        private const float YAW_JITTER_DEGREES = 5f;
        private const uint YAW_JITTER_SALT = 0x27D4EB2Fu;
        private const uint TOP_ITEM_SALT = 0x165667B1u;

        private static readonly string[] transformPropertyPrefixes =
        {
            "m_LocalPosition.",
            "m_LocalRotation.",
            "m_LocalScale.",
            "m_LocalEulerAnglesHint."
        };

        private static readonly HashSet<string> transformPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "m_RootOrder",
            "m_ConstrainProportionsScale"
        };

        private static readonly HashSet<string> networkObjectPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            IN_SCENE_PLACED_PROPERTY,
            GLOBAL_OBJECT_ID_HASH_PROPERTY,
            IN_SCENE_PLACED_SOURCE_HASH_PROPERTY,
            SCENE_MIGRATION_SYNCHRONIZATION_PROPERTY
        };

        private static readonly HashSet<string> networkTransformPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            TICK_SYNC_CHILDREN_PROPERTY
        };

        [MenuItem("Tools/Gun Verse/Apply BopBuy Appliance Variety (Preserve Scene)")]
        public static void ApplyToBopBuyScene()
        {
            if (!BopBuyApplianceVarietyCatalog.HasAllVariantPrefabs())
            {
                Debug.LogError("[BopBuyApplianceVarietyApplier] Variant prefabs are missing. Run Tools/Gun Verse/Build BopBuy Appliance Variants first.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(BOPBUY_SCENE_PATH);
            bool openedByTool = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(BOPBUY_SCENE_PATH, OpenSceneMode.Additive);
                openedByTool = true;
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[BopBuyApplianceVarietyApplier] Could not open " + BOPBUY_SCENE_PATH + ".");
                return;
            }

            try
            {
                int skippedCount = 0;
                int replacedCount = 0;

                List<GunItem> candidates = FindBaseApplianceInstances(scene);
                foreach (GunItem candidate in candidates)
                {
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (HasManualNonTransformModifications(candidate.gameObject))
                    {
                        skippedCount++;
                        continue;
                    }

                    string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate.gameObject);
                    if (!BopBuyApplianceVarietyCatalog.TryGetCategory(sourcePath, out ApplianceCategory category))
                    {
                        skippedCount++;
                        continue;
                    }

                    uint stableHash = BopBuyApplianceVarietyCatalog.ComputeStableHash(candidate.name, candidate.transform.position);
                    string variantPath = BopBuyApplianceVarietyCatalog.ChooseVariantPrefab(category, stableHash);
                    if (string.Equals(variantPath, BopBuyApplianceVarietyCatalog.GetBasePrefabPath(category), StringComparison.Ordinal))
                    {
                        continue;
                    }

                    GameObject variantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                    if (variantPrefab == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    float yawJitter = GetYawJitter(candidate.gameObject, category, stableHash);
                    if (ReplaceInstance(candidate.gameObject, variantPrefab, scene, yawJitter) != null)
                    {
                        replacedCount++;
                    }
                }

                Physics.SyncTransforms();
                int topItemCount = AddTopItems(scene);

                GunItemPrefabRegistry.Rebuild();
                GunPropBakeUtility.AssignSceneProjectileHashes(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    $"[BopBuyApplianceVarietyApplier] Replaced {replacedCount} appliance variants, " +
                    $"skipped {skippedCount} manually modified instances, added {topItemCount} top items.");
            }
            finally
            {
                if (openedByTool)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static List<GunItem> FindBaseApplianceInstances(Scene scene)
        {
            List<GunItem> result = new List<GunItem>();
            foreach (GunItem item in UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (item == null || item.gameObject.scene != scene)
                {
                    continue;
                }

                if (!PrefabUtility.IsPartOfPrefabInstance(item.gameObject))
                {
                    continue;
                }

                string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item.gameObject);
                if (BopBuyApplianceVarietyCatalog.IsBaseAppliancePath(sourcePath))
                {
                    result.Add(item);
                }
            }

            result.Sort(CompareInstances);
            return result;
        }

        private static int CompareInstances(GunItem left, GunItem right)
        {
            int nameComparison = string.CompareOrdinal(left.name, right.name);
            if (nameComparison != 0)
            {
                return nameComparison;
            }

            int xComparison = left.transform.position.x.CompareTo(right.transform.position.x);
            if (xComparison != 0)
            {
                return xComparison;
            }

            return left.transform.position.z.CompareTo(right.transform.position.z);
        }

        private static bool HasManualNonTransformModifications(GameObject instance)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(instance))
            {
                return true;
            }

            if (HasStructuralModifications(instance))
            {
                return true;
            }

            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instance);
            if (modifications == null)
            {
                return false;
            }

            GameObject sourceRoot = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            foreach (PropertyModification modification in modifications)
            {
                if (!IsAllowedModification(sourceRoot, modification))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasStructuralModifications(GameObject instance)
        {
            try
            {
                return PrefabUtility.GetAddedGameObjects(instance).Count > 0 ||
                       PrefabUtility.GetRemovedGameObjects(instance).Count > 0 ||
                       PrefabUtility.GetAddedComponents(instance).Count > 0 ||
                       PrefabUtility.GetRemovedComponents(instance).Count > 0;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static bool IsAllowedModification(GameObject sourceRoot, PropertyModification modification)
        {
            if (modification == null || string.IsNullOrEmpty(modification.propertyPath))
            {
                return false;
            }

            string propertyPath = modification.propertyPath;
            if (string.Equals(propertyPath, NAME_PROPERTY, StringComparison.Ordinal))
            {
                return modification.target == sourceRoot;
            }

            if (string.Equals(propertyPath, PROJECTILE_PREFAB_HASH_PROPERTY, StringComparison.Ordinal))
            {
                return IsRootComponent(sourceRoot, modification.target, typeof(GunItem));
            }

            if (IsTransformProperty(propertyPath))
            {
                return IsRootComponent(sourceRoot, modification.target, typeof(Transform));
            }

            if (networkObjectPropertyNames.Contains(propertyPath))
            {
                return IsRootComponent(sourceRoot, modification.target, typeof(NetworkObject));
            }

            if (networkTransformPropertyNames.Contains(propertyPath))
            {
                return IsRootComponent(sourceRoot, modification.target, typeof(NetworkTransform));
            }

            return false;
        }

        private static bool IsTransformProperty(string propertyPath)
        {
            int separatorIndex = propertyPath.LastIndexOf('/');
            string propertyName = separatorIndex >= 0 ? propertyPath.Substring(separatorIndex + 1) : propertyPath;
            foreach (string prefix in transformPropertyPrefixes)
            {
                if (propertyName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return transformPropertyNames.Contains(propertyName);
        }

        private static bool IsRootComponent(GameObject sourceRoot, UnityEngine.Object target, Type componentType)
        {
            if (sourceRoot == null || target == null)
            {
                return false;
            }

            Component component = target as Component;
            return component != null &&
                   component.gameObject == sourceRoot &&
                   componentType.IsInstanceOfType(component);
        }

        private static float GetYawJitter(GameObject instance, ApplianceCategory category, uint stableHash)
        {
            if (!BopBuyApplianceVarietyCatalog.SupportsYawJitter(category))
            {
                return 0f;
            }

            Vector3 eulerAngles = instance.transform.eulerAngles;
            float defaultYaw = BopBuyApplianceVarietyCatalog.GetBuilderDefaultYaw(category);
            bool isUpright = Mathf.Abs(Mathf.DeltaAngle(eulerAngles.x, 0f)) <= ROTATION_TOLERANCE &&
                             Mathf.Abs(Mathf.DeltaAngle(eulerAngles.z, 0f)) <= ROTATION_TOLERANCE;
            bool isDefaultYaw = Mathf.Abs(Mathf.DeltaAngle(eulerAngles.y, defaultYaw)) <= ROTATION_TOLERANCE;
            if (!isUpright || !isDefaultYaw)
            {
                return 0f;
            }

            return BopBuyApplianceVarietyCatalog.StableRange(
                stableHash ^ YAW_JITTER_SALT,
                -YAW_JITTER_DEGREES,
                YAW_JITTER_DEGREES);
        }

        private static GameObject ReplaceInstance(GameObject oldRoot, GameObject variantPrefab, Scene scene, float yawJitter)
        {
            Transform parent = oldRoot.transform.parent;
            Vector3 worldPosition = oldRoot.transform.position;
            Quaternion worldRotation = oldRoot.transform.rotation;
            Vector3 localScale = oldRoot.transform.localScale;
            string objectName = oldRoot.name;
            int siblingIndex = oldRoot.transform.GetSiblingIndex();

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(variantPrefab, parent);
            if (instance == null)
            {
                return null;
            }

            if (parent == null)
            {
                SceneManager.MoveGameObjectToScene(instance, scene);
            }

            CopyNetworkState(oldRoot, instance, GetPrefabHash(variantPrefab));
            instance.name = objectName;

            Quaternion targetRotation = Quaternion.AngleAxis(yawJitter, Vector3.up) * worldRotation;
            instance.transform.SetPositionAndRotation(worldPosition, targetRotation);
            instance.transform.localScale = localScale;
            CopyConstrainProportionsScale(oldRoot.transform, instance.transform);
            instance.transform.SetSiblingIndex(siblingIndex);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            UnityEngine.Object.DestroyImmediate(oldRoot);
            return instance;
        }

        private static void CopyConstrainProportionsScale(Transform source, Transform destination)
        {
            SerializedObject sourceSerialized = new SerializedObject(source);
            SerializedObject destinationSerialized = new SerializedObject(destination);
            SerializedProperty sourceProperty = sourceSerialized.FindProperty("m_ConstrainProportionsScale");
            SerializedProperty destinationProperty = destinationSerialized.FindProperty("m_ConstrainProportionsScale");
            if (sourceProperty == null || destinationProperty == null)
            {
                return;
            }

            destinationProperty.boolValue = sourceProperty.boolValue;
            destinationSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static uint GetPrefabHash(GameObject prefab)
        {
            NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
            return networkObject != null ? networkObject.PrefabIdHash : 0u;
        }

        private static void CopyNetworkState(GameObject oldRoot, GameObject newRoot, uint variantPrefabHash)
        {
            NetworkObject oldNetworkObject = oldRoot.GetComponent<NetworkObject>();
            NetworkObject newNetworkObject = newRoot.GetComponent<NetworkObject>();
            if (oldNetworkObject != null && newNetworkObject != null)
            {
                SerializedObject oldSerialized = new SerializedObject(oldNetworkObject);
                SerializedObject newSerialized = new SerializedObject(newNetworkObject);
                if (oldNetworkObject.InScenePlaced)
                {
                    CopyBoolProperty(oldSerialized, newSerialized, IN_SCENE_PLACED_PROPERTY);
                    CopyUIntProperty(oldSerialized, newSerialized, GLOBAL_OBJECT_ID_HASH_PROPERTY);

                    SerializedProperty sourceHash = newSerialized.FindProperty(IN_SCENE_PLACED_SOURCE_HASH_PROPERTY);
                    if (sourceHash != null && variantPrefabHash != 0)
                    {
                        sourceHash.longValue = variantPrefabHash;
                    }
                }

                CopyBoolProperty(oldSerialized, newSerialized, SCENE_MIGRATION_SYNCHRONIZATION_PROPERTY);
                newSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            NetworkTransform oldNetworkTransform = oldRoot.GetComponent<NetworkTransform>();
            NetworkTransform newNetworkTransform = newRoot.GetComponent<NetworkTransform>();
            if (oldNetworkTransform != null && newNetworkTransform != null)
            {
                SerializedObject oldSerialized = new SerializedObject(oldNetworkTransform);
                SerializedObject newSerialized = new SerializedObject(newNetworkTransform);
                CopyBoolProperty(oldSerialized, newSerialized, TICK_SYNC_CHILDREN_PROPERTY);
                newSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CopyBoolProperty(SerializedObject source, SerializedObject destination, string propertyName)
        {
            SerializedProperty sourceProperty = source.FindProperty(propertyName);
            SerializedProperty destinationProperty = destination.FindProperty(propertyName);
            if (sourceProperty != null && destinationProperty != null)
            {
                destinationProperty.boolValue = sourceProperty.boolValue;
            }
        }

        private static void CopyUIntProperty(SerializedObject source, SerializedObject destination, string propertyName)
        {
            SerializedProperty sourceProperty = source.FindProperty(propertyName);
            SerializedProperty destinationProperty = destination.FindProperty(propertyName);
            if (sourceProperty != null && destinationProperty != null && sourceProperty.longValue != 0)
            {
                destinationProperty.longValue = sourceProperty.longValue;
            }
        }

        private static int AddTopItems(Scene scene)
        {
            Transform propsRoot = FindPropsRoot(scene);
            if (propsRoot == null)
            {
                Debug.LogWarning("[BopBuyApplianceVarietyApplier] BopBuyProps root was not found; top items will use host parents.");
            }

            int existingTopItems = CountExistingTopItems(scene);
            int remaining = Mathf.Max(0, MAX_TOP_ITEMS - existingTopItems);
            if (remaining == 0)
            {
                return 0;
            }

            List<GunItem> hosts = FindTopItemHosts(scene);
            int addedCount = 0;
            foreach (GunItem host in hosts)
            {
                if (host == null || HasManualNonTransformModifications(host.gameObject))
                {
                    continue;
                }

                uint stableHash = BopBuyApplianceVarietyCatalog.ComputeStableHash(host.name, host.transform.position) ^ TOP_ITEM_SALT;
                if (!BopBuyApplianceVarietyCatalog.StableChance(stableHash, TOP_ITEM_CHANCE))
                {
                    continue;
                }

                string prefabPath = BopBuyApplianceVarietyCatalog.ChooseTopItemPrefab(stableHash);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                if (TryCreateTopItem(host.gameObject, prefab, prefabPath, stableHash, propsRoot) != null)
                {
                    addedCount++;
                    if (addedCount >= remaining)
                    {
                        break;
                    }
                }
            }

            return addedCount;
        }

        private static List<GunItem> FindTopItemHosts(Scene scene)
        {
            List<GunItem> hosts = new List<GunItem>();
            foreach (GunItem item in UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (item == null || item.gameObject.scene != scene)
                {
                    continue;
                }

                if (!PrefabUtility.IsPartOfPrefabInstance(item.gameObject))
                {
                    continue;
                }

                string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item.gameObject);
                if (!BopBuyApplianceVarietyCatalog.TryGetCategory(sourcePath, out ApplianceCategory category))
                {
                    continue;
                }

                if (BopBuyApplianceVarietyCatalog.IsTopItemHostCategory(category))
                {
                    hosts.Add(item);
                }
            }

            hosts.Sort(CompareInstances);
            return hosts;
        }

        private static GameObject TryCreateTopItem(
            GameObject host,
            GameObject prefab,
            string prefabPath,
            uint stableHash,
            Transform propsRoot)
        {
            Transform parent = propsRoot != null ? propsRoot : host.transform.parent;
            GameObject topItem = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (topItem == null)
            {
                return null;
            }

            topItem.name = TOP_ITEM_NAME_PREFIX + BopBuyApplianceVarietyCatalog.GetTopItemDisplayName(prefabPath);
            float yaw = BopBuyApplianceVarietyCatalog.ChooseTopItemYaw(stableHash);
            topItem.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Bounds hostBounds = GetRendererBounds(host);
            Bounds topBounds = GetRendererBounds(topItem);
            Vector3 offset = hostBounds.center - topBounds.center;
            offset.y = hostBounds.max.y - topBounds.min.y + TOP_ITEM_GAP;
            topItem.transform.position += offset;

            Bounds placedBounds = GetRendererBounds(topItem);
            if (IsTopVolumeBlocked(placedBounds, host, topItem))
            {
                UnityEngine.Object.DestroyImmediate(topItem);
                return null;
            }

            return topItem;
        }

        private static bool IsTopVolumeBlocked(Bounds bounds, GameObject host, GameObject candidate)
        {
            Physics.SyncTransforms();
            Vector3 halfExtents = bounds.extents * TOP_ITEM_VOLUME_SCALE;
            halfExtents = Vector3.Max(halfExtents, Vector3.one * MIN_OVERLAP_EXTENT);
            Collider[] overlaps = Physics.OverlapBox(
                bounds.center,
                halfExtents,
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (overlap == null)
                {
                    continue;
                }

                if (overlap.transform == host.transform || overlap.transform.IsChildOf(host.transform))
                {
                    continue;
                }

                if (overlap.transform == candidate.transform || overlap.transform.IsChildOf(candidate.transform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static int CountExistingTopItems(Scene scene)
        {
            int count = 0;
            foreach (GunItem item in UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (item == null || item.gameObject.scene != scene)
                {
                    continue;
                }

                if (!item.name.StartsWith(TOP_ITEM_NAME_PREFIX, StringComparison.Ordinal) ||
                    !PrefabUtility.IsPartOfPrefabInstance(item.gameObject))
                {
                    continue;
                }

                string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item.gameObject);
                if (BopBuyApplianceVarietyCatalog.IsTopItemPath(sourcePath))
                {
                    count++;
                }
            }

            return count;
        }

        private static Transform FindPropsRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, PROPS_ROOT_NAME, StringComparison.Ordinal))
                {
                    return root.transform;
                }
            }

            return null;
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one * MIN_OVERLAP_EXTENT);
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }
    }
}
