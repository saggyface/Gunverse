using UnityEngine;

namespace GunVerse
{
    public static class TpArmPose
    {
        public static void SolveArm(
            Transform upper,
            Transform foreArm,
            Transform hand,
            Vector3 target,
            Quaternion handRotation,
            Vector3 pole)
        {
            if (upper == null || foreArm == null || hand == null)
            {
                return;
            }

            Vector3 shoulder = upper.position;
            Vector3 elbow = foreArm.position;
            float upperLength = Vector3.Distance(shoulder, elbow);
            float foreArmLength = Vector3.Distance(elbow, hand.position);
            Vector3 toTarget = target - shoulder;
            float distance = toTarget.magnitude;
            if (distance < 0.001f)
            {
                return;
            }

            float clamped = Mathf.Clamp(
                distance,
                Mathf.Abs(upperLength - foreArmLength) + 0.001f,
                upperLength + foreArmLength - 0.001f);
            Vector3 direction = toTarget.normalized;
            Vector3 poleDirection = pole - Vector3.Dot(pole, direction) * direction;
            if (poleDirection.sqrMagnitude < 0.0001f)
            {
                poleDirection = Vector3.down;
            }

            poleDirection.Normalize();
            float distanceAlong = (upperLength * upperLength - foreArmLength * foreArmLength + clamped * clamped) / (2f * clamped);
            float height = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - distanceAlong * distanceAlong));
            Vector3 elbowTarget = shoulder + direction * distanceAlong + poleDirection * height;

            AlignBone(upper, foreArm, elbowTarget);
            AlignBone(foreArm, hand, target);
            hand.rotation = handRotation;
        }

        public static void AlignHand(Transform hand, Transform middle, Quaternion handRotation, Vector3 localEulerOffset)
        {
            if (hand == null || middle == null)
            {
                return;
            }

            Vector3 fingerDirection = middle.position - hand.position;
            if (fingerDirection.sqrMagnitude < 0.000001f)
            {
                return;
            }

            Vector3 forward = handRotation * Vector3.forward;
            hand.rotation = Quaternion.FromToRotation(fingerDirection.normalized, forward) * hand.rotation;

            Vector3 handUp = Vector3.ProjectOnPlane(hand.up, forward);
            Vector3 desiredUp = Vector3.ProjectOnPlane(handRotation * Vector3.up, forward);
            if (handUp.sqrMagnitude > 0.0001f && desiredUp.sqrMagnitude > 0.0001f)
            {
                float angle = Vector3.SignedAngle(handUp, desiredUp, forward);
                hand.rotation = Quaternion.AngleAxis(angle, forward) * hand.rotation;
            }

            hand.rotation *= Quaternion.Euler(localEulerOffset);
        }

        private static void AlignBone(Transform bone, Transform child, Vector3 desiredChildPosition)
        {
            Vector3 current = child.position - bone.position;
            Vector3 desired = desiredChildPosition - bone.position;
            if (current.sqrMagnitude < 0.000001f || desired.sqrMagnitude < 0.000001f)
            {
                return;
            }

            bone.rotation = Quaternion.FromToRotation(current, desired) * bone.rotation;
        }
    }
}
