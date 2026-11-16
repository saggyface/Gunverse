using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public enum PlayerLocomotionState : byte
    {
        Idle,
        Walk,
        Run,
        Air
    }

    public sealed class PlayerAnimationState : NetworkBehaviour
    {
        private const float NETWORK_INTERVAL = 0.1f;
        private const float MOVING_SPEED_THRESHOLD = 0.15f;
        private const float MAX_PITCH_DEGREES = 90f;
        private const float PITCH_ENCODING_RANGE = 127f;
        private const float DEGREES_PER_PITCH_UNIT = MAX_PITCH_DEGREES / PITCH_ENCODING_RANGE;
        private const float PITCH_UNITS_PER_DEGREE = PITCH_ENCODING_RANGE / MAX_PITCH_DEGREES;

        private readonly NetworkVariable<Vector2> planarVelocity = new NetworkVariable<Vector2>(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<byte> locomotionState = new NetworkVariable<byte>(
            (byte)PlayerLocomotionState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<bool> isJumping = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<sbyte> viewPitch = new NetworkVariable<sbyte>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private PlayerController playerController;
        private float nextNetworkTime;
        private bool jumpTriggered;

        public Vector2 PlanarVelocity => planarVelocity.Value;
        public float PlanarSpeed => planarVelocity.Value.magnitude;
        public PlayerLocomotionState LocomotionState => (PlayerLocomotionState)locomotionState.Value;
        public bool IsJumping => isJumping.Value;
        public float ViewPitchDegrees => viewPitch.Value * DEGREES_PER_PITCH_UNIT;

        public override void OnNetworkSpawn()
        {
            playerController = GetComponent<PlayerController>();
            if (!IsOwner)
            {
                return;
            }

            planarVelocity.Value = Vector2.zero;
            locomotionState.Value = (byte)PlayerLocomotionState.Idle;
            isJumping.Value = false;
            viewPitch.Value = 0;
            jumpTriggered = false;

            if (playerController != null)
            {
                playerController.OnJumped += OnJumped;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && playerController != null)
            {
                playerController.OnJumped -= OnJumped;
            }
        }

        private void OnJumped()
        {
            jumpTriggered = true;
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || playerController == null || Time.time < nextNetworkTime)
            {
                return;
            }

            nextNetworkTime = Time.time + NETWORK_INTERVAL;
            Vector3 velocity = playerController.PlanarVelocity;
            planarVelocity.Value = new Vector2(velocity.x, velocity.z);

            float pitchDegrees = Mathf.Clamp(playerController.PitchAngle, -MAX_PITCH_DEGREES, MAX_PITCH_DEGREES);
            viewPitch.Value = (sbyte)Mathf.RoundToInt(pitchDegrees * PITCH_UNITS_PER_DEGREE);

            bool isGrounded = playerController.IsGrounded;
            if (isGrounded)
            {
                jumpTriggered = false;
            }

            isJumping.Value = jumpTriggered && !isGrounded;

            PlayerLocomotionState state;
            if (!isGrounded)
            {
                state = PlayerLocomotionState.Air;
            }
            else if (playerController.HorizontalSpeed < MOVING_SPEED_THRESHOLD)
            {
                state = PlayerLocomotionState.Idle;
            }
            else if (playerController.IsSprinting)
            {
                state = PlayerLocomotionState.Run;
            }
            else
            {
                state = PlayerLocomotionState.Walk;
            }

            locomotionState.Value = (byte)state;
        }
    }
}
