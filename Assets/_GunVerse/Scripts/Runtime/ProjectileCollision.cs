using UnityEngine;

namespace GunVerse
{
    public static class ProjectileCollision
    {
        private const string VIEWMODEL_LAYER_NAME = "ViewModel";
        private const int MUZZLE_RESOLVE_ATTEMPTS = 8;
        private const float MIN_MUZZLE_NUDGE = 0.02f;
        private const float MAX_MUZZLE_BACKSTEP = 0.5f;

        private static readonly Collider[] OverlapBuffer = new Collider[16];

        private static int worldMask;
        private static int playerMask;
        private static int viewmodelLayer;

        public static int WorldMask
        {
            get
            {
                EnsureInitialized();
                return worldMask;
            }
        }

        public static int PlayerMask
        {
            get
            {
                EnsureInitialized();
                return playerMask;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            worldMask = 0;
            playerMask = 0;
            viewmodelLayer = int.MinValue;
        }

        public static bool IsViewModelCollider(Collider collider)
        {
            EnsureInitialized();
            return collider != null && viewmodelLayer >= 0 && collider.gameObject.layer == viewmodelLayer;
        }

        public static bool IsStaticPathCollider(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            if (IsViewModelCollider(collider) || collider.isTrigger)
            {
                return false;
            }

            if (collider.GetComponentInParent<PlayerHealth>() != null)
            {
                return false;
            }

            GunItem gunItem = collider.GetComponentInParent<GunItem>();
            if (gunItem != null)
            {
                return gunItem.State == GunItemState.World;
            }

            if (collider.GetComponentInParent<GunProjectileBody>() != null)
            {
                return false;
            }

            Rigidbody attachedRigidbody = collider.attachedRigidbody;
            return attachedRigidbody == null || attachedRigidbody.isKinematic;
        }

        public static bool TryResolveMuzzle(
            Vector3 origin,
            Vector3 direction,
            float radius,
            out Vector3 resolvedOrigin)
        {
            resolvedOrigin = origin;

            Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;
            float nudgeDistance = Mathf.Max(radius * 1.1f, MIN_MUZZLE_NUDGE);
            float remainingBackstep = MAX_MUZZLE_BACKSTEP;

            for (int attempt = 0; attempt < MUZZLE_RESOLVE_ATTEMPTS && remainingBackstep > 0f; attempt++)
            {
                int overlapCount = Physics.OverlapSphereNonAlloc(
                    resolvedOrigin,
                    radius,
                    OverlapBuffer,
                    WorldMask,
                    QueryTriggerInteraction.Ignore);

                bool blocked = false;
                for (int index = 0; index < overlapCount; index++)
                {
                    if (IsStaticPathCollider(OverlapBuffer[index]))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    return true;
                }

                float backstep = Mathf.Min(nudgeDistance, remainingBackstep);
                resolvedOrigin -= normalizedDirection * backstep;
                remainingBackstep -= backstep;
            }

            return false;
        }

        private static void EnsureInitialized()
        {
            if (worldMask != 0)
            {
                return;
            }

            viewmodelLayer = LayerMask.NameToLayer(VIEWMODEL_LAYER_NAME);
            int layerMask = Physics.DefaultRaycastLayers;

            if (viewmodelLayer >= 0)
            {
                layerMask &= ~(1 << viewmodelLayer);
            }

            worldMask = layerMask;
            playerMask = layerMask;
        }
    }
}
