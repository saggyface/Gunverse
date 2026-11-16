using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static partial class BopBuyDesignBuilder
    {
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string DESIGN_SCENE_PATH = "Assets/_GunVerse/Scenes/Design/MapDesignTemplate.unity";
        private const string BACKUP_FOLDER = "Assets/_GunVerse/Scenes/Backup";
        private const string BACKUP_SCENE_PATH = BACKUP_FOLDER + "/BopBuy_PreDesignBackup.unity";
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string GUN_GAME_LADDER_PATH = "Assets/_GunVerse/Settings/GunGameLadder.asset";
        private const string SHOP_GLASS_MATERIAL_PATH = "Assets/_GunVerse/Materials/ShopGlass.mat";

        private const string ENTRANCE_MAT_PREFAB = "Assets/_GunVerse/Prefabs/BopBuy/EntranceMatGun_BopBuy.prefab";
        private const string CHECKOUT_COUNTER_PREFAB = "Assets/_GunVerse/Prefabs/BopBuy/CheckoutCounterGun_BopBuy.prefab";
        private const string REGISTER_PREFAB = "Assets/_GunVerse/Prefabs/BopBuy/CashRegisterGun_BopBuy.prefab";
        private const string MONITOR_PREFAB = "Assets/_GunVerse/Prefabs/Items/MonitorGun.prefab";
        private const string KEYBOARD_PREFAB = "Assets/_GunVerse/Prefabs/Items/KeyboardGun.prefab";
        private const string MOUSE_PREFAB = "Assets/_GunVerse/Prefabs/Items/MouseGun.prefab";
        private const string COMPUTER_PREFAB = "Assets/_GunVerse/Prefabs/Items/ComputerGun.prefab";
        private const string LAPTOP_PREFAB = "Assets/_GunVerse/Prefabs/Items/LaptopGun.prefab";
        private const string TABLET_PREFAB = "Assets/_GunVerse/Prefabs/Items/TabletGun.prefab";
        private const string PHONE_PREFAB = "Assets/_GunVerse/Prefabs/Items/PhoneGun.prefab";
        private const string FLIP_PHONE_PREFAB = "Assets/_GunVerse/Prefabs/Items/FlipPhoneGun.prefab";
        private const string PHONE_CASE_PREFAB = "Assets/_GunVerse/Prefabs/Items/PhoneCaseGun.prefab";
        private const string WALLET_PREFAB = "Assets/_GunVerse/Prefabs/Items/WalletGun.prefab";
        private const string SCREEN_PROTECTOR_PREFAB = "Assets/_GunVerse/Prefabs/Items/ScreenProtectorGun.prefab";
        private const string HEADPHONES_PREFAB = "Assets/_GunVerse/Prefabs/Items/HeadphonesGun.prefab";
        private const string BATTERY_PREFAB = "Assets/_GunVerse/Prefabs/Items/BatteryGun.prefab";
        private const string ROUTER_PREFAB = "Assets/_GunVerse/Prefabs/Items/RouterGun.prefab";
        private const string INK_BOX_PREFAB = "Assets/_GunVerse/Prefabs/Items/InkBoxGun.prefab";
        private const string SPEAKER_PREFAB = "Assets/_GunVerse/Prefabs/Items/SpeakerGun.prefab";
        private const string LARGE_SPEAKER_PREFAB = "Assets/_GunVerse/Prefabs/Items/LargeSpeakerGun.prefab";
        private const string RADIO_PREFAB = "Assets/_GunVerse/Prefabs/Items/RadioGun.prefab";
        private const string TOASTER_PREFAB = "Assets/_GunVerse/Prefabs/Items/ToasterGun.prefab";
        private const string TV_BOX_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/TVBoxGun.prefab";
        private const string TV32_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/TV32Gun.prefab";
        private const string TV_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/TVGun.prefab";
        private const string TV65_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/TV65Gun.prefab";
        private const string TV75_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/TV75Gun.prefab";
        private const string DISPLAY_TABLE_PREFAB = "Assets/_GunVerse/Prefabs/Items/DisplayTableGun.prefab";
        private const string DESIGN_TABLE_PREFAB = "Assets/_GunVerse/Prefabs/Items/DesignTableGun.prefab";
        private const string HIGH_TABLE_PREFAB = "Assets/_GunVerse/Prefabs/Items/HighTableGun.prefab";
        private const string PODIUM_PREFAB = "Assets/_GunVerse/Prefabs/Items/PodiumGun.prefab";
        private const string WORKBENCH_PREFAB = "Assets/_GunVerse/Prefabs/Items/WorkbenchGun.prefab";
        private const string SHELF_RUN_PREFAB = "Assets/_GunVerse/Prefabs/BopBuy/ShelfRunGun_BopBuy.prefab";
        private const string TALL_SHELF_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/TallOpenShelfGun.prefab";
        private const string SHELF_UNIT_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/ShelfUnitGun.prefab";
        private const string DESK_CHAIR_PREFAB = "Assets/_GunVerse/Prefabs/Items/DeskChairGun.prefab";
        private const string POWER_STRIP_PREFAB = "Assets/_GunVerse/Prefabs/Items/PowerStripGun.prefab";
        private const string FRIDGE_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/FridgeGun.prefab";
        private const string WASHER_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/WasherGun.prefab";
        private const string DRYER_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/DryerGun.prefab";
        private const string OVEN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/ElectricOvenGun.prefab";
        private const string MICROWAVE_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/MicrowaveGun.prefab";
        private const string AIR_CONDITIONER_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/AirConditionerGun.prefab";
        private const string COFFEE_MACHINE_PREFAB = "Assets/_GunVerse/Prefabs/Items/CoffeeMachineGun.prefab";
        private const string VENDING_MACHINE_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/VendingMachineGun.prefab";
        private const string PALLET_PREFAB = "Assets/_GunVerse/Prefabs/Items/PalletGun.prefab";
        private const string CARDBOARD_SMALL_PREFAB = "Assets/_GunVerse/Prefabs/Items/CardboardBoxSmallGun.prefab";
        private const string CARDBOARD_MEDIUM_PREFAB = "Assets/_GunVerse/Prefabs/Items/CardboardBoxMediumGun.prefab";
        private const string CARDBOARD_LARGE_PREFAB = "Assets/_GunVerse/Prefabs/Items/CardboardBoxLargeGun.prefab";
        private const string TRASH_BIN_PREFAB = "Assets/_GunVerse/Prefabs/Items/TrashBinGun.prefab";
        private const string TOILET_PREFAB = "Assets/_GunVerse/Prefabs/Items/ToiletGun.prefab";
        private const string SINK_PREFAB = "Assets/_GunVerse/Prefabs/Items/BathroomSinkGun.prefab";
        private const string WHITEBOARD_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/WhiteboardGun.prefab";
        private const string PRINTER_PREFAB = "Assets/_GunVerse/Prefabs/Items/PrinterGun.prefab";
        private const string SECURITY_CAMERA_PREFAB = "Assets/_GunVerse/Prefabs/Items/SecurityCameraGun.prefab";
        private const string DOME_CAMERA_PREFAB = "Assets/_GunVerse/Prefabs/Items/DomeCameraGun.prefab";
        private const string CEILING_LIGHT_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/CeilingLightGun.prefab";
        private const string EAS_PREFAB = "Assets/_GunVerse/Prefabs/Items/EasPedestalGun.prefab";
        private const string SHOPPING_CART_PREFAB = "Assets/_GunVerse/Prefabs/Items/ShoppingCartGun.prefab";
        private const string CART_CORRAL_PREFAB = "Assets/_GunVerse/Prefabs/Items/CartCorralGun.prefab";
        private const string STANCHION_PREFAB = "Assets/_GunVerse/Prefabs/Items/StanchionGun.prefab";
        private const string LANE_MARKER_PREFAB = "Assets/_GunVerse/Prefabs/BopBuy/LaneMarkerGun_BopBuy.prefab";

        private const string BOPBUY_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/BopBuySignGun.prefab";
        private const string GUYSQUAD_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/GuysquadSignGun.prefab";
        private const string MOBILE_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/MobileSignGun.prefab";
        private const string COMPUTERS_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/ComputersSignGun.prefab";
        private const string HOME_THEATRE_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/HomeTheatreSignGun.prefab";
        private const string APPLIANCES_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/AppliancesSignGun.prefab";
        private const string CHECKOUT_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/CheckoutSignGun.prefab";
        private const string LAPTOPS_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/LaptopsSignGun.prefab";
        private const string TV_WALL_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/TvWallSignGun.prefab";
        private const string GUYSQUAD_WALL_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/GuysquadWallSignGun.prefab";
        private const string GUYSQUAD_HANGING_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/GuysquadHangingSignGun.prefab";
        private const string HANGING_MOBILE_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/HangingMobileSignGun.prefab";
        private const string HANGING_COMPUTERS_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/HangingComputersSignGun.prefab";
        private const string HANGING_APPLIANCES_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/HangingAppliancesSignGun.prefab";
        private const string HANGING_HOME_THEATRE_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/HangingHomeTheatreSignGun.prefab";
        private const string HANGING_CHECKOUT_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/HangingCheckoutSignGun.prefab";
        private const string BOPBUY_BANNER_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/BopBuyBannerGun.prefab";
        private const string WELCOME_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/WelcomeDirectorySignGun.prefab";
        private const string NO_BREAKS_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/NoBreaksSignGun.prefab";
        private const string BEHIND_YOU_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/BehindYouSignGun.prefab";
        private const string MEN_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/MenSignGun.prefab";
        private const string WOMEN_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/WomenSignGun.prefab";
        private const string CLEARANCE_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/ClearanceSignGun.prefab";
        private const string WAREHOUSE_SIGN_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/WarehouseSignGun.prefab";

        private const float FLOOR_TOP_Y = 0.14f;
        private const float SHELF_LENGTH = 10f;
        private const float TARGET_SPAWN_COUNT = 28;

        private sealed class BlockRecord
        {
            public string Name;
            public string Label;
            public string LowerLabel;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        private struct Opening
        {
            public float Start;
            public float End;
            public float Top;
        }

        private struct SupportSurface
        {
            public Bounds Bounds;
            public float TopY;
        }

        private sealed class WallOpeningSpec
        {
            public float CenterX;
            public float CenterZ;
            public Opening[] Openings;
        }

        private static readonly WallOpeningSpec[] WallOpenings =
        {
            new WallOpeningSpec
            {
                CenterX = 0f,
                CenterZ = -24.9f,
                Openings = new[]
                {
                    new Opening { Start = -3.245f, End = -0.955f, Top = 2.85f },
                    new Opening { Start = 1.005f, End = 3.295f, Top = 2.85f }
                }
            },
            new WallOpeningSpec
            {
                CenterX = 27.71f,
                CenterZ = -19.34f,
                Openings = new[]
                {
                    new Opening { Start = -22.76f, End = -21.24f, Top = 2.6f }
                }
            },
            new WallOpeningSpec
            {
                CenterX = 32.36f,
                CenterZ = 9.16f,
                Openings = new[]
                {
                    new Opening { Start = 11.11f, End = 12.63f, Top = 2.85f },
                    new Opening { Start = 13.115f, End = 14.635f, Top = 2.85f }
                }
            },
            new WallOpeningSpec
            {
                CenterX = 33.7f,
                CenterZ = -8.28f,
                Openings = new[]
                {
                    new Opening { Start = 28.24f, End = 29.76f, Top = 2.6f },
                    new Opening { Start = 37.32f, End = 38.84f, Top = 2.6f }
                }
            }
        };

        private static readonly List<BlockRecord> blocks = new List<BlockRecord>();
        private static readonly List<Bounds> shelfBounds = new List<Bounds>();
        private static readonly List<string> shelfNames = new List<string>();
        private static readonly List<SupportSurface> supportSurfaces = new List<SupportSurface>();
        private static readonly List<Bounds> largeApplianceBounds = new List<Bounds>();
        private static readonly HashSet<string> placedLabelKeys = new HashSet<string>();

        private static Transform environment;
        private static Transform props;
        private static Transform spawnRoot;
        private static Transform zonesRoot;
        private static Transform lightingRoot;
        private static int productCursor;
        private static System.Random random;

        [MenuItem("Tools/Gun Verse/Build BopBuy From Design")]
        public static void Build()
        {
            random = new System.Random(1773);
            blocks.Clear();
            shelfBounds.Clear();
            shelfNames.Clear();
            supportSurfaces.Clear();
            largeApplianceBounds.Clear();
            placedLabelKeys.Clear();
            productCursor = 0;

            List<BlockRecord> design = ReadDesign();
            if (design.Count == 0)
            {
                Debug.LogError("[BopBuyDesignBuilder] No design blocks were found.");
                return;
            }

            EnsureBackup();
            Scene scene = EditorSceneManager.OpenScene(BOPBUY_SCENE_PATH, OpenSceneMode.Single);
            CleanupScene();
            EnsureGameplaySystems();

            blocks.AddRange(design);
            environment = CreateRoot("BopBuyEnvironment");
            props = CreateRoot("BopBuyProps");
            spawnRoot = CreateRoot("BopBuySpawnPoints");
            zonesRoot = CreateRoot("BopBuyZones");
            lightingRoot = CreateRoot("BopBuyLighting");

            BuildShell();
            BuildZoneFloors();
            BuildEntrance();
            BuildCheckout();
            BuildGuysquadFront();
            BuildMobileKiosk();
            BuildPcCheckout();
            BuildBathrooms();
            BuildApplianceFixtures();
            BuildTallFridgeRows();
            BuildAisles();
            BuildGuysquadRoom();
            BuildMountedDisplays();
            BuildWarehouse();
            BuildGenericChairs();
            BuildSurfaceDevices();
            BuildMiscProps();
            BuildSignage();
            BuildCeilingLights();
            BuildApplianceAtmosphere();
            BuildSpawnPoints();
            ConfigureSceneLighting();

            Physics.SyncTransforms();
            GunPropBakeUtility.BakeAndFinalizeSceneProps();
            SetMapShelvesFixedInWorld();
            ValidateScene();

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(DESIGN_SCENE_PATH, OpenSceneMode.Single);
            Debug.Log($"[BopBuyDesignBuilder] Built BopBuy from {design.Count} design blocks and restored the design scene.");
        }

        internal static void SetMapShelvesFixedInWorld()
        {
            int fixedCount = 0;
            foreach (GunItem item in UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (item == null)
                {
                    continue;
                }

                bool underPropsRoot = props != null
                    ? item.transform.IsChildOf(props)
                    : item.transform.root.name == "BopBuyProps";
                if (!underPropsRoot)
                {
                    continue;
                }

                string itemName = item.name;
                bool isMapShelf = itemName.IndexOf("Shelf", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  itemName.IndexOf("Endcap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  itemName.IndexOf("Mobile Display Tower", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isMapShelf || itemName.IndexOf("Product", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(item);
                SerializedProperty property = serializedObject.FindProperty("fixedInWorld");
                if (property == null || property.boolValue)
                {
                    continue;
                }

                property.boolValue = true;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                fixedCount++;
            }

            Debug.Log($"[BopBuyDesignBuilder] Fixed {fixedCount} map shelves in world after baking.");
        }

        private static List<BlockRecord> ReadDesign()
        {
            Scene designScene = SceneManager.GetSceneByPath(DESIGN_SCENE_PATH);
            bool opened = false;
            if (!designScene.IsValid() || !designScene.isLoaded)
            {
                designScene = EditorSceneManager.OpenScene(DESIGN_SCENE_PATH, OpenSceneMode.Additive);
                opened = true;
            }

            List<BlockRecord> result = new List<BlockRecord>();
            foreach (DesignBlock designBlock in UnityEngine.Object.FindObjectsByType<DesignBlock>(FindObjectsInactive.Include))
            {
                if (designBlock.gameObject.scene != designScene)
                {
                    continue;
                }

                Transform blockTransform = designBlock.transform;
                result.Add(new BlockRecord
                {
                    Name = designBlock.name,
                    Label = designBlock.Label,
                    LowerLabel = designBlock.Label.ToLowerInvariant(),
                    Position = blockTransform.position,
                    Rotation = blockTransform.rotation,
                    Scale = blockTransform.lossyScale
                });
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            if (opened)
            {
                EditorSceneManager.CloseScene(designScene, true);
            }

            return result;
        }

        private static void EnsureBackup()
        {
            EnsureFolder(BACKUP_FOLDER);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BACKUP_SCENE_PATH) != null)
            {
                return;
            }

            if (!AssetDatabase.CopyAsset(BOPBUY_SCENE_PATH, BACKUP_SCENE_PATH))
            {
                Debug.LogError("[BopBuyDesignBuilder] Could not create the pre-design BopBuy backup.");
                return;
            }

            AssetDatabase.ImportAsset(BACKUP_SCENE_PATH);
            Debug.Log("[BopBuyDesignBuilder] Backed up the original BopBuy scene to " + BACKUP_SCENE_PATH);
        }

        private static void CleanupScene()
        {
            string[] removableRoots =
            {
                "StoreEnvironment", "LargeGunProps", "PickupItems", "StoreLighting", "SpawnPoints",
                "BopBuyEnvironment", "BopBuyProps", "BopBuySpawnPoints", "BopBuyZones", "BopBuyLighting",
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
                        UnityEngine.Object.DestroyImmediate(root);
                        break;
                    }
                }
            }
        }

        private static void EnsureGameplaySystems()
        {
            GameModeController gameMode = UnityEngine.Object.FindAnyObjectByType<GameModeController>();
            if (gameMode == null)
            {
                GameObject gameModeObject = new GameObject("GameMode");
                gameModeObject.AddComponent<NetworkObject>();
                gameMode = gameModeObject.AddComponent<GameModeController>();
            }

            GunGameLadder ladder = AssetDatabase.LoadAssetAtPath<GunGameLadder>(GUN_GAME_LADDER_PATH);
            SerializedObject serializedMode = new SerializedObject(gameMode);
            serializedMode.FindProperty("gunGameLadder").objectReferenceValue = ladder;
            serializedMode.ApplyModifiedPropertiesWithoutUndo();

            PlayerSpawnManager spawnManager = UnityEngine.Object.FindAnyObjectByType<PlayerSpawnManager>();
            if (spawnManager == null)
            {
                GameObject spawnManagerObject = new GameObject("PlayerSpawnManager");
                spawnManagerObject.AddComponent<NetworkObject>();
                spawnManager = spawnManagerObject.AddComponent<PlayerSpawnManager>();
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
            SerializedObject serializedSpawn = new SerializedObject(spawnManager);
            serializedSpawn.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            serializedSpawn.ApplyModifiedPropertiesWithoutUndo();

            StoreFogController fogController = UnityEngine.Object.FindAnyObjectByType<StoreFogController>(FindObjectsInactive.Include);
            if (fogController == null)
            {
                GameObject fogObject = new GameObject("StoreFogController");
                fogObject.AddComponent<NetworkObject>();
                fogObject.AddComponent<StoreFogController>();
            }
        }

        private static void BuildShell()
        {
            BlockRecord floorRecord = blocks.FirstOrDefault(block => block.LowerLabel == "floor");
            Vector3 floorCenter = floorRecord != null ? floorRecord.Position : new Vector3(0f, 0.04f, 0f);
            Vector3 floorSize = floorRecord != null ? floorRecord.Scale : new Vector3(78f, 0.2f, 50f);
            CreateStaticBox(environment, "StoreFloor", floorCenter, floorSize, BopBuyMaterials.Floor);
            CreateStaticBox(environment, "StoreCeiling", new Vector3(0f, 8.25f, 0f), new Vector3(78.4f, 0.2f, 50.4f), BopBuyMaterials.Ceiling);

            foreach (BlockRecord wall in blocks.Where(block => block.LowerLabel == "wall"))
            {
                BuildWall(wall);
            }

            BuildFrontDoors();
        }

        private static void BuildWall(BlockRecord wall)
        {
            Bounds bounds = GetWorldBounds(wall);
            if (Mathf.Abs(wall.Position.z - 12.01f) < 0.2f &&
                Mathf.Abs(wall.Position.x - 12.18f) < 0.5f &&
                wall.Scale.z > 30f &&
                Mathf.Abs(Mathf.DeltaAngle(wall.Rotation.eulerAngles.y, 270f)) < 1f)
            {
                const float leftDoorJambX = 31.887f;
                float length = leftDoorJambX - bounds.min.x;
                float centerX = (leftDoorJambX + bounds.min.x) * 0.5f;
                CreateStaticBox(environment, "Warehouse Back Wall", new Vector3(centerX, wall.Position.y, wall.Position.z), new Vector3(wall.Scale.x, wall.Scale.y, length), BopBuyMaterials.Wall, wall.Rotation);
                Debug.Log($"[BopBuyDesignBuilder] Aligned warehouse back wall to the left warehouse door jamb at x={leftDoorJambX:0.000}.");
                return;
            }

            WallOpeningSpec spec = FindWallOpeningSpec(wall);
            if (spec == null)
            {
                CreateStaticBox(environment, "Wall", wall.Position, wall.Scale, BopBuyMaterials.Wall, wall.Rotation);
                return;
            }

            bool longAxisX = bounds.size.x >= bounds.size.z;
            float min = longAxisX ? bounds.min.x : bounds.min.z;
            float max = longAxisX ? bounds.max.x : bounds.max.z;
            float thickness = longAxisX ? bounds.size.z : bounds.size.x;
            float wallHeight = bounds.size.y;
            float wallCenterY = bounds.center.y;
            float thicknessCenter = longAxisX ? bounds.center.z : bounds.center.x;

            List<Opening> openings = spec.Openings.OrderBy(opening => opening.Start).ToList();
            float cursor = min;
            foreach (Opening opening in openings)
            {
                float start = Mathf.Clamp(opening.Start, min, max);
                float end = Mathf.Clamp(opening.End, min, max);
                if (start > cursor + 0.01f)
                {
                    CreateWallSegment(cursor, start, thicknessCenter, wallCenterY, wallHeight, thickness, longAxisX, BopBuyMaterials.Wall);
                }

                float headerTop = bounds.max.y;
                if (headerTop > opening.Top + 0.01f)
                {
                    CreateWallSegment(start, end, thicknessCenter, (opening.Top + headerTop) * 0.5f, headerTop - opening.Top, thickness, longAxisX, BopBuyMaterials.Wall);
                }

                BuildDoorFrame(start, end, bounds, longAxisX);
                cursor = Mathf.Max(cursor, end);
            }

            if (cursor < max - 0.01f)
            {
                CreateWallSegment(cursor, max, thicknessCenter, wallCenterY, wallHeight, thickness, longAxisX, BopBuyMaterials.Wall);
            }
        }

        private static WallOpeningSpec FindWallOpeningSpec(BlockRecord wall)
        {
            foreach (WallOpeningSpec spec in WallOpenings)
            {
                if (Mathf.Abs(wall.Position.x - spec.CenterX) < 0.6f &&
                    Mathf.Abs(wall.Position.z - spec.CenterZ) < 0.6f)
                {
                    return spec;
                }
            }

            return null;
        }

        private static void CreateWallSegment(
            float start,
            float end,
            float thicknessCenter,
            float centerY,
            float height,
            float thickness,
            bool longAxisX,
            Material material)
        {
            float length = end - start;
            if (length <= 0.001f)
            {
                return;
            }

            float center = (start + end) * 0.5f;
            Vector3 position = longAxisX
                ? new Vector3(center, centerY, thicknessCenter)
                : new Vector3(thicknessCenter, centerY, center);
            Vector3 size = longAxisX
                ? new Vector3(length, height, thickness)
                : new Vector3(thickness, height, length);
            CreateStaticBox(environment, "Wall", position, size, material);
        }

        private static void BuildDoorFrame(float start, float end, Bounds wallBounds, bool longAxisX)
        {
            float width = end - start;
            if (width < 0.5f)
            {
                return;
            }

            float center = (start + end) * 0.5f;
            float frameDepth = longAxisX ? wallBounds.size.z + 0.16f : wallBounds.size.x + 0.16f;
            const float height = 2.6f;
            float centerY = FLOOR_TOP_Y + height * 0.5f;
            const float thickness = 0.08f;

            if (longAxisX)
            {
                CreateStaticBox(environment, "DoorPostLeft", new Vector3(start, centerY, wallBounds.center.z), new Vector3(thickness, height + 0.08f, frameDepth), BopBuyMaterials.Metal);
                CreateStaticBox(environment, "DoorPostRight", new Vector3(end, centerY, wallBounds.center.z), new Vector3(thickness, height + 0.08f, frameDepth), BopBuyMaterials.Metal);
                CreateStaticBox(environment, "DoorLintel", new Vector3(center, FLOOR_TOP_Y + height + 0.04f, wallBounds.center.z), new Vector3(width + thickness, 0.08f, frameDepth), BopBuyMaterials.Metal);
            }
            else
            {
                CreateStaticBox(environment, "DoorPostLeft", new Vector3(wallBounds.center.x, centerY, start), new Vector3(frameDepth, height + 0.08f, thickness), BopBuyMaterials.Metal);
                CreateStaticBox(environment, "DoorPostRight", new Vector3(wallBounds.center.x, centerY, end), new Vector3(frameDepth, height + 0.08f, thickness), BopBuyMaterials.Metal);
                CreateStaticBox(environment, "DoorLintel", new Vector3(wallBounds.center.x, FLOOR_TOP_Y + height + 0.04f, center), new Vector3(frameDepth, 0.08f, width + thickness), BopBuyMaterials.Metal);
            }
        }

        private static void BuildFrontDoors()
        {
            Material glass = AssetDatabase.LoadAssetAtPath<Material>(SHOP_GLASS_MATERIAL_PATH);
            BuildGlassDoorPair(-2.1f, glass);
            BuildGlassDoorPair(2.15f, glass);
            CreateStaticBox(environment, "EntranceMullionLeft", new Vector3(-0.955f, 1.4f, -24.29f), new Vector3(0.08f, 2.8f, 0.12f), BopBuyMaterials.Metal);
            CreateStaticBox(environment, "EntranceMullionRight", new Vector3(1.005f, 1.4f, -24.29f), new Vector3(0.08f, 2.8f, 0.12f), BopBuyMaterials.Metal);
            CreateStaticBox(environment, "EntranceHeader", new Vector3(0f, 2.9f, -24.3f), new Vector3(4.4f, 0.12f, 0.16f), BopBuyMaterials.Blue);
        }

        private static void BuildGlassDoorPair(float centerX, Material glass)
        {
            const float leafWidth = 1.1f;
            for (int index = 0; index < 2; index++)
            {
                float x = centerX + (index == 0 ? -leafWidth * 0.5f : leafWidth * 0.5f);
                CreateStaticBox(environment, "GlassDoor", new Vector3(x, 1.42f, -24.29f), new Vector3(leafWidth - 0.08f, 2.64f, 0.06f), glass);
                CreateStaticBox(environment, "GlassDoorFrameLeft", new Vector3(x - leafWidth * 0.5f, 1.42f, -24.29f), new Vector3(0.05f, 2.8f, 0.12f), BopBuyMaterials.Metal);
                CreateStaticBox(environment, "GlassDoorFrameRight", new Vector3(x + leafWidth * 0.5f, 1.42f, -24.29f), new Vector3(0.05f, 2.8f, 0.12f), BopBuyMaterials.Metal);
                CreateStaticBox(environment, "GlassDoorFrameTop", new Vector3(x, 2.78f, -24.29f), new Vector3(leafWidth, 0.06f, 0.12f), BopBuyMaterials.Metal);
                CreateStaticBox(environment, "GlassDoorFrameBottom", new Vector3(x, 0.04f, -24.29f), new Vector3(leafWidth, 0.06f, 0.12f), BopBuyMaterials.Metal);
                CreateStaticBox(environment, "GlassDoorPushRail", new Vector3(x, 1.1f, -24.29f), new Vector3(leafWidth - 0.08f, 0.05f, 0.14f), BopBuyMaterials.Chrome);
            }
        }
    }
}
