using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class FfaSetupBuilder
    {
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";

        public static void Build()
        {
            AddPlayerScoreComponent();
            AddPlayerTeamComponent();

            Scene scene = EditorSceneManager.OpenScene(GROCERY_STORE_SCENE_PATH, OpenSceneMode.Single);
            InteractionSetupBuilder.AddFfaHudElements();
            EnsureGameMode();
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            Debug.Log("[FfaSetupBuilder] Added FFA scoring, scoreboard, kill feed, and game mode.");
        }

        private static void AddPlayerScoreComponent()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);

            if (root.GetComponent<PlayerScore>() == null)
            {
                root.AddComponent<PlayerScore>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PLAYER_PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AddPlayerTeamComponent()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);

            if (root.GetComponent<PlayerTeam>() == null)
            {
                root.AddComponent<PlayerTeam>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PLAYER_PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void EnsureGameMode()
        {
            GameModeController gameMode = Object.FindAnyObjectByType<GameModeController>();
            if (gameMode != null)
            {
                return;
            }

            GameObject gameModeObject = new GameObject("GameMode");
            gameModeObject.AddComponent<NetworkObject>();
            gameModeObject.AddComponent<GameModeController>();
        }
    }
}
