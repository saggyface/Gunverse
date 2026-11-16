using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GunVerse
{
    public sealed class PlayerFpArms : NetworkBehaviour
    {
        private const string ViewModelLayerName = "ViewModel";
        private const float FixedViewmodelFieldOfView = 70f;
        private const int FallbackViewmodelLayer = 8;
        private const float PROBE_INTERVAL = 2f;
        private const float IDLE_SWAY_AMPLITUDE = 0.006f;
        private const float IDLE_SWAY_FREQUENCY = 1.6f;
        private const float IDLE_BOB_AMPLITUDE = 0.004f;
        private const float IDLE_BOB_FREQUENCY = 1.1f;
        private const float MIN_LOCOMOTION_BLEND = 0.01f;
        private const float HAND_SWING_VERTICAL_RATIO = 0.35f;

        [SerializeField] private GameObject viewmodelPrefab;
        [SerializeField] private Vector3 viewmodelLocalPosition = new Vector3(0f, -1.55f, 0.15f);
        [SerializeField] private Vector3 idleViewmodelLocalPosition = new Vector3(0f, -1.55f, 0.30f);
        [SerializeField] private Vector3 viewmodelLocalEulerAngles;
        [SerializeField] private float viewmodelScale = 1f;
        [SerializeField] private Vector3 idleRightHandLocalPosition = new Vector3(0.26f, -0.30f, 0.48f);
        [SerializeField] private Vector3 idleLeftHandLocalPosition = new Vector3(-0.26f, -0.30f, 0.48f);
        [SerializeField] private Vector3 idleRightHandLocalEulerAngles;
        [SerializeField] private Vector3 idleLeftHandLocalEulerAngles;
        [SerializeField] private Vector3 gripRightHandLocalEulerOffset;
        [SerializeField] private Vector3 gripLeftHandLocalEulerOffset;
        [SerializeField] private bool enableProbe = true;

        [Header("Locomotion Motion")]
        [SerializeField] private float locomotionBobAmplitude = 0.02f;
        [SerializeField] private float locomotionSwayAmplitude = 0.012f;
        [SerializeField] private float locomotionRollDegrees = 1.2f;
        [SerializeField] private float locomotionPitchDegrees = 0.8f;
        [SerializeField] private float locomotionHandSwingAmplitude = 0.03f;
        [SerializeField] private float locomotionBlendSpeed = 7f;
        [SerializeField] private float sprintAmplitudeMultiplier = 1.45f;
        [SerializeField] private float sprintFrequencyMultiplier = 1.35f;

        private GameObject viewmodelInstance;
        private Camera mainCamera;
        private Camera overlayCamera;
        private GunItem currentGun;
        private PlayerController playerController;
        private PlayerCameraFeel playerCameraFeel;
        private int viewmodelLayer;
        private int mainCameraMask;
        private float nextProbeTime;
        private float locomotionBlend;
        private float locomotionPhase;
        private float lastCameraBobPhase;
        private Transform rightUpperArm;
        private Transform rightForeArm;
        private Transform rightHand;
        private Transform rightMiddle;
        private Transform leftUpperArm;
        private Transform leftForeArm;
        private Transform leftHand;
        private Transform leftMiddle;

        public override void OnNetworkSpawn()
        {
            playerController = GetComponent<PlayerController>();
            playerCameraFeel = GetComponent<PlayerCameraFeel>();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            mainCamera = Camera.main;
            if (mainCamera == null || viewmodelPrefab == null)
            {
                enabled = false;
                return;
            }

            viewmodelLayer = LayerMask.NameToLayer(ViewModelLayerName);
            if (viewmodelLayer < 0)
            {
                viewmodelLayer = FallbackViewmodelLayer;
            }

            mainCameraMask = mainCamera.cullingMask;
            mainCamera.cullingMask = mainCameraMask & ~(1 << viewmodelLayer);

            CreateOverlayCamera();
            CreateViewmodel();
        }

        public override void OnNetworkDespawn()
        {
            if (mainCamera != null)
            {
                mainCamera.cullingMask = mainCameraMask;
            }

            if (overlayCamera != null)
            {
                Destroy(overlayCamera.gameObject);
            }

            if (viewmodelInstance != null)
            {
                Destroy(viewmodelInstance);
            }

            RestoreGun();
        }

        private void LateUpdate()
        {
            UpdateFieldOfView();
            UpdateLocomotionState();
            UpdateHeldGun();
        }

        private void CreateOverlayCamera()
        {
            GameObject cameraObject = new GameObject("ViewModelCamera");
            cameraObject.transform.SetParent(mainCamera.transform, false);
            overlayCamera = cameraObject.AddComponent<Camera>();
            overlayCamera.clearFlags = CameraClearFlags.Depth;
            overlayCamera.cullingMask = 1 << viewmodelLayer;
            overlayCamera.fieldOfView = FixedViewmodelFieldOfView;
            overlayCamera.nearClipPlane = 0.01f;
            overlayCamera.farClipPlane = 5f;
            overlayCamera.depth = mainCamera.depth + 1;
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = CameraRenderType.Overlay;

            UniversalAdditionalCameraData mainData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
            if (mainData != null)
            {
                mainData.cameraStack.Add(overlayCamera);
            }
        }

        private void CreateViewmodel()
        {
            viewmodelInstance = Instantiate(viewmodelPrefab, mainCamera.transform);
            viewmodelInstance.name = "ViewModelArms";
            viewmodelInstance.transform.localPosition = idleViewmodelLocalPosition;
            viewmodelInstance.transform.localRotation = Quaternion.Euler(viewmodelLocalEulerAngles);
            viewmodelInstance.transform.localScale = Vector3.one * viewmodelScale;
            SetLayerRecursively(viewmodelInstance, viewmodelLayer);

            rightUpperArm = FindBone(viewmodelInstance.transform, "mixamorig1:RightArm");
            rightForeArm = FindBone(viewmodelInstance.transform, "mixamorig1:RightForeArm");
            rightHand = FindBone(viewmodelInstance.transform, "mixamorig1:RightHand");
            rightMiddle = FindBone(viewmodelInstance.transform, "mixamorig1:RightHandMiddle1");
            leftUpperArm = FindBone(viewmodelInstance.transform, "mixamorig1:LeftArm");
            leftForeArm = FindBone(viewmodelInstance.transform, "mixamorig1:LeftForeArm");
            leftHand = FindBone(viewmodelInstance.transform, "mixamorig1:LeftHand");
            leftMiddle = FindBone(viewmodelInstance.transform, "mixamorig1:LeftHandMiddle1");
        }

        private void UpdateFieldOfView()
        {
            if (overlayCamera == null)
            {
                return;
            }

            overlayCamera.fieldOfView = GameSettings.ViewmodelFov == ViewmodelFovMode.MatchWorld
                ? mainCamera.fieldOfView
                : FixedViewmodelFieldOfView;
        }

        private void UpdateHeldGun()
        {
            GunItem held = GunItem.FindHeld(OwnerClientId);
            if (held == currentGun)
            {
                UpdateArms(held);
                return;
            }

            RestoreGun();
            currentGun = held;
            if (currentGun != null)
            {
                SetGunLayer(currentGun, viewmodelLayer);
            }

            UpdateArms(currentGun);
        }

        private void UpdateArms(GunItem held)
        {
            if (held == null)
            {
                UpdateIdleArms();
                return;
            }

            ApplyViewmodelRootMotion(viewmodelLocalPosition);

            if (rightUpperArm == null || rightForeArm == null || rightHand == null)
            {
                return;
            }

            Quaternion gunRotation = held.transform.rotation;
            UpdateArm(
                rightUpperArm,
                rightForeArm,
                rightHand,
                rightMiddle,
                held.GripPrimary,
                gunRotation,
                1f,
                gripRightHandLocalEulerOffset);
            if (held.HoldType != GunHoldType.OneHand && held.GripSupport != null)
            {
                UpdateArm(
                    leftUpperArm,
                    leftForeArm,
                    leftHand,
                    leftMiddle,
                    held.GripSupport,
                    gunRotation,
                    -1f,
                    gripLeftHandLocalEulerOffset);
            }

            LogProbe(held);
        }

        private void UpdateIdleArms()
        {
            if (mainCamera == null ||
                rightUpperArm == null || rightForeArm == null || rightHand == null ||
                leftUpperArm == null || leftForeArm == null || leftHand == null)
            {
                return;
            }

            ApplyViewmodelRootMotion(idleViewmodelLocalPosition);

            float sway = Mathf.Sin(Time.time * IDLE_SWAY_FREQUENCY) * IDLE_SWAY_AMPLITUDE;
            float bob = Mathf.Cos(Time.time * IDLE_BOB_FREQUENCY) * IDLE_BOB_AMPLITUDE;
            Vector3 handSwing = GetIdleHandSwing();
            Vector3 rightTarget = idleRightHandLocalPosition + new Vector3(sway, bob, 0f) + handSwing;
            Vector3 leftTarget = idleLeftHandLocalPosition + new Vector3(-sway, bob, 0f) - handSwing;

            Quaternion rightHandRotation = mainCamera.transform.rotation * Quaternion.Euler(idleRightHandLocalEulerAngles);
            Quaternion leftHandRotation = mainCamera.transform.rotation * Quaternion.Euler(idleLeftHandLocalEulerAngles);
            SolveIdleArm(rightUpperArm, rightForeArm, rightHand, rightTarget, rightHandRotation, 1f);
            SolveIdleArm(leftUpperArm, leftForeArm, leftHand, leftTarget, leftHandRotation, -1f);
        }

        private void UpdateLocomotionState()
        {
            if (playerCameraFeel == null)
            {
                locomotionBlend = 0f;
                return;
            }

            bool isMoving = playerCameraFeel.IsMoving;
            float targetBlend = isMoving ? Mathf.Clamp01(playerCameraFeel.SpeedFactor) : 0f;
            locomotionBlend = Mathf.MoveTowards(locomotionBlend, targetBlend, locomotionBlendSpeed * Time.deltaTime);

            float bobPhase = playerCameraFeel.BobPhase;
            float phaseDelta = bobPhase - lastCameraBobPhase;
            lastCameraBobPhase = bobPhase;
            if (!isMoving || phaseDelta <= 0f)
            {
                return;
            }

            float frequencyMultiplier = IsSprinting ? sprintFrequencyMultiplier : 1f;
            locomotionPhase += phaseDelta * frequencyMultiplier;
        }

        private void ApplyViewmodelRootMotion(Vector3 baseLocalPosition)
        {
            if (viewmodelInstance == null)
            {
                return;
            }

            Transform root = viewmodelInstance.transform;
            root.localPosition = baseLocalPosition + GetLocomotionRootOffset();
            root.localRotation = Quaternion.Euler(viewmodelLocalEulerAngles) * GetLocomotionRootRotation();
        }

        private Vector3 GetLocomotionRootOffset()
        {
            float amplitude = GetLocomotionAmplitude();
            float verticalBob = Mathf.Sin(locomotionPhase * 2f) * locomotionBobAmplitude * amplitude;
            float lateralSway = Mathf.Cos(locomotionPhase) * locomotionSwayAmplitude * amplitude;
            return new Vector3(lateralSway, verticalBob, 0f);
        }

        private Quaternion GetLocomotionRootRotation()
        {
            float amplitude = GetLocomotionAmplitude();
            float roll = Mathf.Sin(locomotionPhase) * locomotionRollDegrees * amplitude;
            float pitch = Mathf.Sin(locomotionPhase * 2f) * locomotionPitchDegrees * amplitude;
            return Quaternion.Euler(pitch, 0f, roll);
        }

        private Vector3 GetIdleHandSwing()
        {
            float swing = Mathf.Sin(locomotionPhase) * locomotionHandSwingAmplitude * GetLocomotionAmplitude();
            return new Vector3(0f, swing * HAND_SWING_VERTICAL_RATIO, swing);
        }

        private float GetLocomotionAmplitude()
        {
            return locomotionBlend * (IsSprinting ? sprintAmplitudeMultiplier : 1f);
        }

        private bool IsSprinting =>
            playerController != null && playerController.IsSprinting && locomotionBlend > MIN_LOCOMOTION_BLEND;

        private void SolveIdleArm(
            Transform upper,
            Transform foreArm,
            Transform hand,
            Vector3 localHandPosition,
            Quaternion handRotation,
            float side)
        {
            Vector3 target = mainCamera.transform.TransformPoint(localHandPosition);
            Vector3 pole = -mainCamera.transform.up
                           - mainCamera.transform.forward * 0.35f
                           + mainCamera.transform.right * side * 0.25f;
            FpArmPose.SolveArm(upper, foreArm, hand, target, handRotation, pole);
        }

        private void UpdateArm(
            Transform upper,
            Transform foreArm,
            Transform hand,
            Transform middle,
            Transform grip,
            Quaternion gunRotation,
            float side,
            Vector3 handEulerOffset)
        {
            if (upper == null || foreArm == null || hand == null || grip == null)
            {
                return;
            }

            Transform cameraTransform = mainCamera != null ? mainCamera.transform : transform;
            Vector3 pole = -cameraTransform.up
                           - cameraTransform.forward * 0.35f
                           + cameraTransform.right * side * 0.25f;
            FpArmPose.SolveArm(upper, foreArm, hand, grip.position, gunRotation, pole);
            FpArmPose.AlignHand(hand, middle, gunRotation);
            hand.rotation *= Quaternion.Euler(handEulerOffset);
        }

        private void LogProbe(GunItem held)
        {
            if (!enableProbe || Time.time < nextProbeTime)
            {
                return;
            }

            nextProbeTime = Time.time + PROBE_INTERVAL;
            string right = DescribeArm("right", rightUpperArm, rightForeArm, rightHand, held.GripPrimary);
            string left = held.HoldType == GunHoldType.OneHand
                ? "left=none"
                : DescribeArm("left", leftUpperArm, leftForeArm, leftHand, held.GripSupport);

            Debug.Log(
                "[FpArmsProbe]" +
                " gun=" + held.name +
                " camera=" + (mainCamera != null ? mainCamera.transform.position.ToString("F3") : "null") +
                " viewmodel=" + (viewmodelInstance != null ? viewmodelInstance.transform.position.ToString("F3") : "null") +
                " " + right +
                " " + left);
        }

        private static string DescribeArm(
            string label,
            Transform upper,
            Transform foreArm,
            Transform hand,
            Transform grip)
        {
            if (upper == null || foreArm == null || hand == null || grip == null)
            {
                return label + "=none";
            }

            float upperLength = Vector3.Distance(upper.position, foreArm.position);
            float foreArmLength = Vector3.Distance(foreArm.position, hand.position);
            float distance = Vector3.Distance(upper.position, grip.position);
            float maxReach = upperLength + foreArmLength;

            return label +
                   " shoulder=" + upper.position.ToString("F3") +
                   " grip=" + grip.position.ToString("F3") +
                   " dist=" + distance.ToString("F3") +
                   " max=" + maxReach.ToString("F3") +
                   " reachable=" + (distance <= maxReach);
        }

        private void RestoreGun()
        {
            if (currentGun != null)
            {
                SetGunLayer(currentGun, 0);
            }

            currentGun = null;
        }

        private static void SetGunLayer(GunItem gun, int layer)
        {
            foreach (Renderer renderer in gun.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.layer = layer;
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        private static Transform FindBone(Transform root, string name)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                {
                    return transform;
                }
            }

            return null;
        }
    }
}
