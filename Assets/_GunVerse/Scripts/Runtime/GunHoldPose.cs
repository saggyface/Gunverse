using UnityEngine;

namespace GunVerse
{
    public static class GunHoldPose
    {
        public const float FP_HOLD_OFFSET_SCALE = 0.25f;

        public static void GetFpPose(
            Vector3 handPosition,
            Quaternion handRotation,
            Vector3 pickupPositionOffset,
            Vector3 pickupRotationEuler,
            float recoilPitch,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = handPosition + handRotation * (pickupPositionOffset * FP_HOLD_OFFSET_SCALE);
            worldRotation = handRotation * Quaternion.Euler(
                pickupRotationEuler.x - recoilPitch,
                pickupRotationEuler.y,
                pickupRotationEuler.z);
        }

        public static void GetCenteredFpPose(
            Vector3 handPosition,
            Quaternion handRotation,
            Vector3 pickupPositionOffset,
            Vector3 pickupRotationEuler,
            float recoilPitch,
            Vector3 itemLocalCenter,
            Vector3 itemScale,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            GetFpPose(
                handPosition,
                handRotation,
                pickupPositionOffset,
                pickupRotationEuler,
                recoilPitch,
                out worldPosition,
                out worldRotation);

            worldPosition -= worldRotation * Vector3.Scale(itemLocalCenter, itemScale);
        }
    }
}
