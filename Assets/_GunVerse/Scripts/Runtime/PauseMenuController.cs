using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GunVerse
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject confirmPanel;

        [Header("Pause Panel")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button leaveGameButton;

        [Header("Settings Panel")]
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_InputField sensitivityInput;
        [SerializeField] private TMP_InputField volumeInput;
        [SerializeField] private TextMeshProUGUI sensitivityValueText;
        [SerializeField] private TextMeshProUGUI volumeValueText;

        [Header("Confirm Panel")]
        [SerializeField] private TextMeshProUGUI confirmText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private bool isPaused;

        private void Awake()
        {
            WireButtons();
            WireSliders();
        }

        private void Start()
        {
            LockCursor();
        }

        private void OnDestroy()
        {
            UnlockCursor();
        }

        private void Update()
        {
            if (MatchResultsController.IsResultsVisible)
            {
                return;
            }

            if (IsInputFieldFocused())
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        private static bool IsInputFieldFocused()
        {
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            return selected != null && selected.GetComponentInParent<TMP_InputField>() != null;
        }

        private void WireButtons()
        {
            resumeButton.onClick.AddListener(Resume);
            settingsButton.onClick.AddListener(ShowSettingsPanel);
            leaveGameButton.onClick.AddListener(OnLeaveClicked);

            settingsBackButton.onClick.AddListener(ShowPausePanel);

            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelButton.onClick.AddListener(ShowPausePanel);
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
        }

        private void TogglePause()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        private void Pause()
        {
            isPaused = true;
            ShowPausePanel();
            UnlockCursor();
        }

        private void Resume()
        {
            isPaused = false;
            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
            confirmPanel.SetActive(false);
            LockCursor();
        }

        private void OnLeaveClicked()
        {
            bool isHost = MultiplayerSessionManager.Instance != null && MultiplayerSessionManager.Instance.IsHost;
            confirmText.text = isHost
                ? "Leaving will end the game for everyone. Leave game?"
                : "Leave the game?";

            pausePanel.SetActive(false);
            confirmPanel.SetActive(true);
        }

        private void OnConfirmClicked()
        {
            if (MultiplayerSessionManager.Instance != null)
            {
                MultiplayerSessionManager.Instance.LeaveGame();
            }
        }

        private void ShowPausePanel()
        {
            pausePanel.SetActive(true);
            settingsPanel.SetActive(false);
            confirmPanel.SetActive(false);
        }

        private void ShowSettingsPanel()
        {
            ApplyStoredSettingsToSliders();
            pausePanel.SetActive(false);
            settingsPanel.SetActive(true);
            confirmPanel.SetActive(false);
        }

        private void ApplyStoredSettingsToSliders()
        {
            sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
            volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            RefreshSettingLabels();
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

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
