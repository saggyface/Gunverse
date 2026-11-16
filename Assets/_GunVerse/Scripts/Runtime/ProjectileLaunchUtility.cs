using UnityEngine;

namespace GunVerse
{
    public static class ProjectileLaunchUtility
    {
        public static Vector3 CenterMuzzleHorizontally(
            Vector3 muzzlePosition,
            Vector3 cameraOrigin,
            Vector3 cameraRight)
        {
            Vector3 offset = muzzlePosition - cameraOrigin;
            Vector3 right = cameraRight.sqrMagnitude > 0.0001f
                ? cameraRight.normalized
                : Vector3.right;

            return cameraOrigin + (offset - Vector3.Project(offset, right));
        }

        public static Vector3 RemoveLateralOffset(Vector3 offset, Vector3 direction)
        {
            Vector3 forward = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            return offset - Vector3.Project(offset, right.normalized);
        }
    }
}
