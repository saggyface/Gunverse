using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class PlayerHealthSetupBuilder
    {
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const float PLAYER_HEIGHT = 1.8f;
        private const float PLAYER_RADIUS = 0.35f;

        public static void Build()
        {
            AddPlayerComponents();

            Scene scene = EditorSceneManager.OpenScene(GROCERY_STORE_SCENE_PATH, OpenSceneMode.Single);
            InteractionSetupBuilder.AddHealthHudElements();
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerHealthSetupBuilder] Added health components and HUD elements.");
        }

        private static void AddPlayerComponents()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);

            if (root.GetComponent<PlayerHealth>() == null)
            {
                root.AddComponent<PlayerHealth>();
            }

            if (root.GetComponent<PlayerHealthHud>() == null)
            {
                root.AddComponent<PlayerHealthHud>();
            }

            Transform hurtbox = root.transform.Find("PlayerHurtbox");
            if (hurtbox == null)
            {
                GameObject hurtboxObject = new GameObject("PlayerHurtbox");
                hurtboxObject.transform.SetParent(root.transform, false);
                hurtbox = hurtboxObject.transform;
            }

            CapsuleCollider hurtboxCollider = hurtbox.GetComponent<CapsuleCollider>();
            if (hurtboxCollider == null)
            {
                hurtboxCollider = hurtbox.gameObject.AddComponent<CapsuleCollider>();
            }

            hurtboxCollider.isTrigger = true;
            hurtboxCollider.height = PLAYER_HEIGHT;
            hurtboxCollider.radius = PLAYER_RADIUS;
            hurtboxCollider.center = new Vector3(0f, PLAYER_HEIGHT * 0.5f, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, PLAYER_PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
