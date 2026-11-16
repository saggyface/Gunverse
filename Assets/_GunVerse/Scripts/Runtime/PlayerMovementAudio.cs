using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerMovementAudio : NetworkBehaviour
    {
        private const float MIN_LANDING_SPEED = 3f;
        private const float SOUND_HEIGHT_OFFSET = 0.2f;
        private const float FOOTSTEP_PITCH_MIN = 0.9f;
        private const float FOOTSTEP_PITCH_MAX = 1.1f;
        private const float LANDING_IMPACT_NORMALIZATION = 10f;
        private const float MIN_LANDING_VOLUME_SCALE = 0.6f;

        private enum MovementSound : byte
        {
            Footstep,
            Jump,
            Land
        }

        [SerializeField] private float footstepVolume = 0.35f;
        [SerializeField] private float landingVolume = 0.6f;
        [SerializeField] private float jumpVolume = 0.45f;

        private PlayerController playerController;

        public override void OnNetworkSpawn()
        {
            playerController = GetComponent<PlayerController>();

            if (!IsOwner)
            {
                return;
            }

            playerController.OnFootstep += HandleFootstep;
            playerController.OnJumped += HandleJump;
            playerController.OnLanded += HandleLanding;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            playerController.OnFootstep -= HandleFootstep;
            playerController.OnJumped -= HandleJump;
            playerController.OnLanded -= HandleLanding;
        }

        private void HandleFootstep()
        {
            if (playerController.IsSneaking)
            {
                return;
            }

            PlayMovementSound(MovementSound.Footstep, 0f);
            ReportMovementSoundServerRpc((byte)MovementSound.Footstep, 0f);
        }

        private void HandleJump()
        {
            if (playerController.IsSneaking)
            {
                return;
            }

            PlayMovementSound(MovementSound.Jump, 0f);
            ReportMovementSoundServerRpc((byte)MovementSound.Jump, 0f);
        }

        private void HandleLanding(float impactSpeed)
        {
            if (playerController.IsSneaking || impactSpeed < MIN_LANDING_SPEED)
            {
                return;
            }

            PlayMovementSound(MovementSound.Land, impactSpeed);
            ReportMovementSoundServerRpc((byte)MovementSound.Land, impactSpeed);
        }

        [ServerRpc]
        private void ReportMovementSoundServerRpc(byte soundId, float intensity)
        {
            PlayMovementSoundClientRpc(soundId, intensity);
        }

        [ClientRpc]
        private void PlayMovementSoundClientRpc(byte soundId, float intensity)
        {
            if (IsOwner)
            {
                return;
            }

            PlayMovementSound((MovementSound)soundId, intensity);
        }

        private void PlayMovementSound(MovementSound sound, float intensity)
        {
            Vector3 soundPosition = transform.position + Vector3.up * SOUND_HEIGHT_OFFSET;

            switch (sound)
            {
                case MovementSound.Footstep:
                    ProceduralAudio.Play(
                        ProceduralAudio.FootstepClip,
                        soundPosition,
                        footstepVolume,
                        Random.Range(FOOTSTEP_PITCH_MIN, FOOTSTEP_PITCH_MAX));
                    break;

                case MovementSound.Jump:
                    ProceduralAudio.Play(
                        ProceduralAudio.JumpClip,
                        soundPosition,
                        jumpVolume,
                        1f);
                    break;

                case MovementSound.Land:
                    float volumeScale = Mathf.Clamp(
                        intensity / LANDING_IMPACT_NORMALIZATION,
                        MIN_LANDING_VOLUME_SCALE,
                        1f);
                    ProceduralAudio.Play(
                        ProceduralAudio.LandingClip,
                        soundPosition,
                        landingVolume * volumeScale,
                        1f);
                    break;
            }
        }
    }
}
