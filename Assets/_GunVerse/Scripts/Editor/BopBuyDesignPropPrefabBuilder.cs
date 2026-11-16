using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class BopBuyDesignPropPrefabBuilder
    {
        private const string ITEMS_FOLDER = "Assets/_GunVerse/Prefabs/Items";
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";
        private const string GLASS_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShopGlass.mat";

        [MenuItem("Tools/Gun Verse/Rebuild BopBuy Design Content")]
        public static void BuildAllDesignContent()
        {
            Build();
            ModelPickupPrefabBuilder.ApplyConfiguredModels();
            BopBuyDesignSignPrefabBuilder.Build();
            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[BopBuyDesignPropPrefabBuilder] Rebuilt all BopBuy design prefabs, models, and signs.");
        }

        [MenuItem("Tools/Gun Verse/Build BopBuy Design Prop Prefabs")]
        public static void Build()
        {
            BuildBaseModelPrefab("FridgeGun", "Refrigerator", LARGE_PROPS_FOLDER, new Vector3(1.47f, 2.68f, 0.97f), 140f);
            BuildBaseModelPrefab("DryerGun", "Clothes Dryer", LARGE_PROPS_FOLDER, new Vector3(0.65f, 0.88f, 0.65f), 70f);
            BuildBaseModelPrefab("CoffeeMachineGun", "Coffee Machine", ITEMS_FOLDER, new Vector3(0.32f, 0.42f, 0.34f), 4f);
            BuildBaseModelPrefab("DeskChairGun", "Desk Chair", ITEMS_FOLDER, new Vector3(0.62f, 0.95f, 0.62f), 7f);
            BuildBaseModelPrefab("PalletGun", "Pallet", ITEMS_FOLDER, new Vector3(1.2f, 0.15f, 1.0f), 12f);
            BuildBaseModelPrefab("TrashBinGun", "Trash Bin", ITEMS_FOLDER, new Vector3(0.42f, 0.62f, 0.42f), 3f);
            BuildBaseModelPrefab("BathroomSinkGun", "Bathroom Sink", ITEMS_FOLDER, new Vector3(0.55f, 0.85f, 0.45f), 18f);
            BuildBaseModelPrefab("ToiletGun", "Toilet", ITEMS_FOLDER, new Vector3(0.42f, 0.78f, 0.7f), 22f);
            BuildBaseModelPrefab("WhiteboardGun", "Whiteboard", LARGE_PROPS_FOLDER, new Vector3(1.81f, 1.41f, 0.08f), 14f);
            BuildBaseModelPrefab("CardboardBoxSmallGun", "Small Cardboard Box", ITEMS_FOLDER, new Vector3(0.45f, 0.35f, 0.4f), 2f);
            BuildBaseModelPrefab("CardboardBoxMediumGun", "Cardboard Box", ITEMS_FOLDER, new Vector3(0.7f, 0.55f, 0.55f), 4f);
            BuildBaseModelPrefab("CardboardBoxLargeGun", "Large Cardboard Box", ITEMS_FOLDER, new Vector3(1f, 0.75f, 0.7f), 7f);

            BuildVendingMachine();
            BuildPowerStrip();
            BuildTallOpenShelf();
            BuildDesignTable();
            BuildShelfUnit();

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[BopBuyDesignPropPrefabBuilder] Built design prop prefabs.");
        }

        private static void BuildBaseModelPrefab(string prefabName, string displayName, string folder, Vector3 colliderSize, float mass)
        {
            GameObject root = CreateRoot(prefabName, colliderSize, mass);
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = "ModelVisual";
            placeholder.transform.SetParent(root.transform, false);
            placeholder.transform.localScale = colliderSize;
            Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            placeholder.GetComponent<MeshRenderer>().sharedMaterial = BopBuyMaterials.GreyLocker;

            ConfigureGun(root.GetComponent<GunItem>(), displayName, colliderSize, mass);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), mass);
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void BuildVendingMachine()
        {
            Vector3 size = new Vector3(1f, 1.9f, 0.85f);
            GameObject root = CreateRoot("VendingMachineGun", size, 160f);
            Material glass = AssetDatabase.LoadAssetAtPath<Material>(GLASS_MATERIAL_PATH);
            AddBox(root.transform, "Body", new Vector3(0f, 0f, 0f), size, BopBuyMaterials.Blue);
            AddBox(root.transform, "Front", new Vector3(0f, 0.08f, 0.4f), new Vector3(0.88f, 1.45f, 0.06f), glass);
            AddBox(root.transform, "ButtonPanel", new Vector3(0.34f, 0.3f, 0.44f), new Vector3(0.2f, 0.55f, 0.04f), BopBuyMaterials.BlackWall);
            AddBox(root.transform, "Chute", new Vector3(0f, -0.78f, 0.44f), new Vector3(0.62f, 0.22f, 0.04f), BopBuyMaterials.Metal);
            for (int index = 0; index < 3; index++)
            {
                AddBox(root.transform, "ProductShelf" + index, new Vector3(0f, -0.25f + index * 0.46f, 0.28f), new Vector3(0.78f, 0.04f, 0.24f), BopBuyMaterials.Metal);
            }

            PropLabelBuilder.AddFrontBackLabels(root.transform, "VendingMachineGun", "VendingMachineGun.png", new Vector3(0f, 0.15f, 0f), 0.82f, 0.55f, size.z * 0.5f + 0.004f);
            ConfigureGun(root.GetComponent<GunItem>(), "Vending Machine", size, 160f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 160f);
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            PrefabUtility.SaveAsPrefabAsset(root, LARGE_PROPS_FOLDER + "/VendingMachineGun.prefab");
            Object.DestroyImmediate(root);
        }

        private static void BuildPowerStrip()
        {
            Vector3 size = new Vector3(0.4f, 0.06f, 0.12f);
            GameObject root = CreateRoot("PowerStripGun", size, 1.2f);
            AddBox(root.transform, "Strip", Vector3.zero, size, BopBuyMaterials.WhiteCounter);
            for (int index = 0; index < 4; index++)
            {
                AddBox(root.transform, "Socket" + index, new Vector3(-0.15f + index * 0.1f, 0.032f, 0f), new Vector3(0.06f, 0.01f, 0.06f), BopBuyMaterials.BlackWall);
            }

            PropLabelBuilder.AddTopLabel(root.transform, "PowerStripGun", "PowerStripGun.png", Vector3.zero, 0.32f, 0.08f, size.y * 0.5f + 0.004f);
            ConfigureGun(root.GetComponent<GunItem>(), "Power Strip", size, 1.2f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 1.2f);
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            PrefabUtility.SaveAsPrefabAsset(root, ITEMS_FOLDER + "/PowerStripGun.prefab");
            Object.DestroyImmediate(root);
        }

        private static void BuildTallOpenShelf()
        {
            Vector3 size = new Vector3(2.5f, 3.3f, 0.9f);
            GameObject root = CreateRoot("TallOpenShelfGun", size, 90f, true);
            AddBox(root.transform, "SideLeft", new Vector3(-1.22f, 0f, 0f), new Vector3(0.06f, 3.3f, 0.9f), BopBuyMaterials.GreyLocker);
            AddBox(root.transform, "SideRight", new Vector3(1.22f, 0f, 0f), new Vector3(0.06f, 3.3f, 0.9f), BopBuyMaterials.GreyLocker);
            AddBox(root.transform, "Top", new Vector3(0f, 1.64f, 0f), new Vector3(2.5f, 0.06f, 0.9f), BopBuyMaterials.GreyLocker);
            AddBox(root.transform, "Bottom", new Vector3(0f, -1.64f, 0f), new Vector3(2.5f, 0.06f, 0.9f), BopBuyMaterials.GreyLocker);
            for (int index = 0; index < 5; index++)
            {
                AddBox(root.transform, "Board" + index, new Vector3(0f, -1.1f + index * 0.55f, 0f), new Vector3(2.38f, 0.05f, 0.86f), BopBuyMaterials.Metal);
            }

            ConfigureGun(root.GetComponent<GunItem>(), "Tall Open Shelf", size, 90f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 90f);
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            PrefabUtility.SaveAsPrefabAsset(root, LARGE_PROPS_FOLDER + "/TallOpenShelfGun.prefab");
            Object.DestroyImmediate(root);
        }

        private static void BuildDesignTable()
        {
            Vector3 size = new Vector3(2.8f, 1f, 1.33f);
            GameObject root = CreateRoot("DesignTableGun", size, 30f);
            AddBox(root.transform, "TableTop", new Vector3(0f, 0.46f, 0f), new Vector3(2.8f, 0.08f, 1.33f), BopBuyMaterials.WhiteCounter);
            AddBox(root.transform, "TableSkirt", new Vector3(0f, 0.18f, 0f), new Vector3(2.6f, 0.46f, 1.15f), BopBuyMaterials.Blue);
            AddBox(root.transform, "LegFrontLeft", new Vector3(-1.2f, -0.04f, -0.5f), new Vector3(0.08f, 0.92f, 0.08f), BopBuyMaterials.Metal);
            AddBox(root.transform, "LegFrontRight", new Vector3(1.2f, -0.04f, -0.5f), new Vector3(0.08f, 0.92f, 0.08f), BopBuyMaterials.Metal);
            AddBox(root.transform, "LegBackLeft", new Vector3(-1.2f, -0.04f, 0.5f), new Vector3(0.08f, 0.92f, 0.08f), BopBuyMaterials.Metal);
            AddBox(root.transform, "LegBackRight", new Vector3(1.2f, -0.04f, 0.5f), new Vector3(0.08f, 0.92f, 0.08f), BopBuyMaterials.Metal);

            PropLabelBuilder.AddFrontBackLabels(root.transform, "DesignTableGun", "DisplayTableGun.png", new Vector3(0f, -0.02f, 0f), 2.05f, 0.52f, size.z * 0.5f + 0.004f);
            ConfigureGun(root.GetComponent<GunItem>(), "Display Table", size, 30f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 30f);
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            PrefabUtility.SaveAsPrefabAsset(root, ITEMS_FOLDER + "/DesignTableGun.prefab");
            Object.DestroyImmediate(root);
        }

        private static void BuildShelfUnit()
        {
            Vector3 colliderSize = new Vector3(2.5f, 3.3f, 0.9f);
            GameObject root = CreateRoot("ShelfUnitGun", colliderSize, 90f, true);
            ConfigureGun(root.GetComponent<GunItem>(), "Shelf Unit", colliderSize, 90f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 90f);
            GunPerformanceSettings.ApplyToGun(root);
            PrefabUtility.SaveAsPrefabAsset(root, LARGE_PROPS_FOLDER + "/ShelfUnitGun.prefab");
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateRoot(string name, Vector3 colliderSize, float mass, bool markColliderAsProjectileHull = false)
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

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = colliderSize;

            if (markColliderAsProjectileHull)
            {
                root.AddComponent<GunItemHullCollider>();
            }

            root.AddComponent<GunItem>();
            root.AddComponent<GunProjectileBody>();
            return root;
        }

        private static void ConfigureGun(GunItem gunItem, string displayName, Vector3 colliderSize, float mass)
        {
            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = displayName;
            float largestDimension = Mathf.Max(colliderSize.x, Mathf.Max(colliderSize.y, colliderSize.z));
            serializedObject.FindProperty("pickupScale").vector3Value = Vector3.one * Mathf.Clamp(0.35f / largestDimension, 0.1f, 0.9f);
            serializedObject.FindProperty("pickupPositionOffset").vector3Value = new Vector3(0f, -0.05f, 0.45f);
            serializedObject.FindProperty("muzzleOffset").vector3Value = new Vector3(0f, 0f, colliderSize.z * 0.5f + 0.08f);
            serializedObject.FindProperty("roundsPerMinute").floatValue = 60f;
            serializedObject.FindProperty("magazineSize").intValue = 6;
            serializedObject.FindProperty("reloadDuration").floatValue = 2.2f;
            serializedObject.FindProperty("damage").floatValue = 30f;
            serializedObject.FindProperty("weightClass").enumValueIndex = (int)GunWeightClass.Heavy;
            serializedObject.FindProperty("projectileSpeed").floatValue = 22f;
            serializedObject.FindProperty("projectileLifetime").floatValue = 3.5f;
            serializedObject.FindProperty("releaseContentImpulse").floatValue = 0.1f;
            serializedObject.FindProperty("releasePlayerLaunchSpeed").floatValue = 2f;
            serializedObject.FindProperty("releasePlayerLaunchUpSpeed").floatValue = 1f;
            serializedObject.FindProperty("projectilesPerShot").intValue = 1;
            serializedObject.FindProperty("burstCount").intValue = 1;
            serializedObject.FindProperty("burstInterval").floatValue = 0.07f;
            serializedObject.FindProperty("spreadAngle").floatValue = 0f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureProjectile(GunProjectileBody projectileBody, float mass)
        {
            SerializedObject serializedObject = new SerializedObject(projectileBody);
            serializedObject.FindProperty("mass").floatValue = Mathf.Max(0.1f, mass);
            serializedObject.FindProperty("maxActiveProjectiles").intValue = 6;
            serializedObject.FindProperty("playerPushSpeed").floatValue = 2f;
            serializedObject.FindProperty("playerPushUpSpeed").floatValue = 1f;
            serializedObject.FindProperty("trailWidthScale").floatValue = 0.3f;
            serializedObject.FindProperty("trailLengthMultiplier").floatValue = 6f;
            serializedObject.FindProperty("gravityScale").floatValue = 1f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddBox(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
