using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public sealed class ServerProjectileFlight : MonoBehaviour
    {
        private const float WORLD_PROP_IMPULSE_SCALE = 0.65f;
        private const float WORLD_PROP_MAX_DELTA_VELOCITY = 6f;
        private const float TEST_TARGET_DAMAGE_RADIUS = 0.5f;
        private const int OVERLAP_BUFFER_SIZE = 32;

        private static readonly Collider[] OverlapBuffer = new Collider[OVERLAP_BUFFER_SIZE];
        private static readonly List<ServerProjectileFlight> activeFlights =
            new List<ServerProjectileFlight>();
        private static Transform flightRoot;

        private readonly HashSet<int> hitPropIds = new HashSet<int>();
        private readonly HashSet<ulong> damagedTestTargets = new HashSet<ulong>();

        private ProjectileShotData shot;
        private ProjectileFlightState state;
        private GunItem weaponItem;
        private GunItem selfItem;
        private float accumulator;
        private bool finished;
        private int worldMask;
        private int playerMask;
        private uint sourceHash;

        public static void Start(in ProjectileShotData shotData, GunItem sourceWeapon, GunItem selfProjectile)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer)
            {
                return;
            }

            EnsureRoot(networkManager);

            GameObject flightObject = new GameObject("ServerProjectileFlight");
            flightObject.transform.SetParent(flightRoot, false);
            ServerProjectileFlight flight = flightObject.AddComponent<ServerProjectileFlight>();
            flight.Initialize(shotData, sourceWeapon, selfProjectile);
            activeFlights.Add(flight);
            ProjectileTrace.Log(
                "server flight start id=" + shotData.shotId +
                " self=" + shotData.selfProjectile +
                " origin=" + shotData.origin.ToString("F2") +
                " speed=" + shotData.speed.ToString("F1"));
        }

        public static void FinishAll()
        {
            ServerProjectileFlight[] flights = activeFlights.ToArray();
            foreach (ServerProjectileFlight flight in flights)
            {
                if (flight != null)
                {
                    flight.Finish();
                }
            }
        }

        private void Initialize(
            in ProjectileShotData shotData,
            GunItem sourceWeapon,
            GunItem selfProjectile)
        {
            shot = shotData;
            weaponItem = sourceWeapon;
            selfItem = selfProjectile;
            state = ProjectileFlightSim.Begin(shot);
            worldMask = ProjectileCollision.WorldMask;
            playerMask = shotData.useItemShape ? ProjectileCollision.PlayerMask : 0;
            sourceHash = (uint)shot.visualPrefabHash;
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer)
            {
                Destroy(gameObject);
                return;
            }

            accumulator += Time.deltaTime;
            int steps = 0;

            while (accumulator >= ProjectileFlightSim.StepSeconds && steps < 32 && !finished)
            {
                Vector3 incomingVelocity = state.Velocity;
                ProjectileFlightEvent flightEvent = ProjectileFlightSim.Step(
                    ref state,
                    shot,
                    worldMask,
                    playerMask,
                    shot.shooter,
                    out RaycastHit hit);
                accumulator -= ProjectileFlightSim.StepSeconds;
                steps++;

                if (flightEvent == ProjectileFlightEvent.Bounce)
                {
                    ApplyPropImpulses(hit, incomingVelocity);
                    ApplyTestTargetDamage(hit);
                    continue;
                }

                if (flightEvent == ProjectileFlightEvent.PlayerHit)
                {
                    Finish();
                    return;
                }

                if (flightEvent == ProjectileFlightEvent.Settled)
                {
                    ApplyPropImpulses(hit, incomingVelocity);
                    ApplyTestTargetDamage(hit);
                    Finish();
                    return;
                }

                if (flightEvent == ProjectileFlightEvent.Expired)
                {
                    ApplyTestTargetDamage(hit);
                    Finish();
                    return;
                }

                ApplyTestTargetDamage(hit);
            }
        }

        private void ApplyPropImpulses(RaycastHit hit, Vector3 incomingVelocity)
        {
            if (hit.collider == null)
            {
                return;
            }

            GunItem otherItem = hit.collider.GetComponentInParent<GunItem>();
            if (otherItem == null ||
                otherItem == selfItem ||
                otherItem == weaponItem ||
                otherItem.State != GunItemState.World)
            {
                return;
            }

            Rigidbody body = otherItem.GetComponent<Rigidbody>();
            if (body == null)
            {
                return;
            }

            int bodyId = body.GetHashCode();
            if (hitPropIds.Contains(bodyId))
            {
                return;
            }

            Vector3 impactDirection = incomingVelocity.sqrMagnitude > 0.0001f
                ? incomingVelocity.normalized
                : (hit.normal.sqrMagnitude > 0.0001f ? -hit.normal : Vector3.up);
            float speed = incomingVelocity.magnitude;
            float bodyMass = Mathf.Max(0.01f, body.mass);
            float deltaVelocity = Mathf.Min(
                shot.mass * speed * WORLD_PROP_IMPULSE_SCALE / bodyMass,
                WORLD_PROP_MAX_DELTA_VELOCITY);

            if (otherItem.IsFixedInWorld && body.isKinematic)
            {
                Vector3 impulse = impactDirection * deltaVelocity * bodyMass;
                hitPropIds.Add(bodyId);
                otherItem.ReleaseFromWorldMountServer(impulse, hit.point);
                return;
            }

            if (!body.isKinematic)
            {
                hitPropIds.Add(bodyId);
                body.WakeUp();
                body.AddForceAtPosition(
                    impactDirection * deltaVelocity * bodyMass,
                    hit.point,
                    ForceMode.Impulse);
            }
        }

        private void ApplyTestTargetDamage(RaycastHit hit)
        {
            Vector3 shapeCenter = ProjectileShapeUtility.GetShapeCenter(
                state.Position,
                state.Rotation,
                shot);
            int overlapCount = Physics.OverlapSphereNonAlloc(
                shapeCenter,
                Mathf.Min(Mathf.Max(0.05f, shot.radius), TEST_TARGET_DAMAGE_RADIUS),
                OverlapBuffer,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < overlapCount; index++)
            {
                Collider overlap = OverlapBuffer[index];
                if (overlap == null)
                {
                    continue;
                }

                PlayerHealth target = overlap.GetComponentInParent<PlayerHealth>();
                if (target == null || !target.IsTestTarget || target.IsDead)
                {
                    continue;
                }

                if (!damagedTestTargets.Add(target.DamageTargetId))
                {
                    continue;
                }

                target.ApplyDamage(
                    shot.damage,
                    shot.shooter,
                    weaponItem != null ? weaponItem.HitCueType : HitCue.Generic);
                return;
            }
        }

        private void Finish()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            activeFlights.Remove(this);
            ProjectileTrace.Log(
                "server flight finish id=" + shot.shotId +
                " self=" + (selfItem != null) +
                " pos=" + state.Position.ToString("F2") +
                " bounces=" + state.Bounces);

            if (selfItem != null && selfItem.IsSpawned)
            {
                Vector3 shapeCenter = state.Position + state.Rotation * shot.localCenterOffset;
                selfItem.RevealClientVisualProjectileServer(
                    shot.shooter,
                    shot.shotId,
                    shapeCenter,
                    state.Rotation,
                    state.Velocity,
                    state.AngularVelocity);
            }
            else
            {
                SpawnPickup();
            }

            Destroy(gameObject);
        }

        private void SpawnPickup()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }

            GameObject prefab = GunItem.FindRegisteredPrefab(networkManager, sourceHash);
            if (prefab == null)
            {
                prefab = GunItem.FindRegisteredPrefab(networkManager, (uint)shot.weaponHash);
            }

            if (prefab == null)
            {
                ProjectileTrace.Log("server pickup missing prefab hash=" + sourceHash);
                return;
            }

            GameObject instance = Instantiate(prefab, state.Position, state.Rotation);
            GunItem item = instance.GetComponent<GunItem>();
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            if (item == null || networkObject == null)
            {
                Destroy(instance);
                return;
            }

            Vector3 shapeCenter = state.Position + state.Rotation * shot.localCenterOffset;
            Vector3 pivotPosition = shot.useItemShape
                ? item.GetPivotPositionForShapeCenter(shapeCenter, state.Rotation)
                : state.Position;
            item.ResolveSafeWorldPoseServer(ref pivotPosition, state.Rotation);
            instance.transform.SetPositionAndRotation(pivotPosition, state.Rotation);
            networkObject.Spawn(destroyWithScene: true);
            ProjectileTrace.Log(
                "server pickup spawned id=" + networkObject.NetworkObjectId +
                " prefab=" + prefab.name +
                " pos=" + state.Position.ToString("F2"));
            item.RevealResolvedClientVisualProjectileServer(
                shot.shooter,
                shot.shotId,
                pivotPosition,
                state.Rotation,
                state.Velocity,
                state.AngularVelocity);
        }

        private void OnDestroy()
        {
            activeFlights.Remove(this);
            hitPropIds.Clear();
            damagedTestTargets.Clear();
        }

        private static void EnsureRoot(NetworkManager networkManager)
        {
            if (flightRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("ServerProjectileFlights");
            flightRoot = root.transform;
            flightRoot.SetParent(networkManager.transform, false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            activeFlights.Clear();
            flightRoot = null;
        }
    }
}
