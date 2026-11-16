using GunVerse;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class BopBuySignFixupBuilder
    {
        private const string BOPBUY_SCENE_PATH = "Assets/_GunVerse/Scenes/BopBuy.unity";
        private const string DESIGN_SCENE_PATH = "Assets/_GunVerse/Scenes/Design/MapDesignTemplate.unity";
        private const string CHECKOUT_SIGN_PREFAB_NAME = "HangingCheckoutSignGun.prefab";
        private const string CHECKOUT_DESIGN_BLOCK_NAME = "DesignBlock (290)";
        private const string HANGER_BAR_NAME = "Hanging Area Sign Hanger Bar";
        private const string CABLE_LEFT_NAME = "Hanging Area Sign Cable Left";
        private const string CABLE_RIGHT_NAME = "Hanging Area Sign Cable Right";
        private static readonly Vector3 CheckoutSignPosition = new Vector3(-30.56f, 5.56f, -21.78f);
        private static readonly Vector3 CheckoutDesignBlockPosition = new Vector3(-30.56f, 6.79f, -21.78f);
        private static readonly Quaternion CheckoutSignRotation = Quaternion.identity;
        private static readonly Quaternion SupportRotationDelta = Quaternion.Euler(0f, 90f, 0f);

        [MenuItem("Tools/Gun Verse/Fix BopBuy Checkout Sign Position")]
        public static void Apply()
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            bool hadPreviousActiveScene = previousActiveScene.IsValid() && previousActiveScene.isLoaded;

            Scene bopBuyScene = OpenAdditive(BOPBUY_SCENE_PATH, out bool openedBopBuy);
            try
            {
                GunItem checkoutSign = FindCheckoutSign(bopBuyScene);
                if (checkoutSign == null)
                {
                    Debug.LogError("[BopBuySignFixupBuilder] Checkout sign instance not found.");
                    return;
                }

                Vector3 oldPosition = checkoutSign.transform.position;
                Transform hangerBar = FindNearestSupport(bopBuyScene, HANGER_BAR_NAME, oldPosition);
                Transform leftCable = FindNearestSupport(bopBuyScene, CABLE_LEFT_NAME, oldPosition);
                Transform rightCable = FindNearestSupport(bopBuyScene, CABLE_RIGHT_NAME, oldPosition);

                MoveSupport(hangerBar, oldPosition, CheckoutSignPosition);
                MoveSupport(leftCable, oldPosition, CheckoutSignPosition);
                MoveSupport(rightCable, oldPosition, CheckoutSignPosition);

                checkoutSign.transform.SetPositionAndRotation(CheckoutSignPosition, CheckoutSignRotation);
                EditorSceneManager.MarkSceneDirty(bopBuyScene);
                EditorSceneManager.SaveScene(bopBuyScene);
                Debug.Log(
                    "[BopBuySignFixupBuilder] Moved checkout sign from " + oldPosition.ToString("F2") +
                    " to " + CheckoutSignPosition.ToString("F2") + ".");
            }
            finally
            {
                if (openedBopBuy)
                {
                    EditorSceneManager.CloseScene(bopBuyScene, true);
                }
            }

            Scene designScene = OpenAdditive(DESIGN_SCENE_PATH, out bool openedDesign);
            try
            {
                GameObject designBlock = FindGameObject(designScene, CHECKOUT_DESIGN_BLOCK_NAME);
                if (designBlock == null)
                {
                    Debug.LogWarning("[BopBuySignFixupBuilder] " + CHECKOUT_DESIGN_BLOCK_NAME + " not found in the design scene.");
                    return;
                }

                designBlock.transform.SetPositionAndRotation(CheckoutDesignBlockPosition, CheckoutSignRotation);
                EditorSceneManager.MarkSceneDirty(designScene);
                EditorSceneManager.SaveScene(designScene);
                Debug.Log("[BopBuySignFixupBuilder] Moved " + CHECKOUT_DESIGN_BLOCK_NAME + " to the checkout area.");
            }
            finally
            {
                if (openedDesign)
                {
                    EditorSceneManager.CloseScene(designScene, true);
                }

                if (hadPreviousActiveScene)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static Scene OpenAdditive(string path, out bool opened)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            return scene;
        }

        private static GunItem FindCheckoutSign(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (GunItem gunItem in root.GetComponentsInChildren<GunItem>(true))
                {
                    if (gunItem.gameObject.scene != scene)
                    {
                        continue;
                    }

                    string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gunItem.gameObject);
                    if (sourcePath != null && sourcePath.EndsWith(CHECKOUT_SIGN_PREFAB_NAME, System.StringComparison.Ordinal))
                    {
                        return gunItem;
                    }
                }
            }

            return null;
        }

        private static Transform FindNearestSupport(Scene scene, string name, Vector3 nearPosition)
        {
            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            Vector2 nearPoint = new Vector2(nearPosition.x, nearPosition.z);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.gameObject.scene != scene || child.name != name)
                    {
                        continue;
                    }

                    Vector2 point = new Vector2(child.position.x, child.position.z);
                    float distance = Vector2.Distance(point, nearPoint);
                    if (distance < nearestDistance)
                    {
                        nearest = child;
                        nearestDistance = distance;
                    }
                }
            }

            return nearest;
        }

        private static void MoveSupport(Transform support, Vector3 oldPivot, Vector3 newPivot)
        {
            if (support == null)
            {
                return;
            }

            Vector3 offset = support.position - oldPivot;
            support.position = newPivot + SupportRotationDelta * offset;
            support.rotation = SupportRotationDelta * support.rotation;
        }

        private static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.gameObject.scene == scene && child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }
    }
}
