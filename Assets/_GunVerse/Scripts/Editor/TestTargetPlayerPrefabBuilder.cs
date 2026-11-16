using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class TestTargetPlayerPrefabBuilder
    {
        private const string PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/TestTargetPlayer.prefab";
        private const string PLAYER_MATERIAL_PATH = "Assets/_GunVerse/Materials/Player.mat";
        private const string NETWORK_PREFABS_PATH = "Assets/_GunVerse/Settings/NetworkPrefabs.asset";

        [MenuItem("Tools/Gun Verse/Build Test Target")]
        public static void Build()
        {
            GameObject root = new GameObject("TestTargetPlayer");
            root.AddComponent<NetworkObject>();
            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.SwitchTransformSpaceWhenParented = false;

            root.AddComponent<PlayerHealth>();
            TestTargetAvatar avatar = root.AddComponent<TestTargetAvatar>();
            root.AddComponent<TestTargetScore>();

            CapsuleCollider hurtbox = root.AddComponent<CapsuleCollider>();
            hurtbox.center = new Vector3(0f, 0.9f, 0f);
            hurtbox.height = 1.8f;
            hurtbox.radius = 0.35f;
            hurtbox.isTrigger = true;

            GameObject visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(root.transform, false);

            Material playerMaterial = AssetDatabase.LoadAssetAtPath<Material>(PLAYER_MATERIAL_PATH);
            AddPrimitive(
                visualRoot.transform,
                PrimitiveType.Capsule,
                "Body",
                new Vector3(0f, 0.9f, 0f),
                new Vector3(0.7f, 0.9f, 0.7f),
                playerMaterial);
            AddPrimitive(
                visualRoot.transform,
                PrimitiveType.Sphere,
                "Head",
                new Vector3(0f, 1.58f, 0f),
                Vector3.one * 0.34f,
                playerMaterial);

            SerializedObject avatarSerialized = new SerializedObject(avatar);
            avatarSerialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            avatarSerialized.FindProperty("hurtbox").objectReferenceValue = hurtbox;
            avatarSerialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            NetworkPrefabsList networkPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NETWORK_PREFABS_PATH);
            if (networkPrefabs != null && prefab != null && !networkPrefabs.Contains(prefab))
            {
                networkPrefabs.Add(new NetworkPrefab { Prefab = prefab });
                EditorUtility.SetDirty(networkPrefabs);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[TestTargetPlayerPrefabBuilder] Built and registered the test target player.");
        }

        private static void AddPrimitive(
            Transform parent,
            PrimitiveType primitiveType,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            Object.DestroyImmediate(primitive.GetComponent<Collider>());

            MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
