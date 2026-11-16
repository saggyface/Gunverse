using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public enum ApplianceCategory
    {
        Fridge,
        Washer,
        Dryer,
        Oven,
        Vending,
        Coffee
    }

    public sealed class ApplianceVariantSpec
    {
        public ApplianceVariantSpec(
            ApplianceCategory category,
            string variantName,
            string prefabPath,
            string targetRendererName,
            string[] targetMaterialNames,
            Color tint,
            float selectionWeight,
            bool isBase)
        {
            Category = category;
            VariantName = variantName;
            PrefabPath = prefabPath;
            TargetRendererName = targetRendererName;
            TargetMaterialNames = targetMaterialNames;
            Tint = tint;
            SelectionWeight = selectionWeight;
            IsBase = isBase;
        }

        public ApplianceCategory Category { get; }
        public string VariantName { get; }
        public string PrefabPath { get; }
        public string TargetRendererName { get; }
        public string[] TargetMaterialNames { get; }
        public Color Tint { get; }
        public float SelectionWeight { get; }
        public bool IsBase { get; }
    }

    public sealed class TopItemSpec
    {
        public TopItemSpec(string prefabPath, string displayName, float selectionWeight)
        {
            PrefabPath = prefabPath;
            DisplayName = displayName;
            SelectionWeight = selectionWeight;
        }

        public string PrefabPath { get; }
        public string DisplayName { get; }
        public float SelectionWeight { get; }
    }

    public static class BopBuyApplianceVarietyCatalog
    {
        public const string VARIANT_MATERIAL_FOLDER = "Assets/_GunVerse/Materials/ApplianceVariants";
        public const string VARIANT_PREFAB_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps/ApplianceVariants";

        public const string FRIDGE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/FridgeGun.prefab";
        public const string WASHER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/WasherGun.prefab";
        public const string DRYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/DryerGun.prefab";
        public const string OVEN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/ElectricOvenGun.prefab";
        public const string VENDING_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/VendingMachineGun.prefab";
        public const string COFFEE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/CoffeeMachineGun.prefab";

        public const string MICROWAVE_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/MicrowaveGun.prefab";
        public const string TOASTER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/ToasterGun.prefab";
        public const string AIR_CONDITIONER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/AirConditionerGun.prefab";
        public const string DESK_FAN_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/DeskFanGun.prefab";
        public const string DETERGENT_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Items/DetergentBottleGun.prefab";

        private const string METAL_LIGHT_MATERIAL = "metalLight";
        private const string OVEN_BODY_MATERIAL = "Solid.080";
        private const string VENDING_BODY_RENDERER = "Body";
        private const string COFFEE_BODY_MATERIAL = "carpetWhite";
        private const string COFFEE_FALLBACK_MATERIAL = "metal";

        private const float BASE_VARIANT_WEIGHT = 0.75f;
        private const float COLOR_VARIANT_WEIGHT = 1f;
        private const float TOP_ITEM_WEIGHT_HIGH = 3f;
        private const float TOP_ITEM_WEIGHT_MEDIUM = 2f;
        private const float TOP_ITEM_WEIGHT_LOW = 1.5f;
        private const uint FNV_OFFSET_BASIS = 2166136261u;
        private const uint FNV_PRIME = 16777619u;
        private const uint STABLE_RESOLUTION = 1000000u;
        private const uint VARIANT_SELECTION_SALT = 0x9E3779B9u;
        private const uint TOP_ITEM_SELECTION_SALT = 0x85EBCA6Bu;
        private const uint YAW_SELECTION_SALT = 0xC2B2AE35u;
        private const uint YAW_JITTER_SALT = 0xD3A2646Cu;
        private const uint LAUNDRY_CATEGORY_SALT = 0x7F4A7C15u;
        private const uint LAUNDRY_CATEGORY_COUNT = 3u;
        private const int TOP_ITEM_YAW_STEP_COUNT = 4;
        private const float TOP_ITEM_YAW_JITTER_DEGREES = 15f;
        private const float APPLIANCE_DEFAULT_YAW = 90f;
        private const float VENDING_DEFAULT_YAW = 0f;

        private static readonly Color sageTint = new Color(0.55f, 0.65f, 0.54f);
        private static readonly Color sandTint = new Color(0.76f, 0.68f, 0.54f);
        private static readonly Color skyTint = new Color(0.53f, 0.65f, 0.76f);
        private static readonly Color roseTint = new Color(0.76f, 0.58f, 0.58f);
        private static readonly Color copperTint = new Color(0.72f, 0.45f, 0.30f);
        private static readonly Color slateTint = new Color(0.38f, 0.43f, 0.48f);
        private static readonly Color creamTint = new Color(0.86f, 0.82f, 0.70f);
        private static readonly Color vendingRedTint = new Color(0.72f, 0.24f, 0.22f);
        private static readonly Color vendingGreenTint = new Color(0.22f, 0.52f, 0.34f);
        private static readonly Color vendingPurpleTint = new Color(0.42f, 0.30f, 0.58f);
        private static readonly Color vendingOrangeTint = new Color(0.82f, 0.48f, 0.18f);
        private static readonly Color terracottaTint = new Color(0.70f, 0.40f, 0.30f);

        private static readonly ApplianceVariantSpec[] fridgeVariants =
        {
            CreateBase(ApplianceCategory.Fridge, "FridgeGun"),
            CreateVariant(ApplianceCategory.Fridge, "FridgeGun", "Sage", sageTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Fridge, "FridgeGun", "Sand", sandTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Fridge, "FridgeGun", "Sky", skyTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Fridge, "FridgeGun", "Rose", roseTint, null, METAL_LIGHT_MATERIAL)
        };

        private static readonly ApplianceVariantSpec[] washerVariants =
        {
            CreateBase(ApplianceCategory.Washer, "WasherGun"),
            CreateVariant(ApplianceCategory.Washer, "WasherGun", "Sage", sageTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Washer, "WasherGun", "Sand", sandTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Washer, "WasherGun", "Sky", skyTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Washer, "WasherGun", "Rose", roseTint, null, METAL_LIGHT_MATERIAL)
        };

        private static readonly ApplianceVariantSpec[] dryerVariants =
        {
            CreateBase(ApplianceCategory.Dryer, "DryerGun"),
            CreateVariant(ApplianceCategory.Dryer, "DryerGun", "Sage", sageTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Dryer, "DryerGun", "Sand", sandTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Dryer, "DryerGun", "Sky", skyTint, null, METAL_LIGHT_MATERIAL),
            CreateVariant(ApplianceCategory.Dryer, "DryerGun", "Rose", roseTint, null, METAL_LIGHT_MATERIAL)
        };

        private static readonly ApplianceVariantSpec[] ovenVariants =
        {
            CreateBase(ApplianceCategory.Oven, "ElectricOvenGun"),
            CreateVariant(ApplianceCategory.Oven, "ElectricOvenGun", "Copper", copperTint, null, OVEN_BODY_MATERIAL),
            CreateVariant(ApplianceCategory.Oven, "ElectricOvenGun", "Slate", slateTint, null, OVEN_BODY_MATERIAL),
            CreateVariant(ApplianceCategory.Oven, "ElectricOvenGun", "Cream", creamTint, null, OVEN_BODY_MATERIAL),
            CreateVariant(ApplianceCategory.Oven, "ElectricOvenGun", "Sage", sageTint, null, OVEN_BODY_MATERIAL)
        };

        private static readonly ApplianceVariantSpec[] vendingVariants =
        {
            CreateBase(ApplianceCategory.Vending, "VendingMachineGun"),
            CreateVariant(ApplianceCategory.Vending, "VendingMachineGun", "Red", vendingRedTint, VENDING_BODY_RENDERER),
            CreateVariant(ApplianceCategory.Vending, "VendingMachineGun", "Green", vendingGreenTint, VENDING_BODY_RENDERER),
            CreateVariant(ApplianceCategory.Vending, "VendingMachineGun", "Purple", vendingPurpleTint, VENDING_BODY_RENDERER),
            CreateVariant(ApplianceCategory.Vending, "VendingMachineGun", "Orange", vendingOrangeTint, VENDING_BODY_RENDERER)
        };

        private static readonly ApplianceVariantSpec[] coffeeVariants =
        {
            CreateBase(ApplianceCategory.Coffee, "CoffeeMachineGun"),
            CreateVariant(ApplianceCategory.Coffee, "CoffeeMachineGun", "Cream", creamTint, null, COFFEE_BODY_MATERIAL, COFFEE_FALLBACK_MATERIAL),
            CreateVariant(ApplianceCategory.Coffee, "CoffeeMachineGun", "Sage", sageTint, null, COFFEE_BODY_MATERIAL, COFFEE_FALLBACK_MATERIAL),
            CreateVariant(ApplianceCategory.Coffee, "CoffeeMachineGun", "Terracotta", terracottaTint, null, COFFEE_BODY_MATERIAL, COFFEE_FALLBACK_MATERIAL)
        };

        private static readonly ApplianceVariantSpec[] allVariants = BuildAllVariants();
        private static readonly ApplianceCategory[] applianceCategories =
        {
            ApplianceCategory.Fridge,
            ApplianceCategory.Washer,
            ApplianceCategory.Dryer,
            ApplianceCategory.Oven,
            ApplianceCategory.Vending,
            ApplianceCategory.Coffee
        };

        private static readonly TopItemSpec[] topItems =
        {
            new TopItemSpec(MICROWAVE_PREFAB_PATH, "Microwave", TOP_ITEM_WEIGHT_HIGH),
            new TopItemSpec(TOASTER_PREFAB_PATH, "Toaster", TOP_ITEM_WEIGHT_MEDIUM),
            new TopItemSpec(AIR_CONDITIONER_PREFAB_PATH, "Air Conditioner", TOP_ITEM_WEIGHT_LOW),
            new TopItemSpec(DESK_FAN_PREFAB_PATH, "Desk Fan", TOP_ITEM_WEIGHT_LOW),
            new TopItemSpec(DETERGENT_PREFAB_PATH, "Detergent Bottle", TOP_ITEM_WEIGHT_MEDIUM)
        };

        public static IReadOnlyList<ApplianceCategory> GetCategories()
        {
            return applianceCategories;
        }

        public static IReadOnlyList<ApplianceVariantSpec> GetVariants(ApplianceCategory category)
        {
            return GetVariantArray(category);
        }

        public static string GetBasePrefabPath(ApplianceCategory category)
        {
            return category switch
            {
                ApplianceCategory.Fridge => FRIDGE_PREFAB_PATH,
                ApplianceCategory.Washer => WASHER_PREFAB_PATH,
                ApplianceCategory.Dryer => DRYER_PREFAB_PATH,
                ApplianceCategory.Oven => OVEN_PREFAB_PATH,
                ApplianceCategory.Vending => VENDING_PREFAB_PATH,
                ApplianceCategory.Coffee => COFFEE_PREFAB_PATH,
                _ => FRIDGE_PREFAB_PATH
            };
        }

        public static bool IsBaseAppliancePath(string prefabPath)
        {
            return string.Equals(prefabPath, FRIDGE_PREFAB_PATH, StringComparison.Ordinal) ||
                   string.Equals(prefabPath, WASHER_PREFAB_PATH, StringComparison.Ordinal) ||
                   string.Equals(prefabPath, DRYER_PREFAB_PATH, StringComparison.Ordinal) ||
                   string.Equals(prefabPath, OVEN_PREFAB_PATH, StringComparison.Ordinal) ||
                   string.Equals(prefabPath, VENDING_PREFAB_PATH, StringComparison.Ordinal) ||
                   string.Equals(prefabPath, COFFEE_PREFAB_PATH, StringComparison.Ordinal);
        }

        public static bool IsTopItemPath(string prefabPath)
        {
            foreach (TopItemSpec topItem in topItems)
            {
                if (string.Equals(topItem.PrefabPath, prefabPath, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetCategory(string prefabPath, out ApplianceCategory category)
        {
            foreach (ApplianceVariantSpec variant in allVariants)
            {
                if (string.Equals(variant.PrefabPath, prefabPath, StringComparison.Ordinal))
                {
                    category = variant.Category;
                    return true;
                }
            }

            category = default;
            return false;
        }

        public static ApplianceCategory ChooseLaundryCategory(uint stableHash)
        {
            uint index = (stableHash ^ LAUNDRY_CATEGORY_SALT) % LAUNDRY_CATEGORY_COUNT;
            return index switch
            {
                0u => ApplianceCategory.Washer,
                1u => ApplianceCategory.Dryer,
                _ => ApplianceCategory.Oven
            };
        }

        public static ApplianceVariantSpec ChooseVariantSpec(ApplianceCategory category, uint stableHash)
        {
            return ChooseWeightedVariant(GetVariantArray(category), stableHash ^ VARIANT_SELECTION_SALT);
        }

        public static string ChooseVariantPrefab(ApplianceCategory category, uint stableHash)
        {
            ApplianceVariantSpec variant = ChooseVariantSpec(category, stableHash);
            if (variant == null || variant.IsBase)
            {
                return GetBasePrefabPath(category);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(variant.PrefabPath) == null)
            {
                return GetBasePrefabPath(category);
            }

            return variant.PrefabPath;
        }

        public static string ChooseTopItemPrefab(uint stableHash)
        {
            TopItemSpec topItem = ChooseTopItemSpec(stableHash);
            return topItem != null ? topItem.PrefabPath : string.Empty;
        }

        public static string GetTopItemDisplayName(string prefabPath)
        {
            foreach (TopItemSpec topItem in topItems)
            {
                if (string.Equals(topItem.PrefabPath, prefabPath, StringComparison.Ordinal))
                {
                    return topItem.DisplayName;
                }
            }

            return "Appliance";
        }

        public static bool IsTopItemHostCategory(ApplianceCategory category)
        {
            return category == ApplianceCategory.Fridge ||
                   category == ApplianceCategory.Washer ||
                   category == ApplianceCategory.Dryer;
        }

        public static bool SupportsYawJitter(ApplianceCategory category)
        {
            return category != ApplianceCategory.Coffee;
        }

        public static float GetBuilderDefaultYaw(ApplianceCategory category)
        {
            return category == ApplianceCategory.Vending ? VENDING_DEFAULT_YAW : APPLIANCE_DEFAULT_YAW;
        }

        public static float ChooseTopItemYaw(uint stableHash)
        {
            uint yawIndex = (stableHash ^ YAW_SELECTION_SALT) % (uint)TOP_ITEM_YAW_STEP_COUNT;
            float baseYaw = yawIndex * (360f / TOP_ITEM_YAW_STEP_COUNT);
            float jitter = StableRange(stableHash ^ YAW_JITTER_SALT, -TOP_ITEM_YAW_JITTER_DEGREES, TOP_ITEM_YAW_JITTER_DEGREES);
            return baseYaw + jitter;
        }

        public static bool StableChance(uint stableHash, float chance)
        {
            return StableRange(stableHash, 0f, 1f) < chance;
        }

        public static float StableRange(uint stableHash, float minimum, float maximum)
        {
            uint value = stableHash % STABLE_RESOLUTION;
            float normalized = value / (float)STABLE_RESOLUTION;
            return Mathf.Lerp(minimum, maximum, normalized);
        }

        public static uint ComputeStableHash(string identity, Vector3 position)
        {
            string key = string.Concat(
                identity,
                "|",
                position.x.ToString("F3", CultureInfo.InvariantCulture),
                "|",
                position.y.ToString("F3", CultureInfo.InvariantCulture),
                "|",
                position.z.ToString("F3", CultureInfo.InvariantCulture));
            return ComputeStableHash(key);
        }

        public static uint ComputeStableHash(string value)
        {
            uint hash = FNV_OFFSET_BASIS;
            foreach (char character in value)
            {
                hash ^= (uint)character;
                hash *= FNV_PRIME;
            }

            return hash;
        }

        public static bool HasAllVariantPrefabs()
        {
            foreach (ApplianceVariantSpec variant in allVariants)
            {
                if (variant.IsBase || variant.Category == ApplianceCategory.Coffee)
                {
                    continue;
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>(variant.PrefabPath) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static ApplianceVariantSpec[] BuildAllVariants()
        {
            List<ApplianceVariantSpec> variants = new List<ApplianceVariantSpec>();
            variants.AddRange(fridgeVariants);
            variants.AddRange(washerVariants);
            variants.AddRange(dryerVariants);
            variants.AddRange(ovenVariants);
            variants.AddRange(vendingVariants);
            variants.AddRange(coffeeVariants);
            return variants.ToArray();
        }

        private static ApplianceVariantSpec[] GetVariantArray(ApplianceCategory category)
        {
            return category switch
            {
                ApplianceCategory.Fridge => fridgeVariants,
                ApplianceCategory.Washer => washerVariants,
                ApplianceCategory.Dryer => dryerVariants,
                ApplianceCategory.Oven => ovenVariants,
                ApplianceCategory.Vending => vendingVariants,
                ApplianceCategory.Coffee => coffeeVariants,
                _ => fridgeVariants
            };
        }

        private static ApplianceVariantSpec CreateBase(ApplianceCategory category, string baseName)
        {
            return new ApplianceVariantSpec(
                category,
                baseName,
                GetBasePrefabPath(category),
                null,
                Array.Empty<string>(),
                Color.white,
                BASE_VARIANT_WEIGHT,
                true);
        }

        private static ApplianceVariantSpec CreateVariant(
            ApplianceCategory category,
            string baseName,
            string suffix,
            Color tint,
            string targetRendererName,
            params string[] targetMaterialNames)
        {
            string variantName = baseName + "_" + suffix;
            return new ApplianceVariantSpec(
                category,
                variantName,
                VARIANT_PREFAB_FOLDER + "/" + variantName + ".prefab",
                targetRendererName,
                targetMaterialNames,
                tint,
                COLOR_VARIANT_WEIGHT,
                false);
        }

        private static ApplianceVariantSpec ChooseWeightedVariant(ApplianceVariantSpec[] variants, uint stableHash)
        {
            float totalWeight = 0f;
            foreach (ApplianceVariantSpec variant in variants)
            {
                totalWeight += variant.SelectionWeight;
            }

            if (totalWeight <= 0f)
            {
                return variants.Length > 0 ? variants[0] : null;
            }

            float targetWeight = StableRange(stableHash, 0f, totalWeight);
            float cumulativeWeight = 0f;
            foreach (ApplianceVariantSpec variant in variants)
            {
                cumulativeWeight += variant.SelectionWeight;
                if (targetWeight <= cumulativeWeight)
                {
                    return variant;
                }
            }

            return variants[variants.Length - 1];
        }

        private static TopItemSpec ChooseTopItemSpec(uint stableHash)
        {
            float totalWeight = 0f;
            foreach (TopItemSpec topItem in topItems)
            {
                totalWeight += topItem.SelectionWeight;
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float targetWeight = StableRange(stableHash ^ TOP_ITEM_SELECTION_SALT, 0f, totalWeight);
            float cumulativeWeight = 0f;
            foreach (TopItemSpec topItem in topItems)
            {
                cumulativeWeight += topItem.SelectionWeight;
                if (targetWeight <= cumulativeWeight)
                {
                    return topItem;
                }
            }

            return topItems[topItems.Length - 1];
        }
    }
}
