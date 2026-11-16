using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class BopBuyExpandedGunPrefabBuilder
    {
        private const string ITEMS_FOLDER = "Assets/_GunVerse/Prefabs/Items";
        private const string LARGE_PROPS_FOLDER = "Assets/_GunVerse/Prefabs/LargeProps";
        private const string SMALL_TEMPLATE_PATH = ITEMS_FOLDER + "/PhoneGun.prefab";
        private const string MEDIUM_TEMPLATE_PATH = ITEMS_FOLDER + "/SpeakerGun.prefab";
        private const string LARGE_TEMPLATE_PATH = LARGE_PROPS_FOLDER + "/TVGun.prefab";

        private sealed class GunSpec
        {
            public string Name;
            public string TemplatePath;
            public string DestinationPath;
            public float Mass;
            public Vector3 PickupScale;
            public Vector3 PickupPosition;
            public Vector3 PickupRotation;
            public Vector3 MuzzleOffset;
            public float RoundsPerMinute;
            public int MagazineSize;
            public float ReloadDuration;
            public float Damage;
            public GunWeightClass WeightClass;
            public float ProjectileSpeed;
            public float ProjectileLifetime;
            public float ReleaseImpulse;
            public float LaunchSpeed;
            public float LaunchUpSpeed;
            public int ProjectilesPerShot;
            public int BurstCount;
            public float BurstInterval;
            public float SpreadAngle;
            public float ProjectileMass;
            public int MaxActiveProjectiles;
            public float PushSpeed;
            public float PushUpSpeed;
            public float TrailWidth;
            public float TrailLength;
            public float GravityScale;
        }

        public static void Build()
        {
            BuildGun(new GunSpec
            {
                Name = "Headphones",
                TemplatePath = SMALL_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/HeadphonesGun.prefab",
                Mass = 1.2f,
                PickupScale = Vector3.one * 0.8f,
                PickupPosition = new Vector3(0f, 0f, 0.35f),
                MuzzleOffset = new Vector3(0f, 0f, 0.08f),
                RoundsPerMinute = 360f,
                MagazineSize = 18,
                ReloadDuration = 1.3f,
                Damage = 12f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 40f,
                ProjectileLifetime = 2.5f,
                ProjectileMass = 0.5f,
                MaxActiveProjectiles = 20,
                PushSpeed = 1f,
                PushUpSpeed = 0.5f,
                TrailWidth = 0.8f,
                TrailLength = 12f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Mouse",
                TemplatePath = SMALL_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/MouseGun.prefab",
                Mass = 0.2f,
                PickupScale = Vector3.one * 1.2f,
                PickupPosition = new Vector3(0f, 0f, 0.3f),
                MuzzleOffset = new Vector3(0f, 0f, 0.06f),
                RoundsPerMinute = 600f,
                MagazineSize = 24,
                ReloadDuration = 1f,
                Damage = 8f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 55f,
                ProjectileLifetime = 2f,
                ProjectileMass = 0.2f,
                MaxActiveProjectiles = 30,
                PushSpeed = 1f,
                PushUpSpeed = 0.5f,
                TrailWidth = 0.6f,
                TrailLength = 8f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Monitor",
                TemplatePath = MEDIUM_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/MonitorGun.prefab",
                Mass = 4f,
                PickupScale = Vector3.one * 0.65f,
                PickupPosition = new Vector3(0f, -0.05f, 0.45f),
                MuzzleOffset = new Vector3(0f, 0f, 0.1f),
                RoundsPerMinute = 120f,
                MagazineSize = 8,
                ReloadDuration = 2.2f,
                Damage = 26f,
                WeightClass = GunWeightClass.Medium,
                ProjectileSpeed = 35f,
                ProjectileLifetime = 3f,
                ProjectileMass = 4f,
                MaxActiveProjectiles = 12,
                PushSpeed = 2f,
                PushUpSpeed = 1f,
                TrailWidth = 0.8f,
                TrailLength = 10f,
                GravityScale = 0.85f
            });

            BuildGun(new GunSpec
            {
                Name = "Computer",
                TemplatePath = MEDIUM_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/ComputerGun.prefab",
                Mass = 7f,
                PickupScale = Vector3.one * 0.55f,
                PickupPosition = new Vector3(0f, -0.05f, 0.5f),
                MuzzleOffset = new Vector3(0f, 0f, 0.1f),
                RoundsPerMinute = 150f,
                MagazineSize = 10,
                ReloadDuration = 2f,
                Damage = 18f,
                WeightClass = GunWeightClass.Medium,
                ProjectileSpeed = 35f,
                ProjectileLifetime = 3f,
                BurstCount = 2,
                BurstInterval = 0.1f,
                ProjectileMass = 5f,
                MaxActiveProjectiles = 12,
                PushSpeed = 3f,
                PushUpSpeed = 1.5f,
                TrailWidth = 0.8f,
                TrailLength = 10f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Radio",
                TemplatePath = MEDIUM_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/RadioGun.prefab",
                Mass = 1.8f,
                PickupScale = Vector3.one * 0.8f,
                PickupPosition = new Vector3(0f, 0f, 0.4f),
                MuzzleOffset = new Vector3(0f, 0f, 0.1f),
                RoundsPerMinute = 180f,
                MagazineSize = 12,
                ReloadDuration = 1.8f,
                Damage = 11f,
                WeightClass = GunWeightClass.Medium,
                ProjectileSpeed = 32f,
                ProjectileLifetime = 2.5f,
                ProjectilesPerShot = 3,
                SpreadAngle = 8f,
                ProjectileMass = 1.5f,
                MaxActiveProjectiles = 24,
                PushSpeed = 1.5f,
                PushUpSpeed = 0.5f,
                TrailWidth = 0.7f,
                TrailLength = 10f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Toaster",
                TemplatePath = MEDIUM_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/ToasterGun.prefab",
                Mass = 2.5f,
                PickupScale = Vector3.one * 0.8f,
                PickupPosition = new Vector3(0f, 0f, 0.4f),
                MuzzleOffset = new Vector3(0f, 0f, 0.1f),
                RoundsPerMinute = 220f,
                MagazineSize = 8,
                ReloadDuration = 1.7f,
                Damage = 16f,
                WeightClass = GunWeightClass.Medium,
                ProjectileSpeed = 30f,
                ProjectileLifetime = 2.5f,
                BurstCount = 2,
                BurstInterval = 0.1f,
                ProjectileMass = 2f,
                MaxActiveProjectiles = 16,
                PushSpeed = 2f,
                PushUpSpeed = 1f,
                TrailWidth = 0.7f,
                TrailLength = 10f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Electric Oven",
                TemplatePath = LARGE_TEMPLATE_PATH,
                DestinationPath = LARGE_PROPS_FOLDER + "/ElectricOvenGun.prefab",
                Mass = 70f,
                PickupScale = Vector3.one * 0.25f,
                PickupPosition = new Vector3(0f, -0.05f, 0.6f),
                MuzzleOffset = new Vector3(0f, 0f, 0.2f),
                RoundsPerMinute = 40f,
                MagazineSize = 3,
                ReloadDuration = 3.2f,
                Damage = 80f,
                WeightClass = GunWeightClass.Heavy,
                ProjectileSpeed = 18f,
                ProjectileLifetime = 4f,
                ProjectileMass = 25f,
                MaxActiveProjectiles = 8,
                PushSpeed = 6f,
                PushUpSpeed = 3f,
                TrailWidth = 0.2f,
                TrailLength = 5f,
                GravityScale = 1.1f
            });

            BuildGun(new GunSpec
            {
                Name = "Washer",
                TemplatePath = LARGE_TEMPLATE_PATH,
                DestinationPath = LARGE_PROPS_FOLDER + "/WasherGun.prefab",
                Mass = 80f,
                PickupScale = Vector3.one * 0.22f,
                PickupPosition = new Vector3(0f, -0.05f, 0.7f),
                MuzzleOffset = new Vector3(0f, 0f, 0.2f),
                RoundsPerMinute = 35f,
                MagazineSize = 3,
                ReloadDuration = 3.4f,
                Damage = 90f,
                WeightClass = GunWeightClass.Heavy,
                ProjectileSpeed = 16f,
                ProjectileLifetime = 4f,
                ProjectileMass = 28f,
                MaxActiveProjectiles = 8,
                PushSpeed = 7f,
                PushUpSpeed = 3f,
                TrailWidth = 0.2f,
                TrailLength = 5f,
                GravityScale = 1.1f
            });

            BuildGun(new GunSpec
            {
                Name = "Air Conditioner",
                TemplatePath = LARGE_TEMPLATE_PATH,
                DestinationPath = LARGE_PROPS_FOLDER + "/AirConditionerGun.prefab",
                Mass = 45f,
                PickupScale = Vector3.one * 0.3f,
                PickupPosition = new Vector3(0f, -0.05f, 0.6f),
                MuzzleOffset = new Vector3(0f, 0f, 0.18f),
                RoundsPerMinute = 50f,
                MagazineSize = 4,
                ReloadDuration = 2.8f,
                Damage = 60f,
                WeightClass = GunWeightClass.Heavy,
                ProjectileSpeed = 20f,
                ProjectileLifetime = 4f,
                ProjectileMass = 15f,
                MaxActiveProjectiles = 8,
                PushSpeed = 5f,
                PushUpSpeed = 2.5f,
                TrailWidth = 0.3f,
                TrailLength = 6f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Large Speaker",
                TemplatePath = MEDIUM_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/LargeSpeakerGun.prefab",
                Mass = 9f,
                PickupScale = Vector3.one * 0.65f,
                PickupPosition = new Vector3(0f, -0.05f, 0.5f),
                MuzzleOffset = new Vector3(0f, 0f, 0.15f),
                RoundsPerMinute = 90f,
                MagazineSize = 6,
                ReloadDuration = 2.4f,
                Damage = 40f,
                WeightClass = GunWeightClass.Heavy,
                ProjectileSpeed = 26f,
                ProjectileLifetime = 3f,
                ProjectileMass = 7f,
                MaxActiveProjectiles = 10,
                PushSpeed = 4f,
                PushUpSpeed = 2f,
                TrailWidth = 0.5f,
                TrailLength = 8f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Router",
                TemplatePath = SMALL_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/RouterGun.prefab",
                Mass = 1.2f,
                PickupScale = Vector3.one * 0.9f,
                PickupPosition = new Vector3(0f, 0f, 0.35f),
                MuzzleOffset = new Vector3(0f, 0f, 0.08f),
                RoundsPerMinute = 260f,
                MagazineSize = 12,
                ReloadDuration = 1.5f,
                Damage = 10f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 38f,
                ProjectileLifetime = 2.5f,
                ProjectilesPerShot = 3,
                SpreadAngle = 8f,
                ProjectileMass = 0.8f,
                MaxActiveProjectiles = 24,
                PushSpeed = 1f,
                PushUpSpeed = 0.5f,
                TrailWidth = 0.7f,
                TrailLength = 10f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Flip Phone",
                TemplatePath = SMALL_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/FlipPhoneGun.prefab",
                Mass = 0.4f,
                PickupScale = Vector3.one * 1.1f,
                PickupPosition = new Vector3(0f, 0f, 0.3f),
                MuzzleOffset = new Vector3(0f, 0f, 0.05f),
                RoundsPerMinute = 420f,
                MagazineSize = 16,
                ReloadDuration = 1.1f,
                Damage = 14f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 48f,
                ProjectileLifetime = 2.2f,
                ProjectileMass = 0.4f,
                MaxActiveProjectiles = 20,
                PushSpeed = 1f,
                PushUpSpeed = 0.5f,
                TrailWidth = 0.6f,
                TrailLength = 10f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "TV 32",
                TemplatePath = LARGE_TEMPLATE_PATH,
                DestinationPath = LARGE_PROPS_FOLDER + "/TV32Gun.prefab",
                Mass = 10f,
                PickupScale = Vector3.one * 0.35f,
                PickupPosition = new Vector3(0f, -0.05f, 0.5f),
                MuzzleOffset = new Vector3(0f, 0f, 0.1f),
                RoundsPerMinute = 60f,
                MagazineSize = 4,
                ReloadDuration = 2.5f,
                Damage = 45f,
                WeightClass = GunWeightClass.Heavy,
                ProjectileSpeed = 24f,
                ProjectileLifetime = 3.5f,
                ProjectileMass = 10f,
                MaxActiveProjectiles = 8,
                PushSpeed = 3f,
                PushUpSpeed = 1.5f,
                TrailWidth = 0.35f,
                TrailLength = 7f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "TV 65",
                TemplatePath = LARGE_TEMPLATE_PATH,
                DestinationPath = LARGE_PROPS_FOLDER + "/TV65Gun.prefab",
                Mass = 28f,
                PickupScale = Vector3.one * 0.2f,
                PickupPosition = new Vector3(0f, -0.05f, 0.6f),
                MuzzleOffset = new Vector3(0f, 0f, 0.18f),
                RoundsPerMinute = 35f,
                MagazineSize = 2,
                ReloadDuration = 3f,
                Damage = 75f,
                WeightClass = GunWeightClass.Heavy,
                ProjectileSpeed = 18f,
                ProjectileLifetime = 4f,
                ProjectileMass = 20f,
                MaxActiveProjectiles = 6,
                PushSpeed = 5f,
                PushUpSpeed = 2.5f,
                TrailWidth = 0.22f,
                TrailLength = 5f,
                GravityScale = 1.1f
            });

            BuildGun(new GunSpec
            {
                Name = "TV 75",
                TemplatePath = LARGE_TEMPLATE_PATH,
                DestinationPath = LARGE_PROPS_FOLDER + "/TV75Gun.prefab",
                Mass = 35f,
                PickupScale = Vector3.one * 0.17f,
                PickupPosition = new Vector3(0f, -0.05f, 0.65f),
                MuzzleOffset = new Vector3(0f, 0f, 0.2f),
                RoundsPerMinute = 30f,
                MagazineSize = 2,
                ReloadDuration = 3.2f,
                Damage = 90f,
                WeightClass = GunWeightClass.Heavy,
                ProjectileSpeed = 16f,
                ProjectileLifetime = 4f,
                ProjectileMass = 25f,
                MaxActiveProjectiles = 6,
                PushSpeed = 6f,
                PushUpSpeed = 3f,
                TrailWidth = 0.2f,
                TrailLength = 5f,
                GravityScale = 1.1f
            });

            BuildGun(new GunSpec
            {
                Name = "Starlink Dish",
                TemplatePath = MEDIUM_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/StarlinkDishGun.prefab",
                Mass = 6f,
                PickupScale = Vector3.one * 0.7f,
                PickupPosition = new Vector3(0f, -0.05f, 0.45f),
                MuzzleOffset = new Vector3(0f, 0f, 0.15f),
                RoundsPerMinute = 80f,
                MagazineSize = 6,
                ReloadDuration = 2.2f,
                Damage = 30f,
                WeightClass = GunWeightClass.Medium,
                ProjectileSpeed = 30f,
                ProjectileLifetime = 3f,
                ProjectileMass = 4f,
                MaxActiveProjectiles = 12,
                PushSpeed = 3f,
                PushUpSpeed = 1.5f,
                TrailWidth = 0.6f,
                TrailLength = 9f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Gift Box",
                TemplatePath = SMALL_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/GiftBoxGun.prefab",
                Mass = 0.8f,
                PickupScale = Vector3.one * 0.8f,
                PickupPosition = new Vector3(0f, 0f, 0.35f),
                MuzzleOffset = new Vector3(0f, 0f, 0.08f),
                RoundsPerMinute = 240f,
                MagazineSize = 10,
                ReloadDuration = 1.5f,
                Damage = 14f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 30f,
                ProjectileLifetime = 2.5f,
                ProjectileMass = 0.8f,
                MaxActiveProjectiles = 20,
                PushSpeed = 1f,
                PushUpSpeed = 0.5f,
                TrailWidth = 0.7f,
                TrailLength = 10f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "TV Box",
                TemplatePath = LARGE_TEMPLATE_PATH,
                DestinationPath = LARGE_PROPS_FOLDER + "/TVBoxGun.prefab",
                Mass = 4f,
                PickupScale = Vector3.one * 0.5f,
                PickupPosition = new Vector3(0f, -0.05f, 0.45f),
                MuzzleOffset = new Vector3(0f, 0f, 0.15f),
                RoundsPerMinute = 100f,
                MagazineSize = 6,
                ReloadDuration = 2f,
                Damage = 22f,
                WeightClass = GunWeightClass.Medium,
                ProjectileSpeed = 28f,
                ProjectileLifetime = 3f,
                ProjectileMass = 3f,
                MaxActiveProjectiles = 12,
                PushSpeed = 2f,
                PushUpSpeed = 1f,
                TrailWidth = 0.7f,
                TrailLength = 9f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Ink Box",
                TemplatePath = SMALL_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/InkBoxGun.prefab",
                Mass = 0.15f,
                PickupScale = Vector3.one * 1.2f,
                PickupPosition = new Vector3(0f, 0f, 0.3f),
                MuzzleOffset = new Vector3(0f, 0f, 0.06f),
                RoundsPerMinute = 300f,
                MagazineSize = 8,
                ReloadDuration = 1.2f,
                Damage = 8f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 36f,
                ProjectileLifetime = 2f,
                ProjectileMass = 0.2f,
                MaxActiveProjectiles = 24,
                PushSpeed = 1f,
                PushUpSpeed = 0.5f,
                TrailWidth = 0.6f,
                TrailLength = 8f,
                GravityScale = 1f
            });

            BuildGun(new GunSpec
            {
                Name = "Tablet",
                TemplatePath = SMALL_TEMPLATE_PATH,
                DestinationPath = ITEMS_FOLDER + "/TabletGun.prefab",
                Mass = 0.6f,
                PickupScale = Vector3.one,
                PickupPosition = new Vector3(0f, 0f, 0.35f),
                MuzzleOffset = new Vector3(0f, 0f, 0.08f),
                RoundsPerMinute = 180f,
                MagazineSize = 8,
                ReloadDuration = 1.4f,
                Damage = 12f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 34f,
                ProjectileLifetime = 2.4f,
                ProjectileMass = 0.6f,
                MaxActiveProjectiles = 18,
                PushSpeed = 1f,
                PushUpSpeed = 0.5f,
                TrailWidth = 0.6f,
                TrailLength = 9f,
                GravityScale = 1f
            });

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[BopBuyExpandedGunPrefabBuilder] Built expanded electronics, TV, and prop-model base prefabs.");
        }

        private static void BuildGun(GunSpec spec)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(spec.DestinationPath) == null &&
                !AssetDatabase.CopyAsset(spec.TemplatePath, spec.DestinationPath))
            {
                Debug.LogError("[BopBuyExpandedGunPrefabBuilder] Could not copy template to " + spec.DestinationPath);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(spec.DestinationPath);
            if (root == null)
            {
                Debug.LogError("[BopBuyExpandedGunPrefabBuilder] Could not load " + spec.DestinationPath);
                return;
            }

            root.name = spec.Name;
            GunItem gunItem = root.GetComponent<GunItem>();
            GunProjectileBody projectileBody = root.GetComponent<GunProjectileBody>();
            if (gunItem == null || projectileBody == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("[BopBuyExpandedGunPrefabBuilder] Missing GunItem or projectile body on " + spec.DestinationPath);
                return;
            }

            ConfigureGunItem(gunItem, spec);
            ConfigureProjectile(projectileBody, spec);
            GunPerformanceSettings.ApplyToGun(root);
            GunItemLabelSetupBuilder.ConfigurePrefab(root, false);
            PrefabUtility.SaveAsPrefabAsset(root, spec.DestinationPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ConfigureGunItem(GunItem gunItem, GunSpec spec)
        {
            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty("displayName").stringValue = spec.Name;
            serializedObject.FindProperty("pickupScale").vector3Value = spec.PickupScale;
            serializedObject.FindProperty("pickupPositionOffset").vector3Value = spec.PickupPosition;
            serializedObject.FindProperty("pickupRotationEuler").vector3Value = spec.PickupRotation;
            serializedObject.FindProperty("muzzleOffset").vector3Value = spec.MuzzleOffset;
            serializedObject.FindProperty("roundsPerMinute").floatValue = spec.RoundsPerMinute;
            serializedObject.FindProperty("magazineSize").intValue = spec.MagazineSize;
            serializedObject.FindProperty("reloadDuration").floatValue = spec.ReloadDuration;
            serializedObject.FindProperty("damage").floatValue = spec.Damage;
            serializedObject.FindProperty("weightClass").enumValueIndex = (int)spec.WeightClass;
            serializedObject.FindProperty("projectileSpeed").floatValue = spec.ProjectileSpeed;
            serializedObject.FindProperty("projectileLifetime").floatValue = spec.ProjectileLifetime;
            serializedObject.FindProperty("releaseContentImpulse").floatValue = spec.ReleaseImpulse;
            serializedObject.FindProperty("releasePlayerLaunchSpeed").floatValue = spec.LaunchSpeed;
            serializedObject.FindProperty("releasePlayerLaunchUpSpeed").floatValue = spec.LaunchUpSpeed;
            serializedObject.FindProperty("projectilesPerShot").intValue = Mathf.Max(1, spec.ProjectilesPerShot);
            serializedObject.FindProperty("burstCount").intValue = Mathf.Max(1, spec.BurstCount);
            serializedObject.FindProperty("burstInterval").floatValue = Mathf.Max(0.01f, spec.BurstInterval);
            serializedObject.FindProperty("spreadAngle").floatValue = Mathf.Max(0f, spec.SpreadAngle);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureProjectile(GunProjectileBody projectileBody, GunSpec spec)
        {
            SerializedObject serializedObject = new SerializedObject(projectileBody);
            serializedObject.FindProperty("mass").floatValue = spec.ProjectileMass;
            serializedObject.FindProperty("maxActiveProjectiles").intValue = Mathf.Max(1, spec.MaxActiveProjectiles);
            serializedObject.FindProperty("playerPushSpeed").floatValue = Mathf.Max(0f, spec.PushSpeed);
            serializedObject.FindProperty("playerPushUpSpeed").floatValue = Mathf.Max(0f, spec.PushUpSpeed);
            serializedObject.FindProperty("trailWidthScale").floatValue = Mathf.Max(0.01f, spec.TrailWidth);
            serializedObject.FindProperty("trailLengthMultiplier").floatValue = Mathf.Max(0.1f, spec.TrailLength);
            serializedObject.FindProperty("gravityScale").floatValue = Mathf.Max(0f, spec.GravityScale);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
