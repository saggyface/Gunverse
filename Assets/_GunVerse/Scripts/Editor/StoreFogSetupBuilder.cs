using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class StoreFogSetupBuilder
    {
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";

        [MenuItem("Tools/Gun Verse/Apply Store Fog To Gameplay Scenes")]
        public static void ApplyToGameplayScenes()
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            ApplyToScene(BOPBUY_SCENE_PATH);
            ApplyToScene(GROCERY_STORE_SCENE_PATH);

            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static void ApplyToScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RemoveLegacyFog();
            EnsureController();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[StoreFogSetupBuilder] Applied global store fog setup to " + scenePath);
        }

        private static void RemoveLegacyFog()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                RemoveLegacyFogRecursive(root);
            }
        }

        private static void RemoveLegacyFogRecursive(GameObject gameObject)
        {
            if (gameObject.name == "Appliance Fog Zone")
            {
                Object.DestroyImmediate(gameObject);
                return;
            }

            foreach (MonoBehaviour behaviour in gameObject.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().Name == "LocalFogZone")
                {
                    Object.DestroyImmediate(behaviour);
                }
            }

            for (int index = gameObject.transform.childCount - 1; index >= 0; index--)
            {
                RemoveLegacyFogRecursive(gameObject.transform.GetChild(index).gameObject);
            }
        }

        private static void EnsureController()
        {
            if (Object.FindAnyObjectByType<StoreFogController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject fogObject = new GameObject("StoreFogController");
            fogObject.AddComponent<NetworkObject>();
            fogObject.AddComponent<StoreFogController>();
        }
    }
}
