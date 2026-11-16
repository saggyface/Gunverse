using System.Collections.Generic;
using UnityEngine;

namespace GunVerse
{
    public static class GunProjectileRegistry
    {
        private const int GLOBAL_ACTIVE_LIMIT = 500;

        private static readonly List<GunProjectileBody> activeProjectiles = new List<GunProjectileBody>();
        private static readonly Dictionary<uint, List<GunProjectileBody>> projectilesByType = new Dictionary<uint, List<GunProjectileBody>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Reset();
        }

        public static void Register(GunProjectileBody projectile)
        {
            if (projectile == null || activeProjectiles.Contains(projectile))
            {
                return;
            }

            activeProjectiles.Add(projectile);

            if (!projectilesByType.TryGetValue(projectile.TypeHash, out List<GunProjectileBody> typedProjectiles))
            {
                typedProjectiles = new List<GunProjectileBody>();
                projectilesByType.Add(projectile.TypeHash, typedProjectiles);
            }

            typedProjectiles.Add(projectile);
        }

        public static void Unregister(GunProjectileBody projectile)
        {
            if (projectile == null)
            {
                return;
            }

            activeProjectiles.Remove(projectile);

            if (!projectilesByType.TryGetValue(projectile.TypeHash, out List<GunProjectileBody> typedProjectiles))
            {
                return;
            }

            typedProjectiles.Remove(projectile);
            if (typedProjectiles.Count == 0)
            {
                projectilesByType.Remove(projectile.TypeHash);
            }
        }

        public static void EnforceLimits(uint typeHash, int perTypeLimit)
        {
            if (perTypeLimit > 0 &&
                projectilesByType.TryGetValue(typeHash, out List<GunProjectileBody> typedProjectiles))
            {
                while (typedProjectiles.Count >= perTypeLimit)
                {
                    GunProjectileBody oldest = typedProjectiles[0];
                    if (oldest == null)
                    {
                        typedProjectiles.RemoveAt(0);
                        continue;
                    }

                    oldest.BeginRetire();
                }
            }

            while (activeProjectiles.Count >= GLOBAL_ACTIVE_LIMIT)
            {
                GunProjectileBody oldest = activeProjectiles[0];
                if (oldest == null)
                {
                    activeProjectiles.RemoveAt(0);
                    continue;
                }

                oldest.BeginRetire();
            }
        }

        public static void IgnoreCollisionsFor(GunProjectileBody projectile)
        {
            if (projectile == null)
            {
                return;
            }

            foreach (GunProjectileBody other in activeProjectiles)
            {
                if (other == null || other == projectile)
                {
                    continue;
                }

                if (other.ShotGroupId == projectile.ShotGroupId)
                {
                    projectile.IgnoreCollisionsWith(other);
                }
            }
        }
        public static void RetireProjectilesForShooter(ulong shooterClientId)
        {
            GunProjectileBody[] snapshot = activeProjectiles.ToArray();
            foreach (GunProjectileBody projectile in snapshot)
            {
                if (projectile != null && projectile.ShooterClientId == shooterClientId)
                {
                    projectile.BeginRetire();
                }
            }
        }

        public static void RetireAll()
        {
            GunProjectileBody[] snapshot = activeProjectiles.ToArray();
            foreach (GunProjectileBody projectile in snapshot)
            {
                if (projectile != null)
                {
                    projectile.BeginRetire();
                }
            }
        }

        public static void Reset()
        {
            activeProjectiles.Clear();
            projectilesByType.Clear();
        }
    }
}
