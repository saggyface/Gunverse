using System.Text;
using GunVerse;
using UnityEditor;
using UnityEngine;

public static class PatchNetworkPlayerBody
{
    private const string PlayerPrefabPath = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
    private const string BodyPrefabPath = "Assets/_GunVerse/Prefabs/Player/MannequinBody.prefab";
    private const string ViewmodelPrefabPath = "Assets/_GunVerse/Prefabs/Player/MannequinViewModelArms.prefab";
    private const string ViewModelLayerName = "ViewModel";

    public static string Build()
    {
        GameObject bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefabPath);
        if (bodyPrefab == null)
        {
            return "Missing MannequinBody prefab.";
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform oldBody = contents.transform.Find("Body");
            if (oldBody != null)
            {
                Object.DestroyImmediate(oldBody.gameObject);
            }

            Transform bodyVisual = contents.transform.Find("BodyVisual");
            if (bodyVisual == null)
            {
                GameObject visualObject = new GameObject("BodyVisual");
                visualObject.transform.SetParent(contents.transform, false);
                bodyVisual = visualObject.transform;
            }

            for (int i = bodyVisual.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(bodyVisual.GetChild(i).gameObject);
            }

            GameObject bodyInstance = (GameObject)PrefabUtility.InstantiatePrefab(bodyPrefab, bodyVisual);
            bodyInstance.name = "MannequinBody";
            bodyInstance.transform.localPosition = Vector3.zero;
            bodyInstance.transform.localRotation = Quaternion.identity;
            bodyInstance.transform.localScale = Vector3.one;

            SkinnedMeshRenderer bodyRenderer = null;
            SkinnedMeshRenderer headRenderer = null;
            foreach (SkinnedMeshRenderer renderer in bodyInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == "Body")
                {
                    bodyRenderer = renderer;
                }
                else if (renderer.name == "Head")
                {
                    headRenderer = renderer;
                }
            }

            if (bodyRenderer == null || headRenderer == null)
            {
                return "MannequinBody is missing the Body or Head renderer.";
            }

            Animator animator = bodyInstance.GetComponent<Animator>();
            Transform torso = FindBone(bodyInstance.transform, "mixamorig1:Spine");
            Transform head = FindBone(bodyInstance.transform, "mixamorig1:Head");
            Transform headSocket = FindBone(bodyInstance.transform, "HeadSocket");
            Transform rightUpperArm = FindBone(bodyInstance.transform, "mixamorig1:RightArm");
            Transform rightForeArm = FindBone(bodyInstance.transform, "mixamorig1:RightForeArm");
            Transform rightHand = FindBone(bodyInstance.transform, "mixamorig1:RightHand");
            Transform rightMiddle = FindBone(bodyInstance.transform, "mixamorig1:RightHandMiddle1");
            Transform leftUpperArm = FindBone(bodyInstance.transform, "mixamorig1:LeftArm");
            Transform leftForeArm = FindBone(bodyInstance.transform, "mixamorig1:LeftForeArm");
            Transform leftHand = FindBone(bodyInstance.transform, "mixamorig1:LeftHand");
            Transform leftMiddle = FindBone(bodyInstance.transform, "mixamorig1:LeftHandMiddle1");
            Transform cameraPivot = contents.transform.Find("CameraPivot");

            PlayerController controller = contents.GetComponent<PlayerController>();
            SerializedObject controllerObject = new SerializedObject(controller);
            controllerObject.FindProperty("bodyVisual").objectReferenceValue = bodyVisual.gameObject;
            SerializedProperty renderersProperty = controllerObject.FindProperty("bodyRenderers");
            renderersProperty.arraySize = 2;
            renderersProperty.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
            renderersProperty.GetArrayElementAtIndex(1).objectReferenceValue = headRenderer;
            controllerObject.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            PlayerAnimationState animationState = contents.GetComponent<PlayerAnimationState>();
            if (animationState == null)
            {
                animationState = contents.AddComponent<PlayerAnimationState>();
            }

            PlayerBodyVisual bodyVisualComponent = contents.GetComponent<PlayerBodyVisual>();
            if (bodyVisualComponent == null)
            {
                bodyVisualComponent = contents.AddComponent<PlayerBodyVisual>();
            }

            SerializedObject bodyVisualObject = new SerializedObject(bodyVisualComponent);
            bodyVisualObject.FindProperty("bodyVisual").objectReferenceValue = bodyVisual.gameObject;
            bodyVisualObject.FindProperty("animator").objectReferenceValue = animator;
            bodyVisualObject.FindProperty("cameraPivot").objectReferenceValue = cameraPivot;
            bodyVisualObject.FindProperty("animationState").objectReferenceValue = animationState;
            bodyVisualObject.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
            bodyVisualObject.FindProperty("headRenderer").objectReferenceValue = headRenderer;
            bodyVisualObject.FindProperty("headSocket").objectReferenceValue = headSocket;
            bodyVisualObject.FindProperty("rightUpperArm").objectReferenceValue = rightUpperArm;
            bodyVisualObject.FindProperty("rightForeArm").objectReferenceValue = rightForeArm;
            bodyVisualObject.FindProperty("rightHand").objectReferenceValue = rightHand;
            bodyVisualObject.FindProperty("rightMiddle").objectReferenceValue = rightMiddle;
            bodyVisualObject.FindProperty("leftUpperArm").objectReferenceValue = leftUpperArm;
            bodyVisualObject.FindProperty("leftForeArm").objectReferenceValue = leftForeArm;
            bodyVisualObject.FindProperty("leftHand").objectReferenceValue = leftHand;
            bodyVisualObject.FindProperty("leftMiddle").objectReferenceValue = leftMiddle;
            bodyVisualObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject viewmodelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ViewmodelPrefabPath);
            PlayerFpArms fpArms = contents.GetComponent<PlayerFpArms>();
            if (fpArms == null)
            {
                fpArms = contents.AddComponent<PlayerFpArms>();
            }

            SerializedObject fpArmsObject = new SerializedObject(fpArms);
            fpArmsObject.FindProperty("viewmodelPrefab").objectReferenceValue = viewmodelPrefab;
            fpArmsObject.ApplyModifiedPropertiesWithoutUndo();
            EnsureLayer(ViewModelLayerName);

            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);

            StringBuilder summary = new StringBuilder();
            summary.AppendLine("Body renderers: " + bodyRenderer.name + ", " + headRenderer.name);
            summary.AppendLine("Head socket: " + (headSocket != null ? headSocket.name : "missing"));
            summary.AppendLine("Torso: " + (torso != null ? torso.name : "missing"));
            summary.AppendLine("Arms: " + (rightUpperArm != null && leftUpperArm != null ? "linked" : "missing"));
            return summary.ToString();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void EnsureLayer(string name)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            return;
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null)
        {
            return;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == name)
            {
                return;
            }
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }

    private static Transform FindBone(Transform root, string name)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name == name)
            {
                return transform;
            }
        }

        return null;
    }
}
