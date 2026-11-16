using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static partial class BopBuyDesignBuilder
    {
        private const int MAX_BUILDER_TOP_ITEMS = 18;
        private const float BUILDER_TOP_ITEM_CHANCE = 0.35f;
        private const float BUILDER_TOP_ITEM_GAP = 0.01f;

        private static readonly List<Bounds> applianceTopBounds = new List<Bounds>();
        private static int applianceTopItemCount;

        private static void BuildApplianceFixtures()
        {
            applianceTopBounds.Clear();
            applianceTopItemCount = 0;

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "random fridge" || block.LowerLabel == "random single washer/dryer/oven"))
            {
                if (IsDuplicate(block.LowerLabel, block.Position))
                {
                    continue;
                }

                bool isFridge = block.LowerLabel == "random fridge";
                uint stableHash = BopBuyApplianceVarietyCatalog.ComputeStableHash(block.Name, block.Position);
                ApplianceCategory category = isFridge
                    ? ApplianceCategory.Fridge
                    : BopBuyApplianceVarietyCatalog.ChooseLaundryCategory(stableHash);
                string prefab = BopBuyApplianceVarietyCatalog.ChooseVariantPrefab(category, stableHash);
                GameObject appliance = PlaceOnFloor(prefab, isFridge ? "Refrigerator" : "Washer Dryer Oven", new Vector3(block.Position.x, 0f, block.Position.z), block.Rotation);
                if (appliance == null)
                {
                    continue;
                }

                Bounds bounds = GetRendererBounds(appliance);
                if (IntersectsAny(shelfBounds, bounds) || IntersectsAny(largeApplianceBounds, bounds))
                {
                    UnityEngine.Object.DestroyImmediate(appliance);
                    continue;
                }

                largeApplianceBounds.Add(bounds);
                if (BopBuyApplianceVarietyCatalog.IsTopItemHostCategory(category))
                {
                    TryPlaceBuilderTopItem(bounds, stableHash);
                }
            }

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "vending machine"))
            {
                if (IsDuplicate(block.LowerLabel, block.Position))
                {
                    continue;
                }

                uint stableHash = BopBuyApplianceVarietyCatalog.ComputeStableHash(block.Name, block.Position);
                string prefab = BopBuyApplianceVarietyCatalog.ChooseVariantPrefab(ApplianceCategory.Vending, stableHash);
                GameObject vending = PlaceOnFloor(prefab, "Vending Machine", new Vector3(block.Position.x, 0f, block.Position.z), block.Rotation);
                if (vending != null)
                {
                    largeApplianceBounds.Add(GetRendererBounds(vending));
                }
            }

        }

        private static void TryPlaceBuilderTopItem(Bounds hostBounds, uint stableHash)
        {
            if (applianceTopItemCount >= MAX_BUILDER_TOP_ITEMS)
            {
                return;
            }

            if (!BopBuyApplianceVarietyCatalog.StableChance(stableHash, BUILDER_TOP_ITEM_CHANCE))
            {
                return;
            }

            string prefab = BopBuyApplianceVarietyCatalog.ChooseTopItemPrefab(stableHash);
            if (string.IsNullOrEmpty(prefab))
            {
                return;
            }

            string displayName = BopBuyApplianceVarietyCatalog.GetTopItemDisplayName(prefab);
            float yaw = BopBuyApplianceVarietyCatalog.ChooseTopItemYaw(stableHash);
            GameObject topItem = PlaceOnSurface(
                prefab,
                "Appliance Top " + displayName,
                new Vector3(hostBounds.center.x, 0f, hostBounds.center.z),
                hostBounds.max.y,
                Quaternion.Euler(0f, yaw, 0f));
            if (topItem == null)
            {
                return;
            }

            Bounds topBounds = GetRendererBounds(topItem);
            Vector3 offset = hostBounds.center - topBounds.center;
            offset.y = hostBounds.max.y - topBounds.min.y + BUILDER_TOP_ITEM_GAP;
            topItem.transform.position += offset;
            topBounds = GetRendererBounds(topItem);

            if (IntersectsAny(shelfBounds, topBounds) || IntersectsAny(applianceTopBounds, topBounds))
            {
                UnityEngine.Object.DestroyImmediate(topItem);
                return;
            }

            applianceTopBounds.Add(topBounds);
            applianceTopItemCount++;
        }

        private static void BuildTallFridgeRows()
        {
            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "very tall unable to jump over fridges"))
            {
                float length = block.Scale.x;
                int count = Mathf.Max(1, Mathf.FloorToInt(length / 1.55f));
                for (int index = 0; index < count; index++)
                {
                    float offset = -length * 0.5f + (index + 0.5f) * (length / count);
                    Vector3 position = block.Position + block.Rotation * new Vector3(offset, 0f, 0f);
                    uint stableHash = BopBuyApplianceVarietyCatalog.ComputeStableHash(block.Name + "|" + index, position);
                    string prefab = BopBuyApplianceVarietyCatalog.ChooseVariantPrefab(ApplianceCategory.Fridge, stableHash);
                    GameObject fridge = PlaceOnFloor(prefab, "Very Tall Fridge", new Vector3(position.x, 0f, position.z), block.Rotation);
                    if (fridge != null)
                    {
                        Bounds bounds = GetRendererBounds(fridge);
                        largeApplianceBounds.Add(bounds);
                        TryPlaceBuilderTopItem(bounds, stableHash);
                    }
                }
            }
        }

        private static void BuildAisles()
        {
            List<Vector3> clearancePositions = blocks
                .Where(block => block.LowerLabel == "clearance laptops")
                .Select(block => block.Position)
                .ToList();

            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel == "aisle full of relative product"))
            {
                if (IsDuplicate("aisle", block.Position))
                {
                    continue;
                }

                float length = block.Scale.x;
                if (length < 1.5f)
                {
                    continue;
                }

                if (IsMobileEntranceShelf(block))
                {
                    BuildMobileDisplayTable(block);
                    continue;
                }

                float heightScale = IsMobileSideShelf(block) ? 0.5f : 1f;
                bool clearance = clearancePositions.Any(position => Mathf.Abs(position.x - block.Position.x) < 0.4f && Mathf.Abs(position.z - block.Position.z) < 0.4f);
                Quaternion shelfRotation = block.Rotation * Quaternion.Euler(0f, 90f, 0f);
                GameObject shelf = PlaceGunAt(
                    SHELF_RUN_PREFAB,
                    clearance ? "Clearance Laptop Shelf" : "Shelf Run",
                    new Vector3(block.Position.x, FLOOR_TOP_Y + 1.1f * heightScale, block.Position.z),
                    shelfRotation);
                if (shelf == null)
                {
                    continue;
                }

                shelf.transform.localScale = new Vector3(1f, heightScale, length / SHELF_LENGTH);
                Physics.SyncTransforms();
                Bounds bounds = GetRendererBounds(shelf);
                if (!TryRegisterShelf(bounds, shelf.name))
                {
                    UnityEngine.Object.DestroyImmediate(shelf);
                    continue;
                }

                RegisterShelfSurfaces(shelf, false, heightScale);
                AddShelfProducts(shelf, length, block, clearance, heightScale);
            }
        }

        private static bool IsMobileEntranceShelf(BlockRecord block)
        {
            return (Mathf.Abs(block.Position.x + 0.93f) < 0.3f && Mathf.Abs(block.Position.z + 7.48f) < 0.3f) ||
                   (Mathf.Abs(block.Position.x - 3.94f) < 0.3f && Mathf.Abs(block.Position.z + 7.32f) < 0.3f);
        }

        private static bool IsMobileSideShelf(BlockRecord block)
        {
            return (Mathf.Abs(block.Position.x + 10.07f) < 0.3f && Mathf.Abs(block.Position.z + 5.6f) < 0.3f) ||
                   (Mathf.Abs(block.Position.x + 7.43f) < 0.3f && Mathf.Abs(block.Position.z + 5.7f) < 0.3f) ||
                   (Mathf.Abs(block.Position.x + 4.68f) < 0.3f && Mathf.Abs(block.Position.z + 5.7f) < 0.3f) ||
                   (Mathf.Abs(block.Position.x - 10.78f) < 0.3f && Mathf.Abs(block.Position.z + 5.85f) < 0.3f) ||
                   (Mathf.Abs(block.Position.x - 8.03f) < 0.3f && Mathf.Abs(block.Position.z + 5.85f) < 0.3f);
        }

        private static void BuildMobileDisplayTable(BlockRecord block)
        {
            GameObject table = PlaceOnFloor(DESIGN_TABLE_PREFAB, "Mobile Display Table", block.Position, block.Rotation);
            if (table == null)
            {
                return;
            }

            RegisterSurface(table);
            float top = GetBoundsTop(table);
            Vector3 forward = block.Rotation * Vector3.forward;
            Vector3 right = block.Rotation * Vector3.right;
            PlaceFlatDevice(PHONE_PREFAB, "Mobile Display Phone 1", block.Position - right * 0.85f + forward * 0.15f, top, 12f);
            PlaceFlatDevice(PHONE_PREFAB, "Mobile Display Phone 2", block.Position - right * 0.4f + forward * 0.05f, top, -8f);
            PlaceFlatDevice(TABLET_PREFAB, "Mobile Display Tablet", block.Position + right * 0.25f + forward * 0.1f, top, 6f);
            PlaceSurfaceDevice(LAPTOP_PREFAB, "Mobile Display Laptop", block.Position + right * 0.9f - forward * 0.1f, top, block.Rotation);
        }

        private static void BuildGuysquadRoom()
        {
            List<BlockRecord> tallFrames = CollectShelfObjects(block => block.LowerLabel.Contains("tall shelf"));
            List<BlockRecord> boards = CollectShelfObjects(block =>
                block.LowerLabel == "shelf" ||
                block.LowerLabel.StartsWith("shelf head chest", StringComparison.Ordinal) ||
                block.LowerLabel.StartsWith("shelf sitting", StringComparison.Ordinal));

            HashSet<BlockRecord> usedBoards = new HashSet<BlockRecord>();
            foreach (BlockRecord frame in tallFrames)
            {
                BuildTallShelfUnit(frame, boards, usedBoards);
            }

            List<BlockRecord> remainingBoards = boards.Where(board => !usedBoards.Contains(board)).ToList();
            foreach (List<BlockRecord> group in GroupShelfBoardsByProximity(remainingBoards))
            {
                BuildShelfBoardUnit(group);
            }

            int productIndex = 0;
            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel.StartsWith("laptop/desktop spam", StringComparison.Ordinal)))
            {
                if (IsDuplicate("laptop", block.Position))
                {
                    continue;
                }

                float top = ResolveSurfaceTop(block.Position.x, block.Position.z, block.Position.y, FLOOR_TOP_Y);
                string prefab = productIndex % 3 == 0 ? COMPUTER_PREFAB : LAPTOP_PREFAB;
                PlaceOnSurface(prefab, "Laptop Desktop Product", block.Position, top, block.Rotation);
                productIndex++;
            }

            PlaceMounted(WHITEBOARD_PREFAB, "Guysquad Whiteboard", new Vector3(28.94f, 3.12f, -16.29f), Quaternion.Euler(0f, 90f, 0f));
            CreateStaticBox(environment, "Whiteboard Mount", new Vector3(29.04f, 3.12f, -16.29f), new Vector3(0.06f, 1.5f, 1.9f), BopBuyMaterials.Metal);
        }

        private static List<BlockRecord> CollectShelfObjects(Func<BlockRecord, bool> predicate)
        {
            List<BlockRecord> result = new List<BlockRecord>();
            HashSet<string> seen = new HashSet<string>();
            foreach (BlockRecord source in blocks.Where(predicate))
            {
                string key = source.LowerLabel + "|" +
                             Mathf.RoundToInt(source.Position.x * 100f) + "|" +
                             Mathf.RoundToInt(source.Position.y * 100f) + "|" +
                             Mathf.RoundToInt(source.Position.z * 100f);
                if (!seen.Add(key))
                {
                    continue;
                }

                result.Add(ClampShelfBlock(source));
            }

            return result;
        }

        private static BlockRecord ClampShelfBlock(BlockRecord source)
        {
            Bounds original = GetWorldBounds(source);
            float halfX = original.size.x * 0.5f;
            float halfZ = original.size.z * 0.5f;
            float clampedX = source.Position.x;
            float clampedZ = source.Position.z;

            if (original.size.x <= 77.8f)
            {
                clampedX = Mathf.Clamp(source.Position.x, -38.9f + halfX, 39.0f - halfX);
            }

            if (original.size.z <= 49.6f)
            {
                clampedZ = Mathf.Clamp(source.Position.z, -24.8f + halfZ, 24.75f - halfZ);
            }

            if (clampedX > 27f && clampedZ > -14.5f && clampedZ < -12.5f)
            {
                clampedZ = Mathf.Max(clampedZ, -13.45f + halfZ);
            }

            if (Mathf.Abs(clampedX - source.Position.x) > 0.001f || Mathf.Abs(clampedZ - source.Position.z) > 0.001f)
            {
                Debug.Log($"[BopBuyDesignBuilder] Moved wall-adjacent shelf '{source.Label}' inside the shell by {clampedX - source.Position.x:0.00},{clampedZ - source.Position.z:0.00}.");
            }

            return new BlockRecord
            {
                Name = source.Name,
                Label = source.Label,
                LowerLabel = source.LowerLabel,
                Position = new Vector3(clampedX, source.Position.y, clampedZ),
                Rotation = source.Rotation,
                Scale = source.Scale
            };
        }

        private static void BuildTallShelfUnit(BlockRecord frame, List<BlockRecord> boards, HashSet<BlockRecord> usedBoards)
        {
            GameObject unit = PlaceGunAt(SHELF_UNIT_PREFAB, "Guysquad Shelf", frame.Position, frame.Rotation);
            if (unit == null)
            {
                return;
            }

            float width = Mathf.Max(0.2f, frame.Scale.x);
            float height = Mathf.Max(0.2f, frame.Scale.y);
            float depth = Mathf.Max(0.2f, frame.Scale.z);

            AddShelfPart(unit.transform, "Shelf Side Left", new Vector3(-(width * 0.5f - 0.03f), 0f, 0f), new Vector3(0.06f, height, depth), BopBuyMaterials.Metal);
            AddShelfPart(unit.transform, "Shelf Side Right", new Vector3(width * 0.5f - 0.03f, 0f, 0f), new Vector3(0.06f, height, depth), BopBuyMaterials.Metal);
            AddShelfPart(unit.transform, "Shelf Top", new Vector3(0f, height * 0.5f - 0.03f, 0f), new Vector3(width, 0.06f, depth), BopBuyMaterials.Metal);
            AddShelfPart(unit.transform, "Shelf Bottom", new Vector3(0f, -height * 0.5f + 0.03f, 0f), new Vector3(width, 0.06f, depth), BopBuyMaterials.Metal);

            float bottomWorld = frame.Position.y - height * 0.5f;
            float legHeight = bottomWorld - FLOOR_TOP_Y;
            if (legHeight > 0.01f)
            {
                AddShelfLegs(unit.transform, width, depth, -height * 0.5f - legHeight * 0.5f, legHeight);
            }

            Bounds frameBounds = GetWorldBounds(frame);
            foreach (BlockRecord board in boards)
            {
                if (usedBoards.Contains(board))
                {
                    continue;
                }

                Bounds boardBounds = GetWorldBounds(board);
                if (!OverlapsXZ(frameBounds, boardBounds, 0.05f))
                {
                    continue;
                }

                AttachShelfBoard(unit, frame.Position, frame.Rotation, board);
                usedBoards.Add(board);
            }

            RegisterShelfUnit(unit, "Guysquad Tall Shelf");
        }

        private static List<List<BlockRecord>> GroupShelfBoardsByProximity(List<BlockRecord> boards)
        {
            List<List<BlockRecord>> groups = new List<List<BlockRecord>>();
            foreach (BlockRecord board in boards)
            {
                List<BlockRecord> target = null;
                foreach (List<BlockRecord> group in groups)
                {
                    BlockRecord anchor = group[0];
                    if (Mathf.Abs(anchor.Position.x - board.Position.x) < 0.3f &&
                        Mathf.Abs(anchor.Position.z - board.Position.z) < 0.3f)
                    {
                        target = group;
                        break;
                    }
                }

                if (target == null)
                {
                    target = new List<BlockRecord>();
                    groups.Add(target);
                }

                target.Add(board);
            }

            return groups;
        }

        private static void BuildShelfBoardUnit(List<BlockRecord> boards)
        {
            if (boards.Count == 0)
            {
                return;
            }

            Bounds bounds = GetWorldBounds(boards[0]);
            foreach (BlockRecord board in boards)
            {
                bounds.Encapsulate(GetWorldBounds(board));
            }

            float maxTop = boards.Max(board => board.Position.y + board.Scale.y * 0.5f);
            if (maxTop <= FLOOR_TOP_Y)
            {
                return;
            }

            Vector3 center = new Vector3(bounds.center.x, (FLOOR_TOP_Y + maxTop) * 0.5f, bounds.center.z);
            GameObject unit = PlaceGunAt(SHELF_UNIT_PREFAB, "Guysquad Shelf", center, Quaternion.identity);
            if (unit == null)
            {
                return;
            }

            AddShelfLegs(unit.transform, bounds.size.x, bounds.size.z, 0f, maxTop - FLOOR_TOP_Y);
            foreach (BlockRecord board in boards)
            {
                AttachShelfBoard(unit, center, Quaternion.identity, board);
            }

            RegisterShelfUnit(unit, "Guysquad Shelf");
        }

        private static void AttachShelfBoard(GameObject unit, Vector3 unitPosition, Quaternion unitRotation, BlockRecord board)
        {
            Vector3 localPosition = Quaternion.Inverse(unitRotation) * (board.Position - unitPosition);
            Quaternion localRotation = Quaternion.Inverse(unitRotation) * board.Rotation;
            GameObject boardObject = AddShelfPart(unit.transform, "Shelf Board", localPosition, board.Scale, BopBuyMaterials.Metal, localRotation);
            Bounds bounds = GetRendererBounds(boardObject);
            supportSurfaces.Add(new SupportSurface { Bounds = bounds, TopY = bounds.max.y });
        }

        private static void AddShelfLegs(Transform parent, float width, float depth, float localCenterY, float height)
        {
            float offsetX = Mathf.Max(0.04f, width * 0.5f - 0.08f);
            float offsetZ = Mathf.Max(0.04f, depth * 0.5f - 0.08f);
            AddShelfPart(parent, "Shelf Leg", new Vector3(-offsetX, localCenterY, -offsetZ), new Vector3(0.08f, height, 0.08f), BopBuyMaterials.Metal);
            AddShelfPart(parent, "Shelf Leg", new Vector3(offsetX, localCenterY, -offsetZ), new Vector3(0.08f, height, 0.08f), BopBuyMaterials.Metal);
            AddShelfPart(parent, "Shelf Leg", new Vector3(-offsetX, localCenterY, offsetZ), new Vector3(0.08f, height, 0.08f), BopBuyMaterials.Metal);
            AddShelfPart(parent, "Shelf Leg", new Vector3(offsetX, localCenterY, offsetZ), new Vector3(0.08f, height, 0.08f), BopBuyMaterials.Metal);
        }

        private static GameObject AddShelfPart(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, Quaternion? localRotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation ?? Quaternion.identity;
            part.transform.localScale = size;
            part.isStatic = false;
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return part;
        }

        private static void RegisterShelfUnit(GameObject unit, string name)
        {
            GunPropPhysicsAssets.ApplyTo(unit);
            Bounds bounds = GetRendererBounds(unit);
            shelfBounds.Add(bounds);
            shelfNames.Add(name);
        }

        private static bool OverlapsXZ(Bounds left, Bounds right, float padding)
        {
            return left.min.x - padding <= right.max.x &&
                   left.max.x + padding >= right.min.x &&
                   left.min.z - padding <= right.max.z &&
                   left.max.z + padding >= right.min.z;
        }

        private static void BuildMountedDisplays()
        {
            foreach (BlockRecord block in blocks.Where(block => block.LowerLabel.Contains("mounted") && block.LowerLabel.Contains("packed")))
            {
                bool monitors = block.LowerLabel.StartsWith("monitors", StringComparison.Ordinal);
                Bounds bounds = GetWorldBounds(block);
                float length = Mathf.Max(bounds.size.x, bounds.size.z);
                Vector3 right = block.Rotation * Vector3.right;
                Vector3 up = Vector3.up;
                Vector3 normal = block.Rotation * Vector3.forward;

                Vector3 target = monitors
                    ? new Vector3(block.Position.x - 3f, block.Position.y, block.Position.z)
                    : block.Position.z > 18f
                        ? new Vector3(block.Position.x, block.Position.y, block.Position.z - 3f)
                        : block.Position.x < -6f
                            ? new Vector3(block.Position.x - 3f, block.Position.y, block.Position.z)
                            : new Vector3(block.Position.x, block.Position.y, block.Position.z - 3f);
                if (Vector3.Dot(normal, target - block.Position) < 0f)
                {
                    normal = -normal;
                }

                bool needsRack = !monitors && block.Position.z > 15.5f && block.Position.z < 17f;
                if (needsRack)
                {
                    BuildTvMountRack(block, bounds, right, length);
                }

                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                int perRow = monitors
                    ? Mathf.Max(3, Mathf.FloorToInt(length / 0.85f))
                    : Mathf.Max(2, Mathf.FloorToInt(length / 2f));
                for (int row = 0; row < 2; row++)
                {
                    float rowY = row == 0 ? -0.63f : 0.63f;
                    for (int column = 0; column < perRow; column++)
                    {
                        float offset = -length * 0.5f + (column + 0.5f) * (length / perRow);
                        Vector3 position = block.Position + right * offset + up * rowY + normal * 0.14f;
                        string prefab = monitors ? MONITOR_PREFAB : ChooseTvPrefab(column + row * perRow);
                        GameObject display = PlaceMounted(prefab, monitors ? "Mounted Monitor" : "Mounted TV", position, facing);
                        if (display != null)
                        {
                            CreateStaticBox(environment, "Display Mount", display.transform.position - normal * 0.08f, new Vector3(0.12f, 0.12f, 0.12f), BopBuyMaterials.Metal);
                        }
                    }
                }
            }
        }

        private static void BuildTvMountRack(BlockRecord block, Bounds bounds, Vector3 right, float length)
        {
            int posts = Mathf.Max(2, Mathf.FloorToInt(length / 4f) + 1);
            for (int index = 0; index < posts; index++)
            {
                float offset = -length * 0.5f + index * (length / (posts - 1));
                Vector3 position = block.Position + right * offset;
                CreateStaticBox(environment, "TV Rack Post", new Vector3(position.x, block.Position.y, position.z), new Vector3(0.08f, bounds.size.y, 0.08f), BopBuyMaterials.Metal);
            }

            for (int row = 0; row < 2; row++)
            {
                float rowY = row == 0 ? -0.63f : 0.63f;
                CreateStaticBox(environment, "TV Rack Rail", block.Position + Vector3.up * rowY, new Vector3(length, 0.06f, 0.08f), BopBuyMaterials.Metal, block.Rotation);
            }
        }

        private static string ChooseTvPrefab(int index)
        {
            return (index % 4) switch
            {
                0 => TV32_PREFAB,
                1 => TV_PREFAB,
                2 => TV65_PREFAB,
                _ => TV75_PREFAB
            };
        }

        private static void BuildWarehouse()
        {
            foreach (BlockRecord warehouse in blocks.Where(block => block.LowerLabel.StartsWith("warehouse", StringComparison.Ordinal)))
            {
                Bounds bounds = GetWorldBounds(warehouse);
                float minX = Mathf.Max(bounds.min.x + 0.8f, -38.5f);
                float maxX = Mathf.Min(bounds.max.x - 0.8f, 38.5f);
                float minZ = Mathf.Max(bounds.min.z + 0.8f, -24.2f);
                float maxZ = Mathf.Min(bounds.max.z - 0.8f, 24.2f);
                if (maxX - minX < 2f || maxZ - minZ < 2f)
                {
                    continue;
                }

                int columns = Mathf.Max(1, Mathf.FloorToInt((maxX - minX) / 2.4f));
                int rows = Mathf.Max(1, Mathf.FloorToInt((maxZ - minZ) / 2.3f));
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        if (random.NextDouble() < 0.14)
                        {
                            continue;
                        }

                        float x = minX + (column + 0.5f) * ((maxX - minX) / columns);
                        float z = minZ + (row + 0.5f) * ((maxZ - minZ) / rows);
                        Quaternion rotation = Quaternion.Euler(0f, random.Next(0, 4) * 90f, 0f);
                        GameObject pallet = PlaceOnFloor(PALLET_PREFAB, "Warehouse Pallet", new Vector3(x, 0f, z), rotation);
                        if (pallet == null)
                        {
                            continue;
                        }

                        float top = GetBoundsTop(pallet);
                        GameObject largeBox = PlaceOnSurface(CARDBOARD_LARGE_PREFAB, "Warehouse Box", new Vector3(x, 0f, z), top, rotation);
                        float secondTop = GetBoundsTop(largeBox);
                        GameObject secondBox = PlaceOnSurface(
                            random.NextDouble() < 0.5 ? CARDBOARD_MEDIUM_PREFAB : TV_BOX_PREFAB,
                            "Warehouse Box",
                            new Vector3(x + 0.08f, 0f, z - 0.06f),
                            secondTop,
                            rotation);
                        if (random.NextDouble() < 0.45)
                        {
                            PlaceOnSurface(CARDBOARD_SMALL_PREFAB, "Warehouse Box", new Vector3(x - 0.06f, 0f, z + 0.08f), GetBoundsTop(secondBox), rotation);
                        }
                    }
                }
            }
        }

        private static bool IntersectsAny(List<Bounds> list, Bounds bounds)
        {
            foreach (Bounds existing in list)
            {
                if (existing.Intersects(bounds))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
