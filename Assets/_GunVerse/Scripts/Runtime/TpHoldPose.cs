using UnityEngine;

namespace GunVerse
{
    public static class TpHoldPose
    {
        public const float PITCH_PIVOT_HEIGHT = 1.38f;

        public static void Compute(
            Vector3 rootPosition,
            float aimYaw,
            float aimPitch,
            float holdHeight,
            float holdDistance,
            Vector3 pickupRotationEuler,
            Vector3 itemLocalCenter,
            Vector3 itemScale,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, aimYaw, 0f);
            Quaternion pitchRotation = Quaternion.Euler(aimPitch, 0f, 0f);
            worldRotation = yawRotation * pitchRotation * Quaternion.Euler(pickupRotationEuler);

            Vector3 pivot = rootPosition + Vector3.up * PITCH_PIVOT_HEIGHT;
            Vector3 offset = new Vector3(0f, holdHeight - PITCH_PIVOT_HEIGHT, holdDistance);
            Vector3 desiredCenter = pivot + yawRotation * (pitchRotation * offset);
            Vector3 scaledLocalCenter = Vector3.Scale(itemLocalCenter, itemScale);

            worldPosition = desiredCenter - worldRotation * scaledLocalCenter;
        }
    }
}
