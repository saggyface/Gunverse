using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class BopBuyFixturePrefabBuilder
    {
        private const string ITEMS_FOLDER = "Assets/_GunVerse/Prefabs/Items";
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";
        private const string GLASS_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShopGlass.mat";

        public static void Build()
        {
            BuildDisplayTable();
            BuildPlinth();
            BuildDisplayLedge();
            BuildGreeterTable();
            BuildPodium();
            BuildStool();
            BuildStanchion();
            BuildShoppingCart();
            BuildCartCorral();
            BuildEasPedestal();
            BuildBeverageCooler();
            BuildLocker();
            BuildSecurityCamera();
            BuildDomeCamera();
            BuildDoorbell();
            BuildDeskFan();
            BuildSecurityMonitor();
            BuildHighTable();
            BuildWorkbench();
            BuildPhoneCase();
            BuildWallet();
            BuildScreenProtector();

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[BopBuyFixturePrefabBuilder] Built generated gun-able fixtures and holiday props.");
        }

        private static void BuildDisplayTable()
        {
            BuildFixture(
                "DisplayTableGun",
                "Display Table",
                new Vector3(3f, 0.9f, 1.3f),
                35f,
                "DisplayTableGun",
                root =>
                {
                    AddBox(root, "TableTop", new Vector3(0f, 0.42f, 0f), new Vector3(3f, 0.12f, 1.3f), BopBuyMaterials.Counter);
                    AddBox(root, "TableSkirt", new Vector3(0f, 0.2f, 0f), new Vector3(2.9f, 0.4f, 1.2f), BopBuyMaterials.Blue);
                    AddLeg(root, new Vector3(-1.25f, 0.2f, -0.48f), 0.8f);
                    AddLeg(root, new Vector3(1.25f, 0.2f, -0.48f), 0.8f);
                    AddLeg(root, new Vector3(-1.25f, 0.2f, 0.48f), 0.8f);
                    AddLeg(root, new Vector3(1.25f, 0.2f, 0.48f), 0.8f);
                },
                new Vector3(0f, 0f, 0.66f),
                1.8f,
                0.5f);
        }

        private static void BuildPlinth()
        {
            BuildFixture(
                "PlinthGun",
                "Display Plinth",
                new Vector3(1.45f, 0.5f, 1.2f),
                40f,
                "PlinthGun",
                root => AddBox(root, "PlinthBody", new Vector3(0f, 0f, 0f), new Vector3(1.45f, 0.5f, 1.2f), BopBuyMaterials.Blue),
                new Vector3(0f, 0f, 0.61f),
                1.1f,
                0.28f);
        }

        private static void BuildDisplayLedge()
        {
            BuildFixture(
                "DisplayLedgeGun",
                "Display Ledge",
                new Vector3(3.4f, 0.08f, 0.55f),
                18f,
                "DisplayLedgeGun",
                root => AddBox(root, "Ledge", Vector3.zero, new Vector3(3.4f, 0.08f, 0.55f), BopBuyMaterials.Metal),
                new Vector3(0f, 0f, 0.28f),
                2.2f,
                0.06f);
        }

        private static void BuildGreeterTable()
        {
            BuildFixture(
                "GreeterTableGun",
                "Greeter Table",
                new Vector3(2.4f, 0.9f, 1.2f),
                30f,
                "GreeterTableGun",
                root =>
                {
                    AddBox(root, "GreeterTop", new Vector3(0f, 0.43f, 0f), new Vector3(2.4f, 0.08f, 1.2f), BopBuyMaterials.WhiteCounter);
                    AddBox(root, "GreeterDrape", new Vector3(0f, 0.16f, 0f), new Vector3(2.3f, 0.48f, 1.1f), BopBuyMaterials.BlueFabric);
                    AddLeg(root, new Vector3(-1f, 0.2f, -0.45f), 0.8f);
                    AddLeg(root, new Vector3(1f, 0.2f, -0.45f), 0.8f);
                    AddLeg(root, new Vector3(-1f, 0.2f, 0.45f), 0.8f);
                    AddLeg(root, new Vector3(1f, 0.2f, 0.45f), 0.8f);
                },
                new Vector3(0f, 0f, 0.61f),
                1.5f,
                0.45f);
        }

        private static void BuildPodium()
        {
            BuildFixture(
                "PodiumGun",
                "Greeter Podium",
                new Vector3(0.8f, 1.1f, 0.6f),
                25f,
                "PodiumGun",
                root =>
                {
                    AddBox(root, "PodiumBody", new Vector3(0f, -0.05f, 0f), new Vector3(0.8f, 1.05f, 0.6f), BopBuyMaterials.WhiteCounter);
                    AddBox(root, "PodiumTop", new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.06f, 0.7f), BopBuyMaterials.Metal);
                },
                new Vector3(0f, 0f, 0.31f),
                0.6f,
                0.5f);
        }

        private static void BuildStool()
        {
            BuildFixture(
                "StoolGun",
                "Stool",
                new Vector3(0.4f, 0.75f, 0.4f),
                6f,
                "StoolGun",
                root =>
                {
                    AddBox(root, "Seat", new Vector3(0f, 0.34f, 0f), new Vector3(0.4f, 0.08f, 0.4f), BopBuyMaterials.WoodTable);
                    AddLeg(root, new Vector3(-0.14f, 0.15f, -0.14f), 0.65f);
                    AddLeg(root, new Vector3(0.14f, 0.15f, -0.14f), 0.65f);
                    AddLeg(root, new Vector3(-0.14f, 0.15f, 0.14f), 0.65f);
                    AddLeg(root, new Vector3(0.14f, 0.15f, 0.14f), 0.65f);
                },
                new Vector3(0f, 0f, 0.21f),
                0.3f,
                0.2f);
        }

        private static void BuildStanchion()
        {
            BuildFixture(
                "StanchionGun",
                "Stanchion",
                new Vector3(0.35f, 0.95f, 0.35f),
                5f,
                "StanchionGun",
                root =>
                {
                    AddBox(root, "StanchionBase", new Vector3(0f, -0.42f, 0f), new Vector3(0.35f, 0.08f, 0.35f), BopBuyMaterials.Metal);
                    AddBox(root, "StanchionPost", new Vector3(0f, 0f, 0f), new Vector3(0.06f, 0.85f, 0.06f), BopBuyMaterials.BlackWall);
                    AddBox(root, "StanchionTop", new Vector3(0f, 0.44f, 0f), new Vector3(0.16f, 0.08f, 0.16f), BopBuyMaterials.Chrome);
                },
                new Vector3(0f, 0f, 0.18f),
                0.24f,
                0.2f);
        }

        private static void BuildShoppingCart()
        {
            BuildFixture(
                "ShoppingCartGun",
                "Shopping Cart",
                new Vector3(0.8f, 0.9f, 0.6f),
                12f,
                "ShoppingCartGun",
                root =>
                {
                    AddBox(root, "CartBasket", new Vector3(0f, 0.12f, 0f), new Vector3(0.8f, 0.45f, 0.6f), BopBuyMaterials.Blue);
                    AddBox(root, "CartHandle", new Vector3(0f, 0.39f, -0.32f), new Vector3(0.72f, 0.06f, 0.06f), BopBuyMaterials.Chrome);
                    AddLeg(root, new Vector3(-0.3f, -0.28f, -0.2f), 0.32f);
                    AddLeg(root, new Vector3(0.3f, -0.28f, -0.2f), 0.32f);
                    AddLeg(root, new Vector3(-0.3f, -0.28f, 0.2f), 0.32f);
                    AddLeg(root, new Vector3(0.3f, -0.28f, 0.2f), 0.32f);
                },
                new Vector3(0f, 0f, 0.32f),
                0.55f,
                0.3f);
        }

        private static void BuildCartCorral()
        {
            BuildFixture(
                "CartCorralGun",
                "Cart Corral",
                new Vector3(2.5f, 1.1f, 0.8f),
                20f,
                "CartCorralGun",
                root =>
                {
                    AddBox(root, "CorralRailLeft", new Vector3(-1.2f, 0f, 0f), new Vector3(0.08f, 1.05f, 0.8f), BopBuyMaterials.Metal);
                    AddBox(root, "CorralRailRight", new Vector3(1.2f, 0f, 0f), new Vector3(0.08f, 1.05f, 0.8f), BopBuyMaterials.Metal);
                    AddBox(root, "CorralRailBack", new Vector3(0f, -0.48f, -0.36f), new Vector3(2.4f, 0.08f, 0.08f), BopBuyMaterials.Metal);
                },
                new Vector3(0f, 0f, 0.41f),
                1.5f,
                0.5f);
        }

        private static void BuildEasPedestal()
        {
            BuildFixture(
                "EasPedestalGun",
                "Security Pedestal",
                new Vector3(0.3f, 1.6f, 0.2f),
                8f,
                "EasPedestalGun",
                root =>
                {
                    AddBox(root, "EasPost", Vector3.zero, new Vector3(0.3f, 1.6f, 0.2f), BopBuyMaterials.WhiteCounter);
                    AddBox(root, "EasTop", new Vector3(0f, 0.8f, 0f), new Vector3(0.36f, 0.08f, 0.24f), BopBuyMaterials.Blue);
                },
                new Vector3(0f, 0f, 0.11f),
                0.2f,
                0.8f);
        }

        private static void BuildBeverageCooler()
        {
            BuildFixture(
                "BeverageCoolerGun",
                "Beverage Cooler",
                new Vector3(0.8f, 1.9f, 0.7f),
                60f,
                "BeverageCoolerGun",
                root =>
                {
                    AddBox(root, "CoolerFrame", Vector3.zero, new Vector3(0.8f, 1.9f, 0.7f), BopBuyMaterials.RedAccent);
                    AddBox(root, "CoolerGlass", new Vector3(0f, 0f, 0.36f), new Vector3(0.65f, 1.7f, 0.02f), LoadGlass());
                    AddBox(root, "CoolerShelf1", new Vector3(0f, 0.3f, 0.2f), new Vector3(0.66f, 0.04f, 0.28f), BopBuyMaterials.Metal);
                    AddBox(root, "CoolerShelf2", new Vector3(0f, -0.3f, 0.2f), new Vector3(0.66f, 0.04f, 0.28f), BopBuyMaterials.Metal);
                },
                new Vector3(0f, 0f, 0.36f),
                0.55f,
                0.65f);
        }

        private static void BuildLocker()
        {
            BuildFixture(
                "LockerGun",
                "Storage Locker",
                new Vector3(1f, 1.8f, 0.6f),
                50f,
                "LockerGun",
                root =>
                {
                    AddBox(root, "LockerBody", Vector3.zero, new Vector3(1f, 1.8f, 0.6f), BopBuyMaterials.GreyLocker);
                    AddBox(root, "LockerDoorLeft", new Vector3(-0.24f, 0f, 0.31f), new Vector3(0.46f, 1.6f, 0.04f), BopBuyMaterials.Metal);
                    AddBox(root, "LockerDoorRight", new Vector3(0.24f, 0f, 0.31f), new Vector3(0.46f, 1.6f, 0.04f), BopBuyMaterials.Metal);
                    AddBox(root, "LockerHandleLeft", new Vector3(-0.05f, 0f, 0.35f), new Vector3(0.04f, 0.24f, 0.04f), BopBuyMaterials.Chrome);
                    AddBox(root, "LockerHandleRight", new Vector3(0.05f, 0f, 0.35f), new Vector3(0.04f, 0.24f, 0.04f), BopBuyMaterials.Chrome);
                },
                new Vector3(0f, 0f, 0.31f),
                0.7f,
                0.55f);
        }

        private static void BuildHolidayTree()
        {
            BuildFixture(
                "HolidayTreeGun",
                "Holiday Tree",
                new Vector3(1.4f, 2.4f, 1.4f),
                20f,
                "HolidayTreeGun",
                root =>
                {
                    AddCylinder(root, "TreeTier1", new Vector3(0f, -0.78f, 0f), new Vector3(1.4f, 0.45f, 1.4f), BopBuyMaterials.YellowTree);
                    AddCylinder(root, "TreeTier2", new Vector3(0f, -0.18f, 0f), new Vector3(1.1f, 0.42f, 1.1f), BopBuyMaterials.YellowTree);
                    AddCylinder(root, "TreeTier3", new Vector3(0f, 0.38f, 0f), new Vector3(0.8f, 0.38f, 0.8f), BopBuyMaterials.YellowTree);
                    AddCylinder(root, "TreeTier4", new Vector3(0f, 0.88f, 0f), new Vector3(0.5f, 0.32f, 0.5f), BopBuyMaterials.YellowTree);
                    AddBox(root, "TreeTrunk", new Vector3(0f, -1.08f, 0f), new Vector3(0.18f, 0.24f, 0.18f), BopBuyMaterials.WoodTable);
                    AddSphere(root, "TreeOrnament1", new Vector3(0.38f, 0.1f, 0.38f), 0.12f, BopBuyMaterials.RedAccent);
                    AddSphere(root, "TreeOrnament2", new Vector3(-0.34f, 0.48f, 0.24f), 0.1f, BopBuyMaterials.Blue);
                    AddSphere(root, "TreeOrnament3", new Vector3(0.18f, 0.75f, -0.3f), 0.1f, BopBuyMaterials.WhiteCounter);
                },
                new Vector3(0f, 0f, 0.71f),
                0.9f,
                1.2f);
        }

        private static void BuildSecurityCamera()
        {
            BuildFixture(
                "SecurityCameraGun",
                "Security Camera",
                new Vector3(0.22f, 0.16f, 0.22f),
                2f,
                "SecurityCameraGun",
                root =>
                {
                    AddBox(root, "CameraBody", Vector3.zero, new Vector3(0.2f, 0.12f, 0.16f), BopBuyMaterials.BlackWall);
                    AddCylinder(root, "CameraLens", new Vector3(0f, 0f, 0.12f), new Vector3(0.1f, 0.06f, 0.1f), BopBuyMaterials.Blue, Quaternion.Euler(90f, 0f, 0f));
                    AddBox(root, "CameraMount", new Vector3(0f, -0.1f, -0.02f), new Vector3(0.08f, 0.08f, 0.08f), BopBuyMaterials.Metal);
                },
                new Vector3(0f, 0f, 0.13f),
                0.16f,
                0.12f);
        }

        private static void BuildDomeCamera()
        {
            BuildFixture(
                "DomeCameraGun",
                "Dome Camera",
                new Vector3(0.25f, 0.18f, 0.25f),
                2f,
                "DomeCameraGun",
                root =>
                {
                    AddCylinder(root, "DomeBase", new Vector3(0f, 0.06f, 0f), new Vector3(0.25f, 0.04f, 0.25f), BopBuyMaterials.BlackWall);
                    AddSphere(root, "DomeGlass", new Vector3(0f, 0f, 0f), 0.12f, LoadGlass());
                },
                new Vector3(0f, 0f, 0.13f),
                0.18f,
                0.12f);
        }

        private static void BuildDoorbell()
        {
            BuildFixture(
                "DoorbellGun",
                "Smart Doorbell",
                new Vector3(0.1f, 0.15f, 0.04f),
                0.3f,
                "DoorbellGun",
                root =>
                {
                    AddBox(root, "DoorbellBody", Vector3.zero, new Vector3(0.1f, 0.15f, 0.04f), BopBuyMaterials.WhiteCounter);
                    AddBox(root, "DoorbellButton", new Vector3(0f, -0.02f, 0.03f), new Vector3(0.04f, 0.04f, 0.02f), BopBuyMaterials.Blue);
                },
                new Vector3(0f, 0f, 0.03f),
                0.07f,
                0.1f);
        }

        private static void BuildDeskFan()
        {
            BuildFixture(
                "DeskFanGun",
                "Desk Fan",
                new Vector3(0.3f, 0.4f, 0.3f),
                2f,
                "DeskFanGun",
                root =>
                {
                    AddCylinder(root, "FanBase", new Vector3(0f, -0.18f, 0f), new Vector3(0.26f, 0.03f, 0.26f), BopBuyMaterials.BlackWall);
                    AddBox(root, "FanNeck", new Vector3(0f, -0.02f, 0f), new Vector3(0.05f, 0.3f, 0.05f), BopBuyMaterials.Metal);
                    AddCylinder(root, "FanHead", new Vector3(0f, 0.16f, 0f), new Vector3(0.28f, 0.04f, 0.28f), BopBuyMaterials.Metal, Quaternion.Euler(90f, 0f, 0f));
                },
                new Vector3(0f, 0f, 0.16f),
                0.2f,
                0.14f);
        }

        private static void BuildSecurityMonitor()
        {
            BuildFixture(
                "SecurityMonitorGun",
                "Security Monitor",
                new Vector3(0.7f, 1.5f, 0.3f),
                10f,
                "SecurityMonitorGun",
                root =>
                {
                    AddBox(root, "MonitorPole", new Vector3(0f, -0.35f, 0f), new Vector3(0.08f, 1.1f, 0.08f), BopBuyMaterials.Metal);
                    AddBox(root, "MonitorBody", new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.45f, 0.1f), BopBuyMaterials.BlackWall);
                    AddBox(root, "MonitorScreen", new Vector3(0f, 0.55f, 0.06f), new Vector3(0.6f, 0.36f, 0.02f), BopBuyMaterials.Blue);
                    AddBox(root, "MonitorMount", new Vector3(0f, 0.15f, 0f), new Vector3(0.2f, 0.12f, 0.12f), BopBuyMaterials.Metal);
                },
                new Vector3(0f, 0f, 0.16f),
                0.5f,
                0.35f);
        }

        private static void BuildHighTable()
        {
            BuildFixture(
                "HighTableGun",
                "High Display Table",
                new Vector3(1.6f, 1.05f, 0.75f),
                25f,
                "HighTableGun",
                root =>
                {
                    AddBox(root, "HighTableTop", new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 0.08f, 0.75f), BopBuyMaterials.WoodTable);
                    AddLeg(root, new Vector3(-0.6f, 0.22f, -0.24f), 0.95f);
                    AddLeg(root, new Vector3(0.6f, 0.22f, -0.24f), 0.95f);
                    AddLeg(root, new Vector3(-0.6f, 0.22f, 0.24f), 0.95f);
                    AddLeg(root, new Vector3(0.6f, 0.22f, 0.24f), 0.95f);
                },
                new Vector3(0f, 0f, 0.38f),
                1.1f,
                0.35f);
        }

        private static void BuildWorkbench()
        {
            BuildFixture(
                "WorkbenchGun",
                "Workbench",
                new Vector3(2.4f, 0.9f, 0.8f),
                45f,
                "WorkbenchGun",
                root =>
                {
                    AddBox(root, "WorkbenchTop", new Vector3(0f, 0.43f, 0f), new Vector3(2.4f, 0.1f, 0.8f), BopBuyMaterials.Metal);
                    AddBox(root, "WorkbenchBody", new Vector3(0f, 0.1f, 0f), new Vector3(2.3f, 0.55f, 0.7f), BopBuyMaterials.Blue);
                    AddBox(root, "WorkbenchShelf", new Vector3(0f, -0.2f, 0f), new Vector3(2.2f, 0.05f, 0.6f), BopBuyMaterials.BlackWall);
                },
                new Vector3(0f, 0f, 0.41f),
                1.7f,
                0.5f);
        }

        private static void BuildPhoneCase()
        {
            BuildFixture(
                "PhoneCaseGun",
                "Phone Case",
                new Vector3(0.09f, 0.16f, 0.015f),
                0.1f,
                "PhoneCaseGun",
                root => AddBox(root, "PhoneCase", Vector3.zero, new Vector3(0.09f, 0.16f, 0.015f), BopBuyMaterials.Blue),
                new Vector3(0f, 0f, 0.012f),
                0.07f,
                0.12f);
        }

        private static void BuildWallet()
        {
            BuildFixture(
                "WalletGun",
                "Wallet",
                new Vector3(0.11f, 0.08f, 0.02f),
                0.1f,
                "WalletGun",
                root => AddBox(root, "Wallet", Vector3.zero, new Vector3(0.11f, 0.08f, 0.02f), BopBuyMaterials.WoodTable),
                new Vector3(0f, 0f, 0.014f),
                0.08f,
                0.06f);
        }

        private static void BuildScreenProtector()
        {
            BuildFixture(
                "ScreenProtectorGun",
                "Screen Protector",
                new Vector3(0.09f, 0.16f, 0.004f),
                0.02f,
                "ScreenProtectorGun",
                root => AddBox(root, "ScreenProtector", Vector3.zero, new Vector3(0.09f, 0.16f, 0.004f), LoadGlass()),
                new Vector3(0f, 0f, 0.006f),
                0.07f,
                0.12f);
        }

        private static void BuildFixture(
            string prefabName,
            string displayName,
            Vector3 colliderSize,
            float mass,
            string labelName,
            Action<Transform> buildGeometry,
            Vector3 labelCenter,
            float labelWidth,
            float labelHeight)
        {
            GameObject root = CreateRoot(prefabName, colliderSize, mass);
            buildGeometry(root.transform);

            PropLabelBuilder.AddFrontBackLabels(
                root.transform,
                labelName,
                labelName + ".png",
                labelCenter,
                labelWidth,
                labelHeight,
                colliderSize.z * 0.5f + 0.004f);

            ConfigureGun(root.GetComponent<GunItem>(), displayName, colliderSize, mass);
            ConfigureProjectile(root.GetComponent<GunProjectileBody>(), mass);
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            PrefabUtility.SaveAsPrefabAsset(root, GetPrefabPath(prefabName));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static string GetPrefabPath(string prefabName)
        {
            return prefabName == "TVBoxGun"
                ? LARGE_PROPS_FOLDER + "/" + prefabName + ".prefab"
                : ITEMS_FOLDER + "/" + prefabName + ".prefab";
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
            collider.size = colliderSize;

            root.AddComponent<GunItem>();
            root.AddComponent<GunProjectileBody>();
            return root;
        }

        private static void ConfigureGun(GunItem gunItem, string displayName, Vector3 colliderSize, float mass)
        {
            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = displayName;
            float largestDimension = Mathf.Max(colliderSize.x, Mathf.Max(colliderSize.y, colliderSize.z));
            serializedObject.FindProperty("pickupScale").vector3Value = Vector3.one * Mathf.Clamp(0.35f / largestDimension, 0.12f, 1f);
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
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void AddCylinder(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, Quaternion? rotation = null)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localScale = size;
            cylinder.transform.localRotation = rotation ?? Quaternion.identity;
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void AddSphere(Transform parent, string name, Vector3 localPosition, float diameter, Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localScale = Vector3.one * diameter;
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void AddLeg(Transform parent, Vector3 localPosition, float height)
        {
            AddBox(parent, "Leg", localPosition, new Vector3(0.06f, height, 0.06f), BopBuyMaterials.Metal);
        }

        private static Material LoadGlass()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(GLASS_MATERIAL_PATH);
        }
    }
}
