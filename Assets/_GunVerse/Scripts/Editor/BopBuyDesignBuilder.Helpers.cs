using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static partial class BopBuyDesignBuilder
    {
        private const float PRODUCT_SURFACE_CLEARANCE = 0.02f;

        private static readonly string[] MobileProducts =
        {
            TABLET_PREFAB, LAPTOP_PREFAB, HEADPHONES_PREFAB, PHONE_PREFAB, FLIP_PHONE_PREFAB,
            SPEAKER_PREFAB, ROUTER_PREFAB, TV_BOX_PREFAB, PHONE_CASE_PREFAB, WALLET_PREFAB
        };

        private static readonly string[] PcProducts =
        {
            MONITOR_PREFAB, COMPUTER_PREFAB, PRINTER_PREFAB, LAPTOP_PREFAB, KEYBOARD_PREFAB,
            MOUSE_PREFAB, ROUTER_PREFAB, SPEAKER_PREFAB, TABLET_PREFAB, TV_BOX_PREFAB
        };

        private static readonly string[] LaptopProducts =
        {
            LAPTOP_PREFAB, COMPUTER_PREFAB, MONITOR_PREFAB, KEYBOARD_PREFAB, MOUSE_PREFAB,
            TABLET_PREFAB, PRINTER_PREFAB
        };

        private static readonly string[] AvProducts =
        {
            LARGE_SPEAKER_PREFAB, SPEAKER_PREFAB, RADIO_PREFAB, TV_BOX_PREFAB, TV32_PREFAB, HEADPHONES_PREFAB
        };

        private static readonly string[] GeneralProducts =
        {
            SPEAKER_PREFAB, TOASTER_PREFAB, RADIO_PREFAB, HEADPHONES_PREFAB, KEYBOARD_PREFAB,
            MOUSE_PREFAB, TV_BOX_PREFAB, TABLET_PREFAB, PHONE_PREFAB
        };

        private static readonly string[] ApplianceDisplayProducts =
        {
            MICROWAVE_PREFAB, TOASTER_PREFAB, RADIO_PREFAB, AIR_CONDITIONER_PREFAB
        };

        private static Transform CreateRoot(string name)
        {
            return new GameObject(name).transform;
        }

        private static GameObject CreateStaticBox(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            return CreateStaticBox(parent, name, position, size, material, Quaternion.identity);
        }

        private static GameObject CreateStaticBox(Transform parent, string name, Vector3 position, Vector3 size, Material material, Quaternion rotation)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.SetPositionAndRotation(position, rotation);
            box.transform.localScale = size;
            box.isStatic = true;
            MeshRenderer renderer = box.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return box;
        }

        private static GameObject PlaceGunAt(string prefabPath, string name, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[BopBuyDesignBuilder] Prefab not found: " + prefabPath);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(props, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static GameObject PlaceOnFloor(string prefabPath, string name, Vector3 xzPosition, Quaternion rotation)
        {
            GameObject instance = PlaceGunAt(prefabPath, name, new Vector3(xzPosition.x, 0f, xzPosition.z), rotation);
            if (instance == null)
            {
                return null;
            }

            Bounds bounds = GetRendererBounds(instance);
            instance.transform.position = new Vector3(xzPosition.x, FLOOR_TOP_Y - bounds.min.y + 0.002f, xzPosition.z);
            return instance;
        }

        private static GameObject PlaceMounted(string prefabPath, string name, Vector3 position, Quaternion rotation)
        {
            GameObject instance = PlaceGunAt(prefabPath, name, position, rotation);
            if (instance != null)
            {
                SetFixedInWorld(instance, true);
            }

            return instance;
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

        private static GameObject PlaceOnSurface(string prefabPath, string name, Vector3 xzPosition, float surfaceY, Quaternion rotation)
        {
            GameObject instance = PlaceGunAt(prefabPath, name, new Vector3(xzPosition.x, 0f, xzPosition.z), rotation);
            if (instance == null)
            {
                return null;
            }

            Physics.SyncTransforms();
            Bounds bounds = GetPlacementBounds(instance);
            instance.transform.position = new Vector3(xzPosition.x, surfaceY - bounds.min.y + PRODUCT_SURFACE_CLEARANCE, xzPosition.z);
            return instance;
        }

        private static GameObject PlaceSurfaceDevice(string prefabPath, string name, Vector3 xzPosition, float surfaceY, Quaternion rotation)
        {
            return PlaceOnSurface(prefabPath, name, xzPosition, surfaceY, rotation);
        }

        private static GameObject PlaceFlatDevice(string prefabPath, string name, Vector3 xzPosition, float surfaceY, float yaw)
        {
            return PlaceOnSurface(prefabPath, name, xzPosition, surfaceY, Quaternion.Euler(90f, yaw, 0f));
        }

        private static void RegisterSurface(GameObject surface)
        {
            if (surface == null)
            {
                return;
            }

            Bounds bounds = GetRendererBounds(surface);
            supportSurfaces.Add(new SupportSurface { Bounds = bounds, TopY = bounds.max.y });
        }

        private static float GetBoundsTop(GameObject instance)
        {
            return instance != null ? GetRendererBounds(instance).max.y : FLOOR_TOP_Y;
        }

        private static float ResolveSurfaceTop(float x, float z, float preferredY, float fallbackTop)
        {
            float bestScore = float.MaxValue;
            float bestTop = fallbackTop;
            bool found = false;

            foreach (SupportSurface surface in supportSurfaces)
            {
                Bounds bounds = surface.Bounds;
                float dx = Mathf.Max(bounds.min.x - x, 0f, x - bounds.max.x);
                float dz = Mathf.Max(bounds.min.z - z, 0f, z - bounds.max.z);
                float horizontalDistance = Mathf.Sqrt(dx * dx + dz * dz);
                if (horizontalDistance > 1.4f)
                {
                    continue;
                }

                float verticalPenalty = preferredY > 0f ? Mathf.Abs(surface.TopY - preferredY) * 0.75f : 0f;
                float score = horizontalDistance * 2f + verticalPenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTop = surface.TopY;
                    found = true;
                }
            }

            return found ? Mathf.Max(bestTop, FLOOR_TOP_Y) : fallbackTop;
        }

        private static bool TryRegisterShelf(Bounds bounds, string name)
        {
            foreach (Bounds existing in shelfBounds)
            {
                if (existing.Intersects(bounds))
                {
                    Debug.Log("[BopBuyDesignBuilder] Skipped overlapping shelf placement: " + name);
                    return false;
                }
            }

            shelfBounds.Add(bounds);
            shelfNames.Add(name);
            return true;
        }

        private static void RegisterShelfSurfaces(GameObject shelf, bool tall, float heightScale = 1f)
        {
            if (shelf == null)
            {
                return;
            }

            Bounds bounds = GetRendererBounds(shelf);
            if (tall)
            {
                float[] heights = { 0.575f, 1.125f, 1.675f, 2.225f, 2.775f };
                foreach (float height in heights)
                {
                    supportSurfaces.Add(new SupportSurface { Bounds = bounds, TopY = FLOOR_TOP_Y + height * heightScale + 0.025f });
                }
            }
            else
            {
                float[] heights = { 0.42f, 0.96f, 1.5f, 2.04f };
                foreach (float height in heights)
                {
                    supportSurfaces.Add(new SupportSurface { Bounds = bounds, TopY = FLOOR_TOP_Y + height * heightScale });
                }
            }
        }

        private static void AddShelfProducts(GameObject shelf, float length, BlockRecord block, bool clearance, float heightScale = 1f)
        {
            if (shelf == null || length < 1f)
            {
                return;
            }

            float scaleZ = length / SHELF_LENGTH;
            float[] boardHeights = { 0.96f, 1.5f };
            int slotCount = length >= 7f ? 5 : 2;
            string[] products = clearance ? LaptopProducts : GetProductSet(block.Position);

            foreach (float boardHeight in boardHeights)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    Quaternion productRotation = shelf.transform.rotation * Quaternion.Euler(0f, side < 0f ? -90f : 90f, 0f);
                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        float worldOffset = -length * 0.5f + (slot + 0.5f) * (length / slotCount);
                        Vector3 localPoint = new Vector3(side * 0.52f, 0f, worldOffset / Mathf.Max(0.01f, scaleZ));
                        Vector3 world = shelf.transform.TransformPoint(localPoint);
                        string prefab = products[productCursor++ % products.Length];
                        PlaceOnSurface(prefab, (clearance ? "Clearance Product" : "Shelf Product"), world, FLOOR_TOP_Y + boardHeight * heightScale, productRotation);
                    }
                }
            }
        }

        private static string[] GetProductSet(Vector3 position)
        {
            if (position.x > 27f && position.z < -12f)
            {
                return LaptopProducts;
            }

            if (position.x > 12f && position.x < 29f && position.z > -11f && position.z < 11.5f)
            {
                return PcProducts;
            }

            if (position.x > -20f && position.x < -6f && position.z > 9f)
            {
                return AvProducts;
            }

            if (position.x > -12f && position.x < 12f && position.z > -12f && position.z < 10f)
            {
                return MobileProducts;
            }

            return GeneralProducts;
        }

        private static bool IsDuplicate(string label, Vector3 position)
        {
            string key = label + "|" + Mathf.RoundToInt(position.x * 10f) + "|" + Mathf.RoundToInt(position.z * 10f);
            return !placedLabelKeys.Add(key);
        }

        private static Bounds GetWorldBounds(BlockRecord block)
        {
            Vector3 half = block.Scale * 0.5f;
            Vector3 axisX = block.Rotation * new Vector3(half.x, 0f, 0f);
            Vector3 axisY = block.Rotation * new Vector3(0f, half.y, 0f);
            Vector3 axisZ = block.Rotation * new Vector3(0f, 0f, half.z);
            Vector3 extent = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(block.Position, extent * 2f);
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
