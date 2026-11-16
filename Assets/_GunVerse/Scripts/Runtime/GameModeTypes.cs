using UnityEngine;

namespace GunVerse
{
    public enum GameModeType
    {
        FreeForAll = 0,
        TeamDeathmatch = 1,
        GunGame = 2
    }

    public enum TeamType
    {
        None = 0,
        Blue = 1,
        Red = 2
    }

    public enum MatchPhase
    {
        WaitingForLoad = 0,
        Countdown = 1,
        Playing = 2,
        Results = 3
    }

    public enum MatchEndReason
    {
        None = 0,
        ScoreLimit = 1,
        TimeExpired = 2,
        LadderComplete = 3
    }

    public static class TeamPalette
    {
        public static readonly Color Blue = new Color(0.20f, 0.48f, 1f, 1f);
        public static readonly Color Red = new Color(1f, 0.24f, 0.20f, 1f);
        public static readonly Color Neutral = new Color(0.82f, 0.84f, 0.88f, 1f);

        public static Color GetColor(TeamType team)
        {
            return team switch
            {
                TeamType.Blue => Blue,
                TeamType.Red => Red,
                _ => Neutral
            };
        }

        public static string GetName(TeamType team)
        {
            return team switch
            {
                TeamType.Blue => "BLUE",
                TeamType.Red => "RED",
                _ => string.Empty
            };
        }

        public static string GetColorHex(TeamType team)
        {
            return ColorUtility.ToHtmlStringRGB(GetColor(team));
        }
    }
}
