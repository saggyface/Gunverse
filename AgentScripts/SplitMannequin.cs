using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SplitMannequin
{
    private const string MannequinPath = "Assets/_GunVerse/Models/Player/Mannequin.fbx";
    private const string PartsFolder = "Assets/_GunVerse/Models/Player/MannequinParts";
    private const string BodyMeshPath = PartsFolder + "/MannequinBody.asset";
    private const string HeadMeshPath = PartsFolder + "/MannequinHead.asset";
    private const string ArmsMeshPath = PartsFolder + "/MannequinArms.asset";
    private const string BodyPrefabPath = "Assets/_GunVerse/Prefabs/Player/MannequinBody.prefab";
    private const string ArmsPrefabPath = "Assets/_GunVerse/Prefabs/Player/MannequinViewModelArms.prefab";
    private const string ControllerPath = "Assets/_GunVerse/Animations/Player/MannequinLocomotion.controller";

    private const int CategoryBody = 0;
    private const int CategoryHead = 1;
    private const int CategoryArm = 2;

    private sealed class SplitResult
    {
        public Mesh BodyMesh;
        public Mesh HeadMesh;
        public Mesh ArmsMesh;
        public int BodyTriangles;
        public int HeadTriangles;
        public int ArmTriangles;
    }

    public static string Build()
    {
        EnsureFolder(PartsFolder);
        EnsureFolder("Assets/_GunVerse/Prefabs/Player");

        ModelImporter importer = AssetImporter.GetAtPath(MannequinPath) as ModelImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        GameObject mannequin = AssetDatabase.LoadAssetAtPath<GameObject>(MannequinPath);
        Avatar avatar = LoadAsset<Avatar>(MannequinPath);
        if (mannequin == null || avatar == null)
        {
            return "Missing mannequin model or avatar.";
        }

        SkinnedMeshRenderer sourceRenderer = mannequin.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
        {
            return "Missing source SkinnedMeshRenderer.";
        }

        Mesh sourceMesh = sourceRenderer.sharedMesh;
        if (!sourceMesh.isReadable)
        {
            return "Source mesh is still not readable.";
        }

        SplitResult result = SplitMesh(sourceMesh, sourceRenderer.bones);
        if (result == null)
        {
            return "Mesh split failed.";
        }

        CreateBodyPrefab(mannequin, sourceRenderer, result.BodyMesh, result.HeadMesh, avatar);
        CreateArmsPrefab(mannequin, sourceRenderer, result.ArmsMesh);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        StringBuilder summary = new StringBuilder();
        summary.AppendLine("Body triangles: " + result.BodyTriangles);
        summary.AppendLine("Head triangles: " + result.HeadTriangles);
        summary.AppendLine("Arm triangles: " + result.ArmTriangles);
        summary.AppendLine("Body prefab: " + BodyPrefabPath);
        summary.AppendLine("Arms prefab: " + ArmsPrefabPath);
        return summary.ToString();
    }

    private static SplitResult SplitMesh(Mesh sourceMesh, Transform[] bones)
    {
        BoneWeight[] weights = sourceMesh.boneWeights;
        if (weights.Length != sourceMesh.vertexCount)
        {
            return null;
        }

        int[] vertexCategories = new int[sourceMesh.vertexCount];
        for (int i = 0; i < vertexCategories.Length; i++)
        {
            vertexCategories[i] = GetVertexCategory(weights[i], bones);
        }

        int[] triangles = sourceMesh.triangles;
        List<int> body = new List<int>(triangles.Length);
        List<int> head = new List<int>();
        List<int> arms = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = vertexCategories[triangles[i]];
            int b = vertexCategories[triangles[i + 1]];
            int c = vertexCategories[triangles[i + 2]];
            int headCount = (a == CategoryHead ? 1 : 0) + (b == CategoryHead ? 1 : 0) + (c == CategoryHead ? 1 : 0);
            int armCount = (a == CategoryArm ? 1 : 0) + (b == CategoryArm ? 1 : 0) + (c == CategoryArm ? 1 : 0);

            if (headCount >= 2)
            {
                head.Add(triangles[i]);
                head.Add(triangles[i + 1]);
                head.Add(triangles[i + 2]);
            }
            else
            {
                body.Add(triangles[i]);
                body.Add(triangles[i + 1]);
                body.Add(triangles[i + 2]);
            }

            if (armCount >= 2)
            {
                arms.Add(triangles[i]);
                arms.Add(triangles[i + 1]);
                arms.Add(triangles[i + 2]);
            }
        }

        Mesh bodyMesh = CreatePartMesh("MannequinBody", BodyMeshPath, sourceMesh, body);
        Mesh headMesh = CreatePartMesh("MannequinHead", HeadMeshPath, sourceMesh, head);
        Mesh armsMesh = CreatePartMesh("MannequinArms", ArmsMeshPath, sourceMesh, arms);
        if (bodyMesh == null || headMesh == null || armsMesh == null)
        {
            return null;
        }

        return new SplitResult
        {
            BodyMesh = bodyMesh,
            HeadMesh = headMesh,
            ArmsMesh = armsMesh,
            BodyTriangles = body.Count / 3,
            HeadTriangles = head.Count / 3,
            ArmTriangles = arms.Count / 3
        };
    }

    private static Mesh CreatePartMesh(string name, string path, Mesh sourceMesh, List<int> triangles)
    {
        if (triangles.Count == 0)
        {
            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        Mesh mesh = Object.Instantiate(sourceMesh);
        mesh.name = name;
        mesh.subMeshCount = 1;
        mesh.SetTriangles(triangles.ToArray(), 0, true);
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static int GetVertexCategory(BoneWeight weight, Transform[] bones)
    {
        int bestBone = -1;
        float bestWeight = -1f;

        if (weight.weight0 > bestWeight) { bestWeight = weight.weight0; bestBone = weight.boneIndex0; }
        if (weight.weight1 > bestWeight) { bestWeight = weight.weight1; bestBone = weight.boneIndex1; }
        if (weight.weight2 > bestWeight) { bestWeight = weight.weight2; bestBone = weight.boneIndex2; }
        if (weight.weight3 > bestWeight) { bestWeight = weight.weight3; bestBone = weight.boneIndex3; }

        if (bestBone < 0 || bestBone >= bones.Length || bones[bestBone] == null)
        {
            return CategoryBody;
        }

        string boneName = bones[bestBone].name;
        if (boneName.Contains("Head") || boneName.Contains("Neck"))
        {
            return CategoryHead;
        }

        if (boneName.Contains("Arm") || boneName.Contains("Hand") || boneName.Contains("Shoulder"))
        {
            return CategoryArm;
        }

        return CategoryBody;
    }

    private static void CreateBodyPrefab(GameObject mannequin, SkinnedMeshRenderer sourceRenderer, Mesh bodyMesh, Mesh headMesh, Avatar avatar)
    {
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(mannequin);
        temp.name = "MannequinBody";

        SkinnedMeshRenderer bodyRenderer = temp.GetComponentInChildren<SkinnedMeshRenderer>(true);
        bodyRenderer.name = "Body";
        bodyRenderer.sharedMesh = bodyMesh;

        GameObject headObject = new GameObject("Head");
        headObject.transform.SetParent(temp.transform, false);
        SkinnedMeshRenderer headRenderer = headObject.AddComponent<SkinnedMeshRenderer>();
        headRenderer.sharedMesh = headMesh;
        headRenderer.bones = bodyRenderer.bones;
        headRenderer.rootBone = bodyRenderer.rootBone;
        headRenderer.sharedMaterial = bodyRenderer.sharedMaterial;
        headRenderer.localBounds = bodyRenderer.localBounds;
        headRenderer.updateWhenOffscreen = bodyRenderer.updateWhenOffscreen;
        headRenderer.quality = bodyRenderer.quality;

        Transform headBone = FindBone(temp.transform, "mixamorig1:Head");
        if (headBone != null)
        {
            GameObject socket = new GameObject("HeadSocket");
            socket.transform.SetParent(headBone, false);
            socket.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            socket.transform.localRotation = Quaternion.identity;
        }

        GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        collar.name = "Collar";
        collar.transform.SetParent(temp.transform, false);
        collar.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        collar.transform.localScale = new Vector3(0.16f, 0.03f, 0.16f);
        Collider collarCollider = collar.GetComponent<Collider>();
        if (collarCollider != null)
        {
            Object.DestroyImmediate(collarCollider);
        }

        MeshRenderer collarRenderer = collar.GetComponent<MeshRenderer>();
        collarRenderer.sharedMaterial = bodyRenderer.sharedMaterial;

        Animator animator = temp.GetComponent<Animator>();
        if (animator == null)
        {
            animator = temp.AddComponent<Animator>();
        }

        animator.avatar = avatar;
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        animator.applyRootMotion = false;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(BodyPrefabPath);
        }

        PrefabUtility.SaveAsPrefabAsset(temp, BodyPrefabPath);
        Object.DestroyImmediate(temp);
    }

    private static void CreateArmsPrefab(GameObject mannequin, SkinnedMeshRenderer sourceRenderer, Mesh armsMesh)
    {
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(mannequin);
        temp.name = "MannequinViewModelArms";

        SkinnedMeshRenderer armsRenderer = temp.GetComponentInChildren<SkinnedMeshRenderer>(true);
        armsRenderer.name = "ViewModelArms";
        armsRenderer.sharedMesh = armsMesh;

        Animator animator = temp.GetComponent<Animator>();
        if (animator != null)
        {
            Object.DestroyImmediate(animator);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ArmsPrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(ArmsPrefabPath);
        }

        PrefabUtility.SaveAsPrefabAsset(temp, ArmsPrefabPath);
        Object.DestroyImmediate(temp);
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

    private static T LoadAsset<T>(string path) where T : Object
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            if (asset is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}
