using UnityEngine;

namespace GunVerse
{
    public enum ProjectileShapeType : byte
    {
        Sphere = 0,
        Capsule = 1,
        Box = 2
    }

    public static class ProjectileShapeUtility
    {
        public const float SPHERE_MAX_ASPECT_RATIO = 1.25f;
        public const float THIN_PANEL_MIN_RATIO = 2.5f;
        public const float MIN_CAPSULE_HALF_LENGTH = 0.05f;
        public const float MIN_SHAPE_EXTENT = 0.01f;

        private const float MIN_MASS = 0.01f;
        private const float MIN_INERTIA = 0.0001f;
        private const float SPHERE_INERTIA_COEFFICIENT = 0.4f;
        private const float BOX_INERTIA_DIVISOR = 12f;

        public static ProjectileShapeType ResolveShapeType(Vector3 halfExtents)
        {
            SortExtents(halfExtents, out float small, out float middle, out float large, out _);

            if (large <= MIN_SHAPE_EXTENT)
            {
                return ProjectileShapeType.Sphere;
            }

            // Thin panels can be near-square in their two larger axes, so the panel
            // check must run before the near-cube sphere check.
            if (middle / Mathf.Max(MIN_SHAPE_EXTENT, small) >= THIN_PANEL_MIN_RATIO)
            {
                return ProjectileShapeType.Box;
            }

            if (large / Mathf.Max(MIN_SHAPE_EXTENT, middle) < SPHERE_MAX_ASPECT_RATIO)
            {
                return ProjectileShapeType.Sphere;
            }

            float radius = Mathf.Max(MIN_SHAPE_EXTENT, middle);
            float halfLength = large - radius;
            if (halfLength < MIN_CAPSULE_HALF_LENGTH)
            {
                return ProjectileShapeType.Sphere;
            }

            return ProjectileShapeType.Capsule;
        }

        public static Vector3 GetShapeCenter(
            Vector3 position,
            Quaternion rotation,
            in ProjectileShotData shot)
        {
            return position + rotation * shot.localCenterOffset;
        }

        public static float GetSphereRadius(in ProjectileShotData shot)
        {
            Vector3 extents = GetAbsoluteExtents(shot.localHalfExtents);
            float radius = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
            return Mathf.Max(MIN_SHAPE_EXTENT, radius);
        }

        public static Vector3 GetBoxHalfExtents(in ProjectileShotData shot)
        {
            Vector3 extents = GetAbsoluteExtents(shot.localHalfExtents);
            return new Vector3(
                Mathf.Max(MIN_SHAPE_EXTENT, extents.x),
                Mathf.Max(MIN_SHAPE_EXTENT, extents.y),
                Mathf.Max(MIN_SHAPE_EXTENT, extents.z));
        }

        public static void GetCapsule(
            in ProjectileShotData shot,
            Vector3 position,
            Quaternion rotation,
            out Vector3 point1,
            out Vector3 point2,
            out float radius)
        {
            SortExtents(
                shot.localHalfExtents,
                out _,
                out float middle,
                out float large,
                out int largeAxis);
            Vector3 axis = GetLocalAxis(largeAxis);
            radius = Mathf.Max(MIN_SHAPE_EXTENT, middle);
            float halfLength = Mathf.Max(0f, large - radius);
            Vector3 center = GetShapeCenter(position, rotation, shot);

            point1 = center + rotation * (axis * halfLength);
            point2 = center - rotation * (axis * halfLength);
        }

        public static Vector3 GetInverseInertiaLocal(in ProjectileShotData shot)
        {
            float mass = Mathf.Max(MIN_MASS, shot.mass);

            switch ((ProjectileShapeType)shot.shapeType)
            {
                case ProjectileShapeType.Box:
                    return GetBoxInverseInertia(mass, GetBoxHalfExtents(shot) * 2f);

                case ProjectileShapeType.Capsule:
                    return GetCapsuleInverseInertia(mass, shot.localHalfExtents);

                default:
                    float radius = GetSphereRadius(shot);
                    float inertia = SPHERE_INERTIA_COEFFICIENT * mass * radius * radius;
                    return Vector3.one * GetInverseInertia(inertia);
            }
        }

        private static Vector3 GetCapsuleInverseInertia(float mass, Vector3 halfExtents)
        {
            SortExtents(halfExtents, out _, out float middle, out float large, out int largeAxis);
            float radius = Mathf.Max(MIN_SHAPE_EXTENT, middle);
            float halfLength = Mathf.Max(0f, large - radius);
            float diameter = radius * 2f;
            float length = (halfLength + radius) * 2f;

            switch (largeAxis)
            {
                case 0:
                    return GetBoxInverseInertia(mass, new Vector3(length, diameter, diameter));

                case 1:
                    return GetBoxInverseInertia(mass, new Vector3(diameter, length, diameter));

                default:
                    return GetBoxInverseInertia(mass, new Vector3(diameter, diameter, length));
            }
        }

        private static Vector3 GetBoxInverseInertia(float mass, Vector3 size)
        {
            float widthSquared = size.x * size.x;
            float heightSquared = size.y * size.y;
            float depthSquared = size.z * size.z;

            float inertiaX = mass / BOX_INERTIA_DIVISOR * (heightSquared + depthSquared);
            float inertiaY = mass / BOX_INERTIA_DIVISOR * (widthSquared + depthSquared);
            float inertiaZ = mass / BOX_INERTIA_DIVISOR * (widthSquared + heightSquared);

            return new Vector3(
                GetInverseInertia(inertiaX),
                GetInverseInertia(inertiaY),
                GetInverseInertia(inertiaZ));
        }

        private static float GetInverseInertia(float inertia)
        {
            if (float.IsNaN(inertia) || float.IsInfinity(inertia))
            {
                return 0f;
            }

            return 1f / Mathf.Max(MIN_INERTIA, inertia);
        }

        private static void SortExtents(
            Vector3 halfExtents,
            out float small,
            out float middle,
            out float large,
            out int largeAxis)
        {
            Vector3 extents = GetAbsoluteExtents(halfExtents);
            float x = extents.x;
            float y = extents.y;
            float z = extents.z;

            small = Mathf.Min(x, Mathf.Min(y, z));
            large = Mathf.Max(x, Mathf.Max(y, z));
            middle = x + y + z - small - large;

            if (large == x)
            {
                largeAxis = 0;
            }
            else if (large == y)
            {
                largeAxis = 1;
            }
            else
            {
                largeAxis = 2;
            }
        }

        private static Vector3 GetAbsoluteExtents(Vector3 halfExtents)
        {
            return new Vector3(
                SanitizeExtent(halfExtents.x),
                SanitizeExtent(halfExtents.y),
                SanitizeExtent(halfExtents.z));
        }

        private static float SanitizeExtent(float extent)
        {
            if (float.IsNaN(extent) || float.IsInfinity(extent))
            {
                return 0f;
            }

            return Mathf.Abs(extent);
        }

        private static Vector3 GetLocalAxis(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0:
                    return Vector3.right;

                case 1:
                    return Vector3.up;

                default:
                    return Vector3.forward;
            }
        }
    }
}
