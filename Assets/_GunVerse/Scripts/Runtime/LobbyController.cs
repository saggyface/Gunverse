using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse
{
    public sealed class LobbyController : MonoBehaviour
    {
        private static Color SelectedModeColor => ArtPalette.Ui.Primary;
        private static Color UnselectedModeColor => ArtPalette.Ui.SurfaceRaised;

        [SerializeField] private TextMeshProUGUI joinCodeText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private Button ffaButton;
        [SerializeField] private Button tdmButton;
        [SerializeField] private Button gunGameButton;
        [SerializeField] private Button mapButton;
        [SerializeField] private Button testModeButton;
        [SerializeField] private Button fogButton;
        [SerializeField] private Slider fogSlider;
        [SerializeField] private TMP_InputField fogInput;
        [SerializeField] private TextMeshProUGUI fogValueText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI[] playerRows;

        private MultiplayerSessionManager sessionManager;
        private bool isLeaving;

        private void Awake()
        {
            WireButtons();
            WireFogSlider();
            UnlockCursor();
        }

        private void Start()
        {
            sessionManager = MultiplayerSessionManager.Instance;
            if (sessionManager == null)
            {
                SceneManager.LoadScene(MultiplayerSessionManager.MAIN_MENU_SCENE_NAME, LoadSceneMode.Single);
                return;
            }

            sessionManager.LobbyChanged += OnLobbyChanged;
            sessionManager.StatusChanged += OnStatusChanged;

            if (sessionManager.CurrentLobby != null)
            {
                PopulateLobby(sessionManager.CurrentLobby);
            }
        }

        private void OnDestroy()
        {
            if (sessionManager == null)
            {
                return;
            }

            sessionManager.LobbyChanged -= OnLobbyChanged;
            sessionManager.StatusChanged -= OnStatusChanged;
        }

        private void WireButtons()
        {
            ffaButton.onClick.AddListener(OnFfaClicked);
            tdmButton.onClick.AddListener(OnTdmClicked);
            gunGameButton.onClick.AddListener(OnGunGameClicked);
            mapButton.onClick.AddListener(OnMapClicked);
            if (testModeButton != null)
            {
                testModeButton.onClick.AddListener(OnTestModeClicked);
            }
            if (fogButton != null)
            {
                fogButton.onClick.AddListener(OnFogToggled);
            }
            startButton.onClick.AddListener(OnStartClicked);
            leaveButton.onClick.AddListener(OnLeaveClicked);
        }

        private void WireFogSlider()
        {
            if (fogSlider == null)
            {
                return;
            }

            fogSlider.minValue = 0f;
            fogSlider.maxValue = 1f;
            fogSlider.onValueChanged.AddListener(OnFogIntensityChanged);

            if (fogInput != null)
            {
                fogInput.onEndEdit.AddListener(OnFogInputEndEdit);
            }
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

        private void OnFfaClicked()
        {
            SetGameMode(GameModeType.FreeForAll);
        }

        private void OnTdmClicked()
        {
            SetGameMode(GameModeType.TeamDeathmatch);
        }

        private void OnGunGameClicked()
        {
            SetGameMode(GameModeType.GunGame);
        }

        private void OnMapClicked()
        {
            if (sessionManager == null)
            {
                return;
            }

            sessionManager.CycleSelectedMap();
            RefreshMapUi();
        }

        private void OnTestModeClicked()
        {
            if (sessionManager == null)
            {
                return;
            }

            sessionManager.ToggleTestMode();
            RefreshTestModeUi();
        }

        private void SetGameMode(GameModeType gameMode)
        {
            if (sessionManager == null)
            {
                return;
            }

            sessionManager.SetSelectedGameMode(gameMode);
            RefreshModeUi();
        }

        private void OnStartClicked()
        {
            if (sessionManager == null)
            {
                return;
            }

            statusText.text = "Starting match...";
            sessionManager.StartGame();
        }

        private void OnLeaveClicked()
        {
            if (isLeaving || sessionManager == null)
            {
                return;
            }

            isLeaving = true;
            startButton.interactable = false;
            leaveButton.interactable = false;
            statusText.text = "Leaving...";
            sessionManager.LeaveGame();
        }

        private void OnLobbyChanged(LobbyInfo info)
        {
            if (info == null)
            {
                if (!isLeaving)
                {
                    SceneManager.LoadScene(MultiplayerSessionManager.MAIN_MENU_SCENE_NAME, LoadSceneMode.Single);
                }

                return;
            }

            PopulateLobby(info);
        }

        private void OnStatusChanged(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void PopulateLobby(LobbyInfo info)
        {
            joinCodeText.text = "Join Code: " + info.JoinCode;
            playerCountText.text = $"Players: {info.PlayerCount} / {info.MaxPlayers}";

            bool isHost = info.IsHost;
            modeText.gameObject.SetActive(isHost);
            ffaButton.gameObject.SetActive(isHost);
            tdmButton.gameObject.SetActive(isHost);
            gunGameButton.gameObject.SetActive(isHost);
            mapButton.gameObject.SetActive(isHost);
            if (testModeButton != null)
            {
                testModeButton.gameObject.SetActive(isHost);
            }
            if (fogButton != null)
            {
                fogButton.gameObject.SetActive(isHost);
            }
            if (fogSlider != null)
            {
                fogSlider.gameObject.SetActive(isHost);
            }
            if (fogInput != null)
            {
                fogInput.gameObject.SetActive(isHost);
            }
            if (fogValueText != null)
            {
                fogValueText.gameObject.SetActive(isHost);
            }
            startButton.gameObject.SetActive(isHost);
            RefreshModeUi();
            RefreshMapUi();
            RefreshTestModeUi();
            RefreshFogUi();

            for (int i = 0; i < playerRows.Length; i++)
            {
                bool hasPlayer = i < info.PlayerNames.Count;
                playerRows[i].gameObject.SetActive(hasPlayer);

                if (hasPlayer)
                {
                    playerRows[i].text = info.PlayerNames[i];
                }
            }

            if (!isHost)
            {
                statusText.text = "Waiting for the host to start...";
            }
        }

        private void RefreshModeUi()
        {
            if (sessionManager == null ||
                modeText == null ||
                ffaButton == null ||
                tdmButton == null ||
                gunGameButton == null)
            {
                return;
            }

            GameModeType mode = sessionManager.SelectedGameMode;
            modeText.text = mode switch
            {
                GameModeType.TeamDeathmatch => "Mode: Team Deathmatch",
                GameModeType.GunGame => "Mode: Gun Game",
                _ => "Mode: Free For All"
            };
            ApplyModeButton(ffaButton, mode == GameModeType.FreeForAll);
            ApplyModeButton(tdmButton, mode == GameModeType.TeamDeathmatch);
            ApplyModeButton(gunGameButton, mode == GameModeType.GunGame);
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
            if (sessionManager == null || mapButton == null)
            {
                return;
            }

            TextMeshProUGUI label = mapButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "Map: " + GameplayMapCatalog.GetDisplayName(sessionManager.SelectedMap);
            }
        }

        private void RefreshTestModeUi()
        {
            if (sessionManager == null || testModeButton == null)
            {
                return;
            }

            TextMeshProUGUI label = testModeButton.GetComponentInChildren<TextMeshProUGUI>(true);
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

            if (fogButton != null)
            {
                TextMeshProUGUI label = fogButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = sessionManager.StoreFogEnabled ? "Store Fog: On" : "Store Fog: Off";
                }
            }

            if (fogSlider != null)
            {
                fogSlider.SetValueWithoutNotify(sessionManager.StoreFogIntensity);
                fogSlider.interactable = sessionManager.StoreFogEnabled;
            }

            int fogPercent = Mathf.RoundToInt(sessionManager.StoreFogIntensity * 100f);
            SetInputText(fogInput, fogValueText, fogPercent.ToString());

            if (fogInput != null)
            {
                fogInput.interactable = sessionManager.StoreFogEnabled;
            }
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

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
