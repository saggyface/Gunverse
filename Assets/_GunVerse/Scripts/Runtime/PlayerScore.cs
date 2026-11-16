using Unity.Collections;
using Unity.Netcode;

namespace GunVerse
{
    public sealed class PlayerScore : NetworkBehaviour
    {
        private const int MAX_DISPLAY_NAME_LENGTH = GameSettings.PLAYER_NAME_MAX_LENGTH;
        private const string FALLBACK_DISPLAY_NAME = "Player";

        private readonly NetworkVariable<FixedString128Bytes> displayName = new NetworkVariable<FixedString128Bytes>(
            new FixedString128Bytes(FALLBACK_DISPLAY_NAME),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> kills = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> deaths = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public string DisplayName => displayName.Value.ToString();
        public int Kills => kills.Value;
        public int Deaths => deaths.Value;

        internal FixedString128Bytes DisplayNameValue => displayName.Value;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }

            FixedString128Bytes requestedName = CreateDisplayName(GameSettings.PlayerName);
            if (IsServer)
            {
                displayName.Value = requestedName;
                return;
            }

            SetDisplayNameServerRpc(requestedName);
        }

        internal void RecordKill()
        {
            if (!IsServer)
            {
                return;
            }

            kills.Value++;
        }

        internal void RecordDeath()
        {
            if (!IsServer)
            {
                return;
            }

            deaths.Value++;
        }

        [ServerRpc]
        private void SetDisplayNameServerRpc(FixedString128Bytes requestedName)
        {
            displayName.Value = CreateDisplayName(requestedName.ToString());
        }

        private static FixedString128Bytes CreateDisplayName(string requestedName)
        {
            string displayName = string.IsNullOrWhiteSpace(requestedName)
                ? FALLBACK_DISPLAY_NAME
                : requestedName.Trim();

            if (displayName.Length > MAX_DISPLAY_NAME_LENGTH)
            {
                displayName = displayName.Substring(0, MAX_DISPLAY_NAME_LENGTH);
            }

            return new FixedString128Bytes(displayName);
        }
    }
}
