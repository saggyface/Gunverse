using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;

namespace GunVerse.EditorTools
{
    public sealed class RigTuningController : MonoBehaviour
    {
        private const string VIEWMODEL_LAYER_NAME = "ViewModel";
        private const int FALLBACK_VIEWMODEL_LAYER = 8;
        private const float FIXED_VIEWMODEL_FOV = 70f;
        private const float IDLE_SWAY_AMPLITUDE = 0.006f;
        private const float IDLE_SWAY_FREQUENCY = 1.6f;
        private const float IDLE_BOB_AMPLITUDE = 0.004f;
        private const float IDLE_BOB_FREQUENCY = 1.1f;

        private enum AnimationMode
        {
            Auto,
            Force
        }

        private enum ForcedAnimation
        {
            Idle,
            Walk,
            Run,
            JumpStart,
            AirLoop,
            Death
        }

        [Header("Scene References")]
        [SerializeField] private GameObject viewmodelPrefab;
        [SerializeField] private GameObject bodyPrefab;
        [SerializeField] private Camera fpCamera;
        [SerializeField] private Camera tpCamera;
        [SerializeField] private Vector3 handLocalPosition = new Vector3(0f, -0.30f, 0.5f);
        [SerializeField] private Vector3 bodyPosition = new Vector3(0f, 0f, 4f);

        [Header("FP Viewmodel")]
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

        [Header("FOV")]
        [SerializeField] private float worldFov = 80f;
        [SerializeField] private float fixedViewmodelFov = FIXED_VIEWMODEL_FOV;
        [SerializeField] private bool matchWorldViewmodel;

        [Header("Held Item")]
        [SerializeField] private Vector3 pickupPositionOffset;
        [SerializeField] private Vector3 pickupRotationEuler;
        [SerializeField] private Vector3 pickupScale = Vector3.one;
        [SerializeField] private Vector3 muzzleOffset;
        [SerializeField] private float tpHoldHeight = 1.48f;
        [SerializeField] private float tpHoldDistance = 0.38f;
        [SerializeField] private Vector3 gripPrimaryLocalPosition;
        [SerializeField] private Vector3 gripPrimaryLocalEuler;
        [SerializeField] private Vector3 gripSupportLocalPosition;
        [SerializeField] private Vector3 gripSupportLocalEuler;

        [Header("Body Preview")]
        [SerializeField] private float aimYaw;
        [SerializeField] private Vector3 leftHandRestLocalPosition = new Vector3(-0.28f, 0.98f, 0.08f);
        [SerializeField] private Vector3 leftHandRestLocalEulerAngles;
        [SerializeField] private AnimationMode animationMode;
        [SerializeField] private ForcedAnimation forcedAnimation;
        [SerializeField] private bool runToggle;

        private readonly List<string> itemNames = new List<string>();
        private readonly List<string> itemPaths = new List<string>();
        private GameObject viewmodelInstance;
        private Camera overlayCamera;
        private Transform viewmodelRoot;
        private Transform rightUpperArm;
        private Transform rightForeArm;
        private Transform rightHand;
        private Transform rightMiddle;
        private Transform leftUpperArm;
        private Transform leftForeArm;
        private Transform leftHand;
        private Transform leftMiddle;
        private Transform handAnchor;
        private GameObject liveItem;
        private GunItem liveGun;
        private GameObject tpItem;
        private GunItem tpGun;
        private GameObject bodyInstance;
        private Transform bodyRoot;
        private Transform bodyRightUpperArm;
        private Transform bodyRightForeArm;
        private Transform bodyRightHand;
        private Transform bodyRightMiddle;
        private Transform bodyLeftUpperArm;
        private Transform bodyLeftForeArm;
        private Transform bodyLeftHand;
        private Transform bodyLeftMiddle;
        private Animator bodyAnimator;
        private bool bodyArmBaseCached;
        private Quaternion bodyRightUpperArmRest;
        private Quaternion bodyRightForeArmRest;
        private Quaternion bodyRightHandRest;
        private Quaternion bodyLeftUpperArmRest;
        private Quaternion bodyLeftForeArmRest;
        private Quaternion bodyLeftHandRest;
        private float nextArmProbeTime;
        private int viewmodelLayer;
        private int fpCameraMask;
        private string itemFilter = string.Empty;
        private string status = "Ready.";
        private Vector2 scroll;
        private bool tpMode;
        private int selectedItemIndex = -1;
        private PlayerRigOverride playerSaved;
        private ItemRigOverride itemSaved;

        private void Start()
        {
            if (fpCamera == null)
            {
                fpCamera = Camera.main;
            }

            if (fpCamera == null)
            {
                Debug.LogError("[RigTuningController] Missing FP camera.");
                enabled = false;
                return;
            }

            LoadItems();
            ReadPlayerBaseline();
            SetupFpPreview();
            SetupBodyPreview();

            int defaultItemIndex = itemNames.IndexOf("BatteryGun");
            SelectItem(defaultItemIndex >= 0 ? defaultItemIndex : itemNames.Count > 0 ? 0 : -1);
            SetTpMode(false);
            ApplyFov();
        }

        private void Update()
        {
            ApplyFov();
            UpdateFpPreview();
            UpdateBodyAnimation();
        }

        private void LateUpdate()
        {
            UpdateBodyPose();
        }

        private void OnGUI()
        {
            DrawTuningPanel();
            DrawAnimationPanel();
        }

        private void DrawTuningPanel()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, 470f, Screen.height - 20f), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("RIG TUNING", GUI.skin.box);
            if (GUILayout.Button(tpMode ? "Show First Person" : "Show Third Person Body"))
            {
                SetTpMode(!tpMode);
            }

            GUILayout.Space(8f);
            GUILayout.Label("FOV", GUI.skin.box);
            worldFov = FloatControl("World FOV", worldFov, 60f, 110f, 80f);
            GUILayout.BeginHorizontal();
            matchWorldViewmodel = GUILayout.Toggle(matchWorldViewmodel, "Viewmodel matches world");
            if (GUILayout.Button("R", GUILayout.Width(22f)))
            {
                matchWorldViewmodel = false;
            }

            GUILayout.EndHorizontal();
            if (!matchWorldViewmodel)
            {
                fixedViewmodelFov = FloatControl("Viewmodel FOV", fixedViewmodelFov, 20f, 120f, FIXED_VIEWMODEL_FOV);
            }

            GUILayout.Space(8f);
            GUILayout.Label("FP VIEWMODEL", GUI.skin.box);
            viewmodelLocalPosition = Vector3Control(
                "Held root",
                viewmodelLocalPosition,
                -3f,
                3f,
                playerSaved != null ? playerSaved.viewmodelLocalPosition : viewmodelLocalPosition);
            idleViewmodelLocalPosition = Vector3Control(
                "Idle root",
                idleViewmodelLocalPosition,
                -3f,
                3f,
                playerSaved != null ? playerSaved.idleViewmodelLocalPosition : idleViewmodelLocalPosition);
            viewmodelLocalEulerAngles = Vector3Control(
                "Root euler",
                viewmodelLocalEulerAngles,
                -180f,
                180f,
                playerSaved != null ? playerSaved.viewmodelLocalEulerAngles : viewmodelLocalEulerAngles);
            viewmodelScale = FloatControl(
                "Scale",
                viewmodelScale,
                0.01f,
                3f,
                playerSaved != null ? playerSaved.viewmodelScale : viewmodelScale);
            idleRightHandLocalPosition = Vector3Control(
                "Idle R hand",
                idleRightHandLocalPosition,
                -1f,
                1f,
                playerSaved != null ? playerSaved.idleRightHandLocalPosition : idleRightHandLocalPosition);
            idleLeftHandLocalPosition = Vector3Control(
                "Idle L hand",
                idleLeftHandLocalPosition,
                -1f,
                1f,
                playerSaved != null ? playerSaved.idleLeftHandLocalPosition : idleLeftHandLocalPosition);
            idleRightHandLocalEulerAngles = Vector3Control(
                "Idle R euler",
                idleRightHandLocalEulerAngles,
                -180f,
                180f,
                playerSaved != null ? playerSaved.idleRightHandLocalEulerAngles : idleRightHandLocalEulerAngles);
            idleLeftHandLocalEulerAngles = Vector3Control(
                "Idle L euler",
                idleLeftHandLocalEulerAngles,
                -180f,
                180f,
                playerSaved != null ? playerSaved.idleLeftHandLocalEulerAngles : idleLeftHandLocalEulerAngles);
            gripRightHandLocalEulerOffset = Vector3Control(
                "Grip R offset",
                gripRightHandLocalEulerOffset,
                -180f,
                180f,
                playerSaved != null ? playerSaved.gripRightHandLocalEulerOffset : gripRightHandLocalEulerOffset);
            gripLeftHandLocalEulerOffset = Vector3Control(
                "Grip L offset",
                gripLeftHandLocalEulerOffset,
                -180f,
                180f,
                playerSaved != null ? playerSaved.gripLeftHandLocalEulerOffset : gripLeftHandLocalEulerOffset);

            GUILayout.Space(8f);
            GUILayout.Label("HELD ITEM", GUI.skin.box);
            itemFilter = GUILayout.TextField(itemFilter);
            if (GUILayout.Button("None / empty hands"))
            {
                SelectItem(-1);
            }

            for (int i = 0; i < itemNames.Count; i++)
            {
                if (!string.IsNullOrEmpty(itemFilter) &&
                    itemNames[i].IndexOf(itemFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (GUILayout.Button((i == selectedItemIndex ? "> " : string.Empty) + itemNames[i]))
                {
                    SelectItem(i);
                }
            }

            if (liveGun != null && itemSaved != null)
            {
                GUILayout.Label("Selected: " + itemNames[selectedItemIndex]);
                pickupPositionOffset = Vector3Control(
                    "Pickup position",
                    pickupPositionOffset,
                    -2f,
                    2f,
                    itemSaved.pickupPositionOffset);
                pickupRotationEuler = Vector3Control(
                    "Pickup euler",
                    pickupRotationEuler,
                    -180f,
                    180f,
                    itemSaved.pickupRotationEuler);
                pickupScale = Vector3Control(
                    "Pickup scale",
                    pickupScale,
                    0.01f,
                    3f,
                    itemSaved.pickupScale);
                muzzleOffset = Vector3Control(
                    "Muzzle offset",
                    muzzleOffset,
                    -2f,
                    2f,
                    itemSaved.muzzleOffset);
                tpHoldHeight = FloatControl("TP hold height", tpHoldHeight, 0f, 3f, itemSaved.tpHoldHeight);
                tpHoldDistance = FloatControl("TP hold distance", tpHoldDistance, 0f, 3f, itemSaved.tpHoldDistance);
                gripPrimaryLocalPosition = Vector3Control(
                    "Grip primary pos",
                    gripPrimaryLocalPosition,
                    -1f,
                    1f,
                    itemSaved.gripPrimaryLocalPosition);
                gripPrimaryLocalEuler = Vector3Control(
                    "Grip primary euler",
                    gripPrimaryLocalEuler,
                    -180f,
                    180f,
                    itemSaved.gripPrimaryLocalEuler);
                if (liveGun.GripSupport != null)
                {
                    gripSupportLocalPosition = Vector3Control(
                        "Grip support pos",
                        gripSupportLocalPosition,
                        -1f,
                        1f,
                        itemSaved.gripSupportLocalPosition);
                    gripSupportLocalEuler = Vector3Control(
                        "Grip support euler",
                        gripSupportLocalEuler,
                        -180f,
                        180f,
                        itemSaved.gripSupportLocalEuler);
                }
            }

            GUILayout.Space(8f);
            GUILayout.Label("BODY PREVIEW", GUI.skin.box);
            aimYaw = FloatControl("Aim yaw", aimYaw, 0f, 360f, 0f);

            GUILayout.Space(8f);
            if (GUILayout.Button("Save Player Rig"))
            {
                SavePlayerRig();
            }

            if (liveGun != null && GUILayout.Button("Save Selected Item"))
            {
                SaveSelectedItem();
            }

            if (GUILayout.Button("Reset All To Saved"))
            {
                ResetAllToSaved();
            }

            if (GUILayout.Button("Apply Rig Overrides To Prefabs"))
            {
                RigOverrideStore.ApplyAll();
                status = "Applied overrides to prefabs.";
            }

            GUILayout.Space(8f);
            GUILayout.Label(status);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawAnimationPanel()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 330f, 10f, 320f, 330f), GUI.skin.box);
            GUILayout.Label("ANIMATIONS", GUI.skin.box);
            GUILayout.Label(animationMode == AnimationMode.Auto ? "Mode: Auto (WASD)" : "Mode: Force");
            if (GUILayout.Button("Auto (WASD)"))
            {
                animationMode = AnimationMode.Auto;
            }

            if (GUILayout.Button("Idle"))
            {
                SetForcedAnimation(ForcedAnimation.Idle);
            }

            if (GUILayout.Button("Walk"))
            {
                SetForcedAnimation(ForcedAnimation.Walk);
            }

            if (GUILayout.Button("Run"))
            {
                SetForcedAnimation(ForcedAnimation.Run);
            }

            if (GUILayout.Button("Jump Start"))
            {
                SetForcedAnimation(ForcedAnimation.JumpStart);
            }

            if (GUILayout.Button("Air Loop"))
            {
                SetForcedAnimation(ForcedAnimation.AirLoop);
            }

            if (GUILayout.Button("Death"))
            {
                SetForcedAnimation(ForcedAnimation.Death);
            }

            runToggle = GUILayout.Toggle(runToggle, "Run toggle (or Shift)");
            if (GUILayout.Button("Reset Animation"))
            {
                animationMode = AnimationMode.Auto;
                forcedAnimation = ForcedAnimation.Idle;
                aimYaw = 0f;
            }

            GUILayout.Label(animationMode == AnimationMode.Auto
                ? "WASD to drive Idle/Walk/Run in place."
                : "Forced state; body stays aligned to aim yaw.");
            GUILayout.EndArea();
        }

        private void SetForcedAnimation(ForcedAnimation animation)
        {
            animationMode = AnimationMode.Force;
            forcedAnimation = animation;
        }

        private void ApplyFov()
        {
            if (fpCamera != null)
            {
                fpCamera.fieldOfView = worldFov;
            }

            if (overlayCamera != null)
            {
                overlayCamera.fieldOfView = matchWorldViewmodel ? worldFov : fixedViewmodelFov;
            }
        }

        private void UpdateFpPreview()
        {
            if (viewmodelInstance == null || fpCamera == null)
            {
                return;
            }

            bool hasItem = liveItem != null && liveGun != null;
            viewmodelRoot.localPosition = hasItem ? viewmodelLocalPosition : idleViewmodelLocalPosition;
            viewmodelRoot.localRotation = Quaternion.Euler(viewmodelLocalEulerAngles);
            viewmodelRoot.localScale = Vector3.one * viewmodelScale;

            if (handAnchor != null)
            {
                handAnchor.localPosition = handLocalPosition;
                handAnchor.localRotation = Quaternion.identity;
            }

            if (hasItem)
            {
                GunHoldPose.GetCenteredFpPose(
                    handAnchor.position,
                    handAnchor.rotation,
                    pickupPositionOffset,
                    pickupRotationEuler,
                    0f,
                    liveGun.LocalBounds.center,
                    pickupScale,
                    out Vector3 itemPosition,
                    out Quaternion itemRotation);
                liveItem.transform.SetPositionAndRotation(itemPosition, itemRotation);
                liveItem.transform.localScale = pickupScale;
                ApplyGripTuning(liveGun);

                Quaternion gunRotation = liveItem.transform.rotation;
                UpdateFpGunArm(rightUpperArm, rightForeArm, rightHand, rightMiddle, liveGun.GripPrimary, gunRotation, 1f, gripRightHandLocalEulerOffset);
                if (liveGun.HoldType != GunHoldType.OneHand && liveGun.GripSupport != null)
                {
                    UpdateFpGunArm(leftUpperArm, leftForeArm, leftHand, leftMiddle, liveGun.GripSupport, gunRotation, -1f, gripLeftHandLocalEulerOffset);
                }
            }
            else
            {
                float sway = Mathf.Sin(Time.time * IDLE_SWAY_FREQUENCY) * IDLE_SWAY_AMPLITUDE;
                float bob = Mathf.Cos(Time.time * IDLE_BOB_FREQUENCY) * IDLE_BOB_AMPLITUDE;
                Quaternion rightHandRotation = fpCamera.transform.rotation * Quaternion.Euler(idleRightHandLocalEulerAngles);
                Quaternion leftHandRotation = fpCamera.transform.rotation * Quaternion.Euler(idleLeftHandLocalEulerAngles);
                UpdateIdleArm(rightUpperArm, rightForeArm, rightHand, idleRightHandLocalPosition + new Vector3(sway, bob, 0f), rightHandRotation, 1f);
                UpdateIdleArm(leftUpperArm, leftForeArm, leftHand, idleLeftHandLocalPosition + new Vector3(-sway, bob, 0f), leftHandRotation, -1f);
            }
        }

        private void UpdateBodyAnimation()
        {
            if (bodyAnimator == null)
            {
                return;
            }

            Vector2 input = ReadMoveInput();
            bool hasInput = input.sqrMagnitude > 0.01f;
            bool run = runToggle || IsRunHeld();

            if (animationMode == AnimationMode.Auto)
            {
                bodyAnimator.SetFloat("Speed", hasInput ? run ? 1f : 0.5f : 0f);
                bodyAnimator.SetBool("Grounded", true);
                bodyAnimator.SetBool("Jumping", false);
                bodyAnimator.SetBool("Dead", false);
                return;
            }

            switch (forcedAnimation)
            {
                case ForcedAnimation.Walk:
                    bodyAnimator.SetFloat("Speed", 0.5f);
                    bodyAnimator.SetBool("Grounded", true);
                    bodyAnimator.SetBool("Jumping", false);
                    bodyAnimator.SetBool("Dead", false);
                    break;
                case ForcedAnimation.Run:
                    bodyAnimator.SetFloat("Speed", 1f);
                    bodyAnimator.SetBool("Grounded", true);
                    bodyAnimator.SetBool("Jumping", false);
                    bodyAnimator.SetBool("Dead", false);
                    break;
                case ForcedAnimation.JumpStart:
                    bodyAnimator.SetFloat("Speed", 0.5f);
                    bodyAnimator.SetBool("Grounded", false);
                    bodyAnimator.SetBool("Jumping", true);
                    bodyAnimator.SetBool("Dead", false);
                    break;
                case ForcedAnimation.AirLoop:
                    bodyAnimator.SetFloat("Speed", 0.5f);
                    bodyAnimator.SetBool("Grounded", false);
                    bodyAnimator.SetBool("Jumping", false);
                    bodyAnimator.SetBool("Dead", false);
                    break;
                case ForcedAnimation.Death:
                    bodyAnimator.SetFloat("Speed", 0f);
                    bodyAnimator.SetBool("Grounded", true);
                    bodyAnimator.SetBool("Jumping", false);
                    bodyAnimator.SetBool("Dead", true);
                    break;
                default:
                    bodyAnimator.SetFloat("Speed", 0f);
                    bodyAnimator.SetBool("Grounded", true);
                    bodyAnimator.SetBool("Jumping", false);
                    bodyAnimator.SetBool("Dead", false);
                    break;
            }
        }

        private void CacheBodyArmBase()
        {
            if (bodyArmBaseCached)
            {
                return;
            }

            if (bodyRightUpperArm == null || bodyRightForeArm == null || bodyRightHand == null ||
                bodyLeftUpperArm == null || bodyLeftForeArm == null || bodyLeftHand == null)
            {
                return;
            }

            bodyRightUpperArmRest = bodyRightUpperArm.localRotation;
            bodyRightForeArmRest = bodyRightForeArm.localRotation;
            bodyRightHandRest = bodyRightHand.localRotation;
            bodyLeftUpperArmRest = bodyLeftUpperArm.localRotation;
            bodyLeftForeArmRest = bodyLeftForeArm.localRotation;
            bodyLeftHandRest = bodyLeftHand.localRotation;
            bodyArmBaseCached = true;
        }

        private void RestoreBodyRightArmBase()
        {
            if (!bodyArmBaseCached)
            {
                return;
            }

            bodyRightUpperArm.localRotation = bodyRightUpperArmRest;
            bodyRightForeArm.localRotation = bodyRightForeArmRest;
            bodyRightHand.localRotation = bodyRightHandRest;
        }

        private void RestoreBodyLeftArmBase()
        {
            if (!bodyArmBaseCached)
            {
                return;
            }

            bodyLeftUpperArm.localRotation = bodyLeftUpperArmRest;
            bodyLeftForeArm.localRotation = bodyLeftForeArmRest;
            bodyLeftHand.localRotation = bodyLeftHandRest;
        }

        private void UpdateBodyPose()
        {
            if (bodyRoot == null)
            {
                return;
            }

            bodyRoot.position = bodyPosition;
            bodyRoot.rotation = Quaternion.Euler(0f, aimYaw, 0f);

            if (animationMode == AnimationMode.Force && forcedAnimation == ForcedAnimation.Death)
            {
                if (tpItem != null && tpMode)
                {
                    tpItem.SetActive(false);
                }

                return;
            }

            if (tpItem != null && tpMode && !tpItem.activeSelf)
            {
                tpItem.SetActive(true);
            }

            if (!tpMode || tpItem == null || tpGun == null || liveGun == null)
            {
                return;
            }

            Quaternion aimRotation = Quaternion.Euler(0f, aimYaw, 0f);
            TpHoldPose.Compute(
                bodyRoot.position,
                aimYaw,
                0f,
                tpHoldHeight,
                tpHoldDistance,
                pickupRotationEuler,
                liveGun.LocalBounds.center,
                pickupScale,
                out Vector3 position,
                out Quaternion rotation);
            tpItem.transform.SetPositionAndRotation(position, rotation);
            tpItem.transform.localScale = pickupScale;
            ApplyGripTuning(tpGun);

            RestoreBodyRightArmBase();
            UpdateTpGunArm(
                bodyRightUpperArm,
                bodyRightForeArm,
                bodyRightHand,
                bodyRightMiddle,
                tpGun.GripPrimary,
                rotation,
                1f,
                gripRightHandLocalEulerOffset,
                "right");
            if (tpGun.HoldType != GunHoldType.OneHand && tpGun.GripSupport != null)
            {
                RestoreBodyLeftArmBase();
                UpdateTpGunArm(
                    bodyLeftUpperArm,
                    bodyLeftForeArm,
                    bodyLeftHand,
                    bodyLeftMiddle,
                    tpGun.GripSupport,
                    rotation,
                    -1f,
                    gripLeftHandLocalEulerOffset,
                    "left");
            }
            else
            {
                RestoreBodyLeftArmBase();
                Vector3 restTarget = bodyRoot.position + aimRotation * leftHandRestLocalPosition;
                Quaternion restRotation = rotation * Quaternion.Euler(leftHandRestLocalEulerAngles);
                Vector3 restPole = Vector3.down - aimRotation * Vector3.forward * 0.35f - aimRotation * Vector3.right * 0.25f;
                TpArmPose.SolveArm(bodyLeftUpperArm, bodyLeftForeArm, bodyLeftHand, restTarget, restRotation, restPole);
                TpArmPose.AlignHand(bodyLeftHand, bodyLeftMiddle, restRotation, gripLeftHandLocalEulerOffset);
            }
        }

        private void UpdateFpGunArm(
            Transform upper,
            Transform foreArm,
            Transform hand,
            Transform middle,
            Transform grip,
            Quaternion gunRotation,
            float side,
            Vector3 handOffset)
        {
            if (upper == null || foreArm == null || hand == null || grip == null)
            {
                return;
            }

            Vector3 pole = -fpCamera.transform.up
                           - fpCamera.transform.forward * 0.35f
                           + fpCamera.transform.right * side * 0.25f;
            FpArmPose.SolveArm(upper, foreArm, hand, grip.position, gunRotation, pole);
            FpArmPose.AlignHand(hand, middle, gunRotation);
            hand.rotation *= Quaternion.Euler(handOffset);
        }

        private void UpdateIdleArm(
            Transform upper,
            Transform foreArm,
            Transform hand,
            Vector3 localHandPosition,
            Quaternion handRotation,
            float side)
        {
            if (upper == null || foreArm == null || hand == null)
            {
                return;
            }

            Vector3 target = fpCamera.transform.TransformPoint(localHandPosition);
            Vector3 pole = -fpCamera.transform.up
                           - fpCamera.transform.forward * 0.35f
                           + fpCamera.transform.right * side * 0.25f;
            FpArmPose.SolveArm(upper, foreArm, hand, target, handRotation, pole);
        }

        private void UpdateTpGunArm(
            Transform upper,
            Transform foreArm,
            Transform hand,
            Transform middle,
            Transform grip,
            Quaternion gunRotation,
            float side,
            Vector3 handOffset,
            string label)
        {
            if (upper == null || foreArm == null || hand == null || grip == null)
            {
                return;
            }

            Vector3 pole = Vector3.down
                           - bodyRoot.forward * 0.35f
                           + bodyRoot.right * side * 0.25f;
            TpArmPose.SolveArm(upper, foreArm, hand, grip.position, gunRotation, pole);
            TpArmPose.AlignHand(hand, middle, gunRotation, handOffset);

            if (Time.time >= nextArmProbeTime)
            {
                nextArmProbeTime = Time.time + 2f;
                float upperLength = Vector3.Distance(upper.position, foreArm.position);
                float foreArmLength = Vector3.Distance(foreArm.position, hand.position);
                float distance = Vector3.Distance(upper.position, grip.position);
                Debug.Log(
                    "[TpArmProbe] " + label +
                    " dist=" + distance.ToString("F3") +
                    " max=" + (upperLength + foreArmLength).ToString("F3") +
                    " handGrip=" + Vector3.Distance(hand.position, grip.position).ToString("F3") +
                    " reachable=" + (distance <= upperLength + foreArmLength));
            }
        }

        private void ApplyGripTuning(GunItem gun)
        {
            if (gun == null)
            {
                return;
            }

            if (gun.GripPrimary != null)
            {
                gun.GripPrimary.localPosition = gripPrimaryLocalPosition;
                gun.GripPrimary.localRotation = Quaternion.Euler(gripPrimaryLocalEuler);
            }

            if (gun.GripSupport != null)
            {
                gun.GripSupport.localPosition = gripSupportLocalPosition;
                gun.GripSupport.localRotation = Quaternion.Euler(gripSupportLocalEuler);
            }
        }

        private void SetupFpPreview()
        {
            if (viewmodelPrefab == null)
            {
                Debug.LogError("[RigTuningController] Missing viewmodel prefab.");
                return;
            }

            viewmodelLayer = LayerMask.NameToLayer(VIEWMODEL_LAYER_NAME);
            if (viewmodelLayer < 0)
            {
                viewmodelLayer = FALLBACK_VIEWMODEL_LAYER;
            }

            fpCameraMask = fpCamera.cullingMask;
            fpCamera.cullingMask = fpCameraMask & ~(1 << viewmodelLayer);

            CreateOverlayCamera();

            viewmodelInstance = Instantiate(viewmodelPrefab, fpCamera.transform);
            viewmodelInstance.name = "TuningViewModelArms";
            viewmodelRoot = viewmodelInstance.transform;
            viewmodelRoot.localPosition = idleViewmodelLocalPosition;
            viewmodelRoot.localRotation = Quaternion.Euler(viewmodelLocalEulerAngles);
            viewmodelRoot.localScale = Vector3.one * viewmodelScale;
            SetLayerRecursively(viewmodelInstance, viewmodelLayer);

            rightUpperArm = FindBone(viewmodelRoot, "mixamorig1:RightArm");
            rightForeArm = FindBone(viewmodelRoot, "mixamorig1:RightForeArm");
            rightHand = FindBone(viewmodelRoot, "mixamorig1:RightHand");
            rightMiddle = FindBone(viewmodelRoot, "mixamorig1:RightHandMiddle1");
            leftUpperArm = FindBone(viewmodelRoot, "mixamorig1:LeftArm");
            leftForeArm = FindBone(viewmodelRoot, "mixamorig1:LeftForeArm");
            leftHand = FindBone(viewmodelRoot, "mixamorig1:LeftHand");
            leftMiddle = FindBone(viewmodelRoot, "mixamorig1:LeftHandMiddle1");

            GameObject hand = new GameObject("TuningHand");
            handAnchor = hand.transform;
            handAnchor.SetParent(fpCamera.transform, false);
            handAnchor.localPosition = handLocalPosition;
            handAnchor.localRotation = Quaternion.identity;
        }

        private void CreateOverlayCamera()
        {
            GameObject cameraObject = new GameObject("TuningViewModelCamera");
            cameraObject.transform.SetParent(fpCamera.transform, false);
            overlayCamera = cameraObject.AddComponent<Camera>();
            overlayCamera.clearFlags = CameraClearFlags.Depth;
            overlayCamera.cullingMask = 1 << viewmodelLayer;
            overlayCamera.fieldOfView = FIXED_VIEWMODEL_FOV;
            overlayCamera.nearClipPlane = 0.01f;
            overlayCamera.farClipPlane = 5f;
            overlayCamera.depth = fpCamera.depth + 1;
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = CameraRenderType.Overlay;

            UniversalAdditionalCameraData mainData = fpCamera.GetComponent<UniversalAdditionalCameraData>();
            if (mainData != null)
            {
                mainData.cameraStack.Add(overlayCamera);
            }
        }

        private void SetupBodyPreview()
        {
            if (bodyPrefab == null)
            {
                Debug.LogError("[RigTuningController] Missing body prefab.");
                return;
            }

            bodyInstance = Instantiate(bodyPrefab, bodyPosition, Quaternion.identity);
            bodyInstance.name = "TuningMannequinBody";
            bodyRoot = bodyInstance.transform;
            bodyRightUpperArm = FindBone(bodyRoot, "mixamorig1:RightArm");
            bodyRightForeArm = FindBone(bodyRoot, "mixamorig1:RightForeArm");
            bodyRightHand = FindBone(bodyRoot, "mixamorig1:RightHand");
            bodyRightMiddle = FindBone(bodyRoot, "mixamorig1:RightHandMiddle1");
            bodyLeftUpperArm = FindBone(bodyRoot, "mixamorig1:LeftArm");
            bodyLeftForeArm = FindBone(bodyRoot, "mixamorig1:LeftForeArm");
            bodyLeftHand = FindBone(bodyRoot, "mixamorig1:LeftHand");
            bodyLeftMiddle = FindBone(bodyRoot, "mixamorig1:LeftHandMiddle1");
            bodyAnimator = bodyInstance.GetComponentInChildren<Animator>(true);
            if (bodyAnimator != null)
            {
                bodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            CacheBodyArmBase();

            if (tpCamera != null)
            {
                tpCamera.transform.position = bodyPosition + new Vector3(0f, 1.4f, 3f);
                tpCamera.transform.LookAt(bodyPosition + new Vector3(0f, 1.0f, 0f));
            }
        }

        private void SelectItem(int index)
        {
            DestroyPreviewItem(liveItem);
            DestroyPreviewItem(tpItem);
            liveItem = null;
            liveGun = null;
            tpItem = null;
            tpGun = null;
            selectedItemIndex = index;
            itemSaved = null;

            if (index < 0 || index >= itemPaths.Count)
            {
                status = "Empty hands.";
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(itemPaths[index]);
            if (prefab == null)
            {
                status = "Missing prefab.";
                return;
            }

            liveItem = Instantiate(prefab);
            liveItem.name = "TuningHeldItem";
            StripItem(liveItem);
            liveGun = liveItem.GetComponent<GunItem>();
            if (liveGun == null)
            {
                DestroyPreviewItem(liveItem);
                liveItem = null;
                status = "Item has no GunItem.";
                return;
            }

            tpItem = Instantiate(prefab);
            tpItem.name = "TuningTpItem";
            StripItem(tpItem);
            tpGun = tpItem.GetComponent<GunItem>();
            SetLayerRecursively(tpItem, 0);
            tpItem.SetActive(false);

            itemSaved = ReadItemBaseline(liveGun, itemPaths[index]);
            ApplyItemToFields(itemSaved);
            SetLayerRecursively(liveItem, viewmodelLayer);
            liveItem.transform.localScale = pickupScale;
            status = "Selected " + itemNames[index] + ".";
        }

        private void DestroyPreviewItem(GameObject item)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        private void StripItem(GameObject item)
        {
            foreach (var behaviour in item.GetComponentsInChildren<Unity.Netcode.NetworkBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (var networkObject in item.GetComponentsInChildren<Unity.Netcode.NetworkObject>(true))
            {
                networkObject.enabled = false;
            }

            foreach (var networkTransform in item.GetComponentsInChildren<Unity.Netcode.Components.NetworkTransform>(true))
            {
                networkTransform.enabled = false;
            }

            foreach (Rigidbody rigidbody in item.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
                rigidbody.useGravity = false;
            }

            foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (GunItemLabelVisual label in item.GetComponentsInChildren<GunItemLabelVisual>(true))
            {
                label.gameObject.SetActive(false);
            }
        }

        private void SetTpMode(bool enabled)
        {
            tpMode = enabled;
            if (fpCamera != null)
            {
                fpCamera.enabled = !enabled;
            }

            if (tpCamera != null)
            {
                tpCamera.enabled = enabled;
            }

            if (viewmodelInstance != null)
            {
                viewmodelInstance.SetActive(!enabled);
            }

            if (overlayCamera != null)
            {
                overlayCamera.enabled = !enabled;
            }

            if (liveItem != null)
            {
                liveItem.SetActive(!enabled);
            }

            if (tpItem != null)
            {
                tpItem.SetActive(enabled);
            }

            if (bodyInstance != null)
            {
                bodyInstance.SetActive(enabled);
            }
        }

        private void LoadItems()
        {
            itemNames.Clear();
            itemPaths.Clear();
            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets/_GunVerse/Prefabs/Items", "Assets/_GunVerse/Prefabs/LargeProps" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<GunItem>() == null)
                {
                    continue;
                }

                itemNames.Add(prefab.name);
                itemPaths.Add(path);
            }

            int[] order = new int[itemNames.Count];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }

            System.Array.Sort(order, (left, right) => string.CompareOrdinal(itemNames[left], itemNames[right]));
            List<string> sortedNames = new List<string>(itemNames.Count);
            List<string> sortedPaths = new List<string>(itemPaths.Count);
            foreach (int index in order)
            {
                sortedNames.Add(itemNames[index]);
                sortedPaths.Add(itemPaths[index]);
            }

            itemNames.Clear();
            itemPaths.Clear();
            itemNames.AddRange(sortedNames);
            itemPaths.AddRange(sortedPaths);
        }

        private void ReadPlayerBaseline()
        {
            playerSaved = new PlayerRigOverride
            {
                viewmodelLocalPosition = viewmodelLocalPosition,
                idleViewmodelLocalPosition = idleViewmodelLocalPosition,
                viewmodelLocalEulerAngles = viewmodelLocalEulerAngles,
                viewmodelScale = viewmodelScale,
                idleRightHandLocalPosition = idleRightHandLocalPosition,
                idleLeftHandLocalPosition = idleLeftHandLocalPosition,
                idleRightHandLocalEulerAngles = idleRightHandLocalEulerAngles,
                idleLeftHandLocalEulerAngles = idleLeftHandLocalEulerAngles,
                gripRightHandLocalEulerOffset = gripRightHandLocalEulerOffset,
                gripLeftHandLocalEulerOffset = gripLeftHandLocalEulerOffset
            };

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab");
            if (prefab == null)
            {
                return;
            }

            PlayerFpArms arms = prefab.GetComponent<PlayerFpArms>();
            if (arms != null)
            {
                SerializedObject serializedObject = new SerializedObject(arms);
                playerSaved.viewmodelLocalPosition = ReadVector(serializedObject, "viewmodelLocalPosition");
                playerSaved.idleViewmodelLocalPosition = ReadVector(serializedObject, "idleViewmodelLocalPosition");
                playerSaved.viewmodelLocalEulerAngles = ReadVector(serializedObject, "viewmodelLocalEulerAngles");
                playerSaved.viewmodelScale = ReadFloat(serializedObject, "viewmodelScale");
                playerSaved.idleRightHandLocalPosition = ReadVector(serializedObject, "idleRightHandLocalPosition");
                playerSaved.idleLeftHandLocalPosition = ReadVector(serializedObject, "idleLeftHandLocalPosition");
                playerSaved.idleRightHandLocalEulerAngles = ReadVector(serializedObject, "idleRightHandLocalEulerAngles");
                playerSaved.idleLeftHandLocalEulerAngles = ReadVector(serializedObject, "idleLeftHandLocalEulerAngles");
                playerSaved.gripRightHandLocalEulerOffset = ReadVector(serializedObject, "gripRightHandLocalEulerOffset");
                playerSaved.gripLeftHandLocalEulerOffset = ReadVector(serializedObject, "gripLeftHandLocalEulerOffset");
            }

            PlayerBodyVisual bodyVisual = prefab.GetComponent<PlayerBodyVisual>();
            if (bodyVisual != null)
            {
                SerializedObject serializedObject = new SerializedObject(bodyVisual);
                playerSaved.gripRightHandLocalEulerOffset = ReadVector(serializedObject, "gripRightHandLocalEulerOffset");
                playerSaved.gripLeftHandLocalEulerOffset = ReadVector(serializedObject, "gripLeftHandLocalEulerOffset");
            }

            ApplyPlayerToFields(playerSaved);
        }

        private ItemRigOverride ReadItemBaseline(GunItem gun, string path)
        {
            ItemRigOverride data = new ItemRigOverride
            {
                itemPrefab = path,
                pickupPositionOffset = gun.PickupPositionOffset,
                pickupRotationEuler = gun.PickupRotationEuler,
                pickupScale = gun.PickupScale,
                muzzleOffset = gun.MuzzleOffset,
                tpHoldHeight = gun.TpHoldHeight,
                tpHoldDistance = gun.TpHoldDistance
            };

            if (gun.GripPrimary != null)
            {
                data.gripPrimaryLocalPosition = gun.GripPrimary.localPosition;
                data.gripPrimaryLocalEuler = gun.GripPrimary.localEulerAngles;
            }

            if (gun.GripSupport != null)
            {
                data.gripSupportLocalPosition = gun.GripSupport.localPosition;
                data.gripSupportLocalEuler = gun.GripSupport.localEulerAngles;
            }

            return data;
        }

        private void ApplyPlayerToFields(PlayerRigOverride data)
        {
            viewmodelLocalPosition = data.viewmodelLocalPosition;
            idleViewmodelLocalPosition = data.idleViewmodelLocalPosition;
            viewmodelLocalEulerAngles = data.viewmodelLocalEulerAngles;
            viewmodelScale = data.viewmodelScale;
            idleRightHandLocalPosition = data.idleRightHandLocalPosition;
            idleLeftHandLocalPosition = data.idleLeftHandLocalPosition;
            idleRightHandLocalEulerAngles = data.idleRightHandLocalEulerAngles;
            idleLeftHandLocalEulerAngles = data.idleLeftHandLocalEulerAngles;
            gripRightHandLocalEulerOffset = data.gripRightHandLocalEulerOffset;
            gripLeftHandLocalEulerOffset = data.gripLeftHandLocalEulerOffset;
        }

        private void ApplyItemToFields(ItemRigOverride data)
        {
            pickupPositionOffset = data.pickupPositionOffset;
            pickupRotationEuler = data.pickupRotationEuler;
            pickupScale = data.pickupScale;
            muzzleOffset = data.muzzleOffset;
            tpHoldHeight = data.tpHoldHeight;
            tpHoldDistance = data.tpHoldDistance;
            gripPrimaryLocalPosition = data.gripPrimaryLocalPosition;
            gripPrimaryLocalEuler = data.gripPrimaryLocalEuler;
            gripSupportLocalPosition = data.gripSupportLocalPosition;
            gripSupportLocalEuler = data.gripSupportLocalEuler;
        }

        private void SavePlayerRig()
        {
            PlayerRigOverride data = new PlayerRigOverride
            {
                viewmodelLocalPosition = viewmodelLocalPosition,
                idleViewmodelLocalPosition = idleViewmodelLocalPosition,
                viewmodelLocalEulerAngles = viewmodelLocalEulerAngles,
                viewmodelScale = viewmodelScale,
                idleRightHandLocalPosition = idleRightHandLocalPosition,
                idleLeftHandLocalPosition = idleLeftHandLocalPosition,
                idleRightHandLocalEulerAngles = idleRightHandLocalEulerAngles,
                idleLeftHandLocalEulerAngles = idleLeftHandLocalEulerAngles,
                gripRightHandLocalEulerOffset = gripRightHandLocalEulerOffset,
                gripLeftHandLocalEulerOffset = gripLeftHandLocalEulerOffset
            };
            RigOverrideStore.SavePlayer(data, true);
            playerSaved = data;
            status = "Saved player rig.";
        }

        private void SaveSelectedItem()
        {
            if (liveGun == null || selectedItemIndex < 0)
            {
                return;
            }

            ItemRigOverride data = new ItemRigOverride
            {
                itemPrefab = itemPaths[selectedItemIndex],
                pickupPositionOffset = pickupPositionOffset,
                pickupRotationEuler = pickupRotationEuler,
                pickupScale = pickupScale,
                muzzleOffset = muzzleOffset,
                tpHoldHeight = tpHoldHeight,
                tpHoldDistance = tpHoldDistance,
                gripPrimaryLocalPosition = gripPrimaryLocalPosition,
                gripPrimaryLocalEuler = gripPrimaryLocalEuler,
                gripSupportLocalPosition = gripSupportLocalPosition,
                gripSupportLocalEuler = gripSupportLocalEuler
            };
            RigOverrideStore.SaveItem(data, true);
            itemSaved = data;
            status = "Saved " + itemNames[selectedItemIndex] + ".";
        }

        private void ResetAllToSaved()
        {
            if (playerSaved != null)
            {
                ApplyPlayerToFields(playerSaved);
            }

            if (itemSaved != null)
            {
                ApplyItemToFields(itemSaved);
            }

            worldFov = 80f;
            fixedViewmodelFov = FIXED_VIEWMODEL_FOV;
            matchWorldViewmodel = false;
            aimYaw = 0f;
            animationMode = AnimationMode.Auto;
            forcedAnimation = ForcedAnimation.Idle;
            runToggle = false;
            status = "Reset to last saved values.";
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            return input;
        }

        private static bool IsRunHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        private static Vector3 ReadVector(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.vector3Value : Vector3.zero;
        }

        private static float ReadFloat(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.floatValue : 0f;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        private static Transform FindBone(Transform root, string boneName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == boneName)
                {
                    return child;
                }
            }

            return null;
        }

        private static float FloatControl(string label, float value, float min, float max, float resetValue)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150f));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(110f));
            value = EditorGUILayout.FloatField(value, GUILayout.Width(62f));
            if (GUILayout.Button("R", GUILayout.Width(22f)))
            {
                value = resetValue;
            }

            GUILayout.EndHorizontal();
            return Mathf.Clamp(value, min, max);
        }

        private static Vector3 Vector3Control(string label, Vector3 value, float min, float max, Vector3 resetValue)
        {
            GUILayout.Label(label);
            value.x = FloatControl("  X", value.x, min, max, resetValue.x);
            value.y = FloatControl("  Y", value.y, min, max, resetValue.y);
            value.z = FloatControl("  Z", value.z, min, max, resetValue.z);
            return value;
        }
    }
}
#endif
