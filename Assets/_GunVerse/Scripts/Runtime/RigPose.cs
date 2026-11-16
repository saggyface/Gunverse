using UnityEngine;

namespace GunVerse
{
    public static class RigPose
    {
        public static void ApplyUpperBodyAim(Transform torso, Transform head, float aimYaw)
        {
            ApplyWorldYaw(torso, Mathf.DeltaAngle(GetPlanarYaw(torso), aimYaw));
            ApplyWorldYaw(head, Mathf.DeltaAngle(GetPlanarYaw(head), aimYaw));
        }

        public static void ApplyUpperBodyAimClamped(
            Transform torso,
            Transform head,
            float lowerBodyYaw,
            float aimYaw,
            float maxTorsoTwist,
            float maxHeadTwist)
        {
            float aimOffset = Mathf.DeltaAngle(lowerBodyYaw, aimYaw);
            float torsoTwist = Mathf.Clamp(aimOffset, -maxTorsoTwist, maxTorsoTwist);
            float headTwist = Mathf.Clamp(aimOffset - torsoTwist, -maxHeadTwist, maxHeadTwist);
            ApplyWorldYaw(torso, torsoTwist);
            ApplyWorldYaw(head, headTwist);
        }

        public static float GetPlanarYaw(Transform bone)
        {
            if (bone == null)
            {
                return 0f;
            }

            Vector3 forward = bone.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg
                : 0f;
        }

        public static void ApplyWorldYaw(Transform bone, float yaw)
        {
            if (bone == null || Mathf.Approximately(yaw, 0f))
            {
                return;
            }

            bone.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * bone.rotation;
        }
    }
}
