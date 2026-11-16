using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GunVerse.EditorTools
{
    public static class TestModeUiBuilder
    {
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";
        private const string LOBBY_SCENE_PATH = "Assets/_GunVerse/Scenes/Lobby.unity";
        private const string TEST_MODE_LABEL = "Test Mode: Off";

        [MenuItem("Tools/Gun Verse/Setup Test Mode UI")]
        public static void Build()
        {
            PatchMainMenu();
            PatchLobby();
            AssetDatabase.SaveAssets();
            Debug.Log("[TestModeUiBuilder] Added host-only Test Mode buttons to MainMenu and Lobby.");
        }

        private static void PatchMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
            if (controller == null)
            {
                Debug.LogError("[TestModeUiBuilder] MainMenuController not found.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            Button mapButton = serializedObject.FindProperty("lobbyMapButton").objectReferenceValue as Button;
            Button testModeButton = serializedObject.FindProperty("lobbyTestModeButton").objectReferenceValue as Button;
            if (testModeButton == null && mapButton != null)
            {
                testModeButton = CloneButton(mapButton, "TestModeButton");
                serializedObject.FindProperty("lobbyTestModeButton").objectReferenceValue = testModeButton;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            if (testModeButton != null)
            {
                ConfigureButton(testModeButton);
                ResizeContent(testModeButton, 1100f);
            }

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PatchLobby()
        {
            var scene = EditorSceneManager.OpenScene(LOBBY_SCENE_PATH, OpenSceneMode.Single);
            LobbyController controller = Object.FindAnyObjectByType<LobbyController>();
            if (controller == null)
            {
                Debug.LogError("[TestModeUiBuilder] LobbyController not found.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(controller);
            Button mapButton = serializedObject.FindProperty("mapButton").objectReferenceValue as Button;
            Button testModeButton = serializedObject.FindProperty("testModeButton").objectReferenceValue as Button;
            if (testModeButton == null && mapButton != null)
            {
                testModeButton = CloneButton(mapButton, "TestModeButton");
                serializedObject.FindProperty("testModeButton").objectReferenceValue = testModeButton;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            if (testModeButton != null)
            {
                ConfigureButton(testModeButton);
                ResizeContent(testModeButton, 1110f);
            }

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Button CloneButton(Button source, string name)
        {
            GameObject clone = Object.Instantiate(source.gameObject, source.transform.parent);
            clone.name = name;
            clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            Button button = clone.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }

            return button;
        }

        private static void ConfigureButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = TEST_MODE_LABEL;
            }
        }

        private static void ResizeContent(Button testModeButton, float height)
        {
            RectTransform content = testModeButton != null ? testModeButton.transform.parent as RectTransform : null;
            if (content != null)
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, height);
            }
        }
    }
}
