using System.Collections.Generic;
using UnityEngine;

namespace GunVerse
{
    public enum ProjectileFlightEvent
    {
        None,
        Bounce,
        PlayerHit,
        Settled,
        Expired
    }

    public struct ProjectileFlightState
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public Quaternion Rotation;
        public float Age;
        public int Bounces;
        public bool Ended;
        public HashSet<EntityId> InitialOverlapColliders;
    }

    public static class ProjectileFlightSim
    {
        public const float StepSeconds = 1f / 60f;
        public const float SelfProjectileSpawnGraceSeconds = 0.5f;

        private const float SettleSpeed = 1f;
        private const float GroundNormalThreshold = 0.7f;
        private const float BounceOffset = 0.001f;
        private const float OverlapExitDistanceScale = 0.1f;
        private const float COLLISION_ANGULAR_SCALE = 0.5f;
        private const float COLLISION_FRICTION_ANGULAR_SCALE = 0.25f;
        private const float MAX_ANGULAR_SPEED = 30f;
        private const float MIN_ANGULAR_SPEED_SQR = 0.000001f;
        private const float MIN_NORMAL_SQR = 0.000001f;
        private const float MIN_SHAPE_EXTENT = 0.01f;
        private const float MIN_PROJECTILE_MASS = 0.01f;
        private const int HitBufferSize = 64;
        private const int OverlapBufferSize = 64;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[HitBufferSize];
        private static readonly Collider[] OverlapBuffer = new Collider[OverlapBufferSize];

        public static ProjectileFlightState Begin(in ProjectileShotData shot)
        {
            Vector3 direction = shot.direction.sqrMagnitude > 0.0001f
                ? shot.direction.normalized
                : Vector3.forward;
            Vector3 rotationUp = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f
                ? Vector3.forward
                : Vector3.up;

            ProjectileFlightState state = new ProjectileFlightState
            {
                Position = shot.origin + direction * Mathf.Max(0f, shot.releaseDistance),
                Velocity = direction * Mathf.Max(0f, shot.speed),
                AngularVelocity = shot.spin,
                Rotation = Quaternion.LookRotation(direction, rotationUp),
                Age = 0f,
                Bounces = 0,
                Ended = false,
                InitialOverlapColliders = null
            };

            if (shot.useItemShape && shot.spawnGraceSeconds > 0f)
            {
                state.InitialOverlapColliders = CollectInitialOverlaps(
                    state.Position,
                    state.Rotation,
                    shot);
            }

            return state;
        }

        public static ProjectileFlightEvent Step(
            ref ProjectileFlightState state,
            in ProjectileShotData shot,
            int worldMask,
            int playerMask,
            ulong shooterClientId,
            out RaycastHit hit)
        {
            hit = default;

            if (state.Ended)
            {
                return ProjectileFlightEvent.None;
            }

            state.Age += StepSeconds;
            state.Velocity += Physics.gravity * shot.gravityScale * StepSeconds;

            if (state.AngularVelocity.sqrMagnitude > MIN_ANGULAR_SPEED_SQR)
            {
                float angle = state.AngularVelocity.magnitude * Mathf.Rad2Deg * StepSeconds;
                state.Rotation = Quaternion.AngleAxis(angle, state.AngularVelocity.normalized) * state.Rotation;
            }

            Vector3 preVelocity = state.Velocity;
            Vector3 delta = state.Velocity * StepSeconds;
            float distance = delta.magnitude;

            if (distance > 0f)
            {
                int hitCount = CastShapeNonAlloc(
                    shot,
                    state.Position,
                    state.Rotation,
                    delta / distance,
                    distance,
                    worldMask | playerMask);

                bool foundHit = false;
                bool foundPlayerHit = false;
                float closestDistance = float.MaxValue;

                for (int index = 0; index < hitCount; index++)
                {
                    RaycastHit candidate = HitBuffer[index];
                    if (candidate.collider == null || candidate.distance >= closestDistance)
                    {
                        continue;
                    }

                    if (ProjectileCollision.IsViewModelCollider(candidate.collider))
                    {
                        continue;
                    }

                    PlayerHealth playerHealth = candidate.collider.GetComponentInParent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        bool shooterStillIgnored =
                            playerHealth.OwnerClientId == shooterClientId &&
                            state.Age <= shot.spawnGraceSeconds;

                        if (playerMask == 0 ||
                            playerHealth.IsTestTarget ||
                            playerHealth.IsDead ||
                            shooterStillIgnored)
                        {
                            continue;
                        }

                        closestDistance = candidate.distance;
                        hit = candidate;
                        foundHit = true;
                        foundPlayerHit = true;
                        continue;
                    }

                    if (!ProjectileCollision.IsStaticPathCollider(candidate.collider))
                    {
                        continue;
                    }

                    bool ignoreInitialOverlap =
                        state.Age <= shot.spawnGraceSeconds &&
                        state.InitialOverlapColliders != null &&
                        state.InitialOverlapColliders.Contains(candidate.collider.GetEntityId());
                    if (ignoreInitialOverlap)
                    {
                        continue;
                    }

                    if (candidate.distance <= 0f && candidate.normal.sqrMagnitude < 0.0001f)
                    {
                        float nudgeDistance = GetShapeNudgeDistance(shot);
                        state.Position += delta.normalized * (nudgeDistance * OverlapExitDistanceScale);
                        return ProjectileFlightEvent.None;
                    }

                    closestDistance = candidate.distance;
                    hit = candidate;
                    foundHit = true;
                    foundPlayerHit = false;
                }

                if (foundHit)
                {
                    Vector3 direction = delta / distance;
                    state.Position += direction * Mathf.Max(0f, hit.distance - BounceOffset);

                    if (!shot.useItemShape && foundPlayerHit)
                    {
                        state.Ended = true;
                        return ProjectileFlightEvent.PlayerHit;
                    }

                    if (hit.normal.sqrMagnitude < MIN_NORMAL_SQR)
                    {
                        hit.normal = -direction;
                    }

                    Vector3 reflectedVelocity =
                        Vector3.Reflect(state.Velocity, hit.normal) * Mathf.Clamp01(shot.bounceDamping);

                    if (shot.useItemShape)
                    {
                        ApplyItemShapeBounceAngular(ref state, shot, hit, preVelocity, reflectedVelocity);
                    }
                    else
                    {
                        state.AngularVelocity *= Mathf.Clamp01(shot.angularDamping);
                    }

                    state.Velocity = reflectedVelocity;
                    state.Bounces++;

                    bool onGround = hit.normal.y > GroundNormalThreshold;
                    bool hasReachedBounceLimit = state.Bounces > Mathf.Max(0, shot.maxBounces);
                    bool hasSettledOnGround = onGround && state.Velocity.magnitude < SettleSpeed;
                    if (hasReachedBounceLimit || hasSettledOnGround)
                    {
                        state.Ended = true;
                        return ProjectileFlightEvent.Settled;
                    }

                    return ProjectileFlightEvent.Bounce;
                }
            }

            state.Position += delta;

            if (state.Age >= Mathf.Max(0.01f, shot.lifetime))
            {
                state.Ended = true;
                return ProjectileFlightEvent.Expired;
            }

            return ProjectileFlightEvent.None;
        }

        private static HashSet<EntityId> CollectInitialOverlaps(
            Vector3 position,
            Quaternion rotation,
            in ProjectileShotData shot)
        {
            Vector3 shapeCenter = ProjectileShapeUtility.GetShapeCenter(position, rotation, shot);
            int overlapCount;

            switch ((ProjectileShapeType)shot.shapeType)
            {
                case ProjectileShapeType.Capsule:
                {
                    ProjectileShapeUtility.GetCapsule(
                        shot,
                        position,
                        rotation,
                        out Vector3 point1,
                        out Vector3 point2,
                        out float radius);
                    overlapCount = Physics.OverlapCapsuleNonAlloc(
                        point1,
                        point2,
                        radius,
                        OverlapBuffer,
                        ProjectileCollision.WorldMask,
                        QueryTriggerInteraction.Collide);
                    break;
                }

                case ProjectileShapeType.Box:
                {
                    overlapCount = Physics.OverlapBoxNonAlloc(
                        shapeCenter,
                        ProjectileShapeUtility.GetBoxHalfExtents(shot),
                        OverlapBuffer,
                        rotation,
                        ProjectileCollision.WorldMask,
                        QueryTriggerInteraction.Collide);
                    break;
                }

                default:
                {
                    overlapCount = Physics.OverlapSphereNonAlloc(
                        shapeCenter,
                        ProjectileShapeUtility.GetSphereRadius(shot),
                        OverlapBuffer,
                        ProjectileCollision.WorldMask,
                        QueryTriggerInteraction.Collide);
                    break;
                }
            }

            HashSet<EntityId> colliders = null;
            for (int index = 0; index < overlapCount; index++)
            {
                Collider collider = OverlapBuffer[index];
                if (collider == null || !ProjectileCollision.IsStaticPathCollider(collider))
                {
                    continue;
                }

                if (colliders == null)
                {
                    colliders = new HashSet<EntityId>();
                }

                colliders.Add(collider.GetEntityId());
            }

            return colliders;
        }

        private static int CastShapeNonAlloc(
            in ProjectileShotData shot,
            Vector3 position,
            Quaternion rotation,
            Vector3 direction,
            float distance,
            int mask)
        {
            if (!shot.useItemShape)
            {
                return Physics.SphereCastNonAlloc(
                    position,
                    Mathf.Max(MIN_SHAPE_EXTENT, shot.radius),
                    direction,
                    HitBuffer,
                    distance,
                    mask,
                    QueryTriggerInteraction.Collide);
            }

            Vector3 shapeCenter = ProjectileShapeUtility.GetShapeCenter(position, rotation, shot);

            switch ((ProjectileShapeType)shot.shapeType)
            {
                case ProjectileShapeType.Capsule:
                    ProjectileShapeUtility.GetCapsule(
                        shot,
                        position,
                        rotation,
                        out Vector3 point1,
                        out Vector3 point2,
                        out float radius);
                    return Physics.CapsuleCastNonAlloc(
                        point1,
                        point2,
                        radius,
                        direction,
                        HitBuffer,
                        distance,
                        mask,
                        QueryTriggerInteraction.Collide);

                case ProjectileShapeType.Box:
                    return Physics.BoxCastNonAlloc(
                        shapeCenter,
                        ProjectileShapeUtility.GetBoxHalfExtents(shot),
                        direction,
                        HitBuffer,
                        rotation,
                        distance,
                        mask,
                        QueryTriggerInteraction.Collide);

                default:
                    return Physics.SphereCastNonAlloc(
                        shapeCenter,
                        ProjectileShapeUtility.GetSphereRadius(shot),
                        direction,
                        HitBuffer,
                        distance,
                        mask,
                        QueryTriggerInteraction.Collide);
            }
        }

        private static float GetShapeNudgeDistance(in ProjectileShotData shot)
        {
            if (!shot.useItemShape)
            {
                return Mathf.Max(MIN_SHAPE_EXTENT, shot.radius);
            }

            Vector3 extents = ProjectileShapeUtility.GetBoxHalfExtents(shot);
            float smallestExtent = Mathf.Min(
                extents.x,
                Mathf.Min(extents.y, extents.z));
            return Mathf.Max(MIN_SHAPE_EXTENT, smallestExtent);
        }

        private static void ApplyItemShapeBounceAngular(
            ref ProjectileFlightState state,
            in ProjectileShotData shot,
            RaycastHit hit,
            Vector3 preVelocity,
            Vector3 reflectedVelocity)
        {
            if (!shot.useItemShape)
            {
                return;
            }

            Vector3 shapeCenter = ProjectileShapeUtility.GetShapeCenter(
                state.Position,
                state.Rotation,
                shot);
            Vector3 contactOffset = hit.point - shapeCenter;
            Quaternion inverseRotation = Quaternion.Inverse(state.Rotation);
            Vector3 localContactOffset = inverseRotation * contactOffset;

            Vector3 normal = hit.normal;
            if (normal.sqrMagnitude < MIN_NORMAL_SQR)
            {
                normal = preVelocity.sqrMagnitude > MIN_NORMAL_SQR
                    ? -preVelocity.normalized
                    : Vector3.up;
            }

            Vector3 deltaVelocity = reflectedVelocity - preVelocity;
            Vector3 localDeltaVelocity = inverseRotation * deltaVelocity;
            Vector3 inverseInertia = ProjectileShapeUtility.GetInverseInertiaLocal(shot);
            float mass = Mathf.Max(MIN_PROJECTILE_MASS, shot.mass);

            Vector3 collisionOmega = Vector3.Scale(
                Vector3.Cross(localContactOffset, localDeltaVelocity * mass),
                inverseInertia);
            state.AngularVelocity += state.Rotation * collisionOmega * COLLISION_ANGULAR_SCALE;

            Vector3 tangent = Vector3.ProjectOnPlane(preVelocity, normal);
            Vector3 localTangent = inverseRotation * tangent;
            Vector3 frictionOmega = Vector3.Scale(
                Vector3.Cross(localContactOffset, -localTangent * mass),
                inverseInertia);
            state.AngularVelocity += state.Rotation * frictionOmega * COLLISION_FRICTION_ANGULAR_SCALE;

            state.AngularVelocity = Vector3.ClampMagnitude(state.AngularVelocity, MAX_ANGULAR_SPEED);
            state.AngularVelocity *= Mathf.Clamp01(shot.angularDamping);
        }

        public static void FastForward(
            ref ProjectileFlightState state,
            in ProjectileShotData shot,
            int worldMask,
            int playerMask,
            ulong shooterClientId,
            float ageSeconds)
        {
            float remaining = Mathf.Max(0f, ageSeconds);

            while (!state.Ended && remaining >= StepSeconds)
            {
                Step(ref state, shot, worldMask, playerMask, shooterClientId, out _);
                remaining -= StepSeconds;
            }
        }
    }
}
