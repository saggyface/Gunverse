using System;
using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public sealed class PlayerTeam : NetworkBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly NetworkVariable<byte> team = new NetworkVariable<byte>(
            (byte)TeamType.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private PlayerController playerController;
        private MaterialPropertyBlock colorBlock;

        public TeamType Team => (TeamType)team.Value;

        public event Action<TeamType> TeamChanged;

        public override void OnNetworkSpawn()
        {
            playerController = GetComponent<PlayerController>();
            team.OnValueChanged += OnTeamValueChanged;
            ApplyTeamColor();
        }

        public override void OnNetworkDespawn()
        {
            team.OnValueChanged -= OnTeamValueChanged;
        }

        public void SetTeamServer(TeamType newTeam)
        {
            if (!IsServer)
            {
                return;
            }

            team.Value = (byte)newTeam;
        }

        private void OnTeamValueChanged(byte previousValue, byte newValue)
        {
            ApplyTeamColor();
            TeamChanged?.Invoke((TeamType)newValue);
        }

        private void ApplyTeamColor()
        {
            Renderer[] renderers = playerController != null ? playerController.BodyRenderers : null;
            if (renderers == null || renderers.Length == 0)
            {
                MeshRenderer legacyRenderer = playerController != null ? playerController.BodyRenderer : null;
                if (legacyRenderer == null)
                {
                    return;
                }

                renderers = new Renderer[] { legacyRenderer };
            }

            colorBlock ??= new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(colorBlock);
                if (Team == TeamType.None)
                {
                    colorBlock.Clear();
                }
                else
                {
                    colorBlock.SetColor(BaseColorId, TeamPalette.GetColor(Team));
                }

                renderer.SetPropertyBlock(colorBlock);
            }
        }
    }
}
