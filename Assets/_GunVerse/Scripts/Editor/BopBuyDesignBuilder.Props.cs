using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunVerse.EditorTools
{
    public static partial class BopBuyDesignBuilder
    {
        private static void BuildGenericChairs()
        {
            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "chair"))
            {
                if (IsDuplicate("chair", block.Position))
                {
                    continue;
                }

                Vector3 chairPosition = new Vector3(block.Position.x, 0f, block.Position.z);
                GameObject chair = PlaceOnFloor(DESK_CHAIR_PREFAB, "Desk Chair", chairPosition, block.Rotation);
                if (chair == null)
                {
                    continue;
                }

                for (int attempt = 0; attempt < 3 && IntersectsAny(shelfBounds, GetRendererBounds(chair)); attempt++)
                {
                    Bounds chairBounds = GetRendererBounds(chair);
                    Vector3 push = Vector3.zero;
                    foreach (Bounds shelf in shelfBounds)
                    {
                        if (shelf.Intersects(chairBounds))
                        {
                            push = chair.transform.position - shelf.center;
                            break;
                        }
                    }

                    if (push.sqrMagnitude < 0.01f)
                    {
                        push = block.Rotation * Vector3.forward;
                    }

                    chair.transform.position += new Vector3(push.x, 0f, push.z).normalized * 0.55f;
                    Physics.SyncTransforms();
                }

                if (IntersectsAny(shelfBounds, GetRendererBounds(chair)))
                {
                    UnityEngine.Object.DestroyImmediate(chair);
                }
            }
        }

        private static void BuildSurfaceDevices()
        {
            foreach (BlockRecord block in blocks)
            {
                bool isTriple = block.LowerLabel == "monitor, mouse, keyboard" ||
                                block.LowerLabel == "monitor mouse and keyboard" ||
                                block.LowerLabel == "monitor + keyboard + mouse";
                bool isPrinter = block.LowerLabel == "printer";
                if (!isTriple && !isPrinter)
                {
                    continue;
                }

                if (Mathf.Abs(block.Position.x) < 0.6f && block.Position.z < -22.8f)
                {
                    continue;
                }

                if (block.Position.x > 28.8f && block.Position.z > 2f && block.Position.z < 9f)
                {
                    continue;
                }

                if (IsDuplicate(isPrinter ? "printer" : "monitor-set", block.Position))
                {
                    continue;
                }

                float fallbackTop = block.Position.y > 0f ? block.Position.y : 0.9f;
                float top = ResolveSurfaceTop(block.Position.x, block.Position.z, block.Position.y, fallbackTop);
                if (isPrinter)
                {
                    PlaceSurfaceDevice(PRINTER_PREFAB, "Printer", block.Position, top, block.Rotation);
                    continue;
                }

                Vector3 forward = block.Rotation * Vector3.forward;
                PlaceSurfaceDevice(MONITOR_PREFAB, "Monitor", block.Position, top, block.Rotation);
                PlaceSurfaceDevice(KEYBOARD_PREFAB, "Keyboard", block.Position + forward * 0.36f, top, block.Rotation);
                PlaceSurfaceDevice(MOUSE_PREFAB, "Mouse", block.Position + forward * 0.36f + block.Rotation * Vector3.right * 0.34f, top, block.Rotation);
            }
        }

        private static void BuildMiscProps()
        {
            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "trash"))
            {
                if (!IsDuplicate("trash", block.Position))
                {
                    PlaceOnFloor(TRASH_BIN_PREFAB, "Trash Bin", new Vector3(block.Position.x, 0f, block.Position.z), block.Rotation);
                }
            }

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "camera"))
            {
                PlaceMounted(SECURITY_CAMERA_PREFAB, "Security Camera", block.Position, Quaternion.Euler(0f, 270f, 0f));
            }

        }

        private static void BuildSignage()
        {
            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "various bopbuy signage"))
            {
                PlaceWallSign(BOPBUY_BANNER_PREFAB, "BopBuy Banner", block.Position, SignRotation(block), 7.166f, 2.39f);
            }

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "hanging relative area sign"))
            {
                string prefab = ChooseHangingSign(block.Position);
                PlaceHangingSign(prefab, "Hanging Area Sign", block.Position, SignRotation(block), 7.166f, 2.39f);
            }

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "large guy squad sign above door"))
            {
                PlaceWallSign(GUYSQUAD_WALL_SIGN_PREFAB, "Guysquad Wall Sign", block.Position, SignRotation(block), 7.07f, 2.45f);
            }

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "guysquad sign hanging"))
            {
                PlaceHangingSign(GUYSQUAD_HANGING_SIGN_PREFAB, "Guysquad Hanging Sign", block.Position, SignRotation(block), 7.166f, 2.39f);
            }

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel.StartsWith("welcome sign + arrow", StringComparison.Ordinal)))
            {
                PlaceOnFloor(WELCOME_SIGN_PREFAB, "Welcome Directory", new Vector3(block.Position.x, 0f, block.Position.z), block.Rotation);
            }

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel.StartsWith("sign saying (\"no breaks", StringComparison.Ordinal)))
            {
                PlaceWallSign(NO_BREAKS_SIGN_PREFAB, "No Breaks Sign", block.Position, SignRotation(block), 1.52f, 2.45f);
            }

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel.StartsWith("sign saying \"behind you\"", StringComparison.Ordinal)))
            {
                PlaceWallSign(BEHIND_YOU_SIGN_PREFAB, "Behind You Sign", block.Position, SignRotation(block), 3.98f, 1.52f);
            }

        }

        private static Quaternion SignRotation(BlockRecord block)
        {
            return block.Scale.z > block.Scale.x
                ? block.Rotation * Quaternion.Euler(0f, 90f, 0f)
                : block.Rotation;
        }

        private static string ChooseHangingSign(Vector3 position)
        {
            float leftSideX = -18f;
            float checkoutFrontZ = -18f;
            float computersX = 15f;
            float homeTheatreZ = 8f;

            bool isLeftSide = position.x < leftSideX;
            bool isCheckoutZone = isLeftSide && position.z < checkoutFrontZ;
            bool isAppliancesZone = isLeftSide && !isCheckoutZone;

            if (isCheckoutZone)
            {
                return HANGING_CHECKOUT_SIGN_PREFAB;
            }

            if (isAppliancesZone)
            {
                return HANGING_APPLIANCES_SIGN_PREFAB;
            }

            if (position.x > computersX)
            {
                return HANGING_COMPUTERS_SIGN_PREFAB;
            }

            if (position.z > homeTheatreZ)
            {
                return HANGING_HOME_THEATRE_SIGN_PREFAB;
            }

            return HANGING_MOBILE_SIGN_PREFAB;
        }

        private static void PlaceWallSign(string prefab, string name, Vector3 position, Quaternion rotation, float width, float height)
        {
            GameObject sign = PlaceMounted(prefab, name, position, rotation);
            Vector3 normal = rotation * Vector3.forward;
            Physics.SyncTransforms();
            float positiveDistance = FindBackingDistance(position, normal, sign);
            float negativeDistance = FindBackingDistance(position, -normal, sign);
            bool positiveCloser = positiveDistance < negativeDistance;
            Vector3 supportPosition = position + normal * (positiveCloser ? 0.075f : -0.075f);
            CreateStaticBox(environment, name + " Support", supportPosition, new Vector3(width + 0.12f, height + 0.12f, 0.04f), BopBuyMaterials.Metal, rotation);
        }

        private static float FindBackingDistance(Vector3 origin, Vector3 direction, GameObject ignore)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, 1.6f, ~0, QueryTriggerInteraction.Ignore);
            float distance = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                if (ignore != null && hit.collider.transform.IsChildOf(ignore.transform))
                {
                    continue;
                }

                if (hit.distance < 0.02f)
                {
                    continue;
                }

                distance = Mathf.Min(distance, hit.distance);
            }

            return distance;
        }

        private static void PlaceHangingSign(string prefab, string name, Vector3 position, Quaternion rotation, float width, float height)
        {
            PlaceMounted(prefab, name, position, rotation);
            Vector3 right = rotation * Vector3.right;
            Vector3 top = position + Vector3.up * (height * 0.5f + 0.12f);
            CreateStaticBox(environment, name + " Hanger Bar", top, new Vector3(width * 0.92f, 0.08f, 0.08f), BopBuyMaterials.Metal, rotation);
            Vector3 cableOffset = right * (width * 0.35f);
            CreateStaticBox(environment, name + " Cable Left", top - cableOffset + Vector3.up * 0.12f, new Vector3(0.025f, 0.36f, 0.025f), BopBuyMaterials.Metal);
            CreateStaticBox(environment, name + " Cable Right", top + cableOffset + Vector3.up * 0.12f, new Vector3(0.025f, 0.36f, 0.025f), BopBuyMaterials.Metal);
        }

        private static void BuildCeilingLights()
        {
            float[] xPositions = { -16f, -4f, 8f, 20f, 32f };
            float[] zPositions = { -17f, -5f, 7f, 19f };
            int index = 0;
            foreach (float x in xPositions)
            {
                foreach (float z in zPositions)
                {
                    PlaceCeilingLight(x, z, ++index);
                }
            }

            PlaceCeilingLight(-36f, -21.5f, ++index);
            PlaceCeilingLight(-30f, -21.5f, ++index);
            PlaceCeilingLight(-24f, -21.5f, ++index);
            PlaceCeilingLight(0f, -22.4f, ++index);
            PlaceCeilingLight(32f, -21.5f, ++index);
            PlaceCeilingLight(2f, -4f, ++index);
            PlaceCeilingLight(-13f, 20f, ++index);
            PlaceCeilingLight(18f, 21f, ++index);
        }

        private static void PlaceCeilingLight(float x, float z, int index)
        {
            PlaceMounted(CEILING_LIGHT_PREFAB, "Ceiling Light " + index, new Vector3(x, 7.55f, z), Quaternion.identity);
            CreateStaticBox(lightingRoot, "Light Support " + index, new Vector3(x, 7.66f, z), new Vector3(0.12f, 0.04f, 3.5f), BopBuyMaterials.Metal);
            CreateStaticBox(lightingRoot, "Light Hanger " + index + " A", new Vector3(x, 7.9f, z - 1.6f), new Vector3(0.025f, 0.5f, 0.025f), BopBuyMaterials.Metal);
            CreateStaticBox(lightingRoot, "Light Hanger " + index + " B", new Vector3(x, 7.9f, z + 1.6f), new Vector3(0.025f, 0.5f, 0.025f), BopBuyMaterials.Metal);
        }

        private static void BuildApplianceAtmosphere()
        {
            float[] xs = { -36f, -30f, -24f };
            float[] zs = { -6f, 5f, 16f };
            for (int index = 0; index < xs.Length; index++)
            {
                for (int zIndex = 0; zIndex < zs.Length; zIndex++)
                {
                    if ((index + zIndex) % 2 != 0)
                    {
                        continue;
                    }

                    CreatePointLight(new Vector3(xs[index], 3.1f, zs[zIndex]), index % 2 == 0 ? ArtPalette.BopBuy.Blue : ArtPalette.BopBuy.Yellow, 2.6f, 7f);
                }
            }
        }

        private static void CreatePointLight(Vector3 position, Color color, float intensity, float range)
        {
            GameObject fixture = CreateStaticBox(lightingRoot, "Creepy Light", new Vector3(position.x, position.y + 0.12f, position.z), new Vector3(0.4f, 0.08f, 0.4f), BopBuyMaterials.Metal);
            Light light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void BuildSpawnPoints()
        {
            List<Vector3> candidates = new List<Vector3>();
            for (float x = -36f; x <= 36f; x += 3.2f)
            {
                for (float z = -23f; z <= 23f; z += 3.2f)
                {
                    if (IsExcludedSpawnArea(x, z))
                    {
                        continue;
                    }

                    candidates.Add(new Vector3(x, FLOOR_TOP_Y + 0.06f, z));
                }
            }

            Shuffle(candidates);
            List<Vector3> chosen = new List<Vector3>();
            Physics.SyncTransforms();
            foreach (Vector3 candidate in candidates)
            {
                if (chosen.Count >= TARGET_SPAWN_COUNT)
                {
                    break;
                }

                if (chosen.Any(point => Vector3.Distance(point, candidate) < 3.6f))
                {
                    continue;
                }

                if (IsSpawnBlocked(candidate))
                {
                    continue;
                }

                chosen.Add(candidate);
            }

            if (chosen.Count < 12)
            {
                foreach (Vector3 candidate in candidates)
                {
                    if (chosen.Count >= TARGET_SPAWN_COUNT)
                    {
                        break;
                    }

                    if (chosen.Any(point => Vector3.Distance(point, candidate) < 2.4f) || IsSpawnBlocked(candidate))
                    {
                        continue;
                    }

                    chosen.Add(candidate);
                }
            }

            for (int index = 0; index < chosen.Count; index++)
            {
                Vector3 position = chosen[index];
                Vector3 direction = new Vector3(-position.x, 0f, -position.z);
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Vector3.back;
                }

                GameObject spawnObject = new GameObject("Spawn Point " + (index + 1));
                spawnObject.transform.SetParent(spawnRoot, false);
                spawnObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
                spawnObject.AddComponent<PlayerSpawnPoint>();
            }

            Debug.Log($"[BopBuyDesignBuilder] Spawn points: {chosen.Count}.");
        }

        private static bool IsExcludedSpawnArea(float x, float z)
        {
            if (x > 32.2f && z > -0.2f)
            {
                return true;
            }

            if (x > -4.5f && x < 31f && z > 18.6f)
            {
                return true;
            }

            if (x > 27.3f && x < 39.2f && z > -8.5f && z < -0.2f)
            {
                return true;
            }

            if (Mathf.Abs(x) > 38f || Mathf.Abs(z) > 24f)
            {
                return true;
            }

            return false;
        }

        private static bool IsSpawnBlocked(Vector3 point)
        {
            Vector3 bottom = point + Vector3.up * 0.55f;
            Vector3 top = point + Vector3.up * 1.45f;
            Collider[] hits = Physics.OverlapCapsule(bottom, top, 0.36f, ~0, QueryTriggerInteraction.Ignore);
            return hits.Length > 0;
        }

        private static void Shuffle(List<Vector3> values)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swap = random.Next(0, index + 1);
                (values[index], values[swap]) = (values[swap], values[index]);
            }
        }

        private static void ConfigureSceneLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ArtPalette.BopBuy.Ambient;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            Light directional = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                .FirstOrDefault(light => light.type == LightType.Directional);
            if (directional != null)
            {
                directional.color = ArtPalette.BopBuy.KeyLight;
                directional.intensity = 1.1f;
                directional.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
                directional.shadows = LightShadows.Soft;
                RenderSettings.sun = directional;
            }
        }

        private static void ValidateScene()
        {
            int propCount = 0;
            foreach (GunItem gunItem in UnityEngine.Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include))
            {
                if (gunItem.transform.root.name == "BopBuyProps")
                {
                    propCount++;
                }
            }

            int spawnCount = UnityEngine.Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include).Length;
            int shelfOverlaps = 0;
            for (int left = 0; left < shelfBounds.Count; left++)
            {
                for (int right = left + 1; right < shelfBounds.Count; right++)
                {
                    if (shelfBounds[left].Intersects(shelfBounds[right]))
                    {
                        shelfOverlaps++;
                        Debug.LogError($"[BopBuyDesignBuilder] Shelf overlap: {shelfNames[left]} vs {shelfNames[right]}.");
                    }
                }
            }

            GameObject ceiling = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Where(transform => transform.name == "StoreCeiling")
                .Select(transform => transform.gameObject)
                .FirstOrDefault();
            if (ceiling == null)
            {
                Debug.LogError("[BopBuyDesignBuilder] StoreCeiling is missing.");
            }
            else
            {
                if (!ceiling.activeInHierarchy)
                {
                    Debug.LogError("[BopBuyDesignBuilder] StoreCeiling is inactive.");
                }

                if (ceiling.GetComponent<Collider>() == null)
                {
                    Debug.LogError("[BopBuyDesignBuilder] StoreCeiling has no collider.");
                }
            }

            Debug.Log($"[BopBuyDesignBuilder] Validation: {propCount} props, {spawnCount} spawns, {shelfBounds.Count} shelves, {shelfOverlaps} shelf overlaps.");
            Debug.Log($"[BopBuyDesignBuilder] Sightline sample: {MeasureLongestSightline(new Vector3(0f, 1.65f, 0f)):0.0} m at store center.");
            if (propCount < 350)
            {
                Debug.LogWarning("[BopBuyDesignBuilder] Prop count is lower than the density target.");
            }
        }

        private static float MeasureLongestSightline(Vector3 origin)
        {
            float longest = 0f;
            Vector3[] directions =
            {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                new Vector3(1f, 0f, 1f).normalized, new Vector3(-1f, 0f, 1f).normalized,
                new Vector3(1f, 0f, -1f).normalized, new Vector3(-1f, 0f, -1f).normalized
            };

            foreach (Vector3 direction in directions)
            {
                if (Physics.Raycast(origin, direction, out RaycastHit hit, 80f))
                {
                    longest = Mathf.Max(longest, hit.distance);
                }
            }

            return longest;
        }
    }
}
