using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class PlayerSetupBuilder
    {
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string PLAYER_MATERIAL_PATH = "Assets/_GunVerse/Materials/Player.mat";
        private const string NETWORK_PREFABS_LIST_PATH = "Assets/_GunVerse/Settings/NetworkPrefabs.asset";
        private const string GROCERY_STORE_SCENE_PATH = "Assets/_GunVerse/Scenes/GroceryStore.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/_GunVerse/Scenes/MainMenu.unity";

        private const float PLAYER_HEIGHT = 1.8f;
        private const float PLAYER_RADIUS = 0.35f;
        private const float EYE_HEIGHT = 1.65f;

        private static readonly Vector3 bodyScale = new Vector3(0.7f, 0.9f, 0.7f);
        private static readonly Vector3 handLocalPosition = new Vector3(0f, -0.30f, 0.5f);

        private static readonly Vector3[] spawnPositions =
        {
            new Vector3(0f, 0.05f, 0f),
            new Vector3(8f, 0.05f, 8f),
            new Vector3(-8f, 0.05f, 8f),
            new Vector3(8f, 0.05f, -8f),
            new Vector3(-8f, 0.05f, -8f),
            new Vector3(0f, 0.05f, 8f),
            new Vector3(0f, 0.05f, -8f),
            new Vector3(8f, 0.05f, 0f)
        };

        public static void Build()
        {
            Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(PLAYER_MATERIAL_PATH);

            GameObject playerPrefab = LoadOrCreatePlayerPrefab(bodyMaterial);
            ConfigureNetworkManager(playerPrefab);
            BuildGroceryStore(playerPrefab);

            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);

            Debug.Log("[PlayerSetupBuilder] Built " + PLAYER_PREFAB_PATH);
        }

        private static GameObject LoadOrCreatePlayerPrefab(Material bodyMaterial)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
            if (existingPrefab != null &&
                existingPrefab.GetComponentInChildren<PlayerBodyVisual>(true) != null)
            {
                Debug.LogWarning(
                    "[PlayerSetupBuilder] Existing mannequin player prefab detected. " +
                    "Keeping it instead of recreating the placeholder capsule player.");
                return existingPrefab;
            }

            return CreatePlayerPrefab(bodyMaterial);
        }

        private static GameObject CreatePlayerPrefab(Material bodyMaterial)
        {
            GameObject root = new GameObject("NetworkPlayer");
            root.transform.position = Vector3.zero;

            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = PLAYER_HEIGHT;
            characterController.radius = PLAYER_RADIUS;
            characterController.center = new Vector3(0f, PLAYER_HEIGHT * 0.5f, 0f);

            root.AddComponent<NetworkObject>();

            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            GunPerformanceSettings.ApplyToPlayer(root);

            PlayerController playerController = root.AddComponent<PlayerController>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, PLAYER_HEIGHT * 0.5f, 0f);
            body.transform.localScale = bodyScale;
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

            MeshRenderer bodyRenderer = body.GetComponent<MeshRenderer>();
            if (bodyMaterial != null)
            {
                bodyRenderer.sharedMaterial = bodyMaterial;
            }

            GameObject cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform, false);
            cameraPivot.transform.localPosition = new Vector3(0f, EYE_HEIGHT, 0f);

            GameObject playerCameraGameObject = new GameObject("PlayerCamera");
            playerCameraGameObject.transform.SetParent(cameraPivot.transform, false);
            CinemachineCamera playerCamera = playerCameraGameObject.AddComponent<CinemachineCamera>();

            GameObject hand = new GameObject("Hand");
            hand.transform.SetParent(cameraPivot.transform, false);
            hand.transform.localPosition = handLocalPosition;
            hand.transform.localRotation = Quaternion.identity;

            GameObject hurtbox = new GameObject("PlayerHurtbox");
            hurtbox.transform.SetParent(root.transform, false);
            CapsuleCollider hurtboxCollider = hurtbox.AddComponent<CapsuleCollider>();
            hurtboxCollider.isTrigger = true;
            hurtboxCollider.height = PLAYER_HEIGHT;
            hurtboxCollider.radius = PLAYER_RADIUS;
            hurtboxCollider.center = new Vector3(0f, PLAYER_HEIGHT * 0.5f, 0f);

            PlayerInteraction interaction = root.AddComponent<PlayerInteraction>();
            root.AddComponent<PlayerHealth>();
            root.AddComponent<PlayerHealthHud>();
            root.AddComponent<PlayerScore>();

            AssignController(playerController, characterController, cameraPivot.transform, playerCamera, bodyRenderer, hand.transform);
            AssignInteraction(interaction, playerCameraGameObject.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PLAYER_PREFAB_PATH);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AssignController(PlayerController playerController, CharacterController characterController, Transform cameraPivot, CinemachineCamera playerCamera, MeshRenderer bodyRenderer, Transform handAnchor)
        {
            SerializedObject serializedObject = new SerializedObject(playerController);

            serializedObject.FindProperty("characterController").objectReferenceValue = characterController;
            serializedObject.FindProperty("cameraPivot").objectReferenceValue = cameraPivot;
            serializedObject.FindProperty("playerCamera").objectReferenceValue = playerCamera;
            serializedObject.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
            serializedObject.FindProperty("handAnchor").objectReferenceValue = handAnchor;

            serializedObject.ApplyModifiedProperties();
        }

        private static void AssignInteraction(PlayerInteraction interaction, Transform interactionCamera)
        {
            SerializedObject serializedObject = new SerializedObject(interaction);
            serializedObject.FindProperty("interactionCamera").objectReferenceValue = interactionCamera;
            serializedObject.ApplyModifiedProperties();
        }

        private static void ConfigureNetworkManager(GameObject playerPrefab)
        {
            NetworkPrefabsList prefabsList = CreateNetworkPrefabsList(playerPrefab);

            Scene mainMenuScene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);

            NetworkManager networkManager = Object.FindFirstObjectByType<NetworkManager>();
            if (networkManager != null)
            {
                networkManager.NetworkConfig.PlayerPrefab = null;
                networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists = new List<NetworkPrefabsList> { prefabsList };
                networkManager.NetworkConfig.TickRate = MultiplayerSetupBuilder.NetworkTickRate;
                EditorUtility.SetDirty(networkManager);
            }

            EditorSceneManager.SaveScene(mainMenuScene);
        }

        private static NetworkPrefabsList CreateNetworkPrefabsList(GameObject playerPrefab)
        {
            if (AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NETWORK_PREFABS_LIST_PATH) != null)
            {
                AssetDatabase.DeleteAsset(NETWORK_PREFABS_LIST_PATH);
            }

            NetworkPrefabsList prefabsList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            prefabsList.Add(new NetworkPrefab { Prefab = playerPrefab });
            AssetDatabase.CreateAsset(prefabsList, NETWORK_PREFABS_LIST_PATH);
            return prefabsList;
        }

        private static void BuildGroceryStore(GameObject playerPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(GROCERY_STORE_SCENE_PATH, OpenSceneMode.Single);

            RemoveSceneCamera();
            BuildBrainCamera();
            BuildSpawnManager(playerPrefab);
            BuildSpawnPoints();

            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveSceneCamera()
        {
            GameObject sceneCamera = GameObject.Find("Main Camera");
            if (sceneCamera != null)
            {
                Object.DestroyImmediate(sceneCamera);
            }
        }

        private static void BuildBrainCamera()
        {
            GameObject cameraGameObject = new GameObject("Main Camera");
            cameraGameObject.tag = "MainCamera";
            cameraGameObject.transform.position = new Vector3(0f, 2f, -5f);

            cameraGameObject.AddComponent<Camera>();
            cameraGameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraGameObject.AddComponent<AudioListener>();
            cameraGameObject.AddComponent<CinemachineBrain>();
        }

        private static void BuildSpawnManager(GameObject playerPrefab)
        {
            GameObject spawnManagerGameObject = new GameObject("PlayerSpawnManager");
            spawnManagerGameObject.AddComponent<NetworkObject>();

            PlayerSpawnManager spawnManager = spawnManagerGameObject.AddComponent<PlayerSpawnManager>();

            SerializedObject serializedObject = new SerializedObject(spawnManager);
            serializedObject.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            serializedObject.ApplyModifiedProperties();
        }

        private static void BuildSpawnPoints()
        {
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject spawnPoint = new GameObject("Spawn Point " + (i + 1));
                spawnPoint.transform.position = spawnPositions[i];
                spawnPoint.AddComponent<PlayerSpawnPoint>();
            }
        }
    }
}
