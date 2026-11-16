using UnityEngine;

namespace GunVerse
{
    public static class ProjectileShotIds
    {
        private static uint nextShotId;

        public static uint Next()
        {
            nextShotId++;
            if (nextShotId == 0)
            {
                nextShotId = 1;
            }

            return nextShotId;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            nextShotId = 0;
        }
    }
}
