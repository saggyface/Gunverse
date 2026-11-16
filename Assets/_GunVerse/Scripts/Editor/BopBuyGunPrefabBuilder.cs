using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class BopBuyGunPrefabBuilder
    {
        private const string ITEMS_FOLDER = "Assets/_GunVerse/Prefabs/Items";
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";

        private const string TV_PREFAB_PATH = LARGE_PROPS_FOLDER + "/TVGun.prefab";
        private const string BATTERY_PREFAB_PATH = ITEMS_FOLDER + "/BatteryGun.prefab";
        private const string SPEAKER_PREFAB_PATH = ITEMS_FOLDER + "/SpeakerGun.prefab";
        private const string KEYBOARD_PREFAB_PATH = ITEMS_FOLDER + "/KeyboardGun.prefab";
        private const string LAPTOP_PREFAB_PATH = ITEMS_FOLDER + "/LaptopGun.prefab";
        private const string PHONE_PREFAB_PATH = ITEMS_FOLDER + "/PhoneGun.prefab";
        private const string PRINTER_PREFAB_PATH = ITEMS_FOLDER + "/PrinterGun.prefab";
        private const string MICROWAVE_PREFAB_PATH = LARGE_PROPS_FOLDER + "/MicrowaveGun.prefab";
        private const string GUYSQUAD_COUNTER_PREFAB_PATH = LARGE_PROPS_FOLDER + "/GuysquadCounterGun.prefab";
        private const string BOPBUY_SIGN_PREFAB_PATH = LARGE_PROPS_FOLDER + "/BopBuySignGun.prefab";

        public static void Build()
        {
            EnsureFolder(ITEMS_FOLDER);
            EnsureFolder(LARGE_PROPS_FOLDER);

            BuildTv();
            BuildBattery();
            BuildSpeaker();
            BuildKeyboard();
            BuildLaptop();
            BuildPhone();
            BuildPrinter();
            BuildMicrowave();
            BuildGuysquadCounter();
            BuildBopBuySign();

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[BopBuyGunPrefabBuilder] Built electronics guns and named BopBuy props.");
        }

        private static void BuildTv()
        {
            GameObject root = CreateRoot("TV", new Vector3(1.15f, 0.72f, 0.12f), 18f);
            ConfigureGun(
                root.GetComponent<GunItem>(),
                "TV",
                Vector3.one * 0.28f,
                new Vector3(0f, -0.05f, 0.55f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.1f),
                38f,
                2,
                3.2f,
                70f,
                GunWeightClass.Heavy,
                16f,
                4f,
                0.2f,
                4f,
                2f,
                1,
                1,
                0.07f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 18f, 8, 6f, 3f, 0.2f, 5f, 1f);
            FinishGunPrefab(root, TV_PREFAB_PATH);
        }

        private static void BuildBattery()
        {
            GameObject root = CreateRoot("Battery", new Vector3(0.06f, 0.18f, 0.06f), 0.25f);
            ConfigureGun(
                root.GetComponent<GunItem>(),
                "Battery",
                Vector3.one * 1.4f,
                new Vector3(0f, 0f, 0.25f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.08f),
                420f,
                20,
                1.2f,
                7f,
                GunWeightClass.Light,
                48f,
                2.5f,
                0f,
                1f,
                0.5f,
                1,
                1,
                0.07f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 0.25f, 30, 2f, 1f, 1f, 20f, 1f);
            GunPropPhysicsAssets.ApplyBouncyTo(root);
            FinishGunPrefab(root, BATTERY_PREFAB_PATH);
        }

        private static void BuildSpeaker()
        {
            GameObject root = CreateRoot("Speaker", new Vector3(0.35f, 0.55f, 0.3f), 3f);
            ConfigureGun(
                root.GetComponent<GunItem>(),
                "Speaker",
                Vector3.one * 0.7f,
                new Vector3(0f, -0.05f, 0.45f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.15f),
                110f,
                8,
                1.8f,
                24f,
                GunWeightClass.Medium,
                30f,
                3f,
                0.1f,
                3f,
                1.5f,
                1,
                1,
                0.07f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 3f, 20, 4f, 2f, 0.8f, 12f, 1f);
            GunPropPhysicsAssets.ApplyBouncyTo(root);
            FinishGunPrefab(root, SPEAKER_PREFAB_PATH);
        }

        private static void BuildKeyboard()
        {
            GameObject root = CreateRoot("Keyboard", new Vector3(0.45f, 0.05f, 0.16f), 1f);
            ConfigureGun(
                root.GetComponent<GunItem>(),
                "Keyboard",
                Vector3.one * 0.8f,
                new Vector3(0f, 0f, 0.35f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.1f),
                80f,
                10,
                1.8f,
                8f,
                GunWeightClass.Light,
                34f,
                2.5f,
                0f,
                1f,
                0.5f,
                5,
                1,
                0.07f,
                12f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 1f, 30, 2f, 1f, 1f, 15f, 1f);
            FinishGunPrefab(root, KEYBOARD_PREFAB_PATH);
        }

        private static void BuildLaptop()
        {
            GameObject root = CreateRoot("Laptop", new Vector3(0.38f, 0.06f, 0.26f), 3f);
            ConfigureGun(
                root.GetComponent<GunItem>(),
                "Laptop",
                Vector3.one * 0.8f,
                new Vector3(0f, 0f, 0.4f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.15f),
                70f,
                5,
                2.2f,
                42f,
                GunWeightClass.Medium,
                22f,
                3.5f,
                0f,
                2f,
                1f,
                1,
                1,
                0.07f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 3f, 12, 4f, 2f, 0.8f, 12f, 1.6f);
            FinishGunPrefab(root, LAPTOP_PREFAB_PATH);
        }

        private static void BuildPhone()
        {
            GameObject root = CreateRoot("Phone", new Vector3(0.08f, 0.16f, 0.012f), 0.35f);
            ConfigureGun(
                root.GetComponent<GunItem>(),
                "Phone",
                Vector3.one * 1.2f,
                new Vector3(0f, 0f, 0.25f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.03f),
                520f,
                30,
                1.4f,
                6f,
                GunWeightClass.Light,
                50f,
                2f,
                0f,
                1f,
                0.5f,
                1,
                1,
                0.07f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 0.35f, 30, 2f, 1f, 1f, 20f, 1f);
            FinishGunPrefab(root, PHONE_PREFAB_PATH);
        }

        private static void BuildPrinter()
        {
            GameObject root = CreateRoot("Printer", new Vector3(0.45f, 0.3f, 0.4f), 6f);
            ConfigureGun(
                root.GetComponent<GunItem>(),
                "Printer",
                Vector3.one * 0.5f,
                new Vector3(0f, -0.05f, 0.45f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.22f),
                90f,
                9,
                2.4f,
                16f,
                GunWeightClass.Medium,
                30f,
                3f,
                0.1f,
                3f,
                2f,
                1,
                3,
                0.08f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 6f, 18, 3f, 2f, 0.7f, 12f, 1f);
            FinishGunPrefab(root, PRINTER_PREFAB_PATH);
        }

        private static void BuildMicrowave()
        {
            GameObject root = CreateRoot("Microwave", new Vector3(0.55f, 0.35f, 0.4f), 12f);
            ConfigureGun(
                root.GetComponent<GunItem>(),
                "Microwave",
                Vector3.one * 0.35f,
                new Vector3(0f, -0.05f, 0.5f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.22f),
                50f,
                3,
                2.8f,
                60f,
                GunWeightClass.Heavy,
                18f,
                4f,
                0.2f,
                5f,
                3f,
                1,
                1,
                0.07f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 12f, 10, 5f, 3f, 0.3f, 6f, 1f);
            FinishGunPrefab(root, MICROWAVE_PREFAB_PATH);
        }

        private static void BuildGuysquadCounter()
        {
            GameObject root = CreateRoot("Guysquad Counter", new Vector3(1.1f, 1.16f, 3.2f), 90f);
            AddBoxPart(root.transform, "CounterBody", Vector3.zero, new Vector3(1.1f, 1.16f, 3.2f), BopBuyMaterials.Counter);
            AddBoxPart(root.transform, "CounterTop", new Vector3(0f, 0.595f, 0f), new Vector3(1.18f, 0.03f, 3.24f), BopBuyMaterials.Metal);
            AddBoxPart(root.transform, "CounterAccent", new Vector3(0f, 0.42f, 0f), new Vector3(1.12f, 0.12f, 3.22f), BopBuyMaterials.Yellow);

            PropLabelBuilder.AddSideLabels(
                root.transform,
                "GuysquadCounter",
                "GuysquadCounterGun.png",
                new Vector3(0f, 0.18f, 0f),
                1.7f,
                0.48f,
                0.57f);

            ConfigureGun(
                root.GetComponent<GunItem>(),
                "Guysquad Counter",
                Vector3.one * 0.2f,
                new Vector3(0f, -0.05f, 0.65f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.2f),
                45f,
                2,
                2.8f,
                55f,
                GunWeightClass.Heavy,
                20f,
                3f,
                0.2f,
                5f,
                3f,
                1,
                1,
                0.07f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 90f, 8, 6f, 3f, 0.2f, 5f, 1f);
            FinishGunPrefab(root, GUYSQUAD_COUNTER_PREFAB_PATH);
        }

        private static void BuildBopBuySign()
        {
            GameObject root = CreateRoot("BopBuy Sign", new Vector3(3.2f, 1.2f, 0.12f), 35f);
            AddBoxPart(root.transform, "SignPanel", Vector3.zero, new Vector3(3.2f, 1.2f, 0.12f), BopBuyMaterials.Blue);
            AddBoxPart(root.transform, "SignTrimTop", new Vector3(0f, 0.62f, 0f), new Vector3(3.24f, 0.06f, 0.16f), BopBuyMaterials.Yellow);
            AddBoxPart(root.transform, "SignTrimBottom", new Vector3(0f, -0.62f, 0f), new Vector3(3.24f, 0.06f, 0.16f), BopBuyMaterials.Yellow);

            PropLabelBuilder.AddFrontBackLabels(
                root.transform,
                "BopBuySign",
                "BopBuySignGun.png",
                Vector3.zero,
                2.8f,
                0.95f,
                0.07f);

            ConfigureGun(
                root.GetComponent<GunItem>(),
                "BopBuy Sign",
                Vector3.one * 0.15f,
                new Vector3(0f, 0f, 0.5f),
                Vector3.zero,
                new Vector3(0f, 0f, 0.08f),
                40f,
                2,
                3f,
                70f,
                GunWeightClass.Heavy,
                16f,
                4f,
                0.3f,
                6f,
                3f,
                1,
                1,
                0.07f,
                0f);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), 35f, 4, 6f, 3f, 0.2f, 5f, 1f);
            FinishGunPrefab(root, BOPBUY_SIGN_PREFAB_PATH, true);
        }

        private static GameObject CreateRoot(string name, Vector3 colliderSize, float mass)
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
            collider.center = Vector3.zero;
            collider.size = colliderSize;

            root.AddComponent<GunItem>();
            root.AddComponent<GunProjectileBody>();
            return root;
        }

        private static void ConfigureGun(
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
            float releasePlayerLaunchUpSpeed,
            int projectilesPerShot,
            int burstCount,
            float burstInterval,
            float spreadAngle)
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
            serializedObject.FindProperty("projectilesPerShot").intValue = Mathf.Max(1, projectilesPerShot);
            serializedObject.FindProperty("burstCount").intValue = Mathf.Max(1, burstCount);
            serializedObject.FindProperty("burstInterval").floatValue = Mathf.Max(0.01f, burstInterval);
            serializedObject.FindProperty("spreadAngle").floatValue = Mathf.Max(0f, spreadAngle);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureProjectile(
            GunProjectileBody projectileBody,
            float mass,
            int maxActiveProjectiles,
            float playerPushSpeed,
            float playerPushUpSpeed,
            float trailWidthScale,
            float trailLengthMultiplier,
            float gravityScale)
        {
            SerializedObject serializedObject = new SerializedObject(projectileBody);
            serializedObject.FindProperty("mass").floatValue = mass;
            serializedObject.FindProperty("maxActiveProjectiles").intValue = Mathf.Max(1, maxActiveProjectiles);
            serializedObject.FindProperty("playerPushSpeed").floatValue = Mathf.Max(0f, playerPushSpeed);
            serializedObject.FindProperty("playerPushUpSpeed").floatValue = Mathf.Max(0f, playerPushUpSpeed);
            serializedObject.FindProperty("trailWidthScale").floatValue = Mathf.Max(0.01f, trailWidthScale);
            serializedObject.FindProperty("trailLengthMultiplier").floatValue = Mathf.Max(0.1f, trailLengthMultiplier);
            serializedObject.FindProperty("gravityScale").floatValue = Mathf.Max(0f, gravityScale);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddBoxPart(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;

            Collider collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            MeshRenderer renderer = box.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void FinishGunPrefab(GameObject root, string path, bool alwaysShowLabel = false)
        {
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, alwaysShowLabel);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
