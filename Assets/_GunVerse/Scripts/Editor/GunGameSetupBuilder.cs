using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class GunGameSetupBuilder
    {
        private const string LADDER_ASSET_PATH = "Assets/_GunVerse/Settings/GunGameLadder.asset";
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string NETWORK_PREFABS_LIST_PATH = "Assets/_GunVerse/Settings/NetworkPrefabs.asset";
        private const float LOBBY_CONTENT_HEIGHT = 1060f;

        private static readonly string[] CuratedLadderPaths =
        {
            "Assets/_GunVerse/Prefabs/Items/SoupCanGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/TomatoCanGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/DetergentBottleGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/KeyboardGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/PrinterGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/PhoneGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/BatteryGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/ToasterGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/CerealBoxGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/RadioGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/LaptopGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/BreadLoafGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/SpeakerGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/LaneMarkerGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/ConveyorBeltGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/CheckoutCounterGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/ShelfRunGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/TallOpenShelfGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/StarlinkDishGun.prefab",
            "Assets/_GunVerse/Prefabs/Items/StanchionGun.prefab"
        };

        public static void Build()
        {
            GunGameWeaponTuningBuilder.ApplyAll();

            GunGameLadder ladder = SyncLadder();
            if (ladder == null)
            {
                Debug.LogError("[GunGameSetupBuilder] Gun Game build aborted because the ladder is invalid.");
                return;
            }

            AddPlayerGunGameComponent();
            AssignLadderToGameMode(ladder);
            PatchMainMenuLobby();

            AssetDatabase.SaveAssets();
            Debug.Log($"[GunGameSetupBuilder] Gun Game ready with {ladder.Count} ladder weapons.");
        }

        [MenuItem("Tools/Gun Verse/Build Gun Game Progression")]
        public static void BuildProgressionMenu()
        {
            Build();
        }

        [MenuItem("Tools/Gun Verse/Sync Gun Game Ladder")]
        public static void SyncLadderMenu()
        {
            GunGameLadder ladder = SyncLadder();
            if (ladder == null)
            {
                Debug.LogError("[GunGameSetupBuilder] Gun Game ladder sync aborted because the ladder is invalid.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GunGameSetupBuilder] Synced Gun Game ladder: {ladder.Count} weapons.");
        }

        [MenuItem("Tools/Gun Verse/Validate Gun Game Ladder")]
        public static void LogLadderSummary()
        {
            GunGameLadder ladder = AssetDatabase.LoadAssetAtPath<GunGameLadder>(LADDER_ASSET_PATH);
            if (ladder == null)
            {
                Debug.LogError("[GunGameSetupBuilder] Gun Game ladder asset not found.");
                return;
            }

            System.Text.StringBuilder summary = new System.Text.StringBuilder();
            summary.Append("[GunGameSetupBuilder] Ladder has ").Append(ladder.Count).Append(" weapons:");
            bool hasMissingWeapon = false;

            for (int index = 0; index < ladder.Count; index++)
            {
                GameObject weapon = ladder.GetWeapon(index);
                summary.Append("\n  ").Append(index + 1).Append(". ").Append(ladder.GetWeaponName(index));
                if (weapon == null)
                {
                    summary.Append(" [MISSING]");
                    hasMissingWeapon = true;
                }
            }

            if (hasMissingWeapon)
            {
                Debug.LogError(summary.ToString());
                return;
            }

            Debug.Log(summary.ToString());
        }

        private static GunGameLadder SyncLadder()
        {
            GunGameLadder ladder = AssetDatabase.LoadAssetAtPath<GunGameLadder>(LADDER_ASSET_PATH);
            if (ladder == null)
            {
                ladder = ScriptableObject.CreateInstance<GunGameLadder>();
                AssetDatabase.CreateAsset(ladder, LADDER_ASSET_PATH);
            }

            List<GameObject> ordered = new List<GameObject>();
            bool hasErrors = false;

            foreach (string path in CuratedLadderPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"[GunGameSetupBuilder] Curated ladder prefab not found: {path}");
                    hasErrors = true;
                    continue;
                }

                if (prefab.GetComponent<GunItem>() == null || prefab.GetComponent<NetworkObject>() == null)
                {
                    Debug.LogError($"[GunGameSetupBuilder] Curated ladder prefab is missing GunItem or NetworkObject: {path}");
                    hasErrors = true;
                    continue;
                }

                if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0)
                {
                    Debug.LogError($"[GunGameSetupBuilder] Curated ladder prefab has no renderer and would be invisible: {path}");
                    hasErrors = true;
                    continue;
                }

                ordered.Add(prefab);
            }

            if (hasErrors || ordered.Count != CuratedLadderPaths.Length)
            {
                Debug.LogError(
                    $"[GunGameSetupBuilder] Ladder sync aborted: expected {CuratedLadderPaths.Length} valid prefabs, " +
                    $"found {ordered.Count}. Fix the errors above and rebuild the progression.");
                return null;
            }

            SerializedObject serializedObject = new SerializedObject(ladder);
            SerializedProperty weaponsProperty = serializedObject.FindProperty("weapons");
            WriteWeapons(weaponsProperty, ordered);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ladder);

            WarnAboutUnregisteredPrefabs(ordered);
            return ladder;
        }

        private static void WriteWeapons(SerializedProperty weaponsProperty, List<GameObject> ordered)
        {
            weaponsProperty.arraySize = ordered.Count;
            for (int i = 0; i < ordered.Count; i++)
            {
                weaponsProperty.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
            }
        }

        private static void WarnAboutUnregisteredPrefabs(List<GameObject> ordered)
        {
            NetworkPrefabsList networkPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NETWORK_PREFABS_LIST_PATH);
            if (networkPrefabs == null)
            {
                Debug.LogWarning("[GunGameSetupBuilder] NetworkPrefabs list not found; cannot validate Gun Game prefabs.");
                return;
            }

            foreach (GameObject prefab in ordered)
            {
                if (prefab != null && !networkPrefabs.Contains(prefab))
                {
                    Debug.LogWarning(
                        $"[GunGameSetupBuilder] {prefab.name} is not a registered network prefab. " +
                        "Register it before using it in Gun Game.");
                }
            }
        }

        private static void AddPlayerGunGameComponent()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);
            if (root.GetComponent<PlayerGunGame>() == null)
            {
                root.AddComponent<PlayerGunGame>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PLAYER_PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AssignLadderToGameMode(GunGameLadder ladder)
        {
            Scene scene = EditorSceneManager.OpenScene(GROCERY_STORE_SCENE_PATH, OpenSceneMode.Single);
            GameModeController gameMode = UnityEngine.Object.FindAnyObjectByType<GameModeController>();
            if (gameMode == null)
            {
                Debug.LogError("[GunGameSetupBuilder] GameModeController not found in GroceryStore.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(gameMode);
            serializedObject.FindProperty("gunGameLadder").objectReferenceValue = ladder;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameMode);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PatchMainMenuLobby()
        {
            Scene scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            MainMenuController controller = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            if (controller == null)
            {
                Debug.LogError("[GunGameSetupBuilder] MainMenuController not found.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            SerializedProperty gunGameProperty = serializedObject.FindProperty("lobbyGunGameButton");
            SerializedProperty tdmProperty = serializedObject.FindProperty("lobbyTdmButton");
            Button tdmButton = tdmProperty.objectReferenceValue as Button;
            Transform content = tdmButton != null ? tdmButton.transform.parent : null;
            Button gunGameButton = gunGameProperty.objectReferenceValue as Button;

            if (gunGameButton == null && content != null)
            {
                Transform existing = content.Find("GunGameButton");
                if (existing != null)
                {
                    gunGameButton = existing.GetComponent<Button>();
                }
            }

            if (gunGameButton == null && tdmButton != null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(tdmButton.gameObject, tdmButton.transform.parent);
                clone.name = "GunGameButton";
                clone.transform.SetSiblingIndex(tdmButton.transform.GetSiblingIndex() + 1);

                TextMeshProUGUI label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "Gun Game";
                }

                gunGameButton = clone.GetComponent<Button>();
            }

            if (gunGameButton == null)
            {
                Debug.LogError("[GunGameSetupBuilder] Could not create the Gun Game lobby button.");
                return;
            }

            gunGameProperty.objectReferenceValue = gunGameButton;

            RectTransform contentRect = gunGameButton.transform.parent as RectTransform;
            if (contentRect != null)
            {
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, LOBBY_CONTENT_HEIGHT);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
