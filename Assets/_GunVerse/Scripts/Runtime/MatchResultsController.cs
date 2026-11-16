using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace GunVerse
{
    public sealed class MatchResultsController : MonoBehaviour
    {
        private const float REFRESH_INTERVAL = 0.2f;
        private const int FFA_RESULT_ROWS = 3;
        private static string PlayerNameColorHex => ArtPalette.ToHtmlRgb(ArtPalette.Ui.Primary);

        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultReasonText;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button rematchButton;
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TextMeshProUGUI waitingText;
        [SerializeField] private TextMeshProUGUI statusText;

        private readonly List<PlayerScore> scores = new List<PlayerScore>();
        private readonly List<PlayerGunGame> gunGameScores = new List<PlayerGunGame>();

        private bool isVisible;
        private bool isReturning;
        private float nextRefreshTime;

        public static bool IsResultsVisible { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsResultsVisible = false;
        }

        private void Awake()
        {
            WireButtons();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            UnwireButtons();

            if (IsResultsVisible)
            {
                IsResultsVisible = false;
            }
        }

        private void Update()
        {
            GameModeController gameMode = GameModeController.Instance;
            bool shouldShow = gameMode != null && gameMode.Phase == MatchPhase.Results;

            if (shouldShow != isVisible)
            {
                SetVisible(shouldShow);
            }

            if (isVisible && Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + REFRESH_INTERVAL;
                RefreshResults(gameMode);
            }
        }

        private void WireButtons()
        {
            if (rematchButton != null)
            {
                rematchButton.onClick.AddListener(OnRematchClicked);
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void UnwireButtons()
        {
            if (rematchButton != null)
            {
                rematchButton.onClick.RemoveListener(OnRematchClicked);
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.RemoveListener(OnReturnToLobbyClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
            }
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            IsResultsVisible = visible;

            if (resultsPanel != null)
            {
                resultsPanel.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            isReturning = false;
            nextRefreshTime = 0f;
            SetButtonsInteractable(true);
            UnlockCursor();
            ConfigureHostControls();

            GameHudController hud = GameHudController.Instance;
            if (hud != null)
            {
                hud.HidePrompt();
                hud.HideHeldItem();
                hud.HideAmmo();
            }

            if (statusText != null)
            {
                statusText.text = string.Empty;
            }
        }

        private void ConfigureHostControls()
        {
            bool isHost = MultiplayerSessionManager.Instance != null && MultiplayerSessionManager.Instance.IsHost;

            if (rematchButton != null)
            {
                rematchButton.gameObject.SetActive(isHost);
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.gameObject.SetActive(isHost);
            }

            if (waitingText != null)
            {
                waitingText.gameObject.SetActive(!isHost);
            }
        }

        private void RefreshResults(GameModeController gameMode)
        {
            if (gameMode == null)
            {
                return;
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = "MATCH OVER";
            }

            if (resultReasonText != null)
            {
                resultReasonText.text = gameMode.EndReason switch
                {
                    MatchEndReason.ScoreLimit => "SCORE LIMIT REACHED",
                    MatchEndReason.TimeExpired => "TIME EXPIRED",
                    MatchEndReason.LadderComplete => "LADDER COMPLETE",
                    _ => string.Empty
                };
            }

            if (winnerText != null)
            {
                winnerText.text = BuildWinnerText(gameMode);
            }

            if (scoreText != null)
            {
                scoreText.text = BuildScoreText(gameMode);
            }
        }

        private string BuildWinnerText(GameModeController gameMode)
        {
            if (gameMode.IsDraw)
            {
                return "DRAW";
            }

            if (gameMode.Mode == GameModeType.TeamDeathmatch)
            {
                TeamType winnerTeam = gameMode.WinnerTeam;
                if (winnerTeam == TeamType.None)
                {
                    return "DRAW";
                }

                return $"<color=#{TeamPalette.GetColorHex(winnerTeam)}>" +
                       $"{TeamPalette.GetName(winnerTeam)} TEAM WINS</color>";
            }

            string winnerName = FindWinnerName(gameMode.WinningClientId);
            return winnerName == null
                ? "MATCH COMPLETE"
                : $"<color=#{PlayerNameColorHex}>{winnerName} WINS</color>";
        }

        private string BuildScoreText(GameModeController gameMode)
        {
            if (gameMode.Mode == GameModeType.TeamDeathmatch)
            {
                return $"<color=#{TeamPalette.GetColorHex(TeamType.Blue)}>BLUE {gameMode.BlueScore}</color>" +
                       $"   -   " +
                       $"<color=#{TeamPalette.GetColorHex(TeamType.Red)}>RED {gameMode.RedScore}</color>";
            }

            if (gameMode.Mode == GameModeType.GunGame)
            {
                return BuildGunGameScoreText(gameMode);
            }

            CollectScores(gameMode.Mode);
            if (scores.Count == 0)
            {
                return "NO SCORES";
            }

            List<string> lines = new List<string>();
            int rowCount = Mathf.Min(FFA_RESULT_ROWS, scores.Count);
            for (int i = 0; i < rowCount; i++)
            {
                PlayerScore score = scores[i];
                string name = string.IsNullOrWhiteSpace(score.DisplayName) ? "Player" : score.DisplayName;
                lines.Add($"{i + 1}. {name} - {score.Kills} kills");
            }

            return string.Join("\n", lines);
        }

        private string BuildGunGameScoreText(GameModeController gameMode)
        {
            CollectGunGameScores();
            if (gunGameScores.Count == 0)
            {
                return "NO SCORES";
            }

            int total = Mathf.Max(1, gameMode.GunGameStepCount);
            List<string> lines = new List<string>();
            int rowCount = Mathf.Min(FFA_RESULT_ROWS, gunGameScores.Count);
            for (int i = 0; i < rowCount; i++)
            {
                PlayerGunGame progress = gunGameScores[i];
                string name = progress.TryGetComponent(out PlayerScore score) &&
                              !string.IsNullOrWhiteSpace(score.DisplayName)
                    ? score.DisplayName
                    : "Player";
                int level = Mathf.Clamp(progress.Level, 0, total - 1);
                lines.Add($"{i + 1}. {name} - {level + 1}/{total}");
            }

            return string.Join("\n", lines);
        }

        private static string FindWinnerName(int winningClientId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.SpawnManager == null || winningClientId < 0)
            {
                return null;
            }

            foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjects.Values)
            {
                if (networkObject == null ||
                    networkObject.OwnerClientId != (ulong)winningClientId ||
                    !networkObject.TryGetComponent(out PlayerScore score))
                {
                    continue;
                }

                return string.IsNullOrWhiteSpace(score.DisplayName) ? "Player" : score.DisplayName;
            }

            return null;
        }

        private void CollectScores(GameModeType gameMode)
        {
            scores.Clear();

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.SpawnManager == null)
            {
                return;
            }

            foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjects.Values)
            {
                if (networkObject != null &&
                    networkObject.IsSpawned &&
                    networkObject.TryGetComponent(out PlayerScore playerScore))
                {
                    scores.Add(playerScore);
                }
            }

            if (gameMode == GameModeType.FreeForAll)
            {
                scores.Sort(CompareScores);
            }
        }

        private static int CompareScores(PlayerScore left, PlayerScore right)
        {
            int killComparison = right.Kills.CompareTo(left.Kills);
            if (killComparison != 0)
            {
                return killComparison;
            }

            int deathComparison = left.Deaths.CompareTo(right.Deaths);
            if (deathComparison != 0)
            {
                return deathComparison;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private void CollectGunGameScores()
        {
            gunGameScores.Clear();

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.SpawnManager == null)
            {
                return;
            }

            foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjects.Values)
            {
                if (networkObject != null &&
                    networkObject.IsSpawned &&
                    networkObject.TryGetComponent(out PlayerGunGame progress))
                {
                    gunGameScores.Add(progress);
                }
            }

            gunGameScores.Sort(CompareGunGameScores);
        }

        private static int CompareGunGameScores(PlayerGunGame left, PlayerGunGame right)
        {
            int levelComparison = right.Level.CompareTo(left.Level);
            if (levelComparison != 0)
            {
                return levelComparison;
            }

            PlayerScore leftScore = left.TryGetComponent(out PlayerScore leftPlayerScore) ? leftPlayerScore : null;
            PlayerScore rightScore = right.TryGetComponent(out PlayerScore rightPlayerScore) ? rightPlayerScore : null;
            int leftKills = leftScore != null ? leftScore.Kills : 0;
            int rightKills = rightScore != null ? rightScore.Kills : 0;
            int killComparison = rightKills.CompareTo(leftKills);
            if (killComparison != 0)
            {
                return killComparison;
            }

            string leftName = leftScore != null ? leftScore.DisplayName : string.Empty;
            string rightName = rightScore != null ? rightScore.DisplayName : string.Empty;
            return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
        }

        private void OnRematchClicked()
        {
            RunHostAction("Starting rematch...", static sessionManager => sessionManager.ReloadGameplayScene());
        }

        private void OnReturnToLobbyClicked()
        {
            RunHostAction("Returning to lobby...", static sessionManager => sessionManager.ReturnToLobby());
        }

        private void RunHostAction(string statusMessage, Action<MultiplayerSessionManager> action)
        {
            if (isReturning)
            {
                return;
            }

            MultiplayerSessionManager sessionManager = MultiplayerSessionManager.Instance;
            if (sessionManager == null || !sessionManager.IsHost)
            {
                return;
            }

            isReturning = true;
            SetButtonsInteractable(false);

            if (statusText != null)
            {
                statusText.text = statusMessage;
            }

            action(sessionManager);
        }

        private void OnLeaveClicked()
        {
            if (isReturning)
            {
                return;
            }

            isReturning = true;
            SetButtonsInteractable(false);

            if (statusText != null)
            {
                statusText.text = "Leaving...";
            }

            MultiplayerSessionManager sessionManager = MultiplayerSessionManager.Instance;
            if (sessionManager != null)
            {
                sessionManager.LeaveGame();
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (rematchButton != null)
            {
                rematchButton.interactable = interactable;
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.interactable = interactable;
            }

            if (leaveButton != null)
            {
                leaveButton.interactable = interactable;
            }
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
