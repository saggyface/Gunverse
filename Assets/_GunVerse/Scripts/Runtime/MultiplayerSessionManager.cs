using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse
{
    public class MultiplayerSessionManager : MonoBehaviour
    {
        private const int MAX_PLAYERS = 8;
        private const string SESSION_NAME = "GunVerse";
        private const string FALLBACK_PLAYER_NAME = "Player";
        private const float CLIENT_CONNECT_TIMEOUT_SECONDS = 15f;

        public const string MAIN_MENU_SCENE_NAME = "MainMenu";
        public const string LOBBY_SCENE_NAME = "Lobby";
        public const string GAMEPLAY_SCENE_NAME = "GroceryStore";

        public static MultiplayerSessionManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticInstance()
        {
            Instance = null;
        }

        public event Action<string> StatusChanged;
        public event Action<LobbyInfo> LobbyChanged;

        private bool isBusy;
        private bool isReturningToMenu;
        private bool networkReturnScheduled;
        private bool networkEventsSubscribed;
        private NetworkManager subscribedNetworkManager;
        private Task initializationTask;
        private IHostSession hostSession;

        public ISession CurrentSession { get; private set; }

        public LobbyInfo CurrentLobby { get; private set; }

        public GameModeType SelectedGameMode { get; private set; } = GameModeType.FreeForAll;

        public MatchRules SelectedMatchRules { get; private set; } = new MatchRules();

        public GameplayMapType SelectedMap { get; private set; } = GameplayMapType.GroceryStore;

        public GameplayMapType ActiveMap { get; private set; } = GameplayMapType.GroceryStore;

        public bool TestModeEnabled { get; private set; }

        public bool StoreFogEnabled { get; private set; }

        public float StoreFogIntensity { get; private set; } = StoreFogController.DEFAULT_INTENSITY;

        public bool IsHost
        {
            get
            {
                if (CurrentSession == null || NetworkManager.Singleton == null)
                {
                    return false;
                }

                return NetworkManager.Singleton.IsHost;
            }
        }

        public void SetSelectedGameMode(GameModeType gameMode)
        {
            if (CurrentSession != null && CurrentSession.IsHost)
            {
                SelectedGameMode = gameMode;
            }
        }

        public void SetSelectedMatchRules(MatchRules matchRules)
        {
            if (matchRules == null || CurrentSession == null || !CurrentSession.IsHost)
            {
                return;
            }

            SelectedMatchRules.CopyFrom(matchRules);
        }

        public void SetSelectedMap(GameplayMapType map)
        {
            if (CurrentSession != null && CurrentSession.IsHost && GameplayMapCatalog.IsValid(map))
            {
                SelectedMap = map;
            }
        }

        public void CycleSelectedMap()
        {
            if (CurrentSession == null || !CurrentSession.IsHost)
            {
                return;
            }

            SelectedMap = GameplayMapCatalog.GetNext(SelectedMap);
        }

        public void SetTestModeEnabled(bool enabled)
        {
            if (CurrentSession != null && CurrentSession.IsHost)
            {
                TestModeEnabled = enabled;
            }
        }

        public void ToggleTestMode()
        {
            SetTestModeEnabled(!TestModeEnabled);
        }

        public void SetStoreFogEnabled(bool enabled)
        {
            if (CurrentSession != null && CurrentSession.IsHost)
            {
                StoreFogEnabled = enabled;
            }
        }

        public void ToggleStoreFog()
        {
            SetStoreFogEnabled(!StoreFogEnabled);
        }

        public void SetStoreFogIntensity(float intensity)
        {
            if (CurrentSession != null && CurrentSession.IsHost)
            {
                StoreFogIntensity = Mathf.Clamp01(intensity);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            UnsubscribeFromNetworkEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!networkEventsSubscribed && !isReturningToMenu)
            {
                SubscribeToNetworkEvents();
            }
        }

        public async void HostGame()
        {
            if (!TryBeginOperation())
            {
                return;
            }

            try
            {
                SelectedGameMode = GameModeType.FreeForAll;
                SelectedMap = GameplayMapType.GroceryStore;
                ActiveMap = GameplayMapType.GroceryStore;
                TestModeEnabled = false;
                StoreFogEnabled = false;
                StoreFogIntensity = StoreFogController.DEFAULT_INTENSITY;
                ReportStatus("Connecting...");
                await EnsureInitializedAsync();
                await SetPlayerNameAsync();

                SessionOptions options = new SessionOptions
                {
                    Name = SESSION_NAME,
                    MaxPlayers = MAX_PLAYERS,
                    IsPrivate = true
                }.WithPlayerName(VisibilityPropertyOptions.Member);

                hostSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                CurrentSession = hostSession;

                SubscribeToSession();
                PublishLobby();
            }
            catch (Exception exception)
            {
                hostSession = null;
                CurrentSession = null;
                ReportStatus($"Host failed: {exception.Message}");
                Debug.LogException(exception);
            }
            finally
            {
                isBusy = false;
            }
        }

        public async void JoinGame(string joinCode)
        {
            if (!TryBeginOperation())
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(joinCode))
                {
                    ReportStatus("Enter a join code.");
                    return;
                }

                ReportStatus("Joining...");
                await EnsureInitializedAsync();
                await SetPlayerNameAsync();

                JoinSessionOptions options = new JoinSessionOptions().WithPlayerName(VisibilityPropertyOptions.Member);
                CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode, options);

                SubscribeToSession();
                PublishLobby();
            }
            catch (Exception exception)
            {
                CurrentSession = null;
                ReportStatus($"Join failed: {exception.Message}");
                Debug.LogException(exception);
            }
            finally
            {
                isBusy = false;
            }
        }

        public async void StartGame()
        {
            if (hostSession == null || isBusy)
            {
                return;
            }

            isBusy = true;
            try
            {
                ReportStatus("Starting game...");

                NetworkManager networkManager = NetworkManager.Singleton;
                if (networkManager == null)
                {
                    ReportStatus("Network manager is unavailable.");
                    return;
                }

                if (!networkManager.IsListening)
                {
                    await hostSession.Network.StartRelayNetworkAsync(RelayNetworkOptions.Default);
                }

                await WaitForClientsBeforeSceneLoadAsync(networkManager);

                ActiveMap = SelectedMap;
                LoadGameplayScene();
            }
            catch (Exception exception)
            {
                ReportStatus($"Start failed: {exception.Message}");
                Debug.LogException(exception);
            }
            finally
            {
                isBusy = false;
            }
        }

        private async Task WaitForClientsBeforeSceneLoadAsync(NetworkManager networkManager)
        {
            int expectedPlayers = CurrentSession != null
                ? Mathf.Max(1, CurrentSession.PlayerCount)
                : Mathf.Max(1, networkManager.ConnectedClientsIds.Count);

            if (expectedPlayers <= 1)
            {
                return;
            }

            float deadline = Time.realtimeSinceStartup + CLIENT_CONNECT_TIMEOUT_SECONDS;
            while (networkManager != null &&
                   networkManager.IsListening &&
                   networkManager.ConnectedClientsIds.Count < expectedPlayers &&
                   Time.realtimeSinceStartup < deadline)
            {
                ReportStatus($"Waiting for players... ({networkManager.ConnectedClientsIds.Count}/{expectedPlayers})");
                await Task.Delay(100);
            }

            if (networkManager == null || networkManager.ConnectedClientsIds.Count < expectedPlayers)
            {
                Debug.LogWarning("[MultiplayerSessionManager] Timed out waiting for all session players to connect.");
            }
        }

        public void ReloadGameplayScene()
        {
            if (!IsHost)
            {
                return;
            }

            LoadNetworkScene(GameplayMapCatalog.GetSceneName(ActiveMap));
        }

        public void ReturnToLobby()
        {
            if (!IsHost)
            {
                return;
            }

            LoadNetworkScene(LOBBY_SCENE_NAME);
        }

        public async void LeaveLobby()
        {
            if (CurrentSession == null || isBusy)
            {
                return;
            }

            isBusy = true;
            try
            {
                await CurrentSession.LeaveAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                UnsubscribeFromSession();
                hostSession = null;
                CurrentSession = null;
                isBusy = false;
                PublishLobby();
            }
        }

        private bool TryBeginOperation()
        {
            if (isBusy)
            {
                return false;
            }

            isBusy = true;
            return true;
        }

        private Task EnsureInitializedAsync()
        {
            if (initializationTask != null && !initializationTask.IsFaulted)
            {
                return initializationTask;
            }

            initializationTask = InitializeAsync();
            return initializationTask;
        }

        private async Task InitializeAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();

                if (AuthenticationService.Instance.IsSignedIn)
                {
                    return;
                }

                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                catch (AuthenticationException) when (AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.LogWarning("[MultiplayerSessionManager] Sign-in reported an error, but the player is already signed in.");
                }
            }
            catch
            {
                initializationTask = null;
                throw;
            }
        }

        private async Task SetPlayerNameAsync()
        {
            string playerName = GameSettings.NormalizePlayerName(GameSettings.PlayerName);
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = FALLBACK_PLAYER_NAME;
            }

            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
        }

        private void SubscribeToSession()
        {
            if (CurrentSession == null)
            {
                return;
            }

            CurrentSession.Changed += OnSessionChanged;
            CurrentSession.PlayerJoined += OnPlayerJoined;
            CurrentSession.PlayerHasLeft += OnPlayerHasLeft;
            CurrentSession.PlayerPropertiesChanged += OnSessionChanged;
            CurrentSession.RemovedFromSession += OnRemovedFromSession;
            CurrentSession.Deleted += OnSessionDeleted;
        }

        private void UnsubscribeFromSession()
        {
            if (CurrentSession == null)
            {
                return;
            }

            CurrentSession.Changed -= OnSessionChanged;
            CurrentSession.PlayerJoined -= OnPlayerJoined;
            CurrentSession.PlayerHasLeft -= OnPlayerHasLeft;
            CurrentSession.PlayerPropertiesChanged -= OnSessionChanged;
            CurrentSession.RemovedFromSession -= OnRemovedFromSession;
            CurrentSession.Deleted -= OnSessionDeleted;
        }

        private void SubscribeToNetworkEvents()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnTransportFailure += OnNetworkTransportFailure;
            networkManager.OnClientDisconnectCallback += OnNetworkClientDisconnected;
            subscribedNetworkManager = networkManager;
            networkEventsSubscribed = true;
        }

        private void UnsubscribeFromNetworkEvents()
        {
            if (subscribedNetworkManager != null)
            {
                subscribedNetworkManager.OnTransportFailure -= OnNetworkTransportFailure;
                subscribedNetworkManager.OnClientDisconnectCallback -= OnNetworkClientDisconnected;
            }

            subscribedNetworkManager = null;
            networkEventsSubscribed = false;
        }

        private void OnNetworkClientDisconnected(ulong clientId)
        {
            if (isReturningToMenu || isBusy)
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.IsServer || clientId != networkManager.LocalClientId)
            {
                return;
            }

            Debug.LogWarning("[MultiplayerSessionManager] Connection to the host was lost. Returning to the main menu.");
            RequestReturnToMenu();
        }

        private void OnNetworkTransportFailure()
        {
            if (isReturningToMenu || isBusy)
            {
                return;
            }

            Debug.LogWarning("[MultiplayerSessionManager] Network transport failed. Returning to the main menu.");
            RequestReturnToMenu();
        }

        private void RequestReturnToMenu()
        {
            if (isReturningToMenu || networkReturnScheduled)
            {
                return;
            }

            networkReturnScheduled = true;
            StartCoroutine(ReturnToMenuAfterFrame());
        }

        private IEnumerator ReturnToMenuAfterFrame()
        {
            yield return null;
            networkReturnScheduled = false;
            ReturnToMenu();
        }

        private void OnSessionChanged()
        {
            PublishLobby();
        }

        private void OnPlayerJoined(string playerId)
        {
            PublishLobby();
        }

        private void OnPlayerHasLeft(string playerId)
        {
            PublishLobby();
        }

        public async void LeaveGame()
        {
            if (!TryBeginOperation())
            {
                return;
            }

            try
            {
                await LeaveCurrentSessionAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                isBusy = false;
            }

            ReturnToMenu();
        }

        private async Task LeaveCurrentSessionAsync()
        {
            if (CurrentSession == null)
            {
                return;
            }

            await CurrentSession.LeaveAsync();
        }

        private void OnRemovedFromSession()
        {
            if (isBusy)
            {
                return;
            }

            ReturnToMenu();
        }

        private void OnSessionDeleted()
        {
            if (isBusy)
            {
                return;
            }

            ReturnToMenu();
        }

        private void ReturnToMenu()
        {
            if (isReturningToMenu)
            {
                return;
            }

            isReturningToMenu = true;

            UnsubscribeFromNetworkEvents();
            UnsubscribeFromSession();

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                networkManager.Shutdown();
            }

            hostSession = null;
            CurrentSession = null;
            CurrentLobby = null;
            isBusy = false;

            if (networkManager != null)
            {
                UnityEngine.Object.DestroyImmediate(networkManager.gameObject);
            }

            SceneManager.LoadScene(MAIN_MENU_SCENE_NAME, LoadSceneMode.Single);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        private void PublishLobby()
        {
            if (CurrentSession == null)
            {
                CurrentLobby = null;
                LobbyChanged?.Invoke(null);
                return;
            }

            List<string> playerNames = new List<string>();
            foreach (IReadOnlyPlayer player in CurrentSession.Players)
            {
                string playerName = GameSettings.NormalizePlayerName(player.GetPlayerName());
                playerNames.Add(string.IsNullOrWhiteSpace(playerName) ? FALLBACK_PLAYER_NAME : playerName);
            }

            CurrentLobby = new LobbyInfo(
                playerNames,
                CurrentSession.PlayerCount,
                CurrentSession.MaxPlayers,
                CurrentSession.Code,
                CurrentSession.IsHost);

            LobbyChanged?.Invoke(CurrentLobby);
        }

        private void LoadGameplayScene()
        {
            LoadNetworkScene(GameplayMapCatalog.GetSceneName(ActiveMap));
        }

        private void LoadNetworkScene(string sceneName)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.SceneManager == null)
            {
                ReportStatus("Network manager is unavailable.");
                return;
            }

            SceneEventProgressStatus status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            if (status != SceneEventProgressStatus.Started)
            {
                ReportStatus($"Failed to load {sceneName}: {status}");
                Debug.LogError($"[MultiplayerSessionManager] Failed to load {sceneName}. Status: {status}");
            }
        }

        private void ReportStatus(string message)
        {
            StatusChanged?.Invoke(message);
            Debug.Log($"[MultiplayerSessionManager] {message}");
        }
    }
}
