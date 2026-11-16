using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GunVerse
{
    public class PlayerController : NetworkBehaviour
    {
        private const float PITCH_MIN = -90f;
        private const float PITCH_MAX = 90f;
        private const int LOCAL_CAMERA_PRIORITY = 100;
        private const float GRAVITY_STICK = -2f;
        private const float LOOK_BASE_SENSITIVITY = 0.1f;
        private const float MIN_MOVE_INPUT_SQR = 0.0001f;
        private const float MIN_FOOTSTEP_SPEED = 1f;
        private const float EXTERNAL_VELOCITY_DECAY = 6f;
        private const float FALL_RECOVERY_Y = -2f;
        private const float FALL_RECOVERY_SERVER_MARGIN = 1f;
        private const float FALL_RECOVERY_REQUEST_INTERVAL = 0.5f;
        private const float FALL_RECOVERY_SERVER_INTERVAL = 1f;

        private static readonly List<CharacterController> activeCharacterControllers = new List<CharacterController>();

        public static IReadOnlyList<CharacterController> ActiveCharacterControllers => activeCharacterControllers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveCharacterControllers()
        {
            activeCharacterControllers.Clear();
        }

        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private CinemachineCamera playerCamera;
        [SerializeField] private GameObject bodyVisual;
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private MeshRenderer bodyRenderer;
        [SerializeField] private Transform handAnchor;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float sprintSpeed = 8.5f;
        [SerializeField] private float sneakSpeed = 2.5f;
        [SerializeField] private float groundAcceleration = 45f;
        [SerializeField] private float groundDeceleration = 38f;
        [SerializeField] private float airAcceleration = 18f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float fallGravityMultiplier = 1.7f;
        [SerializeField] private float terminalVelocity = 40f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("Footsteps")]
        [SerializeField] private float footstepStrideDistance = 2.2f;

        private float pitchAngle;
        private float verticalVelocity;
        private Vector3 horizontalVelocity;
        private Vector3 externalVelocity;
        private float lastGroundedTime = float.NegativeInfinity;
        private float jumpBufferedUntilTime = float.NegativeInfinity;
        private float distanceSinceFootstep;
        private bool wasGroundedLastFrame;
        private float nextFallRecoveryRequestTime;
        private float nextServerFallRecoveryTime;
        private PlayerHealth playerHealth;

        public Transform HandAnchor => handAnchor;
        public Transform CameraPivot => cameraPivot;
        public CinemachineCamera PlayerCamera => playerCamera;
        public float PitchAngle => pitchAngle;
        public GameObject BodyVisual => bodyVisual;
        public Renderer[] BodyRenderers => bodyRenderers;
        public MeshRenderer BodyRenderer => bodyRenderer;
        public float WalkSpeed => walkSpeed;
        public float HorizontalSpeed => horizontalVelocity.magnitude;
        public Vector3 PlanarVelocity => horizontalVelocity;
        public bool IsGrounded => characterController.isGrounded;
        public bool IsSprinting { get; private set; }
        public bool IsSneaking { get; private set; }

        public bool IsMovementActive => IsOwner && (playerHealth == null || !playerHealth.IsDead);

        public event Action OnFootstep;
        public event Action OnJumped;
        public event Action<float> OnLanded;
        public event Action OnRespawned;

        public override void OnNetworkSpawn()
        {
            playerHealth = GetComponent<PlayerHealth>();

            if (IsServer && GetComponent<PlayerHitboxHistory>() == null)
            {
                gameObject.AddComponent<PlayerHitboxHistory>();
            }

            bool isLocalOwner = IsOwner;

            playerCamera.enabled = isLocalOwner;
            playerCamera.Priority = isLocalOwner ? LOCAL_CAMERA_PRIORITY : 0;
            if (bodyVisual != null)
            {
                bodyVisual.SetActive(!isLocalOwner);
            }
            else if (bodyRenderer != null)
            {
                bodyRenderer.enabled = !isLocalOwner;
            }

            if (!isLocalOwner)
            {
                characterController.enabled = false;
            }

            if (characterController != null && !activeCharacterControllers.Contains(characterController))
            {
                activeCharacterControllers.Add(characterController);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (characterController != null)
            {
                activeCharacterControllers.Remove(characterController);
            }
        }

        public override void OnDestroy()
        {
            if (characterController != null)
            {
                activeCharacterControllers.Remove(characterController);
            }

            base.OnDestroy();
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);

            verticalVelocity = 0f;
            horizontalVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
            pitchAngle = 0f;
            cameraPivot.localRotation = Quaternion.identity;
            wasGroundedLastFrame = false;
            lastGroundedTime = float.NegativeInfinity;
            jumpBufferedUntilTime = float.NegativeInfinity;
            distanceSinceFootstep = 0f;
            nextFallRecoveryRequestTime = 0f;
            nextServerFallRecoveryTime = 0f;
            IsSprinting = false;
            IsSneaking = false;

            characterController.enabled = true;
            OnRespawned?.Invoke();
        }

        public void ApplyLaunch(Vector3 launchVelocity)
        {
            if (!IsServer || (playerHealth != null && playerHealth.IsDead))
            {
                return;
            }

            if (NetworkManager == null || !NetworkManager.ConnectedClients.ContainsKey(OwnerClientId))
            {
                return;
            }

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            };

            ApplyLaunchClientRpc(launchVelocity, clientRpcParams);
        }

        [ClientRpc]
        private void ApplyLaunchClientRpc(Vector3 launchVelocity, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner || (playerHealth != null && playerHealth.IsDead))
            {
                return;
            }

            externalVelocity += new Vector3(launchVelocity.x, 0f, launchVelocity.z);

            if (launchVelocity.y > 0f)
            {
                verticalVelocity = Mathf.Max(verticalVelocity, launchVelocity.y);
            }
        }

        [ServerRpc]
        private void RequestFallRecoveryServerRpc()
        {
            TryRecoverFromFallServer();
        }

        private void Update()
        {
            if (!IsMovementActive)
            {
                return;
            }

            if (transform.position.y < FALL_RECOVERY_Y)
            {
                TryRequestFallRecovery();
                return;
            }

            ApplyLook();

            if (!GameModeController.IsGameplayActive)
            {
                return;
            }

            ApplyMovement();
        }

        private void TryRequestFallRecovery()
        {
            if (Time.time < nextFallRecoveryRequestTime)
            {
                return;
            }

            nextFallRecoveryRequestTime = Time.time + FALL_RECOVERY_REQUEST_INTERVAL;

            if (IsServer)
            {
                TryRecoverFromFallServer();
                return;
            }

            RequestFallRecoveryServerRpc();
        }

        private void TryRecoverFromFallServer()
        {
            if (!IsServer || playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            if (Time.time < nextServerFallRecoveryTime)
            {
                return;
            }

            if (transform.position.y > FALL_RECOVERY_Y + FALL_RECOVERY_SERVER_MARGIN)
            {
                return;
            }

            nextServerFallRecoveryTime = Time.time + FALL_RECOVERY_SERVER_INTERVAL;
            playerHealth.RecoverFromFall();
        }

        private void ApplyLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            float sensitivity = LOOK_BASE_SENSITIVITY * GameSettings.Sensitivity;

            transform.Rotate(Vector3.up, delta.x * sensitivity);

            pitchAngle = Mathf.Clamp(pitchAngle - delta.y * sensitivity, PITCH_MIN, PITCH_MAX);
            cameraPivot.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        }

        private void ApplyMovement()
        {
            Vector3 wishDirection = GetWorldMoveDirection(ReadMoveInput());
            bool wantsSneak = IsSneakHeld();
            IsSneaking = wantsSneak;
            bool wantsSprint = !wantsSneak && IsSprintHeld() && wishDirection.sqrMagnitude > MIN_MOVE_INPUT_SQR;
            bool isGrounded = characterController.isGrounded;

            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }

            BufferJumpIfPressed();
            bool hasJumped = TryJump();

            if (!hasJumped && isGrounded && !wasGroundedLastFrame && verticalVelocity < 0f)
            {
                RaiseLanded(-verticalVelocity);
            }

            UpdateHorizontalVelocity(wishDirection, wantsSprint, wantsSneak, isGrounded);
            UpdateVerticalVelocity(isGrounded, hasJumped);

            externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, EXTERNAL_VELOCITY_DECAY * Time.deltaTime);
            Vector3 motion = horizontalVelocity + externalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(motion * Time.deltaTime);

            UpdateFootsteps(isGrounded);

            wasGroundedLastFrame = isGrounded;
        }

        private Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;

            return input;
        }

        private Vector3 GetWorldMoveDirection(Vector2 input)
        {
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            direction.y = 0f;

            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            return direction;
        }

        private static bool IsSprintHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        private static bool IsSneakHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
        }

        private void UpdateHorizontalVelocity(Vector3 wishDirection, bool wantsSprint, bool wantsSneak, bool isGrounded)
        {
            bool hasMoveInput = wishDirection.sqrMagnitude > MIN_MOVE_INPUT_SQR;
            IsSneaking = wantsSneak;
            IsSprinting = wantsSprint && hasMoveInput;

            if (!isGrounded && !hasMoveInput)
            {
                return;
            }

            float targetSpeed = IsSneaking ? sneakSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = wishDirection * targetSpeed;

            float acceleration;
            if (!isGrounded)
            {
                acceleration = airAcceleration;
            }
            else if (hasMoveInput)
            {
                acceleration = groundAcceleration;
            }
            else
            {
                acceleration = groundDeceleration;
            }

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);
        }

        private void BufferJumpIfPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpBufferedUntilTime = Time.time + jumpBufferTime;
            }
        }

        private bool TryJump()
        {
            bool hasBufferedJump = Time.time <= jumpBufferedUntilTime;
            bool isWithinCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;

            if (!hasBufferedJump || !isWithinCoyoteTime || verticalVelocity > 0f)
            {
                return false;
            }

            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            jumpBufferedUntilTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            distanceSinceFootstep = 0f;
            OnJumped?.Invoke();
            return true;
        }

        private void UpdateVerticalVelocity(bool isGrounded, bool hasJumped)
        {
            if (isGrounded && !hasJumped && verticalVelocity <= 0f)
            {
                verticalVelocity = GRAVITY_STICK;
                return;
            }

            float gravityMultiplier = verticalVelocity < 0f ? fallGravityMultiplier : 1f;
            verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -terminalVelocity);
        }

        private void UpdateFootsteps(bool isGrounded)
        {
            if (!isGrounded)
            {
                return;
            }

            float speed = horizontalVelocity.magnitude;
            if (speed < MIN_FOOTSTEP_SPEED)
            {
                return;
            }

            distanceSinceFootstep += speed * Time.deltaTime;
            if (distanceSinceFootstep < footstepStrideDistance)
            {
                return;
            }

            distanceSinceFootstep -= footstepStrideDistance;
            OnFootstep?.Invoke();
        }

        private void RaiseLanded(float impactSpeed)
        {
            distanceSinceFootstep = 0f;
            OnLanded?.Invoke(impactSpeed);
        }
    }
}
