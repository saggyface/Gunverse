using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class GunGameWeaponTuningBuilder
    {
        private const string DISPLAY_NAME_PROPERTY = "displayName";
        private const string PICKUP_POSITION_OFFSET_PROPERTY = "pickupPositionOffset";
        private const string PICKUP_SCALE_PROPERTY = "pickupScale";
        private const string ROUNDS_PER_MINUTE_PROPERTY = "roundsPerMinute";
        private const string MAGAZINE_SIZE_PROPERTY = "magazineSize";
        private const string RELOAD_DURATION_PROPERTY = "reloadDuration";
        private const string DAMAGE_PROPERTY = "damage";
        private const string WEIGHT_CLASS_PROPERTY = "weightClass";
        private const string PROJECTILES_PER_SHOT_PROPERTY = "projectilesPerShot";
        private const string FIRE_MODE_PROPERTY = "fireMode";
        private const string BURST_COUNT_PROPERTY = "burstCount";
        private const string BURST_INTERVAL_PROPERTY = "burstInterval";
        private const string SPREAD_ANGLE_PROPERTY = "spreadAngle";
        private const string PROJECTILE_SPEED_PROPERTY = "projectileSpeed";
        private const string PROJECTILE_LIFETIME_PROPERTY = "projectileLifetime";
        private const string FIRE_AS_SELF_PROJECTILE_PROPERTY = "fireAsSelfProjectile";
        private const string GRAVITY_SCALE_PROPERTY = "gravityScale";
        private const string MAX_ACTIVE_PROJECTILES_PROPERTY = "maxActiveProjectiles";
        private const float DEFAULT_BURST_INTERVAL = 0.07f;

        private sealed class WeaponSpec
        {
            public string PrefabPath;
            public string DisplayName;
            public float RoundsPerMinute;
            public int MagazineSize;
            public float ReloadDuration;
            public float Damage;
            public GunWeightClass WeightClass;
            public GunFireMode FireMode = GunFireMode.Automatic;
            public int ProjectilesPerShot = 1;
            public int BurstCount = 1;
            public float BurstInterval = DEFAULT_BURST_INTERVAL;
            public float SpreadAngle;
            public float ProjectileSpeed;
            public float ProjectileLifetime;
            public float GravityScale = 1f;
            public int MaxActiveProjectiles = 30;
            public bool FireAsSelfProjectile;
            public Vector3? PickupScale;
            public Vector3? PickupPositionOffset;
        }

        private static readonly WeaponSpec[] Specs =
        {
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/SoupCanGun.prefab",
                DisplayName = "Soup Can",
                RoundsPerMinute = 400f,
                MagazineSize = 10,
                ReloadDuration = 1.2f,
                Damage = 15f,
                WeightClass = GunWeightClass.Light,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 38f,
                ProjectileLifetime = 3f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/TomatoCanGun.prefab",
                DisplayName = "Tomato Can",
                RoundsPerMinute = 420f,
                MagazineSize = 12,
                ReloadDuration = 1.3f,
                Damage = 12f,
                WeightClass = GunWeightClass.Light,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 40f,
                ProjectileLifetime = 3f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/DetergentBottleGun.prefab",
                DisplayName = "Detergent Bottle",
                RoundsPerMinute = 90f,
                MagazineSize = 8,
                ReloadDuration = 2.4f,
                Damage = 14f,
                WeightClass = GunWeightClass.Heavy,
                FireMode = GunFireMode.SemiAuto,
                ProjectilesPerShot = 4,
                SpreadAngle = 6f,
                ProjectileSpeed = 30f,
                ProjectileLifetime = 3f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/KeyboardGun.prefab",
                DisplayName = "Keyboard",
                RoundsPerMinute = 80f,
                MagazineSize = 12,
                ReloadDuration = 1.8f,
                Damage = 9f,
                WeightClass = GunWeightClass.Light,
                FireMode = GunFireMode.SemiAuto,
                ProjectilesPerShot = 6,
                SpreadAngle = 10f,
                ProjectileSpeed = 34f,
                ProjectileLifetime = 2.5f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/PrinterGun.prefab",
                DisplayName = "Printer",
                RoundsPerMinute = 70f,
                MagazineSize = 16,
                ReloadDuration = 2.4f,
                Damage = 8f,
                WeightClass = GunWeightClass.Medium,
                FireMode = GunFireMode.SemiAuto,
                ProjectilesPerShot = 4,
                SpreadAngle = 12f,
                ProjectileSpeed = 30f,
                ProjectileLifetime = 3f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/PhoneGun.prefab",
                DisplayName = "Phone",
                RoundsPerMinute = 650f,
                MagazineSize = 30,
                ReloadDuration = 1.6f,
                Damage = 7f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 50f,
                ProjectileLifetime = 2f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/BatteryGun.prefab",
                DisplayName = "Battery",
                RoundsPerMinute = 750f,
                MagazineSize = 24,
                ReloadDuration = 1.5f,
                Damage = 6f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 52f,
                ProjectileLifetime = 2f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/ToasterGun.prefab",
                DisplayName = "Toaster",
                RoundsPerMinute = 500f,
                MagazineSize = 24,
                ReloadDuration = 1.7f,
                Damage = 9f,
                WeightClass = GunWeightClass.Light,
                FireMode = GunFireMode.Burst,
                BurstCount = 3,
                BurstInterval = 0.06f,
                ProjectileSpeed = 46f,
                ProjectileLifetime = 2.5f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/CerealBoxGun.prefab",
                DisplayName = "Cereal Box",
                RoundsPerMinute = 600f,
                MagazineSize = 30,
                ReloadDuration = 2f,
                Damage = 10f,
                WeightClass = GunWeightClass.Light,
                ProjectileSpeed = 45f,
                ProjectileLifetime = 4f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/RadioGun.prefab",
                DisplayName = "Radio",
                RoundsPerMinute = 450f,
                MagazineSize = 27,
                ReloadDuration = 2.2f,
                Damage = 12f,
                WeightClass = GunWeightClass.Medium,
                FireMode = GunFireMode.Burst,
                BurstCount = 3,
                BurstInterval = 0.07f,
                ProjectileSpeed = 46f,
                ProjectileLifetime = 3f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/LaptopGun.prefab",
                DisplayName = "Laptop",
                RoundsPerMinute = 300f,
                MagazineSize = 12,
                ReloadDuration = 2f,
                Damage = 22f,
                WeightClass = GunWeightClass.Medium,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 42f,
                ProjectileLifetime = 3.5f,
                MaxActiveProjectiles = 20
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/BreadLoafGun.prefab",
                DisplayName = "Bread Loaf",
                RoundsPerMinute = 500f,
                MagazineSize = 30,
                ReloadDuration = 2.8f,
                Damage = 9f,
                WeightClass = GunWeightClass.Medium,
                ProjectileSpeed = 40f,
                ProjectileLifetime = 3f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/SpeakerGun.prefab",
                DisplayName = "Speaker",
                RoundsPerMinute = 450f,
                MagazineSize = 40,
                ReloadDuration = 3f,
                Damage = 11f,
                WeightClass = GunWeightClass.Heavy,
                ProjectileSpeed = 42f,
                ProjectileLifetime = 3f
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/LaneMarkerGun.prefab",
                DisplayName = "Lane Marker",
                RoundsPerMinute = 90f,
                MagazineSize = 6,
                ReloadDuration = 2.8f,
                Damage = 60f,
                WeightClass = GunWeightClass.Heavy,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 85f,
                ProjectileLifetime = 3.5f,
                GravityScale = 0.35f,
                MaxActiveProjectiles = 6,
                PickupScale = Vector3.one * 1.6f,
                PickupPositionOffset = new Vector3(0f, -0.05f, 0.55f)
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/LargeProps/ConveyorBeltGun.prefab",
                DisplayName = "Conveyor Belt",
                RoundsPerMinute = 40f,
                MagazineSize = 2,
                ReloadDuration = 3.6f,
                Damage = 100f,
                WeightClass = GunWeightClass.Heavy,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 110f,
                ProjectileLifetime = 4f,
                GravityScale = 0.15f,
                MaxActiveProjectiles = 4,
                PickupScale = Vector3.one * 0.6f,
                PickupPositionOffset = new Vector3(0f, 0.05f, 0.6f)
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/LargeProps/CheckoutCounterGun.prefab",
                DisplayName = "Checkout Counter",
                RoundsPerMinute = 45f,
                MagazineSize = 1,
                ReloadDuration = 3.2f,
                Damage = 90f,
                WeightClass = GunWeightClass.Heavy,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 100f,
                ProjectileLifetime = 4f,
                GravityScale = 0.2f,
                MaxActiveProjectiles = 2,
                PickupScale = Vector3.one * 0.28f,
                PickupPositionOffset = new Vector3(0f, -0.05f, 0.8f)
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/LargeProps/ShelfRunGun.prefab",
                DisplayName = "Shelf Run",
                RoundsPerMinute = 30f,
                MagazineSize = 1,
                ReloadDuration = 0f,
                Damage = 100f,
                WeightClass = GunWeightClass.Heavy,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 24f,
                ProjectileLifetime = 5f,
                MaxActiveProjectiles = 4,
                FireAsSelfProjectile = true
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/LargeProps/TallOpenShelfGun.prefab",
                DisplayName = "Tall Open Shelf",
                RoundsPerMinute = 30f,
                MagazineSize = 1,
                ReloadDuration = 0f,
                Damage = 110f,
                WeightClass = GunWeightClass.Heavy,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 22f,
                ProjectileLifetime = 5f,
                MaxActiveProjectiles = 4,
                FireAsSelfProjectile = true
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/StarlinkDishGun.prefab",
                DisplayName = "Starlink Dish",
                RoundsPerMinute = 60f,
                MagazineSize = 1,
                ReloadDuration = 2.8f,
                Damage = 100f,
                WeightClass = GunWeightClass.Heavy,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 95f,
                ProjectileLifetime = 3.5f,
                GravityScale = 0.4f,
                MaxActiveProjectiles = 4
            },
            new WeaponSpec
            {
                PrefabPath = "Assets/_GunVerse/Prefabs/Items/StanchionGun.prefab",
                DisplayName = "Stanchion",
                RoundsPerMinute = 30f,
                MagazineSize = 1,
                ReloadDuration = 7f,
                Damage = 100f,
                WeightClass = GunWeightClass.Heavy,
                FireMode = GunFireMode.SemiAuto,
                ProjectileSpeed = 60f,
                ProjectileLifetime = 5f,
                GravityScale = 0.35f,
                MaxActiveProjectiles = 2,
                PickupScale = Vector3.one * 0.7f,
                PickupPositionOffset = new Vector3(0f, -0.05f, 0.5f)
            }
        };

        [MenuItem("Tools/Gun Verse/Apply Gun Game Weapon Tuning")]
        public static void ApplyAll()
        {
            int appliedCount = 0;

            foreach (WeaponSpec spec in Specs)
            {
                if (Apply(spec))
                {
                    appliedCount++;
                }
            }

            GunItemPrefabRegistry.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log($"[GunGameWeaponTuningBuilder] Applied tuning to {appliedCount}/{Specs.Length} ladder weapons.");
        }

        private static bool Apply(WeaponSpec spec)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[GunGameWeaponTuningBuilder] Prefab not found: {spec.PrefabPath}");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(spec.PrefabPath);
            GunItem gunItem = root.GetComponent<GunItem>();
            GunProjectileBody projectileBody = root.GetComponent<GunProjectileBody>();
            if (gunItem == null || projectileBody == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError(
                    $"[GunGameWeaponTuningBuilder] {spec.PrefabPath} is missing GunItem or GunProjectileBody.");
                return false;
            }

            ApplyGunItem(gunItem, spec);
            ApplyProjectileBody(projectileBody, spec);
            GunPerformanceSettings.ApplyToGun(root);

            PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return true;
        }

        private static void ApplyGunItem(GunItem gunItem, WeaponSpec spec)
        {
            SerializedObject serializedObject = new SerializedObject(gunItem);
            serializedObject.FindProperty(DISPLAY_NAME_PROPERTY).stringValue = spec.DisplayName;
            serializedObject.FindProperty(ROUNDS_PER_MINUTE_PROPERTY).floatValue = spec.RoundsPerMinute;
            serializedObject.FindProperty(MAGAZINE_SIZE_PROPERTY).intValue = spec.MagazineSize;
            serializedObject.FindProperty(RELOAD_DURATION_PROPERTY).floatValue = spec.ReloadDuration;
            serializedObject.FindProperty(DAMAGE_PROPERTY).floatValue = spec.Damage;
            serializedObject.FindProperty(WEIGHT_CLASS_PROPERTY).enumValueIndex = (int)spec.WeightClass;
            serializedObject.FindProperty(FIRE_MODE_PROPERTY).enumValueIndex = (int)spec.FireMode;
            serializedObject.FindProperty(PROJECTILES_PER_SHOT_PROPERTY).intValue = Mathf.Max(1, spec.ProjectilesPerShot);
            serializedObject.FindProperty(BURST_COUNT_PROPERTY).intValue = Mathf.Max(1, spec.BurstCount);
            serializedObject.FindProperty(BURST_INTERVAL_PROPERTY).floatValue = Mathf.Max(0.01f, spec.BurstInterval);
            serializedObject.FindProperty(SPREAD_ANGLE_PROPERTY).floatValue = Mathf.Max(0f, spec.SpreadAngle);
            serializedObject.FindProperty(PROJECTILE_SPEED_PROPERTY).floatValue = spec.ProjectileSpeed;
            serializedObject.FindProperty(PROJECTILE_LIFETIME_PROPERTY).floatValue = spec.ProjectileLifetime;
            serializedObject.FindProperty(FIRE_AS_SELF_PROJECTILE_PROPERTY).boolValue = spec.FireAsSelfProjectile;

            if (spec.PickupScale.HasValue)
            {
                serializedObject.FindProperty(PICKUP_SCALE_PROPERTY).vector3Value = spec.PickupScale.Value;
            }

            if (spec.PickupPositionOffset.HasValue)
            {
                serializedObject.FindProperty(PICKUP_POSITION_OFFSET_PROPERTY).vector3Value =
                    spec.PickupPositionOffset.Value;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyProjectileBody(GunProjectileBody projectileBody, WeaponSpec spec)
        {
            SerializedObject serializedObject = new SerializedObject(projectileBody);
            serializedObject.FindProperty(GRAVITY_SCALE_PROPERTY).floatValue = Mathf.Max(0.01f, spec.GravityScale);
            serializedObject.FindProperty(MAX_ACTIVE_PROJECTILES_PROPERTY).intValue = Mathf.Max(1, spec.MaxActiveProjectiles);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
