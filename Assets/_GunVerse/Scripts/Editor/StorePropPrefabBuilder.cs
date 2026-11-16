using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class StorePropPrefabBuilder
    {
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";
        private const string ITEMS_FOLDER = "Assets/_GunVerse/Prefabs/Items";
        private const string COUNTER_PREFAB_PATH = LARGE_PROPS_FOLDER + "/CheckoutCounterGun.prefab";
        private const string BELT_PREFAB_PATH = LARGE_PROPS_FOLDER + "/ConveyorBeltGun.prefab";
        private const string LANE_MARKER_PREFAB_PATH = ITEMS_FOLDER + "/LaneMarkerGun.prefab";
        private const string LIGHT_PREFAB_PATH = LARGE_PROPS_FOLDER + "/CeilingLightGun.prefab";
        private const string MAT_PREFAB_PATH = LARGE_PROPS_FOLDER + "/EntranceMatGun.prefab";

        private const string CHECKOUT_MATERIAL_PATH = "Assets/_GunVerse/Materials/Checkout.mat";
        private const string CHECKOUT_TOP_MATERIAL_PATH = "Assets/_GunVerse/Materials/CheckoutTop.mat";
        private const string SHELF_METAL_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShelfMetal.mat";
        private const string LIGHT_PANEL_MATERIAL_PATH = "Assets/_GunVerse/Materials/LightPanel.mat";

        private const float COUNTER_WIDTH = 1.1f;
        private const float COUNTER_HEIGHT = 1.16f;
        private const float COUNTER_LENGTH = 3.2f;
        private const float COUNTER_TOP_HEIGHT = 0.03f;
        private const float COUNTER_TOP_OVERHANG = 0.04f;
        private const float COUNTER_TOP_LOCAL_Y = COUNTER_HEIGHT * 0.5f + COUNTER_TOP_HEIGHT * 0.5f;

        private const float BELT_WIDTH = 0.55f;
        private const float BELT_HEIGHT = 0.03f;
        private const float BELT_LENGTH = 1.7f;

        private const float LANE_WIDTH = 0.03f;
        private const float LANE_HEIGHT = 0.12f;
        private const float LANE_LENGTH = 0.55f;

        private const float LIGHT_WIDTH = 0.5f;
        private const float LIGHT_HEIGHT = 0.08f;
        private const float LIGHT_LENGTH = 3.4f;

        private const float MAT_WIDTH = 2f;
        private const float MAT_HEIGHT = 0.02f;
        private const float MAT_LENGTH = 4.6f;

        public static void Build()
        {
            EnsureFolder(LARGE_PROPS_FOLDER);
            EnsureFolder(ITEMS_FOLDER);

            Material checkout = LoadMaterial(CHECKOUT_MATERIAL_PATH);
            Material checkoutTop = LoadMaterial(CHECKOUT_TOP_MATERIAL_PATH);
            Material shelfMetal = LoadMaterial(SHELF_METAL_MATERIAL_PATH);
            Material lightPanel = LoadMaterial(LIGHT_PANEL_MATERIAL_PATH);

            BuildCounterPrefab(checkout, checkoutTop);
            BuildBeltPrefab(shelfMetal);
            BuildLaneMarkerPrefab(shelfMetal);
            BuildLightPrefab(lightPanel);
            BuildMatPrefab(checkout);

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[StorePropPrefabBuilder] Built the checkout counter, belt, lane marker, ceiling light, and entrance mat prefabs.");
        }

        private static void BuildCounterPrefab(Material bodyMaterial, Material topMaterial)
        {
            GameObject root = CreateRoot("Checkout Counter", 90f);
            AddRootCollider(root, new Vector3(COUNTER_WIDTH, COUNTER_HEIGHT, COUNTER_LENGTH));
            AddBoxPart(root.transform, "CounterBody", Vector3.zero, new Vector3(COUNTER_WIDTH, COUNTER_HEIGHT, COUNTER_LENGTH), bodyMaterial, false);
            AddBoxPart(
                root.transform,
                "CounterTop",
                new Vector3(0f, COUNTER_TOP_LOCAL_Y, 0f),
                new Vector3(COUNTER_WIDTH + COUNTER_TOP_OVERHANG, COUNTER_TOP_HEIGHT, COUNTER_LENGTH + COUNTER_TOP_OVERHANG),
                topMaterial,
                true);

            PropLabelBuilder.AddSideLabels(
                root.transform,
                "CheckoutCounterGun",
                "CheckoutCounterGun.png",
                Vector3.zero,
                1.8f,
                0.8f,
                COUNTER_WIDTH * 0.5f + 0.004f);

            ConfigureGunItem(
                root.GetComponent<GunItem>(),
                "Checkout Counter",
                Vector3.one * 0.2f,
                new Vector3(0f, -0.05f, 0.65f),
                Vector3.zero,
                new Vector3(0f, 0f, COUNTER_LENGTH * 0.5f + 0.15f),
                12f,
                1,
                4f,
                70f,
                GunWeightClass.Heavy,
                14f,
                5f,
                1f,
                5f,
                2.5f);

            ConfigureProjectileBody(root.GetComponent<GunProjectileBody>(), 90f, 6, 7f, 3f, 0.2f, 5f);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            SavePrefab(root, COUNTER_PREFAB_PATH);
        }

        private static void BuildBeltPrefab(Material material)
        {
            GameObject root = CreateRoot("Conveyor Belt", 4f);
            AddRootCollider(root, new Vector3(BELT_WIDTH, BELT_HEIGHT, BELT_LENGTH));
            AddBoxPart(root.transform, "Belt", Vector3.zero, new Vector3(BELT_WIDTH, BELT_HEIGHT, BELT_LENGTH), material, false);

            PropLabelBuilder.AddTopLabel(
                root.transform,
                "ConveyorBeltGun",
                "ConveyorBeltGun.png",
                Vector3.zero,
                0.4f,
                0.2f,
                BELT_HEIGHT * 0.5f + 0.004f);

            ConfigureGunItem(
                root.GetComponent<GunItem>(),
                "Conveyor Belt",
                Vector3.one * 0.45f,
                new Vector3(0f, 0.05f, 0.45f),
                Vector3.zero,
                new Vector3(0f, 0f, BELT_LENGTH * 0.5f + 0.15f),
                60f,
                6,
                2f,
                20f,
                GunWeightClass.Medium,
                28f,
                3f,
                0f,
                0f,
                0f);

            ConfigureProjectileBody(root.GetComponent<GunProjectileBody>(), 4f, 20, 3f, 1.5f, 0.4f, 15f);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            SavePrefab(root, BELT_PREFAB_PATH);
        }

        private static void BuildLaneMarkerPrefab(Material material)
        {
            GameObject root = CreateRoot("Lane Marker", 0.3f);
            AddRootCollider(root, new Vector3(LANE_WIDTH, LANE_HEIGHT, LANE_LENGTH));
            AddBoxPart(root.transform, "Marker", Vector3.zero, new Vector3(LANE_WIDTH, LANE_HEIGHT, LANE_LENGTH), material, false);

            PropLabelBuilder.AddSideLabels(
                root.transform,
                "LaneMarkerGun",
                "LaneMarkerGun.png",
                Vector3.zero,
                0.3f,
                0.12f,
                LANE_WIDTH * 0.5f + 0.004f);

            ConfigureGunItem(
                root.GetComponent<GunItem>(),
                "Lane Marker",
                Vector3.one,
                new Vector3(0f, -0.05f, 0.4f),
                Vector3.zero,
                new Vector3(0f, 0f, LANE_LENGTH * 0.5f + 0.05f),
                300f,
                12,
                1f,
                10f,
                GunWeightClass.Light,
                34f,
                2.5f,
                0f,
                0f,
                0f);

            ConfigureProjectileBody(root.GetComponent<GunProjectileBody>(), 0.3f, 30, 2f, 1f, 1f, 20f);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            SavePrefab(root, LANE_MARKER_PREFAB_PATH);
        }

        private static void BuildLightPrefab(Material material)
        {
            GameObject root = CreateRoot("Ceiling Light", 10f);
            AddRootCollider(root, new Vector3(LIGHT_WIDTH, LIGHT_HEIGHT, LIGHT_LENGTH));
            AddBoxPart(root.transform, "Fixture", Vector3.zero, new Vector3(LIGHT_WIDTH, LIGHT_HEIGHT, LIGHT_LENGTH), material, false);

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.93f, 0.78f);
            light.intensity = 10f;
            light.range = 18f;
            light.shadows = LightShadows.None;

            PropLabelBuilder.AddTopAndBottomLabels(
                root.transform,
                "CeilingLightGun",
                "CeilingLightGun.png",
                Vector3.zero,
                0.4f,
                0.2f,
                LIGHT_HEIGHT * 0.5f + 0.004f);

            ConfigureGunItem(
                root.GetComponent<GunItem>(),
                "Ceiling Light",
                Vector3.one * 0.25f,
                new Vector3(0f, -0.1f, 0.55f),
                Vector3.zero,
                new Vector3(0f, 0f, LIGHT_LENGTH * 0.5f + 0.15f),
                50f,
                3,
                2.8f,
                35f,
                GunWeightClass.Heavy,
                20f,
                4f,
                0f,
                0f,
                0f);

            ConfigureProjectileBody(root.GetComponent<GunProjectileBody>(), 10f, 6, 4f, 2f, 0.2f, 5f);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            SavePrefab(root, LIGHT_PREFAB_PATH);
        }

        private static void BuildMatPrefab(Material material)
        {
            GameObject root = CreateRoot("Entrance Mat", 2f);
            AddRootCollider(root, new Vector3(MAT_WIDTH, MAT_HEIGHT, MAT_LENGTH));
            AddBoxPart(root.transform, "Mat", Vector3.zero, new Vector3(MAT_WIDTH, MAT_HEIGHT, MAT_LENGTH), material, false);

            PropLabelBuilder.AddTopLabel(
                root.transform,
                "EntranceMatGun",
                "EntranceMatGun.png",
                Vector3.zero,
                1.6f,
                0.8f,
                MAT_HEIGHT * 0.5f + 0.004f);

            ConfigureGunItem(
                root.GetComponent<GunItem>(),
                "Entrance Mat",
                Vector3.one * 0.3f,
                new Vector3(0f, 0.05f, 0.6f),
                Vector3.zero,
                new Vector3(0f, 0f, MAT_LENGTH * 0.5f + 0.1f),
                120f,
                8,
                1.6f,
                12f,
                GunWeightClass.Light,
                30f,
                3f,
                0f,
                0f,
                0f);

            ConfigureProjectileBody(root.GetComponent<GunProjectileBody>(), 2f, 10, 2f, 1f, 0.3f, 10f);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, true);
            SavePrefab(root, MAT_PREFAB_PATH);
        }

        private static GameObject CreateRoot(string name, float mass)
        {
            GameObject root = new GameObject(name);
            root.transform.localScale = Vector3.one;

            root.AddComponent<NetworkObject>();
            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.SwitchTransformSpaceWhenParented = true;

            Rigidbody rigidBody = root.AddComponent<Rigidbody>();
            rigidBody.mass = mass;
            rigidBody.useGravity = true;
            rigidBody.isKinematic = true;

            root.AddComponent<GunItem>();
            root.AddComponent<GunProjectileBody>();
            GunPerformanceSettings.ApplyToGun(root);
            return root;
        }

        private static void AddRootCollider(GameObject root, Vector3 size)
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void AddBoxPart(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, bool addCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            box.isStatic = false;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (!addCollider)
            {
                Object.DestroyImmediate(box.GetComponent<Collider>());
            }
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
            GunPropPhysicsAssets.ApplyTo(root);
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
                Debug.LogError("[StorePropPrefabBuilder] Material not found: " + path);
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
