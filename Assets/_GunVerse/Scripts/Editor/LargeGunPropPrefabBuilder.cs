using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class LargeGunPropPrefabBuilder
    {
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";
        private const string SHELF_PREFAB_PATH = LARGE_PROPS_FOLDER + "/ShelfRunGun.prefab";
        private const string REGISTER_PREFAB_PATH = LARGE_PROPS_FOLDER + "/CashRegisterGun.prefab";
        private const string LEGACY_SHELF_PROJECTILE_PREFAB_PATH = LARGE_PROPS_FOLDER + "/ShelfRunProjectile.prefab";
        private const string SHELF_METAL_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShelfMetal.mat";
        private const string SHELF_BACK_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShelfBack.mat";

        private const float SHELF_WIDTH = 1.6f;
        private const float SHELF_LENGTH = 10f;
        private const float SHELF_HEIGHT = 2.2f;
        private const float SHELF_CENTER_Y = SHELF_HEIGHT * 0.5f;
        private const float END_PANEL_WIDTH = SHELF_WIDTH + 0.08f;
        private const float END_PANEL_HEIGHT = SHELF_HEIGHT + 0.04f;
        private const float END_PANEL_THICKNESS = 0.16f;
        private const float END_PANEL_FRONT_LOCAL_Z = -4.95f;
        private const float END_PANEL_BACK_LOCAL_Z = 4.95f;
        private const float TOP_CAP_HEIGHT = 0.06f;
        private const float SHELF_BASE_HEIGHT = 0.16f;
        private const float SHELF_BOARD_HEIGHT = 0.08f;
        private const float SHELF_BOARD_DEPTH = 10f;
        private const float SHELF_MUZZLE_LOCAL_Z = SHELF_LENGTH * 0.5f + 0.15f;

        private const float SHELF_HELD_SCALE = 0.12f;
        private const float SHELF_HELD_OFFSET_Y = -0.1f;
        private const float SHELF_HELD_OFFSET_Z = 0.55f;
        private const float SHELF_RIGIDBODY_MASS = 100f;
        private const int SHELF_MAX_ACTIVE_PROJECTILES = 8;
        private const float SHELF_TRAIL_WIDTH_SCALE = 0.15f;
        private const float SHELF_TRAIL_LENGTH_MULTIPLIER = 5f;
        private const float SHELF_PLAYER_PUSH_SPEED = 8f;
        private const float SHELF_PLAYER_PUSH_UP_SPEED = 4f;
        private const float SHELF_RELEASE_CONTENT_IMPULSE = 1f;
        private const float SHELF_RELEASE_PLAYER_LAUNCH_SPEED = 6f;
        private const float SHELF_RELEASE_PLAYER_LAUNCH_UP_SPEED = 3f;
        private const float SHELF_ROUNDS_PER_MINUTE = 40f;
        private const int SHELF_MAGAZINE_SIZE = 2;
        private const float SHELF_RELOAD_DURATION = 3f;
        private const float SHELF_DAMAGE = 60f;
        private const float SHELF_PROJECTILE_SPEED = 18f;
        private const float SHELF_PROJECTILE_LIFETIME = 4f;

        private const float REGISTER_WIDTH = 0.45f;
        private const float REGISTER_HEIGHT = 0.3f;
        private const float REGISTER_DEPTH = 0.4f;
        private const float REGISTER_MUZZLE_Z = 0.25f;
        private const float REGISTER_RIGIDBODY_MASS = 6f;
        private const int REGISTER_MAX_ACTIVE_PROJECTILES = 30;
        private const float REGISTER_TRAIL_WIDTH_SCALE = 0.4f;
        private const float REGISTER_TRAIL_LENGTH_MULTIPLIER = 15f;
        private const float REGISTER_PLAYER_PUSH_SPEED = 6f;
        private const float REGISTER_PLAYER_PUSH_UP_SPEED = 3f;
        private const float REGISTER_ROUNDS_PER_MINUTE = 90f;
        private const int REGISTER_MAGAZINE_SIZE = 4;
        private const float REGISTER_RELOAD_DURATION = 2.2f;
        private const float REGISTER_DAMAGE = 45f;
        private const float REGISTER_PROJECTILE_SPEED = 24f;
        private const float REGISTER_PROJECTILE_LIFETIME = 3f;

        private static readonly float[] shelfBoardHeights = { 0.38f, 0.92f, 1.46f, 2.0f };

        public static void Build()
        {
            EnsureFolder(LARGE_PROPS_FOLDER);
            AssetDatabase.DeleteAsset(LEGACY_SHELF_PROJECTILE_PREFAB_PATH);

            Material shelfMetal = LoadMaterial(SHELF_METAL_MATERIAL_PATH);
            Material shelfBack = LoadMaterial(SHELF_BACK_MATERIAL_PATH);

            BuildShelfPrefab(shelfMetal, shelfBack);
            BuildRegisterPrefab(shelfMetal);

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[LargeGunPropPrefabBuilder] Built the full-size shelf run and cash register projectile props.");
        }

        private static void BuildShelfPrefab(Material shelfMetal, Material shelfBack)
        {
            GameObject root = new GameObject("ShelfRunGun");
            AddNetworkComponents(root, SHELF_RIGIDBODY_MASS);

            CreateBoxPart(root.transform, "Base", new Vector3(0f, 0.08f - SHELF_CENTER_Y, 0f), new Vector3(SHELF_WIDTH, SHELF_BASE_HEIGHT, SHELF_LENGTH), shelfMetal);
            CreateBoxPart(root.transform, "Spine", new Vector3(0f, 0f, 0f), new Vector3(0.12f, SHELF_HEIGHT, SHELF_LENGTH), shelfMetal);
            CreateBoxPart(root.transform, "EndPanelFront", new Vector3(0f, 0f, END_PANEL_FRONT_LOCAL_Z), new Vector3(END_PANEL_WIDTH, END_PANEL_HEIGHT, END_PANEL_THICKNESS), shelfBack);
            CreateBoxPart(root.transform, "EndPanelBack", new Vector3(0f, 0f, END_PANEL_BACK_LOCAL_Z), new Vector3(END_PANEL_WIDTH, END_PANEL_HEIGHT, END_PANEL_THICKNESS), shelfBack);

            for (int i = 0; i < shelfBoardHeights.Length; i++)
            {
                float localY = shelfBoardHeights[i] - SHELF_CENTER_Y;
                CreateBoxPart(root.transform, "ShelfBoard" + (i + 1), new Vector3(0f, localY, 0f), new Vector3(SHELF_WIDTH, SHELF_BOARD_HEIGHT, SHELF_BOARD_DEPTH), shelfMetal);
            }

            CreateBoxPart(root.transform, "TopCap", new Vector3(0f, SHELF_HEIGHT + TOP_CAP_HEIGHT * 0.5f - SHELF_CENTER_Y, 0f), new Vector3(SHELF_WIDTH + TOP_CAP_HEIGHT, TOP_CAP_HEIGHT, SHELF_LENGTH + 0.08f), shelfMetal);

            EnsureShelfProjectileHull(root);

            ConfigureGunItem(
                root.GetComponent<GunItem>(),
                "Shelf Run",
                Vector3.one * SHELF_HELD_SCALE,
                new Vector3(0f, SHELF_HELD_OFFSET_Y, SHELF_HELD_OFFSET_Z),
                Vector3.zero,
                new Vector3(0f, 0f, SHELF_MUZZLE_LOCAL_Z),
                SHELF_ROUNDS_PER_MINUTE,
                SHELF_MAGAZINE_SIZE,
                SHELF_RELOAD_DURATION,
                SHELF_DAMAGE,
                GunWeightClass.Heavy,
                SHELF_PROJECTILE_SPEED,
                SHELF_PROJECTILE_LIFETIME,
                SHELF_RELEASE_CONTENT_IMPULSE,
                SHELF_RELEASE_PLAYER_LAUNCH_SPEED,
                SHELF_RELEASE_PLAYER_LAUNCH_UP_SPEED);

            ConfigureProjectileBody(
                root.GetComponent<GunProjectileBody>(),
                SHELF_RIGIDBODY_MASS,
                SHELF_MAX_ACTIVE_PROJECTILES,
                SHELF_PLAYER_PUSH_SPEED,
                SHELF_PLAYER_PUSH_UP_SPEED,
                SHELF_TRAIL_WIDTH_SCALE,
                SHELF_TRAIL_LENGTH_MULTIPLIER);

            GunPropPhysicsAssets.ApplyTo(root);
            SavePrefab(root, SHELF_PREFAB_PATH);
        }

        private static void BuildRegisterPrefab(Material material)
        {
            GameObject root = new GameObject("Cash Register");
            AddNetworkComponents(root, REGISTER_RIGIDBODY_MASS);

            BoxCollider rootCollider = root.AddComponent<BoxCollider>();
            rootCollider.size = new Vector3(REGISTER_WIDTH, REGISTER_HEIGHT, REGISTER_DEPTH);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(REGISTER_WIDTH, REGISTER_HEIGHT, REGISTER_DEPTH);
            visual.transform.localPosition = Vector3.zero;
            visual.isStatic = false;
            visual.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            PropLabelBuilder.AddFrontBackLabels(
                root.transform,
                "CashRegisterGun",
                "CashRegisterGun.png",
                Vector3.zero,
                0.38f,
                0.18f,
                REGISTER_DEPTH * 0.5f + 0.004f);

            ConfigureGunItem(
                root.GetComponent<GunItem>(),
                "Cash Register",
                Vector3.one,
                Vector3.zero,
                Vector3.zero,
                new Vector3(0f, 0f, REGISTER_MUZZLE_Z),
                REGISTER_ROUNDS_PER_MINUTE,
                REGISTER_MAGAZINE_SIZE,
                REGISTER_RELOAD_DURATION,
                REGISTER_DAMAGE,
                GunWeightClass.Heavy,
                REGISTER_PROJECTILE_SPEED,
                REGISTER_PROJECTILE_LIFETIME,
                0f,
                0f,
                0f);

            ConfigureProjectileBody(
                root.GetComponent<GunProjectileBody>(),
                REGISTER_RIGIDBODY_MASS,
                REGISTER_MAX_ACTIVE_PROJECTILES,
                REGISTER_PLAYER_PUSH_SPEED,
                REGISTER_PLAYER_PUSH_UP_SPEED,
                REGISTER_TRAIL_WIDTH_SCALE,
                REGISTER_TRAIL_LENGTH_MULTIPLIER);

            GunPropPhysicsAssets.ApplyTo(root);
            SavePrefab(root, REGISTER_PREFAB_PATH);
        }

        private static GameObject CreateBoxPart(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            box.isStatic = false;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static void EnsureShelfProjectileHull(GameObject root)
        {
            BoxCollider hullCollider = root.GetComponent<BoxCollider>();
            if (hullCollider == null)
            {
                hullCollider = root.AddComponent<BoxCollider>();

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int index = 1; index < renderers.Length; index++)
                    {
                        bounds.Encapsulate(renderers[index].bounds);
                    }

                    hullCollider.center = root.transform.InverseTransformPoint(bounds.center);
                    hullCollider.size = bounds.size;
                }
            }

            if (root.GetComponent<GunItemHullCollider>() == null)
            {
                root.AddComponent<GunItemHullCollider>();
            }
        }

        private static void AddNetworkComponents(GameObject root, float mass)
        {
            root.transform.localScale = Vector3.one;
            root.AddComponent<NetworkObject>();

            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.SwitchTransformSpaceWhenParented = true;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = mass;
            rigidbody.useGravity = true;
            rigidbody.isKinematic = true;

            root.AddComponent<GunItem>();
            root.AddComponent<GunProjectileBody>();
            GunPerformanceSettings.ApplyToGun(root);
        }

        private static void ConfigureGunItem(
            GunItem gunItem,
            string displayName,
            Vector3 pickupScale,
            Vector3 pickupPositionOffset,
            Vector3 pickupRotationEuler,
            Vector3 muzzleOffset,
            float roundsPerMinute,
            int magazineSize,
            float reloadDuration,
            float damage,
            GunWeightClass weightClass,
            float projectileSpeed,
            float projectileLifetime,
            float releaseContentImpulse,
            float releasePlayerLaunchSpeed,
            float releasePlayerLaunchUpSpeed)
        {
            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("pickupScale").vector3Value = pickupScale;
            serializedObject.FindProperty("pickupPositionOffset").vector3Value = pickupPositionOffset;
            serializedObject.FindProperty("pickupRotationEuler").vector3Value = pickupRotationEuler;
            serializedObject.FindProperty("muzzleOffset").vector3Value = muzzleOffset;
            serializedObject.FindProperty("roundsPerMinute").floatValue = roundsPerMinute;
            serializedObject.FindProperty("magazineSize").intValue = magazineSize;
            serializedObject.FindProperty("reloadDuration").floatValue = reloadDuration;
            serializedObject.FindProperty("damage").floatValue = damage;
            serializedObject.FindProperty("weightClass").enumValueIndex = (int)weightClass;
            serializedObject.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            serializedObject.FindProperty("projectileLifetime").floatValue = projectileLifetime;
            serializedObject.FindProperty("releaseContentImpulse").floatValue = releaseContentImpulse;
            serializedObject.FindProperty("releasePlayerLaunchSpeed").floatValue = releasePlayerLaunchSpeed;
            serializedObject.FindProperty("releasePlayerLaunchUpSpeed").floatValue = releasePlayerLaunchUpSpeed;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureProjectileBody(
            GunProjectileBody projectileBody,
            float mass,
            int maxActiveProjectiles,
            float playerPushSpeed,
            float playerPushUpSpeed,
            float trailWidthScale,
            float trailLengthMultiplier)
        {
            SerializedObject serializedObject = new SerializedObject(projectileBody);
            serializedObject.FindProperty("mass").floatValue = mass;
            serializedObject.FindProperty("maxActiveProjectiles").intValue = maxActiveProjectiles;
            serializedObject.FindProperty("playerPushSpeed").floatValue = playerPushSpeed;
            serializedObject.FindProperty("playerPushUpSpeed").floatValue = playerPushUpSpeed;
            serializedObject.FindProperty("trailWidthScale").floatValue = trailWidthScale;
            serializedObject.FindProperty("trailLengthMultiplier").floatValue = trailLengthMultiplier;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefabAsset);
        }

        private static Material LoadMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Debug.LogError("[LargeGunPropPrefabBuilder] Material not found: " + path);
            }

            return material;
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
