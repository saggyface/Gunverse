using System.Collections.Generic;

namespace GunVerse
{
    public sealed class LobbyInfo
    {
        public IReadOnlyList<string> PlayerNames { get; }
        public int PlayerCount { get; }
        public int MaxPlayers { get; }
        public string JoinCode { get; }
        public bool IsHost { get; }

        public LobbyInfo(IReadOnlyList<string> playerNames, int playerCount, int maxPlayers, string joinCode, bool isHost)
        {
            PlayerNames = playerNames;
            PlayerCount = playerCount;
            MaxPlayers = maxPlayers;
            JoinCode = joinCode;
            IsHost = isHost;
        }
    }
}
