using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GunVerse
{
    public sealed class ScoreboardController : MonoBehaviour
    {
        private const float REFRESH_INTERVAL = 0.2f;

        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private TextMeshProUGUI[] nameTexts;
        [SerializeField] private TextMeshProUGUI[] killTexts;
        [SerializeField] private TextMeshProUGUI[] deathTexts;
        [SerializeField] private TextMeshProUGUI teamScoreText;

        private readonly List<ScoreRow> scores = new List<ScoreRow>();

        private sealed class ScoreRow
        {
            public string DisplayName;
            public int Kills;
            public int Deaths;
            public TeamType Team;
        }

        private bool isVisible;
        private float nextRefreshTime;

        private void Awake()
        {
            SetVisible(false);
        }

        private void Update()
        {
            bool shouldShow = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
            if (shouldShow != isVisible)
            {
                SetVisible(shouldShow);
            }

            if (isVisible && Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + REFRESH_INTERVAL;
                RefreshRows();
            }
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            nextRefreshTime = 0f;

            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(visible);
            }
        }

        private void RefreshRows()
        {
            if (nameTexts == null || killTexts == null || deathTexts == null)
            {
                return;
            }

            scores.Clear();

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.SpawnManager != null)
            {
                foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjects.Values)
                {
                    if (networkObject == null || !networkObject.IsSpawned)
                    {
                        continue;
                    }

                    if (networkObject.TryGetComponent(out PlayerScore playerScore))
                    {
                        TeamType team = networkObject.TryGetComponent(out PlayerTeam playerTeam)
                            ? playerTeam.Team
                            : TeamType.None;
                        scores.Add(new ScoreRow
                        {
                            DisplayName = playerScore.DisplayName,
                            Kills = playerScore.Kills,
                            Deaths = playerScore.Deaths,
                            Team = team
                        });
                    }
                    else if (networkObject.TryGetComponent(out TestTargetScore testScore))
                    {
                        scores.Add(new ScoreRow
                        {
                            DisplayName = testScore.DisplayName,
                            Kills = testScore.Kills,
                            Deaths = testScore.Deaths,
                            Team = TeamType.None
                        });
                    }
                }
            }

            scores.Sort(CompareScores);

            UpdateTeamScore();

            int rowCount = Mathf.Min(nameTexts.Length, Mathf.Min(killTexts.Length, deathTexts.Length));
            for (int i = 0; i < rowCount; i++)
            {
                bool hasPlayer = i < scores.Count;
                SetRowVisible(nameTexts[i], hasPlayer);
                SetRowVisible(killTexts[i], hasPlayer);
                SetRowVisible(deathTexts[i], hasPlayer);

                if (!hasPlayer)
                {
                    continue;
                }

                ScoreRow score = scores[i];
                nameTexts[i].text = score.Team == TeamType.None
                    ? score.DisplayName
                    : $"<color=#{TeamPalette.GetColorHex(score.Team)}>{score.DisplayName}</color>";
                killTexts[i].text = score.Kills.ToString();
                deathTexts[i].text = score.Deaths.ToString();
            }
        }

        private void UpdateTeamScore()
        {
            if (teamScoreText == null)
            {
                return;
            }

            GameModeController gameMode = GameModeController.Instance;
            bool showTeamScore = gameMode != null && gameMode.Mode == GameModeType.TeamDeathmatch;
            teamScoreText.gameObject.SetActive(showTeamScore);

            if (!showTeamScore)
            {
                return;
            }

            teamScoreText.text =
                $"<color=#{TeamPalette.GetColorHex(TeamType.Blue)}>BLUE {gameMode.BlueScore} / {gameMode.TeamScoreLimit}</color>" +
                $"    <color=#{TeamPalette.GetColorHex(TeamType.Red)}>RED {gameMode.RedScore} / {gameMode.TeamScoreLimit}</color>";
        }

        private static void SetRowVisible(TextMeshProUGUI text, bool visible)
        {
            if (text != null)
            {
                text.gameObject.SetActive(visible);
            }
        }

        private static int CompareScores(ScoreRow left, ScoreRow right)
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
    }
}
