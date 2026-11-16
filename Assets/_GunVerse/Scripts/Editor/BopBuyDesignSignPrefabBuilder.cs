using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class BopBuyDesignSignPrefabBuilder
    {
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";
        private const float TRIM_THICKNESS = 0.06f;

        private sealed class SignSpec
        {
            public string Name;
            public string PrefabName;
            public string TextureName;
            public float Width;
            public float Height;
            public float Thickness;
            public float Mass;
            public bool Legs;
            public GunHoldType HoldType = GunHoldType.TwoHand;
            public float TpHoldHeight = 1.48f;
        }

        private static readonly SignSpec[] SignSpecs =
        {
            new SignSpec { Name = "Guysquad Wall Sign", PrefabName = "GuysquadWallSignGun", TextureName = "GuysquadWallSignGun.png", Width = 7.07f, Height = 2.45f, Thickness = 0.12f, Mass = 55f },
            new SignSpec { Name = "Guysquad Hanging Sign", PrefabName = "GuysquadHangingSignGun", TextureName = "GuysquadHangingSignGun.png", Width = 7.166f, Height = 2.39f, Thickness = 0.1f, Mass = 50f },
            new SignSpec { Name = "Hanging Mobile Sign", PrefabName = "HangingMobileSignGun", TextureName = "HangingMobileSignGun.png", Width = 7.166f, Height = 2.39f, Thickness = 0.1f, Mass = 50f },
            new SignSpec { Name = "Hanging Computers Sign", PrefabName = "HangingComputersSignGun", TextureName = "HangingComputersSignGun.png", Width = 7.166f, Height = 2.39f, Thickness = 0.1f, Mass = 50f },
            new SignSpec { Name = "Hanging Appliances Sign", PrefabName = "HangingAppliancesSignGun", TextureName = "HangingAppliancesSignGun.png", Width = 7.166f, Height = 2.39f, Thickness = 0.1f, Mass = 50f },
            new SignSpec { Name = "Hanging Home Theatre Sign", PrefabName = "HangingHomeTheatreSignGun", TextureName = "HangingHomeTheatreSignGun.png", Width = 7.166f, Height = 2.39f, Thickness = 0.1f, Mass = 50f },
            new SignSpec { Name = "Hanging Checkout Sign", PrefabName = "HangingCheckoutSignGun", TextureName = "HangingCheckoutSignGun.png", Width = 7.166f, Height = 2.39f, Thickness = 0.1f, Mass = 50f },
            new SignSpec { Name = "BopBuy Banner", PrefabName = "BopBuyBannerGun", TextureName = "BopBuyBannerGun.png", Width = 7.166f, Height = 2.39f, Thickness = 0.1f, Mass = 50f, TpHoldHeight = 1.35f },
            new SignSpec { Name = "Welcome Directory Sign", PrefabName = "WelcomeDirectorySignGun", TextureName = "WelcomeDirectorySignGun.png", Width = 2.4f, Height = 1.05f, Thickness = 0.16f, Mass = 28f, Legs = true, TpHoldHeight = 1.34f },
            new SignSpec { Name = "No Breaks Sign", PrefabName = "NoBreaksSignGun", TextureName = "NoBreaksSignGun.png", Width = 1.52f, Height = 2.45f, Thickness = 0.1f, Mass = 26f, TpHoldHeight = 1.34f },
            new SignSpec { Name = "Behind You Sign", PrefabName = "BehindYouSignGun", TextureName = "BehindYouSignGun.png", Width = 3.98f, Height = 1.52f, Thickness = 0.26f, Mass = 32f },
            new SignSpec { Name = "Men Sign", PrefabName = "MenSignGun", TextureName = "MenSignGun.png", Width = 0.8f, Height = 0.5f, Thickness = 0.06f, Mass = 4f, HoldType = GunHoldType.OneHand, TpHoldHeight = 1.48f },
            new SignSpec { Name = "Women Sign", PrefabName = "WomenSignGun", TextureName = "WomenSignGun.png", Width = 0.8f, Height = 0.5f, Thickness = 0.06f, Mass = 4f, HoldType = GunHoldType.OneHand, TpHoldHeight = 1.48f },
            new SignSpec { Name = "Clearance Sign", PrefabName = "ClearanceSignGun", TextureName = "ClearanceSignGun.png", Width = 1.8f, Height = 0.7f, Thickness = 0.08f, Mass = 8f },
            new SignSpec { Name = "Warehouse Sign", PrefabName = "WarehouseSignGun", TextureName = "WarehouseSignGun.png", Width = 2.6f, Height = 0.8f, Thickness = 0.08f, Mass = 12f }
        };

        [MenuItem("Tools/Gun Verse/Build BopBuy Design Signs")]
        public static void Build()
        {
            EnsureFolder(LARGE_PROPS_FOLDER);
            foreach (SignSpec spec in SignSpecs)
            {
                BuildSign(spec);
            }

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[BopBuyDesignSignPrefabBuilder] Built design signage prefabs.");
        }

        private static void BuildSign(SignSpec spec)
        {
            GameObject root = new GameObject(spec.PrefabName);
            root.transform.localScale = Vector3.one;
            root.AddComponent<NetworkObject>();
            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.SwitchTransformSpaceWhenParented = true;

            Rigidbody rigidBody = root.AddComponent<Rigidbody>();
            rigidBody.mass = spec.Mass;
            rigidBody.useGravity = true;
            rigidBody.isKinematic = true;

            float legHeight = spec.Legs ? 0.85f : 0f;
            float totalHeight = legHeight + spec.Height;
            float panelCenterY = legHeight + spec.Height * 0.5f;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(spec.Width, totalHeight, spec.Thickness);
            collider.center = new Vector3(0f, totalHeight * 0.5f, 0f);

            AddBoxPart(root.transform, "SignPanel", new Vector3(0f, panelCenterY, 0f), new Vector3(spec.Width, spec.Height, spec.Thickness), BopBuyMaterials.Blue);
            AddBoxPart(root.transform, "SignTopTrim", new Vector3(0f, panelCenterY + spec.Height * 0.5f + TRIM_THICKNESS * 0.5f, 0f), new Vector3(spec.Width + 0.04f, TRIM_THICKNESS, spec.Thickness + 0.04f), BopBuyMaterials.Yellow);
            AddBoxPart(root.transform, "SignBottomTrim", new Vector3(0f, panelCenterY - spec.Height * 0.5f - TRIM_THICKNESS * 0.5f, 0f), new Vector3(spec.Width + 0.04f, TRIM_THICKNESS, spec.Thickness + 0.04f), BopBuyMaterials.Yellow);

            if (spec.Legs)
            {
                float legCenterY = legHeight * 0.5f;
                float legOffset = spec.Width * 0.5f - 0.22f;
                AddBoxPart(root.transform, "LegLeft", new Vector3(-legOffset, legCenterY, 0f), new Vector3(0.12f, legHeight, 0.12f), BopBuyMaterials.Metal);
                AddBoxPart(root.transform, "LegRight", new Vector3(legOffset, legCenterY, 0f), new Vector3(0.12f, legHeight, 0.12f), BopBuyMaterials.Metal);
                AddBoxPart(root.transform, "LegFootLeft", new Vector3(-legOffset, 0.03f, 0f), new Vector3(0.4f, 0.06f, 0.4f), BopBuyMaterials.Metal);
                AddBoxPart(root.transform, "LegFootRight", new Vector3(legOffset, 0.03f, 0f), new Vector3(0.4f, 0.06f, 0.4f), BopBuyMaterials.Metal);
            }

            float gripY = spec.Legs
                ? legHeight + spec.Height * 0.17f
                : panelCenterY + spec.Height * 0.027f;
            float gripX = spec.Width * 0.28f;
            Transform gripPrimary = CreateGrip(root.transform, "GripPrimary", new Vector3(gripX, gripY, -spec.Thickness * 0.3f));
            Transform gripSupport = CreateGrip(root.transform, "GripSupport", new Vector3(-gripX, gripY, spec.Thickness * 0.05f));

            PropLabelBuilder.AddFrontBackLabels(
                root.transform,
                spec.PrefabName,
                spec.TextureName,
                new Vector3(0f, panelCenterY, 0f),
                spec.Width * 0.92f,
                spec.Height * 0.8f,
                spec.Thickness * 0.5f + 0.004f);

            GunItem gunItem = root.AddComponent<GunItem>();
            GunProjectileBody projectileBody = root.AddComponent<GunProjectileBody>();
            ConfigureGun(gunItem, spec, gripPrimary, gripSupport);
            ConfigureProjectile(projectileBody, spec.Mass);
            GunPropPhysicsAssets.ApplyTo(root);
            GunPerformanceSettings.ApplyToGun(root);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, true);
            PrefabUtility.SaveAsPrefabAsset(root, LARGE_PROPS_FOLDER + "/" + spec.PrefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void ConfigureGun(GunItem gunItem, SignSpec spec, Transform gripPrimary, Transform gripSupport)
        {
            float largest = Mathf.Max(spec.Width, Mathf.Max(spec.Height, spec.Thickness));
            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = spec.Name;
            serializedObject.FindProperty("pickupScale").vector3Value = Vector3.one * Mathf.Clamp(0.35f / largest, 0.08f, 0.25f);
            serializedObject.FindProperty("pickupPositionOffset").vector3Value = new Vector3(0f, -0.05f, 0.7f);
            serializedObject.FindProperty("holdType").enumValueIndex = (int)spec.HoldType;
            serializedObject.FindProperty("gripPrimary").objectReferenceValue = gripPrimary;
            serializedObject.FindProperty("gripSupport").objectReferenceValue = gripSupport;
            serializedObject.FindProperty("tpHoldHeight").floatValue = spec.TpHoldHeight;
            serializedObject.FindProperty("muzzleOffset").vector3Value = new Vector3(0f, 0f, spec.Thickness * 0.5f + 0.1f);
            serializedObject.FindProperty("roundsPerMinute").floatValue = 35f;
            serializedObject.FindProperty("magazineSize").intValue = 2;
            serializedObject.FindProperty("reloadDuration").floatValue = 3f;
            serializedObject.FindProperty("damage").floatValue = 55f;
            serializedObject.FindProperty("weightClass").enumValueIndex = (int)GunWeightClass.Heavy;
            serializedObject.FindProperty("projectileSpeed").floatValue = 18f;
            serializedObject.FindProperty("projectileLifetime").floatValue = 4f;
            serializedObject.FindProperty("releaseContentImpulse").floatValue = 0.15f;
            serializedObject.FindProperty("releasePlayerLaunchSpeed").floatValue = 3.5f;
            serializedObject.FindProperty("releasePlayerLaunchUpSpeed").floatValue = 1.6f;
            serializedObject.FindProperty("projectilesPerShot").intValue = 1;
            serializedObject.FindProperty("burstCount").intValue = 1;
            serializedObject.FindProperty("burstInterval").floatValue = 0.07f;
            serializedObject.FindProperty("spreadAngle").floatValue = 0f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureProjectile(GunProjectileBody projectileBody, float mass)
        {
            SerializedObject serializedObject = new SerializedObject(projectileBody);
            serializedObject.FindProperty("mass").floatValue = Mathf.Max(4f, mass * 0.25f);
            serializedObject.FindProperty("maxActiveProjectiles").intValue = 4;
            serializedObject.FindProperty("playerPushSpeed").floatValue = 3.5f;
            serializedObject.FindProperty("playerPushUpSpeed").floatValue = 1.8f;
            serializedObject.FindProperty("trailWidthScale").floatValue = 0.2f;
            serializedObject.FindProperty("trailLengthMultiplier").floatValue = 5f;
            serializedObject.FindProperty("gravityScale").floatValue = 1f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateGrip(Transform parent, string name, Vector3 localPosition)
        {
            GameObject grip = new GameObject(name);
            grip.transform.SetParent(parent, false);
            grip.transform.localPosition = localPosition;
            grip.transform.localRotation = Quaternion.identity;
            return grip.transform;
        }

        private static void AddBoxPart(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
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
