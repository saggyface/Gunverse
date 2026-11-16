using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GunVerse
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class GunProjectileBody : NetworkBehaviour
    {
        private const float DEFAULT_PUSH_INTERVAL = 0.2f;
        private const float RETIRE_DURATION = 0.35f;
        private const float MIN_SWEEP_SPEED = 4f;
        private const float WORLD_PROP_IMPULSE_SCALE = 0.65f;
        private const float WORLD_PROP_MAX_DELTA_V = 6f;
        private const float SWEEP_SKIN = 0.05f;
        private const float MIN_DIRECTION_SQR = 0.0001f;
        private const float MIN_LIFETIME = 0.1f;
        private const float MIN_TRAIL_LENGTH = 4.5f;
        private const float MAX_TRAIL_LENGTH = 12f;
        private const float MAX_SPENT_SETTLE_SECONDS = 3f;
        private const float HEAVY_PUSH_SCALE = 1.5f;
        private const float MIN_DAMAGE_SPEED_RATIO = 0.2f;
        private const float PLAYER_HIT_COOLDOWN = 0.1f;
        private const int SERVER_TRACE_FRAMES = 10;
        private const int CLIENT_TRACE_FRAMES = 30;
        private const float CLIENT_TRACE_DURATION = 0.5f;

        private readonly struct IgnoredColliderPair : IEquatable<IgnoredColliderPair>
        {
            private readonly Collider first;
            private readonly Collider second;

            public IgnoredColliderPair(Collider first, Collider second)
            {
                this.first = first;
                this.second = second;
            }

            public Collider First => first;
            public Collider Second => second;

            public bool Equals(IgnoredColliderPair other)
            {
                return (first == other.first && second == other.second) ||
                       (first == other.second && second == other.first);
            }

            public override bool Equals(object other)
            {
                return other is IgnoredColliderPair pair && Equals(pair);
            }

            public override int GetHashCode()
            {
                int firstHash = first != null ? first.GetHashCode() : 0;
                int secondHash = second != null ? second.GetHashCode() : 0;
                return firstHash ^ secondHash;
            }
        }

        [Header("Physics")]
        [SerializeField] private float mass = 1f;
        [Tooltip("Multiplier applied to gravity while in flight. 1 = normal gravity.")]
        [SerializeField, Min(0f)] private float gravityScale = 1f;

        [Header("Limits")]
        [SerializeField] private int maxActiveProjectiles = 30;

        [Header("Player Impact")]
        [SerializeField] private float playerPushSpeed = 4f;
        [SerializeField] private float playerPushUpSpeed = 2f;

        [Header("Spent Pickup")]
        [SerializeField] private float slowPickupSpeed = 1.5f;
        [SerializeField] private float slowPickupDelay = 0.25f;

        [Header("Trail")]
        [SerializeField] private float trailWidthScale = 1f;
        [SerializeField] private float trailLengthMultiplier = 20f;

        private readonly NetworkVariable<ulong> networkShooterClientId = new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> isSpent = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> isRetiring = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<Vector3> networkLaunchVelocity = new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly HashSet<IgnoredColliderPair> ignoredColliderPairs = new HashSet<IgnoredColliderPair>();
        private readonly HashSet<ulong> damagedPlayers = new HashSet<ulong>();
        private readonly Dictionary<ulong, float> lastPushTimes = new Dictionary<ulong, float>();
        private readonly Dictionary<ulong, float> lastDamageTimes = new Dictionary<ulong, float>();

        private GunItem gunItem;
        private Rigidbody rigidBody;
        private Collider[] projectileColliders;
        private TrailRenderer trail;
        private ulong shooterClientId;
        private float launchSpeed;
        private float projectileRadius;
        private bool historyExpired;
        private float currentSpeed;
        private float remainingLifetime;
        private float slowTimer;
        private float spentAtTime;
        private bool preparedForSpawn;
        private bool finished;
        private bool playerDamageEnabled = true;
        private Coroutine clientRetireCoroutine;
        private int serverTraceFrames;
        private int clientTraceFrames;
        private bool clientTraceStarted;
        private float clientTraceUntil;
        private bool projectileNetworkTuningApplied;

        public ulong ShooterClientId => IsServer ? shooterClientId : networkShooterClientId.Value;
        public int ShotGroupId { get; private set; }
        public uint TypeHash { get; private set; }
        public GunItem Item => gunItem;
        public float ProjectileRadius => projectileRadius;
        public Vector3 LaunchVelocity => networkLaunchVelocity.Value;
        public int MaxActiveProjectiles => maxActiveProjectiles;
        public float GravityScale => gravityScale <= 0.01f ? 1f : gravityScale;
        public float TrailWidthScale => trailWidthScale;
        public float TrailLengthMultiplier => trailLengthMultiplier;
        public bool IsSpent => isSpent.Value;
        public bool IsRetiring => isRetiring.Value;
        public bool IsActiveProjectile => preparedForSpawn && !finished && !isSpent.Value && !isRetiring.Value;

        public void PrepareServerSpawn(ulong shooter, int shotGroupId, float initialLaunchSpeed)
        {
            shooterClientId = shooter;
            ShotGroupId = shotGroupId;
            launchSpeed = Mathf.Max(0.01f, initialLaunchSpeed);
            currentSpeed = launchSpeed;
            preparedForSpawn = true;
        }

        public void BeginSelfProjectileServer(ulong shooter, int shotGroupId, float initialLaunchSpeed)
        {
            if (!IsServer)
            {
                return;
            }

            shooterClientId = shooter;
            networkShooterClientId.Value = shooter;
            ShotGroupId = shotGroupId;
            launchSpeed = Mathf.Max(0.01f, initialLaunchSpeed);
            currentSpeed = launchSpeed;
            remainingLifetime = Mathf.Max(MIN_LIFETIME, gunItem.ProjectileLifetime);
            slowTimer = 0f;
            preparedForSpawn = true;
            finished = false;
            CalculateProjectileRadius();
            RegisterProjectileHistory();

            if (isSpent.Value)
            {
                isSpent.Value = false;
            }

            if (rigidBody == null)
            {
                rigidBody = GetComponent<Rigidbody>();
            }

            if (rigidBody != null)
            {
                rigidBody.mass = mass;
                rigidBody.useGravity = true;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            if (projectileColliders == null)
            {
                projectileColliders = GetComponentsInChildren<Collider>(true);
            }

            GunProjectileRegistry.Register(this);
            IgnoreShooterCollisions();
            GunProjectileRegistry.IgnoreCollisionsFor(this);
        }

        public override void OnNetworkSpawn()
        {
            gunItem = GetComponent<GunItem>();
            rigidBody = GetComponent<Rigidbody>();
            projectileColliders = GetComponentsInChildren<Collider>(true);
            CalculateProjectileRadius();
            TypeHash = gunItem != null && gunItem.ProjectilePrefabHash != 0
                ? gunItem.ProjectilePrefabHash
                : NetworkObject.PrefabIdHash;

            bool clientProjectileTrackingNeeded = gunItem != null &&
                (gunItem.State == GunItemState.Projectile || gunItem.FireAsSelfProjectile);
            if (IsClient && clientProjectileTrackingNeeded &&
                GetComponent<ClientProjectileHitDetector>() == null)
            {
                gameObject.AddComponent<ClientProjectileHitDetector>();
            }

            if (IsServer &&
                (preparedForSpawn || (gunItem != null && gunItem.State == GunItemState.Projectile)))
            {
                RegisterProjectileHistory();
            }

            if (IsServer && preparedForSpawn)
            {
                networkShooterClientId.Value = shooterClientId;
            }

            networkShooterClientId.OnValueChanged += OnNetworkShooterChanged;
            isSpent.OnValueChanged += OnSpentChanged;
            isRetiring.OnValueChanged += OnRetiringChanged;

            if (IsClient && isRetiring.Value)
            {
                DisableColliders();
                StartClientRetireShrink();
            }

            LogNetworkSpawn();
            ApplyProjectileNetworkTuning();

            if (preparedForSpawn || (gunItem != null && gunItem.State == GunItemState.Projectile))
            {
                IgnoreShooterCollisions();
            }

            if (!IsServer || !preparedForSpawn)
            {
                return;
            }

            rigidBody.mass = mass;
            rigidBody.useGravity = true;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            remainingLifetime = Mathf.Max(MIN_LIFETIME, gunItem.ProjectileLifetime);

            GunProjectileRegistry.Register(this);
            GunProjectileRegistry.IgnoreCollisionsFor(this);
        }

        private void CalculateProjectileRadius()
        {
            Bounds bounds = gunItem != null ? gunItem.LocalBounds : new Bounds(Vector3.zero, Vector3.zero);
            Vector3 extents = bounds.extents;
            projectileRadius = Mathf.Max(
                0.05f,
                Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z)));
        }

        private void RegisterProjectileHistory()
        {
            if (!IsServer || gunItem == null)
            {
                return;
            }

            historyExpired = false;
            ServerProjectileHistory.Register(
                NetworkObjectId,
                ShooterClientId,
                gunItem.Damage,
                gunItem.HitCueType,
                projectileRadius,
                NetworkManager.ServerTime.Time,
                transform.position);
        }

        public void ReportProjectileHit(
            ulong victimClientId,
            Vector3 hitPoint,
            double projectileViewTime,
            double victimViewTime)
        {
            if (IsServer)
            {
                if (!ServerProjectileHistory.TryApplyHit(
                        NetworkObjectId,
                        ShooterClientId,
                        victimClientId,
                        hitPoint,
                        projectileViewTime,
                        victimViewTime,
                        out string rejection))
                {
                    ProjectileTrace.Log(
                        "projectile hit rejected projectile=" + NetworkObjectId +
                        " victim=" + victimClientId +
                        " reason=" + rejection);
                }

                return;
            }

            ReportProjectileHitServerRpc(
                victimClientId,
                hitPoint,
                projectileViewTime,
                victimViewTime);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ReportProjectileHitServerRpc(
            ulong victimClientId,
            Vector3 hitPoint,
            double projectileViewTime,
            double victimViewTime,
            ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId != ShooterClientId)
            {
                return;
            }

            if (!ServerProjectileHistory.TryApplyHit(
                    NetworkObjectId,
                    serverRpcParams.Receive.SenderClientId,
                    victimClientId,
                    hitPoint,
                    projectileViewTime,
                    victimViewTime,
                    out string rejection))
            {
                ProjectileTrace.Log(
                    "projectile hit rejected projectile=" + NetworkObjectId +
                    " victim=" + victimClientId +
                    " reason=" + rejection);
            }
        }

        private void LogNetworkSpawn()
        {
            NetworkTransform networkTransform = GetComponent<NetworkTransform>();
            Rigidbody body = GetComponent<Rigidbody>();
            ProjectileTrace.Log(
                "net spawn t=" + Time.time.ToString("F3") +
                " id=" + (NetworkObject != null ? NetworkObjectId : 0UL) +
                " owner=" + (NetworkObject != null ? NetworkObject.OwnerClientId : 0UL) +
                " server=" + IsServer +
                " client=" + IsClient +
                " isOwner=" + IsOwner +
                " pos=" + transform.position.ToString("F3") +
                " state=" + (gunItem != null ? gunItem.State.ToString() : "null") +
                " shooterNet=" + networkShooterClientId.Value +
                " spent=" + IsSpent +
                " retiring=" + IsRetiring +
                " kinematic=" + (body != null && body.isKinematic) +
                " interp=" + (networkTransform != null && networkTransform.Interpolate) +
                " interpType=" + (networkTransform != null ? networkTransform.PositionInterpolationType.ToString() : "null") +
                " smoothing=" + (networkTransform != null && networkTransform.PositionLerpSmoothing) +
                " maxInterp=" + (networkTransform != null ? networkTransform.PositionMaxInterpolationTime.ToString("F3") : "null"));
        }

        private void ApplyProjectileNetworkTuning()
        {
            if (projectileNetworkTuningApplied || gunItem == null)
            {
                return;
            }

            bool isProjectile = gunItem.State == GunItemState.Projectile ||
                                networkShooterClientId.Value != ulong.MaxValue;
            if (!isProjectile || !TryGetComponent(out NetworkTransform networkTransform))
            {
                return;
            }

            networkTransform.Interpolate = true;
            networkTransform.PositionInterpolationType = NetworkTransform.InterpolationTypes.Lerp;
            networkTransform.PositionLerpSmoothing = false;
            networkTransform.PositionMaxInterpolationTime = 0.05f;
            projectileNetworkTuningApplied = true;

            ProjectileTrace.Log(
                "tuned t=" + Time.time.ToString("F3") +
                " id=" + NetworkObjectId +
                " interpType=" + networkTransform.PositionInterpolationType +
                " smoothing=" + networkTransform.PositionLerpSmoothing +
                " maxInterp=" + networkTransform.PositionMaxInterpolationTime.ToString("F3"));
        }

        public override void OnNetworkDespawn()
        {
            networkShooterClientId.OnValueChanged -= OnNetworkShooterChanged;
            isSpent.OnValueChanged -= OnSpentChanged;
            isRetiring.OnValueChanged -= OnRetiringChanged;

            if (clientRetireCoroutine != null)
            {
                StopCoroutine(clientRetireCoroutine);
                clientRetireCoroutine = null;
            }

            if (IsServer)
            {
                ServerProjectileHistory.MarkExpired(
                    NetworkObjectId,
                    NetworkManager.ServerTime.Time);
                GunProjectileRegistry.Unregister(this);
            }

            RestoreIgnoredCollisions();
        }

        private void Update()
        {
            if (!IsClient || gunItem == null)
            {
                return;
            }

            if (!clientTraceStarted && gunItem.State == GunItemState.Projectile)
            {
                clientTraceStarted = true;
                clientTraceUntil = Time.time + CLIENT_TRACE_DURATION;
                ProjectileTrace.Log(
                    "client visible t=" + Time.time.ToString("F3") +
                    " id=" + NetworkObjectId +
                    " pos=" + transform.position.ToString("F3") +
                    " state=" + gunItem.State);
            }

            if (clientTraceStarted &&
                clientTraceFrames < CLIENT_TRACE_FRAMES &&
                Time.time <= clientTraceUntil)
            {
                ProjectileTrace.Log(
                    "client frame t=" + Time.time.ToString("F3") +
                    " id=" + NetworkObjectId +
                    " pos=" + transform.position.ToString("F3") +
                    " spent=" + isSpent.Value +
                    " retiring=" + isRetiring.Value);
                clientTraceFrames++;
            }

            ApplyProjectileNetworkTuning();

            bool shouldEmitTrail = gunItem.State == GunItemState.Projectile &&
                                   !isSpent.Value &&
                                   !isRetiring.Value;

            if (shouldEmitTrail && trail == null)
            {
                AttachTrail();
            }

            if (trail != null)
            {
                trail.emitting = shouldEmitTrail;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer || !preparedForSpawn || finished || isRetiring.Value)
            {
                return;
            }

            if (gunItem.State != GunItemState.Projectile)
            {
                if (!historyExpired)
                {
                    ServerProjectileHistory.MarkExpired(
                        NetworkObjectId,
                        NetworkManager.ServerTime.Time);
                    historyExpired = true;
                }

                finished = true;
                return;
            }

            ServerProjectileHistory.Record(
                NetworkObjectId,
                NetworkManager.ServerTime.Time,
                transform.position);

            float effectiveGravityScale = GravityScale;
            if (!Mathf.Approximately(effectiveGravityScale, 1f))
            {
                rigidBody.AddForce(Physics.gravity * (effectiveGravityScale - 1f), ForceMode.Acceleration);
            }

            currentSpeed = rigidBody.linearVelocity.magnitude;

            if (serverTraceFrames < SERVER_TRACE_FRAMES)
            {
                ProjectileTrace.Log(
                    "server tick t=" + Time.fixedTime.ToString("F3") +
                    " id=" + NetworkObjectId +
                    " pos=" + transform.position.ToString("F3") +
                    " vel=" + rigidBody.linearVelocity.ToString("F2") +
                    " speed=" + currentSpeed.ToString("F2") +
                    " spent=" + isSpent.Value +
                    " retiring=" + isRetiring.Value);
                serverTraceFrames++;
            }

            if (!isSpent.Value)
            {
                remainingLifetime -= Time.fixedDeltaTime;
                if (remainingLifetime <= 0f ||
                    currentSpeed <= launchSpeed * MIN_DAMAGE_SPEED_RATIO)
                {
                    ExpireToWorldServer();
                    return;
                }
            }

            if (isSpent.Value)
            {
                UpdateSpentPickup();
                return;
            }

            DetectPlayerHits();
        }

        private void DetectPlayerHits()
        {
            Vector3 velocity = rigidBody.linearVelocity;
            float speed = velocity.magnitude;
            if (speed < MIN_SWEEP_SPEED)
            {
                return;
            }

            float sweepDistance = speed * Time.fixedDeltaTime + SWEEP_SKIN;
            RaycastHit[] hits = rigidBody.SweepTestAll(velocity / speed, sweepDistance, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    HandlePlayerHit(playerHealth, hit.point, hit.normal);
                    if (isSpent.Value)
                    {
                        return;
                    }

                    continue;
                }

                if (!hit.collider.isTrigger)
                {
                    return;
                }
            }
        }

        private void UpdateSpentPickup()
        {
            if (Time.time - spentAtTime > MAX_SPENT_SETTLE_SECONDS)
            {
                FinishSpentPickup();
                return;
            }

            if (rigidBody.linearVelocity.sqrMagnitude > slowPickupSpeed * slowPickupSpeed)
            {
                slowTimer = 0f;
                return;
            }

            slowTimer += Time.fixedDeltaTime;
            if (slowTimer < slowPickupDelay)
            {
                return;
            }

            FinishSpentPickup();
        }

        private void FinishSpentPickup()
        {
            finished = true;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            GunProjectileRegistry.Unregister(this);
            RestoreIgnoredCollisions();
            gunItem.EnterWorldStateServer();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !preparedForSpawn || finished || isSpent.Value || isRetiring.Value)
            {
                return;
            }

            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }

            Vector3 contactPoint = other.ClosestPoint(transform.position);
            Vector3 contactNormal = transform.position - contactPoint;
            if (contactNormal.sqrMagnitude < MIN_DIRECTION_SQR)
            {
                contactNormal = Vector3.up;
            }

            HandlePlayerHit(playerHealth, contactPoint, contactNormal.normalized);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer || !preparedForSpawn || finished || isRetiring.Value)
            {
                return;
            }

            Collider other = collision.collider;
            if (other == null)
            {
                return;
            }

            Vector3 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Vector3 contactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector3.up;

            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                if (playerHealth.DamageTargetId != shooterClientId)
                {
                    HandlePlayerHit(playerHealth, contactPoint, contactNormal);
                }

                return;
            }

            if (TryReleaseWorldMount(other, contactPoint, contactNormal))
            {
                return;
            }

            GunProjectileBody otherProjectile = other.GetComponentInParent<GunProjectileBody>();
            if (otherProjectile != null && otherProjectile != this)
            {
                if (otherProjectile.ShotGroupId == ShotGroupId)
                {
                    IgnoreCollisionsWith(otherProjectile);
                    return;
                }

                if (otherProjectile.IsActiveProjectile)
                {
                    Spend();
                    otherProjectile.Spend();
                    ProjectileImpactClientRpc(contactPoint, contactNormal, false);
                    return;
                }
            }

            ClearDamageHistory();
        }

        private bool TryReleaseWorldMount(Collider other, Vector3 contactPoint, Vector3 contactNormal)
        {
            GunItem hitItem = other.GetComponentInParent<GunItem>();
            if (hitItem == null ||
                hitItem == gunItem ||
                !hitItem.IsFixedInWorld ||
                hitItem.State != GunItemState.World)
            {
                return false;
            }

            Rigidbody targetRigidbody = other.attachedRigidbody;
            if (targetRigidbody == null || !targetRigidbody.isKinematic)
            {
                return false;
            }

            if (!IsActiveProjectile)
            {
                return false;
            }

            Vector3 impulse = CalculateWorldMountImpulse(targetRigidbody);
            hitItem.ReleaseFromWorldMountServer(impulse, contactPoint);
            Spend();
            ProjectileImpactClientRpc(contactPoint, contactNormal, false);
            return true;
        }

        private Vector3 CalculateWorldMountImpulse(Rigidbody targetRigidbody)
        {
            if (rigidBody == null)
            {
                return Vector3.zero;
            }

            Vector3 velocity = rigidBody.linearVelocity;
            if (velocity.sqrMagnitude < MIN_DIRECTION_SQR || velocity.magnitude < MIN_SWEEP_SPEED)
            {
                return Vector3.zero;
            }

            float targetMass = targetRigidbody.mass;
            if (targetMass <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 deltaVelocity = velocity.normalized * (rigidBody.mass / targetMass) * WORLD_PROP_IMPULSE_SCALE;
            deltaVelocity = Vector3.ClampMagnitude(deltaVelocity, WORLD_PROP_MAX_DELTA_V);
            return deltaVelocity * targetMass;
        }

        private void HandlePlayerHit(PlayerHealth playerHealth, Vector3 contactPoint, Vector3 contactNormal)
        {
            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            ulong playerClientId = playerHealth.DamageTargetId;
            if (playerClientId == shooterClientId)
            {
                return;
            }

            float speedRatio = launchSpeed > 0f
                ? Mathf.Clamp01(currentSpeed / launchSpeed)
                : 0f;
            if (speedRatio <= MIN_DAMAGE_SPEED_RATIO)
            {
                return;
            }

            float now = Time.time;
            if (lastDamageTimes.TryGetValue(playerClientId, out float lastDamageTime) &&
                now - lastDamageTime < PLAYER_HIT_COOLDOWN)
            {
                return;
            }

            if (!damagedPlayers.Add(playerClientId))
            {
                return;
            }

            lastDamageTimes[playerClientId] = now;
            bool isSpawnProtected = playerHealth.IsSpawnProtected;

            if (playerDamageEnabled || playerHealth.IsTestTarget)
            {
                playerHealth.ApplyDamage(gunItem.Damage * speedRatio, shooterClientId, gunItem.HitCueType);
            }

            ProjectileImpactClientRpc(contactPoint, contactNormal, true);

            if (!isSpawnProtected && !playerHealth.IsTestTarget)
            {
                TryPushPlayer(playerClientId, playerHealth);
            }
        }

        private void ClearDamageHistory()
        {
            damagedPlayers.Clear();
            lastDamageTimes.Clear();
        }

        private void TryPushPlayer(ulong playerClientId, PlayerHealth playerHealth)
        {
            if (playerPushSpeed <= 0f && playerPushUpSpeed <= 0f)
            {
                return;
            }

            float now = Time.time;
            if (lastPushTimes.TryGetValue(playerClientId, out float lastPushTime) &&
                now - lastPushTime < DEFAULT_PUSH_INTERVAL)
            {
                return;
            }

            lastPushTimes[playerClientId] = now;

            PlayerController controller = playerHealth.GetComponent<PlayerController>();
            if (controller == null)
            {
                return;
            }

            Vector3 horizontalVelocity = rigidBody.linearVelocity;
            horizontalVelocity.y = 0f;

            Vector3 direction;
            if (horizontalVelocity.sqrMagnitude > MIN_DIRECTION_SQR)
            {
                direction = horizontalVelocity.normalized;
            }
            else
            {
                direction = playerHealth.transform.position - transform.position;
                direction.y = 0f;
                direction = direction.sqrMagnitude > MIN_DIRECTION_SQR ? direction.normalized : transform.forward;
            }

            float weightScale = gunItem.WeightClass == GunWeightClass.Heavy ? HEAVY_PUSH_SCALE : 1f;
            Vector3 launchVelocity = direction * (playerPushSpeed * weightScale) + Vector3.up * (playerPushUpSpeed * weightScale);
            controller.ApplyLaunch(launchVelocity);
        }

        public void IgnoreCollisionsWith(GunProjectileBody other)
        {
            if (other == null || other == this || projectileColliders == null)
            {
                return;
            }

            Collider[] otherColliders = other.GetComponentsInChildren<Collider>(true);
            foreach (Collider ownCollider in projectileColliders)
            {
                if (ownCollider == null)
                {
                    continue;
                }

                foreach (Collider otherCollider in otherColliders)
                {
                    if (otherCollider != null)
                    {
                        IgnoreCollisionPair(ownCollider, otherCollider);
                    }
                }
            }
        }

        public void RestoreIgnoredCollisions()
        {
            if (IsActiveProjectile)
            {
                return;
            }

            foreach (IgnoredColliderPair pair in ignoredColliderPairs)
            {
                Collider first = pair.First;
                Collider second = pair.Second;
                if (first != null && second != null)
                {
                    Physics.IgnoreCollision(first, second, false);
                }
            }

            ignoredColliderPairs.Clear();
        }

        private void IgnoreCollisionPair(Collider first, Collider second)
        {
            if (first == null || second == null)
            {
                return;
            }

            Physics.IgnoreCollision(first, second, true);
            ignoredColliderPairs.Add(new IgnoredColliderPair(first, second));
        }

        internal bool Spend()
        {
            if (!IsServer || isSpent.Value || finished || isRetiring.Value)
            {
                return false;
            }

            isSpent.Value = true;
            spentAtTime = Time.time;
            GunProjectileRegistry.IgnoreCollisionsFor(this);
            ProjectileTrace.Log(
                "spend t=" + Time.time.ToString("F3") +
                " id=" + NetworkObjectId +
                " pos=" + transform.position.ToString("F3"));
            return true;
        }

        private void ExpireToWorldServer()
        {
            if (!IsServer || finished || isRetiring.Value)
            {
                return;
            }

            isSpent.Value = true;
            finished = true;
            GunProjectileRegistry.Unregister(this);
            RestoreIgnoredCollisions();
            ProjectileTrace.Log(
                "expire t=" + Time.time.ToString("F3") +
                " id=" + NetworkObjectId +
                " pos=" + transform.position.ToString("F3") +
                " speed=" + (rigidBody != null ? rigidBody.linearVelocity.magnitude.ToString("F2") : "null"));

            if (trail != null)
            {
                trail.emitting = false;
            }

            if (gunItem != null)
            {
                gunItem.EnterWorldStateServer();
            }
        }

        internal void BeginRetire()
        {
            if (!IsServer)
            {
                return;
            }

            GunProjectileRegistry.Unregister(this);

            if (!IsSpawned || isRetiring.Value)
            {
                return;
            }

            if (gunItem != null)
            {
                gunItem.NotifyRetiringServer();
            }

            ProjectileTrace.Log(
                "retire t=" + Time.time.ToString("F3") +
                " id=" + NetworkObjectId +
                " pos=" + transform.position.ToString("F3"));

            isRetiring.Value = true;
            finished = true;
            RestoreIgnoredCollisions();
            DisableColliders();

            if (rigidBody != null)
            {
                rigidBody.linearVelocity = Vector3.zero;
                rigidBody.angularVelocity = Vector3.zero;
                rigidBody.isKinematic = true;
            }

            StartCoroutine(RetireRoutine());
        }

        private IEnumerator RetireRoutine()
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < RETIRE_DURATION)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / RETIRE_DURATION);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
                yield return null;
            }

            if (IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

        internal void NotifyPickedUpServer()
        {
            if (!IsServer)
            {
                return;
            }

            GunProjectileRegistry.Unregister(this);
            finished = true;
            RestoreIgnoredCollisions();
            ProjectileTrace.Log(
                "picked up t=" + Time.time.ToString("F3") +
                " id=" + NetworkObjectId +
                " pos=" + transform.position.ToString("F3"));
        }

        private void OnNetworkShooterChanged(ulong previousValue, ulong newValue)
        {
            if (gunItem != null && gunItem.State == GunItemState.Projectile)
            {
                IgnoreShooterCollisions();
            }

        }

        public void SetPlayerDamageEnabledServer(bool enabled)
        {
            playerDamageEnabled = enabled;
        }

        public void SetLaunchVelocityServer(Vector3 velocity)
        {
            if (IsServer)
            {
                networkLaunchVelocity.Value = velocity;
            }
        }


        private void OnSpentChanged(bool previousValue, bool newValue)
        {
            if (newValue && trail != null)
            {
                trail.emitting = false;
            }
        }

        private void OnRetiringChanged(bool previousValue, bool newValue)
        {
            if (!newValue)
            {
                return;
            }

            DisableColliders();

            if (trail != null)
            {
                trail.emitting = false;
            }

            if (!IsServer)
            {
                StartClientRetireShrink();
            }
        }

        private void StartClientRetireShrink()
        {
            if (IsServer || !IsClient || clientRetireCoroutine != null)
            {
                return;
            }

            clientRetireCoroutine = StartCoroutine(ClientRetireRoutine());
        }

        private IEnumerator ClientRetireRoutine()
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < RETIRE_DURATION)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, Mathf.Clamp01(elapsed / RETIRE_DURATION));
                yield return null;
            }

            clientRetireCoroutine = null;
        }

        private void DisableColliders()
        {
            foreach (Collider projectileCollider in projectileColliders)
            {
                if (projectileCollider != null)
                {
                    projectileCollider.enabled = false;
                }
            }
        }

        public void RefreshShooterCollisionIgnores()
        {
            IgnoreShooterCollisions();
        }

        private void IgnoreShooterCollisions()
        {
            if (projectileColliders == null ||
                NetworkManager == null ||
                NetworkManager.SpawnManager == null)
            {
                return;
            }

            ulong shooter = IsServer ? shooterClientId : networkShooterClientId.Value;
            NetworkObject shooterObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(shooter);
            if (shooterObject == null)
            {
                return;
            }

            Collider[] shooterColliders = shooterObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider projectileCollider in projectileColliders)
            {
                if (projectileCollider == null)
                {
                    continue;
                }

                foreach (Collider shooterCollider in shooterColliders)
                {
                    if (shooterCollider != null)
                    {
                        IgnoreCollisionPair(projectileCollider, shooterCollider);
                    }
                }
            }
        }

        [ClientRpc]
        private void ProjectileImpactClientRpc(Vector3 position, Vector3 normal, bool hitPlayer)
        {
            GunEffects.SpawnImpact(position, normal, hitPlayer);
        }

        private void AttachTrail()
        {
            MeshRenderer mainRenderer = GunColorUtility.GetMainRenderer(gameObject);
            if (mainRenderer == null)
            {
                return;
            }

            Vector3 scaledSize = Vector3.Scale(mainRenderer.localBounds.size, mainRenderer.transform.lossyScale);
            float itemLength = Mathf.Max(scaledSize.x, Mathf.Max(scaledSize.y, scaledSize.z));
            float trailWidth = Mathf.Max(scaledSize.x, scaledSize.y) * trailWidthScale;
            float trailLength = Mathf.Clamp(itemLength * trailLengthMultiplier, MIN_TRAIL_LENGTH, MAX_TRAIL_LENGTH);
            Color trailColor = GunColorUtility.GetMainColor(gameObject);

            trail = GunEffects.AttachProjectileTrail(gameObject, trailColor, trailWidth, trailLength, gunItem.ProjectileSpeed);
        }
    }
}
