using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class GunPerformanceSettings
    {
        public const float GunPositionThreshold = 0.01f;
        public const float GunRotationThreshold = 0.1f;

        public static void ApplyToGun(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            NetworkTransform networkTransform = root.GetComponentInChildren<NetworkTransform>(true);
            if (networkTransform != null)
            {
                ApplyGunNetworkTransform(networkTransform);
            }

            GunPropPhysicsAssets.ApplyForWeightClass(root);
        }

        public static void ApplyToPlayer(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            NetworkTransform networkTransform = root.GetComponentInChildren<NetworkTransform>(true);
            if (networkTransform != null)
            {
                DisableScaleSync(networkTransform);
            }
        }

        public static void ApplyGunNetworkTransform(NetworkTransform networkTransform)
        {
            if (networkTransform == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(networkTransform);
            DisableScaleSync(serializedObject);

            SerializedProperty positionThreshold = serializedObject.FindProperty("PositionThreshold");
            if (positionThreshold != null)
            {
                positionThreshold.floatValue = GunPositionThreshold;
            }

            SerializedProperty rotationThreshold = serializedObject.FindProperty("RotAngleThreshold");
            if (rotationThreshold != null)
            {
                rotationThreshold.floatValue = GunRotationThreshold;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(networkTransform);
        }

        public static void DisableScaleSync(NetworkTransform networkTransform)
        {
            if (networkTransform == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(networkTransform);
            DisableScaleSync(serializedObject);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(networkTransform);
        }

        private static void DisableScaleSync(SerializedObject serializedObject)
        {
            SetBoolean(serializedObject, "SyncScaleX");
            SetBoolean(serializedObject, "SyncScaleY");
            SetBoolean(serializedObject, "SyncScaleZ");
        }

        private static void SetBoolean(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = false;
            }
        }
    }
}
