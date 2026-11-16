using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public sealed class PlayerRigDiagnostics : NetworkBehaviour
    {
        private const float DEFAULT_LOG_INTERVAL = 0.5f;
        private const string HIPS_BONE = "mixamorig1:Hips";
        private const string SPINE_BONE = "mixamorig1:Spine2";
        private const string HEAD_BONE = "mixamorig1:Head";
        private const string LEFT_FOOT_BONE = "mixamorig1:LeftFoot";
        private const string LEFT_TOE_BONE = "mixamorig1:LeftToeBase";
        private const string RIGHT_FOOT_BONE = "mixamorig1:RightFoot";
        private const string RIGHT_TOE_BONE = "mixamorig1:RightToeBase";
        private const string SPEED_PARAMETER = "Speed";
        private const string GROUNDED_PARAMETER = "Grounded";
        private const string JUMPING_PARAMETER = "Jumping";
        private const string DEAD_PARAMETER = "Dead";

        [SerializeField] private bool enableLogging = true;
        [SerializeField] private float logInterval = DEFAULT_LOG_INTERVAL;

        private readonly StringBuilder line = new StringBuilder(512);

        private PlayerController playerController;
        private PlayerAnimationState animationState;
        private PlayerBodyVisual bodyVisual;
        private PlayerHealth playerHealth;
        private Animator animator;
        private Transform hipsBone;
        private Transform spineBone;
        private Transform headBone;
        private Transform leftFootBone;
        private Transform leftToeBone;
        private Transform rightFootBone;
        private Transform rightToeBone;
        private float nextLogTime;

        public override void OnNetworkSpawn()
        {
            playerController = GetComponent<PlayerController>();
            animationState = GetComponent<PlayerAnimationState>();
            bodyVisual = GetComponent<PlayerBodyVisual>();
            playerHealth = GetComponent<PlayerHealth>();
            animator = GetComponentInChildren<Animator>(true);
            CacheBones();
        }

        private void Update()
        {
            if (!enableLogging || !IsSpawned || Time.time < nextLogTime)
            {
                return;
            }

            nextLogTime = Time.time + Mathf.Max(0.1f, logInterval);
            LogSnapshot();
        }

        private void CacheBones()
        {
            foreach (Transform bone in GetComponentsInChildren<Transform>(true))
            {
                switch (bone.name)
                {
                    case HIPS_BONE:
                        hipsBone = bone;
                        break;
                    case SPINE_BONE:
                        spineBone = bone;
                        break;
                    case HEAD_BONE:
                        headBone = bone;
                        break;
                    case LEFT_FOOT_BONE:
                        leftFootBone = bone;
                        break;
                    case LEFT_TOE_BONE:
                        leftToeBone = bone;
                        break;
                    case RIGHT_FOOT_BONE:
                        rightFootBone = bone;
                        break;
                    case RIGHT_TOE_BONE:
                        rightToeBone = bone;
                        break;
                }
            }
        }

        private void LogSnapshot()
        {
            AnimatorStateInfo stateInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
            AnimatorClipInfo[] clipInfo = animator != null ? animator.GetCurrentAnimatorClipInfo(0) : null;
            string clipName = clipInfo != null && clipInfo.Length > 0 && clipInfo[0].clip != null
                ? clipInfo[0].clip.name
                : "none";
            GunItem held = GunItem.FindHeld(OwnerClientId);
            Transform bodyTransform = bodyVisual != null ? bodyVisual.BodyVisualTransform : null;
            float aimYaw = playerController != null
                ? playerController.transform.eulerAngles.y
                : transform.eulerAngles.y;

            line.Clear();
            line.Append("[RigProbe]");
            line.Append(" t=").Append(Time.time.ToString("F2"));
            line.Append(" owner=").Append(IsOwner);
            line.Append(" localId=").Append(NetworkManager != null ? NetworkManager.LocalClientId.ToString() : "null");
            line.Append(" ownerId=").Append(OwnerClientId);
            line.Append(" dead=").Append(playerHealth != null && playerHealth.IsDead);
            line.Append(" bodyActive=").Append(bodyTransform != null && bodyTransform.gameObject.activeSelf);
            line.Append(" rootYaw=").Append(Angle(playerController != null ? playerController.transform : null));
            line.Append(" bodyLocalYaw=").Append(bodyTransform != null ? bodyTransform.localEulerAngles.y.ToString("F1") : "null");
            line.Append(" bodyWorldYaw=").Append(Angle(bodyTransform));
            line.Append(" bodyAimOffset=").Append(YawOffset(bodyTransform, aimYaw));
            line.Append(" camPivotPos=").Append(Position(playerController != null ? playerController.CameraPivot : null));
            line.Append(" camPivotFwd=").Append(Direction(playerController != null ? playerController.CameraPivot : null));
            line.Append(" camFwd=").Append(Direction(
                playerController != null && playerController.PlayerCamera != null
                    ? playerController.PlayerCamera.transform
                    : null));
            line.Append(" hipsFwd=").Append(Direction(hipsBone));
            line.Append(" spineFwd=").Append(Direction(spineBone));
            line.Append(" headFwd=").Append(Direction(headBone));
            line.Append(" leftToe=").Append(ToeDirection(leftFootBone, leftToeBone));
            line.Append(" rightToe=").Append(ToeDirection(rightFootBone, rightToeBone));
            line.Append(" animator=").Append(animator != null && animator.enabled);
            line.Append(" culling=").Append(animator != null ? animator.cullingMode.ToString() : "null");
            line.Append(" state=").Append(clipName);
            line.Append(" norm=").Append(stateInfo.normalizedTime.ToString("F2"));
            line.Append(" speedParam=").Append(FloatParameter(SPEED_PARAMETER));
            line.Append(" groundedParam=").Append(BoolParameter(GROUNDED_PARAMETER));
            line.Append(" jumpingParam=").Append(BoolParameter(JUMPING_PARAMETER));
            line.Append(" deadParam=").Append(BoolParameter(DEAD_PARAMETER));
            line.Append(" planarVel=").Append(animationState != null ? animationState.PlanarVelocity.ToString() : "null");
            line.Append(" loco=").Append(animationState != null ? animationState.LocomotionState.ToString() : "null");
            line.Append(" jumpingNet=").Append(animationState != null && animationState.IsJumping);
            line.Append(" held=").Append(held != null ? held.name : "none");
            line.Append(" tpGun=").Append(bodyVisual != null && bodyVisual.TpGunVisual != null ? bodyVisual.TpGunVisual.name : "none");

            Debug.Log(line.ToString());
        }

        private string FloatParameter(string parameterName)
        {
            return animator != null ? animator.GetFloat(parameterName).ToString("F2") : "null";
        }

        private string BoolParameter(string parameterName)
        {
            return animator != null ? animator.GetBool(parameterName).ToString() : "null";
        }

        private static string Angle(Transform target)
        {
            return target != null ? target.eulerAngles.y.ToString("F1") : "null";
        }

        private static string YawOffset(Transform target, float referenceYaw)
        {
            return target != null
                ? Mathf.DeltaAngle(referenceYaw, target.eulerAngles.y).ToString("F1")
                : "null";
        }

        private static string Position(Transform target)
        {
            return target != null ? target.position.ToString("F2") : "null";
        }

        private static string Direction(Transform target)
        {
            return target != null ? target.forward.ToString("F2") : "null";
        }

        private static string ToeDirection(Transform foot, Transform toe)
        {
            if (foot == null || toe == null)
            {
                return "null";
            }

            Vector3 direction = toe.position - foot.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized.ToString("F2") : "zero";
        }
    }
}
