using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class GunItemPrefabBuilder
    {
        private const string MATERIALS_FOLDER = "Assets/_GunVerse/Materials";
        private const string ITEMS_FOLDER = "Assets/_GunVerse/Prefabs/Items";
        private const string SOUP_CAN_MATERIAL_PATH = "Assets/_GunVerse/Materials/SoupCan.mat";
        private const string CEREAL_BOX_MATERIAL_PATH = "Assets/_GunVerse/Materials/CerealBox.mat";
        private const string SOUP_CAN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/SoupCanGun.prefab";
        private const string CEREAL_BOX_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/CerealBoxGun.prefab";
        private const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";
        private const int DEFAULT_MAX_ACTIVE_PROJECTILES = 30;
        private const float SOUP_CAN_MASS = 0.4f;
        private const float CEREAL_BOX_MASS = 0.6f;
        private const float DEFAULT_TRAIL_WIDTH_SCALE = 1f;
        private const float DEFAULT_TRAIL_LENGTH_MULTIPLIER = 20f;
        private const float DEFAULT_PLAYER_PUSH_SPEED = 4f;
        private const float DEFAULT_PLAYER_PUSH_UP_SPEED = 2f;

        private static readonly Color soupCanColor = new Color(0.85f, 0.25f, 0.15f, 1f);
        private static readonly Color cerealBoxColor = new Color(0.95f, 0.80f, 0.20f, 1f);

        public static void Build()
        {
            EnsureFolder(MATERIALS_FOLDER);
            EnsureFolder(ITEMS_FOLDER);

            Material soupMaterial = GetOrCreateMaterial(SOUP_CAN_MATERIAL_PATH, soupCanColor);
            Material cerealMaterial = GetOrCreateMaterial(CEREAL_BOX_MATERIAL_PATH, cerealBoxColor);

            BuildSoupCanGun(soupMaterial);
            BuildCerealBoxGun(cerealMaterial);

            ModelPickupPrefabBuilder.ApplyConfiguredModels();
            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[GunItemPrefabBuilder] Built example gun prefabs.");
        }

        private static void BuildSoupCanGun(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "SoupCanGun";
            root.transform.localScale = new Vector3(0.13f, 0.05f, 0.13f);

            root.GetComponent<MeshRenderer>().sharedMaterial = material;

            GunItem gunItem = root.AddComponent<GunItem>();
            GunProjectileBody projectileBody = root.AddComponent<GunProjectileBody>();
            ConfigureNetworkTransform(root);
            ConfigureRigidbody(root, SOUP_CAN_MASS);

            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = "Soup Can";
            serializedObject.FindProperty("pickupScale").vector3Value = new Vector3(0.13f, 0.05f, 0.13f);
            serializedObject.FindProperty("muzzleOffset").vector3Value = new Vector3(0f, 0f, 0.08f);
            serializedObject.FindProperty("roundsPerMinute").floatValue = 280f;
            serializedObject.FindProperty("magazineSize").intValue = 8;
            serializedObject.FindProperty("reloadDuration").floatValue = 1.2f;
            serializedObject.FindProperty("damage").floatValue = 15f;
            serializedObject.FindProperty("weightClass").enumValueIndex = (int)GunWeightClass.Light;
            serializedObject.FindProperty("projectileSpeed").floatValue = 32f;
            serializedObject.FindProperty("projectileLifetime").floatValue = 3f;
            serializedObject.ApplyModifiedProperties();

            ConfigureProjectileBody(projectileBody, SOUP_CAN_MASS);
            GunPropPhysicsAssets.ApplyTo(root);
            SavePrefab(root, SOUP_CAN_PREFAB_PATH);
        }

        private static void BuildCerealBoxGun(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "CerealBoxGun";
            root.transform.localScale = new Vector3(0.2f, 0.3f, 0.08f);

            root.GetComponent<MeshRenderer>().sharedMaterial = material;

            GunItem gunItem = root.AddComponent<GunItem>();
            GunProjectileBody projectileBody = root.AddComponent<GunProjectileBody>();
            ConfigureNetworkTransform(root);
            ConfigureRigidbody(root, CEREAL_BOX_MASS);

            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = "Cereal Box";
            serializedObject.FindProperty("pickupScale").vector3Value = new Vector3(0.2f, 0.3f, 0.08f);
            serializedObject.FindProperty("muzzleOffset").vector3Value = new Vector3(0f, 0f, 0.12f);
            serializedObject.FindProperty("roundsPerMinute").floatValue = 600f;
            serializedObject.FindProperty("magazineSize").intValue = 30;
            serializedObject.FindProperty("reloadDuration").floatValue = 2f;
            serializedObject.FindProperty("damage").floatValue = 8f;
            serializedObject.FindProperty("weightClass").enumValueIndex = (int)GunWeightClass.Medium;
            serializedObject.FindProperty("projectileSpeed").floatValue = 40f;
            serializedObject.FindProperty("projectileLifetime").floatValue = 4f;
            serializedObject.ApplyModifiedProperties();

            ConfigureProjectileBody(projectileBody, CEREAL_BOX_MASS);
            GunPropPhysicsAssets.ApplyTo(root);
            SavePrefab(root, CEREAL_BOX_PREFAB_PATH);
        }

        private static void SavePrefab(GameObject root, string prefabPath)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            ClearInScenePlacedFlags(prefabPath);
        }

        private static void ClearInScenePlacedFlags(string prefabPath)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            NetworkObject networkObject = prefabAsset != null ? prefabAsset.GetComponent<NetworkObject>() : null;
            if (networkObject == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(networkObject);
            serializedObject.FindProperty("m_InScenePlaced").boolValue = false;
            serializedObject.FindProperty("SceneMigrationSynchronization").boolValue = true;
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(prefabAsset);
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                string name = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void ConfigureNetworkTransform(GameObject root)
        {
            NetworkTransform networkTransform = root.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
                networkTransform.SwitchTransformSpaceWhenParented = true;
            }
        }

        private static void ConfigureRigidbody(GameObject root, float mass)
        {
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = mass;
            rigidbody.useGravity = true;
            rigidbody.isKinematic = true;
            GunPerformanceSettings.ApplyToGun(root);
        }

        private static void ConfigureProjectileBody(GunProjectileBody projectileBody, float mass)
        {
            SerializedObject serializedObject = new SerializedObject(projectileBody);
            serializedObject.FindProperty("mass").floatValue = mass;
            serializedObject.FindProperty("maxActiveProjectiles").intValue = DEFAULT_MAX_ACTIVE_PROJECTILES;
            serializedObject.FindProperty("playerPushSpeed").floatValue = DEFAULT_PLAYER_PUSH_SPEED;
            serializedObject.FindProperty("playerPushUpSpeed").floatValue = DEFAULT_PLAYER_PUSH_UP_SPEED;
            serializedObject.FindProperty("trailWidthScale").floatValue = DEFAULT_TRAIL_WIDTH_SCALE;
            serializedObject.FindProperty("trailLengthMultiplier").floatValue = DEFAULT_TRAIL_LENGTH_MULTIPLIER;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material GetOrCreateMaterial(string assetPath, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(URP_LIT_SHADER);
            if (shader == null)
            {
                Debug.LogError("[GunItemPrefabBuilder] URP/Lit shader not found.");
                return null;
            }

            material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }
    }
}
