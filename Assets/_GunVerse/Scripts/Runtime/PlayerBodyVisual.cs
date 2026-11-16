using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GunVerse
{
    public sealed class PlayerBodyVisual : NetworkBehaviour
    {
        [Header("Body")]
        [SerializeField] private GameObject bodyVisual;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private PlayerAnimationState animationState;
        [SerializeField] private SkinnedMeshRenderer bodyRenderer;
        [SerializeField] private SkinnedMeshRenderer headRenderer;
        [SerializeField] private Transform headSocket;
        [Tooltip("Extra local yaw applied to the model. The Quaternius mannequin faces +Z, so this should stay at 0.")]
        [SerializeField] private float modelForwardOffset;

        [Header("Third Person Hold")]
        [SerializeField] private float tpPitchSmoothTime = 0.06f;

        [Header("Arms")]
        [SerializeField] private Transform rightUpperArm;
        [SerializeField] private Transform rightForeArm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform rightMiddle;
        [SerializeField] private Transform leftUpperArm;
        [SerializeField] private Transform leftForeArm;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform leftMiddle;
        [SerializeField] private Vector3 gripRightHandLocalEulerOffset;
        [SerializeField] private Vector3 gripLeftHandLocalEulerOffset;
        [SerializeField] private Vector3 leftHandRestLocalPosition = new Vector3(-0.28f, 0.98f, 0.08f);
        [SerializeField] private Vector3 leftHandRestLocalEulerAngles;
        [SerializeField] private bool enableArmProbe = true;

        private GameObject currentGunVisual;
        private GunItem currentGun;
        private PlayerHealth playerHealth;
        private bool armBaseCached;
        private Quaternion rightUpperArmRest;
        private Quaternion rightForeArmRest;
        private Quaternion rightHandRest;
        private Quaternion leftUpperArmRest;
        private Quaternion leftForeArmRest;
        private Quaternion leftHandRest;
        private float tpPitch;
        private float tpPitchVelocity;
        private float nextArmProbeTime;

        public Transform BodyVisualTransform => bodyVisual != null ? bodyVisual.transform : null;
        public GameObject TpGunVisual => currentGunVisual;
        public Animator Animator => animator;

        public override void OnNetworkSpawn()
        {
            playerHealth = GetComponent<PlayerHealth>();

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            CacheArmBase();

            if (IsOwner)
            {
                enabled = false;
                if (bodyVisual != null)
                {
                    bodyVisual.SetActive(false);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            DestroyGunVisual();
        }

        private void CacheArmBase()
        {
            if (armBaseCached)
            {
                return;
            }

            if (rightUpperArm == null || rightForeArm == null || rightHand == null ||
                leftUpperArm == null || leftForeArm == null || leftHand == null)
            {
                return;
            }

            rightUpperArmRest = rightUpperArm.localRotation;
            rightForeArmRest = rightForeArm.localRotation;
            rightHandRest = rightHand.localRotation;
            leftUpperArmRest = leftUpperArm.localRotation;
            leftForeArmRest = leftForeArm.localRotation;
            leftHandRest = leftHand.localRotation;
            armBaseCached = true;
        }

        private void RestoreRightArmBase()
        {
            if (!armBaseCached)
            {
                return;
            }

            rightUpperArm.localRotation = rightUpperArmRest;
            rightForeArm.localRotation = rightForeArmRest;
            rightHand.localRotation = rightHandRest;
        }

        private void RestoreLeftArmBase()
        {
            if (!armBaseCached)
            {
                return;
            }

            leftUpperArm.localRotation = leftUpperArmRest;
            leftForeArm.localRotation = leftForeArmRest;
            leftHand.localRotation = leftHandRest;
        }

        private void LateUpdate()
        {
            UpdateLocomotion();
            UpdateHeldVisual();
        }

        private void UpdateLocomotion()
        {
            if (bodyVisual != null)
            {
                bodyVisual.transform.localRotation = Quaternion.Euler(0f, modelForwardOffset, 0f);
            }

            if (animator == null)
            {
                return;
            }

            if (playerHealth != null && playerHealth.IsDead)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("Grounded", true);
                animator.SetBool("Jumping", false);
                animator.SetBool("Dead", true);
                return;
            }

            PlayerLocomotionState state = animationState != null
                ? animationState.LocomotionState
                : PlayerLocomotionState.Idle;
            float targetSpeed = state switch
            {
                PlayerLocomotionState.Walk => 0.5f,
                PlayerLocomotionState.Run => 1f,
                PlayerLocomotionState.Air => 0.5f,
                _ => 0f
            };
            animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("Grounded", state != PlayerLocomotionState.Air);
            animator.SetBool("Jumping", animationState != null && animationState.IsJumping);
            animator.SetBool("Dead", false);
        }

        private void UpdateHeldVisual()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                DestroyGunVisual();
                return;
            }

            GunItem held = GunItem.FindHeld(OwnerClientId);
            if (held != currentGun)
            {
                DestroyGunVisual();
                currentGun = held;
                if (held != null)
                {
                    CreateGunVisual(held);
                }
            }

            if (currentGun == null || currentGunVisual == null)
            {
                return;
            }

            CacheArmBase();

            float aimYaw = transform.eulerAngles.y;
            float targetPitch = animationState != null ? animationState.ViewPitchDegrees : 0f;
            tpPitch = Mathf.SmoothDampAngle(tpPitch, targetPitch, ref tpPitchVelocity, tpPitchSmoothTime);

            TpHoldPose.Compute(
                transform.position,
                aimYaw,
                tpPitch,
                currentGun.TpHoldHeight,
                currentGun.TpHoldDistance,
                currentGun.PickupRotationEuler,
                currentGun.LocalBounds.center,
                currentGun.PickupScale,
                out Vector3 position,
                out Quaternion rotation);

            currentGunVisual.transform.SetPositionAndRotation(position, rotation);
            currentGunVisual.transform.localScale = currentGun.PickupScale;

            ProjectileMuzzleCache.Set(
                OwnerClientId,
                currentGunVisual.transform.TransformPoint(currentGun.MuzzleOffset));

            GunItem visualGun = currentGunVisual.GetComponent<GunItem>();
            if (visualGun != null)
            {
                RestoreRightArmBase();
                UpdateArm(rightUpperArm, rightForeArm, rightHand, rightMiddle, visualGun.GripPrimary, rotation, 1f, "right");
                if (currentGun.HoldType != GunHoldType.OneHand && visualGun.GripSupport != null)
                {
                    RestoreLeftArmBase();
                    UpdateArm(leftUpperArm, leftForeArm, leftHand, leftMiddle, visualGun.GripSupport, rotation, -1f, "left");
                }
                else
                {
                    UpdateLeftRestArm(rotation);
                }
            }
        }

        private void UpdateLeftRestArm(Quaternion itemRotation)
        {
            if (!armBaseCached)
            {
                return;
            }

            RestoreLeftArmBase();
            Vector3 target = transform.TransformPoint(leftHandRestLocalPosition);
            Quaternion handRotation = itemRotation * Quaternion.Euler(leftHandRestLocalEulerAngles);
            Vector3 pole = Vector3.down - transform.forward * 0.35f - transform.right * 0.25f;
            TpArmPose.SolveArm(leftUpperArm, leftForeArm, leftHand, target, handRotation, pole);
            TpArmPose.AlignHand(leftHand, leftMiddle, handRotation, gripLeftHandLocalEulerOffset);
        }

        private void UpdateArm(
            Transform upper,
            Transform foreArm,
            Transform hand,
            Transform middle,
            Transform grip,
            Quaternion gunRotation,
            float side,
            string label)
        {
            if (upper == null || foreArm == null || hand == null || grip == null)
            {
                return;
            }

            Vector3 target = grip.position;
            Vector3 pole = Vector3.down - transform.forward * 0.35f + transform.right * side * 0.25f;
            Vector3 handOffset = side > 0f ? gripRightHandLocalEulerOffset : gripLeftHandLocalEulerOffset;
            TpArmPose.SolveArm(upper, foreArm, hand, target, gunRotation, pole);
            TpArmPose.AlignHand(hand, middle, gunRotation, handOffset);

            if (enableArmProbe && Time.time >= nextArmProbeTime)
            {
                nextArmProbeTime = Time.time + 2f;
                float upperLength = Vector3.Distance(upper.position, foreArm.position);
                float foreArmLength = Vector3.Distance(foreArm.position, hand.position);
                float distance = Vector3.Distance(upper.position, target);
                Debug.Log(
                    "[TpArmProbe] " + label +
                    " dist=" + distance.ToString("F3") +
                    " max=" + (upperLength + foreArmLength).ToString("F3") +
                    " handGrip=" + Vector3.Distance(hand.position, target).ToString("F3") +
                    " reachable=" + (distance <= upperLength + foreArmLength));
            }
        }

        private void CreateGunVisual(GunItem held)
        {
            currentGunVisual = Instantiate(held.gameObject);
            currentGunVisual.name = "TPGunVisual";
            StripVisual(currentGunVisual, held);
            LogVisualIsolation(held);

            foreach (Renderer renderer in held.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
        }

        private void LogVisualIsolation(GunItem held)
        {
            NetworkObject cloneNetworkObject = currentGunVisual.GetComponent<NetworkObject>();
            NetworkTransform cloneNetworkTransform = currentGunVisual.GetComponent<NetworkTransform>();
            Rigidbody cloneRigidbody = currentGunVisual.GetComponent<Rigidbody>();
            NetworkObject liveNetworkObject = held.GetComponent<NetworkObject>();
            Rigidbody liveRigidbody = held.GetComponent<Rigidbody>();

            ProjectileTrace.Log(
                "tp clone gun=" + held.name +
                " cloneObject=" + (cloneNetworkObject != null && cloneNetworkObject.enabled) +
                " cloneTransform=" + (cloneNetworkTransform != null && cloneNetworkTransform.enabled) +
                " cloneKinematic=" + (cloneRigidbody != null && cloneRigidbody.isKinematic) +
                " cloneColliders=" + CountEnabledColliders(currentGunVisual) +
                " liveObject=" + (liveNetworkObject != null && liveNetworkObject.enabled) +
                " liveSpawned=" + (liveNetworkObject != null && liveNetworkObject.IsSpawned) +
                " liveKinematic=" + (liveRigidbody != null && liveRigidbody.isKinematic) +
                " liveColliders=" + CountEnabledColliders(held.gameObject));
        }

        private static int CountEnabledColliders(GameObject root)
        {
            int count = 0;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && collider.enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private void DestroyGunVisual()
        {
            if (currentGunVisual != null)
            {
                Destroy(currentGunVisual);
                currentGunVisual = null;
            }

            if (currentGun != null)
            {
                foreach (Renderer renderer in currentGun.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = true;
                }
            }

            currentGun = null;
        }

        private static void StripVisual(GameObject visual, GunItem held)
        {
            foreach (NetworkBehaviour behaviour in visual.GetComponentsInChildren<NetworkBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (NetworkObject networkObject in visual.GetComponentsInChildren<NetworkObject>(true))
            {
                networkObject.enabled = false;
            }

            foreach (NetworkTransform networkTransform in visual.GetComponentsInChildren<NetworkTransform>(true))
            {
                networkTransform.enabled = false;
            }

            foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
                rigidbody.useGravity = false;
            }

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (GunItemLabelVisual label in visual.GetComponentsInChildren<GunItemLabelVisual>(true))
            {
                label.gameObject.SetActive(false);
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
            }

            Transform root = visual.transform;
            root.localScale = held.PickupScale;
        }

    }
}
