using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static partial class BopBuyDesignBuilder
    {
        private const string PROPS_ROOT_NAME = "BopBuyProps";
        private const string CHECKOUT_COUNTER_NAME_PREFIX = "Checkout Counter ";
        private const string CHECKOUT_REGISTER_NAME_PREFIX = "Checkout Register ";
        private const string GUYSQUAD_REGISTER_NAME = "Guysquad Register";
        private const string GUYSQUAD_COUNTER_ONE_NAME = "Guysquad Checkout Counter 1";
        private const string GUYSQUAD_COUNTER_TWO_NAME = "Guysquad Checkout Counter 2";

        private static void BuildZoneFloors()
        {
            int zoneIndex = 0;
            foreach (BlockRecord block in blocks)
            {
                string materialKey = GetZoneFloorKey(block.LowerLabel);
                if (materialKey == null)
                {
                    continue;
                }

                Bounds bounds = GetWorldBounds(block);
                float minX = Mathf.Max(bounds.min.x, -39.05f);
                float maxX = Mathf.Min(bounds.max.x, 39.1f);
                float minZ = Mathf.Max(bounds.min.z, -24.85f);
                float maxZ = Mathf.Min(bounds.max.z, 24.8f);
                if (maxX - minX < 0.5f || maxZ - minZ < 0.5f)
                {
                    continue;
                }

                Vector3 position = new Vector3((minX + maxX) * 0.5f, FLOOR_TOP_Y + 0.004f + zoneIndex * 0.0008f, (minZ + maxZ) * 0.5f);
                Vector3 size = new Vector3(maxX - minX, 0.01f, maxZ - minZ);
                GameObject patch = CreateStaticBox(zonesRoot, "Zone " + materialKey, position, size, GetZoneMaterial(materialKey));
                Collider patchCollider = patch.GetComponent<Collider>();
                if (patchCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(patchCollider);
                }

                zoneIndex++;
            }
        }

        private static string GetZoneFloorKey(string label)
        {
            if (label.StartsWith("appliances (", StringComparison.Ordinal))
            {
                return "Appliance Carpet";
            }

            if (label.StartsWith("guy squad", StringComparison.Ordinal))
            {
                return "Guysquad Carpet";
            }

            if (label == "pc and games dept" || label == "pc dept")
            {
                return "PC Carpet";
            }

            if (label == "mobile")
            {
                return "Mobile Carpet";
            }

            if (label == "home theatre/tvs")
            {
                return "Theatre Carpet";
            }

            if (label.StartsWith("warehouse", StringComparison.Ordinal))
            {
                return "Warehouse Floor";
            }

            if (label == "checkoutf")
            {
                return "Checkout Carpet";
            }

            if (label == "mens bathroom" || label == "girls bathroom")
            {
                return "Bathroom Tile";
            }

            return null;
        }

        private static Material GetZoneMaterial(string key)
        {
            return key switch
            {
                "Appliance Carpet" => BopBuyMaterials.CharcoalCarpet,
                "Guysquad Carpet" => BopBuyMaterials.Carpet,
                "PC Carpet" => BopBuyMaterials.Carpet,
                "Mobile Carpet" => BopBuyMaterials.Carpet,
                "Theatre Carpet" => BopBuyMaterials.CharcoalCarpet,
                "Warehouse Floor" => BopBuyMaterials.CoinRubber,
                "Checkout Carpet" => BopBuyMaterials.Carpet,
                "Bathroom Tile" => BopBuyMaterials.WhiteBlock,
                _ => BopBuyMaterials.Floor
            };
        }

        private static void BuildEntrance()
        {
            GameObject desk = PlaceOnFloor(PODIUM_PREFAB, "Loss Prevention Desk", new Vector3(-0.02f, 0f, -23.09f), Quaternion.identity);
            float deskTop = GetBoundsTop(desk);
            PlaceSurfaceDevice(MONITOR_PREFAB, "Loss Prevention Monitor", new Vector3(-0.02f, 0f, -23.09f), deskTop, Quaternion.Euler(0f, 180f, 0f));
            PlaceSurfaceDevice(KEYBOARD_PREFAB, "Loss Prevention Keyboard", new Vector3(0.1f, 0f, -23.22f), deskTop, Quaternion.identity);
            PlaceSurfaceDevice(MOUSE_PREFAB, "Loss Prevention Mouse", new Vector3(0.3f, 0f, -23.22f), deskTop, Quaternion.identity);
        }

        private static void BuildCheckout()
        {
            BuildCheckoutCounters();
            AddLaneMarkings(new Vector3(-30.56f, 0f, -20.16f), 11.8f, 1.56f);
            BuildCheckoutRegisters();
        }

        private static void BuildCheckoutCounters()
        {
            const float counterZ = -23.32f;
            for (int index = 0; index < 5; index++)
            {
                float x = -36.4f + index * 3.2f;
                GameObject counter = PlaceOnFloor(CHECKOUT_COUNTER_PREFAB, CHECKOUT_COUNTER_NAME_PREFIX + (index + 1), new Vector3(x, 0f, counterZ), Quaternion.Euler(0f, 90f, 0f));
                if (counter != null)
                {
                    RegisterSurface(counter);
                }
            }
        }

        private static void BuildCheckoutRegisters(bool placeDevices = true)
        {
            int registerIndex = 0;
            foreach (BlockRecord block in blocks.Where(block =>
                         block.LowerLabel.StartsWith("register, monitor", StringComparison.Ordinal) &&
                         block.Position.z < -22.8f))
            {
                if (IsDuplicate("checkout-register", block.Position))
                {
                    continue;
                }

                registerIndex++;
                BuildCheckoutRegister(block, CHECKOUT_REGISTER_NAME_PREFIX + registerIndex, placeDevices);
            }
        }

        private static void BuildCheckoutRegister(BlockRecord block, string name, bool placeDevices)
        {
            float top = ResolveSurfaceTop(block.Position.x, block.Position.z, block.Position.y, 1.3f);
            Quaternion rotation = block.Rotation;
            Vector3 forward = rotation * Vector3.forward;
            PlaceSurfaceDevice(REGISTER_PREFAB, name, block.Position, top, rotation);
            if (!placeDevices)
            {
                return;
            }

            PlaceSurfaceDevice(MONITOR_PREFAB, name + " Monitor", block.Position - forward * 0.2f, top, rotation);
            PlaceSurfaceDevice(KEYBOARD_PREFAB, name + " Keyboard", block.Position + forward * 0.28f, top, rotation);
            PlaceSurfaceDevice(MOUSE_PREFAB, name + " Mouse", block.Position + forward * 0.28f + rotation * Vector3.right * 0.34f, top, rotation);
        }

        private static void AddLaneMarkings(Vector3 center, float width, float depth)
        {
            const float lineWidth = 0.08f;
            Material yellow = BopBuyMaterials.Yellow;
            CreateStaticBox(zonesRoot, "Lane Line North", new Vector3(center.x, FLOOR_TOP_Y + 0.012f, center.z + depth * 0.5f), new Vector3(width, 0.024f, lineWidth), yellow);
            CreateStaticBox(zonesRoot, "Lane Line South", new Vector3(center.x, FLOOR_TOP_Y + 0.012f, center.z - depth * 0.5f), new Vector3(width, 0.024f, lineWidth), yellow);
            CreateStaticBox(zonesRoot, "Lane Line West", new Vector3(center.x - width * 0.5f, FLOOR_TOP_Y + 0.012f, center.z), new Vector3(lineWidth, 0.024f, depth), yellow);
            CreateStaticBox(zonesRoot, "Lane Line East", new Vector3(center.x + width * 0.5f, FLOOR_TOP_Y + 0.012f, center.z), new Vector3(lineWidth, 0.024f, depth), yellow);
        }

        private static void BuildGuysquadFront()
        {
            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "table"))
            {
                if (IsDuplicate("guysquad-table", block.Position))
                {
                    continue;
                }

                GameObject table = PlaceOnFloor(DESIGN_TABLE_PREFAB, "Guysquad Consultation Table", block.Position, block.Rotation);
                if (table != null)
                {
                    RegisterSurface(table);
                    Vector3 stripPosition = block.Position + block.Rotation * new Vector3(0f, 0f, 0.35f);
                    PlaceOnSurface(POWER_STRIP_PREFAB, "Guysquad Power Strip", stripPosition, GetBoundsTop(table), block.Rotation);
                }
            }

            GameObject checkoutLeft = PlaceOnFloor(CHECKOUT_COUNTER_PREFAB, GUYSQUAD_COUNTER_ONE_NAME, new Vector3(13.94f, 0f, -22.33f), Quaternion.Euler(0f, 90f, 0f));
            if (checkoutLeft != null)
            {
                checkoutLeft.transform.localScale = new Vector3(1f, 1f, 0.86f);
                RegisterSurface(checkoutLeft);
            }

            GameObject checkoutRight = PlaceOnFloor(CHECKOUT_COUNTER_PREFAB, GUYSQUAD_COUNTER_TWO_NAME, new Vector3(16.91f, 0f, -22.33f), Quaternion.Euler(0f, 90f, 0f));
            if (checkoutRight != null)
            {
                checkoutRight.transform.localScale = new Vector3(1f, 1f, 0.86f);
                RegisterSurface(checkoutRight);
            }

            BuildGuysquadCheckoutRegisters();
        }

        private static void BuildGuysquadCheckoutRegisters(bool placeDevices = true)
        {
            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "guysquad checkout"))
            {
                float top = ResolveSurfaceTop(block.Position.x, block.Position.z, block.Position.y, 1.3f);
                Vector3 forward = block.Rotation * Vector3.forward;
                PlaceSurfaceDevice(REGISTER_PREFAB, GUYSQUAD_REGISTER_NAME, block.Position, top, block.Rotation);
                if (!placeDevices)
                {
                    continue;
                }

                PlaceSurfaceDevice(MONITOR_PREFAB, "Guysquad Checkout Monitor", block.Position - forward * 0.16f, top, block.Rotation);
                PlaceSurfaceDevice(KEYBOARD_PREFAB, "Guysquad Checkout Keyboard", block.Position + forward * 0.26f, top, block.Rotation);
                PlaceSurfaceDevice(MOUSE_PREFAB, "Guysquad Checkout Mouse", block.Position + forward * 0.26f + block.Rotation * Vector3.right * 0.3f, top, block.Rotation);
            }
        }

        private static void BuildMobileKiosk()
        {
            GameObject staffCounter = PlaceOnFloor(CHECKOUT_COUNTER_PREFAB, "Mobile Staff Counter", new Vector3(1.602f, 0f, -3.32f), Quaternion.Euler(0f, 90f, 0f));
            if (staffCounter != null)
            {
                staffCounter.transform.localScale = new Vector3(1f, 1f, 5.101f / 3.2f);
                RegisterSurface(staffCounter);
            }

            GameObject customerLeft = PlaceOnFloor(DESIGN_TABLE_PREFAB, "Mobile Customer Table Left", new Vector3(-0.8f, 0f, -4.72f), Quaternion.identity);
            if (customerLeft != null)
            {
                customerLeft.transform.localScale = new Vector3(3.379f / 2.8f, 1f, 1f);
                Physics.SyncTransforms();
                RegisterSurface(customerLeft);
            }

            GameObject customerRight = PlaceOnFloor(DESIGN_TABLE_PREFAB, "Mobile Customer Table Right", new Vector3(3.89f, 0f, -4.72f), Quaternion.identity);
            if (customerRight != null)
            {
                customerRight.transform.localScale = new Vector3(3.379f / 2.8f, 1f, 1f);
                Physics.SyncTransforms();
                RegisterSurface(customerRight);
            }

            float top = staffCounter != null ? GetBoundsTop(staffCounter) : 1.3f;
            PlaceSurfaceDevice(MONITOR_PREFAB, "Mobile Kiosk Monitor", new Vector3(1.6f, 0f, -3.55f), top, Quaternion.Euler(0f, 180f, 0f));
            PlaceSurfaceDevice(KEYBOARD_PREFAB, "Mobile Kiosk Keyboard", new Vector3(1.6f, 0f, -3.0f), top, Quaternion.identity);
            PlaceSurfaceDevice(MOUSE_PREFAB, "Mobile Kiosk Mouse", new Vector3(2.0f, 0f, -3.0f), top, Quaternion.identity);
            PlaceFlatDevice(PHONE_PREFAB, "Mobile Transfer Phone 1", new Vector3(0.65f, 0f, -3.0f), top, 18f);
            PlaceFlatDevice(PHONE_PREFAB, "Mobile Transfer Phone 2", new Vector3(1.05f, 0f, -3.05f), top, -14f);
            PlaceFlatDevice(TABLET_PREFAB, "Mobile Transfer Tablet", new Vector3(2.55f, 0f, -3.0f), top, 8f);
            PlaceSurfaceDevice(REGISTER_PREFAB, "Mobile Kiosk Register", new Vector3(3.18f, 0f, -3.23f), top, Quaternion.identity);

            BuildMobileSignSupport(new Vector3(1.6f, 0f, -2.62f));
        }

        private static void BuildMobileSignSupport(Vector3 signBase)
        {
            CreateStaticBox(environment, "Mobile Sign Post Left", new Vector3(signBase.x - 1.2f, 2.4f, signBase.z), new Vector3(0.08f, 2.8f, 0.08f), BopBuyMaterials.Metal);
            CreateStaticBox(environment, "Mobile Sign Post Right", new Vector3(signBase.x + 1.2f, 2.4f, signBase.z), new Vector3(0.08f, 2.8f, 0.08f), BopBuyMaterials.Metal);
            CreateStaticBox(environment, "Mobile Sign Bar", new Vector3(signBase.x, 3.75f, signBase.z), new Vector3(2.6f, 0.08f, 0.08f), BopBuyMaterials.Metal);
            PlaceMounted(MOBILE_SIGN_PREFAB, "Mobile Kiosk Sign", new Vector3(signBase.x, 3.3f, signBase.z), Quaternion.identity);
        }

        private static void BuildPcCheckout()
        {
            for (int index = 0; index < 3; index++)
            {
                float z = 3.24f + index * 2.39f;
                GameObject counter = PlaceOnFloor(CHECKOUT_COUNTER_PREFAB, "PC Checkout Counter " + (index + 1), new Vector3(29.46f, 0f, z), Quaternion.identity);
                if (counter != null)
                {
                    counter.transform.localScale = new Vector3(1f, 1f, 0.75f);
                    RegisterSurface(counter);
                }
            }

            float[] monitorZ = { 3.24f, 5.63f, 7.93f };
            foreach (float z in monitorZ)
            {
                float top = ResolveSurfaceTop(29.44f, z, 1.64f, 1.3f);
                Quaternion monitorRotation = Quaternion.Euler(0f, 270f, 0f);
                PlaceSurfaceDevice(MONITOR_PREFAB, "PC Checkout Monitor", new Vector3(29.35f, 0f, z), top, monitorRotation);
                PlaceSurfaceDevice(KEYBOARD_PREFAB, "PC Checkout Keyboard", new Vector3(28.95f, 0f, z), top, monitorRotation);
                PlaceSurfaceDevice(MOUSE_PREFAB, "PC Checkout Mouse", new Vector3(28.95f, 0f, z + 0.3f), top, monitorRotation);
            }

        }

        private static void BuildBathrooms()
        {
            foreach (BlockRecord bathroom in blocks.Where(block => block.LowerLabel == "mens bathroom" || block.LowerLabel == "girls bathroom"))
            {
                Bounds bounds = GetWorldBounds(bathroom);
                bool isMens = bathroom.LowerLabel == "mens bathroom";
                string prefix = isMens ? "Mens" : "Womens";
                float centerX = bounds.center.x;
                float centerZ = bounds.center.z;

                PlaceOnFloor(TOILET_PREFAB, prefix + " Toilet 1", new Vector3(centerX - 1.1f, 0f, bounds.max.z - 0.55f), Quaternion.Euler(0f, 180f, 0f));
                PlaceOnFloor(TOILET_PREFAB, prefix + " Toilet 2", new Vector3(centerX + 1.1f, 0f, bounds.max.z - 0.55f), Quaternion.Euler(0f, 180f, 0f));
                PlaceOnFloor(SINK_PREFAB, prefix + " Sink 1", new Vector3(bounds.min.x + 0.45f, 0f, centerZ - 1.3f), Quaternion.Euler(0f, 90f, 0f));
                PlaceOnFloor(SINK_PREFAB, prefix + " Sink 2", new Vector3(bounds.min.x + 0.45f, 0f, centerZ + 1.3f), Quaternion.Euler(0f, 90f, 0f));
                PlaceOnFloor(TRASH_BIN_PREFAB, prefix + " Trash", new Vector3(bounds.max.x - 0.6f, 0f, bounds.max.z - 0.6f), Quaternion.identity);
            }
        }

        [MenuItem("Tools/Gun Verse/Repair BopBuy Checkout Fixtures")]
        public static void RepairCheckoutFixtures()
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            bool hasPreviousActiveScene = previousActiveScene.IsValid() && previousActiveScene.isLoaded;
            Scene bopBuyScene = SceneManager.GetSceneByPath(BOPBUY_SCENE_PATH);
            bool openedBopBuyScene = false;
            if (!bopBuyScene.IsValid() || !bopBuyScene.isLoaded)
            {
                bopBuyScene = EditorSceneManager.OpenScene(BOPBUY_SCENE_PATH, OpenSceneMode.Additive);
                openedBopBuyScene = true;
            }

            random = new System.Random(1773);
            blocks.Clear();
            shelfBounds.Clear();
            shelfNames.Clear();
            supportSurfaces.Clear();
            largeApplianceBounds.Clear();
            placedLabelKeys.Clear();
            productCursor = 0;
            blocks.AddRange(ReadDesign());

            if (blocks.Count == 0)
            {
                Debug.LogError("[BopBuyDesignBuilder] Checkout repair aborted: no design blocks were found.");
                RestoreSceneAfterRepair(bopBuyScene, openedBopBuyScene, previousActiveScene, hasPreviousActiveScene);
                return;
            }

            SceneManager.SetActiveScene(bopBuyScene);
            props = FindSceneRoot(bopBuyScene, PROPS_ROOT_NAME);
            if (props == null)
            {
                Debug.LogError("[BopBuyDesignBuilder] Checkout repair aborted: " + PROPS_ROOT_NAME + " was not found in the BopBuy scene.");
                RestoreSceneAfterRepair(bopBuyScene, openedBopBuyScene, previousActiveScene, hasPreviousActiveScene);
                return;
            }

            int removedCounters = RemoveBrokenFixtures(bopBuyScene, CHECKOUT_COUNTER_PREFAB, CHECKOUT_COUNTER_NAME_PREFIX);
            int removedRegisters = RemoveBrokenFixtures(bopBuyScene, REGISTER_PREFAB, CHECKOUT_REGISTER_NAME_PREFIX, GUYSQUAD_REGISTER_NAME);

            placedLabelKeys.Clear();
            Physics.SyncTransforms();
            RegisterExistingSurface(bopBuyScene, GUYSQUAD_COUNTER_ONE_NAME);
            RegisterExistingSurface(bopBuyScene, GUYSQUAD_COUNTER_TWO_NAME);

            BuildCheckoutCounters();
            BuildCheckoutRegisters(false);
            BuildGuysquadCheckoutRegisters(false);

            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(bopBuyScene);
            EditorSceneManager.SaveScene(bopBuyScene);

            List<GameObject> counters = FindFixtures(bopBuyScene, CHECKOUT_COUNTER_PREFAB, CHECKOUT_COUNTER_NAME_PREFIX);
            List<GameObject> registers = FindFixtures(bopBuyScene, REGISTER_PREFAB, CHECKOUT_REGISTER_NAME_PREFIX, GUYSQUAD_REGISTER_NAME);
            SortFixtures(counters);
            SortFixtures(registers);

            Debug.Log($"[BopBuyDesignBuilder] Checkout repair removed {removedCounters} counters and {removedRegisters} registers; rebuilt {counters.Count} counters and {registers.Count} registers.");
            LogFixturePositions("Checkout counters", counters);
            LogFixturePositions("Checkout registers", registers);

            RestoreSceneAfterRepair(bopBuyScene, openedBopBuyScene, previousActiveScene, hasPreviousActiveScene);
        }

        private static Transform FindSceneRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root.transform;
                }
            }

            return null;
        }

        private static GameObject FindSceneGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name && child.gameObject.scene == scene)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static void RegisterExistingSurface(Scene scene, string name)
        {
            GameObject surface = FindSceneGameObject(scene, name);
            if (surface == null)
            {
                Debug.LogWarning("[BopBuyDesignBuilder] Existing checkout surface not found: " + name);
                return;
            }

            RegisterSurface(surface);
        }

        private static int RemoveBrokenFixtures(Scene scene, string prefabPath, params string[] namePrefixes)
        {
            List<GameObject> brokenFixtures = FindFixtures(scene, prefabPath, namePrefixes);
            int removedCount = brokenFixtures.Count;
            foreach (GameObject fixture in brokenFixtures)
            {
                UnityEngine.Object.DestroyImmediate(fixture);
            }

            return removedCount;
        }

        private static List<GameObject> FindFixtures(Scene scene, string prefabPath, params string[] namePrefixes)
        {
            string prefabFileName = System.IO.Path.GetFileName(prefabPath);
            List<GameObject> fixtures = new List<GameObject>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (GunItem gunItem in root.GetComponentsInChildren<GunItem>(true))
                {
                    GameObject fixture = gunItem.gameObject;
                    if (fixture.scene != scene || !NameStartsWithAny(fixture.name, namePrefixes))
                    {
                        continue;
                    }

                    string sourcePath = GetSourcePrefabPath(fixture);
                    if (sourcePath != null && sourcePath.EndsWith(prefabFileName, StringComparison.Ordinal))
                    {
                        fixtures.Add(fixture);
                    }
                }
            }

            return fixtures;
        }

        private static bool NameStartsWithAny(string name, string[] prefixes)
        {
            return prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static string GetSourcePrefabPath(GameObject instance)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            return source != null ? AssetDatabase.GetAssetPath(source) : null;
        }

        private static void SortFixtures(List<GameObject> fixtures)
        {
            fixtures.Sort((left, right) =>
            {
                int nameComparison = string.CompareOrdinal(left.name, right.name);
                if (nameComparison != 0)
                {
                    return nameComparison;
                }

                int xComparison = left.transform.position.x.CompareTo(right.transform.position.x);
                return xComparison != 0 ? xComparison : left.transform.position.z.CompareTo(right.transform.position.z);
            });
        }

        private static void LogFixturePositions(string label, List<GameObject> fixtures)
        {
            string positions = string.Join(", ", fixtures.Select(fixture => fixture.name + " " + FormatPosition(fixture.transform.position)));
            Debug.Log($"[BopBuyDesignBuilder] {label} ({fixtures.Count}): {positions}");
        }

        private static string FormatPosition(Vector3 position)
        {
            return $"({position.x:0.00}, {position.y:0.00}, {position.z:0.00})";
        }

        private static void RestoreSceneAfterRepair(Scene bopBuyScene, bool openedBopBuyScene, Scene previousActiveScene, bool hasPreviousActiveScene)
        {
            if (openedBopBuyScene && bopBuyScene.IsValid())
            {
                EditorSceneManager.CloseScene(bopBuyScene, true);
            }

            if (hasPreviousActiveScene && previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }
        }
    }
}
