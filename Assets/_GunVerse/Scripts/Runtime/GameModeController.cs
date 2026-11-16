using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class GameModeController : NetworkBehaviour
    {
        private const float PLAYER_WAIT_TIMEOUT_SECONDS = 15f;
        private const float PLAYER_WAIT_POLL_INTERVAL = 0.25f;
        private const int COUNTDOWN_FROM = 3;
        private const int NO_WINNER = -1;

        private readonly NetworkVariable<byte> mode = new NetworkVariable<byte>(
            (byte)GameModeType.FreeForAll,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<byte> phase = new NetworkVariable<byte>(
            (byte)MatchPhase.WaitingForLoad,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> countdownSeconds = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> remainingSeconds = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> teamScoreLimit = new NetworkVariable<int>(
            MatchRules.DEFAULT_TEAM_SCORE_LIMIT,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> playerKillLimit = new NetworkVariable<int>(
            MatchRules.DEFAULT_PLAYER_KILL_LIMIT,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> blueScore = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> redScore = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<byte> winnerTeam = new NetworkVariable<byte>(
            (byte)TeamType.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> winnerClientId = new NetworkVariable<int>(
            NO_WINNER,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> isDraw = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<byte> endReason = new NetworkVariable<byte>(
            (byte)MatchEndReason.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> useClientVisualProjectiles = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        [SerializeField] private MatchRules defaultMatchRules = new MatchRules();
        [SerializeField] private GunGameLadder gunGameLadder;

        private bool loadEventSubscribed;
        private bool sceneLoadCompleted;
        private int activeMatchDurationSeconds = MatchRules.DEFAULT_MATCH_DURATION_SECONDS;
        private Coroutine matchTimerCoroutine;
        private Coroutine gunGameMaintenanceCoroutine;
        private Coroutine waitForPlayersCoroutine;
        private int pendingGunGameWinnerClientId = NO_WINNER;
        private readonly Dictionary<ulong, Coroutine> gunGameSwapCoroutines =
            new Dictionary<ulong, Coroutine>();
        private readonly Dictionary<ulong, int> pendingGunGameSwapLevels =
            new Dictionary<ulong, int>();

        public static GameModeController Instance { get; private set; }

        public GameModeType Mode => (GameModeType)mode.Value;
        public MatchPhase Phase => (MatchPhase)phase.Value;
        public int CountdownSeconds => countdownSeconds.Value;
        public int RemainingSeconds => remainingSeconds.Value;
        public int TeamScoreLimit => teamScoreLimit.Value;
        public int PlayerKillLimit => playerKillLimit.Value;
        public int BlueScore => blueScore.Value;
        public int RedScore => redScore.Value;
        public TeamType WinnerTeam => (TeamType)winnerTeam.Value;
        public int WinningClientId => winnerClientId.Value;
        public bool HasWinningClient => winnerClientId.Value >= 0;
        public bool IsDraw => isDraw.Value;
        public MatchEndReason EndReason => (MatchEndReason)endReason.Value;

        public int GunGameStepCount => gunGameLadder != null ? gunGameLadder.Count : 0;
        public bool UseClientVisualProjectiles => useClientVisualProjectiles.Value;
        public static bool IsClientVisualProjectileMode =>
            Instance != null && Instance.UseClientVisualProjectiles;

        public string GetGunGameWeaponName(int level)
        {
            return gunGameLadder != null ? gunGameLadder.GetWeaponName(level) : string.Empty;
        }

        public bool IsCurrentGunGameWeapon(ulong clientId, uint prefabHash)
        {
            if (gunGameLadder == null || GunGameStepCount == 0 || prefabHash == 0)
            {
                return false;
            }

            PlayerGunGame progress = FindPlayerGunGame(clientId);
            if (progress == null)
            {
                return false;
            }

            int level = Mathf.Clamp(progress.Level, 0, GunGameStepCount - 1);
            GameObject weaponPrefab = gunGameLadder.GetWeapon(level);
            return weaponPrefab != null &&
                   weaponPrefab.TryGetComponent(out NetworkObject networkObject) &&
                   networkObject.PrefabIdHash == prefabHash;
        }

        public static bool IsGameplayActive => Instance == null || Instance.Phase == MatchPhase.Playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticInstance()
        {
            Instance = null;
        }

        private void Awake()
        {
            Instance = this;

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsServer &&
                NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
                loadEventSubscribed = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            ProjectileVisualPool.ReturnAll();

            if (!IsServer)
            {
                return;
            }

            sceneLoadCompleted = false;
            ProjectileReplicationSettings projectileSettings = ProjectileReplicationSettings.Load();
            useClientVisualProjectiles.Value = projectileSettings != null &&
                                                projectileSettings.UseClientVisualProjectiles;
            ProjectileTrace.Log(
                "projectile mode t=" + Time.time.ToString("F3") +
                " clientVisual=" + useClientVisualProjectiles.Value);

            mode.Value = (byte)(MultiplayerSessionManager.Instance != null
                ? MultiplayerSessionManager.Instance.SelectedGameMode
                : GameModeType.FreeForAll);

            ApplyMatchRules();

            if (Mode == GameModeType.GunGame)
            {
                gunGameMaintenanceCoroutine = StartCoroutine(GunGameMaintenanceRoutine());
            }

            if (!loadEventSubscribed && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
                loadEventSubscribed = true;
            }
            else if (NetworkManager.SceneManager == null)
            {
                sceneLoadCompleted = true;
            }

            if (phase.Value == (byte)MatchPhase.WaitingForLoad)
            {
                StartWaitForPlayers();
            }
        }

        public override void OnNetworkDespawn()
        {
            ServerProjectileFlight.FinishAll();
            ProjectileVisualPool.ReturnAll();
            StopMatchTimer();
            StopGunGameMaintenance();
            StopWaitingForPlayers();
            StopAllGunGameSwaps();

            if (loadEventSubscribed && NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
                loadEventSubscribed = false;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static bool IsDamageAllowed(ulong attackerClientId, ulong victimClientId)
        {
            return Instance == null
                ? attackerClientId != victimClientId
                : Instance.IsDamageAllowedInternal(attackerClientId, victimClientId);
        }

        public static void ReportElimination(ulong victimClientId, ulong attackerClientId)
        {
            if (Instance != null)
            {
                Instance.HandleElimination(victimClientId, attackerClientId);
            }
        }

        private void ApplyMatchRules()
        {
            MatchRules rules = MultiplayerSessionManager.Instance != null
                ? MultiplayerSessionManager.Instance.SelectedMatchRules
                : null;

            rules ??= defaultMatchRules;
            activeMatchDurationSeconds = rules.MatchDurationSeconds;
            teamScoreLimit.Value = rules.TeamScoreLimit;
            playerKillLimit.Value = rules.PlayerKillLimit;
        }

        private bool IsDamageAllowedInternal(ulong attackerClientId, ulong victimClientId)
        {
            if (attackerClientId == victimClientId)
            {
                return false;
            }

            if (Mode == GameModeType.FreeForAll || Mode == GameModeType.GunGame)
            {
                return true;
            }

            TeamType attackerTeam = FindPlayerTeam(attackerClientId);
            TeamType victimTeam = FindPlayerTeam(victimClientId);
            return attackerTeam == TeamType.None || victimTeam == TeamType.None || attackerTeam != victimTeam;
        }

        private void HandleElimination(ulong victimClientId, ulong attackerClientId)
        {
            if (Phase != MatchPhase.Playing)
            {
                return;
            }

            TestTargetScore testVictim = FindTestTargetScore(victimClientId);
            PlayerScore victimScore = testVictim == null ? FindPlayerScore(victimClientId) : null;

            if (testVictim != null)
            {
                testVictim.RecordDeathServer();
            }
            else if (victimScore != null)
            {
                victimScore.RecordDeath();
            }

            if (attackerClientId == victimClientId)
            {
                return;
            }

            PlayerScore attackerScore = FindPlayerScore(attackerClientId);
            if (attackerScore == null || (testVictim == null && victimScore == null))
            {
                return;
            }

            TeamType attackerTeam = FindPlayerTeam(attackerClientId);
            TeamType victimTeam = testVictim != null ? TeamType.None : FindPlayerTeam(victimClientId);

            if (Mode == GameModeType.TeamDeathmatch &&
                attackerTeam != TeamType.None &&
                attackerTeam == victimTeam)
            {
                return;
            }

            attackerScore.RecordKill();

            if (Mode == GameModeType.TeamDeathmatch)
            {
                if (attackerTeam == TeamType.Blue)
                {
                    blueScore.Value++;
                }
                else if (attackerTeam == TeamType.Red)
                {
                    redScore.Value++;
                }
            }

            FixedString128Bytes victimName = testVictim != null
                ? testVictim.DisplayNameValue
                : victimScore.DisplayNameValue;

            ShowKillFeedClientRpc(
                attackerScore.DisplayNameValue,
                victimName,
                (byte)attackerTeam,
                (byte)victimTeam);

            if (Mode == GameModeType.GunGame)
            {
                AdvanceGunGame(attackerClientId);
                return;
            }

            CheckScoreLimit(attackerScore);
        }

        private void AdvanceGunGame(ulong attackerClientId)
        {
            if (!IsServer || gunGameLadder == null || GunGameStepCount == 0)
            {
                return;
            }

            PlayerGunGame progress = FindPlayerGunGame(attackerClientId);
            if (progress == null)
            {
                return;
            }

            int currentLevel = Mathf.Clamp(progress.Level, 0, GunGameStepCount - 1);
            if (currentLevel >= GunGameStepCount - 1)
            {
                pendingGunGameWinnerClientId = (int)attackerClientId;
                EndMatch(MatchEndReason.LadderComplete);
                return;
            }

            int nextLevel = currentLevel + 1;
            GrantGunGameWeapon(attackerClientId, nextLevel, replaceHeldItem: true);
        }

        private void GrantGunGameWeapon(ulong clientId, int level, bool replaceHeldItem)
        {
            if (!IsServer || gunGameLadder == null || GunGameStepCount == 0)
            {
                return;
            }

            int clampedLevel = Mathf.Clamp(level, 0, GunGameStepCount - 1);
            GameObject weaponPrefab = gunGameLadder.GetWeapon(clampedLevel);
            if (weaponPrefab == null)
            {
                Debug.LogError(
                    $"[GameModeController] Gun Game level {clampedLevel + 1} has no weapon prefab. " +
                    "Rebuild the Gun Game progression.");
                return;
            }

            PlayerGunGame progress = FindPlayerGunGame(clientId);
            if (progress == null)
            {
                return;
            }

            progress.SetProgressServer(clampedLevel, GetGunGameWeaponName(clampedLevel));

            if (replaceHeldItem)
            {
                GunItem.ClearOutstandingThrownItem(clientId);
                if (gunGameSwapCoroutines.ContainsKey(clientId))
                {
                    pendingGunGameSwapLevels[clientId] = clampedLevel;
                    return;
                }

                gunGameSwapCoroutines[clientId] = StartCoroutine(
                    GunGameWeaponSwapRoutine(clientId, clampedLevel));
                return;
            }

            if (gunGameSwapCoroutines.ContainsKey(clientId) ||
                pendingGunGameSwapLevels.ContainsKey(clientId))
            {
                return;
            }

            if (GunItem.HasHeldItem(clientId) || GunItem.HasOutstandingThrownItem(clientId))
            {
                return;
            }

            if (!GunItem.GrantToPlayerServer(clientId, weaponPrefab))
            {
                Debug.LogWarning(
                    $"[GameModeController] Could not grant {weaponPrefab.name} to client {clientId}; " +
                    "the player is likely dead or missing.");
            }
        }

        private IEnumerator GunGameWeaponSwapRoutine(ulong clientId, int initialLevel)
        {
            int targetLevel = initialLevel;

            while (IsServer && Mode == GameModeType.GunGame)
            {
                if (pendingGunGameSwapLevels.TryGetValue(clientId, out int pendingLevel))
                {
                    targetLevel = pendingLevel;
                    pendingGunGameSwapLevels.Remove(clientId);
                }

                GunItem heldItem = GunItem.FindHeld(clientId);
                float tossDuration = heldItem != null ? heldItem.SwapTossDuration : 0f;
                if (heldItem != null)
                {
                    Vector3 forward = GetPlayerForward(clientId);
                    heldItem.TossHeldItemServer(forward);
                }

                if (tossDuration > 0f)
                {
                    yield return new WaitForSeconds(tossDuration);
                }

                if (!TryGetLivingPlayer(clientId, out _))
                {
                    break;
                }

                if (pendingGunGameSwapLevels.TryGetValue(clientId, out int newestLevel))
                {
                    targetLevel = newestLevel;
                    pendingGunGameSwapLevels.Remove(clientId);
                }

                GameObject weaponPrefab = gunGameLadder != null ? gunGameLadder.GetWeapon(targetLevel) : null;
                if (weaponPrefab == null)
                {
                    Debug.LogError(
                        $"[GameModeController] Gun Game level {targetLevel + 1} has no weapon prefab " +
                        "during a swap. Rebuild the Gun Game progression.");
                    break;
                }

                PlayerGunGame progress = FindPlayerGunGame(clientId);
                if (progress != null)
                {
                    progress.SetProgressServer(targetLevel, GetGunGameWeaponName(targetLevel));
                }

                if (!GunItem.GrantToPlayerServer(clientId, weaponPrefab))
                {
                    Debug.LogWarning(
                        $"[GameModeController] Could not grant {weaponPrefab.name} to client {clientId} " +
                        "during a swap.");
                    break;
                }

                GunItem newHeldItem = GunItem.FindHeld(clientId);
                float drawDuration = newHeldItem != null ? newHeldItem.SwapDrawDuration : 0f;
                if (drawDuration > 0f)
                {
                    yield return new WaitForSeconds(drawDuration);
                }

                if (!pendingGunGameSwapLevels.ContainsKey(clientId))
                {
                    break;
                }
            }

            gunGameSwapCoroutines.Remove(clientId);
            pendingGunGameSwapLevels.Remove(clientId);
        }

        private Vector3 GetPlayerForward(ulong clientId)
        {
            NetworkObject playerObject = GetPlayerObject(clientId);
            return playerObject != null ? playerObject.transform.forward : Vector3.forward;
        }

        private bool TryGetLivingPlayer(ulong clientId, out NetworkObject playerObject)
        {
            playerObject = GetPlayerObject(clientId);
            if (playerObject == null)
            {
                return false;
            }

            return playerObject.TryGetComponent(out PlayerHealth health) && !health.IsDead;
        }

        public void CancelGunGameSwap(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            if (gunGameSwapCoroutines.TryGetValue(clientId, out Coroutine swapCoroutine) &&
                swapCoroutine != null)
            {
                StopCoroutine(swapCoroutine);
            }

            gunGameSwapCoroutines.Remove(clientId);
            pendingGunGameSwapLevels.Remove(clientId);
        }

        private void StopAllGunGameSwaps()
        {
            foreach (Coroutine swapCoroutine in gunGameSwapCoroutines.Values)
            {
                if (swapCoroutine != null)
                {
                    StopCoroutine(swapCoroutine);
                }
            }

            gunGameSwapCoroutines.Clear();
            pendingGunGameSwapLevels.Clear();
        }

        private void GrantGunGameWeaponsToAll()
        {
            if (!IsServer || Mode != GameModeType.GunGame || NetworkManager == null || NetworkManager.SpawnManager == null)
            {
                return;
            }

            List<NetworkObject> players = new List<NetworkObject>(NetworkManager.SpawnManager.PlayerObjects);
            foreach (NetworkObject networkObject in players)
            {
                if (networkObject == null || !networkObject.TryGetComponent(out PlayerGunGame progress))
                {
                    continue;
                }

                if (!networkObject.TryGetComponent(out PlayerHealth health) || health.IsDead)
                {
                    continue;
                }

                GrantGunGameWeapon(networkObject.OwnerClientId, progress.Level, replaceHeldItem: false);
            }
        }

        private IEnumerator GunGameMaintenanceRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.5f);

            while (IsServer && Mode == GameModeType.GunGame)
            {
                if (Phase == MatchPhase.Countdown || Phase == MatchPhase.Playing)
                {
                    GrantGunGameWeaponsToAll();
                }

                yield return wait;
            }

            gunGameMaintenanceCoroutine = null;
        }

        private void StopGunGameMaintenance()
        {
            if (gunGameMaintenanceCoroutine != null)
            {
                StopCoroutine(gunGameMaintenanceCoroutine);
                gunGameMaintenanceCoroutine = null;
            }
        }

        private void CheckScoreLimit(PlayerScore attackerScore)
        {
            if (Mode == GameModeType.GunGame)
            {
                return;
            }

            if (Mode == GameModeType.TeamDeathmatch)
            {
                if (blueScore.Value >= teamScoreLimit.Value || redScore.Value >= teamScoreLimit.Value)
                {
                    EndMatch(MatchEndReason.ScoreLimit);
                }

                return;
            }

            if (attackerScore != null && attackerScore.Kills >= playerKillLimit.Value)
            {
                EndMatch(MatchEndReason.ScoreLimit);
            }
        }

        private void EndMatch(MatchEndReason reason)
        {
            if (!IsServer || Phase != MatchPhase.Playing)
            {
                return;
            }

            StopMatchTimer();
            DetermineWinner(reason);
            endReason.Value = (byte)reason;
            phase.Value = (byte)MatchPhase.Results;
            GunProjectileRegistry.RetireAll();
            StopAllGunGameSwaps();
            CancelPlayerRespawns();
        }

        private void DetermineWinner(MatchEndReason reason)
        {
            winnerTeam.Value = (byte)TeamType.None;
            winnerClientId.Value = NO_WINNER;
            isDraw.Value = false;

            if (Mode == GameModeType.TeamDeathmatch)
            {
                if (blueScore.Value == redScore.Value)
                {
                    isDraw.Value = true;
                    return;
                }

                winnerTeam.Value = (byte)(blueScore.Value > redScore.Value ? TeamType.Blue : TeamType.Red);
                return;
            }

            if (Mode == GameModeType.GunGame)
            {
                DetermineGunGameWinner(reason);
                return;
            }

            DetermineFreeForAllWinner();
        }

        private void DetermineFreeForAllWinner()
        {
            int bestKills = -1;
            int bestClientId = NO_WINNER;
            bool isTied = false;

            if (NetworkManager != null && NetworkManager.SpawnManager != null)
            {
                foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjects.Values)
                {
                    if (networkObject == null || !networkObject.TryGetComponent(out PlayerScore score))
                    {
                        continue;
                    }

                    if (score.Kills > bestKills)
                    {
                        bestKills = score.Kills;
                        bestClientId = (int)score.OwnerClientId;
                        isTied = false;
                    }
                    else if (score.Kills == bestKills)
                    {
                        isTied = true;
                    }
                }
            }

            if (bestKills <= 0 || isTied)
            {
                isDraw.Value = true;
                return;
            }

            winnerClientId.Value = bestClientId;
        }

        private void DetermineGunGameWinner(MatchEndReason reason)
        {
            if (reason == MatchEndReason.LadderComplete && pendingGunGameWinnerClientId >= 0)
            {
                winnerClientId.Value = pendingGunGameWinnerClientId;
                return;
            }

            int bestLevel = -1;
            int bestKills = -1;
            int bestClientId = NO_WINNER;
            bool isTied = false;

            if (NetworkManager != null && NetworkManager.SpawnManager != null)
            {
                foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjects.Values)
                {
                    if (networkObject == null || !networkObject.TryGetComponent(out PlayerGunGame progress))
                    {
                        continue;
                    }

                    int level = progress.Level;
                    int kills = networkObject.TryGetComponent(out PlayerScore score) ? score.Kills : 0;

                    if (level > bestLevel || (level == bestLevel && kills > bestKills))
                    {
                        bestLevel = level;
                        bestKills = kills;
                        bestClientId = (int)networkObject.OwnerClientId;
                        isTied = false;
                    }
                    else if (level == bestLevel && kills == bestKills)
                    {
                        isTied = true;
                    }
                }
            }

            if (bestLevel < 0 || isTied)
            {
                isDraw.Value = true;
                return;
            }

            winnerClientId.Value = bestClientId;
        }

        private void CancelPlayerRespawns()
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
            {
                return;
            }

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjects.Values)
            {
                if (networkObject != null && networkObject.TryGetComponent(out PlayerHealth health))
                {
                    health.CancelRespawnServer();
                }
            }
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!IsServer || sceneName != gameObject.scene.name)
            {
                return;
            }

            sceneLoadCompleted = true;
            StartWaitForPlayers();
        }

        private void StartWaitForPlayers()
        {
            if (!IsServer || waitForPlayersCoroutine != null || Phase != MatchPhase.WaitingForLoad)
            {
                return;
            }

            waitForPlayersCoroutine = StartCoroutine(WaitForPlayersRoutine());
        }

        private IEnumerator WaitForPlayersRoutine()
        {
            float startedAt = Time.time;
            bool loggedWaiting = false;

            while (IsServer && Phase == MatchPhase.WaitingForLoad)
            {
                if (AllExpectedPlayersReady())
                {
                    waitForPlayersCoroutine = null;
                    BeginCountdown();
                    yield break;
                }

                if (!loggedWaiting)
                {
                    loggedWaiting = true;
                    Debug.Log("[GameModeController] Waiting for players to connect and spawn.");
                }

                if (Time.time - startedAt >= PLAYER_WAIT_TIMEOUT_SECONDS)
                {
                    Debug.LogWarning("[GameModeController] Timed out waiting for players; starting countdown.");
                    waitForPlayersCoroutine = null;
                    BeginCountdown();
                    yield break;
                }

                yield return new WaitForSeconds(PLAYER_WAIT_POLL_INTERVAL);
            }

            waitForPlayersCoroutine = null;
        }

        private bool AllExpectedPlayersReady()
        {
            if (!sceneLoadCompleted)
            {
                return false;
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return false;
            }

            int expectedPlayers = 1;
            MultiplayerSessionManager sessionManager = MultiplayerSessionManager.Instance;
            if (sessionManager != null && sessionManager.CurrentSession != null)
            {
                expectedPlayers = Mathf.Max(1, sessionManager.CurrentSession.PlayerCount);
            }
            else
            {
                expectedPlayers = Mathf.Max(1, networkManager.ConnectedClientsIds.Count);
            }

            if (networkManager.ConnectedClientsIds.Count < expectedPlayers)
            {
                return false;
            }

            foreach (ulong clientId in networkManager.ConnectedClientsIds)
            {
                if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
                {
                    return false;
                }

                NetworkObject playerObject = client.PlayerObject;
                if (playerObject == null || !playerObject.IsSpawned)
                {
                    return false;
                }
            }

            return true;
        }

        private void StopWaitingForPlayers()
        {
            if (waitForPlayersCoroutine != null)
            {
                StopCoroutine(waitForPlayersCoroutine);
                waitForPlayersCoroutine = null;
            }
        }

        private void BeginCountdown()
        {
            if (!IsServer || Phase != MatchPhase.WaitingForLoad)
            {
                return;
            }

            phase.Value = (byte)MatchPhase.Countdown;

            if (Mode == GameModeType.GunGame)
            {
                GrantGunGameWeaponsToAll();
            }

            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            for (int seconds = COUNTDOWN_FROM; seconds > 0; seconds--)
            {
                countdownSeconds.Value = seconds;
                yield return new WaitForSeconds(1f);
            }

            countdownSeconds.Value = 0;
            BeginPlaying();
        }

        private void BeginPlaying()
        {
            if (!IsServer)
            {
                return;
            }

            remainingSeconds.Value = activeMatchDurationSeconds;
            phase.Value = (byte)MatchPhase.Playing;

            if (Mode == GameModeType.GunGame)
            {
                GrantGunGameWeaponsToAll();
            }

            StopMatchTimer();
            matchTimerCoroutine = StartCoroutine(MatchTimerRoutine());
        }

        private IEnumerator MatchTimerRoutine()
        {
            while (Phase == MatchPhase.Playing && remainingSeconds.Value > 0)
            {
                yield return new WaitForSeconds(1f);

                if (Phase != MatchPhase.Playing)
                {
                    matchTimerCoroutine = null;
                    yield break;
                }

                remainingSeconds.Value = Mathf.Max(0, remainingSeconds.Value - 1);
            }

            matchTimerCoroutine = null;

            if (Phase == MatchPhase.Playing)
            {
                EndMatch(MatchEndReason.TimeExpired);
            }
        }

        private void StopMatchTimer()
        {
            if (matchTimerCoroutine != null)
            {
                StopCoroutine(matchTimerCoroutine);
                matchTimerCoroutine = null;
            }
        }

        private PlayerScore FindPlayerScore(ulong clientId)
        {
            NetworkObject playerObject = GetPlayerObject(clientId);
            return playerObject != null && playerObject.TryGetComponent(out PlayerScore score)
                ? score
                : null;
        }

        private TestTargetScore FindTestTargetScore(ulong scoreId)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
            {
                return null;
            }

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjects.Values)
            {
                if (networkObject != null &&
                    networkObject.TryGetComponent(out TestTargetScore score) &&
                    score.ScoreId == scoreId)
                {
                    return score;
                }
            }

            return null;
        }

        private TeamType FindPlayerTeam(ulong clientId)
        {
            NetworkObject playerObject = GetPlayerObject(clientId);
            return playerObject != null && playerObject.TryGetComponent(out PlayerTeam playerTeam)
                ? playerTeam.Team
                : TeamType.None;
        }

        private PlayerGunGame FindPlayerGunGame(ulong clientId)
        {
            NetworkObject playerObject = GetPlayerObject(clientId);
            return playerObject != null && playerObject.TryGetComponent(out PlayerGunGame progress)
                ? progress
                : null;
        }

        private NetworkObject GetPlayerObject(ulong clientId)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
            {
                return null;
            }

            return NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
        }

        [ClientRpc]
        private void ShowKillFeedClientRpc(FixedString128Bytes killerName, FixedString128Bytes victimName, byte killerTeam, byte victimTeam)
        {
            GameHudController hud = GameHudController.Instance;
            if (hud != null)
            {
                hud.ShowKillFeed(
                    killerName.ToString(),
                    victimName.ToString(),
                    (TeamType)killerTeam,
                    (TeamType)victimTeam);
            }
        }
    }
}
