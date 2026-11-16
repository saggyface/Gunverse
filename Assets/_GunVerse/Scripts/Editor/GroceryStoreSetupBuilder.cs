using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class GroceryStoreSetupBuilder
    {
        private const string MATERIALS_FOLDER = "Assets/_GunVerse/Materials";
        private const string ITEMS_FOLDER = "Assets/_GunVerse/Prefabs/Items";
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";
        private const string URP_UNLIT_SHADER = "Universal Render Pipeline/Unlit";

        private const string SOUP_CAN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/SoupCanGun.prefab";
        private const string CEREAL_BOX_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/CerealBoxGun.prefab";
        private const string MILK_CARTON_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/MilkCartonGun.prefab";
        private const string BREAD_LOAF_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/BreadLoafGun.prefab";
        private const string DETERGENT_BOTTLE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/DetergentBottleGun.prefab";
        private const string TOMATO_CAN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/TomatoCanGun.prefab";
        private const string SHELF_GUN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/ShelfRunGun.prefab";
        private const string REGISTER_GUN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/CashRegisterGun.prefab";
        private const string COUNTER_GUN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/CheckoutCounterGun.prefab";
        private const string BELT_GUN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/ConveyorBeltGun.prefab";
        private const string LANE_MARKER_GUN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/LaneMarkerGun.prefab";
        private const string CEILING_LIGHT_GUN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/CeilingLightGun.prefab";
        private const string ENTRANCE_MAT_GUN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/EntranceMatGun.prefab";

        private const string FLOOR_MATERIAL_PATH = "Assets/_GunVerse/Materials/Floor.mat";
        private const string WALL_MATERIAL_PATH = "Assets/_GunVerse/Materials/StoreWall.mat";
        private const string CEILING_MATERIAL_PATH = "Assets/_GunVerse/Materials/StoreCeiling.mat";
        private const string SHELF_METAL_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShelfMetal.mat";
        private const string SHELF_BACK_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShelfBack.mat";
        private const string CHECKOUT_MATERIAL_PATH = "Assets/_GunVerse/Materials/Checkout.mat";
        private const string CHECKOUT_TOP_MATERIAL_PATH = "Assets/_GunVerse/Materials/CheckoutTop.mat";
        private const string GLASS_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShopGlass.mat";
        private const string LIGHT_PANEL_MATERIAL_PATH = "Assets/_GunVerse/Materials/LightPanel.mat";
        private const string MILK_MATERIAL_PATH = "Assets/_GunVerse/Materials/MilkCarton.mat";
        private const string BREAD_MATERIAL_PATH = "Assets/_GunVerse/Materials/BreadLoaf.mat";
        private const string DETERGENT_MATERIAL_PATH = "Assets/_GunVerse/Materials/DetergentBottle.mat";
        private const string TOMATO_MATERIAL_PATH = "Assets/_GunVerse/Materials/TomatoCan.mat";
        private const string PRODUCT_RED_MATERIAL_PATH = "Assets/_GunVerse/Materials/ProductRed.mat";
        private const string PRODUCT_BLUE_MATERIAL_PATH = "Assets/_GunVerse/Materials/ProductBlue.mat";
        private const string PRODUCT_GREEN_MATERIAL_PATH = "Assets/_GunVerse/Materials/ProductGreen.mat";
        private const string PRODUCT_YELLOW_MATERIAL_PATH = "Assets/_GunVerse/Materials/ProductYellow.mat";
        private const string PRODUCT_ORANGE_MATERIAL_PATH = "Assets/_GunVerse/Materials/ProductOrange.mat";

        private const float STORE_WIDTH = 72f;
        private const float STORE_LENGTH = 49f;
        private const float WALL_HEIGHT = 5.1f;
        private const float ENTRANCE_WIDTH = 8f;
        private const float ENTRANCE_HEIGHT = 3.75f;
        private const float SHELF_WIDTH = 1.6f;
        private const float SHELF_LENGTH = 10f;
        private const float SHELF_HEIGHT = 2.2f;
        private const float END_PANEL_WIDTH = SHELF_WIDTH + 0.08f;
        private const float END_PANEL_HEIGHT = SHELF_HEIGHT + 0.04f;
        private const float END_PANEL_THICKNESS = 0.16f;
        private const float CHECKOUT_HEIGHT = 1.16f;
        private const float COUNTER_TOP_SURFACE_Y = CHECKOUT_HEIGHT + 0.03f;
        private const float BELT_CENTER_Y = COUNTER_TOP_SURFACE_Y + 0.015f;
        private const float REGISTER_HEIGHT = 0.3f;
        private const float REGISTER_CENTER_Y = COUNTER_TOP_SURFACE_Y + REGISTER_HEIGHT * 0.5f;
        private const float LANE_MARKER_HEIGHT = 0.12f;
        private const float LANE_MARKER_CENTER_Y = COUNTER_TOP_SURFACE_Y + LANE_MARKER_HEIGHT * 0.5f;
        private const float FIXTURE_Y = 3.36f;
        private const float FIXTURE_RAIL_Y = 3.31f;
        private const float FIXTURE_RAIL_HEIGHT = 0.02f;
        private const float FIXTURE_RAIL_Z_OFFSET = 0.225f;
        private const float CHECKOUT_Z = -18f;
        private const float STOCKROOM_PARTITION_Z = 13f;

        private const string PRODUCE_FLOOR_MATERIAL_PATH = "Assets/_GunVerse/Materials/ProduceFloor.mat";
        private const string SPAWN_SHIELD_MATERIAL_PATH = "Assets/_GunVerse/Resources/SpawnShield.mat";

        private static readonly Vector3[] shelfRunPositions =
        {
            new Vector3(-32f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(-26f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(-20f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(-14f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(-2f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(2f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(14f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(20f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(26f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(32f, SHELF_HEIGHT * 0.5f, 0f),
            new Vector3(-8f, SHELF_HEIGHT * 0.5f, 18.5f),
            new Vector3(8f, SHELF_HEIGHT * 0.5f, 18.5f)
        };
        private static readonly float[] shelfBoardHeights = { 0.38f, 0.92f, 1.46f, 2.0f };
        private static readonly float[] productZPositions = { -2.6f, 0f, 2.6f };
        private static readonly string[] shelfProductPrefabPaths =
        {
            SOUP_CAN_PREFAB_PATH,
            CEREAL_BOX_PREFAB_PATH,
            MILK_CARTON_PREFAB_PATH,
            BREAD_LOAF_PREFAB_PATH,
            DETERGENT_BOTTLE_PREFAB_PATH,
            TOMATO_CAN_PREFAB_PATH
        };
        private static readonly float[] checkoutLaneCenters = { -18f, 0f, 18f };

        private static readonly Vector3[] spawnPositions =
        {
            new Vector3(-31f, 0.05f, -21f),
            new Vector3(31f, 0.05f, -21f),
            new Vector3(-2f, 0.05f, -22.5f),
            new Vector3(2f, 0.05f, -22.5f),
            new Vector3(-34.5f, 0.05f, -6f),
            new Vector3(-34.5f, 0.05f, 6f),
            new Vector3(34.5f, 0.05f, -6f),
            new Vector3(34.5f, 0.05f, 6f),
            new Vector3(-6f, 0.05f, -8f),
            new Vector3(6f, 0.05f, -8f),
            new Vector3(-6f, 0.05f, 8f),
            new Vector3(6f, 0.05f, 8f),
            new Vector3(-12f, 0.05f, 15f),
            new Vector3(12f, 0.05f, 15f),
            new Vector3(0f, 0.05f, -10f),
            new Vector3(-24f, 0.05f, -8f),
            new Vector3(24f, 0.05f, -8f),
            new Vector3(-24f, 0.05f, 8f),
            new Vector3(24f, 0.05f, 8f),
            new Vector3(0f, 0.05f, 10f)
        };

        private static readonly float[] spawnYaws =
        {
            0f, 0f, 0f, 0f,
            90f, 90f,
            -90f, -90f,
            0f, 0f, 180f, 180f,
            180f, 180f,
            0f,
            0f, 0f, 180f, 180f,
            180f
        };

        private static readonly Vector3[] ceilingLightPositions =
        {
            new Vector3(-18f, FIXTURE_Y, -18f),
            new Vector3(0f, FIXTURE_Y, -18f),
            new Vector3(18f, FIXTURE_Y, -18f),
            new Vector3(-24f, FIXTURE_Y, -6f),
            new Vector3(-24f, FIXTURE_Y, 6f),
            new Vector3(24f, FIXTURE_Y, -6f),
            new Vector3(24f, FIXTURE_Y, 6f),
            new Vector3(0f, FIXTURE_Y, 0f),
            new Vector3(-12f, FIXTURE_Y, 18f),
            new Vector3(12f, FIXTURE_Y, 18f)
        };

        private sealed class StoreMaterials
        {
            public Material Floor;
            public Material Wall;
            public Material Ceiling;
            public Material ShelfMetal;
            public Material ShelfBack;
            public Material Checkout;
            public Material CheckoutTop;
            public Material ProduceFloor;
            public Material Glass;
            public Material LightPanel;
            public Material Milk;
            public Material Bread;
            public Material Detergent;
            public Material Tomato;
            public Material[] Products;
        }

        private sealed class GunConfiguration
        {
            public string DisplayName;
            public PrimitiveType Shape;
            public Vector3 WorldScale;
            public Vector3 PickupPositionOffset;
            public Vector3 PickupRotationEuler;
            public Vector3 MuzzleOffset;
            public float RoundsPerMinute;
            public int MagazineSize;
            public float ReloadDuration;
            public float Damage;
            public GunWeightClass WeightClass;
            public float Mass = 1f;
            public float TrailWidthScale = 1f;
            public float TrailLengthMultiplier = 20f;
            public float ProjectileSpeed;
            public float ProjectileLifetime;
        }

        private sealed class ItemPlacement
        {
            public string PrefabPath;
            public Vector3 Position;
            public float Yaw;
            public float SurfaceY;

            public ItemPlacement(string prefabPath, Vector3 position, float yaw, float surfaceY)
            {
                PrefabPath = prefabPath;
                Position = position;
                Yaw = yaw;
                SurfaceY = surfaceY;
            }
        }

        public static void Build()
        {
            EnsureFolder(MATERIALS_FOLDER);
            EnsureFolder(ITEMS_FOLDER);

            StoreMaterials materials = CreateMaterials();
            EnsureSpawnShieldMaterial();
            BuildNewItemPrefabs(materials);
            ModelPickupPrefabBuilder.ApplyConfiguredModels();
            LargeGunPropPrefabBuilder.Build();
            StorePropPrefabBuilder.Build();
            GunItemPrefabRegistry.Rebuild();

            Scene scene = EditorSceneManager.OpenScene(GROCERY_STORE_SCENE_PATH, OpenSceneMode.Single);
            CleanupScene();
            BuildSpawnManager();
            BuildSpawnPoints();
            Transform largeGunPropsTransform = BuildEnvironment(materials);
            PlacePickupItems();
            ConfigureLighting(materials, largeGunPropsTransform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            Debug.Log("[GroceryStoreSetupBuilder] Built the grocery store, lighting, spawn points, and pickup content.");
        }

        private static StoreMaterials CreateMaterials()
        {
            return new StoreMaterials
            {
                Floor = GetOrCreateMaterial(FLOOR_MATERIAL_PATH, ArtPalette.GroceryStore.Floor, 0.3f, 0.05f, false),
                Wall = GetOrCreateMaterial(WALL_MATERIAL_PATH, ArtPalette.GroceryStore.Wall, 0.18f, 0f, false),
                Ceiling = GetOrCreateMaterial(CEILING_MATERIAL_PATH, ArtPalette.GroceryStore.Ceiling, 0.12f, 0f, false),
                ShelfMetal = GetOrCreateMaterial(SHELF_METAL_MATERIAL_PATH, ArtPalette.GroceryStore.ShelfMetal, 0.3f, 0.15f, false),
                ShelfBack = GetOrCreateMaterial(SHELF_BACK_MATERIAL_PATH, ArtPalette.GroceryStore.ShelfBack, 0.15f, 0f, false),
                Checkout = GetOrCreateMaterial(CHECKOUT_MATERIAL_PATH, ArtPalette.GroceryStore.Checkout, 0.22f, 0.05f, false),
                CheckoutTop = GetOrCreateMaterial(CHECKOUT_TOP_MATERIAL_PATH, ArtPalette.GroceryStore.CheckoutTop, 0.3f, 0.2f, false),
                ProduceFloor = GetOrCreateMaterial(PRODUCE_FLOOR_MATERIAL_PATH, ArtPalette.GroceryStore.ProduceFloor, 0.2f, 0f, false),
                Glass = GetOrCreateMaterial(GLASS_MATERIAL_PATH, new Color(0.34f, 0.54f, 0.63f), 0.75f, 0.1f, false),
                LightPanel = GetOrCreateMaterial(LIGHT_PANEL_MATERIAL_PATH, new Color(1f, 0.94f, 0.78f), 0f, 0f, true),
                Milk = GetOrCreateMaterial(MILK_MATERIAL_PATH, new Color(0.88f, 0.92f, 0.95f), 0.3f, 0f, false),
                Bread = GetOrCreateMaterial(BREAD_MATERIAL_PATH, new Color(0.80f, 0.58f, 0.32f), 0.2f, 0f, false),
                Detergent = GetOrCreateMaterial(DETERGENT_MATERIAL_PATH, new Color(0.16f, 0.64f, 0.44f), 0.35f, 0f, false),
                Tomato = GetOrCreateMaterial(TOMATO_MATERIAL_PATH, new Color(0.80f, 0.18f, 0.15f), 0.3f, 0f, false),
                Products = new[]
                {
                    GetOrCreateMaterial(PRODUCT_RED_MATERIAL_PATH, new Color(0.74f, 0.16f, 0.14f), 0.15f, 0f, false),
                    GetOrCreateMaterial(PRODUCT_BLUE_MATERIAL_PATH, new Color(0.12f, 0.36f, 0.68f), 0.15f, 0f, false),
                    GetOrCreateMaterial(PRODUCT_GREEN_MATERIAL_PATH, new Color(0.14f, 0.52f, 0.28f), 0.15f, 0f, false),
                    GetOrCreateMaterial(PRODUCT_YELLOW_MATERIAL_PATH, new Color(0.88f, 0.68f, 0.12f), 0.15f, 0f, false),
                    GetOrCreateMaterial(PRODUCT_ORANGE_MATERIAL_PATH, new Color(0.85f, 0.38f, 0.10f), 0.15f, 0f, false)
                }
            };
        }

        public static void ApplyPaletteToMaterials()
        {
            CreateMaterials();
            AssetDatabase.SaveAssets();
        }

        private static void BuildNewItemPrefabs(StoreMaterials materials)
        {
            BuildGunPrefab(MILK_CARTON_PREFAB_PATH, "MilkCartonGun", materials.Milk, new GunConfiguration
            {
                DisplayName = "Milk Carton",
                Shape = PrimitiveType.Cube,
                WorldScale = new Vector3(0.22f, 0.38f, 0.18f),
                PickupPositionOffset = Vector3.zero,
                PickupRotationEuler = Vector3.zero,
                MuzzleOffset = new Vector3(0f, 0f, 0.14f),
                RoundsPerMinute = 150f,
                MagazineSize = 6,
                ReloadDuration = 1.8f,
                Damage = 30f,
                WeightClass = GunWeightClass.Heavy,
                Mass = 1.2f,
                ProjectileSpeed = 26f,
                ProjectileLifetime = 3.5f
            });

            BuildGunPrefab(BREAD_LOAF_PREFAB_PATH, "BreadLoafGun", materials.Bread, new GunConfiguration
            {
                DisplayName = "Bread Loaf",
                Shape = PrimitiveType.Cube,
                WorldScale = new Vector3(0.38f, 0.2f, 0.24f),
                PickupPositionOffset = Vector3.zero,
                PickupRotationEuler = Vector3.zero,
                MuzzleOffset = new Vector3(0f, 0f, 0.2f),
                RoundsPerMinute = 480f,
                MagazineSize = 20,
                ReloadDuration = 1.6f,
                Damage = 9f,
                WeightClass = GunWeightClass.Light,
                Mass = 0.5f,
                ProjectileSpeed = 22f,
                ProjectileLifetime = 2.4f
            });

            BuildGunPrefab(DETERGENT_BOTTLE_PREFAB_PATH, "DetergentBottleGun", materials.Detergent, new GunConfiguration
            {
                DisplayName = "Detergent Bottle",
                Shape = PrimitiveType.Cube,
                WorldScale = new Vector3(0.24f, 0.46f, 0.22f),
                PickupPositionOffset = Vector3.zero,
                PickupRotationEuler = Vector3.zero,
                MuzzleOffset = new Vector3(0f, 0f, 0.15f),
                RoundsPerMinute = 100f,
                MagazineSize = 4,
                ReloadDuration = 2.4f,
                Damage = 45f,
                WeightClass = GunWeightClass.Heavy,
                Mass = 1.0f,
                ProjectileSpeed = 20f,
                ProjectileLifetime = 4f
            });

            BuildGunPrefab(TOMATO_CAN_PREFAB_PATH, "TomatoCanGun", materials.Tomato, new GunConfiguration
            {
                DisplayName = "Tomato Can",
                Shape = PrimitiveType.Cylinder,
                WorldScale = new Vector3(0.16f, 0.08f, 0.16f),
                PickupPositionOffset = Vector3.zero,
                PickupRotationEuler = Vector3.zero,
                MuzzleOffset = new Vector3(0f, 0f, 0.1f),
                RoundsPerMinute = 320f,
                MagazineSize = 10,
                ReloadDuration = 1.1f,
                Damage = 18f,
                WeightClass = GunWeightClass.Medium,
                Mass = 0.5f,
                ProjectileSpeed = 36f,
                ProjectileLifetime = 3f
            });
        }

        private static void BuildGunPrefab(string prefabPath, string itemName, Material material, GunConfiguration configuration)
        {
            GameObject root = GameObject.CreatePrimitive(configuration.Shape);
            root.name = itemName;
            root.transform.localScale = configuration.WorldScale;
            root.GetComponent<MeshRenderer>().sharedMaterial = material;

            root.AddComponent<NetworkObject>();
            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.SwitchTransformSpaceWhenParented = true;

            Rigidbody rigidBody = root.AddComponent<Rigidbody>();
            rigidBody.mass = configuration.Mass;
            rigidBody.useGravity = true;
            rigidBody.isKinematic = true;

            GunItem gunItem = root.AddComponent<GunItem>();
            GunProjectileBody projectileBody = root.AddComponent<GunProjectileBody>();
            GunPerformanceSettings.ApplyToGun(root);
            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = configuration.DisplayName;
            serializedObject.FindProperty("pickupPositionOffset").vector3Value = configuration.PickupPositionOffset;
            serializedObject.FindProperty("pickupScale").vector3Value = configuration.WorldScale;
            serializedObject.FindProperty("pickupRotationEuler").vector3Value = configuration.PickupRotationEuler;
            serializedObject.FindProperty("muzzleOffset").vector3Value = configuration.MuzzleOffset;
            serializedObject.FindProperty("roundsPerMinute").floatValue = configuration.RoundsPerMinute;
            serializedObject.FindProperty("magazineSize").intValue = configuration.MagazineSize;
            serializedObject.FindProperty("reloadDuration").floatValue = configuration.ReloadDuration;
            serializedObject.FindProperty("damage").floatValue = configuration.Damage;
            serializedObject.FindProperty("weightClass").enumValueIndex = (int)configuration.WeightClass;
            serializedObject.FindProperty("projectileSpeed").floatValue = configuration.ProjectileSpeed;
            serializedObject.FindProperty("projectileLifetime").floatValue = configuration.ProjectileLifetime;
            serializedObject.ApplyModifiedProperties();

            SerializedObject projectileSerializedObject = new SerializedObject(projectileBody);
            projectileSerializedObject.FindProperty("mass").floatValue = configuration.Mass;
            projectileSerializedObject.FindProperty("trailWidthScale").floatValue = configuration.TrailWidthScale;
            projectileSerializedObject.FindProperty("trailLengthMultiplier").floatValue = configuration.TrailLengthMultiplier;
            projectileSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            GunPropPhysicsAssets.ApplyTo(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            ClearInScenePlacedFlags(prefabPath);
        }

        private static void CleanupScene()
        {
            foreach (GunItem gunItem in Object.FindObjectsByType<GunItem>())
            {
                Object.DestroyImmediate(gunItem.gameObject);
            }

            foreach (PlayerSpawnPoint spawnPoint in Object.FindObjectsByType<PlayerSpawnPoint>())
            {
                Object.DestroyImmediate(spawnPoint.gameObject);
            }

            foreach (PlayerSpawnManager spawnManager in Object.FindObjectsByType<PlayerSpawnManager>())
            {
                Object.DestroyImmediate(spawnManager.gameObject);
            }

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                if (root.name == "Floor" || root.name == "StoreEnvironment" || root.name == "StoreLighting" || root.name == "SpawnPoints" || root.name == "PickupItems" || root.name == "LargeGunProps" || root.name == "FfaGameMode" || root.name == "GameMode")
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void BuildSpawnManager()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
            if (playerPrefab == null)
            {
                Debug.LogError("[GroceryStoreSetupBuilder] Player prefab not found: " + PLAYER_PREFAB_PATH);
                return;
            }

            GameObject spawnManagerObject = new GameObject("PlayerSpawnManager");
            spawnManagerObject.AddComponent<NetworkObject>();
            PlayerSpawnManager spawnManager = spawnManagerObject.AddComponent<PlayerSpawnManager>();

            SerializedObject serializedObject = new SerializedObject(spawnManager);
            serializedObject.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            serializedObject.ApplyModifiedProperties();

            if (Object.FindAnyObjectByType<StoreFogController>(FindObjectsInactive.Include) == null)
            {
                GameObject fogObject = new GameObject("StoreFogController");
                fogObject.AddComponent<NetworkObject>();
                fogObject.AddComponent<StoreFogController>();
            }
        }

        private static void BuildSpawnPoints()
        {
            GameObject spawnPointsRoot = new GameObject("SpawnPoints");

            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject spawnPointObject = new GameObject("Spawn Point " + (i + 1));
                spawnPointObject.transform.SetParent(spawnPointsRoot.transform, false);
                spawnPointObject.transform.position = spawnPositions[i];
                spawnPointObject.transform.rotation = Quaternion.Euler(0f, spawnYaws[i], 0f);
                spawnPointObject.AddComponent<PlayerSpawnPoint>();
            }
        }

        private static Transform BuildEnvironment(StoreMaterials materials)
        {
            GameObject environment = new GameObject("StoreEnvironment");
            Transform environmentTransform = environment.transform;

            GameObject largeGunProps = new GameObject("LargeGunProps");
            Transform largeGunPropsTransform = largeGunProps.transform;

            GameObject shelfProducts = new GameObject("ShelfProducts");
            shelfProducts.transform.SetParent(largeGunPropsTransform, false);
            Transform shelfProductsTransform = shelfProducts.transform;

            CreateBox("StoreFloor", environmentTransform, new Vector3(0f, -0.1f, 0f), new Vector3(STORE_WIDTH, 0.2f, STORE_LENGTH), materials.Floor);
            CreateBox("StoreCeiling", environmentTransform, new Vector3(0f, WALL_HEIGHT + 0.1f, 0f), new Vector3(STORE_WIDTH, 0.2f, STORE_LENGTH), materials.Ceiling);
            CreateBox("BackWall", environmentTransform, new Vector3(0f, WALL_HEIGHT * 0.5f, STORE_LENGTH * 0.5f), new Vector3(STORE_WIDTH, WALL_HEIGHT, 0.3f), materials.Wall);
            CreateBox("LeftWall", environmentTransform, new Vector3(-STORE_WIDTH * 0.5f, WALL_HEIGHT * 0.5f, 0f), new Vector3(0.3f, WALL_HEIGHT, STORE_LENGTH), materials.Wall);
            CreateBox("RightWall", environmentTransform, new Vector3(STORE_WIDTH * 0.5f, WALL_HEIGHT * 0.5f, 0f), new Vector3(0.3f, WALL_HEIGHT, STORE_LENGTH), materials.Wall);

            BuildEntrance(environmentTransform, materials);
            BuildInteriorArchitecture(environmentTransform, materials);

            for (int i = 0; i < shelfRunPositions.Length; i++)
            {
                BuildShelfRun(largeGunPropsTransform, shelfProductsTransform, shelfRunPositions[i], i);
            }

            for (int i = 0; i < checkoutLaneCenters.Length; i++)
            {
                BuildCheckoutLane(largeGunPropsTransform, checkoutLaneCenters[i], i);
            }

            InstantiateProp(ENTRANCE_MAT_GUN_PREFAB_PATH, "Entrance Mat", largeGunPropsTransform, new Vector3(0f, 0.01f, -STORE_LENGTH * 0.5f + 1.8f), Quaternion.Euler(0f, 90f, 0f));
            CreateBox("CheckoutFloorStripe", environmentTransform, new Vector3(0f, 0.005f, -12f), new Vector3(STORE_WIDTH - 1f, 0.01f, 0.08f), materials.CheckoutTop, false);
            CreateBox("ProduceFloor", environmentTransform, new Vector3(0f, 0.004f, 0f), new Vector3(16f, 0.01f, 24f), materials.ProduceFloor, false);
            CreateBox("StockroomFloor", environmentTransform, new Vector3(0f, 0.004f, 18.5f), new Vector3(STORE_WIDTH - 1f, 0.01f, 10.5f), materials.Checkout, false);

            return largeGunPropsTransform;
        }

        private static void BuildEntrance(Transform environmentTransform, StoreMaterials materials)
        {
            float sideSegmentWidth = (STORE_WIDTH - ENTRANCE_WIDTH) * 0.5f;
            float sideSegmentCenter = (STORE_WIDTH + ENTRANCE_WIDTH) * 0.25f;

            CreateBox("FrontWallLeft", environmentTransform, new Vector3(-sideSegmentCenter, WALL_HEIGHT * 0.5f, -STORE_LENGTH * 0.5f), new Vector3(sideSegmentWidth, WALL_HEIGHT, 0.3f), materials.Wall);
            CreateBox("FrontWallRight", environmentTransform, new Vector3(sideSegmentCenter, WALL_HEIGHT * 0.5f, -STORE_LENGTH * 0.5f), new Vector3(sideSegmentWidth, WALL_HEIGHT, 0.3f), materials.Wall);
            CreateBox("EntranceHeader", environmentTransform, new Vector3(0f, ENTRANCE_HEIGHT + (WALL_HEIGHT - ENTRANCE_HEIGHT) * 0.5f, -STORE_LENGTH * 0.5f), new Vector3(ENTRANCE_WIDTH, WALL_HEIGHT - ENTRANCE_HEIGHT, 0.3f), materials.Wall);

            CreateBox("EntranceDoorLeft", environmentTransform, new Vector3(-ENTRANCE_WIDTH * 0.25f, ENTRANCE_HEIGHT * 0.5f, -STORE_LENGTH * 0.5f), new Vector3(ENTRANCE_WIDTH * 0.5f, ENTRANCE_HEIGHT, 0.08f), materials.Glass);
            CreateBox("EntranceDoorRight", environmentTransform, new Vector3(ENTRANCE_WIDTH * 0.25f, ENTRANCE_HEIGHT * 0.5f, -STORE_LENGTH * 0.5f), new Vector3(ENTRANCE_WIDTH * 0.5f, ENTRANCE_HEIGHT, 0.08f), materials.Glass);
            CreateBox("EntranceMullion", environmentTransform, new Vector3(0f, ENTRANCE_HEIGHT * 0.5f, -STORE_LENGTH * 0.5f), new Vector3(0.1f, ENTRANCE_HEIGHT, 0.12f), materials.ShelfMetal);
        }

        private static void BuildInteriorArchitecture(Transform environmentTransform, StoreMaterials materials)
        {
            float vestibuleDepth = 3.5f;
            float vestibuleCenterZ = -STORE_LENGTH * 0.5f + vestibuleDepth * 0.5f;
            CreateBox("VestibuleLeftWall", environmentTransform, new Vector3(-ENTRANCE_WIDTH * 0.5f, WALL_HEIGHT * 0.5f, vestibuleCenterZ), new Vector3(0.3f, WALL_HEIGHT, vestibuleDepth), materials.Wall);
            CreateBox("VestibuleRightWall", environmentTransform, new Vector3(ENTRANCE_WIDTH * 0.5f, WALL_HEIGHT * 0.5f, vestibuleCenterZ), new Vector3(0.3f, WALL_HEIGHT, vestibuleDepth), materials.Wall);

            CreateBox("StockroomWallLeft", environmentTransform, new Vector3(-26f, WALL_HEIGHT * 0.5f, STOCKROOM_PARTITION_Z), new Vector3(20f, WALL_HEIGHT, 0.3f), materials.Wall);
            CreateBox("StockroomWallCenter", environmentTransform, new Vector3(0f, WALL_HEIGHT * 0.5f, STOCKROOM_PARTITION_Z), new Vector3(20f, WALL_HEIGHT, 0.3f), materials.Wall);
            CreateBox("StockroomWallRight", environmentTransform, new Vector3(26f, WALL_HEIGHT * 0.5f, STOCKROOM_PARTITION_Z), new Vector3(20f, WALL_HEIGHT, 0.3f), materials.Wall);

            CreateBox("LoadingDoor", environmentTransform, new Vector3(0f, 2f, STORE_LENGTH * 0.5f - 0.16f), new Vector3(8f, 4f, 0.08f), materials.CheckoutTop);

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    CreateBox(
                        "InteriorColumn",
                        environmentTransform,
                        new Vector3(x * 24f, WALL_HEIGHT * 0.5f, z * 12f),
                        new Vector3(0.6f, WALL_HEIGHT, 0.6f),
                        materials.Wall);
                }
            }
        }

        private static void BuildShelfRun(Transform largeGunPropsTransform, Transform shelfProductsTransform, Vector3 shelfPosition, int index)
        {
            GameObject shelfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SHELF_GUN_PREFAB_PATH);
            if (shelfPrefab == null)
            {
                Debug.LogError("[GroceryStoreSetupBuilder] Shelf gun prefab not found: " + SHELF_GUN_PREFAB_PATH);
                return;
            }

            GameObject shelfInstance = (GameObject)PrefabUtility.InstantiatePrefab(shelfPrefab);
            shelfInstance.name = "Shelf Run " + (index + 1);
            shelfInstance.transform.SetParent(largeGunPropsTransform, false);
            shelfInstance.transform.SetPositionAndRotation(shelfPosition, Quaternion.identity);

            BuildShelfProducts(shelfProductsTransform, shelfPosition, index);
        }

        private static void BuildShelfProducts(Transform productParent, Vector3 shelfPosition, int shelfIndex)
        {
            for (int boardIndex = 0; boardIndex < shelfBoardHeights.Length - 1; boardIndex++)
            {
                float boardTopY = shelfBoardHeights[boardIndex] + 0.04f;
                int slotIndex = 0;

                for (int side = -1; side <= 1; side += 2)
                {
                    for (int productIndex = 0; productIndex < productZPositions.Length; productIndex++)
                    {
                        int typeIndex = (shelfIndex * 3 + boardIndex + slotIndex) % shelfProductPrefabPaths.Length;
                        string prefabPath = shelfProductPrefabPaths[typeIndex];
                        GameObject productPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (productPrefab == null)
                        {
                            Debug.LogError("[GroceryStoreSetupBuilder] Shelf product prefab not found: " + prefabPath);
                            slotIndex++;
                            continue;
                        }

                        Vector3 position = new Vector3(
                            shelfPosition.x + side * 0.46f,
                            0f,
                            shelfPosition.z + productZPositions[productIndex]);
                        Quaternion rotation = GetShelfProductRotation(prefabPath, side);

                        GameObject productInstance = (GameObject)PrefabUtility.InstantiatePrefab(productPrefab);
                        productInstance.transform.SetParent(productParent, false);
                        productInstance.transform.SetPositionAndRotation(position, rotation);

                        Bounds bounds = GetRendererBounds(productInstance);
                        productInstance.transform.position = new Vector3(position.x, boardTopY - bounds.min.y + 0.01f, position.z);
                        slotIndex++;
                    }
                }
            }
        }

        private static Quaternion GetShelfProductRotation(string prefabPath, int side)
        {
            float yaw = side < 0 ? -90f : 90f;
            Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
            if (prefabPath == DETERGENT_BOTTLE_PREFAB_PATH)
            {
                return facing * Quaternion.Euler(0f, 0f, 90f);
            }

            return facing;
        }

        private static void BuildCheckoutLane(Transform largeGunPropsTransform, float centerX, int index)
        {
            InstantiateProp(
                COUNTER_GUN_PREFAB_PATH,
                "Checkout Counter " + (index + 1),
                largeGunPropsTransform,
                new Vector3(centerX, CHECKOUT_HEIGHT * 0.5f, CHECKOUT_Z),
                Quaternion.Euler(0f, 90f, 0f));

            InstantiateProp(
                BELT_GUN_PREFAB_PATH,
                "Conveyor Belt " + (index + 1),
                largeGunPropsTransform,
                new Vector3(centerX - 0.25f, BELT_CENTER_Y, CHECKOUT_Z),
                Quaternion.Euler(0f, 90f, 0f));

            InstantiateProp(
                REGISTER_GUN_PREFAB_PATH,
                "Cash Register " + (index + 1),
                largeGunPropsTransform,
                new Vector3(centerX + 1.1f, REGISTER_CENTER_Y, CHECKOUT_Z),
                Quaternion.identity);

            InstantiateProp(
                LANE_MARKER_GUN_PREFAB_PATH,
                "Lane Marker " + (index + 1),
                largeGunPropsTransform,
                new Vector3(centerX, LANE_MARKER_CENTER_Y, CHECKOUT_Z + 0.53f),
                Quaternion.Euler(0f, 90f, 0f));
        }

        private static GameObject InstantiateProp(string prefabPath, string instanceName, Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[GroceryStoreSetupBuilder] Prop prefab not found: " + prefabPath);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static void PlacePickupItems()
        {
            GameObject pickupRoot = new GameObject("PickupItems");

            ItemPlacement[] placements =
            {
                new ItemPlacement(SOUP_CAN_PREFAB_PATH, new Vector3(-19.4f, 0f, CHECKOUT_Z - 0.2f), 0f, COUNTER_TOP_SURFACE_Y),
                new ItemPlacement(CEREAL_BOX_PREFAB_PATH, new Vector3(-1.35f, 0f, CHECKOUT_Z - 0.2f), 0f, COUNTER_TOP_SURFACE_Y),
                new ItemPlacement(TOMATO_CAN_PREFAB_PATH, new Vector3(16.7f, 0f, CHECKOUT_Z - 0.2f), 0f, COUNTER_TOP_SURFACE_Y),

                new ItemPlacement(MILK_CARTON_PREFAB_PATH, new Vector3(-4f, 0f, -8f), 20f, 0f),
                new ItemPlacement(BREAD_LOAF_PREFAB_PATH, new Vector3(0f, 0f, -8f), 90f, 0f),
                new ItemPlacement(DETERGENT_BOTTLE_PREFAB_PATH, new Vector3(4f, 0f, -8f), 180f, 0f),
                new ItemPlacement(SOUP_CAN_PREFAB_PATH, new Vector3(-4f, 0f, 8f), 180f, 0f),
                new ItemPlacement(CEREAL_BOX_PREFAB_PATH, new Vector3(0f, 0f, 8f), 90f, 0f),
                new ItemPlacement(TOMATO_CAN_PREFAB_PATH, new Vector3(4f, 0f, 8f), 0f, 0f),

                new ItemPlacement(MILK_CARTON_PREFAB_PATH, new Vector3(-32f, 0f, -6f), 0f, 0f),
                new ItemPlacement(BREAD_LOAF_PREFAB_PATH, new Vector3(-26f, 0f, -6f), 0f, 0f),
                new ItemPlacement(DETERGENT_BOTTLE_PREFAB_PATH, new Vector3(-20f, 0f, -6f), 0f, 0f),
                new ItemPlacement(SOUP_CAN_PREFAB_PATH, new Vector3(-14f, 0f, -6f), 0f, 0f),

                new ItemPlacement(CEREAL_BOX_PREFAB_PATH, new Vector3(14f, 0f, -6f), 0f, 0f),
                new ItemPlacement(TOMATO_CAN_PREFAB_PATH, new Vector3(20f, 0f, -6f), 0f, 0f),
                new ItemPlacement(MILK_CARTON_PREFAB_PATH, new Vector3(26f, 0f, -6f), 0f, 0f),
                new ItemPlacement(BREAD_LOAF_PREFAB_PATH, new Vector3(32f, 0f, -6f), 0f, 0f),

                new ItemPlacement(DETERGENT_BOTTLE_PREFAB_PATH, new Vector3(-28f, 0f, 22f), 180f, 0f),
                new ItemPlacement(SOUP_CAN_PREFAB_PATH, new Vector3(-12f, 0f, 22f), 180f, 0f),
                new ItemPlacement(CEREAL_BOX_PREFAB_PATH, new Vector3(12f, 0f, 22f), 180f, 0f),
                new ItemPlacement(TOMATO_CAN_PREFAB_PATH, new Vector3(28f, 0f, 22f), 180f, 0f),

                new ItemPlacement(BREAD_LOAF_PREFAB_PATH, new Vector3(-34f, 0f, 0f), 90f, 0f),
                new ItemPlacement(MILK_CARTON_PREFAB_PATH, new Vector3(34f, 0f, 0f), -90f, 0f),

                new ItemPlacement(DETERGENT_BOTTLE_PREFAB_PATH, new Vector3(-10f, 0f, -10f), 0f, 0f),
                new ItemPlacement(TOMATO_CAN_PREFAB_PATH, new Vector3(10f, 0f, -10f), 0f, 0f)
            };

            foreach (ItemPlacement placement in placements)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(placement.PrefabPath);
                if (prefab == null)
                {
                    Debug.LogError("[GroceryStoreSetupBuilder] Pickup prefab not found: " + placement.PrefabPath);
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(pickupRoot.transform, false);
                instance.transform.SetPositionAndRotation(new Vector3(placement.Position.x, 0f, placement.Position.z), Quaternion.Euler(0f, placement.Yaw, 0f));

                Bounds bounds = GetRendererBounds(instance);
                float centerY = placement.SurfaceY + 0.005f;
                if (bounds.size.y > 0.0001f)
                {
                    centerY -= bounds.min.y;
                }

                instance.transform.position = new Vector3(placement.Position.x, centerY, placement.Position.z);
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

        private static void ConfigureLighting(StoreMaterials materials, Transform largeGunPropsTransform)
        {
            Light directionalLight = null;
            foreach (Light light in Object.FindObjectsByType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }

            if (directionalLight == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                directionalLight = lightObject.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            directionalLight.transform.rotation = Quaternion.Euler(50f, 330f, 0f);
            directionalLight.color = ArtPalette.GroceryStore.KeyLight;
            directionalLight.intensity = 0.6f;
            directionalLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ArtPalette.GroceryStore.Ambient;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.4f;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.sun = directionalLight;

            GameObject lightingRoot = new GameObject("StoreLighting");

            for (int i = 0; i < ceilingLightPositions.Length; i++)
            {
                Vector3 lightPosition = ceilingLightPositions[i];
                InstantiateProp(
                    CEILING_LIGHT_GUN_PREFAB_PATH,
                    "Ceiling Light " + (i + 1),
                    largeGunPropsTransform,
                    new Vector3(lightPosition.x, FIXTURE_Y, lightPosition.z),
                    Quaternion.Euler(0f, 90f, 0f));

                CreateBox(
                    "CeilingFixtureRail",
                    lightingRoot.transform,
                    new Vector3(lightPosition.x, FIXTURE_RAIL_Y, lightPosition.z - FIXTURE_RAIL_Z_OFFSET),
                    new Vector3(3.4f, FIXTURE_RAIL_HEIGHT, 0.05f),
                    materials.ShelfMetal);

                CreateBox(
                    "CeilingFixtureRail",
                    lightingRoot.transform,
                    new Vector3(lightPosition.x, FIXTURE_RAIL_Y, lightPosition.z + FIXTURE_RAIL_Z_OFFSET),
                    new Vector3(3.4f, FIXTURE_RAIL_HEIGHT, 0.05f),
                    materials.ShelfMetal);

                float rodHeight = WALL_HEIGHT - FIXTURE_RAIL_Y;
                float rodCenterY = FIXTURE_RAIL_Y + rodHeight * 0.5f;
                CreateBox(
                    "CeilingFixtureRod",
                    lightingRoot.transform,
                    new Vector3(lightPosition.x - 1.4f, rodCenterY, lightPosition.z),
                    new Vector3(0.04f, rodHeight, 0.04f),
                    materials.ShelfMetal,
                    false);
                CreateBox(
                    "CeilingFixtureRod",
                    lightingRoot.transform,
                    new Vector3(lightPosition.x + 1.4f, rodCenterY, lightPosition.z),
                    new Vector3(0.04f, rodHeight, 0.04f),
                    materials.ShelfMetal,
                    false);
            }

            Camera sceneCamera = Object.FindAnyObjectByType<Camera>();
            if (sceneCamera != null)
            {
                sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                sceneCamera.backgroundColor = ArtPalette.GroceryStore.CameraBackground;
            }
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool addCollider = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = size;
            box.isStatic = true;

            if (!addCollider)
            {
                Object.DestroyImmediate(box.GetComponent<Collider>());
            }

            if (material != null)
            {
                box.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return box;
        }

        private static void EnsureSpawnShieldMaterial()
        {
            const string resourcesFolder = "Assets/_GunVerse/Resources";
            EnsureFolder(resourcesFolder);

            Shader shader = Shader.Find(URP_UNLIT_SHADER);
            if (shader == null)
            {
                Debug.LogError("[GroceryStoreSetupBuilder] URP Unlit shader not found for spawn shield.");
                return;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(SPAWN_SHIELD_MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SPAWN_SHIELD_MATERIAL_PATH);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(0.16f, 0.52f, 1f, 0.22f));
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static Material GetOrCreateMaterial(string assetPath, Color color, float smoothness, float metallic, bool unlit)
        {
            Shader shader = Shader.Find(unlit ? URP_UNLIT_SHADER : URP_LIT_SHADER);
            if (shader == null)
            {
                Debug.LogError("[GroceryStoreSetupBuilder] Shader not found for " + assetPath);
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", color);
            if (!unlit)
            {
                material.SetFloat("_Smoothness", smoothness);
                material.SetFloat("_Metallic", metallic);
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
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
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
