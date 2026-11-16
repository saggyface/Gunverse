using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GunVerse
{
    public class MainMenuController : MonoBehaviour
    {
        private static Color SelectedModeColor => ArtPalette.Ui.Primary;
        private static Color UnselectedModeColor => ArtPalette.Ui.SurfaceRaised;

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject usernamePanel;
        [SerializeField] private GameObject hostJoinPanel;
        [SerializeField] private GameObject joinCodePanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main Panel")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Username Panel")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private Button usernameContinueButton;
        [SerializeField] private Button usernameBackButton;
        [SerializeField] private TextMeshProUGUI usernameErrorText;

        [Header("Host/Join Panel")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button hostJoinBackButton;
        [SerializeField] private TextMeshProUGUI hostJoinStatusText;

        [Header("Join Code Panel")]
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button joinCodeBackButton;
        [SerializeField] private TextMeshProUGUI joinCodeStatusText;

        [Header("Lobby Panel")]
        [SerializeField] private TextMeshProUGUI lobbyJoinCodeText;
        [SerializeField] private TextMeshProUGUI lobbyPlayerCountText;
        [SerializeField] private TextMeshProUGUI lobbyModeText;
        [SerializeField] private Button lobbyFfaButton;
        [SerializeField] private Button lobbyTdmButton;
        [SerializeField] private Button lobbyGunGameButton;
        [SerializeField] private Button lobbyMapButton;
        [SerializeField] private Button lobbyTestModeButton;
        [SerializeField] private Button lobbyFogButton;
        [SerializeField] private Slider lobbyFogSlider;
        [SerializeField] private TMP_InputField lobbyFogInput;
        [SerializeField] private TextMeshProUGUI lobbyFogValueText;
        [SerializeField] private Button lobbyStartButton;
        [SerializeField] private Button lobbyLeaveButton;
        [SerializeField] private TextMeshProUGUI lobbyStatusText;
        [SerializeField] private TextMeshProUGUI[] lobbyPlayerRows;

        [Header("Settings Panel")]
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_InputField sensitivityInput;
        [SerializeField] private TMP_InputField volumeInput;
        [SerializeField] private TextMeshProUGUI sensitivityValueText;
        [SerializeField] private TextMeshProUGUI volumeValueText;

        [Header("Multiplayer")]
        [SerializeField] private MultiplayerSessionManager sessionManager;

        private TextMeshProUGUI activeStatusText;

        private void Awake()
        {
            WireButtons();
            WireSliders();
            WireMultiplayer();
            ApplyStoredSettings();
            ShowMainPanel();
        }

        private void OnDestroy()
        {
            if (sessionManager != null)
            {
                sessionManager.StatusChanged -= OnSessionStatusChanged;
                sessionManager.LobbyChanged -= OnLobbyChanged;
            }
        }

        private void WireButtons()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            settingsButton.onClick.AddListener(ShowSettingsPanel);
            quitButton.onClick.AddListener(QuitGame);

            usernameContinueButton.onClick.AddListener(OnUsernameContinueClicked);
            usernameBackButton.onClick.AddListener(ShowMainPanel);

            hostButton.onClick.AddListener(OnHostClicked);
            joinButton.onClick.AddListener(ShowJoinCodePanel);
            hostJoinBackButton.onClick.AddListener(ShowUsernamePanel);

            joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
            joinCodeBackButton.onClick.AddListener(ShowHostJoinPanel);

            lobbyFfaButton.onClick.AddListener(OnFfaModeClicked);
            lobbyTdmButton.onClick.AddListener(OnTdmModeClicked);
            lobbyGunGameButton.onClick.AddListener(OnGunGameModeClicked);
            lobbyMapButton.onClick.AddListener(OnMapClicked);
            if (lobbyTestModeButton != null)
            {
                lobbyTestModeButton.onClick.AddListener(OnTestModeClicked);
            }
            if (lobbyFogButton != null)
            {
                lobbyFogButton.onClick.AddListener(OnFogToggled);
            }
            lobbyStartButton.onClick.AddListener(OnStartClicked);
            lobbyLeaveButton.onClick.AddListener(OnLeaveClicked);

            settingsBackButton.onClick.AddListener(ShowMainPanel);
        }

        private void WireSliders()
        {
            sensitivitySlider.minValue = GameSettings.SENSITIVITY_MIN;
            sensitivitySlider.maxValue = GameSettings.SENSITIVITY_MAX;
            volumeSlider.minValue = GameSettings.MASTER_VOLUME_MIN;
            volumeSlider.maxValue = GameSettings.MASTER_VOLUME_MAX;

            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

            if (sensitivityInput != null)
            {
                sensitivityInput.onEndEdit.AddListener(OnSensitivityInputEndEdit);
            }

            if (volumeInput != null)
            {
                volumeInput.onEndEdit.AddListener(OnVolumeInputEndEdit);
            }

            if (lobbyFogSlider != null)
            {
                lobbyFogSlider.minValue = 0f;
                lobbyFogSlider.maxValue = 1f;
                lobbyFogSlider.onValueChanged.AddListener(OnFogIntensityChanged);
            }

            if (lobbyFogInput != null)
            {
                lobbyFogInput.onEndEdit.AddListener(OnFogInputEndEdit);
            }
        }

        private void WireMultiplayer()
        {
            if (sessionManager == null)
            {
                return;
            }

            sessionManager.StatusChanged += OnSessionStatusChanged;
            sessionManager.LobbyChanged += OnLobbyChanged;
        }

        private void ApplyStoredSettings()
        {
            sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
            volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            RefreshSettingLabels();
            GameSettings.ApplyMasterVolume();
        }

        private void OnSensitivityChanged(float value)
        {
            GameSettings.Sensitivity = value;
            RefreshSettingLabels();
        }

        private void OnVolumeChanged(float value)
        {
            GameSettings.MasterVolume = value;
            RefreshSettingLabels();
        }

        private void RefreshSettingLabels()
        {
            SetInputText(sensitivityInput, sensitivityValueText, sensitivitySlider.value.ToString("0.00"));
            SetInputText(volumeInput, volumeValueText, Mathf.RoundToInt(volumeSlider.value * 100f).ToString());
        }

        private void OnSensitivityInputEndEdit(string text)
        {
            if (float.TryParse(text, out float value))
            {
                value = Mathf.Clamp(value, GameSettings.SENSITIVITY_MIN, GameSettings.SENSITIVITY_MAX);
                sensitivitySlider.SetValueWithoutNotify(value);
                GameSettings.Sensitivity = value;
            }

            RefreshSettingLabels();
        }

        private void OnVolumeInputEndEdit(string text)
        {
            if (int.TryParse(text, out int percent))
            {
                float value = Mathf.Clamp01(percent / 100f);
                volumeSlider.SetValueWithoutNotify(value);
                GameSettings.MasterVolume = value;
            }

            RefreshSettingLabels();
        }

        private void OnFogInputEndEdit(string text)
        {
            if (sessionManager != null && int.TryParse(text, out int percent))
            {
                sessionManager.SetStoreFogIntensity(Mathf.Clamp01(percent / 100f));
            }

            RefreshFogUi();
        }

        private static void SetInputText(TMP_InputField input, TextMeshProUGUI fallback, string text)
        {
            if (input != null)
            {
                input.SetTextWithoutNotify(text);
                return;
            }

            if (fallback != null)
            {
                fallback.text = text;
            }
        }

        private void OnPlayClicked()
        {
            usernameInput.text = GameSettings.NormalizePlayerName(GameSettings.PlayerName);
            usernameErrorText.text = string.Empty;
            ShowUsernamePanel();
        }

        private void OnUsernameContinueClicked()
        {
            string username = usernameInput.text.Trim();
            if (username.Length == 0)
            {
                usernameErrorText.text = "Enter a username.";
                return;
            }

            if (username.Length > GameSettings.PLAYER_NAME_MAX_LENGTH)
            {
                usernameErrorText.text = $"Username must be {GameSettings.PLAYER_NAME_MAX_LENGTH} characters or fewer.";
                return;
            }

            usernameErrorText.text = string.Empty;
            usernameInput.text = username;
            GameSettings.PlayerName = username;
            ShowHostJoinPanel();
        }

        private void OnHostClicked()
        {
            activeStatusText = hostJoinStatusText;
            hostJoinStatusText.text = string.Empty;
            sessionManager.HostGame();
        }

        private void OnJoinLobbyClicked()
        {
            activeStatusText = joinCodeStatusText;
            joinCodeStatusText.text = string.Empty;
            sessionManager.JoinGame(joinCodeInput.text);
        }

        private void OnFfaModeClicked()
        {
            sessionManager.SetSelectedGameMode(GameModeType.FreeForAll);
            RefreshModeUi();
        }

        private void OnTdmModeClicked()
        {
            sessionManager.SetSelectedGameMode(GameModeType.TeamDeathmatch);
            RefreshModeUi();
        }

        private void OnGunGameModeClicked()
        {
            sessionManager.SetSelectedGameMode(GameModeType.GunGame);
            RefreshModeUi();
        }

        private void OnMapClicked()
        {
            sessionManager.CycleSelectedMap();
            RefreshMapUi();
        }

        private void OnTestModeClicked()
        {
            sessionManager.ToggleTestMode();
            RefreshTestModeUi();
        }

        private void OnFogToggled()
        {
            if (sessionManager == null)
            {
                return;
            }

            sessionManager.ToggleStoreFog();
            RefreshFogUi();
        }

        private void OnFogIntensityChanged(float value)
        {
            if (sessionManager == null)
            {
                return;
            }

            sessionManager.SetStoreFogIntensity(value);
            RefreshFogUi();
        }

        private void OnStartClicked()
        {
            activeStatusText = lobbyStatusText;
            lobbyStatusText.text = string.Empty;
            sessionManager.StartGame();
        }

        private void OnLeaveClicked()
        {
            activeStatusText = null;
            sessionManager.LeaveLobby();
        }

        private void OnSessionStatusChanged(string message)
        {
            if (activeStatusText != null)
            {
                activeStatusText.text = message;
            }
        }

        private void OnLobbyChanged(LobbyInfo info)
        {
            if (info == null)
            {
                ShowHostJoinPanel();
                return;
            }

            PopulateLobby(info);
            ShowLobbyPanel();
        }

        private void PopulateLobby(LobbyInfo info)
        {
            lobbyJoinCodeText.text = "Join Code: " + info.JoinCode;
            lobbyPlayerCountText.text = $"Players: {info.PlayerCount} / {info.MaxPlayers}";

            bool isHost = info.IsHost;
            lobbyModeText.gameObject.SetActive(isHost);
            lobbyFfaButton.gameObject.SetActive(isHost);
            lobbyTdmButton.gameObject.SetActive(isHost);
            lobbyGunGameButton.gameObject.SetActive(isHost);
            lobbyMapButton.gameObject.SetActive(isHost);
            if (lobbyTestModeButton != null)
            {
                lobbyTestModeButton.gameObject.SetActive(isHost);
            }
            if (lobbyFogButton != null)
            {
                lobbyFogButton.gameObject.SetActive(isHost);
            }
            if (lobbyFogSlider != null)
            {
                lobbyFogSlider.gameObject.SetActive(isHost);
            }
            if (lobbyFogInput != null)
            {
                lobbyFogInput.gameObject.SetActive(isHost);
            }
            if (lobbyFogValueText != null)
            {
                lobbyFogValueText.gameObject.SetActive(isHost);
            }
            RefreshModeUi();
            RefreshMapUi();
            RefreshTestModeUi();
            RefreshFogUi();

            for (int i = 0; i < lobbyPlayerRows.Length; i++)
            {
                bool hasPlayer = i < info.PlayerNames.Count;
                lobbyPlayerRows[i].gameObject.SetActive(hasPlayer);

                if (hasPlayer)
                {
                    lobbyPlayerRows[i].text = info.PlayerNames[i];
                }
            }

            lobbyStartButton.gameObject.SetActive(info.IsHost);
        }

        private void RefreshModeUi()
        {
            if (sessionManager == null ||
                lobbyModeText == null ||
                lobbyFfaButton == null ||
                lobbyTdmButton == null ||
                lobbyGunGameButton == null)
            {
                return;
            }

            GameModeType mode = sessionManager.SelectedGameMode;
            lobbyModeText.text = mode switch
            {
                GameModeType.TeamDeathmatch => "Mode: Team Deathmatch",
                GameModeType.GunGame => "Mode: Gun Game",
                _ => "Mode: Free For All"
            };
            ApplyModeButton(lobbyFfaButton, mode == GameModeType.FreeForAll);
            ApplyModeButton(lobbyTdmButton, mode == GameModeType.TeamDeathmatch);
            ApplyModeButton(lobbyGunGameButton, mode == GameModeType.GunGame);
        }

        private static void ApplyModeButton(Button button, bool isSelected)
        {
            if (button == null)
            {
                return;
            }

            button.image.color = isSelected ? SelectedModeColor : UnselectedModeColor;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = isSelected ? ArtPalette.Ui.Ink : ArtPalette.Ui.Cream;
            }
        }

        private void RefreshMapUi()
        {
            if (sessionManager == null || lobbyMapButton == null)
            {
                return;
            }

            TextMeshProUGUI label = lobbyMapButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "Map: " + GameplayMapCatalog.GetDisplayName(sessionManager.SelectedMap);
            }
        }

        private void RefreshTestModeUi()
        {
            if (sessionManager == null || lobbyTestModeButton == null)
            {
                return;
            }

            TextMeshProUGUI label = lobbyTestModeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = sessionManager.TestModeEnabled ? "Test Mode: On" : "Test Mode: Off";
            }
        }

        private void RefreshFogUi()
        {
            if (sessionManager == null)
            {
                return;
            }

            if (lobbyFogButton != null)
            {
                TextMeshProUGUI label = lobbyFogButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = sessionManager.StoreFogEnabled ? "Store Fog: On" : "Store Fog: Off";
                }
            }

            if (lobbyFogSlider != null)
            {
                lobbyFogSlider.SetValueWithoutNotify(sessionManager.StoreFogIntensity);
                lobbyFogSlider.interactable = sessionManager.StoreFogEnabled;
            }

            int fogPercent = Mathf.RoundToInt(sessionManager.StoreFogIntensity * 100f);
            SetInputText(lobbyFogInput, lobbyFogValueText, fogPercent.ToString());

            if (lobbyFogInput != null)
            {
                lobbyFogInput.interactable = sessionManager.StoreFogEnabled;
            }
        }

        private void ShowMainPanel() => ShowPanel(mainPanel);

        private void ShowUsernamePanel() => ShowPanel(usernamePanel);

        private void ShowHostJoinPanel() => ShowPanel(hostJoinPanel);

        private void ShowJoinCodePanel() => ShowPanel(joinCodePanel);

        private void ShowLobbyPanel() => ShowPanel(lobbyPanel);

        private void ShowSettingsPanel() => ShowPanel(settingsPanel);

        private void ShowPanel(GameObject panelToShow)
        {
            mainPanel.SetActive(panelToShow == mainPanel);
            usernamePanel.SetActive(panelToShow == usernamePanel);
            hostJoinPanel.SetActive(panelToShow == hostJoinPanel);
            joinCodePanel.SetActive(panelToShow == joinCodePanel);
            lobbyPanel.SetActive(panelToShow == lobbyPanel);
            settingsPanel.SetActive(panelToShow == settingsPanel);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
