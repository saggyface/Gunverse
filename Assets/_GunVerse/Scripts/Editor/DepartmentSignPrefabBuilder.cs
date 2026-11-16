using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class DepartmentSignPrefabBuilder
    {
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";
        private const float SIGN_WIDTH = 2.8f;
        private const float SIGN_HEIGHT = 0.9f;
        private const float SIGN_THICKNESS = 0.12f;
        private const float TRIM_THICKNESS = 0.05f;
        private const float MASS = 30f;

        private sealed class SignSpec
        {
            public string Name;
            public string PrefabName;
            public string TextureName;
        }

        private static readonly SignSpec[] SignSpecs =
        {
            new SignSpec { Name = "Guysquad Sign", PrefabName = "GuysquadSignGun", TextureName = "GuysquadCounterGun.png" },
            new SignSpec { Name = "Mobile Sign", PrefabName = "MobileSignGun", TextureName = "MobileSignGun.png" },
            new SignSpec { Name = "Computers Sign", PrefabName = "ComputersSignGun", TextureName = "ComputersSignGun.png" },
            new SignSpec { Name = "Home Theatre Sign", PrefabName = "HomeTheatreSignGun", TextureName = "HomeTheatreSignGun.png" },
            new SignSpec { Name = "Appliances Sign", PrefabName = "AppliancesSignGun", TextureName = "AppliancesSignGun.png" },
            new SignSpec { Name = "Checkout Sign", PrefabName = "CheckoutSignGun", TextureName = "CheckoutSignGun.png" },
            new SignSpec { Name = "TV Wall Sign", PrefabName = "TvWallSignGun", TextureName = "TvWallSignGun.png" },
            new SignSpec { Name = "Smart Home Sign", PrefabName = "SmartHomeSignGun", TextureName = "SmartHomeSignGun.png" },
            new SignSpec { Name = "Cell Phones Sign", PrefabName = "CellPhonesSignGun", TextureName = "CellPhonesSignGun.png" },
            new SignSpec { Name = "Mobile Accessories Sign", PrefabName = "MobileAccessoriesSignGun", TextureName = "MobileAccessoriesSignGun.png" },
            new SignSpec { Name = "Printers Ink Sign", PrefabName = "PrintersInkSignGun", TextureName = "PrintersInkSignGun.png" },
            new SignSpec { Name = "Tablets Sign", PrefabName = "TabletsSignGun", TextureName = "TabletsSignGun.png" },
            new SignSpec { Name = "Laptops Sign", PrefabName = "LaptopsSignGun", TextureName = "LaptopsSignGun.png" },
            new SignSpec { Name = "Computing Sign", PrefabName = "ComputingSignGun", TextureName = "ComputingSignGun.png" },
            new SignSpec { Name = "Order Pickup Sign", PrefabName = "OrderPickupSignGun", TextureName = "OrderPickupSignGun.png" },
            new SignSpec { Name = "Customer Service Sign", PrefabName = "CustomerServiceSignGun", TextureName = "CustomerServiceSignGun.png" },
            new SignSpec { Name = "Holiday Sign", PrefabName = "HolidaySignGun", TextureName = "HolidaySignGun.png" },
            new SignSpec { Name = "Gifts Sign", PrefabName = "GiftsSignGun", TextureName = "GiftsSignGun.png" },
            new SignSpec { Name = "Smart Home Banner", PrefabName = "SmartHomeBannerGun", TextureName = "SmartHomeSignGun.png" },
            new SignSpec { Name = "Cell Phones Banner", PrefabName = "CellPhonesBannerGun", TextureName = "CellPhonesSignGun.png" },
            new SignSpec { Name = "Tablets Banner", PrefabName = "TabletsBannerGun", TextureName = "TabletsSignGun.png" },
            new SignSpec { Name = "Computing Banner", PrefabName = "ComputingBannerGun", TextureName = "ComputingSignGun.png" },
            new SignSpec { Name = "Holiday Banner", PrefabName = "HolidayBannerGun", TextureName = "HolidaySignGun.png" },
            new SignSpec { Name = "Gifts Banner", PrefabName = "GiftsBannerGun", TextureName = "GiftsSignGun.png" }
        };

        [MenuItem("Tools/Gun Verse/Build Department Signs")]
        public static void Build()
        {
            EnsureFolder(LARGE_PROPS_FOLDER);
            foreach (SignSpec spec in SignSpecs)
            {
                BuildSign(spec);
            }

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[DepartmentSignPrefabBuilder] Built pickable department signs.");
        }

        private static void BuildSign(SignSpec spec)
        {
            GameObject root = new GameObject(spec.Name);
            root.transform.localScale = Vector3.one;
            root.AddComponent<NetworkObject>();
            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.SwitchTransformSpaceWhenParented = true;

            Rigidbody rigidBody = root.AddComponent<Rigidbody>();
            rigidBody.mass = MASS;
            rigidBody.useGravity = true;
            rigidBody.isKinematic = true;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(SIGN_WIDTH, SIGN_HEIGHT, SIGN_THICKNESS);

            AddBoxPart(root.transform, "SignPanel", Vector3.zero, new Vector3(SIGN_WIDTH, SIGN_HEIGHT, SIGN_THICKNESS), BopBuyMaterials.Blue);
            AddBoxPart(root.transform, "SignTrimTop", new Vector3(0f, SIGN_HEIGHT * 0.5f + TRIM_THICKNESS * 0.5f, 0f), new Vector3(SIGN_WIDTH + 0.04f, TRIM_THICKNESS, SIGN_THICKNESS + 0.04f), BopBuyMaterials.Yellow);
            AddBoxPart(root.transform, "SignTrimBottom", new Vector3(0f, -SIGN_HEIGHT * 0.5f - TRIM_THICKNESS * 0.5f, 0f), new Vector3(SIGN_WIDTH + 0.04f, TRIM_THICKNESS, SIGN_THICKNESS + 0.04f), BopBuyMaterials.Yellow);

            PropLabelBuilder.AddFrontBackLabels(
                root.transform,
                spec.PrefabName,
                spec.TextureName,
                Vector3.zero,
                SIGN_WIDTH * 0.88f,
                SIGN_HEIGHT * 0.78f,
                SIGN_THICKNESS * 0.5f + 0.004f);

            GunItem gunItem = root.AddComponent<GunItem>();
            GunProjectileBody projectileBody = root.AddComponent<GunProjectileBody>();
            ConfigureGun(gunItem, spec.Name);
            ConfigureProjectile(projectileBody);
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, true);
            PrefabUtility.SaveAsPrefabAsset(root, LARGE_PROPS_FOLDER + "/" + spec.PrefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void ConfigureGun(GunItem gunItem, string displayName)
        {
            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("pickupScale").vector3Value = Vector3.one * 0.18f;
            serializedObject.FindProperty("pickupPositionOffset").vector3Value = new Vector3(0f, -0.05f, 0.6f);
            serializedObject.FindProperty("muzzleOffset").vector3Value = new Vector3(0f, 0f, 0.1f);
            serializedObject.FindProperty("roundsPerMinute").floatValue = 40f;
            serializedObject.FindProperty("magazineSize").intValue = 2;
            serializedObject.FindProperty("reloadDuration").floatValue = 3f;
            serializedObject.FindProperty("damage").floatValue = 55f;
            serializedObject.FindProperty("weightClass").enumValueIndex = (int)GunWeightClass.Heavy;
            serializedObject.FindProperty("projectileSpeed").floatValue = 20f;
            serializedObject.FindProperty("projectileLifetime").floatValue = 4f;
            serializedObject.FindProperty("releaseContentImpulse").floatValue = 0.2f;
            serializedObject.FindProperty("releasePlayerLaunchSpeed").floatValue = 4f;
            serializedObject.FindProperty("releasePlayerLaunchUpSpeed").floatValue = 2f;
            serializedObject.FindProperty("projectilesPerShot").intValue = 1;
            serializedObject.FindProperty("burstCount").intValue = 1;
            serializedObject.FindProperty("burstInterval").floatValue = 0.07f;
            serializedObject.FindProperty("spreadAngle").floatValue = 0f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureProjectile(GunProjectileBody projectileBody)
        {
            SerializedObject serializedObject = new SerializedObject(projectileBody);
            serializedObject.FindProperty("mass").floatValue = 12f;
            serializedObject.FindProperty("maxActiveProjectiles").intValue = 4;
            serializedObject.FindProperty("playerPushSpeed").floatValue = 4f;
            serializedObject.FindProperty("playerPushUpSpeed").floatValue = 2f;
            serializedObject.FindProperty("trailWidthScale").floatValue = 0.2f;
            serializedObject.FindProperty("trailLengthMultiplier").floatValue = 5f;
            serializedObject.FindProperty("gravityScale").floatValue = 1f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddBoxPart(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            Object.DestroyImmediate(box.GetComponent<Collider>());

            MeshRenderer renderer = box.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
