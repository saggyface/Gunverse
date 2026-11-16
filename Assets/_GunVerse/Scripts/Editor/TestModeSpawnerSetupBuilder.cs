using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class TestModeSpawnerSetupBuilder
    {
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string TEST_TARGET_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/TestTargetPlayer.prefab";

        [MenuItem("Tools/Gun Verse/Setup Test Mode Spawner")]
        public static void Build()
        {
            GameObject testTargetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TEST_TARGET_PREFAB_PATH);
            if (testTargetPrefab == null)
            {
                Debug.LogError("[TestModeSpawnerSetupBuilder] Test target prefab not found. Build the test target first.");
                return;
            }

            AddSpawner(GROCERY_STORE_SCENE_PATH, testTargetPrefab);
            AddSpawner(BOPBUY_SCENE_PATH, testTargetPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[TestModeSpawnerSetupBuilder] Added TestModeSpawner to GroceryStore and BopBuy.");
        }

        private static void AddSpawner(string scenePath, GameObject testTargetPrefab)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameModeController gameMode = Object.FindAnyObjectByType<GameModeController>();
            if (gameMode == null)
            {
                Debug.LogError("[TestModeSpawnerSetupBuilder] GameModeController not found in " + scenePath);
                return;
            }

            TestModeSpawner spawner = gameMode.GetComponent<TestModeSpawner>();
            if (spawner == null)
            {
                spawner = gameMode.gameObject.AddComponent<TestModeSpawner>();
            }

            SerializedObject serializedObject = new SerializedObject(spawner);
            serializedObject.FindProperty("testTargetPrefab").objectReferenceValue = testTargetPrefab;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(gameMode);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
