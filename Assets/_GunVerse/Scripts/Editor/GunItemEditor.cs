using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    [CustomEditor(typeof(GunItem))]
    public class GunItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GunItem gunItem = (GunItem)target;

            EditorGUILayout.Space();

            foreach (string warning in GunItemValidator.Validate(gunItem))
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            if (GUILayout.Button("Rebuild Network Prefab List"))
            {
                GunItemPrefabRegistry.Rebuild();
                EditorUtility.DisplayDialog("Gun Item", "Rebuilt the network prefab list.", "OK");
            }
        }
    }

    public static class GunItemValidator
    {
        public static List<string> Validate(GunItem item)
        {
            List<string> warnings = new List<string>();

            if (item.GetComponentInChildren<Collider>() == null)
            {
                warnings.Add("No collider found. Add a collider so the item can be picked up.");
            }

            MeshCollider[] meshColliders = item.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider meshCollider in meshColliders)
            {
                if (!meshCollider.convex)
                {
                    warnings.Add("MeshCollider '" + meshCollider.name + "' is not convex. Convex colliders are required on moving and held objects.");
                }
            }

            if (item.RoundsPerMinute <= 0f) warnings.Add("Rounds Per Minute must be greater than 0.");
            if (item.MagazineSize <= 0) warnings.Add("Magazine Size must be greater than 0.");
            if (item.ReloadDuration < 0f) warnings.Add("Reload Duration cannot be negative.");
            if (item.Damage < 0f) warnings.Add("Damage cannot be negative.");
            if (item.ProjectileSpeed <= 0f) warnings.Add("Projectile Speed must be greater than 0.");
            if (item.ProjectileLifetime <= 0f) warnings.Add("Projectile Lifetime must be greater than 0.");

            if (item.GetComponent<NetworkObject>() == null)
            {
                warnings.Add("A NetworkObject component is required.");
            }

            if (item.GetComponent<Rigidbody>() == null)
            {
                warnings.Add("A Rigidbody component is required for physical projectiles.");
            }

            GunProjectileBody projectileBody = item.GetComponent<GunProjectileBody>();
            if (projectileBody == null)
            {
                warnings.Add("A GunProjectileBody component is required for physical projectiles.");
                return warnings;
            }

            if (projectileBody.MaxActiveProjectiles <= 0)
            {
                warnings.Add("Max Active Projectiles must be greater than 0.");
            }

            return warnings;
        }
    }

    public static class GunItemGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawGizmos(GunItem item, GizmoType gizmoType)
        {
            Transform transform = item.transform;
            Vector3 muzzleWorld = transform.TransformPoint(item.MuzzleOffset);
            float handleSize = HandleUtility.GetHandleSize(muzzleWorld) * 0.1f;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(muzzleWorld, handleSize);
            Gizmos.DrawLine(muzzleWorld, muzzleWorld + transform.forward * (handleSize * 3f));

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, handleSize * 0.6f);
        }
    }
}
