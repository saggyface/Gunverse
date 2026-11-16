using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public sealed class TestModeSpawner : NetworkBehaviour
    {
        private const int TARGET_COUNT = 5;
        private const ulong TEST_SCORE_BASE = 1000;

        [SerializeField] private GameObject testTargetPrefab;

        public override void OnNetworkSpawn()
        {
            if (!IsServer || MultiplayerSessionManager.Instance == null || !MultiplayerSessionManager.Instance.TestModeEnabled)
            {
                return;
            }

            SpawnTargets();
        }

        private void SpawnTargets()
        {
            if (testTargetPrefab == null)
            {
                Debug.LogError("[TestModeSpawner] Test target prefab is not assigned.");
                return;
            }

            int spawnPointCount = PlayerSpawnPoint.SpawnPoints.Count;
            if (spawnPointCount == 0)
            {
                Debug.LogError("[TestModeSpawner] No spawn points available for test targets.");
                return;
            }

            for (int index = 0; index < TARGET_COUNT; index++)
            {
                ulong scoreId = TEST_SCORE_BASE + (ulong)index;
                int spawnIndex = (index * 5 + 3) % spawnPointCount;
                Vector3 position = PlayerSpawnPoint.SpawnPoints[spawnIndex].transform.position + Vector3.up * 0.05f;
                Quaternion rotation = PlayerSpawnPoint.SpawnPoints[spawnIndex].transform.rotation;

                GameObject instance = Instantiate(testTargetPrefab, position, rotation);
                NetworkObject networkObject = instance.GetComponent<NetworkObject>();
                TestTargetScore score = instance.GetComponent<TestTargetScore>();
                if (networkObject == null || score == null)
                {
                    Debug.LogError("[TestModeSpawner] Test target prefab is missing NetworkObject or TestTargetScore.");
                    Destroy(instance);
                    continue;
                }

                networkObject.Spawn(destroyWithScene: true);
                score.InitializeServer(scoreId, "TEST " + (index + 1));
            }
        }
    }
}
