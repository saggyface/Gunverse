using Unity.Collections;
using Unity.Netcode;

namespace GunVerse
{
    public sealed class TestTargetScore : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString128Bytes> displayName = new NetworkVariable<FixedString128Bytes>(
            new FixedString128Bytes("TEST"),
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

        public ulong ScoreId { get; private set; }
        public string DisplayName => displayName.Value.ToString();
        public int Kills => kills.Value;
        public int Deaths => deaths.Value;
        internal FixedString128Bytes DisplayNameValue => displayName.Value;

        internal void InitializeServer(ulong scoreId, string targetName)
        {
            if (!IsServer)
            {
                return;
            }

            ScoreId = scoreId;
            displayName.Value = new FixedString128Bytes(targetName);
        }

        internal void RecordDeathServer()
        {
            if (!IsServer)
            {
                return;
            }

            deaths.Value++;
        }
    }
}
