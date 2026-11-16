using GunVerse;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class GunVerseFixupBuilder
    {
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string HAND_ANCHOR_PATH = "CameraPivot/Hand";
        private const string SHELF_UNIT_PREFAB_PATH = "Assets/_GunVerse/Prefabs/LargeProps/ShelfUnitGun.prefab";
        private const string RIG_TUNING_SCENE_PATH = "Assets/_GunVerse/Scenes/Spike/RigTuning.unity";
        private static readonly Vector3 HandAnchorLocalPosition = new Vector3(0f, -0.30f, 0.5f);
        private static readonly Vector3 ShelfUnitHullSize = new Vector3(2.5f, 3.3f, 0.9f);

        private static readonly string[] ShelfHullPrefabPaths =
        {
            "Assets/_GunVerse/Prefabs/BopBuy/ShelfRunGun_BopBuy.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/ShelfRunGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/ShelfUnitGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/TallOpenShelfGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/ShelfRunLongGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/ShelfRunShortGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/HalfShelfRunGun.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/Mobile Shelf Bridge_3_103_164.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/Guysquad Shelf (1)_3346_259_-1905.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/Guysquad Shelf (2)_3346_259_-2008.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/Guysquad Shelf (3)_3377_259_-1408.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/Guysquad Shelf (4)_3859_259_-1894.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/Guysquad Shelf (5)_3359_259_-2418.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/Guysquad Shelf_2838_259_-1958.prefab",
            "Assets/_GunVerse/Prefabs/LargeProps/Baked/Guysquad Shelf_3367_92_-1294.prefab"
        };

        [MenuItem("Tools/Gun Verse/Apply Player Hand Anchor")]
        public static void ApplyPlayerHandAnchor()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);
            try
            {
                Transform hand = contents.transform.Find(HAND_ANCHOR_PATH);
                if (hand == null)
                {
                    Debug.LogError("[GunVerseFixupBuilder] Hand anchor not found in " + PLAYER_PREFAB_PATH);
                    return;
                }

                hand.localPosition = HandAnchorLocalPosition;
                hand.localRotation = Quaternion.identity;
                PrefabUtility.SaveAsPrefabAsset(contents, PLAYER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [MenuItem("Tools/Gun Verse/Mark Shelf Projectile Hulls")]
        public static void MarkShelfProjectileHulls()
        {
            int markedCount = 0;
            foreach (string path in ShelfHullPrefabPaths)
            {
                if (MarkShelfProjectileHull(path))
                {
                    markedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GunVerseFixupBuilder] Marked " + markedCount + " shelf projectile hulls.");
        }

        [MenuItem("Tools/Gun Verse/Update Rig Tuning Scene Hand Anchor")]
        public static void UpdateRigTuningSceneHandAnchor()
        {
            Scene scene = EditorSceneManager.OpenScene(RIG_TUNING_SCENE_PATH, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    RigTuningController controller = root.GetComponentInChildren<RigTuningController>(true);
                    if (controller == null)
                    {
                        continue;
                    }

                    SerializedObject serializedObject = new SerializedObject(controller);
                    SerializedProperty handPositionProperty = serializedObject.FindProperty("handLocalPosition");
                    if (handPositionProperty != null)
                    {
                        handPositionProperty.vector3Value = HandAnchorLocalPosition;
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(controller);
                    }

                    break;
                }

                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Tools/Gun Verse/Apply All Fixups")]
        public static void ApplyAll()
        {
            ApplyPlayerHandAnchor();
            MarkShelfProjectileHulls();
            UpdateRigTuningSceneHandAnchor();
        }

        private static bool MarkShelfProjectileHull(string path)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                GunItem gunItem = contents.GetComponent<GunItem>();
                if (gunItem == null)
                {
                    Debug.LogWarning("[GunVerseFixupBuilder] Skipped shelf without a root GunItem: " + path);
                    return false;
                }

                Collider hullCollider = contents.GetComponent<Collider>();
                if (hullCollider == null)
                {
                    if (path != SHELF_UNIT_PREFAB_PATH)
                    {
                        Debug.LogWarning("[GunVerseFixupBuilder] Skipped shelf without a root collider: " + path);
                        return false;
                    }

                    BoxCollider addedCollider = contents.AddComponent<BoxCollider>();
                    addedCollider.size = ShelfUnitHullSize;
                }

                if (contents.GetComponent<GunItemHullCollider>() == null)
                {
                    contents.AddComponent<GunItemHullCollider>();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
