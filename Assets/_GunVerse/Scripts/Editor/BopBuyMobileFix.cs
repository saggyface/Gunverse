using System.Collections.Generic;
using GunVerse;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class BopBuyMobileFix
    {
        private const string CHECKOUT_COUNTER_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/CheckoutCounterGun.prefab";
        private const string DESIGN_TABLE_PREFAB = "Assets/_GunVerse/Prefabs/Items/DesignTableGun.prefab";
        private const string REGISTER_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/CashRegisterGun.prefab";
        private const string MONITOR_PREFAB = "Assets/_GunVerse/Prefabs/Items/MonitorGun.prefab";
        private const string KEYBOARD_PREFAB = "Assets/_GunVerse/Prefabs/Items/KeyboardGun.prefab";
        private const string MOUSE_PREFAB = "Assets/_GunVerse/Prefabs/Items/MouseGun.prefab";
        private const string PHONE_PREFAB = "Assets/_GunVerse/Prefabs/Items/PhoneGun.prefab";
        private const string FLIP_PHONE_PREFAB = "Assets/_GunVerse/Prefabs/Items/FlipPhoneGun.prefab";
        private const string PHONE_CASE_PREFAB = "Assets/_GunVerse/Prefabs/Items/PhoneCaseGun.prefab";
        private const string TABLET_PREFAB = "Assets/_GunVerse/Prefabs/Items/TabletGun.prefab";
        private const string HEADPHONES_PREFAB = "Assets/_GunVerse/Prefabs/Items/HeadphonesGun.prefab";
        private const string SPEAKER_PREFAB = "Assets/_GunVerse/Prefabs/Items/SpeakerGun.prefab";
        private const string ROUTER_PREFAB = "Assets/_GunVerse/Prefabs/Items/RouterGun.prefab";
        private const string LAPTOP_PREFAB = "Assets/_GunVerse/Prefabs/Items/LaptopGun.prefab";
        private const string SHELF_UNIT_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/ShelfUnitGun.prefab";
        private const string HALF_SHELF_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/Baked/HalfShelfRunGun.prefab";
        private const string MOBILE_STAFF_COUNTER_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/Baked/MobileStaffCounterGun.prefab";
        private const string MOBILE_CUSTOMER_TABLE_PREFAB = "Assets/_GunVerse/Prefabs/LargeProps/Baked/MobileCustomerTableGun.prefab";

        private const float FLOOR_TOP_Y = 0.14f;
        private const float HALF_HEIGHT_SCALE = 0.5f;
        private const float PRODUCT_SURFACE_CLEARANCE = 0.02f;

        private static readonly string[] MobileProducts =
        {
            PHONE_PREFAB, FLIP_PHONE_PREFAB, PHONE_CASE_PREFAB, TABLET_PREFAB,
            HEADPHONES_PREFAB, SPEAKER_PREFAB, ROUTER_PREFAB, LAPTOP_PREFAB
        };

        [MenuItem("Tools/Gun Verse/Fix BopBuy Mobile Department")]
        public static void Apply()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/_GunVerse/Scenes/BopBuy.unity")
            {
                EditorSceneManager.OpenScene("Assets/_GunVerse/Scenes/BopBuy.unity", OpenSceneMode.Single);
            }

            Transform props = GameObject.Find("BopBuyProps")?.transform;
            if (props == null)
            {
                Debug.LogError("[BopBuyMobileFix] BopBuyProps root not found.");
                return;
            }

            GunPropBakeUtility.EnsureBakedVariants();
            RemoveOldMobileContent();
            BuildKiosk(props);
            BuildDisplayTable(props, new Vector3(-0.93f, 0f, -7.48f), Quaternion.identity);
            BuildDisplayTable(props, new Vector3(3.94f, 0f, -7.32f), Quaternion.identity);
            BuildHalfShelf(props, new Vector3(-10.07f, 0f, -5.6f), Quaternion.Euler(0f, 90f, 0f));
            BuildHalfShelf(props, new Vector3(-7.43f, 0f, -5.7f), Quaternion.Euler(0f, 90f, 0f));
            BuildHalfShelf(props, new Vector3(-4.68f, 0f, -5.7f), Quaternion.Euler(0f, 90f, 0f));
            BuildHalfShelf(props, new Vector3(8.03f, 0f, -5.85f), Quaternion.Euler(0f, 90f, 0f));
            BuildHalfShelf(props, new Vector3(10.78f, 0f, -5.85f), Quaternion.Euler(0f, 90f, 0f));
            BuildHalfShelf(props, new Vector3(-5.71f, 0f, 1.72f), Quaternion.identity);
            BuildHalfShelf(props, new Vector3(5.85f, 0f, 1.71f), Quaternion.identity);
            BuildDisplayTower(props);
            BuildShelfBridge(props);
            GunPropBakeUtility.BakeAndFinalizeSceneProps();
            BopBuyDesignBuilder.SetMapShelvesFixedInWorld();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[BopBuyMobileFix] Rebuilt the mobile department.");
        }

        private static void RemoveOldMobileContent()
        {
            List<GameObject> remove = new List<GameObject>();
            foreach (GunItem item in UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                Vector3 position = item.transform.position;
                if (!InMobileRegion(position))
                {
                    continue;
                }

                string name = item.name;
                if (name == "Shelf Run" ||
                    name == "Half Shelf Run" ||
                    name.StartsWith("Shelf Product") ||
                    name == "Mobile Staff Counter" ||
                    name.StartsWith("Mobile Customer Table") ||
                    name.StartsWith("Mobile Kiosk Monitor") ||
                    name.StartsWith("Mobile Kiosk Keyboard") ||
                    name.StartsWith("Mobile Kiosk Mouse") ||
                    name.StartsWith("Mobile Kiosk Register") ||
                    name.StartsWith("Mobile Transfer") ||
                    name.StartsWith("Mobile Display") ||
                    name == "Mobile Shelf Bridge" ||
                    name.StartsWith("Guysquad Shelf"))
                {
                    remove.Add(item.gameObject);
                }
            }

            foreach (GameObject target in remove)
            {
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }

        private static void BuildKiosk(Transform props)
        {
            GameObject staffCounter = PlaceOnFloor(MOBILE_STAFF_COUNTER_PREFAB, "Mobile Staff Counter", new Vector3(1.6f, 0f, -3.32f), Quaternion.Euler(0f, 90f, 0f));
            if (staffCounter == null)
            {
                return;
            }

            float staffTop = GetTop(staffCounter);

            GameObject leftTable = PlaceOnFloor(MOBILE_CUSTOMER_TABLE_PREFAB, "Mobile Customer Table Left", new Vector3(-0.8f, 0f, -4.2f), Quaternion.identity);
            GameObject rightTable = PlaceOnFloor(MOBILE_CUSTOMER_TABLE_PREFAB, "Mobile Customer Table Right", new Vector3(3.89f, 0f, -4.2f), Quaternion.identity);
            float leftTop = leftTable != null ? GetTop(leftTable) : FLOOR_TOP_Y;
            float rightTop = rightTable != null ? GetTop(rightTable) : FLOOR_TOP_Y;

            BuildRegister(props, "Mobile Kiosk Register Left", new Vector3(0.41f, 0f, -3.23f), staffTop);
            BuildRegister(props, "Mobile Kiosk Register Right", new Vector3(3.18f, 0f, -3.19f), staffTop);

            PlaceFlat(PHONE_PREFAB, "Mobile Transfer Phone Left", new Vector3(3.55f, 0f, -4.2f), rightTop, 14f);
            PlaceFlat(PHONE_PREFAB, "Mobile Transfer Phone Right", new Vector3(4.1f, 0f, -4.2f), rightTop, -8f);
            PlaceFlat(TABLET_PREFAB, "Mobile Transfer Tablet", new Vector3(4.65f, 0f, -4.2f), rightTop, 6f);
            PlaceFlat(PHONE_PREFAB, "Mobile Customer Phone 1", new Vector3(-1.4f, 0f, -4.2f), leftTop, 10f);
            PlaceFlat(PHONE_PREFAB, "Mobile Customer Phone 2", new Vector3(-0.85f, 0f, -4.2f), leftTop, -12f);
        }

        private static void BuildRegister(Transform props, string name, Vector3 position, float surfaceY)
        {
            PlaceOnSurface(REGISTER_PREFAB, name, position, surfaceY, Quaternion.identity);
            PlaceOnSurface(MONITOR_PREFAB, name + " Monitor", position + new Vector3(0f, 0f, -0.2f), surfaceY, Quaternion.identity);
            PlaceOnSurface(KEYBOARD_PREFAB, name + " Keyboard", position + new Vector3(0f, 0f, 0.25f), surfaceY, Quaternion.identity);
            PlaceOnSurface(MOUSE_PREFAB, name + " Mouse", position + new Vector3(0.3f, 0f, 0.25f), surfaceY, Quaternion.identity);
        }

        private static void BuildDisplayTable(Transform props, Vector3 position, Quaternion rotation)
        {
            GameObject table = PlaceOnFloor(DESIGN_TABLE_PREFAB, "Mobile Display Table", position, rotation);
            if (table == null)
            {
                return;
            }

            float top = GetTop(table);
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            PlaceFlat(PHONE_PREFAB, "Mobile Display Phone 1", position - right * 0.85f + forward * 0.1f, top, 12f);
            PlaceFlat(PHONE_PREFAB, "Mobile Display Phone 2", position - right * 0.4f + forward * 0.05f, top, -8f);
            PlaceFlat(TABLET_PREFAB, "Mobile Display Tablet", position + right * 0.25f, top, 6f);
            PlaceOnSurface(LAPTOP_PREFAB, "Mobile Display Laptop", position + right * 0.9f, top, rotation);
        }

        private static void BuildHalfShelf(Transform props, Vector3 position, Quaternion blockRotation)
        {
            Quaternion shelfRotation = blockRotation * Quaternion.Euler(0f, 90f, 0f);
            GameObject shelf = PlaceGun(HALF_SHELF_PREFAB, "Half Shelf Run", new Vector3(position.x, FLOOR_TOP_Y + 1.1f * HALF_HEIGHT_SCALE, position.z), shelfRotation);
            if (shelf == null)
            {
                return;
            }

            SetFixedInWorld(shelf, true);
            Physics.SyncTransforms();

            float[] boardHeights = { 0.96f, 1.5f };
            const int slotCount = 3;
            int productIndex = 0;
            foreach (float boardHeight in boardHeights)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    Quaternion productRotation = shelf.transform.rotation * Quaternion.Euler(0f, side < 0f ? -90f : 90f, 0f);
                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        float worldOffset = -9.37f * 0.5f + (slot + 0.5f) * (9.37f / slotCount);
                        Vector3 localPoint = new Vector3(side * 0.52f, 0f, worldOffset);
                        Vector3 world = shelf.transform.TransformPoint(localPoint);
                        string prefab = MobileProducts[productIndex++ % MobileProducts.Length];
                        PlaceOnSurface(prefab, "Shelf Product", world, FLOOR_TOP_Y + boardHeight * HALF_HEIGHT_SCALE, productRotation);
                    }
                }
            }
        }

        private static void BuildDisplayTower(Transform props)
        {
            GameObject tower = PlaceGun(SHELF_UNIT_PREFAB, "Mobile Display Tower", new Vector3(-0.57f, 2.59f, -0.23f), Quaternion.Euler(0f, -90f, 0f));
            if (tower == null)
            {
                return;
            }

            float width = 1.36f;
            float height = 3.62f;
            float depth = 1.01f;
            AddShelfPart(tower.transform, "Shelf Side Left", new Vector3(-(width * 0.5f - 0.03f), 0f, 0f), new Vector3(0.06f, height, depth));
            AddShelfPart(tower.transform, "Shelf Side Right", new Vector3(width * 0.5f - 0.03f, 0f, 0f), new Vector3(0.06f, height, depth));
            AddShelfPart(tower.transform, "Shelf Top", new Vector3(0f, height * 0.5f - 0.03f, 0f), new Vector3(width, 0.06f, depth));
            AddShelfPart(tower.transform, "Shelf Bottom", new Vector3(0f, -height * 0.5f + 0.03f, 0f), new Vector3(width, 0.06f, depth));

            float bottomWorld = 2.59f - height * 0.5f;
            float legHeight = bottomWorld - FLOOR_TOP_Y;
            if (legHeight > 0.01f)
            {
                float legX = width * 0.5f - 0.08f;
                float legZ = depth * 0.5f - 0.08f;
                float localY = -height * 0.5f - legHeight * 0.5f;
                AddShelfPart(tower.transform, "Shelf Leg", new Vector3(-legX, localY, -legZ), new Vector3(0.08f, legHeight, 0.08f));
                AddShelfPart(tower.transform, "Shelf Leg", new Vector3(legX, localY, -legZ), new Vector3(0.08f, legHeight, 0.08f));
                AddShelfPart(tower.transform, "Shelf Leg", new Vector3(-legX, localY, legZ), new Vector3(0.08f, legHeight, 0.08f));
                AddShelfPart(tower.transform, "Shelf Leg", new Vector3(legX, localY, legZ), new Vector3(0.08f, legHeight, 0.08f));
            }

            GunPropPhysicsAssets.ApplyTo(tower);
            SetFixedInWorld(tower, true);
        }

        private static void BuildShelfBridge(Transform props)
        {
            GameObject bridge = PlaceGun(SHELF_UNIT_PREFAB, "Mobile Shelf Bridge", new Vector3(0.03f, 1.032f, 1.64f), Quaternion.identity);
            if (bridge == null)
            {
                return;
            }

            AddShelfPart(bridge.transform, "Shelf Board", Vector3.zero, new Vector3(9.78f, 0.17f, 1.01f));
            GunPropPhysicsAssets.ApplyTo(bridge);
            SetFixedInWorld(bridge, true);
        }

        private static void AddShelfPart(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            part.isStatic = false;
            part.GetComponent<MeshRenderer>().sharedMaterial = BopBuyMaterials.Metal;
        }

        private static GameObject PlaceGun(string prefabPath, string name, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[BopBuyMobileFix] Prefab not found: " + prefabPath);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(GameObject.Find("BopBuyProps")?.transform, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static GameObject PlaceOnFloor(string prefabPath, string name, Vector3 xzPosition, Quaternion rotation)
        {
            GameObject instance = PlaceGun(prefabPath, name, new Vector3(xzPosition.x, 0f, xzPosition.z), rotation);
            if (instance == null)
            {
                return null;
            }

            Bounds bounds = GetBounds(instance);
            instance.transform.position = new Vector3(xzPosition.x, FLOOR_TOP_Y - bounds.min.y + 0.002f, xzPosition.z);
            return instance;
        }

        private static GameObject PlaceOnSurface(string prefabPath, string name, Vector3 xzPosition, float surfaceY, Quaternion rotation)
        {
            GameObject instance = PlaceGun(prefabPath, name, new Vector3(xzPosition.x, 0f, xzPosition.z), rotation);
            if (instance == null)
            {
                return null;
            }

            Physics.SyncTransforms();
            Bounds bounds = GetPlacementBounds(instance);
            instance.transform.position = new Vector3(xzPosition.x, surfaceY - bounds.min.y + PRODUCT_SURFACE_CLEARANCE, xzPosition.z);
            return instance;
        }

        private static GameObject PlaceFlat(string prefabPath, string name, Vector3 xzPosition, float surfaceY, float yaw)
        {
            return PlaceOnSurface(prefabPath, name, xzPosition, surfaceY, Quaternion.Euler(90f, yaw, 0f));
        }

        private static float GetTop(GameObject instance)
        {
            return instance != null ? GetBounds(instance).max.y : FLOOR_TOP_Y;
        }

        private static Bounds GetBounds(GameObject root)
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

        private static Bounds GetPlacementBounds(GameObject root)
        {
            Bounds bounds = GetBounds(root);
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider.enabled)
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return bounds;
        }

        private static bool InMobileRegion(Vector3 position)
        {
            return position.x >= -14f && position.x <= 14f && position.z >= -13f && position.z <= 4f;
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
            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
