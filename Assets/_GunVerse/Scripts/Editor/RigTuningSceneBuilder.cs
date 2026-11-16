using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class RigTuningSceneBuilder
    {
        private const string SPIKE_SCENE_PATH = "Assets/_GunVerse/Scenes/Spike/MannequinSpike.unity";
        private const string RIG_SCENE_PATH = "Assets/_GunVerse/Scenes/Spike/RigTuning.unity";
        private const string VIEWMODEL_PREFAB_PATH =
            "Assets/_GunVerse/Prefabs/Player/MannequinViewModelArms.prefab";
        private const string BODY_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/MannequinBody.prefab";
        private const string FP_CAMERA_NAME = "SpikeCamera";
        private const string TP_CAMERA_NAME = "TuningTpCamera";
        private const string CONTROLLER_NAME = "RigTuning";
        private const float FP_EYE_HEIGHT = 1.63f;
        private const float TP_CAMERA_HEIGHT = 1.4f;
        private const float TP_CAMERA_DISTANCE = 3f;
        private const float BODY_TARGET_HEIGHT = 1f;

        [MenuItem("Tools/Gun Verse/Build Rig Tuning Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RIG_SCENE_PATH) == null)
            {
                if (!AssetDatabase.CopyAsset(SPIKE_SCENE_PATH, RIG_SCENE_PATH))
                {
                    Debug.LogError("[RigTuningSceneBuilder] Could not copy spike scene.");
                    return;
                }
            }

            Scene scene = EditorSceneManager.OpenScene(RIG_SCENE_PATH, OpenSceneMode.Single);
            Camera fpCamera = FindOrCreateCamera(FP_CAMERA_NAME);
            fpCamera.tag = "MainCamera";
            fpCamera.transform.position = new Vector3(0f, FP_EYE_HEIGHT, 0f);
            fpCamera.transform.rotation = Quaternion.identity;
            fpCamera.fieldOfView = 80f;
            EnsureCameraData(fpCamera).renderType = CameraRenderType.Base;

            Vector3 bodyPosition = new Vector3(0f, 0f, 4f);
            Camera tpCamera = FindOrCreateCamera(TP_CAMERA_NAME);
            tpCamera.tag = "Untagged";
            tpCamera.transform.position = bodyPosition + new Vector3(0f, TP_CAMERA_HEIGHT, TP_CAMERA_DISTANCE);
            tpCamera.transform.LookAt(bodyPosition + new Vector3(0f, BODY_TARGET_HEIGHT, 0f));
            tpCamera.fieldOfView = 60f;
            tpCamera.enabled = false;
            EnsureCameraData(tpCamera).renderType = CameraRenderType.Base;

            GameObject controllerObject = GameObject.Find(CONTROLLER_NAME);
            if (controllerObject == null)
            {
                controllerObject = new GameObject(CONTROLLER_NAME);
            }

            RigTuningController controller = controllerObject.GetComponent<RigTuningController>();
            if (controller == null)
            {
                controller = controllerObject.AddComponent<RigTuningController>();
            }

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("viewmodelPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(VIEWMODEL_PREFAB_PATH);
            serializedController.FindProperty("bodyPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(BODY_PREFAB_PATH);
            serializedController.FindProperty("fpCamera").objectReferenceValue = fpCamera;
            serializedController.FindProperty("tpCamera").objectReferenceValue = tpCamera;
            serializedController.FindProperty("bodyPosition").vector3Value = bodyPosition;
            serializedController.FindProperty("handLocalPosition").vector3Value = new Vector3(0f, -0.30f, 0.5f);
            serializedController.FindProperty("viewmodelLocalPosition").vector3Value = new Vector3(0f, -1.55f, 0.15f);
            serializedController.FindProperty("idleViewmodelLocalPosition").vector3Value = new Vector3(0f, -1.55f, 0.30f);
            serializedController.FindProperty("idleRightHandLocalPosition").vector3Value = new Vector3(0.26f, -0.30f, 0.48f);
            serializedController.FindProperty("idleLeftHandLocalPosition").vector3Value = new Vector3(-0.26f, -0.30f, 0.48f);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[RigTuningSceneBuilder] Built " + RIG_SCENE_PATH);
        }

        private static Camera FindOrCreateCamera(string objectName)
        {
            GameObject cameraObject = GameObject.Find(objectName);
            if (cameraObject == null)
            {
                cameraObject = new GameObject(objectName);
            }

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            return camera;
        }

        private static UniversalAdditionalCameraData EnsureCameraData(Camera camera)
        {
            UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
            {
                data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            return data;
        }
    }
}
