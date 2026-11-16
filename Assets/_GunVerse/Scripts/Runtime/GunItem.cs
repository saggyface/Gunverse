using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GunVerse
{
    public enum GunItemState
    {
        World,
        Held,
        Projectile
    }

    public enum GunWeightClass
    {
        Light,
        Medium,
        Heavy
    }

    public enum GunFireMode
    {
        Automatic,
        SemiAuto,
        Burst
    }

    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    public sealed class GunItem : NetworkBehaviour
    {
        public const float PICKUP_RANGE = 3f;
        private const float EYE_HEIGHT = 1.65f;
        private const float RAYCAST_SKIN = 0.5f;
        private const float PICKUP_POINT_TOLERANCE = 0.5f;
        private const float MIN_FIRE_DIRECTION_SQR = 0.0001f;
        private const float BASE_RECOIL_DISTANCE = 0.06f;
        private const float BASE_RECOIL_PITCH = 2.5f;
        private const float BASE_CAMERA_SHAKE = 0.2f;
        private const float BASE_FIELD_OF_VIEW_PUNCH = 0.6f;
        private const float BASE_MUZZLE_FLASH_SCALE = 0.18f;
        private const float RECOIL_RECOVERY_SPEED = 2.5f;
        private const float RECOIL_PITCH_RECOVERY_SPEED = 20f;

        private const float LIGHT_WEIGHT_SCALE = 0.6f;
        private const float HEAVY_WEIGHT_SCALE = 1.8f;
        private const float DEFAULT_FIRE_PITCH = 1f;
        private const float LIGHT_FIRE_PITCH = 1.15f;
        private const float HEAVY_FIRE_PITCH = 0.85f;
        private const float LIGHT_FIRE_VOLUME = 0.45f;
        private const float MEDIUM_FIRE_VOLUME = 0.55f;
        private const float HEAVY_FIRE_VOLUME = 0.7f;
        private const float RELOAD_DIP_DISTANCE = 0.1f;
        private const float RELOAD_SIDE_DISTANCE = 0.02f;
        private const float RELOAD_DIP_ANGLE = 32f;
        private const float RELOAD_SIDE_ANGLE = 8f;
        private const float RELOAD_ROLL_ANGLE = 10f;
        private const float MIN_RELEASE_BOUNDS_EXTENT = 0.01f;
        private const int BOUNDS_CORNER_COUNT = 8;
        private const float RELEASE_UPWARD_IMPULSE_SCALE = 0.5f;
        private const float WAKE_BOUNDS_MARGIN = 0.2f;
        private const float DROP_POSE_MAX_DISTANCE = 5f;
        private const float LARGE_DROP_MIN_LENGTH = 1.5f;
        private const float DROP_FLOOR_MARGIN = 0.05f;
        private const float DROP_FLOOR_CLEARANCE = 0.1f;
        private const float DROP_REAR_CLEARANCE = 0.75f;
        private const float DROP_MAX_TOP = 3.3f;
        private const float WORLD_COLLIDER_ENABLE_DELAY = 0.2f;
        private const float WORLD_SLEEP_THRESHOLD = 0.01f;
        private const float MAX_ANGULAR_VELOCITY = 30f;
        private const float THROWABLE_RECLAIM_TIMEOUT_SECONDS = 30f;
        private const float DRAW_LOWER_DISTANCE = 0.35f;
        private const float DRAW_PITCH_ANGLE = 30f;
        private const float DRAW_ROLL_ANGLE = 8f;
        private const float TOSS_FORWARD_SPEED = 3f;
        private const float TOSS_UP_SPEED = 2f;
        private const float TOSS_SPIN_SPEED = 3f;
        private const int MAX_CLIENT_SHOTS_PER_VOLLEY = 16;
        private const int SELF_PROJECTILE_PICKUP_AMMO = 1;
        private const int SAFE_POSE_OVERLAP_BUFFER_SIZE = 32;
        private const int MAX_SAFE_POSE_ITERATIONS = 4;
        private const float CLIENT_SHOT_GROUP_TOLERANCE = 0.02f;
        private const int GUN_PROP_LAYER_INDEX = 9;
        public const int WorldHullLayerIndex = 10;
        private const float LARGE_WORLD_HULL_EXTENT = 1.5f;
        private const float MAX_SAFE_POSE_STEP = 1.5f;
        private const float MAX_SAFE_POSE_TOTAL_DISTANCE = 4f;
        private const float MIN_SAFE_POSE_SIZE = 0.0001f;
        private const float MIN_MUZZLE_PROBE_RADIUS = 0.05f;
        private const float MAX_MUZZLE_PROBE_RADIUS = 0.25f;

        private const ulong NO_HOLDER = ulong.MaxValue;

        [Header("Identity")]
        [Tooltip("Name shown in the HUD and interaction prompts.")]
        [SerializeField] private string displayName = string.Empty;

        [Header("Held Pose")]
        [Tooltip("Offset from the player's hand anchor when this item is held.")]
        [SerializeField] private Vector3 pickupPositionOffset = Vector3.zero;

        [Tooltip("Scale applied to this item while it is held.")]
        [SerializeField] private Vector3 pickupScale = Vector3.one;

        [Tooltip("Rotation (Euler angles) applied to this item while it is held.")]
        [SerializeField] private Vector3 pickupRotationEuler = Vector3.zero;

        [Header("Grip")]
        [SerializeField] private GunHoldType holdType = GunHoldType.OneHand;
        [SerializeField] private Transform gripPrimary;
        [SerializeField] private Transform gripSupport;
        [SerializeField] private float tpHoldDistance = 0.38f;
        [SerializeField] private float tpHoldHeight = 1.48f;

        [Tooltip("Local position where the projectile leaves this item.")]
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0f, 0f, 0.15f);

        [Header("Gun Behavior")]
        [SerializeField] private GunFireMode fireMode = GunFireMode.Automatic;
        [SerializeField] private float roundsPerMinute = 300f;
        [SerializeField] private int magazineSize = 12;
        [SerializeField] private float reloadDuration = 1.5f;
        [SerializeField] private float damage = 20f;

        [Header("Burst and Spread")]
        [Tooltip("Projectiles fired by each trigger pull or burst shot.")]
        [SerializeField, Min(1)] private int projectilesPerShot = 1;
        [Tooltip("Number of shots in a burst. Use 1 for a normal single shot.")]
        [SerializeField, Min(1)] private int burstCount = 1;
        [Tooltip("Delay between burst shots.")]
        [SerializeField, Min(0f)] private float burstInterval = 0.07f;
        [Tooltip("Maximum random spread in degrees when firing multiple projectiles.")]
        [SerializeField, Min(0f)] private float spreadAngle = 0f;

        [Header("Combat Feel")]
        [Tooltip("Controls recoil, camera shake, muzzle flash size, and fire sound.")]
        [SerializeField] private GunWeightClass weightClass = GunWeightClass.Medium;
        [Tooltip("Hit confirmation sound family. All current cues fall back to the generic hit or kill clip.")]
        [SerializeField] private HitCue hitCue = HitCue.Generic;

        [Header("Projectile")]
        [SerializeField] private float projectileSpeed = 30f;
        [SerializeField] private float projectileLifetime = 3f;
        [SerializeField, Min(1)] private int projectileMaxBounces = 8;
        [SerializeField, Range(0f, 1f)] private float projectileBounceDamping = 0.65f;
        [SerializeField, Range(0f, 1f)] private float projectileAngularDamping = 0.7f;
        [SerializeField, Min(0f)] private float projectileGravityScale = 1f;
        [Tooltip("Launches this held item itself as the projectile instead of spawning a projectile clone.")]
        [SerializeField] private bool fireAsSelfProjectile;

        [Header("Labels")]
        [Tooltip("Keeps the label visible even when the item is not hovered, held, or travelling as a projectile.")]
        [SerializeField] private bool alwaysShowLabel;

        [Header("World Placement")]
        [Tooltip("While in world state, keeps this item fixed in place until it is picked up for the first time.")]
        [SerializeField] private bool fixedInWorld;

        [HideInInspector]
        [SerializeField] private uint projectilePrefabHash;

        private bool hasBeenPickedUp;

        [Header("Release Physics")]
        [Tooltip("Impulse applied to objects trapped inside this item when it is released.")]
        [SerializeField] private float releaseContentImpulse = 0f;

        [Tooltip("Horizontal launch speed applied to players caught under this item when it is released.")]
        [SerializeField] private float releasePlayerLaunchSpeed = 0f;

        [Tooltip("Upward launch speed applied to players caught under this item when it is released.")]
        [SerializeField] private float releasePlayerLaunchUpSpeed = 0f;

        private readonly NetworkVariable<int> state = new NetworkVariable<int>(
            (int)GunItemState.World,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<ulong> holderId = new NetworkVariable<ulong>(
            NO_HOLDER,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> currentAmmo = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> isReloading = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> isProjectileHidden = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private static readonly Dictionary<ulong, GunItem> heldItems = new Dictionary<ulong, GunItem>();
        private static readonly List<GunItem> allItems = new List<GunItem>();
        private static readonly Dictionary<ulong, OutstandingThrow> outstandingThrows =
            new Dictionary<ulong, OutstandingThrow>();
        private static readonly Collider[] safePoseOverlapBuffer = new Collider[SAFE_POSE_OVERLAP_BUFFER_SIZE];
        private static bool projectileLayerCollisionsConfigured;

        private sealed class OutstandingThrow
        {
            public GunItem Item;
            public float RegisteredAt;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAllItems()
        {
            allItems.Clear();
        }

        public static GunItem FindHeld(ulong clientId)
        {
            for (int i = 0; i < allItems.Count; i++)
            {
                GunItem item = allItems[i];
                if (item != null && item.IsSpawned && item.State == GunItemState.Held && item.HolderId == clientId)
                {
                    return item;
                }
            }

            return null;
        }

        private Collider[] colliders;
        private Collider projectileHullCollider;
        private Rigidbody rigidBody;
        private GunProjectileBody projectileBody;
        private GunItemLabelVisual[] labelVisuals;
        private Vector3 originalWorldScale;
        private Bounds localBounds;
        private Color mainColor = ArtPalette.Ui.Cream;
        private bool hasBoundHeldVisual;
        private bool isPreparedProjectile;

        private float nextFireTime;
        private int nextShotGroupId;
        private readonly Dictionary<ulong, uint> clientShotGroups = new Dictionary<ulong, uint>();
        private Coroutine reloadCoroutine;
        private Coroutine worldColliderEnableCoroutine;
        private Coroutine burstCoroutine;
        private GameObject cachedProjectilePrefab;
        private uint cachedProjectilePrefabHash;
        private readonly List<ulong> fireFeedbackTargets = new List<ulong>();
        private ulong[] fireFeedbackTargetArray;

        private float recoilAmount;
        private float recoilPitchAmount;
        private float reloadMotionTime;
        private float drawStartTime;
        private bool isDrawing;

        public GunItemState State => (GunItemState)state.Value;
        public ulong HolderId => holderId.Value;
        public int CurrentAmmo => currentAmmo.Value;
        public bool IsReloading => isReloading.Value;
        public bool IsProjectileHidden => isProjectileHidden.Value;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? gameObject.name.Replace("(Clone)", string.Empty).Trim()
            : displayName;

        public bool AlwaysShowLabel => alwaysShowLabel;
        public GunWeightClass WeightClass => weightClass;
        public bool FireAsSelfProjectile => fireAsSelfProjectile;
        public bool CanReload => !fireAsSelfProjectile;
        public GunFireMode FireMode => fireMode;
        public bool IsDrawing => isDrawing;
        public float SwapTossDuration => weightClass switch
        {
            GunWeightClass.Light => 0.25f,
            GunWeightClass.Medium => 0.45f,
            _ => 0.8f
        };
        public float SwapDrawDuration => weightClass switch
        {
            GunWeightClass.Light => 0.35f,
            GunWeightClass.Medium => 0.65f,
            _ => 1.35f
        };
        public bool IsFixedInWorld => fixedInWorld;
        public float Damage => damage;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public int ProjectileMaxBounces => Mathf.Max(1, projectileMaxBounces);
        public float ProjectileBounceDamping => Mathf.Clamp01(projectileBounceDamping);
        public float ProjectileAngularDamping => Mathf.Clamp01(projectileAngularDamping);
        public float ProjectileGravityScale => Mathf.Max(0f, projectileGravityScale);
        public float ProjectileRadius => Mathf.Max(
            0.05f,
            Mathf.Max(localBounds.extents.x, Mathf.Max(localBounds.extents.y, localBounds.extents.z)));
        public float MuzzleProbeRadius => Mathf.Clamp(
            ProjectileRadius * Mathf.Max(pickupScale.x, Mathf.Max(pickupScale.y, pickupScale.z)),
            MIN_MUZZLE_PROBE_RADIUS,
            MAX_MUZZLE_PROBE_RADIUS);
        public float ProjectileMass => rigidBody != null ? Mathf.Max(0.01f, rigidBody.mass) : 1f;
        public uint ProjectilePrefabHash => projectilePrefabHash;
        public uint ProjectileVisualHash
        {
            get
            {
                uint itemHash = NetworkObject != null ? NetworkObject.PrefabIdHash : 0u;
                return fireAsSelfProjectile || projectilePrefabHash == 0 ? itemHash : projectilePrefabHash;
            }
        }
        public HitCue HitCueType => hitCue;
        public Color MainColor => mainColor;
        public Bounds LocalBounds => localBounds;
        public Vector3 OriginalWorldScale => originalWorldScale;
        public Vector3 WorldCenter => transform.TransformPoint(localBounds.center);

        public Vector3 ProjectileLaunchLocalPosition => fireAsSelfProjectile
            ? localBounds.center
            : new Vector3(localBounds.center.x, localBounds.center.y, localBounds.max.z);

        public Vector3 GetMuzzleWorldPosition()
        {
            return transform.TransformPoint(ProjectileLaunchLocalPosition);
        }

        public Vector3 GetProjectileLaunchOrigin()
        {
            return transform.TransformPoint(ProjectileLaunchLocalPosition);
        }

        public Vector3 GetMuzzleTipWorldPosition()
        {
            return transform.TransformPoint(muzzleOffset);
        }

        public Vector3 CreateClientThrowSpin()
        {
            return transform.right * TOSS_SPIN_SPEED + transform.up * (TOSS_SPIN_SPEED * 0.5f);
        }

        public bool IsWithinPickupRange(Vector3 position)
        {
            Bounds bounds = GetWorldBounds();
            return Vector3.Distance(position, bounds.ClosestPoint(position)) <= PICKUP_RANGE;
        }

        public void SetLabelVisible(bool isVisible)
        {
            if (labelVisuals == null)
            {
                return;
            }

            bool shouldShow = alwaysShowLabel || (isVisible && State == GunItemState.World);
            foreach (GunItemLabelVisual labelVisual in labelVisuals)
            {
                if (labelVisual != null)
                {
                    labelVisual.gameObject.SetActive(shouldShow);
                }
            }
        }

        public Vector3 PickupPositionOffset => pickupPositionOffset;
        public Vector3 PickupScale => pickupScale;
        public Vector3 PickupRotationEuler => pickupRotationEuler;
        public Vector3 MuzzleOffset => muzzleOffset;
        public GunHoldType HoldType => holdType;
        public Transform GripPrimary => gripPrimary;
        public Transform GripSupport => gripSupport;
        public float TpHoldDistance => tpHoldDistance;
        public float TpHoldHeight => tpHoldHeight;

        public float RoundsPerMinute => roundsPerMinute;
        public int MagazineSize => magazineSize;
        public float ReloadDuration => reloadDuration;
        public int ProjectilesPerShot => Mathf.Max(1, projectilesPerShot);
        public int BurstCount => Mathf.Max(1, burstCount);
        public float BurstInterval => Mathf.Max(0.01f, burstInterval);
        public float SpreadAngle => Mathf.Max(0f, spreadAngle);

        public float RecoilDistance => BASE_RECOIL_DISTANCE * WeightScale;
        public float RecoilPitch => BASE_RECOIL_PITCH * WeightScale;
        public float CameraShake => BASE_CAMERA_SHAKE * WeightScale;
        public float FieldOfViewPunch => BASE_FIELD_OF_VIEW_PUNCH * WeightScale;
        public float MuzzleFlashScale => BASE_MUZZLE_FLASH_SCALE * WeightScale;
        public float FireVolume => weightClass switch
        {
            GunWeightClass.Light => LIGHT_FIRE_VOLUME,
            GunWeightClass.Heavy => HEAVY_FIRE_VOLUME,
            _ => MEDIUM_FIRE_VOLUME
        };
        public float FirePitch => weightClass switch
        {
            GunWeightClass.Light => LIGHT_FIRE_PITCH,
            GunWeightClass.Heavy => HEAVY_FIRE_PITCH,
            _ => DEFAULT_FIRE_PITCH
        };

        private float WeightScale => weightClass switch
        {
            GunWeightClass.Light => LIGHT_WEIGHT_SCALE,
            GunWeightClass.Heavy => HEAVY_WEIGHT_SCALE,
            _ => 1f
        };

        public static event Action<ulong, GunItem> HolderChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            heldItems.Clear();
            outstandingThrows.Clear();
            HolderChanged = null;
            projectileLayerCollisionsConfigured = false;
            EnsureProjectileLayerCollisions();
        }

        private static void EnsureProjectileLayerCollisions()
        {
            if (projectileLayerCollisionsConfigured)
            {
                return;
            }

            Physics.IgnoreLayerCollision(
                GUN_PROP_LAYER_INDEX,
                WorldHullLayerIndex,
                true);
            Physics.IgnoreLayerCollision(GUN_PROP_LAYER_INDEX, 0, false);
            Physics.IgnoreLayerCollision(WorldHullLayerIndex, 0, false);
            Physics.IgnoreLayerCollision(GUN_PROP_LAYER_INDEX, GUN_PROP_LAYER_INDEX, false);
            Physics.IgnoreLayerCollision(WorldHullLayerIndex, WorldHullLayerIndex, false);
            projectileLayerCollisionsConfigured = true;
        }

        private void Awake()
        {
            CacheComponents();
            originalWorldScale = transform.localScale;
            localBounds = CalculateLocalBounds();
            mainColor = GunColorUtility.GetMainColor(gameObject);
            SetLabelVisible(false);
        }

        private void CacheComponents()
        {
            colliders = GetComponentsInChildren<Collider>();
            GunItemHullCollider hullMarker = GetComponent<GunItemHullCollider>();
            projectileHullCollider = hullMarker != null ? hullMarker.GetComponent<Collider>() : null;
            rigidBody = GetComponent<Rigidbody>();
            projectileBody = GetComponent<GunProjectileBody>();
            labelVisuals = GetComponentsInChildren<GunItemLabelVisual>(true);

            if (rigidBody != null)
            {
                rigidBody.sleepThreshold = WORLD_SLEEP_THRESHOLD;
                rigidBody.maxAngularVelocity = MAX_ANGULAR_VELOCITY;
            }
        }

        public override void OnNetworkSpawn()
        {
            CacheComponents();
            if (!allItems.Contains(this))
            {
                allItems.Add(this);
            }

            cachedProjectilePrefab = null;
            cachedProjectilePrefabHash = 0;

            if (IsServer)
            {
                if (isPreparedProjectile)
                {
                    state.Value = (int)GunItemState.Projectile;
                    currentAmmo.Value = magazineSize;
                    isReloading.Value = false;
                }
                else if (currentAmmo.Value == 0)
                {
                    currentAmmo.Value = magazineSize;
                }
            }

            state.OnValueChanged += OnStateChanged;
            holderId.OnValueChanged += OnHolderChanged;
            isReloading.OnValueChanged += OnReloadChanged;
            isProjectileHidden.OnValueChanged += OnProjectileHiddenChanged;

            SetPhysicsEnabled(State);
            ApplyProjectileVisibility(isProjectileHidden.Value);
            ApplyStateScale();
            TryBindHeldVisualToHolder();
            SetLabelVisible(false);

            if (State == GunItemState.Held && holderId.Value != NO_HOLDER)
            {
                HolderChanged?.Invoke(holderId.Value, this);
            }

            if (IsOwner && State == GunItemState.Held)
            {
                StartDrawAnimation();
            }
        }

        public override void OnNetworkDespawn()
        {
            allItems.Remove(this);
            ClearOutstandingThrownItemFor(this);
            clientShotGroups.Clear();
            state.OnValueChanged -= OnStateChanged;
            holderId.OnValueChanged -= OnHolderChanged;
            isReloading.OnValueChanged -= OnReloadChanged;
            isProjectileHidden.OnValueChanged -= OnProjectileHiddenChanged;
            StopReloadCoroutine();
            StopBurstCoroutine();

            if (worldColliderEnableCoroutine != null)
            {
                StopCoroutine(worldColliderEnableCoroutine);
                worldColliderEnableCoroutine = null;
            }

            if (!IsServer)
            {
                return;
            }

            foreach (KeyValuePair<ulong, GunItem> pair in heldItems)
            {
                if (pair.Value == this)
                {
                    heldItems.Remove(pair.Key);
                    break;
                }
            }
        }

        public void PrepareAsProjectile()
        {
            isPreparedProjectile = true;
            hasBeenPickedUp = true;
        }

        public void EnterWorldStateServer()
        {
            if (!IsServer)
            {
                return;
            }

            currentAmmo.Value = magazineSize;
            holderId.Value = NO_HOLDER;
            isReloading.Value = false;
            StopBurstCoroutine();
            state.Value = (int)GunItemState.World;
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }

        public void ReleaseFromWorldMountServer(Vector3 impulse, Vector3 contactPoint)
        {
            if (!IsServer ||
                State != GunItemState.World ||
                !fixedInWorld ||
                hasBeenPickedUp)
            {
                return;
            }

            hasBeenPickedUp = true;
            SetPhysicsEnabled(GunItemState.World);

            if (rigidBody == null)
            {
                return;
            }

            rigidBody.WakeUp();
            rigidBody.AddForceAtPosition(impulse, contactPoint, ForceMode.Impulse);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPickUpServerRpc(Vector3 pickupPoint, ServerRpcParams serverRpcParams = default)
        {
            TryPickUp(serverRpcParams.Receive.SenderClientId, pickupPoint);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestDropServerRpc(Vector3 heldPosition, Quaternion heldRotation, ServerRpcParams serverRpcParams = default)
        {
            TryDrop(serverRpcParams.Receive.SenderClientId, heldPosition, heldRotation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestFireServerRpc(
            Vector3 muzzlePosition,
            Vector3 fireDirection,
            ServerRpcParams serverRpcParams = default)
        {
            ProjectileTrace.Log(
                "rpc fire t=" + Time.time.ToString("F3") +
                " sender=" + serverRpcParams.Receive.SenderClientId +
                " gunId=" + NetworkObjectId +
                " state=" + State +
                " holder=" + HolderId +
                " ammo=" + CurrentAmmo +
                " reload=" + IsReloading +
                " muzzle=" + muzzlePosition.ToString("F3") +
                " dir=" + fireDirection.ToString("F3"));

            TryFire(serverRpcParams.Receive.SenderClientId, muzzlePosition, fireDirection);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReloadServerRpc(ServerRpcParams serverRpcParams = default)
        {
            TryReload(serverRpcParams.Receive.SenderClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void FireClientVisualShotsServerRpc(
            ProjectileShotVolley volley,
            ServerRpcParams serverRpcParams = default)
        {
            if (!IsServer || volley.Shots == null || volley.Shots.Length == 0)
            {
                return;
            }

            if (GameModeController.Instance == null ||
                !GameModeController.Instance.UseClientVisualProjectiles)
            {
                return;
            }

            ulong senderClientId = serverRpcParams.Receive.SenderClientId;
            int shotCount = volley.Shots.Length;

            if (State != GunItemState.Held ||
                holderId.Value != senderClientId ||
                !GameModeController.IsGameplayActive)
            {
                LogRejectedClientShot(senderClientId, "state");
                return;
            }

            if (shotCount > MAX_CLIENT_SHOTS_PER_VOLLEY ||
                shotCount > ProjectilesPerShot)
            {
                LogRejectedClientShot(senderClientId, "pellet-cap");
                return;
            }

            uint shotGroupId = volley.Shots[0].shotGroupId;
            bool isNewGroup = !clientShotGroups.TryGetValue(senderClientId, out uint currentGroupId) ||
                              currentGroupId != shotGroupId;

            if (isNewGroup && Time.time < nextFireTime - CLIENT_SHOT_GROUP_TOLERANCE)
            {
                LogRejectedClientShot(senderClientId, "fire-rate");
                return;
            }

            if (currentAmmo.Value < shotCount)
            {
                LogRejectedClientShot(senderClientId, "ammo");
                return;
            }

            if (isNewGroup)
            {
                nextFireTime = Time.time + 60f / Mathf.Max(1f, roundsPerMinute);
                clientShotGroups[senderClientId] = shotGroupId;
            }

            currentAmmo.Value -= shotCount;

            for (int index = 0; index < shotCount; index++)
            {
                ProjectileShotData shot = volley.Shots[index];
                if (!IsFinite(shot.origin) || !IsFinite(shot.direction))
                {
                    LogRejectedClientShot(senderClientId, "pose");
                    return;
                }

                Vector3 direction = shot.direction.sqrMagnitude > MIN_FIRE_DIRECTION_SQR
                    ? shot.direction.normalized
                    : transform.forward;

                shot.shooter = senderClientId;
                shot.shotGroupId = shotGroupId;
                shot.selfProjectile = fireAsSelfProjectile;
                shot.selfProjectileNetworkObjectId = fireAsSelfProjectile ? NetworkObjectId : 0UL;
                shot.direction = direction;
                shot.speed = projectileSpeed;
                shot.radius = ProjectileRadius;
                shot.gravityScale = ProjectileGravityScale;
                shot.lifetime = projectileLifetime;
                shot.bounceDamping = ProjectileBounceDamping;
                shot.angularDamping = ProjectileAngularDamping;
                shot.mass = ProjectileMass;
                shot.damage = damage;
                shot.releaseDistance = Mathf.Max(0f, shot.releaseDistance);
                uint prefabIdHash = NetworkObject != null ? NetworkObject.PrefabIdHash : 0u;
                bool useItemShape = fireAsSelfProjectile ||
                                    (prefabIdHash != 0u && ProjectileVisualHash == prefabIdHash);
                Vector3 scaledExtents = Vector3.Scale(localBounds.extents, originalWorldScale);
                Vector3 scaledCenter = Vector3.Scale(localBounds.center, originalWorldScale);
                Vector3 scaledLaunch = Vector3.Scale(ProjectileLaunchLocalPosition, originalWorldScale);
                if (!IsFinite(scaledExtents) ||
                    !IsFinite(scaledCenter) ||
                    !IsFinite(scaledLaunch) ||
                    scaledExtents.x <= 0f ||
                    scaledExtents.y <= 0f ||
                    scaledExtents.z <= 0f)
                {
                    useItemShape = false;
                }

                shot.useItemShape = useItemShape;
                shot.shapeType = useItemShape
                    ? (byte)ProjectileShapeUtility.ResolveShapeType(scaledExtents)
                    : (byte)ProjectileShapeType.Sphere;
                shot.localHalfExtents = useItemShape ? scaledExtents : Vector3.zero;
                shot.localCenterOffset = useItemShape ? scaledCenter - scaledLaunch : Vector3.zero;
                shot.localCenter = useItemShape ? scaledCenter : Vector3.zero;
                shot.spawnGraceSeconds = useItemShape ? ProjectileFlightSim.SelfProjectileSpawnGraceSeconds : 0f;
                shot.maxBounces = ProjectileMaxBounces;
                shot.visualPrefabHash = unchecked((int)ProjectileVisualHash);
                shot.weaponHash = unchecked((int)prefabIdHash);
                shot.pelletIndex = index;
                shot.pelletCount = shotCount;
                shot.spin = Vector3.ClampMagnitude(shot.spin, TOSS_SPIN_SPEED * 4f);
                shot.serverTime = shot.serverTime > 0d
                    ? shot.serverTime
                    : NetworkManager.ServerTime.Time;
                volley.Shots[index] = shot;
            }

            ProjectileTrace.Log(
                "client shot volley t=" + Time.time.ToString("F3") +
                " sender=" + senderClientId +
                " gun=" + name +
                " shots=" + shotCount +
                " group=" + shotGroupId +
                " ammo=" + currentAmmo.Value);

            for (int index = 0; index < shotCount; index++)
            {
                ProjectileShotData shot = volley.Shots[index];
                if (shot.selfProjectile)
                {
                    Vector3 direction = shot.direction.sqrMagnitude > MIN_FIRE_DIRECTION_SQR
                        ? shot.direction.normalized
                        : transform.forward;
                    Vector3 rotationUp = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f
                        ? transform.up
                        : Vector3.up;
                    Quaternion rotation = Quaternion.LookRotation(direction, rotationUp);
                    Vector3 position = shot.origin + direction * Mathf.Max(0f, shot.releaseDistance);
                    if (!PrepareSelfProjectileForClientVisualFlightServer(
                            senderClientId,
                            (int)shot.shotGroupId,
                            position,
                            rotation))
                    {
                        LogRejectedClientShot(senderClientId, "self-projectile");
                        continue;
                    }
                }

                ServerProjectileFlight.Start(shot, this, shot.selfProjectile ? this : null);
            }

            BroadcastClientVisualShots(volley, senderClientId);
        }

        [ClientRpc]
        private void FireClientVisualShotsClientRpc(
            ProjectileShotVolley volley,
            ClientRpcParams clientRpcParams = default)
        {
            if (volley.Shots == null || volley.Shots.Length == 0)
            {
                return;
            }

            ProjectileShotData firstShot = volley.Shots[0];
            if (NetworkManager.LocalClientId == firstShot.shooter)
            {
                return;
            }

            Vector3 renderedMuzzle = ProjectileMuzzleCache.TryGet(firstShot.shooter, out Vector3 cachedMuzzle)
                ? cachedMuzzle
                : firstShot.origin;
            Vector3 flashMuzzle = firstShot.origin + ProjectileLaunchUtility.RemoveLateralOffset(
                renderedMuzzle - firstShot.origin,
                firstShot.direction);

            for (int index = 0; index < volley.Shots.Length; index++)
            {
                ProjectileVisualPool.Spawn(volley.Shots[index], false, renderedMuzzle, null);
            }

            PlayLocalFireFeedback(flashMuzzle, firstShot.direction);
        }

        private void BroadcastClientVisualShots(ProjectileShotVolley volley, ulong shooterClientId)
        {
            List<ulong> targetClientIds = new List<ulong>();
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId != shooterClientId)
                {
                    targetClientIds.Add(clientId);
                }
            }

            if (targetClientIds.Count == 0)
            {
                return;
            }

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = targetClientIds
                }
            };

            FireClientVisualShotsClientRpc(volley, clientRpcParams);
        }

        private void LogRejectedClientShot(ulong senderClientId, string reason)
        {
            ProjectileTrace.Log(
                "client shot reject sender=" + senderClientId +
                " gun=" + name +
                " reason=" + reason);
        }

        private void OnStateChanged(int previousValue, int newValue)
        {
            GunItemState previousState = (GunItemState)previousValue;
            GunItemState newState = (GunItemState)newValue;
            hasBoundHeldVisual = false;

            if (worldColliderEnableCoroutine != null)
            {
                StopCoroutine(worldColliderEnableCoroutine);
                worldColliderEnableCoroutine = null;
            }

            SetPhysicsEnabled(newState);
            ApplyStateScale();
            SetLabelVisible(false);

            if (projectileBody != null && newState != GunItemState.Projectile)
            {
                projectileBody.RestoreIgnoredCollisions();
            }
            else if (projectileBody != null && !IsServer)
            {
                projectileBody.RefreshShooterCollisionIgnores();
            }

            if (IsServer && newState == GunItemState.Held)
            {
                nextFireTime = 0f;
            }
            else if (IsServer)
            {
                StopBurstCoroutine();
            }

            if (!IsServer && previousState == GunItemState.Held && newState == GunItemState.World)
            {
                StartWorldColliderDelay();
            }
        }

        private void StartWorldColliderDelay()
        {
            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }

            worldColliderEnableCoroutine = StartCoroutine(EnableWorldCollidersAfterDelay());
        }

        private IEnumerator EnableWorldCollidersAfterDelay()
        {
            yield return new WaitForSeconds(WORLD_COLLIDER_ENABLE_DELAY);
            worldColliderEnableCoroutine = null;

            if (State == GunItemState.World)
            {
                SetPhysicsEnabled(GunItemState.World);
            }
        }

        private void OnHolderChanged(ulong previousValue, ulong newValue)
        {
            hasBoundHeldVisual = false;

            if (previousValue != NO_HOLDER)
            {
                HolderChanged?.Invoke(previousValue, null);
            }

            if (newValue != NO_HOLDER)
            {
                HolderChanged?.Invoke(newValue, this);
            }

            if (IsOwner && newValue == OwnerClientId)
            {
                StartDrawAnimation();
            }
        }

        private void StartDrawAnimation()
        {
            if (!IsOwner)
            {
                return;
            }

            GameModeController controller = GameModeController.Instance;
            if (controller == null || controller.Mode != GameModeType.GunGame)
            {
                return;
            }

            drawStartTime = Time.time;
            isDrawing = true;
        }

        private void OnReloadChanged(bool previousValue, bool newValue)
        {
            if (!IsOwner)
            {
                return;
            }

            if (newValue && !previousValue)
            {
                ProceduralAudio.Play(ProceduralAudio.ReloadClip, transform.position, 0.4f);
                return;
            }

            if (!newValue && State == GunItemState.Held && currentAmmo.Value >= magazineSize)
            {
                ProceduralAudio.Play(ProceduralAudio.ReloadCompleteClip, transform.position, 0.4f);
            }
        }

        private void TryFire(ulong clientId, Vector3 muzzlePosition, Vector3 fireDirection)
        {
            if (!IsServer || State != GunItemState.Held || !GameModeController.IsGameplayActive)
            {
                return;
            }

            if (holderId.Value != clientId || isReloading.Value || burstCoroutine != null)
            {
                return;
            }

            if (currentAmmo.Value <= 0)
            {
                TryReload(clientId);
                return;
            }

            float now = Time.time;
            if (now < nextFireTime)
            {
                return;
            }

            int pelletsPerShot = Mathf.Min(ProjectilesPerShot, currentAmmo.Value);
            if (pelletsPerShot <= 0)
            {
                return;
            }

            NetworkObject playerObject = GetPlayerObject(clientId);
            if (playerObject != null && playerObject.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.EndSpawnProtection();
            }

            ProjectileTrace.Log(
                "try fire t=" + now.ToString("F3") +
                " client=" + clientId +
                " gunId=" + NetworkObjectId +
                " ammo=" + currentAmmo.Value +
                " pellets=" + pelletsPerShot +
                " burst=" + BurstCount +
                " next=" + nextFireTime.ToString("F3") +
                " self=" + fireAsSelfProjectile);

            nextFireTime = now + 60f / roundsPerMinute;
            int shotGroupId = ++nextShotGroupId;
            GetProjectileSpawnPose(
                muzzlePosition,
                fireDirection,
                out Vector3 feedbackPosition,
                out _);
            PlayFireFeedbackForOthers(feedbackPosition, fireDirection, clientId);

            if (fireAsSelfProjectile)
            {
                if (TryLaunchSelfAsProjectile(clientId, muzzlePosition, fireDirection, shotGroupId))
                {
                    currentAmmo.Value = 0;
                }

                return;
            }

            if (BurstCount <= 1)
            {
                FireProjectileVolley(clientId, muzzlePosition, fireDirection, pelletsPerShot, shotGroupId);
                return;
            }

            burstCoroutine = StartCoroutine(BurstRoutine(clientId, muzzlePosition, fireDirection, pelletsPerShot, shotGroupId));
        }

        private bool TryLaunchSelfAsProjectile(
            ulong clientId,
            Vector3 muzzlePosition,
            Vector3 fireDirection,
            int shotGroupId)
        {
            if (!IsServer ||
                State != GunItemState.Held ||
                holderId.Value != clientId ||
                projectileBody == null)
            {
                return false;
            }

            Vector3 direction = fireDirection.sqrMagnitude > MIN_FIRE_DIRECTION_SQR
                ? fireDirection.normalized
                : transform.forward;
            Vector3 finalUp = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f
                ? transform.up
                : Vector3.up;
            Quaternion launchRotation = Quaternion.LookRotation(direction, finalUp);
            Vector3 launchPosition = GetReleasePosition(muzzlePosition, launchRotation);

            if (heldItems.TryGetValue(clientId, out GunItem heldItem) && heldItem == this)
            {
                heldItems.Remove(clientId);
            }

            holderId.Value = NO_HOLDER;
            currentAmmo.Value = 0;
            state.Value = (int)GunItemState.Projectile;
            outstandingThrows[clientId] = new OutstandingThrow
            {
                Item = this,
                RegisteredAt = Time.time
            };
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);

            transform.SetPositionAndRotation(launchPosition, launchRotation);

            ProjectileTrace.Log(
                "self launch t=" + Time.time.ToString("F3") +
                " client=" + clientId +
                " gunId=" + NetworkObjectId +
                " pos=" + launchPosition.ToString("F3") +
                " dir=" + direction.ToString("F3") +
                " speed=" + projectileSpeed);

            projectileBody.SetPlayerDamageEnabledServer(false);
            projectileBody.SetLaunchVelocityServer(direction * projectileSpeed);
            projectileBody.BeginSelfProjectileServer(clientId, shotGroupId, projectileSpeed);

            Rigidbody itemRigidbody = GetComponent<Rigidbody>();
            if (itemRigidbody != null)
            {
                itemRigidbody.linearVelocity = direction * projectileSpeed;
            }

            return true;
        }

        private void FireProjectileVolley(ulong clientId, Vector3 muzzlePosition, Vector3 fireDirection, int pellets, int shotGroupId)
        {
            int fired = 0;
            for (int i = 0; i < pellets; i++)
            {
                if (currentAmmo.Value <= 0)
                {
                    break;
                }

                if (SpawnPhysicalProjectile(clientId, muzzlePosition, fireDirection, pellets > 1, shotGroupId))
                {
                    fired++;
                }
            }

            if (fired > 0)
            {
                currentAmmo.Value = Mathf.Max(0, currentAmmo.Value - fired);
            }
        }

        private IEnumerator BurstRoutine(ulong clientId, Vector3 muzzlePosition, Vector3 fireDirection, int pellets, int shotGroupId)
        {
            int burstShots = BurstCount;

            for (int shot = 0; shot < burstShots; shot++)
            {
                if (State != GunItemState.Held ||
                    holderId.Value != clientId ||
                    isReloading.Value ||
                    currentAmmo.Value <= 0)
                {
                    break;
                }

                int pelletsThisShot = Mathf.Min(pellets, currentAmmo.Value);
                int fired = 0;
                for (int i = 0; i < pelletsThisShot; i++)
                {
                    if (SpawnPhysicalProjectile(clientId, muzzlePosition, fireDirection, pelletsThisShot > 1, shotGroupId))
                    {
                        fired++;
                    }
                }

                if (fired > 0)
                {
                    currentAmmo.Value = Mathf.Max(0, currentAmmo.Value - fired);
                }

                if (fired == 0)
                {
                    break;
                }

                if (shot + 1 < burstShots)
                {
                    yield return new WaitForSeconds(BurstInterval);
                }
            }

            burstCoroutine = null;
        }

        private void StopBurstCoroutine()
        {
            if (burstCoroutine != null)
            {
                StopCoroutine(burstCoroutine);
                burstCoroutine = null;
            }
        }

        private bool SpawnPhysicalProjectile(ulong shooterClientId, Vector3 muzzlePosition, Vector3 fireDirection, bool applySpread, int shotGroupId)
        {
            NetworkManager networkManager = GetNetworkManager();
            if (networkManager == null)
            {
                return false;
            }

            uint prefabHash = projectilePrefabHash != 0 ? projectilePrefabHash : NetworkObject.PrefabIdHash;
            GameObject prefab = GetCachedProjectilePrefab(networkManager, prefabHash);
            if (prefab == null)
            {
                Debug.LogWarning($"[{nameof(GunItem)}] No registered prefab found for '{name}'. Rebuild the network prefab list.", this);
                return false;
            }

            GunProjectileBody prefabProjectileBody = prefab.GetComponent<GunProjectileBody>();
            if (prefabProjectileBody == null)
            {
                Debug.LogWarning($"[{nameof(GunItem)}] Projectile prefab '{prefab.name}' is missing {nameof(GunProjectileBody)}.", this);
                return false;
            }

            Vector3 direction = fireDirection.sqrMagnitude > MIN_FIRE_DIRECTION_SQR
                ? fireDirection.normalized
                : transform.forward;

            if (applySpread && SpreadAngle > 0f)
            {
                Vector3 rotationUp = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f
                    ? transform.up
                    : Vector3.up;
                Quaternion baseRotation = Quaternion.LookRotation(direction, rotationUp);
                float yaw = UnityEngine.Random.Range(-SpreadAngle, SpreadAngle);
                float pitch = UnityEngine.Random.Range(-SpreadAngle, SpreadAngle);
                direction = baseRotation * (Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);
                direction.Normalize();
            }

            GetProjectileSpawnPose(muzzlePosition, direction, out Vector3 spawnPosition, out Quaternion spawnRotation);

            ProjectileTrace.Log(
                "spawn t=" + Time.time.ToString("F3") +
                " prefab=" + prefab.name +
                " hash=" + prefabHash +
                " pos=" + spawnPosition.ToString("F3") +
                " rot=" + spawnRotation.eulerAngles.ToString("F1") +
                " dir=" + direction.ToString("F3") +
                " speed=" + projectileSpeed);

            GunProjectileRegistry.EnforceLimits(prefabHash, prefabProjectileBody.MaxActiveProjectiles);

            GameObject projectileInstance = Instantiate(prefab, spawnPosition, spawnRotation);

            GunItem projectileItem = projectileInstance.GetComponent<GunItem>();
            GunProjectileBody projectile = projectileInstance.GetComponent<GunProjectileBody>();
            NetworkObject projectileNetworkObject = projectileInstance.GetComponent<NetworkObject>();
            if (projectileItem == null || projectile == null || projectileNetworkObject == null)
            {
                Destroy(projectileInstance);
                return false;
            }

            projectileItem.PrepareAsProjectile();
            projectile.PrepareServerSpawn(shooterClientId, shotGroupId, projectileSpeed);
            projectile.SetPlayerDamageEnabledServer(false);
            projectile.SetLaunchVelocityServer(direction * projectileSpeed);
            projectileNetworkObject.Spawn(destroyWithScene: true);

            Rigidbody projectileRigidbody = projectileInstance.GetComponent<Rigidbody>();
            if (projectileRigidbody != null)
            {
                projectileRigidbody.linearVelocity = direction * projectileSpeed;
            }

            NetworkTransform networkTransform = projectileInstance.GetComponent<NetworkTransform>();
            ProjectileTrace.Log(
                "spawned t=" + Time.time.ToString("F3") +
                " id=" + projectileNetworkObject.NetworkObjectId +
                " owner=" + projectileNetworkObject.OwnerClientId +
                " server=" + networkManager.IsServer +
                " client=" + networkManager.IsClient +
                " spent=" + projectile.IsSpent +
                " retiring=" + projectile.IsRetiring +
                " kinematic=" + (projectileRigidbody != null && projectileRigidbody.isKinematic) +
                " interp=" + (networkTransform != null && networkTransform.Interpolate) +
                " interpType=" + (networkTransform != null ? networkTransform.PositionInterpolationType.ToString() : "null") +
                " smoothing=" + (networkTransform != null && networkTransform.PositionLerpSmoothing) +
                " maxInterp=" + (networkTransform != null ? networkTransform.PositionMaxInterpolationTime.ToString("F3") : "null"));

            return true;
        }

        private void PlayFireFeedbackForOthers(Vector3 muzzlePosition, Vector3 direction, ulong shooterClientId)
        {
            NetworkManager networkManager = GetNetworkManager();
            if (networkManager == null)
            {
                return;
            }

            fireFeedbackTargets.Clear();
            foreach (ulong clientId in networkManager.ConnectedClientsIds)
            {
                if (clientId != shooterClientId)
                {
                    fireFeedbackTargets.Add(clientId);
                }
            }

            int targetCount = fireFeedbackTargets.Count;
            if (targetCount == 0)
            {
                return;
            }

            if (fireFeedbackTargetArray == null || fireFeedbackTargetArray.Length != targetCount)
            {
                fireFeedbackTargetArray = new ulong[targetCount];
            }

            fireFeedbackTargets.CopyTo(fireFeedbackTargetArray);
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = fireFeedbackTargetArray
                }
            };

            PlayFireFeedbackClientRpc(muzzlePosition, direction, clientRpcParams);
        }

        [ClientRpc]
        private void PlayFireFeedbackClientRpc(Vector3 muzzlePosition, Vector3 direction, ClientRpcParams clientRpcParams = default)
        {
            PlayFireFeedback(muzzlePosition, direction);
        }

        public void PlayLocalFireFeedback(Vector3 muzzlePosition, Vector3 direction)
        {
            PlayFireFeedback(muzzlePosition, direction);
        }

        private void PlayFireFeedback(Vector3 muzzlePosition, Vector3 direction)
        {
            ProceduralAudio.Play(ProceduralAudio.FireClip, muzzlePosition, FireVolume, FirePitch);
            // Muzzle flash VFX is currently broken; disabled until fixed.
        }

        private void TryReload(ulong clientId)
        {
            if (!CanReload)
            {
                return;
            }

            if (!IsServer || State != GunItemState.Held || !GameModeController.IsGameplayActive)
            {
                return;
            }

            if (holderId.Value != clientId || isReloading.Value || burstCoroutine != null || currentAmmo.Value >= magazineSize)
            {
                return;
            }

            isReloading.Value = true;
            reloadCoroutine = StartCoroutine(ReloadRoutine());
        }

        private System.Collections.IEnumerator ReloadRoutine()
        {
            yield return new WaitForSeconds(reloadDuration);

            if (State == GunItemState.Held)
            {
                currentAmmo.Value = magazineSize;
            }

            isReloading.Value = false;
            nextFireTime = 0f;
            reloadCoroutine = null;
        }

        private void CancelReload()
        {
            StopReloadCoroutine();
            isReloading.Value = false;
        }

        private void StopReloadCoroutine()
        {
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }
        }

        public void ApplyLocalRecoil()
        {
            recoilAmount = RecoilDistance;
            recoilPitchAmount = RecoilPitch;
        }

        private void Update()
        {
            TryBindHeldVisualToHolder();

            if (isProjectileHidden.Value)
            {
                ApplyProjectileVisibility(true);
            }
        }

        public void SetProjectileHiddenServer(bool hidden)
        {
            if (!IsServer)
            {
                return;
            }

            isProjectileHidden.Value = hidden;
            ApplyProjectilePresentation(hidden);
        }

        private void OnProjectileHiddenChanged(bool previousValue, bool newValue)
        {
            if (!IsServer)
            {
                ApplyProjectilePresentation(newValue);
            }
        }

        private void ApplyProjectilePresentation(bool hidden)
        {
            NetworkTransform networkTransform = GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.enabled = !hidden;
            }

            if (projectileBody != null)
            {
                projectileBody.enabled = !hidden;
            }

            SetPhysicsEnabled(State);
            ApplyProjectileVisibility(hidden);
        }

        private void ApplyProjectileVisibility(bool hidden)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                {
                    renderer.enabled = !hidden;
                }
            }

            SetLabelVisible(false);
        }

        public bool PrepareSelfProjectileForClientVisualFlightServer(
            ulong clientId,
            int shotGroupId,
            Vector3 position,
            Quaternion rotation)
        {
            if (!IsServer ||
                !fireAsSelfProjectile ||
                State != GunItemState.Held ||
                holderId.Value != clientId)
            {
                return false;
            }

            if (heldItems.TryGetValue(clientId, out GunItem heldItem) && heldItem == this)
            {
                heldItems.Remove(clientId);
            }

            holderId.Value = NO_HOLDER;
            currentAmmo.Value = 0;
            outstandingThrows[clientId] = new OutstandingThrow
            {
                Item = this,
                RegisteredAt = Time.time
            };
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            transform.SetPositionAndRotation(position, rotation);
            ProjectileTrace.Log(
                "self flight prepare item=" + name +
                " client=" + clientId +
                " pos=" + position.ToString("F2"));
            SetProjectileHiddenServer(true);
            state.Value = (int)GunItemState.Projectile;
            return true;
        }

        public void RevealClientVisualProjectileServer(
            ulong shooterClientId,
            uint shotId,
            Vector3 shapeCenter,
            Quaternion rotation,
            Vector3 linearVelocity,
            Vector3 angularVelocity)
        {
            if (!IsServer)
            {
                return;
            }

            Vector3 pivotPosition = GetPivotPositionForShapeCenter(shapeCenter, rotation);
            ResolveSafeWorldPoseServer(ref pivotPosition, rotation);
            RevealResolvedClientVisualProjectileServer(
                shooterClientId,
                shotId,
                pivotPosition,
                rotation,
                linearVelocity,
                angularVelocity);
        }

        public Vector3 GetPivotPositionForShapeCenter(Vector3 shapeCenter, Quaternion rotation)
        {
            return shapeCenter - rotation * Vector3.Scale(localBounds.center, originalWorldScale);
        }

        public void RevealResolvedClientVisualProjectileServer(
            ulong shooterClientId,
            uint shotId,
            Vector3 pivotPosition,
            Quaternion rotation,
            Vector3 linearVelocity,
            Vector3 angularVelocity)
        {
            if (!IsServer)
            {
                return;
            }

            transform.SetPositionAndRotation(pivotPosition, rotation);
            SetProjectileHiddenServer(false);
            state.Value = (int)GunItemState.World;
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            if (rigidBody != null && !rigidBody.isKinematic)
            {
                rigidBody.WakeUp();
                rigidBody.linearVelocity = linearVelocity;
                rigidBody.angularVelocity = angularVelocity;
            }

            RevealClientVisualProjectileClientRpc(shooterClientId, shotId, pivotPosition, rotation);
        }

        public void ResolveSafeWorldPoseServer(ref Vector3 pivotPosition, Quaternion rotation)
        {
            if (!IsServer)
            {
                return;
            }

            Vector3 scaledSize = Vector3.Scale(localBounds.size, originalWorldScale);
            if (scaledSize.x <= MIN_SAFE_POSE_SIZE ||
                scaledSize.y <= MIN_SAFE_POSE_SIZE ||
                scaledSize.z <= MIN_SAFE_POSE_SIZE)
            {
                return;
            }

            GameObject probeObject = new GameObject("GunItemSafePoseProbe");
            BoxCollider probeCollider = probeObject.AddComponent<BoxCollider>();
            probeCollider.enabled = true;
            probeCollider.size = scaledSize;
            probeCollider.center = Vector3.Scale(localBounds.center, originalWorldScale);
            probeObject.transform.SetPositionAndRotation(pivotPosition, rotation);
            probeObject.transform.localScale = Vector3.one;
            Physics.SyncTransforms();

            float movedDistance = 0f;
            for (int iteration = 0; iteration < MAX_SAFE_POSE_ITERATIONS; iteration++)
            {
                Vector3 worldCenter = probeObject.transform.TransformPoint(probeCollider.center);
                Vector3 halfExtents = probeCollider.size * 0.5f;
                int overlapCount = Physics.OverlapBoxNonAlloc(
                    worldCenter,
                    halfExtents,
                    safePoseOverlapBuffer,
                    rotation,
                    ProjectileCollision.WorldMask,
                    QueryTriggerInteraction.Collide);

                bool moved = false;
                for (int index = 0; index < overlapCount; index++)
                {
                    Collider other = safePoseOverlapBuffer[index];
                    if (other == null || other == probeCollider)
                    {
                        continue;
                    }

                    GunItem otherItem = other.GetComponentInParent<GunItem>();
                    if (otherItem == this ||
                        (otherItem != null && otherItem.State == GunItemState.Projectile) ||
                        other.GetComponentInParent<PlayerHealth>() != null)
                    {
                        continue;
                    }

                    if (!Physics.ComputePenetration(
                            probeCollider,
                            probeObject.transform.position,
                            rotation,
                            other,
                            other.transform.position,
                            other.transform.rotation,
                            out Vector3 direction,
                            out float distance))
                    {
                        continue;
                    }

                    if (distance <= 0f)
                    {
                        continue;
                    }

                    float remainingDistance = MAX_SAFE_POSE_TOTAL_DISTANCE - movedDistance;
                    if (remainingDistance <= 0f)
                    {
                        break;
                    }

                    float step = Mathf.Min(distance, Mathf.Min(MAX_SAFE_POSE_STEP, remainingDistance));
                    pivotPosition += direction * step;
                    movedDistance += step;
                    probeObject.transform.position = pivotPosition;
                    Physics.SyncTransforms();
                    moved = true;
                    break;
                }

                if (!moved)
                {
                    break;
                }
            }

            probeCollider.enabled = false;
            Destroy(probeObject);
        }

        [ClientRpc]
        private void RevealClientVisualProjectileClientRpc(
            ulong shooterClientId,
            uint shotId,
            Vector3 pivotPosition,
            Quaternion rotation)
        {
            transform.SetPositionAndRotation(pivotPosition, rotation);
            ApplyProjectilePresentation(false);
            ProjectileVisualPool.ReturnByShot(shooterClientId, shotId);
        }

        private void TryBindHeldVisualToHolder()
        {
            if (!IsSpawned || IsOwner || hasBoundHeldVisual || State != GunItemState.Held)
            {
                return;
            }

            NetworkObject playerObject = GetPlayerObject(holderId.Value);
            if (playerObject == null)
            {
                return;
            }

            Transform hand = GetHandAnchor(playerObject);
            if (hand == null)
            {
                return;
            }

            GunHoldPose.GetCenteredFpPose(
                hand.position,
                hand.rotation,
                pickupPositionOffset,
                pickupRotationEuler,
                0f,
                localBounds.center,
                pickupScale,
                out Vector3 worldPosition,
                out Quaternion worldRotation);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            hasBoundHeldVisual = true;
        }

        private void LateUpdate()
        {
            if (!IsSpawned || !IsOwner || State != GunItemState.Held)
            {
                return;
            }

            NetworkObject playerObject = GetPlayerObject(holderId.Value);
            if (playerObject == null)
            {
                return;
            }

            Transform hand = GetHandAnchor(playerObject);
            if (hand == null)
            {
                return;
            }

            recoilAmount = Mathf.MoveTowards(recoilAmount, 0f, Time.deltaTime * RECOIL_RECOVERY_SPEED);
            recoilPitchAmount = Mathf.MoveTowards(recoilPitchAmount, 0f, Time.deltaTime * RECOIL_PITCH_RECOVERY_SPEED);
            reloadMotionTime = isReloading.Value ? reloadMotionTime + Time.deltaTime : 0f;

            Vector3 motionOffset = Vector3.back * recoilAmount;
            GunHoldPose.GetCenteredFpPose(
                hand.position,
                hand.rotation,
                pickupPositionOffset,
                pickupRotationEuler,
                recoilPitchAmount,
                localBounds.center,
                pickupScale,
                out Vector3 worldPosition,
                out Quaternion worldRotation);

            if (isReloading.Value && reloadDuration > 0f)
            {
                float t = Mathf.Clamp01(reloadMotionTime / reloadDuration);
                float dip = Mathf.Sin(t * Mathf.PI);
                float sideSway = Mathf.Sin(t * Mathf.PI * 2f);
                worldPosition += hand.rotation * (Vector3.down * (dip * RELOAD_DIP_DISTANCE) + Vector3.right * (sideSway * RELOAD_SIDE_DISTANCE));
                worldRotation *= Quaternion.Euler(RELOAD_DIP_ANGLE * dip, -RELOAD_SIDE_ANGLE * sideSway, RELOAD_ROLL_ANGLE * dip);
            }

            worldPosition += hand.rotation * motionOffset;

            if (isDrawing)
            {
                float drawDuration = Mathf.Max(0.01f, SwapDrawDuration);
                float drawProgress = Mathf.Clamp01((Time.time - drawStartTime) / drawDuration);
                float easedProgress = 1f - Mathf.Pow(1f - drawProgress, 3f);
                float lowerAmount = 1f - easedProgress;
                worldPosition += hand.rotation * (
                    Vector3.down * (DRAW_LOWER_DISTANCE * lowerAmount) +
                    Vector3.back * (DRAW_LOWER_DISTANCE * 0.2f * lowerAmount));
                worldRotation *= Quaternion.Euler(
                    DRAW_PITCH_ANGLE * lowerAmount,
                    0f,
                    DRAW_ROLL_ANGLE * lowerAmount);

                if (drawProgress >= 1f)
                {
                    isDrawing = false;
                }
            }

            transform.SetPositionAndRotation(worldPosition, worldRotation);
        }

        private bool IsCurrentGunGameThrowable(ulong clientId)
        {
            if (!fireAsSelfProjectile)
            {
                return false;
            }

            GameModeController controller = GameModeController.Instance;
            return controller != null &&
                   controller.IsCurrentGunGameWeapon(clientId, NetworkObject.PrefabIdHash);
        }

        private void TryPickUp(ulong clientId, Vector3 pickupPoint)
        {
            if (!IsServer || State != GunItemState.World || !GameModeController.IsGameplayActive)
            {
                return;
            }

            bool isCurrentGunGameThrowable = IsStrictGunGameActive && IsCurrentGunGameThrowable(clientId);
            if (IsStrictGunGameActive && !isCurrentGunGameThrowable)
            {
                return;
            }

            if (projectileBody != null && projectileBody.IsRetiring)
            {
                return;
            }

            NetworkObject playerObject = GetPlayerObject(clientId);
            if (playerObject == null)
            {
                return;
            }

            Transform playerTransform = playerObject.transform;
            if (!IsWithinPickupRange(playerTransform.position))
            {
                return;
            }

            Vector3 targetPoint = GetValidatedPickupPoint(playerTransform, pickupPoint);
            if (!isCurrentGunGameThrowable && !HasLineOfSight(playerTransform, targetPoint))
            {
                return;
            }

            if (heldItems.TryGetValue(clientId, out GunItem currentItem))
            {
                if (currentItem == this)
                {
                    return;
                }

                if (currentItem.State != GunItemState.Held || currentItem.HolderId != clientId)
                {
                    return;
                }

                currentItem.ReleaseHeldItem(clientId);
            }

            if (projectileBody != null)
            {
                projectileBody.NotifyPickedUpServer();
            }

            ClearOutstandingThrownItemFor(this);

            holderId.Value = clientId;
            state.Value = (int)GunItemState.Held;
            heldItems[clientId] = this;
            if (fireAsSelfProjectile)
            {
                currentAmmo.Value = SELF_PROJECTILE_PICKUP_AMMO;
            }

            if (isCurrentGunGameThrowable)
            {
                nextFireTime = Time.time + SwapDrawDuration;
            }

            NetworkObject.ChangeOwnership(clientId);
        }

        private void TryDrop(ulong clientId, Vector3 heldPosition, Quaternion heldRotation)
        {
            if (!IsServer || State != GunItemState.Held || holderId.Value != clientId || IsStrictGunGameActive)
            {
                return;
            }

            ReleaseHeldItem(clientId, heldPosition, heldRotation);
        }

        private void ReleaseHeldItem(ulong clientId)
        {
            ReleaseHeldItem(clientId, transform.position, transform.rotation);
        }

        private void ReleaseHeldItem(ulong clientId, Vector3 heldPosition, Quaternion heldRotation)
        {
            if (!IsFinite(heldPosition) || !IsFinite(heldRotation) ||
                (heldPosition - transform.position).sqrMagnitude > DROP_POSE_MAX_DISTANCE * DROP_POSE_MAX_DISTANCE)
            {
                heldPosition = transform.position;
                heldRotation = transform.rotation;
            }

            Vector3 muzzlePosition = heldPosition + heldRotation * Vector3.Scale(muzzleOffset, pickupScale);
            Quaternion releaseRotation = heldRotation;
            Vector3 releasePosition = GetReleasePosition(muzzlePosition, releaseRotation);
            ApplySafeLargeDropPose(ref releasePosition, ref releaseRotation, clientId);

            CancelReload();
            StopBurstCoroutine();

            holderId.Value = NO_HOLDER;
            state.Value = (int)GunItemState.World;

            if (heldItems.TryGetValue(clientId, out GunItem item) && item == this)
            {
                heldItems.Remove(clientId);
            }

            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            transform.SetPositionAndRotation(releasePosition, releaseRotation);
            transform.localScale = originalWorldScale;
            ApplyReleaseImpulses(clientId);
        }

        public void TossHeldItemServer(Vector3 forwardDirection)
        {
            if (!IsServer || State != GunItemState.Held || holderId.Value == NO_HOLDER)
            {
                return;
            }

            ulong clientId = holderId.Value;
            NetworkObject playerObject = GetPlayerObject(clientId);
            Vector3 direction = forwardDirection.sqrMagnitude > MIN_FIRE_DIRECTION_SQR
                ? forwardDirection
                : (playerObject != null ? playerObject.transform.forward : transform.forward);
            direction.y = 0f;

            if (direction.sqrMagnitude < MIN_FIRE_DIRECTION_SQR)
            {
                direction = playerObject != null ? playerObject.transform.forward : Vector3.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude < MIN_FIRE_DIRECTION_SQR)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();

            ReleaseHeldItem(clientId, transform.position, transform.rotation);

            if (rigidBody == null)
            {
                return;
            }

            rigidBody.WakeUp();
            rigidBody.linearVelocity = direction * TOSS_FORWARD_SPEED + Vector3.up * TOSS_UP_SPEED;
            rigidBody.angularVelocity = new Vector3(
                UnityEngine.Random.Range(-TOSS_SPIN_SPEED, TOSS_SPIN_SPEED),
                UnityEngine.Random.Range(-TOSS_SPIN_SPEED, TOSS_SPIN_SPEED),
                UnityEngine.Random.Range(-TOSS_SPIN_SPEED, TOSS_SPIN_SPEED));
        }

        private void ApplyReleaseImpulses(ulong releasingClientId)
        {
            if (releaseContentImpulse <= 0f && releasePlayerLaunchSpeed <= 0f)
            {
                return;
            }

            Vector3 worldCenter = transform.TransformPoint(localBounds.center);
            Vector3 worldHalfExtents = Vector3.Scale(localBounds.extents, originalWorldScale);
            worldHalfExtents = Vector3.Max(worldHalfExtents, Vector3.one * MIN_RELEASE_BOUNDS_EXTENT);

            Collider[] overlaps = Physics.OverlapBox(
                worldCenter,
                worldHalfExtents,
                transform.rotation,
                ~0,
                QueryTriggerInteraction.Collide);

            HashSet<ulong> launchedPlayers = new HashSet<ulong>();
            HashSet<Rigidbody> pushedBodies = new HashSet<Rigidbody>();

            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                PlayerHealth playerHealth = overlap.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    TryLaunchPlayer(playerHealth, releasingClientId, worldCenter, launchedPlayers);
                    continue;
                }

                if (releaseContentImpulse <= 0f)
                {
                    continue;
                }

                Rigidbody otherRigidbody = overlap.attachedRigidbody;
                if (otherRigidbody == null || otherRigidbody == rigidBody || !pushedBodies.Add(otherRigidbody))
                {
                    continue;
                }

                Vector3 pushDirection = overlap.bounds.center - worldCenter;
                pushDirection.y = 0f;
                pushDirection = pushDirection.sqrMagnitude > MIN_FIRE_DIRECTION_SQR
                    ? pushDirection.normalized
                    : transform.forward;

                Vector3 impulse = pushDirection * releaseContentImpulse +
                                  Vector3.up * (releaseContentImpulse * RELEASE_UPWARD_IMPULSE_SCALE);
                otherRigidbody.AddForce(impulse, ForceMode.Impulse);
            }
        }

        private void TryLaunchPlayer(
            PlayerHealth playerHealth,
            ulong releasingClientId,
            Vector3 itemCenter,
            HashSet<ulong> launchedPlayers)
        {
            if (releasePlayerLaunchSpeed <= 0f || playerHealth.IsDead)
            {
                return;
            }

            ulong playerClientId = playerHealth.OwnerClientId;
            if (playerClientId == releasingClientId || !launchedPlayers.Add(playerClientId))
            {
                return;
            }

            PlayerController controller = playerHealth.GetComponent<PlayerController>();
            if (controller == null)
            {
                return;
            }

            Vector3 launchDirection = playerHealth.transform.position - itemCenter;
            launchDirection.y = 0f;
            launchDirection = launchDirection.sqrMagnitude > MIN_FIRE_DIRECTION_SQR
                ? launchDirection.normalized
                : transform.forward;

            Vector3 launchVelocity = launchDirection * releasePlayerLaunchSpeed +
                                     Vector3.up * releasePlayerLaunchUpSpeed;
            controller.ApplyLaunch(launchVelocity);
        }

        public void GetProjectileSpawnPose(
            Vector3 muzzlePosition,
            Vector3 fireDirection,
            out Vector3 spawnPosition,
            out Quaternion spawnRotation)
        {
            Vector3 direction = fireDirection.sqrMagnitude > MIN_FIRE_DIRECTION_SQR
                ? fireDirection.normalized
                : transform.forward;
            Vector3 finalUp = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f
                ? transform.up
                : Vector3.up;
            spawnRotation = Quaternion.LookRotation(direction, finalUp);
            spawnPosition = GetReleasePosition(muzzlePosition, spawnRotation);
        }

        private Vector3 GetReleasePosition(Vector3 muzzlePosition, Quaternion releaseRotation)
        {
            Vector3 rearFaceCenter = new Vector3(localBounds.center.x, localBounds.center.y, localBounds.min.z);
            Vector3 scaledRearFaceCenter = Vector3.Scale(rearFaceCenter, originalWorldScale);
            return muzzlePosition - releaseRotation * scaledRearFaceCenter;
        }


        private void ApplySafeLargeDropPose(ref Vector3 releasePosition, ref Quaternion releaseRotation, ulong clientId)
        {
            if (localBounds.size.z * originalWorldScale.z < LARGE_DROP_MIN_LENGTH)
            {
                return;
            }

            NetworkObject playerObject = GetPlayerObject(clientId);
            if (playerObject == null)
            {
                return;
            }

            Vector3 playerPosition = playerObject.transform.position;
            Bounds releaseBounds = CalculateWorldBounds(releasePosition, releaseRotation);
            bool clipsFloor = releaseBounds.min.y < playerPosition.y + DROP_FLOOR_MARGIN;
            bool clipsCeiling = releaseBounds.max.y > DROP_MAX_TOP;

            if (!clipsFloor && !clipsCeiling)
            {
                return;
            }

            Vector3 horizontalForward = releaseRotation * Vector3.forward;
            horizontalForward.y = 0f;

            if (horizontalForward.sqrMagnitude < MIN_FIRE_DIRECTION_SQR)
            {
                horizontalForward = playerObject.transform.forward;
                horizontalForward.y = 0f;
            }

            if (horizontalForward.sqrMagnitude < MIN_FIRE_DIRECTION_SQR)
            {
                horizontalForward = Vector3.forward;
            }

            horizontalForward.Normalize();

            Quaternion levelRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);
            Vector3 scaledExtents = Vector3.Scale(localBounds.extents, originalWorldScale);
            Vector3 scaledCenter = Vector3.Scale(localBounds.center, originalWorldScale);
            float desiredCenterY = Mathf.Min(
                playerPosition.y + scaledExtents.y + DROP_FLOOR_CLEARANCE,
                DROP_MAX_TOP - scaledExtents.y);
            Vector3 desiredCenter = playerPosition + horizontalForward * (scaledExtents.z + DROP_REAR_CLEARANCE);
            desiredCenter.y = desiredCenterY;

            releasePosition = desiredCenter - levelRotation * scaledCenter;
            releaseRotation = levelRotation;
        }

        private Bounds CalculateWorldBounds(Vector3 position, Quaternion rotation)
        {
            Vector3 scaledCenter = Vector3.Scale(localBounds.center, originalWorldScale);
            Vector3 scaledExtents = Vector3.Scale(localBounds.extents, originalWorldScale);
            Bounds bounds = new Bounds(position + rotation * scaledCenter, Vector3.zero);

            for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
            {
                Vector3 corner = scaledCenter + new Vector3(
                    (cornerIndex & 1) == 0 ? -scaledExtents.x : scaledExtents.x,
                    (cornerIndex & 2) == 0 ? -scaledExtents.y : scaledExtents.y,
                    (cornerIndex & 4) == 0 ? -scaledExtents.z : scaledExtents.z);
                bounds.Encapsulate(position + rotation * corner);
            }

            return bounds;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private Vector3 GetValidatedPickupPoint(Transform playerTransform, Vector3 pickupPoint)
        {
            Bounds bounds = GetWorldBounds();
            Vector3 closestPoint = bounds.ClosestPoint(pickupPoint);
            if ((closestPoint - pickupPoint).sqrMagnitude <= PICKUP_POINT_TOLERANCE * PICKUP_POINT_TOLERANCE)
            {
                return pickupPoint;
            }

            Vector3 eye = playerTransform.position + Vector3.up * EYE_HEIGHT;
            return bounds.ClosestPoint(eye);
        }

        private bool HasLineOfSight(Transform playerTransform, Vector3 target)
        {
            Vector3 eye = playerTransform.position + Vector3.up * EYE_HEIGHT;
            Vector3 direction = target - eye;
            float distance = direction.magnitude;

            if (distance <= RAYCAST_SKIN)
            {
                return true;
            }

            Vector3 origin = eye + direction.normalized * RAYCAST_SKIN;
            float remaining = distance - RAYCAST_SKIN;

            int lineOfSightMask = Physics.DefaultRaycastLayers & ~(1 << WorldHullLayerIndex);
            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, remaining, lineOfSightMask))
            {
                GunItem hitItem = hit.collider.GetComponentInParent<GunItem>();
                return hitItem == this;
            }

            return true;
        }

        private Transform GetHandAnchor(NetworkObject playerObject)
        {
            PlayerController controller = playerObject.GetComponent<PlayerController>();
            return controller != null ? controller.HandAnchor : null;
        }

        private Bounds GetWorldBounds()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
            }

            return bounds;
        }

        private Bounds CalculateLocalBounds()
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                EncapsulateColliderBounds(collider, ref bounds, ref hasBounds);
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
        }

        private void EncapsulateColliderBounds(Collider collider, ref Bounds bounds, ref bool hasBounds)
        {
            if (TryGetColliderLocalBox(collider, out Vector3 center, out Vector3 size))
            {
                EncapsulateLocalBox(collider.transform, center, size, ref bounds, ref hasBounds);
                return;
            }

            Bounds worldBounds = collider.bounds;
            EncapsulateLocalBox(null, worldBounds.center, worldBounds.size, ref bounds, ref hasBounds);
        }

        private static bool TryGetColliderLocalBox(Collider collider, out Vector3 center, out Vector3 size)
        {
            switch (collider)
            {
                case BoxCollider box:
                    center = box.center;
                    size = box.size;
                    return true;
                case SphereCollider sphere:
                    center = sphere.center;
                    size = Vector3.one * (sphere.radius * 2f);
                    return true;
                case CapsuleCollider capsule:
                    center = capsule.center;
                    size = GetCapsuleSize(capsule);
                    return true;
                case MeshCollider meshCollider when meshCollider.sharedMesh != null:
                    center = meshCollider.sharedMesh.bounds.center;
                    size = meshCollider.sharedMesh.bounds.size;
                    return true;
                default:
                    center = Vector3.zero;
                    size = Vector3.zero;
                    return false;
            }
        }

        private static Vector3 GetCapsuleSize(CapsuleCollider capsule)
        {
            float diameter = capsule.radius * 2f;
            return capsule.direction switch
            {
                0 => new Vector3(capsule.height, diameter, diameter),
                1 => new Vector3(diameter, capsule.height, diameter),
                _ => new Vector3(diameter, diameter, capsule.height)
            };
        }

        private void EncapsulateLocalBox(
            Transform source,
            Vector3 center,
            Vector3 size,
            ref Bounds bounds,
            ref bool hasBounds)
        {
            Vector3 extents = size * 0.5f;
            for (int cornerIndex = 0; cornerIndex < BOUNDS_CORNER_COUNT; cornerIndex++)
            {
                Vector3 localCorner = center + new Vector3(
                    (cornerIndex & 1) == 0 ? -extents.x : extents.x,
                    (cornerIndex & 2) == 0 ? -extents.y : extents.y,
                    (cornerIndex & 4) == 0 ? -extents.z : extents.z);
                Vector3 worldCorner = source != null ? source.TransformPoint(localCorner) : localCorner;
                Vector3 rootLocalCorner = transform.InverseTransformPoint(worldCorner);

                if (!hasBounds)
                {
                    bounds = new Bounds(rootLocalCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rootLocalCorner);
                }
            }
        }

        public static void DropHeldItem(ulong clientId)
        {
            if (heldItems.TryGetValue(clientId, out GunItem item))
            {
                item.ReleaseHeldItem(clientId);
            }
        }

        public static void DespawnHeldItem(ulong clientId)
        {
            if (heldItems.TryGetValue(clientId, out GunItem item) && item != null)
            {
                item.DespawnHeldItemServer(clientId);
            }
        }

        public static bool HasHeldItem(ulong clientId)
        {
            return heldItems.TryGetValue(clientId, out GunItem item) &&
                   item != null &&
                   item.State == GunItemState.Held;
        }

        public static bool HasOutstandingThrownItem(ulong clientId)
        {
            if (!outstandingThrows.TryGetValue(clientId, out OutstandingThrow outstanding))
            {
                return false;
            }

            bool isTimedOut = Time.time - outstanding.RegisteredAt > THROWABLE_RECLAIM_TIMEOUT_SECONDS;
            if (outstanding.Item == null || isTimedOut)
            {
                outstandingThrows.Remove(clientId);
                return false;
            }

            return true;
        }

        public static void ClearOutstandingThrownItem(ulong clientId)
        {
            outstandingThrows.Remove(clientId);
        }

        private static void ClearOutstandingThrownItemFor(GunItem item)
        {
            ulong throwerClientId = NO_HOLDER;
            foreach (KeyValuePair<ulong, OutstandingThrow> pair in outstandingThrows)
            {
                if (pair.Value.Item == item)
                {
                    throwerClientId = pair.Key;
                    break;
                }
            }

            if (throwerClientId != NO_HOLDER)
            {
                outstandingThrows.Remove(throwerClientId);
            }
        }

        public static void HandlePlayerRemoved(ulong clientId)
        {
            ClearOutstandingThrownItem(clientId);

            if (IsStrictGunGameActive)
            {
                DespawnHeldItem(clientId);
                return;
            }

            DropHeldItem(clientId);
        }

        public static bool GrantToPlayerServer(ulong clientId, GameObject weaponPrefab)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null ||
                !networkManager.IsServer ||
                weaponPrefab == null ||
                !weaponPrefab.TryGetComponent(out GunItem _) ||
                !weaponPrefab.TryGetComponent(out NetworkObject _))
            {
                return false;
            }

            NetworkObject playerObject = networkManager.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObject == null)
            {
                return false;
            }

            if (playerObject.TryGetComponent(out PlayerHealth playerHealth) && playerHealth.IsDead)
            {
                return false;
            }

            DespawnHeldItem(clientId);

            Transform hand = playerObject.TryGetComponent(out PlayerController playerController)
                ? playerController.HandAnchor
                : null;
            Vector3 spawnPosition = hand != null ? hand.position : playerObject.transform.position + Vector3.up;
            Quaternion spawnRotation = hand != null ? hand.rotation : playerObject.transform.rotation;

            GameObject instance = UnityEngine.Object.Instantiate(weaponPrefab, spawnPosition, spawnRotation);
            if (!instance.TryGetComponent(out NetworkObject networkObject) ||
                !instance.TryGetComponent(out GunItem item))
            {
                UnityEngine.Object.Destroy(instance);
                return false;
            }

            networkObject.Spawn(destroyWithScene: true);
            if (!networkObject.IsSpawned)
            {
                UnityEngine.Object.Destroy(instance);
                return false;
            }

            item.GrantHeldServer(clientId);
            return true;
        }

        private void GrantHeldServer(ulong clientId)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            CancelReload();
            currentAmmo.Value = magazineSize;
            state.Value = (int)GunItemState.Held;
            holderId.Value = clientId;
            heldItems[clientId] = this;
            nextFireTime = Time.time + SwapDrawDuration;
            NetworkObject.ChangeOwnership(clientId);
        }

        private void DespawnHeldItemServer(ulong clientId)
        {
            if (!IsServer || holderId.Value != clientId)
            {
                return;
            }

            CancelReload();
            ClearOutstandingThrownItem(clientId);
            ClearOutstandingThrownItemFor(this);
            heldItems.Remove(clientId);
            holderId.Value = NO_HOLDER;

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
                return;
            }

            UnityEngine.Object.Destroy(gameObject);
        }

        private static bool IsStrictGunGameActive =>
            (GameModeController.Instance != null && GameModeController.Instance.Mode == GameModeType.GunGame) ||
            (GameModeController.Instance == null &&
             MultiplayerSessionManager.Instance != null &&
             MultiplayerSessionManager.Instance.SelectedGameMode == GameModeType.GunGame);

        internal void NotifyRetiringServer()
        {
            ClearOutstandingThrownItemFor(this);
        }

        internal static void ResetHeldItems()
        {
            heldItems.Clear();
            outstandingThrows.Clear();
        }

        private GameObject GetCachedProjectilePrefab(NetworkManager networkManager, uint prefabHash)
        {
            if (cachedProjectilePrefab != null && cachedProjectilePrefabHash == prefabHash)
            {
                return cachedProjectilePrefab;
            }

            GameObject prefab = FindRegisteredPrefab(networkManager, prefabHash);
            if (prefab != null)
            {
                cachedProjectilePrefab = prefab;
                cachedProjectilePrefabHash = prefabHash;
            }

            return prefab;
        }

        internal static GameObject FindRegisteredPrefab(NetworkManager networkManager, uint prefabHash)
        {
            if (prefabHash == 0)
            {
                return null;
            }

            foreach (NetworkPrefab networkPrefab in networkManager.NetworkConfig.Prefabs.Prefabs)
            {
                if (networkPrefab.Prefab == null || networkPrefab.Override != NetworkPrefabOverride.None)
                {
                    continue;
                }

                if (!networkPrefab.Prefab.TryGetComponent(out NetworkObject networkObject))
                {
                    continue;
                }

                if (networkObject.PrefabIdHash == prefabHash)
                {
                    return networkPrefab.Prefab;
                }
            }

            return null;
        }

        private NetworkManager GetNetworkManager()
        {
            return NetworkObject != null ? NetworkObject.NetworkManager : null;
        }

        private NetworkObject GetPlayerObject(ulong clientId)
        {
            NetworkManager networkManager = GetNetworkManager();
            if (networkManager == null || networkManager.SpawnManager == null)
            {
                return null;
            }

            return networkManager.SpawnManager.GetPlayerNetworkObject(clientId);
        }

        private void ApplyStateScale()
        {
            transform.localScale = State == GunItemState.Held ? pickupScale : originalWorldScale;
        }

        private void WakeSupportedRigidbodies()
        {
            Bounds bounds = GetWorldBounds();
            bounds.Expand(WAKE_BOUNDS_MARGIN);

            Collider[] overlaps = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                Rigidbody otherRigidbody = overlap.attachedRigidbody;
                if (otherRigidbody == null || otherRigidbody == rigidBody || otherRigidbody.isKinematic)
                {
                    continue;
                }

                otherRigidbody.WakeUp();
            }
        }

        private void SetPhysicsEnabled(GunItemState itemState)
        {
            bool isHeld = itemState == GunItemState.Held;
            bool isProjectile = itemState == GunItemState.Projectile;
            bool isRetiring = projectileBody != null && projectileBody.IsRetiring;
            bool isHidden = isProjectileHidden.Value;
            bool collidersEnabled = !isHeld && !isRetiring && !isHidden;
            bool hasPartCollider = HasPartCollider();
            int enabledColliderCount = 0;

            EnsureProjectileLayerCollisions();
            if (!isHeld)
            {
                ApplyColliderLayers(itemState);
            }

            if (isHeld)
            {
                hasBeenPickedUp = true;
            }

            if (IsServer && isHeld)
            {
                WakeSupportedRigidbodies();
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider == null)
                {
                    continue;
                }

                bool isProjectileHull = collider == projectileHullCollider;
                bool enableHull = isProjectile || itemState == GunItemState.World || !hasPartCollider;
                collider.enabled = collidersEnabled && (!isProjectileHull || enableHull);
                if (collider.enabled)
                {
                    enabledColliderCount++;
                }
            }

            if (rigidBody == null)
            {
                return;
            }

            bool fixedWorld = fixedInWorld && itemState == GunItemState.World && !hasBeenPickedUp;
            bool serverSimulatesPhysics = IsServer && !isHeld && !fixedWorld && !isHidden;
            if (IsServer && itemState == GunItemState.World)
            {
                rigidBody.sleepThreshold = WORLD_SLEEP_THRESHOLD;
            }

            rigidBody.isKinematic = !serverSimulatesPhysics;
            rigidBody.useGravity = true;

            if (fireAsSelfProjectile)
            {
                ProjectileTrace.Log(
                    "self physics t=" + Time.time.ToString("F3") +
                    " item=" + name +
                    " state=" + itemState +
                    " hidden=" + isHidden +
                    " colliders=" + enabledColliderCount +
                    " kinematic=" + rigidBody.isKinematic +
                    " pos=" + transform.position.ToString("F2"));
            }
        }

        private bool HasPartCollider()
        {
            foreach (Collider collider in colliders)
            {
                if (collider != null && collider != projectileHullCollider)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyColliderLayers(GunItemState itemState)
        {
            bool useWorldHull = itemState == GunItemState.World && IsLargeWorldHull();
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider == null)
                {
                    continue;
                }

                collider.gameObject.layer = collider == projectileHullCollider && useWorldHull
                    ? WorldHullLayerIndex
                    : GUN_PROP_LAYER_INDEX;
            }
        }

        private bool IsLargeWorldHull()
        {
            Vector3 extents = localBounds.extents;
            return fireAsSelfProjectile &&
                   Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z)) >= LARGE_WORLD_HULL_EXTENT;
        }
    }
}
