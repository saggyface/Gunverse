using System.Collections.Generic;
using UnityEngine;

namespace GunVerse
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        private static readonly List<PlayerSpawnPoint> spawnPoints = new List<PlayerSpawnPoint>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSpawnPoints()
        {
            spawnPoints.Clear();
        }

        private void Awake()
        {
            spawnPoints.Add(this);
        }

        private void OnDestroy()
        {
            spawnPoints.Remove(this);
        }

        public static IReadOnlyList<PlayerSpawnPoint> SpawnPoints => spawnPoints;

        public Quaternion Rotation => transform.rotation;

        public static Vector3 GetSpawnPosition(ulong clientId)
        {
            if (spawnPoints.Count == 0)
            {
                return Vector3.up;
            }

            int index = (int)(clientId % (ulong)spawnPoints.Count);
            return spawnPoints[index].transform.position;
        }

        public static Quaternion GetSpawnRotation(ulong clientId)
        {
            if (spawnPoints.Count == 0)
            {
                return Quaternion.identity;
            }

            int index = (int)(clientId % (ulong)spawnPoints.Count);
            return spawnPoints[index].transform.rotation;
        }

        public static bool TryGetRandomSpawnPose(out Vector3 position, out Quaternion rotation)
        {
            if (spawnPoints.Count == 0)
            {
                position = Vector3.up;
                rotation = Quaternion.identity;
                return false;
            }

            PlayerSpawnPoint spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            position = spawnPoint.transform.position;
            rotation = spawnPoint.transform.rotation;
            return true;
        }
    }
}
