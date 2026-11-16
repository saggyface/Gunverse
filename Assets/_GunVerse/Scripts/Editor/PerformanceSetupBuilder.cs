using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class PerformanceSetupBuilder
    {
        private const string PREFABS_ROOT = "Assets/_GunVerse/Prefabs";
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";

        [MenuItem("Tools/Gun Verse/Apply Performance Settings")]
        public static void Apply()
        {
            int gunPrefabCount = ApplyGunPrefabSettings();
            ApplyPlayerPrefabSettings();
            int groceryInstanceCount = ApplySceneSettings(GROCERY_STORE_SCENE_PATH);
            int bopBuyInstanceCount = ApplySceneSettings(BOPBUY_SCENE_PATH);
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[PerformanceSetupBuilder] Applied performance settings to {gunPrefabCount} gun prefabs, " +
                $"{groceryInstanceCount} GroceryStore instances, and {bopBuyInstanceCount} BopBuy instances.");
        }

        private static int ApplyGunPrefabSettings()
        {
            int count = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFABS_ROOT });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || !prefab.TryGetComponent(out GunItem _))
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                GunPerformanceSettings.ApplyToGun(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                count++;
            }

            return count;
        }

        private static void ApplyPlayerPrefabSettings()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);
            GunPerformanceSettings.ApplyToPlayer(root);
            PrefabUtility.SaveAsPrefabAsset(root, PLAYER_PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static int ApplySceneSettings(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GunItem[] gunItems = Object.FindObjectsByType<GunItem>(FindObjectsInactive.Include);

            foreach (GunItem gunItem in gunItems)
            {
                NetworkTransform networkTransform = gunItem.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    GunPerformanceSettings.ApplyGunNetworkTransform(networkTransform);
                }

                EditorUtility.SetDirty(gunItem);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return gunItems.Length;
        }
    }
}
