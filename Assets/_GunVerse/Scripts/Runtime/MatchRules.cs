using System;
using UnityEngine;

namespace GunVerse
{
    [Serializable]
    public sealed class MatchRules
    {
        public const int DEFAULT_MATCH_DURATION_SECONDS = 300;
        public const int DEFAULT_TEAM_SCORE_LIMIT = 50;
        public const int DEFAULT_PLAYER_KILL_LIMIT = 25;

        private const int MIN_MATCH_DURATION_SECONDS = 30;
        private const int MIN_SCORE_LIMIT = 1;

        [SerializeField] private int matchDurationSeconds = DEFAULT_MATCH_DURATION_SECONDS;
        [SerializeField] private int teamScoreLimit = DEFAULT_TEAM_SCORE_LIMIT;
        [SerializeField] private int playerKillLimit = DEFAULT_PLAYER_KILL_LIMIT;

        public int MatchDurationSeconds => Mathf.Max(MIN_MATCH_DURATION_SECONDS, matchDurationSeconds);
        public int TeamScoreLimit => Mathf.Max(MIN_SCORE_LIMIT, teamScoreLimit);
        public int PlayerKillLimit => Mathf.Max(MIN_SCORE_LIMIT, playerKillLimit);

        public MatchRules()
        {
        }

        public MatchRules(int matchDurationSeconds, int teamScoreLimit, int playerKillLimit)
        {
            this.matchDurationSeconds = matchDurationSeconds;
            this.teamScoreLimit = teamScoreLimit;
            this.playerKillLimit = playerKillLimit;
        }

        public void CopyFrom(MatchRules other)
        {
            if (other == null)
            {
                return;
            }

            matchDurationSeconds = other.matchDurationSeconds;
            teamScoreLimit = other.teamScoreLimit;
            playerKillLimit = other.playerKillLimit;
        }
    }
}
