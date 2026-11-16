using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse
{
    public class PlayerSpawnManager : NetworkBehaviour
    {
        private const float MIN_SPAWN_DISTANCE = 10f;
        private const float SPAWN_EYE_HEIGHT = 1.65f;
        private const float SPAWN_LOS_PADDING = 0.2f;
        private const float LOAD_EVENT_FALLBACK_SECONDS = 5f;
        private const float RECONCILE_DURATION_SECONDS = 5f;
        private const float RECONCILE_INTERVAL_SECONDS = 0.5f;

        private static PlayerSpawnManager instance;

        [SerializeField] private GameObject playerPrefab;

        private readonly HashSet<ulong> pendingClients = new HashSet<ulong>();
        private readonly Dictionary<ulong, int> lastSpawnPointIndices = new Dictionary<ulong, int>();
        private Coroutine fallbackSpawnCoroutine;
        private Coroutine reconcileCoroutine;
        private bool sceneLoadComplete;

        public static PlayerSpawnManager Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            instance = this;
            sceneLoadComplete = false;
            pendingClients.Clear();
            lastSpawnPointIndices.Clear();
            GunItem.ResetHeldItems();
            GunProjectileRegistry.Reset();
            ServerProjectileHistory.Reset();
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            }

            fallbackSpawnCoroutine = StartCoroutine(WaitForSceneLoadFallback());

            int expectedPlayers = MultiplayerSessionManager.Instance != null &&
                                  MultiplayerSessionManager.Instance.CurrentSession != null
                ? MultiplayerSessionManager.Instance.CurrentSession.PlayerCount
                : NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (expectedPlayers <= 1 && NetworkManager.Singleton.ConnectedClientsIds.Count <= 1)
            {
                CompleteSceneSpawn();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                return;
            }

            if (instance == this)
            {
                instance = null;
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
                }
            }

            StopSpawnCoroutines();
            pendingClients.Clear();
            lastSpawnPointIndices.Clear();
            GunProjectileRegistry.Reset();
            ServerProjectileHistory.Reset();
        }

        private void OnLoadEventCompleted(
            string sceneName,
            LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted,
            List<ulong> clientsTimedOut)
        {
            if (!IsServer || sceneName != gameObject.scene.name)
            {
                return;
            }

            CompleteSceneSpawn();
        }

        private IEnumerator WaitForSceneLoadFallback()
        {
            yield return new WaitForSeconds(LOAD_EVENT_FALLBACK_SECONDS);
            fallbackSpawnCoroutine = null;

            if (!sceneLoadComplete)
            {
                CompleteSceneSpawn();
            }
        }

        private void CompleteSceneSpawn()
        {
            if (sceneLoadComplete)
            {
                return;
            }

            sceneLoadComplete = true;

            if (fallbackSpawnCoroutine != null)
            {
                StopCoroutine(fallbackSpawnCoroutine);
                fallbackSpawnCoroutine = null;
            }

            SpawnExistingPlayers();
            pendingClients.Clear();

            if (reconcileCoroutine != null)
            {
                StopCoroutine(reconcileCoroutine);
            }

            reconcileCoroutine = StartCoroutine(ReconcileMissingPlayers());
        }

        private IEnumerator ReconcileMissingPlayers()
        {
            float elapsed = 0f;
            while (elapsed < RECONCILE_DURATION_SECONDS)
            {
                yield return new WaitForSeconds(RECONCILE_INTERVAL_SECONDS);
                elapsed += RECONCILE_INTERVAL_SECONDS;

                if (!IsServer || NetworkManager.Singleton == null)
                {
                    yield break;
                }

                SpawnExistingPlayers();
            }

            reconcileCoroutine = null;
        }

        private void StopSpawnCoroutines()
        {
            if (fallbackSpawnCoroutine != null)
            {
                StopCoroutine(fallbackSpawnCoroutine);
                fallbackSpawnCoroutine = null;
            }

            if (reconcileCoroutine != null)
            {
                StopCoroutine(reconcileCoroutine);
                reconcileCoroutine = null;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!sceneLoadComplete)
            {
                pendingClients.Add(clientId);
                return;
            }

            SpawnPlayer(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            pendingClients.Remove(clientId);
            lastSpawnPointIndices.Remove(clientId);

            if (NetworkManager.Singleton == null || NetworkManager.Singleton.ShutdownInProgress)
            {
                return;
            }

            GameModeController.Instance?.CancelGunGameSwap(clientId);
            GunItem.HandlePlayerRemoved(clientId);
            GunProjectileRegistry.RetireProjectilesForShooter(clientId);
        }

        private void SpawnExistingPlayers()
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnPlayer(clientId);
            }
        }

        public static bool TryGetSpawnPose(ulong clientId, out Vector3 position, out Quaternion rotation)
        {
            if (instance != null && instance.TryGetSafeSpawnPose(clientId, out position, out rotation))
            {
                return true;
            }

            return PlayerSpawnPoint.TryGetRandomSpawnPose(out position, out rotation);
        }

        private bool TryGetSafeSpawnPose(ulong clientId, out Vector3 position, out Quaternion rotation)
        {
            IReadOnlyList<PlayerSpawnPoint> spawnPoints = PlayerSpawnPoint.SpawnPoints;
            if (spawnPoints.Count == 0)
            {
                position = Vector3.up;
                rotation = Quaternion.identity;
                return false;
            }

            PlayerHealth[] players = Object.FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);
            List<int> safeIndices = new List<int>(spawnPoints.Count);
            List<int> allIndices = new List<int>(spawnPoints.Count);

            for (int index = 0; index < spawnPoints.Count; index++)
            {
                PlayerSpawnPoint spawnPoint = spawnPoints[index];
                if (spawnPoint == null)
                {
                    continue;
                }

                allIndices.Add(index);
                Vector3 spawnPosition = spawnPoint.transform.position;
                bool isSafe = true;

                foreach (PlayerHealth player in players)
                {
                    if (player == null || player.IsDead || player.OwnerClientId == clientId)
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(spawnPosition, player.transform.position);
                    if (distance < MIN_SPAWN_DISTANCE || HasLineOfSight(spawnPosition, player.transform.position))
                    {
                        isSafe = false;
                        break;
                    }
                }

                if (isSafe)
                {
                    safeIndices.Add(index);
                }
            }

            List<int> candidates = safeIndices.Count > 0 ? safeIndices : allIndices;
            if (candidates.Count == 0)
            {
                position = Vector3.up;
                rotation = Quaternion.identity;
                return false;
            }

            if (candidates.Count > 1 && lastSpawnPointIndices.TryGetValue(clientId, out int lastIndex))
            {
                candidates.Remove(lastIndex);
            }

            int selectedIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            lastSpawnPointIndices[clientId] = selectedIndex;
            PlayerSpawnPoint selectedPoint = spawnPoints[selectedIndex];
            position = selectedPoint.transform.position;
            rotation = selectedPoint.transform.rotation;
            return true;
        }

        private static bool HasLineOfSight(Vector3 spawnPosition, Vector3 playerPosition)
        {
            Vector3 eye = spawnPosition + Vector3.up * SPAWN_EYE_HEIGHT;
            Vector3 target = playerPosition + Vector3.up * SPAWN_EYE_HEIGHT;
            Vector3 direction = target - eye;
            float distance = direction.magnitude;

            if (distance <= SPAWN_LOS_PADDING)
            {
                return true;
            }

            if (!Physics.Raycast(eye, direction / distance, out RaycastHit hit, distance - SPAWN_LOS_PADDING, ~0, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider.GetComponentInParent<PlayerHealth>() != null;
        }

        private void SpawnPlayer(ulong clientId)
        {
            if (playerPrefab == null ||
                NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                return;
            }

            NetworkObject existingPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (existingPlayer != null && existingPlayer.IsSpawned)
            {
                if (existingPlayer.gameObject.scene == gameObject.scene)
                {
                    return;
                }

                existingPlayer.Despawn(true);
            }

            GameModeType selectedMode = MultiplayerSessionManager.Instance != null
                ? MultiplayerSessionManager.Instance.SelectedGameMode
                : GameModeType.FreeForAll;
            TeamType assignedTeam = selectedMode == GameModeType.TeamDeathmatch
                ? ChooseTeam()
                : TeamType.None;

            TryGetSpawnPose(clientId, out Vector3 spawnPosition, out Quaternion spawnRotation);
            GameObject instance = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
            Debug.Log($"[PlayerSpawnManager] Spawned player for client {clientId} in {gameObject.scene.name}.");

            if (instance.TryGetComponent(out PlayerTeam playerTeam))
            {
                playerTeam.SetTeamServer(assignedTeam);
            }
        }

        private static TeamType ChooseTeam()
        {
            int blueCount = 0;
            int redCount = 0;

            foreach (PlayerTeam playerTeam in Object.FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude))
            {
                if (playerTeam.Team == TeamType.Blue)
                {
                    blueCount++;
                }
                else if (playerTeam.Team == TeamType.Red)
                {
                    redCount++;
                }
            }

            return blueCount <= redCount ? TeamType.Blue : TeamType.Red;
        }
    }
}
