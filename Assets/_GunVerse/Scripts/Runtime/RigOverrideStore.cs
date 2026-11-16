using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace GunVerse.EditorTools
{
    [Serializable]
    public sealed class PlayerRigOverride
    {
        public Vector3 viewmodelLocalPosition;
        public Vector3 idleViewmodelLocalPosition;
        public Vector3 viewmodelLocalEulerAngles;
        public float viewmodelScale = 1f;
        public Vector3 idleRightHandLocalPosition;
        public Vector3 idleLeftHandLocalPosition;
        public Vector3 idleRightHandLocalEulerAngles;
        public Vector3 idleLeftHandLocalEulerAngles;
        public Vector3 gripRightHandLocalEulerOffset;
        public Vector3 gripLeftHandLocalEulerOffset;
    }

    [Serializable]
    public sealed class ItemRigOverride
    {
        public string itemPrefab;
        public Vector3 pickupPositionOffset;
        public Vector3 pickupRotationEuler;
        public Vector3 pickupScale = Vector3.one;
        public Vector3 muzzleOffset;
        public float tpHoldHeight;
        public float tpHoldDistance;
        public Vector3 gripPrimaryLocalPosition;
        public Vector3 gripPrimaryLocalEuler;
        public Vector3 gripSupportLocalPosition;
        public Vector3 gripSupportLocalEuler;
    }

    [Serializable]
    public sealed class RigOverrideData
    {
        public PlayerRigOverride player;
        public List<ItemRigOverride> items = new List<ItemRigOverride>();
    }

#if UNITY_EDITOR
    public static class RigOverrideStore
    {
        private const string CONFIG_PATH = "Tools/AssetIntake/rig-overrides.json";
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";

        public static RigOverrideData Load()
        {
            string path = GetPath();
            if (!File.Exists(path))
            {
                return new RigOverrideData();
            }

            try
            {
                RigOverrideData data = JsonUtility.FromJson<RigOverrideData>(File.ReadAllText(path));
                if (data == null)
                {
                    return new RigOverrideData();
                }

                if (data.items == null)
                {
                    data.items = new List<ItemRigOverride>();
                }

                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[RigOverrideStore] Could not read overrides: " + exception.Message);
                return new RigOverrideData();
            }
        }

        public static void Save(RigOverrideData data)
        {
            string path = GetPath();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }

        public static void SavePlayer(PlayerRigOverride player, bool applyToPrefab)
        {
            RigOverrideData data = Load();
            data.player = player;
            Save(data);

            if (applyToPrefab)
            {
                ApplyPlayer(player);
            }
        }

        public static void SaveItem(ItemRigOverride item, bool applyToPrefab)
        {
            RigOverrideData data = Load();
            int index = data.items.FindIndex(entry => entry.itemPrefab == item.itemPrefab);
            if (index >= 0)
            {
                data.items[index] = item;
            }
            else
            {
                data.items.Add(item);
            }

            Save(data);

            if (applyToPrefab)
            {
                ApplyItem(item);
            }
        }

        public static void ApplyAll()
        {
            RigOverrideData data = Load();
            if (data.player != null)
            {
                ApplyPlayer(data.player);
            }

            foreach (ItemRigOverride item in data.items)
            {
                ApplyItem(item);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[RigOverrideStore] Applied rig overrides.");
        }

        public static PlayerRigOverride FindPlayer()
        {
            return Load().player;
        }

        public static ItemRigOverride FindItem(string itemPrefab)
        {
            return Load().items.Find(entry => entry.itemPrefab == itemPrefab);
        }

        private static void ApplyPlayer(PlayerRigOverride data)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError("[RigOverrideStore] Missing player prefab.");
                return;
            }

            PlayerFpArms arms = prefab.GetComponent<PlayerFpArms>();
            if (arms == null)
            {
                Debug.LogError("[RigOverrideStore] Missing PlayerFpArms.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(arms);
            SetVector3(serializedObject, "viewmodelLocalPosition", data.viewmodelLocalPosition);
            SetVector3(serializedObject, "idleViewmodelLocalPosition", data.idleViewmodelLocalPosition);
            SetVector3(serializedObject, "viewmodelLocalEulerAngles", data.viewmodelLocalEulerAngles);
            SetFloat(serializedObject, "viewmodelScale", data.viewmodelScale);
            SetVector3(serializedObject, "idleRightHandLocalPosition", data.idleRightHandLocalPosition);
            SetVector3(serializedObject, "idleLeftHandLocalPosition", data.idleLeftHandLocalPosition);
            SetVector3(serializedObject, "idleRightHandLocalEulerAngles", data.idleRightHandLocalEulerAngles);
            SetVector3(serializedObject, "idleLeftHandLocalEulerAngles", data.idleLeftHandLocalEulerAngles);
            SetVector3(serializedObject, "gripRightHandLocalEulerOffset", data.gripRightHandLocalEulerOffset);
            SetVector3(serializedObject, "gripLeftHandLocalEulerOffset", data.gripLeftHandLocalEulerOffset);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(arms);

            PlayerBodyVisual bodyVisual = prefab.GetComponent<PlayerBodyVisual>();
            if (bodyVisual != null)
            {
                SerializedObject bodyObject = new SerializedObject(bodyVisual);
                SetVector3(bodyObject, "gripRightHandLocalEulerOffset", data.gripRightHandLocalEulerOffset);
                SetVector3(bodyObject, "gripLeftHandLocalEulerOffset", data.gripLeftHandLocalEulerOffset);
                bodyObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bodyVisual);
            }
        }

        private static void SetVector3(SerializedObject serializedObject, string propertyName, Vector3 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void ApplyItem(ItemRigOverride data)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.itemPrefab);
            if (prefab == null)
            {
                Debug.LogError("[RigOverrideStore] Missing item prefab: " + data.itemPrefab);
                return;
            }

            GunItem gun = prefab.GetComponent<GunItem>();
            if (gun == null)
            {
                Debug.LogError("[RigOverrideStore] Missing GunItem on " + data.itemPrefab);
                return;
            }

            SerializedObject serializedObject = new SerializedObject(gun);
            SetVector3(serializedObject, "pickupPositionOffset", data.pickupPositionOffset);
            SetVector3(serializedObject, "pickupRotationEuler", data.pickupRotationEuler);
            SetVector3(serializedObject, "pickupScale", data.pickupScale);
            SetVector3(serializedObject, "muzzleOffset", data.muzzleOffset);
            SetFloat(serializedObject, "tpHoldHeight", data.tpHoldHeight);
            SetFloat(serializedObject, "tpHoldDistance", data.tpHoldDistance);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            ApplyGrip(gun.GripPrimary, data.gripPrimaryLocalPosition, data.gripPrimaryLocalEuler);
            ApplyGrip(gun.GripSupport, data.gripSupportLocalPosition, data.gripSupportLocalEuler);
            EditorUtility.SetDirty(gun);
        }

        private static void ApplyGrip(Transform grip, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (grip == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(grip);
            serializedObject.FindProperty("m_LocalPosition").vector3Value = localPosition;
            serializedObject.FindProperty("m_LocalRotation").quaternionValue = Quaternion.Euler(localEulerAngles);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(grip);
        }

        private static string GetPath()
        {
            return Path.Combine(Application.dataPath, "..", CONFIG_PATH);
        }
    }

    public static class RigOverrideApplier
    {
        [MenuItem("Tools/Gun Verse/Apply Rig Overrides")]
        public static void Apply()
        {
            RigOverrideStore.ApplyAll();
        }
    }
#endif
}
