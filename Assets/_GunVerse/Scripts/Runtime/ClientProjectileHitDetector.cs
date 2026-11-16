using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GunVerse
{
    [DefaultExecutionOrder(200)]
    public sealed class ClientProjectileHitDetector : MonoBehaviour
    {
        private const int HIT_BUFFER_SIZE = 32;
        private const float MIN_SWEEP_DISTANCE = 0.0001f;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[HIT_BUFFER_SIZE];
        private readonly HashSet<ulong> reportedVictims = new HashSet<ulong>();

        private GunProjectileBody projectileBody;
        private Vector3 lastPosition;
        private bool hasLastPosition;
        private bool wasProjectile;

        private void Awake()
        {
            projectileBody = GetComponent<GunProjectileBody>();
        }

        private void LateUpdate()
        {
            if (projectileBody == null || projectileBody.Item == null)
            {
                return;
            }

            bool isProjectile = projectileBody.Item.State == GunItemState.Projectile;
            if (!isProjectile)
            {
                wasProjectile = false;
                hasLastPosition = false;
                return;
            }

            if (!wasProjectile)
            {
                wasProjectile = true;
                reportedVictims.Clear();
                lastPosition = transform.position;
                hasLastPosition = true;
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null ||
                projectileBody.ShooterClientId != networkManager.LocalClientId)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            if (!hasLastPosition)
            {
                lastPosition = currentPosition;
                hasLastPosition = true;
                return;
            }

            Vector3 delta = currentPosition - lastPosition;
            float distance = delta.magnitude;
            if (distance <= MIN_SWEEP_DISTANCE)
            {
                lastPosition = currentPosition;
                return;
            }

            int hitCount = Physics.SphereCastNonAlloc(
                lastPosition,
                projectileBody.ProjectileRadius,
                delta / distance,
                hitBuffer,
                distance,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = hitBuffer[index];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (playerHealth == null ||
                    playerHealth.IsDead ||
                    playerHealth.IsTestTarget ||
                    playerHealth.OwnerClientId == projectileBody.ShooterClientId)
                {
                    continue;
                }

                if (!reportedVictims.Add(playerHealth.OwnerClientId))
                {
                    continue;
                }

                double projectileViewTime;
                double victimViewTime;
                if (networkManager.IsServer)
                {
                    projectileViewTime = networkManager.ServerTime.Time;
                    victimViewTime = networkManager.ServerTime.Time -
                                     1f / Mathf.Max(1, networkManager.NetworkConfig.TickRate);
                }
                else
                {
                    projectileViewTime = networkManager.ServerTime.Time -
                                         NetworkTransform.GetTickLatencyInSeconds();
                    victimViewTime = projectileViewTime;
                }

                projectileBody.ReportProjectileHit(
                    playerHealth.OwnerClientId,
                    hit.point,
                    projectileViewTime,
                    victimViewTime);
            }

            lastPosition = currentPosition;
        }
    }
}
