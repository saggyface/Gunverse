using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public sealed class ProjectileVisual : MonoBehaviour
    {
        private const float OffsetDecaySeconds = 0.15f;
        private const float MIN_TRAIL_LENGTH = 4.5f;
        private const float MAX_TRAIL_LENGTH = 12f;
        private const float TRAIL_LENGTH_MULTIPLIER = 20f;
        private const float MIN_RADIUS = 0.01f;
        private const float SETTLED_TIMEOUT_SECONDS = 5f;

        private readonly List<Vector3> flightPath = new List<Vector3>();

        private ProjectileShotData shot;
        private ProjectileFlightState state;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 renderOffset;
        private float offsetElapsed;
        private float accumulator;
        private bool isActive;
        private bool isLocalShooter;
        private bool isSettled;
        private float settledAt;
        private int worldMask;
        private int playerMask;
        private TrailRenderer trail;

        internal ulong SourceKey { get; set; }
        public ulong ShooterClientId => shot.shooter;
        public uint ShotId => shot.shotId;
        public bool IsActive => isActive;
        public IReadOnlyList<Vector3> FlightPath => flightPath;

        public bool Begin(ProjectileShotData shotData, bool localShooter, Vector3 renderedMuzzle)
        {
            shot = shotData;
            isLocalShooter = localShooter;
            state = ProjectileFlightSim.Begin(shot);
            worldMask = ProjectileCollision.WorldMask;
            playerMask = shot.useItemShape ? ProjectileCollision.PlayerMask : 0;
            flightPath.Clear();
            flightPath.Add(state.Position);
            accumulator = 0f;
            offsetElapsed = 0f;
            isSettled = false;
            settledAt = 0f;

            Vector3 shotDirection = shot.direction.sqrMagnitude > 0.0001f
                ? shot.direction.normalized
                : Vector3.forward;
            Vector3 shapeCenter = state.Position + state.Rotation * shot.localCenterOffset;
            Vector3 renderedVisualStart = renderedMuzzle + shotDirection * Mathf.Max(0f, shot.releaseDistance);
            if (shot.useItemShape)
            {
                renderedVisualStart -= state.Rotation * shot.localCenter;
            }

            Vector3 trueVisualStart = GetVisualPivot(shapeCenter, state.Rotation);
            renderOffset = localShooter
                ? renderedVisualStart - trueVisualStart
                : ProjectileLaunchUtility.RemoveLateralOffset(renderedVisualStart - trueVisualStart, shotDirection);
            gameObject.SetActive(true);

            if (!localShooter)
            {
                float age = GetRemoteAge();
                if (age <= 0f)
                {
                    previousPosition = state.Position;
                    previousRotation = state.Rotation;
                }
                else
                {
                    ProjectileFlightSim.FastForward(
                        ref state,
                        shot,
                        worldMask,
                        playerMask,
                        shot.shooter,
                        age);

                    previousPosition = state.Position;
                    previousRotation = state.Rotation;
                }
            }
            else
            {
                previousPosition = state.Position;
                previousRotation = state.Rotation;
            }

            // Remote visuals fast-forward before this point, so resolve the pivot from the advanced state.
            Vector3 advancedShapeCenter = state.Position + state.Rotation * shot.localCenterOffset;
            transform.SetPositionAndRotation(
                GetVisualPivot(advancedShapeCenter, state.Rotation) + renderOffset,
                state.Rotation);
            AttachTrail();
            isActive = true;

            if (state.Ended)
            {
                MarkSettled();
            }

            ProjectileTrace.Log(
                "visual spawn id=" + shot.shotId +
                " local=" + localShooter +
                " age=" + (localShooter ? "0" : GetRemoteAge().ToString("F3")) +
                " pos=" + state.Position.ToString("F3") +
                " dir=" + shot.direction.ToString("F3"));

            return true;
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            if (isSettled)
            {
                if (Time.time - settledAt >= SETTLED_TIMEOUT_SECONDS)
                {
                    ReturnToPool();
                }

                return;
            }

            accumulator += Time.deltaTime;
            int steps = 0;

            while (accumulator >= ProjectileFlightSim.StepSeconds && steps < 32)
            {
                previousPosition = state.Position;
                previousRotation = state.Rotation;
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
                    flightPath.Add(state.Position);
                    GunEffects.SpawnImpact(hit.point, hit.normal, false);
                    continue;
                }

                if (flightEvent == ProjectileFlightEvent.PlayerHit)
                {
                    ReturnToPool();
                    return;
                }

                if (flightEvent == ProjectileFlightEvent.Settled ||
                    flightEvent == ProjectileFlightEvent.Expired)
                {
                    MarkSettled();
                    return;
                }
            }

            float interpolation = Mathf.Clamp01(accumulator / ProjectileFlightSim.StepSeconds);
            Vector3 renderedPosition = Vector3.Lerp(previousPosition, state.Position, interpolation);
            Quaternion renderedRotation = Quaternion.Slerp(previousRotation, state.Rotation, interpolation);
            Vector3 shapeCenter = renderedPosition + renderedRotation * shot.localCenterOffset;
            Vector3 visualPivot = GetVisualPivot(shapeCenter, renderedRotation);
            Vector3 offset = Vector3.zero;

            if (offsetElapsed < OffsetDecaySeconds)
            {
                float offsetWeight = 1f - Mathf.Clamp01(offsetElapsed / OffsetDecaySeconds);
                offset = renderOffset * offsetWeight;
            }

            transform.SetPositionAndRotation(visualPivot + offset, renderedRotation);
            offsetElapsed += Time.deltaTime;
        }

        private void MarkSettled()
        {
            isSettled = true;
            settledAt = Time.time;
            renderOffset = Vector3.zero;
            Vector3 shapeCenter = state.Position + state.Rotation * shot.localCenterOffset;
            transform.SetPositionAndRotation(GetVisualPivot(shapeCenter, state.Rotation), state.Rotation);

            if (trail != null)
            {
                trail.emitting = false;
            }

            ProjectileTrace.Log(
                "visual settle id=" + shot.shotId +
                " age=" + state.Age.ToString("F3") +
                " pos=" + state.Position.ToString("F3") +
                " bounces=" + state.Bounces);
        }

        public void ResetForPool()
        {
            isActive = false;
            isSettled = false;
            settledAt = 0f;
            accumulator = 0f;
            offsetElapsed = 0f;
            renderOffset = Vector3.zero;
            flightPath.Clear();

            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
        }

        private void ReturnToPool()
        {
            ProjectileVisualPool.Return(this);
        }

        private float GetRemoteAge()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, (float)(networkManager.ServerTime.Time - shot.serverTime));
        }

        private Vector3 GetVisualPivot(Vector3 shapeCenter, Quaternion rotation)
        {
            return shot.useItemShape
                ? shapeCenter - rotation * shot.localCenter
                : shapeCenter;
        }

        private void AttachTrail()
        {
            if (trail != null)
            {
                trail.emitting = true;
                trail.Clear();
                return;
            }

            MeshRenderer mainRenderer = GunColorUtility.GetMainRenderer(gameObject);
            if (mainRenderer == null)
            {
                return;
            }

            Vector3 scaledSize = Vector3.Scale(mainRenderer.localBounds.size, mainRenderer.transform.lossyScale);
            float itemLength = Mathf.Max(scaledSize.x, Mathf.Max(scaledSize.y, scaledSize.z));
            float trailWidth = Mathf.Max(scaledSize.x, scaledSize.y);
            float trailLength = Mathf.Clamp(itemLength * TRAIL_LENGTH_MULTIPLIER, MIN_TRAIL_LENGTH, MAX_TRAIL_LENGTH);
            Color trailColor = GunColorUtility.GetMainColor(gameObject);

            trail = GunEffects.AttachProjectileTrail(
                gameObject,
                trailColor,
                Mathf.Max(MIN_RADIUS, trailWidth),
                trailLength,
                shot.speed);
        }
    }
}
