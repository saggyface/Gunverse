using System.Collections.Generic;
using UnityEngine;

namespace GunVerse
{
    public static class ProjectileMuzzleCache
    {
        private static readonly Dictionary<ulong, Vector3> muzzles = new Dictionary<ulong, Vector3>();

        public static void Set(ulong clientId, Vector3 muzzlePosition)
        {
            muzzles[clientId] = muzzlePosition;
        }

        public static bool TryGet(ulong clientId, out Vector3 muzzlePosition)
        {
            return muzzles.TryGetValue(clientId, out muzzlePosition);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            muzzles.Clear();
        }
    }
}
