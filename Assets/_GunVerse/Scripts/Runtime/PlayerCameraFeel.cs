using UnityEngine;
using UnityEngine.InputSystem;

namespace GunVerse
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerCameraFeel : MonoBehaviour
    {
        private const float MIN_MOVEMENT_SPEED = 0.5f;
        private const float BOB_HORIZONTAL_RATIO = 0.4f;
        private const float BOB_WEAPON_RATIO = 0.3f;
        private const float MAX_SHOT_KICK_DEGREES = 12f;
        private const float MAX_FIELD_OF_VIEW_KICK = 3f;
        private const float SHAKE_ROLL_RATIO = 0.5f;

        [Header("Sprint Field Of View")]
        [SerializeField] private float sprintFieldOfViewBoost = 5f;
        [SerializeField] private float fieldOfViewChangeSpeed = 20f;

        [Header("Camera Bob")]
        [SerializeField] private float bobFrequency = 1.1f;
        [SerializeField] private float bobAmplitude = 0.03f;
        [SerializeField] private float sprintRollDegrees = 0.6f;

        [Header("Landing Dip")]
        [SerializeField] private float landingDipPerSpeed = 0.012f;
        [SerializeField] private float maxLandingDip = 0.12f;
        [SerializeField] private float landingDipSmoothTime = 0.09f;

        [Header("Weapon Sway")]
        [SerializeField] private float swayPositionScale = 0.0015f;
        [SerializeField] private float swayRotationScale = 3f;
        [SerializeField] private float swaySmoothness = 10f;
        [SerializeField] private float maxSwayPosition = 0.03f;

        [Header("Shot Kick")]
        [SerializeField] private float kickRecoverySpeed = 24f;
        [SerializeField] private float shakeDecaySpeed = 2.5f;
        [SerializeField] private float maxShakeAngle = 2f;
        [SerializeField] private float shakeFrequency = 22f;
        [SerializeField] private float fieldOfViewKickRecoverySpeed = 30f;

        private PlayerController playerController;
        private Vector3 cameraPivotBasePosition;
        private Vector3 handBasePosition;
        private Quaternion handBaseRotation;
        private float baseFieldOfView;
        private float bobPhase;
        private float landingDip;
        private float landingDipVelocity;
        private float currentBobX;
        private float currentBobY;
        private float currentSpeedFactor;
        private Vector2 weaponSway;
        private float shotKickPitch;
        private float shakeTrauma;
        private float fieldOfViewKick;

        public float BobPhase => bobPhase;
        public float SpeedFactor => currentSpeedFactor;
        public bool IsMoving =>
            playerController != null &&
            playerController.IsGrounded &&
            playerController.HorizontalSpeed > MIN_MOVEMENT_SPEED;
        public bool IsGrounded => playerController != null && playerController.IsGrounded;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            cameraPivotBasePosition = playerController.CameraPivot.localPosition;
            handBasePosition = playerController.HandAnchor.localPosition;
            handBaseRotation = playerController.HandAnchor.localRotation;
            baseFieldOfView = GameSettings.FieldOfView;
            playerController.PlayerCamera.Lens.FieldOfView = baseFieldOfView;
        }

        private void OnEnable()
        {
            playerController.OnLanded += OnLanded;
            playerController.OnRespawned += OnRespawned;
            GameSettings.FieldOfViewChanged += OnFieldOfViewChanged;
        }

        private void OnDisable()
        {
            playerController.OnLanded -= OnLanded;
            playerController.OnRespawned -= OnRespawned;
            GameSettings.FieldOfViewChanged -= OnFieldOfViewChanged;
        }

        private void OnFieldOfViewChanged(float fieldOfView)
        {
            baseFieldOfView = fieldOfView;
            if (playerController != null && playerController.PlayerCamera != null && playerController.PlayerCamera.enabled)
            {
                playerController.PlayerCamera.Lens.FieldOfView = fieldOfView;
            }
        }

        public void ApplyShotKick(float pitchKickDegrees, float shakeStrength, float fieldOfViewPunch)
        {
            shotKickPitch = Mathf.Min(shotKickPitch + pitchKickDegrees, MAX_SHOT_KICK_DEGREES);
            shakeTrauma = Mathf.Clamp01(shakeTrauma + shakeStrength);
            fieldOfViewKick = Mathf.Min(fieldOfViewKick + fieldOfViewPunch, MAX_FIELD_OF_VIEW_KICK);
        }

        private void LateUpdate()
        {
            if (!playerController.IsMovementActive)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            UpdateShotKick(deltaTime);
            UpdateFieldOfView(deltaTime);
            UpdateCameraOffset(deltaTime);
            UpdateWeaponSway(deltaTime);
        }

        private void UpdateShotKick(float deltaTime)
        {
            shotKickPitch = Mathf.MoveTowards(shotKickPitch, 0f, kickRecoverySpeed * deltaTime);
            fieldOfViewKick = Mathf.MoveTowards(fieldOfViewKick, 0f, fieldOfViewKickRecoverySpeed * deltaTime);
            shakeTrauma = Mathf.MoveTowards(shakeTrauma, 0f, shakeDecaySpeed * deltaTime);
        }

        private void UpdateFieldOfView(float deltaTime)
        {
            float targetFieldOfView = baseFieldOfView
                + (playerController.IsSprinting ? sprintFieldOfViewBoost : 0f)
                + fieldOfViewKick;
            targetFieldOfView = Mathf.Clamp(targetFieldOfView, GameSettings.FIELD_OF_VIEW_MIN, GameSettings.FIELD_OF_VIEW_MAX);
            float currentFieldOfView = playerController.PlayerCamera.Lens.FieldOfView;
            playerController.PlayerCamera.Lens.FieldOfView = Mathf.MoveTowards(currentFieldOfView, targetFieldOfView, fieldOfViewChangeSpeed * deltaTime);
        }

        private void UpdateCameraOffset(float deltaTime)
        {
            float speed = playerController.HorizontalSpeed;
            bool isMoving = playerController.IsGrounded && speed > MIN_MOVEMENT_SPEED;

            currentSpeedFactor = Mathf.Clamp01(speed / playerController.WalkSpeed);
            if (isMoving)
            {
                bobPhase += speed * bobFrequency * deltaTime;
            }

            currentBobX = Mathf.Cos(bobPhase) * bobAmplitude * BOB_HORIZONTAL_RATIO * currentSpeedFactor;
            currentBobY = Mathf.Sin(bobPhase * 2f) * bobAmplitude * currentSpeedFactor;

            landingDip = Mathf.SmoothDamp(landingDip, 0f, ref landingDipVelocity, landingDipSmoothTime, Mathf.Infinity, deltaTime);

            Vector3 offset = new Vector3(currentBobX, currentBobY - landingDip, 0f);
            Transform cameraPivot = playerController.CameraPivot;
            cameraPivot.localPosition = cameraPivotBasePosition + offset;

            float sprintRoll = playerController.IsSprinting
                ? Mathf.Sin(bobPhase) * sprintRollDegrees * currentSpeedFactor
                : 0f;

            float shakeAmount = shakeTrauma * shakeTrauma;
            float shakeTime = Time.time * shakeFrequency;
            float shakePitch = (Mathf.PerlinNoise(shakeTime, 0f) - 0.5f) * 2f * shakeAmount * maxShakeAngle;
            float shakeYaw = (Mathf.PerlinNoise(0f, shakeTime) - 0.5f) * 2f * shakeAmount * maxShakeAngle;
            float shakeRoll = (Mathf.PerlinNoise(shakeTime, shakeTime) - 0.5f) * 2f * shakeAmount * maxShakeAngle * SHAKE_ROLL_RATIO;

            cameraPivot.localRotation *= Quaternion.Euler(-shotKickPitch + shakePitch, shakeYaw, sprintRoll + shakeRoll);
        }

        private void UpdateWeaponSway(float deltaTime)
        {
            Mouse mouse = Mouse.current;
            Vector2 mouseDelta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;

            Vector2 targetSway = new Vector2(-mouseDelta.x, -mouseDelta.y) * swayPositionScale;
            targetSway = Vector2.ClampMagnitude(targetSway, maxSwayPosition);

            float blend = 1f - Mathf.Exp(-swaySmoothness * deltaTime);
            weaponSway = Vector2.Lerp(weaponSway, targetSway, blend);

            Vector2 normalizedSway = maxSwayPosition > 0f ? weaponSway / maxSwayPosition : Vector2.zero;
            float swayYaw = -normalizedSway.x * swayRotationScale;
            float swayPitch = normalizedSway.y * swayRotationScale;
            float swayRoll = normalizedSway.x * swayRotationScale * 0.5f;

            Vector3 positionOffset = new Vector3(
                weaponSway.x + currentBobX * BOB_WEAPON_RATIO,
                weaponSway.y + currentBobY * BOB_WEAPON_RATIO,
                0f);

            Transform handAnchor = playerController.HandAnchor;
            handAnchor.localPosition = handBasePosition + positionOffset;
            handAnchor.localRotation = handBaseRotation * Quaternion.Euler(swayPitch, swayYaw, swayRoll);
        }

        private void OnLanded(float impactSpeed)
        {
            landingDip = Mathf.Clamp(impactSpeed * landingDipPerSpeed, 0f, maxLandingDip);
            landingDipVelocity = 0f;
        }

        private void OnRespawned()
        {
            bobPhase = 0f;
            landingDip = 0f;
            landingDipVelocity = 0f;
            currentBobX = 0f;
            currentBobY = 0f;
            currentSpeedFactor = 0f;
            weaponSway = Vector2.zero;
            shotKickPitch = 0f;
            shakeTrauma = 0f;
            fieldOfViewKick = 0f;

            playerController.CameraPivot.localPosition = cameraPivotBasePosition;
            playerController.HandAnchor.localPosition = handBasePosition;
            playerController.HandAnchor.localRotation = handBaseRotation;
            playerController.PlayerCamera.Lens.FieldOfView = baseFieldOfView;
        }
    }
}
