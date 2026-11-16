using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class BopBuySetupBuilder
    {
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string LOBBY_SCENE_PATH = "Assets/_GunVerse/Scenes/Lobby.unity";
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string GUN_GAME_LADDER_PATH = "Assets/_GunVerse/Settings/GunGameLadder.asset";
        private const string SHOP_GLASS_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShopGlass.mat";

        private const string TV_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/TVGun.prefab";
        private const string TV32_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/TV32Gun.prefab";
        private const string TV65_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/TV65Gun.prefab";
        private const string TV75_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/TV75Gun.prefab";
        private const string BATTERY_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/BatteryGun.prefab";
        private const string SPEAKER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/SpeakerGun.prefab";
        private const string LARGE_SPEAKER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/LargeSpeakerGun.prefab";
        private const string KEYBOARD_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/KeyboardGun.prefab";
        private const string LAPTOP_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/LaptopGun.prefab";
        private const string PHONE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/PhoneGun.prefab";
        private const string FLIP_PHONE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/FlipPhoneGun.prefab";
        private const string PRINTER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/PrinterGun.prefab";
        private const string MONITOR_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/MonitorGun.prefab";
        private const string COMPUTER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/ComputerGun.prefab";
        private const string MOUSE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/MouseGun.prefab";
        private const string ROUTER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/RouterGun.prefab";
        private const string RADIO_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/RadioGun.prefab";
        private const string HEADPHONES_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/HeadphonesGun.prefab";
        private const string TOASTER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/ToasterGun.prefab";
        private const string MICROWAVE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/MicrowaveGun.prefab";
        private const string ELECTRIC_OVEN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/ElectricOvenGun.prefab";
        private const string WASHER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/WasherGun.prefab";
        private const string AIR_CONDITIONER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/AirConditionerGun.prefab";
        private const string STARLINK_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/StarlinkDishGun.prefab";
        private const string GIFT_BOX_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/GiftBoxGun.prefab";
        private const string TV_BOX_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/TVBoxGun.prefab";
        private const string INK_BOX_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/InkBoxGun.prefab";
        private const string TABLET_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/TabletGun.prefab";
        private const string DISPLAY_TABLE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/DisplayTableGun.prefab";
        private const string PLINTH_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/PlinthGun.prefab";
        private const string DISPLAY_LEDGE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/DisplayLedgeGun.prefab";
        private const string GREETER_TABLE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/GreeterTableGun.prefab";
        private const string PODIUM_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/PodiumGun.prefab";
        private const string STOOL_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/StoolGun.prefab";
        private const string STANCHION_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/StanchionGun.prefab";
        private const string CART_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/ShoppingCartGun.prefab";
        private const string CART_CORRAL_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/CartCorralGun.prefab";
        private const string EAS_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/EasPedestalGun.prefab";
        private const string COOLER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/BeverageCoolerGun.prefab";
        private const string LOCKER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/LockerGun.prefab";
        private const string SECURITY_CAMERA_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/SecurityCameraGun.prefab";
        private const string DOME_CAMERA_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/DomeCameraGun.prefab";
        private const string DOORBELL_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/DoorbellGun.prefab";
        private const string FAN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/DeskFanGun.prefab";
        private const string SECURITY_MONITOR_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/SecurityMonitorGun.prefab";
        private const string HIGH_TABLE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/HighTableGun.prefab";
        private const string WORKBENCH_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/WorkbenchGun.prefab";
        private const string PHONE_CASE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/PhoneCaseGun.prefab";
        private const string WALLET_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/WalletGun.prefab";
        private const string SCREEN_PROTECTOR_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/ScreenProtectorGun.prefab";
        private const string GUYSQUAD_COUNTER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/GuysquadCounterGun.prefab";
        private const string BOPBUY_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/BopBuySignGun.prefab";
        private const string GUYSQUAD_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/GuysquadSignGun.prefab";
        private const string MOBILE_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/MobileSignGun.prefab";
        private const string COMPUTERS_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/ComputersSignGun.prefab";
        private const string HOME_THEATRE_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/HomeTheatreSignGun.prefab";
        private const string APPLIANCES_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/AppliancesSignGun.prefab";
        private const string CHECKOUT_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/CheckoutSignGun.prefab";
        private const string SMART_HOME_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/SmartHomeSignGun.prefab";
        private const string CELL_PHONES_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/CellPhonesSignGun.prefab";
        private const string MOBILE_ACCESSORIES_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/MobileAccessoriesSignGun.prefab";
        private const string PRINTERS_INK_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/PrintersInkSignGun.prefab";
        private const string TABLETS_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/TabletsSignGun.prefab";
        private const string LAPTOPS_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/LaptopsSignGun.prefab";
        private const string HOLIDAY_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/HolidaySignGun.prefab";
        private const string GIFTS_SIGN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/GiftsSignGun.prefab";
        private const string SHELF_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/ShelfRunGun.prefab";
        private const string COUNTER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/CheckoutCounterGun.prefab";
        private const string REGISTER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/CashRegisterGun.prefab";
        private const string BELT_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/ConveyorBeltGun.prefab";
        private const string LANE_MARKER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/LaneMarkerGun.prefab";
        private const string CEILING_LIGHT_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/CeilingLightGun.prefab";
        private const string MAT_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/EntranceMatGun.prefab";

        private const float STORE_WIDTH = 78f;
        private const float STORE_LENGTH = 50f;
        private const float HALF_WIDTH = STORE_WIDTH * 0.5f;
        private const float HALF_LENGTH = STORE_LENGTH * 0.5f;
        private const float WALL_HEIGHT = 6.5f;
        private const float ENTRANCE_WIDTH = 12f;
        private const float ENTRANCE_HEIGHT = 4.4f;
        private const float COUNTER_HEIGHT = 1.16f;
        private const float COUNTER_CENTER_Y = COUNTER_HEIGHT * 0.5f;
        private const float COUNTER_SURFACE_Y = COUNTER_HEIGHT;
        private const float TABLE_SURFACE_Y = 0.9f;
        private const float PLINTH_SURFACE_Y = 0.54f;
        private const float SHELF_ROOT_Y = 1.1f;
        private const float SIGN_LEDGE_Y = 3.35f;
        private const float FIXTURE_Y = 4.9f;
        private const float TV_WALL_Z = 23.6f;
        private const float TV_BACKBOARD_Z = 24.65f;
        private const float SHELF_SIDE_OFFSET = 0.46f;
        private const float SHELF_PRODUCT_SPACING = 2.6f;
        private const int SHELF_BOARD_COUNT = 3;
        private const int SHELF_SIDE_COUNT = 2;
        private const int SHELF_SLOT_COUNT = 3;
        private const float PRODUCT_SURFACE_CLEARANCE = 0.02f;

        private static readonly float[] ShelfBoardSurfaceYs = { 0.42f, 0.96f, 1.5f };
        private static readonly float[] TvRowTops = { 0.64f, 1.64f, 2.64f, 3.64f };
        private static readonly float[][] TvRowPositions =
        {
            new[] { -24f, -16f, -8f, 0f, 8f },
            new[] { -16f, -8f, 0f, 8f, 16f },
            new[] { -24f, -8f, 8f, 24f },
            new[] { -16f, 0f, 16f, 24f }
        };

        private static readonly string[] TvPrefabPaths =
        {
            TV32_PREFAB_PATH,
            TV_PREFAB_PATH,
            TV65_PREFAB_PATH,
            TV75_PREFAB_PATH
        };

        private static readonly string[] MobileShelfProducts =
        {
            PHONE_PREFAB_PATH,
            FLIP_PHONE_PREFAB_PATH,
            PHONE_CASE_PREFAB_PATH,
            WALLET_PREFAB_PATH,
            SCREEN_PROTECTOR_PREFAB_PATH,
            HEADPHONES_PREFAB_PATH,
            BATTERY_PREFAB_PATH,
            GIFT_BOX_PREFAB_PATH
        };

        private static readonly string[] PcShelfProducts =
        {
            MONITOR_PREFAB_PATH,
            MOUSE_PREFAB_PATH,
            KEYBOARD_PREFAB_PATH,
            ROUTER_PREFAB_PATH,
            TABLET_PREFAB_PATH,
            INK_BOX_PREFAB_PATH,
            LAPTOP_PREFAB_PATH
        };

        private static readonly string[] ApplianceShelfProducts =
        {
            TOASTER_PREFAB_PATH,
            RADIO_PREFAB_PATH,
            HEADPHONES_PREFAB_PATH,
            BATTERY_PREFAB_PATH,
            SECURITY_CAMERA_PREFAB_PATH,
            DOORBELL_PREFAB_PATH
        };

        private static readonly string[] AudioShelfProducts =
        {
            SPEAKER_PREFAB_PATH,
            RADIO_PREFAB_PATH,
            HEADPHONES_PREFAB_PATH,
            GIFT_BOX_PREFAB_PATH,
            TV_BOX_PREFAB_PATH
        };

        private static readonly string[] StockShelfProducts =
        {
            PHONE_PREFAB_PATH,
            FLIP_PHONE_PREFAB_PATH,
            BATTERY_PREFAB_PATH,
            MOUSE_PREFAB_PATH,
            KEYBOARD_PREFAB_PATH,
            TABLET_PREFAB_PATH,
            INK_BOX_PREFAB_PATH,
            PHONE_CASE_PREFAB_PATH,
            WALLET_PREFAB_PATH,
            SCREEN_PROTECTOR_PREFAB_PATH
        };

        private static readonly Vector3[] SpawnPositions =
        {
            new Vector3(-34f, 0.05f, -21f),
            new Vector3(-22f, 0.05f, -21f),
            new Vector3(-8f, 0.05f, -21f),
            new Vector3(12f, 0.05f, -21f),
            new Vector3(22f, 0.05f, -21f),
            new Vector3(34f, 0.05f, -21f),
            new Vector3(-36f, 0.05f, -15f),
            new Vector3(-26f, 0.05f, -15f),
            new Vector3(-20f, 0.05f, -15f),
            new Vector3(20f, 0.05f, -18f),
            new Vector3(21f, 0.05f, -21f),
            new Vector3(32f, 0.05f, -18f),
            new Vector3(-12f, 0.05f, -15f),
            new Vector3(0f, 0.05f, -19f),
            new Vector3(12f, 0.05f, -15f),
            new Vector3(14f, 0.05f, -4f),
            new Vector3(22f, 0.05f, -4f),
            new Vector3(30f, 0.05f, -4f),
            new Vector3(34f, 0.05f, -6f),
            new Vector3(-34f, 0.05f, -4f),
            new Vector3(-28f, 0.05f, 5f),
            new Vector3(-34f, 0.05f, 15f),
            new Vector3(-26f, 0.05f, 21f),
            new Vector3(-12f, 0.05f, 19f),
            new Vector3(0f, 0.05f, 19f),
            new Vector3(12f, 0.05f, 19f),
            new Vector3(24f, 0.05f, 21f),
            new Vector3(0f, 0.05f, 12f)
        };

        private static readonly float[] SpawnYaws =
        {
            0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f,
            180f, 180f, 180f,
            0f, 0f, 0f,
            180f, 180f, 180f, 180f,
            0f, 0f, 0f, 0f,
            180f, 180f, 180f, 180f,
            180f
        };

        public static void Build()
        {
            BopBuyDesignBuilder.Build();
        }

        public static void BuildLegacyLayout()
        {
            EnsureSceneExists();
            Scene scene = EditorSceneManager.OpenScene(BOPBUY_SCENE_PATH, OpenSceneMode.Single);

            CleanupScene();
            EnsureGameplaySystems();

            Transform environment = CreateRoot("BopBuyEnvironment");
            Transform props = CreateRoot("BopBuyProps");
            Transform spawnRoot = CreateRoot("BopBuySpawnPoints");

            BuildShell(environment);
            BuildEntrance(environment, props);
            BuildCheckout(environment, props);
            BuildGuysquad(environment, props);
            BuildMobile(environment, props);
            BuildPc(environment, props);
            BuildAppliance(environment, props);
            BuildHomeTheatre(environment, props);
            BuildStockroom(environment, props);
            BuildDenseShelves(environment, props);
            BuildLighting(environment, props);
            BuildSpawnPoints(spawnRoot);
            ConfigureLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            ValidateBopBuy();

            ConfigureBuildSettings();
            PatchMainMenuLobby();
            PlaceGrocerySamples();

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            Debug.Log("[BopBuySetupBuilder] Restored the original 78 x 50 m layout with dense shelves.");
        }

        private static void EnsureSceneExists()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BOPBUY_SCENE_PATH) != null)
            {
                return;
            }

            if (!AssetDatabase.CopyAsset(GROCERY_STORE_SCENE_PATH, BOPBUY_SCENE_PATH))
            {
                Debug.LogError("[BopBuySetupBuilder] Failed to copy GroceryStore scene.");
                return;
            }

            AssetDatabase.ImportAsset(BOPBUY_SCENE_PATH);
        }

        private static void CleanupScene()
        {
            string[] removableRoots =
            {
                "StoreEnvironment",
                "LargeGunProps",
                "PickupItems",
                "StoreLighting",
                "SpawnPoints",
                "BopBuyEnvironment",
                "BopBuyProps",
                "BopBuySpawnPoints",
                "BopBuyGrocerySamples"
            };

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                foreach (string removable in removableRoots)
                {
                    if (root.name == removable)
                    {
                        Object.DestroyImmediate(root);
                        break;
                    }
                }
            }
        }

        private static void EnsureGameplaySystems()
        {
            GameModeController gameMode = Object.FindAnyObjectByType<GameModeController>();
            if (gameMode == null)
            {
                GameObject gameModeObject = new GameObject("GameMode");
                gameModeObject.AddComponent<NetworkObject>();
                gameMode = gameModeObject.AddComponent<GameModeController>();
            }

            GunGameLadder ladder = AssetDatabase.LoadAssetAtPath<GunGameLadder>(GUN_GAME_LADDER_PATH);
            SerializedObject gameModeObjectSerialized = new SerializedObject(gameMode);
            gameModeObjectSerialized.FindProperty("gunGameLadder").objectReferenceValue = ladder;
            gameModeObjectSerialized.ApplyModifiedProperties();

            PlayerSpawnManager spawnManager = Object.FindAnyObjectByType<PlayerSpawnManager>();
            if (spawnManager == null)
            {
                GameObject spawnManagerObject = new GameObject("PlayerSpawnManager");
                spawnManagerObject.AddComponent<NetworkObject>();
                spawnManager = spawnManagerObject.AddComponent<PlayerSpawnManager>();
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
            SerializedObject spawnManagerSerialized = new SerializedObject(spawnManager);
            spawnManagerSerialized.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            spawnManagerSerialized.ApplyModifiedProperties();
        }

        private static Transform CreateRoot(string name)
        {
            return new GameObject(name).transform;
        }

        private static void BuildShell(Transform environment)
        {
            CreateStaticBox(environment, "StoreFloor", new Vector3(0f, -0.1f, 0f), new Vector3(STORE_WIDTH, 0.2f, STORE_LENGTH), BopBuyMaterials.Floor);
            CreateStaticBox(environment, "StoreCeiling", new Vector3(0f, WALL_HEIGHT + 0.1f, 0f), new Vector3(STORE_WIDTH, 0.2f, STORE_LENGTH), BopBuyMaterials.Ceiling);
            CreateStaticBox(environment, "BackWall", new Vector3(0f, WALL_HEIGHT * 0.5f, HALF_LENGTH), new Vector3(STORE_WIDTH, WALL_HEIGHT, 0.3f), BopBuyMaterials.Wall);
            CreateStaticBox(environment, "LeftWall", new Vector3(-HALF_WIDTH, WALL_HEIGHT * 0.5f, 0f), new Vector3(0.3f, WALL_HEIGHT, STORE_LENGTH), BopBuyMaterials.Wall);
            CreateStaticBox(environment, "RightWall", new Vector3(HALF_WIDTH, WALL_HEIGHT * 0.5f, 0f), new Vector3(0.3f, WALL_HEIGHT, STORE_LENGTH), BopBuyMaterials.Wall);

            BuildEntranceShell(environment);
            BuildAccentArchitecture(environment);
        }

        private static void BuildEntranceShell(Transform environment)
        {
            float sideSegmentWidth = (STORE_WIDTH - ENTRANCE_WIDTH) * 0.5f;
            float sideSegmentCenter = (STORE_WIDTH + ENTRANCE_WIDTH) * 0.25f;
            CreateStaticBox(environment, "FrontWallLeft", new Vector3(-sideSegmentCenter, WALL_HEIGHT * 0.5f, -HALF_LENGTH), new Vector3(sideSegmentWidth, WALL_HEIGHT, 0.3f), BopBuyMaterials.Wall);
            CreateStaticBox(environment, "FrontWallRight", new Vector3(sideSegmentCenter, WALL_HEIGHT * 0.5f, -HALF_LENGTH), new Vector3(sideSegmentWidth, WALL_HEIGHT, 0.3f), BopBuyMaterials.Wall);
            CreateStaticBox(environment, "EntranceHeader", new Vector3(0f, ENTRANCE_HEIGHT + (WALL_HEIGHT - ENTRANCE_HEIGHT) * 0.5f, -HALF_LENGTH), new Vector3(ENTRANCE_WIDTH, WALL_HEIGHT - ENTRANCE_HEIGHT, 0.3f), BopBuyMaterials.Blue);

            Material glass = AssetDatabase.LoadAssetAtPath<Material>(SHOP_GLASS_MATERIAL_PATH);
            CreateStaticBox(environment, "SlidingDoorLeft", new Vector3(-ENTRANCE_WIDTH * 0.25f, ENTRANCE_HEIGHT * 0.5f, -HALF_LENGTH), new Vector3(ENTRANCE_WIDTH * 0.5f, ENTRANCE_HEIGHT, 0.08f), glass);
            CreateStaticBox(environment, "SlidingDoorRight", new Vector3(ENTRANCE_WIDTH * 0.25f, ENTRANCE_HEIGHT * 0.5f, -HALF_LENGTH), new Vector3(ENTRANCE_WIDTH * 0.5f, ENTRANCE_HEIGHT, 0.08f), glass);
            CreateStaticBox(environment, "EntranceMullion", new Vector3(0f, ENTRANCE_HEIGHT * 0.5f, -HALF_LENGTH), new Vector3(0.1f, ENTRANCE_HEIGHT, 0.12f), BopBuyMaterials.Metal);

            CreateStaticBox(environment, "EntranceSignSupport", new Vector3(0f, SIGN_LEDGE_Y - 0.06f, -24.4f), new Vector3(4.2f, 0.08f, 0.55f), BopBuyMaterials.Metal);
        }

        private static void BuildAccentArchitecture(Transform environment)
        {
            CreateStaticBox(environment, "YellowBandLeft", new Vector3(-HALF_WIDTH + 0.2f, 3.3f, 0f), new Vector3(0.08f, 0.28f, STORE_LENGTH - 1f), BopBuyMaterials.Yellow);
            CreateStaticBox(environment, "YellowBandRight", new Vector3(HALF_WIDTH - 0.2f, 3.3f, 0f), new Vector3(0.08f, 0.28f, STORE_LENGTH - 1f), BopBuyMaterials.Yellow);
            CreateStaticBox(environment, "YellowBandBack", new Vector3(0f, 3.3f, HALF_LENGTH - 0.2f), new Vector3(STORE_WIDTH - 1f, 0.28f, 0.08f), BopBuyMaterials.Yellow);
            CreateStaticBox(environment, "CheckoutBluePanel", new Vector3(-38.55f, 2.3f, -18f), new Vector3(0.12f, 2.8f, 12f), BopBuyMaterials.Blue);
            CreateStaticBox(environment, "GuysquadBluePanel", new Vector3(38.55f, 2.3f, -17f), new Vector3(0.12f, 2.8f, 12f), BopBuyMaterials.Blue);

            CreateStaticBox(environment, "StockroomWallLeft", new Vector3(28f, WALL_HEIGHT * 0.5f, 18f), new Vector3(0.2f, WALL_HEIGHT, 12f), BopBuyMaterials.Wall);
            CreateStaticBox(environment, "StockroomWallFrontLeft", new Vector3(29.5f, WALL_HEIGHT * 0.5f, 12f), new Vector3(3f, WALL_HEIGHT, 0.2f), BopBuyMaterials.Wall);
            CreateStaticBox(environment, "StockroomWallFrontRight", new Vector3(35f, WALL_HEIGHT * 0.5f, 12f), new Vector3(4f, WALL_HEIGHT, 0.2f), BopBuyMaterials.Wall);
        }

        private static void BuildEntrance(Transform environment, Transform props)
        {
            PlaceOnSurface(BOPBUY_SIGN_PREFAB_PATH, "BopBuy Entrance Sign", props, new Vector3(0f, 0f, -24.4f), SIGN_LEDGE_Y, Quaternion.identity);
            PlaceGunAt(MAT_PREFAB_PATH, "Entrance Mat", props, new Vector3(0f, 0.03f, -23.6f), Quaternion.identity);

            PlaceGunAt(EAS_PREFAB_PATH, "Security Pedestal 1", props, new Vector3(-2f, 0.8f, -22.5f), Quaternion.identity);
            PlaceGunAt(EAS_PREFAB_PATH, "Security Pedestal 2", props, new Vector3(0f, 0.8f, -22.5f), Quaternion.identity);
            PlaceGunAt(EAS_PREFAB_PATH, "Security Pedestal 3", props, new Vector3(2f, 0.8f, -22.5f), Quaternion.identity);

            PlaceGunAt(CART_PREFAB_PATH, "Shopping Cart 1", props, new Vector3(-10f, 0.45f, -22.6f), Quaternion.Euler(0f, 25f, 0f));
            PlaceGunAt(CART_PREFAB_PATH, "Shopping Cart 2", props, new Vector3(-10f, 0.45f, -21.8f), Quaternion.Euler(0f, 20f, 0f));
            PlaceGunAt(CART_CORRAL_PREFAB_PATH, "Cart Corral", props, new Vector3(-12.5f, 0.55f, -22.2f), Quaternion.Euler(0f, 90f, 0f));

            PlaceGunAt(GREETER_TABLE_PREFAB_PATH, "Greeter Table", props, new Vector3(8.5f, 0.45f, -21.8f), Quaternion.Euler(0f, -20f, 0f));
            PlaceGunAt(PODIUM_PREFAB_PATH, "Greeter Podium", props, new Vector3(10.5f, 0.55f, -20.8f), Quaternion.identity);
            PlaceOnSurface(SECURITY_MONITOR_PREFAB_PATH, "Greeter Security Monitor", props, new Vector3(10.4f, 0f, -21.5f), 1.15f, Quaternion.Euler(0f, 180f, 0f));
            PlaceOnSurface(FAN_PREFAB_PATH, "Greeter Fan", props, new Vector3(9.2f, 0f, -21.8f), 1.15f, Quaternion.identity);
            PlaceGunAt(STOOL_PREFAB_PATH, "Greeter Stool", props, new Vector3(8.6f, 0.38f, -20.8f), Quaternion.Euler(0f, 180f, 0f));
            PlaceGunAt(STANCHION_PREFAB_PATH, "Greeter Stanchion 1", props, new Vector3(6.2f, 0.48f, -21.2f), Quaternion.identity);
            PlaceGunAt(STANCHION_PREFAB_PATH, "Greeter Stanchion 2", props, new Vector3(6.2f, 0.48f, -18.8f), Quaternion.identity);
            PlaceGunAt(DOME_CAMERA_PREFAB_PATH, "Entrance Dome Camera", props, new Vector3(0f, 5.7f, -23.8f), Quaternion.identity);

            PlaceSign(environment, props, CHECKOUT_SIGN_PREFAB_PATH, "Checkout Sign", new Vector3(-30f, 0f, -24.25f), 0f, SIGN_LEDGE_Y);
            PlaceSign(environment, props, MOBILE_SIGN_PREFAB_PATH, "Mobile Sign", new Vector3(0f, 0f, -24.25f), 0f, SIGN_LEDGE_Y);
            PlaceSign(environment, props, GUYSQUAD_SIGN_PREFAB_PATH, "Guysquad Sign", new Vector3(28f, 0f, -24.25f), 0f, SIGN_LEDGE_Y);
        }

        private static void BuildCheckout(Transform environment, Transform props)
        {
            for (int index = 0; index < 5; index++)
            {
                float x = -34f + index * 5f;
                PlaceGunAt(COUNTER_PREFAB_PATH, "Checkout Counter " + (index + 1), props, new Vector3(x, COUNTER_CENTER_Y, -18f), Quaternion.Euler(0f, 90f, 0f));
                PlaceOnSurface(REGISTER_PREFAB_PATH, "Checkout Register " + (index + 1), props, new Vector3(x, 0f, -18f), COUNTER_SURFACE_Y, Quaternion.Euler(0f, 90f, 0f));
                PlaceOnSurface(MONITOR_PREFAB_PATH, "Checkout Monitor " + (index + 1), props, new Vector3(x, 0f, -19.2f), COUNTER_SURFACE_Y, Quaternion.identity);
                PlaceOnSurface(KEYBOARD_PREFAB_PATH, "Checkout Keyboard " + (index + 1), props, new Vector3(x - 0.3f, 0f, -17.5f), COUNTER_SURFACE_Y, Quaternion.identity);
                PlaceOnSurface(FAN_PREFAB_PATH, "Checkout Fan " + (index + 1), props, new Vector3(x + 0.35f, 0f, -19f), COUNTER_SURFACE_Y, Quaternion.identity);
                PlaceOnSurface(BELT_PREFAB_PATH, "Checkout Belt " + (index + 1), props, new Vector3(x, 0f, -16.8f), COUNTER_SURFACE_Y, Quaternion.Euler(0f, 90f, 0f));
                PlaceOnSurface(LANE_MARKER_PREFAB_PATH, "Checkout Lane Marker " + (index + 1), props, new Vector3(x + 0.8f, 0f, -20.4f), 0.02f, Quaternion.identity);
            }

            for (int index = 0; index < 3; index++)
            {
                PlaceGunAt(STANCHION_PREFAB_PATH, "Checkout Stanchion " + (index + 1), props, new Vector3(-32f + index * 4f, 0.48f, -14.5f), Quaternion.identity);
            }

            PlaceGunAt(COOLER_PREFAB_PATH, "Beverage Cooler", props, new Vector3(-36.5f, 0.95f, -12.5f), Quaternion.Euler(0f, 90f, 0f));
            PlaceSign(environment, props, CHECKOUT_SIGN_PREFAB_PATH, "Checkout Wall Sign", new Vector3(-36.5f, 0f, -21f), 90f, SIGN_LEDGE_Y);
        }

        private static void BuildGuysquad(Transform environment, Transform props)
        {
            CreateStaticBox(environment, "GuysquadLeftWallFront", new Vector3(18f, WALL_HEIGHT * 0.5f, -22.5f), new Vector3(0.2f, WALL_HEIGHT, 5f), BopBuyMaterials.Wall);
            CreateStaticBox(environment, "GuysquadLeftWallBack", new Vector3(18f, WALL_HEIGHT * 0.5f, -15.5f), new Vector3(0.2f, WALL_HEIGHT, 5f), BopBuyMaterials.Wall);
            CreateStaticBox(environment, "GuysquadBackWall", new Vector3(28f, WALL_HEIGHT * 0.5f, -13f), new Vector3(20f, WALL_HEIGHT, 0.2f), BopBuyMaterials.Wall);
            PlaceGunAt(GUYSQUAD_COUNTER_PREFAB_PATH, "Guysquad Service Counter", props, new Vector3(23f, COUNTER_CENTER_Y, -17f), Quaternion.Euler(0f, 90f, 0f));
            PlaceGunAt(GUYSQUAD_COUNTER_PREFAB_PATH, "Guysquad Checkout Counter", props, new Vector3(28f, COUNTER_CENTER_Y, -17f), Quaternion.Euler(0f, 90f, 0f));
            PlaceOnSurface(REGISTER_PREFAB_PATH, "Guysquad Register", props, new Vector3(28f, 0f, -17f), COUNTER_SURFACE_Y, Quaternion.Euler(0f, 90f, 0f));
            PlaceOnSurface(MONITOR_PREFAB_PATH, "Guysquad Monitor", props, new Vector3(23f, 0f, -18f), COUNTER_SURFACE_Y, Quaternion.identity);
            PlaceOnSurface(PRINTER_PREFAB_PATH, "Guysquad Printer", props, new Vector3(25f, 0f, -17f), COUNTER_SURFACE_Y, Quaternion.identity);
            PlaceOnSurface(LAPTOP_PREFAB_PATH, "Guysquad Laptop", props, new Vector3(26f, 0f, -18.2f), COUNTER_SURFACE_Y, Quaternion.Euler(0f, 20f, 0f));

            for (int index = 0; index < 4; index++)
            {
                PlaceGunAt(LOCKER_PREFAB_PATH, "Guysquad Locker " + (index + 1), props, new Vector3(35f, 0.9f, -20f + index * 2.2f), Quaternion.Euler(0f, 90f, 0f));
            }

            PlaceGunAt(STANCHION_PREFAB_PATH, "Guysquad Stanchion 1", props, new Vector3(20f, 0.48f, -19f), Quaternion.identity);
            PlaceGunAt(STANCHION_PREFAB_PATH, "Guysquad Stanchion 2", props, new Vector3(20f, 0.48f, -16f), Quaternion.identity);
            PlaceSign(environment, props, GUYSQUAD_SIGN_PREFAB_PATH, "Guysquad Room Sign", new Vector3(28f, 0f, -13.3f), 180f, SIGN_LEDGE_Y);
        }

        private static void BuildMobile(Transform environment, Transform props)
        {
            for (int index = 0; index < 3; index++)
            {
                float x = -8f + index * 8f;
                PlaceGunAt(DISPLAY_TABLE_PREFAB_PATH, "Mobile Table " + (index + 1), props, new Vector3(x, 0.45f, -14f), Quaternion.identity);
                for (int phoneIndex = 0; phoneIndex < 5; phoneIndex++)
                {
                    PlaceOnSurface(
                        phoneIndex % 2 == 0 ? PHONE_PREFAB_PATH : FLIP_PHONE_PREFAB_PATH,
                        "Mobile Phone " + (index * 5 + phoneIndex + 1),
                        props,
                        new Vector3(x - 1f + phoneIndex * 0.5f, 0f, -14f),
                        TABLE_SURFACE_Y,
                        Quaternion.Euler(0f, 180f, 0f));
                }
            }

            PlaceGunAt(COUNTER_PREFAB_PATH, "Mobile Staff Counter", props, new Vector3(0f, COUNTER_CENTER_Y, -11f), Quaternion.identity);
            PlaceOnSurface(REGISTER_PREFAB_PATH, "Mobile Register", props, new Vector3(1.1f, 0f, -11f), COUNTER_SURFACE_Y, Quaternion.identity);
            PlaceGunAt(DISPLAY_TABLE_PREFAB_PATH, "Satellite Demo Table", props, new Vector3(14f, 0.45f, -10f), Quaternion.identity);
            PlaceOnSurface(STARLINK_PREFAB_PATH, "Starlink Display", props, new Vector3(14f, 0f, -10f), TABLE_SURFACE_Y, Quaternion.identity);
            PlaceSign(environment, props, MOBILE_SIGN_PREFAB_PATH, "Mobile Wall Sign", new Vector3(0f, 0f, -10.3f), 180f, SIGN_LEDGE_Y);
        }

        private static void BuildPc(Transform environment, Transform props)
        {
            float[] tableX = { 12f, 20f, 28f, 36f };
            for (int index = 0; index < tableX.Length; index++)
            {
                float x = tableX[index];
                PlaceGunAt(DISPLAY_TABLE_PREFAB_PATH, "PC Table " + (index + 1), props, new Vector3(x, 0.45f, -3f), Quaternion.identity);
                PlaceOnSurface(MONITOR_PREFAB_PATH, "PC Monitor " + (index + 1), props, new Vector3(x - 0.8f, 0f, -3.2f), TABLE_SURFACE_Y, Quaternion.identity);
                PlaceOnSurface(COMPUTER_PREFAB_PATH, "PC Computer " + (index + 1), props, new Vector3(x + 0.8f, 0f, -3.2f), TABLE_SURFACE_Y, Quaternion.identity);
                PlaceOnSurface(KEYBOARD_PREFAB_PATH, "PC Keyboard " + (index + 1), props, new Vector3(x - 0.6f, 0f, -2.7f), TABLE_SURFACE_Y, Quaternion.identity);
                PlaceOnSurface(MOUSE_PREFAB_PATH, "PC Mouse " + (index + 1), props, new Vector3(x + 0.6f, 0f, -2.7f), TABLE_SURFACE_Y, Quaternion.identity);
            }

            PlaceSign(environment, props, COMPUTERS_SIGN_PREFAB_PATH, "Computers Sign", new Vector3(24f, 0f, -6f), 180f, SIGN_LEDGE_Y);
            PlaceSign(environment, props, TABLETS_SIGN_PREFAB_PATH, "Tablets Sign", new Vector3(32f, 0f, -6f), 180f, SIGN_LEDGE_Y);
        }

        private static void BuildAppliance(Transform environment, Transform props)
        {
            for (int index = 0; index < 4; index++)
            {
                float x = -34f + index * 4f;
                PlaceGunAt(PLINTH_PREFAB_PATH, "Appliance Plinth " + (index + 1), props, new Vector3(x, 0.27f, 12f), Quaternion.identity);
                PlaceOnSurface(
                    index % 4 == 0 ? WASHER_PREFAB_PATH : index % 4 == 1 ? ELECTRIC_OVEN_PREFAB_PATH : index % 4 == 2 ? AIR_CONDITIONER_PREFAB_PATH : MICROWAVE_PREFAB_PATH,
                    "Appliance " + (index + 1),
                    props,
                    new Vector3(x, 0f, 12f),
                    PLINTH_SURFACE_Y,
                    Quaternion.identity);
            }

            for (int index = 0; index < 4; index++)
            {
                float x = -34f + index * 4f;
                PlaceGunAt(PLINTH_PREFAB_PATH, "Appliance Plinth " + (index + 5), props, new Vector3(x, 0.27f, 18f), Quaternion.identity);
                PlaceOnSurface(
                    index % 4 == 0 ? MICROWAVE_PREFAB_PATH : index % 4 == 1 ? TOASTER_PREFAB_PATH : index % 4 == 2 ? RADIO_PREFAB_PATH : TOASTER_PREFAB_PATH,
                    "Appliance " + (index + 5),
                    props,
                    new Vector3(x, 0f, 18f),
                    PLINTH_SURFACE_Y,
                    Quaternion.identity);
            }

            PlaceGunAt(DISPLAY_TABLE_PREFAB_PATH, "Appliance Display Table", props, new Vector3(-30f, 0.45f, 7f), Quaternion.Euler(0f, 90f, 0f));
            PlaceSign(environment, props, APPLIANCES_SIGN_PREFAB_PATH, "Appliances Sign", new Vector3(-37.8f, 0f, 14f), 90f, SIGN_LEDGE_Y);
        }

        private static void BuildHomeTheatre(Transform environment, Transform props)
        {
            CreateStaticBox(environment, "TvWallBackboard", new Vector3(0f, 2.8f, TV_BACKBOARD_Z), new Vector3(56f, 5.6f, 0.2f), BopBuyMaterials.Blue);
            CreateStaticBox(environment, "TvWallSoffit", new Vector3(0f, 5.2f, TV_BACKBOARD_Z - 0.2f), new Vector3(56f, 0.35f, 0.3f), BopBuyMaterials.Blue);

            int tvIndex = 0;
            for (int row = 0; row < TvRowTops.Length; row++)
            {
                foreach (float x in TvRowPositions[row])
                {
                    PlaceGunAt(DISPLAY_LEDGE_PREFAB_PATH, "TV Ledge " + (tvIndex + 1), props, new Vector3(x, TvRowTops[row] - 0.04f, TV_WALL_Z + 0.3f), Quaternion.identity);
                    PlaceOnSurface(
                        TvPrefabPaths[tvIndex % TvPrefabPaths.Length],
                        "TV Wall " + (++tvIndex),
                        props,
                        new Vector3(x, 0f, TV_WALL_Z),
                        TvRowTops[row],
                        Quaternion.Euler(0f, 180f, 0f));
                }
            }

            PlaceGunAt(DISPLAY_TABLE_PREFAB_PATH, "Home Theatre Demo Table", props, new Vector3(-8f, 0.45f, 15f), Quaternion.Euler(0f, 90f, 0f));
            PlaceOnSurface(LARGE_SPEAKER_PREFAB_PATH, "Home Theatre Speaker 1", props, new Vector3(-8f, 0f, 15f), TABLE_SURFACE_Y, Quaternion.identity);
            PlaceOnSurface(LARGE_SPEAKER_PREFAB_PATH, "Home Theatre Speaker 2", props, new Vector3(-8f, 0f, 16f), TABLE_SURFACE_Y, Quaternion.identity);
            PlaceOnSurface(TV_BOX_PREFAB_PATH, "Home Theatre TV Box 1", props, new Vector3(-6f, 0f, 13f), 0f, Quaternion.identity);
            PlaceOnSurface(TV_BOX_PREFAB_PATH, "Home Theatre TV Box 2", props, new Vector3(-5f, 0f, 13.6f), 0f, Quaternion.identity);
            PlaceSign(environment, props, HOME_THEATRE_SIGN_PREFAB_PATH, "Home Theatre Sign", new Vector3(0f, 0f, 24.35f), 180f, 4.65f);
        }

        private static void BuildStockroom(Transform environment, Transform props)
        {
            PlaceGunAt(WORKBENCH_PREFAB_PATH, "Stockroom Workbench", props, new Vector3(29.2f, 0.45f, 16f), Quaternion.Euler(0f, 90f, 0f));
            PlaceGunAt(LOCKER_PREFAB_PATH, "Stockroom Locker 1", props, new Vector3(29f, 0.9f, 20f), Quaternion.Euler(0f, 90f, 0f));
            PlaceGunAt(LOCKER_PREFAB_PATH, "Stockroom Locker 2", props, new Vector3(29f, 0.9f, 22f), Quaternion.Euler(0f, 90f, 0f));
        }

        private static void BuildDenseShelves(Transform environment, Transform props)
        {
            BuildShelfRun(environment, props, new Vector3(-32f, SHELF_ROOT_Y, 1f), "Appliance Shelf 1", ApplianceShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(-25f, SHELF_ROOT_Y, 1f), "Appliance Shelf 2", ApplianceShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(-18f, SHELF_ROOT_Y, 1f), "Appliance Shelf 3", ApplianceShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(16f, SHELF_ROOT_Y, 2f), "PC Shelf 1", PcShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(24f, SHELF_ROOT_Y, 2f), "PC Shelf 2", PcShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(32f, SHELF_ROOT_Y, 2f), "PC Shelf 3", PcShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(-8f, SHELF_ROOT_Y, 6f), "Mobile Shelf 1", MobileShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(8f, SHELF_ROOT_Y, 6f), "Mobile Shelf 2", MobileShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(-12f, SHELF_ROOT_Y, 13f), "Audio Shelf 1", AudioShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(14f, SHELF_ROOT_Y, 13f), "Audio Shelf 2", AudioShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(0f, SHELF_ROOT_Y, -5f), "Center Endcap 1", StockShelfProducts, 90f);
            BuildShelfRun(environment, props, new Vector3(0f, SHELF_ROOT_Y, 3f), "Center Endcap 2", StockShelfProducts, 90f);
            BuildShelfRun(environment, props, new Vector3(0f, SHELF_ROOT_Y, 10f), "Center Endcap 3", StockShelfProducts, 90f);
            BuildShelfRun(environment, props, new Vector3(0f, SHELF_ROOT_Y, 16f), "Center Endcap 4", StockShelfProducts, 90f);
            BuildShelfRun(environment, props, new Vector3(-11f, SHELF_ROOT_Y, 1f), "Appliance Shelf 4", ApplianceShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(36f, SHELF_ROOT_Y, 3.5f), "PC Shelf 4", PcShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(-18f, SHELF_ROOT_Y, 7f), "Stock Shelf 1", StockShelfProducts, 90f);
            BuildShelfRun(environment, props, new Vector3(31f, SHELF_ROOT_Y, 19f), "Stock Shelf 2", StockShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(35f, SHELF_ROOT_Y, 19f), "Stock Shelf 3", StockShelfProducts, 0f);
            BuildShelfRun(environment, props, new Vector3(-8f, SHELF_ROOT_Y, -8f), "Mobile Endcap", MobileShelfProducts, 90f);
        }

        private static void BuildLighting(Transform environment, Transform props)
        {
            float[] xPositions = { -24f, -8f, 8f, 24f };
            float[] zPositions = { -18f, 0f, 18f };
            int index = 0;

            foreach (float x in xPositions)
            {
                foreach (float z in zPositions)
                {
                    index++;
                    CreateStaticBox(environment, "LightSupport", new Vector3(x, FIXTURE_Y + 0.18f, z), new Vector3(0.08f, 0.02f, 3.5f), BopBuyMaterials.Metal);
                    PlaceGunAt(CEILING_LIGHT_PREFAB_PATH, "Ceiling Light " + index, props, new Vector3(x, FIXTURE_Y, z), Quaternion.identity);
                }
            }
        }

        private static void BuildSpawnPoints(Transform spawnRoot)
        {
            for (int index = 0; index < SpawnPositions.Length; index++)
            {
                GameObject spawnPointObject = new GameObject("Spawn Point " + (index + 1));
                spawnPointObject.transform.SetParent(spawnRoot, false);
                spawnPointObject.transform.position = SpawnPositions[index];
                spawnPointObject.transform.rotation = Quaternion.Euler(0f, SpawnYaws[index], 0f);
                spawnPointObject.AddComponent<PlayerSpawnPoint>();
            }
        }

        private static void ConfigureLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ArtPalette.BopBuy.Ambient;
            RenderSettings.skybox = null;

            Light directional = null;
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.type == LightType.Directional)
                {
                    directional = light;
                    break;
                }
            }

            if (directional != null)
            {
                directional.color = ArtPalette.BopBuy.KeyLight;
                directional.intensity = 1.1f;
                directional.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            }
        }

        private static void BuildShelfRun(Transform environment, Transform props, Vector3 position, string name, string[] productPaths, float yaw)
        {
            Quaternion shelfRotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject shelf = PlaceGunAt(SHELF_PREFAB_PATH, name, props, position, shelfRotation);
            if (shelf == null)
            {
                return;
            }

            SetFixedInWorld(shelf, true);

            for (int boardIndex = 0; boardIndex < SHELF_BOARD_COUNT; boardIndex++)
            {
                for (int sideIndex = 0; sideIndex < SHELF_SIDE_COUNT; sideIndex++)
                {
                    int side = sideIndex == 0 ? -1 : 1;
                    for (int slotIndex = 0; slotIndex < SHELF_SLOT_COUNT; slotIndex++)
                    {
                        string prefabPath = productPaths[(boardIndex + sideIndex + slotIndex) % productPaths.Length];
                        Vector3 localOffset = new Vector3(side * SHELF_SIDE_OFFSET, 0f, (slotIndex - 1) * SHELF_PRODUCT_SPACING);
                        Vector3 productPosition = position + shelfRotation * localOffset;
                        Quaternion productRotation = shelfRotation * Quaternion.Euler(0f, side < 0 ? -90f : 90f, 0f);
                        PlaceOnSurface(prefabPath, name + " Product", props, productPosition, ShelfBoardSurfaceYs[boardIndex], productRotation);
                    }
                }
            }
        }

        private static GameObject PlaceSign(Transform environment, Transform props, string prefabPath, string name, Vector3 position, float yaw, float surfaceY)
        {
            CreateStaticBox(environment, name + " Support", new Vector3(position.x, surfaceY - 0.06f, position.z), new Vector3(3.2f, 0.08f, 0.5f), BopBuyMaterials.Metal);
            return PlaceOnSurface(prefabPath, name, props, position, surfaceY, Quaternion.Euler(0f, yaw, 0f));
        }

        private static GameObject PlaceGunAt(string prefabPath, string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[BopBuySetupBuilder] Prefab not found: " + prefabPath);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static GameObject PlaceOnSurface(string prefabPath, string name, Transform parent, Vector3 position, float surfaceY, Quaternion rotation)
        {
            GameObject instance = PlaceGunAt(prefabPath, name, parent, position, rotation);
            if (instance == null)
            {
                return null;
            }

            Physics.SyncTransforms();
            Bounds bounds = GetPlacementBounds(instance);
            instance.transform.position = new Vector3(position.x, surfaceY - bounds.min.y + PRODUCT_SURFACE_CLEARANCE, position.z);
            return instance;
        }

        private static Bounds GetPlacementBounds(GameObject root)
        {
            Bounds bounds = GetRendererBounds(root);
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider.enabled)
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return bounds;
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one * 0.1f);
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void SetFixedInWorld(GameObject instance, bool value)
        {
            GunItem gunItem = instance.GetComponent<GunItem>();
            if (gunItem == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(gunItem);
            SerializedProperty property = serializedObject.FindProperty("fixedInWorld");
            if (property == null)
            {
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateStaticBox(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = size;
            box.isStatic = true;

            MeshRenderer renderer = box.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return box;
        }

        private static void ValidateBopBuy()
        {
            int propCount = 0;
            foreach (GunItem gunItem in Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (gunItem.transform.root.name == "BopBuyProps")
                {
                    propCount++;
                }
            }

            int spawnCount = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include).Length;
            Debug.Log($"[BopBuySetupBuilder] BopBuy validation: {propCount} props, {spawnCount} spawns.");
            ValidateShelfClearance();

            if (propCount < 500)
            {
                Debug.LogWarning("[BopBuySetupBuilder] BopBuy prop count is below the density target.");
            }

            if (spawnCount != SpawnPositions.Length)
            {
                Debug.LogWarning("[BopBuySetupBuilder] BopBuy spawn count does not match the configured layout.");
            }
        }

        private static void ValidateShelfClearance()
        {
            var shelves = new System.Collections.Generic.List<GunItem>();
            foreach (GunItem gunItem in Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (gunItem.transform.root.name != "BopBuyProps")
                {
                    continue;
                }

                if ((gunItem.name.Contains("Shelf") || gunItem.name.Contains("Endcap")) &&
                    !gunItem.name.Contains("Product"))
                {
                    shelves.Add(gunItem);
                }
            }

            int collisionCount = 0;
            for (int leftIndex = 0; leftIndex < shelves.Count; leftIndex++)
            {
                Bounds leftBounds = GetRendererBounds(shelves[leftIndex].gameObject);
                for (int rightIndex = leftIndex + 1; rightIndex < shelves.Count; rightIndex++)
                {
                    Bounds rightBounds = GetRendererBounds(shelves[rightIndex].gameObject);
                    if (leftBounds.Intersects(rightBounds))
                    {
                        collisionCount++;
                        Debug.LogError($"[BopBuySetupBuilder] Shelf overlap: {shelves[leftIndex].name} vs {shelves[rightIndex].name}.");
                    }
                }

                foreach (GunItem fixture in Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
                {
                    if (fixture.transform.root.name != "BopBuyProps" ||
                        fixture == shelves[leftIndex] ||
                        fixture.name.Contains("Product") ||
                        fixture.name.Contains("Shelf") ||
                        fixture.name.Contains("Endcap"))
                    {
                        continue;
                    }

                    Bounds fixtureBounds = GetRendererBounds(fixture.gameObject);
                    if (leftBounds.Intersects(fixtureBounds))
                    {
                        collisionCount++;
                        Debug.LogError($"[BopBuySetupBuilder] Shelf fixture overlap: {shelves[leftIndex].name} vs {fixture.name}.");
                    }
                }
            }

            Debug.Log($"[BopBuySetupBuilder] Shelf clearance validation: {shelves.Count} shelves, {collisionCount} overlaps.");
        }

        private static void PlaceGrocerySamples()
        {
            Scene scene = EditorSceneManager.OpenScene(GROCERY_STORE_SCENE_PATH, OpenSceneMode.Single);
            GameObject existing = GameObject.Find("BopBuyGrocerySamples");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            Transform samples = CreateRoot("BopBuyGrocerySamples");
            PlaceOnSurface(BATTERY_PREFAB_PATH, "BopBuy Sample Battery", samples, new Vector3(-2f, 0f, -24.2f), 0f, Quaternion.identity);
            PlaceOnSurface(PHONE_PREFAB_PATH, "BopBuy Sample Phone", samples, new Vector3(2f, 0f, -24.2f), 0f, Quaternion.identity);
            PlaceOnSurface(KEYBOARD_PREFAB_PATH, "BopBuy Sample Keyboard", samples, new Vector3(0f, 0f, -23.2f), 0f, Quaternion.identity);
            PlaceOnSurface(TV_PREFAB_PATH, "BopBuy Sample TV", samples, new Vector3(0f, 0f, 20f), 0f, Quaternion.identity);
            PlaceOnSurface(SPEAKER_PREFAB_PATH, "BopBuy Sample Speaker", samples, new Vector3(-10f, 0f, 20f), 0f, Quaternion.identity);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
                new EditorBuildSettingsScene(LOBBY_SCENE_PATH, true),
                new EditorBuildSettingsScene(GROCERY_STORE_SCENE_PATH, true),
                new EditorBuildSettingsScene(BOPBUY_SCENE_PATH, true)
            };
        }

        private static void PatchMainMenuLobby()
        {
            Scene scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
            if (controller == null)
            {
                Debug.LogError("[BopBuySetupBuilder] MainMenuController not found.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            Button gunGameButton = serializedObject.FindProperty("lobbyGunGameButton").objectReferenceValue as Button;
            Button mapButton = serializedObject.FindProperty("lobbyMapButton").objectReferenceValue as Button;

            if (mapButton == null && gunGameButton != null)
            {
                GameObject clone = Object.Instantiate(gunGameButton.gameObject, gunGameButton.transform.parent);
                clone.name = "MapButton";
                clone.transform.SetSiblingIndex(gunGameButton.transform.GetSiblingIndex() + 1);
                TextMeshProUGUI label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "Map: Grocery Store";
                }

                mapButton = clone.GetComponent<Button>();
                serializedObject.FindProperty("lobbyMapButton").objectReferenceValue = mapButton;
                serializedObject.ApplyModifiedProperties();
            }

            RectTransform content = mapButton != null ? mapButton.transform.parent as RectTransform : null;
            if (content != null)
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, 1100f);
                VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 10f;
                }
            }

            EditorUtility.SetDirty(controller);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
