using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public class GunItemPrefabRegistry : AssetPostprocessor
    {
        private const string PREFABS_LIST_PATH = "Assets/_GunVerse/Settings/NetworkPrefabs.asset";
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string PROJECTILE_PREFAB_HASH_PROPERTY = "projectilePrefabHash";

        private void OnPostprocessPrefab(GameObject prefab)
        {
            if (prefab.GetComponent<GunItem>() != null)
            {
                EditorApplication.delayCall += Rebuild;
            }
        }

        public static void Rebuild()
        {
            NetworkPrefabsList list = LoadList();
            if (list == null)
            {
                return;
            }

            foreach (NetworkPrefab prefab in list.PrefabList.ToList())
            {
                list.Remove(prefab);
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
            if (playerPrefab != null)
            {
                list.Add(new NetworkPrefab { Prefab = playerPrefab });
            }

            foreach (GameObject gunItemPrefab in FindAllGunItemPrefabs())
            {
                RepairProjectilePrefabHash(gunItemPrefab);
                VerifyProjectileComponents(gunItemPrefab);
                list.Add(new NetworkPrefab { Prefab = gunItemPrefab });
            }

            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
        }

        private static void RepairProjectilePrefabHash(GameObject prefab)
        {
            GunItem gunItem = prefab.GetComponent<GunItem>();
            NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
            if (gunItem == null || networkObject == null)
            {
                return;
            }

            uint prefabHash = networkObject.PrefabIdHash;
            if (prefabHash == 0)
            {
                Debug.LogError($"[{nameof(GunItemPrefabRegistry)}] {prefab.name} has no prefab hash. Reimport the prefab and rebuild the network prefab list.", prefab);
                return;
            }

            SerializedObject serializedObject = new SerializedObject(gunItem);
            SerializedProperty hashProperty = serializedObject.FindProperty(PROJECTILE_PREFAB_HASH_PROPERTY);
            if (hashProperty == null || (uint)hashProperty.longValue == prefabHash)
            {
                return;
            }

            hashProperty.longValue = prefabHash;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
        }

        private static void VerifyProjectileComponents(GameObject prefab)
        {
            if (prefab.GetComponent<GunProjectileBody>() == null)
            {
                Debug.LogError($"[{nameof(GunItemPrefabRegistry)}] {prefab.name} is missing {nameof(GunProjectileBody)}. Rebuild it with its prefab builder.", prefab);
            }

            if (prefab.GetComponent<Rigidbody>() == null)
            {
                Debug.LogError($"[{nameof(GunItemPrefabRegistry)}] {prefab.name} is missing a {nameof(Rigidbody)}.", prefab);
            }
        }

        private static NetworkPrefabsList LoadList()
        {
            NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(PREFABS_LIST_PATH);
            if (list == null)
            {
                Debug.LogError("[GunItemPrefabRegistry] NetworkPrefabsList not found at " + PREFABS_LIST_PATH + ". Run PlayerSetupBuilder first.");
            }

            return list;
        }

        private static List<GameObject> FindAllGunItemPrefabs()
        {
            List<GameObject> result = new List<GameObject>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null && prefab.GetComponent<GunItem>() != null)
                {
                    result.Add(prefab);
                }
            }

            return result;
        }
    }
}
