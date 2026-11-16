using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunVerse.EditorTools
{
    public static class MannequinWaistSplitBuilder
    {
        private const string SOURCE_MESH_PATH =
            "Assets/_GunVerse/Models/Player/MannequinParts/MannequinBody.asset";
        private const string LOWER_MESH_PATH =
            "Assets/_GunVerse/Models/Player/MannequinParts/MannequinLowerBody.asset";
        private const string UPPER_MESH_PATH =
            "Assets/_GunVerse/Models/Player/MannequinParts/MannequinUpperBody.asset";
        private const string BODY_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/MannequinBody.prefab";
        private const string WAIST_MATERIAL_PATH = "Assets/_GunVerse/Materials/PlayerWaistJoint.mat";
        private const string BODY_OBJECT_NAME = "Body";
        private const string UPPER_OBJECT_NAME = "UpperBody";
        private const string WAIST_OBJECT_NAME = "WaistJoint";
        private const string HIPS_BONE = "mixamorig1:Hips";
        private const string LEFT_UP_LEG_BONE = "mixamorig1:LeftUpLeg";
        private const string RIGHT_UP_LEG_BONE = "mixamorig1:RightUpLeg";
        private const float MIN_WEIGHT = 0.0001f;

        private static readonly HashSet<string> lowerBones = new HashSet<string>
        {
            "mixamorig1:LeftUpLeg",
            "mixamorig1:RightUpLeg",
            "mixamorig1:LeftLeg",
            "mixamorig1:RightLeg",
            "mixamorig1:LeftFoot",
            "mixamorig1:RightFoot",
            "mixamorig1:LeftToeBase",
            "mixamorig1:RightToeBase",
            "mixamorig1:LeftToe_End",
            "mixamorig1:RightToe_End"
        };

        [MenuItem("Tools/Gun Verse/Build Mannequin Waist Split")]
        public static void Build()
        {
            Mesh sourceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(SOURCE_MESH_PATH);
            if (sourceMesh == null)
            {
                Debug.LogError("[MannequinWaistSplitBuilder] Source mesh not found: " + SOURCE_MESH_PATH);
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(BODY_PREFAB_PATH);
            try
            {
                SkinnedMeshRenderer bodyRenderer = FindBodyRenderer(contents);
                if (bodyRenderer == null)
                {
                    Debug.LogError("[MannequinWaistSplitBuilder] Body SkinnedMeshRenderer not found.");
                    return;
                }

                Transform[] bones = bodyRenderer.bones;
                int hipsIndex = FindBoneIndex(bones, HIPS_BONE);
                int leftUpLegIndex = FindBoneIndex(bones, LEFT_UP_LEG_BONE);
                int rightUpLegIndex = FindBoneIndex(bones, RIGHT_UP_LEG_BONE);
                if (hipsIndex < 0 || leftUpLegIndex < 0 || rightUpLegIndex < 0)
                {
                    Debug.LogError("[MannequinWaistSplitBuilder] Hips or UpLeg bone index missing.");
                    return;
                }

                BoneWeight[] sourceWeights = sourceMesh.boneWeights;
                bool[] vertexIsLower = ClassifyVertices(sourceWeights, bones);
                Mesh lowerMesh = BuildPartMesh(
                    sourceMesh,
                    sourceWeights,
                    vertexIsLower,
                    bones,
                    hipsIndex,
                    leftUpLegIndex,
                    rightUpLegIndex,
                    false);
                Mesh upperMesh = BuildPartMesh(
                    sourceMesh,
                    sourceWeights,
                    vertexIsLower,
                    bones,
                    hipsIndex,
                    leftUpLegIndex,
                    rightUpLegIndex,
                    true);

                if (!ValidatePartWeights(lowerMesh, bones, true) ||
                    !ValidatePartWeights(upperMesh, bones, false))
                {
                    Debug.LogError("[MannequinWaistSplitBuilder] Split weight validation failed.");
                    return;
                }

                CreateMeshAsset(lowerMesh, LOWER_MESH_PATH);
                CreateMeshAsset(upperMesh, UPPER_MESH_PATH);
                ConfigureBodyRenderer(bodyRenderer, lowerMesh);
                ConfigureUpperRenderer(bodyRenderer, upperMesh);
                RemoveWaistJoint(bodyRenderer.transform);

                PrefabUtility.SaveAsPrefabAsset(contents, BODY_PREFAB_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[MannequinWaistSplitBuilder] Lower verts=" + lowerMesh.vertexCount +
                    " tris=" + (lowerMesh.triangles.Length / 3) +
                    " | Upper verts=" + upperMesh.vertexCount +
                    " tris=" + (upperMesh.triangles.Length / 3));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static SkinnedMeshRenderer FindBodyRenderer(GameObject root)
        {
            SkinnedMeshRenderer fallback = null;
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.gameObject.name == BODY_OBJECT_NAME)
                {
                    return renderer;
                }

                if (fallback == null && renderer.sharedMesh != null && renderer.sharedMesh.name == "MannequinBody")
                {
                    fallback = renderer;
                }
            }

            return fallback;
        }

        private static int FindBoneIndex(Transform[] bones, string boneName)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null && bones[i].name == boneName)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool[] ClassifyVertices(BoneWeight[] weights, Transform[] bones)
        {
            bool[] result = new bool[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                result[i] = IsDominantLower(weights[i], bones);
            }

            return result;
        }

        private static bool IsDominantLower(BoneWeight weight, Transform[] bones)
        {
            int bestBone = -1;
            float bestWeight = -1f;
            int[] indices = { weight.boneIndex0, weight.boneIndex1, weight.boneIndex2, weight.boneIndex3 };
            float[] values = { weight.weight0, weight.weight1, weight.weight2, weight.weight3 };

            for (int i = 0; i < 4; i++)
            {
                if (indices[i] >= 0 && indices[i] < bones.Length && values[i] > bestWeight)
                {
                    bestWeight = values[i];
                    bestBone = indices[i];
                }
            }

            return bestBone >= 0 && lowerBones.Contains(bones[bestBone].name);
        }

        private static Mesh BuildPartMesh(
            Mesh source,
            BoneWeight[] sourceWeights,
            bool[] vertexIsLower,
            Transform[] bones,
            int hipsIndex,
            int leftUpLegIndex,
            int rightUpLegIndex,
            bool upperPart)
        {
            int[] sourceTriangles = source.triangles;
            Dictionary<int, int> remap = new Dictionary<int, int>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector4> tangents = new List<Vector4>();
            List<Vector2> uv = new List<Vector2>();
            List<Vector2> uv2 = new List<Vector2>();
            List<Color> colors = new List<Color>();
            List<BoneWeight> weights = new List<BoneWeight>();
            List<int> triangles = new List<int>();

            Vector3[] sourceVertices = source.vertices;
            Vector3[] sourceNormals = source.normals;
            Vector4[] sourceTangents = source.tangents;
            Vector2[] sourceUv = source.uv;
            Vector2[] sourceUv2 = source.uv2;
            Color[] sourceColors = source.colors;

            for (int triangleIndex = 0; triangleIndex < sourceTriangles.Length; triangleIndex += 3)
            {
                int lowerCount = 0;
                for (int corner = 0; corner < 3; corner++)
                {
                    if (vertexIsLower[sourceTriangles[triangleIndex + corner]])
                    {
                        lowerCount++;
                    }
                }

                bool triangleIsLower = lowerCount >= 2;
                if (triangleIsLower == upperPart)
                {
                    continue;
                }

                for (int corner = 0; corner < 3; corner++)
                {
                    int sourceIndex = sourceTriangles[triangleIndex + corner];
                    if (!remap.TryGetValue(sourceIndex, out int targetIndex))
                    {
                        targetIndex = vertices.Count;
                        remap.Add(sourceIndex, targetIndex);
                        vertices.Add(sourceVertices[sourceIndex]);
                        normals.Add(sourceNormals.Length > sourceIndex ? sourceNormals[sourceIndex] : Vector3.up);
                        tangents.Add(sourceTangents.Length > sourceIndex ? sourceTangents[sourceIndex] : new Vector4(1f, 0f, 0f, -1f));
                        uv.Add(sourceUv.Length > sourceIndex ? sourceUv[sourceIndex] : Vector2.zero);
                        uv2.Add(sourceUv2.Length > sourceIndex ? sourceUv2[sourceIndex] : Vector2.zero);
                        colors.Add(sourceColors.Length > sourceIndex ? sourceColors[sourceIndex] : Color.white);
                        weights.Add(SanitizeWeight(
                            sourceWeights[sourceIndex],
                            bones,
                            hipsIndex,
                            leftUpLegIndex,
                            rightUpLegIndex,
                            sourceVertices[sourceIndex].x,
                            upperPart));
                    }

                    triangles.Add(targetIndex);
                }
            }

            Mesh mesh = new Mesh
            {
                name = upperPart ? "MannequinUpperBody" : "MannequinLowerBody",
                indexFormat = source.indexFormat
            };

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uv);
            if (sourceUv2.Length > 0)
            {
                mesh.SetUVs(1, uv2);
            }

            if (sourceColors.Length > 0)
            {
                mesh.SetColors(colors);
            }

            mesh.boneWeights = weights.ToArray();
            mesh.bindposes = source.bindposes;
            mesh.subMeshCount = 1;
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static BoneWeight SanitizeWeight(
            BoneWeight source,
            Transform[] bones,
            int hipsIndex,
            int leftUpLegIndex,
            int rightUpLegIndex,
            float vertexX,
            bool upperPart)
        {
            int[] indices = { source.boneIndex0, source.boneIndex1, source.boneIndex2, source.boneIndex3 };
            float[] values = { source.weight0, source.weight1, source.weight2, source.weight3 };

            for (int i = 0; i < 4; i++)
            {
                if (indices[i] < 0 || indices[i] >= bones.Length)
                {
                    values[i] = 0f;
                    indices[i] = 0;
                    continue;
                }

                bool isLower = lowerBones.Contains(bones[indices[i]].name);
                if ((upperPart && isLower) || (!upperPart && !isLower))
                {
                    values[i] = 0f;
                }
            }

            float total = values[0] + values[1] + values[2] + values[3];
            if (total <= MIN_WEIGHT)
            {
                int fallbackIndex = upperPart
                    ? hipsIndex
                    : vertexX < 0f ? leftUpLegIndex : rightUpLegIndex;
                indices[0] = fallbackIndex;
                values[0] = 1f;
                indices[1] = 0;
                indices[2] = 0;
                indices[3] = 0;
                values[1] = 0f;
                values[2] = 0f;
                values[3] = 0f;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    values[i] /= total;
                }
            }

            return new BoneWeight
            {
                boneIndex0 = indices[0],
                boneIndex1 = indices[1],
                boneIndex2 = indices[2],
                boneIndex3 = indices[3],
                weight0 = values[0],
                weight1 = values[1],
                weight2 = values[2],
                weight3 = values[3]
            };
        }

        private static bool ValidatePartWeights(Mesh mesh, Transform[] bones, bool expectLower)
        {
            BoneWeight[] weights = mesh.boneWeights;
            for (int i = 0; i < weights.Length; i++)
            {
                BoneWeight weight = weights[i];
                int[] indices = { weight.boneIndex0, weight.boneIndex1, weight.boneIndex2, weight.boneIndex3 };
                float[] values = { weight.weight0, weight.weight1, weight.weight2, weight.weight3 };

                for (int slot = 0; slot < 4; slot++)
                {
                    if (values[slot] <= MIN_WEIGHT || indices[slot] < 0 || indices[slot] >= bones.Length)
                    {
                        continue;
                    }

                    bool isLower = lowerBones.Contains(bones[indices[slot]].name);
                    if (isLower != expectLower)
                    {
                        Debug.LogError(
                            "[MannequinWaistSplitBuilder] Vertex " + i +
                            " has unexpected bone " + bones[indices[slot]].name +
                            " on " + (expectLower ? "lower" : "upper") + " mesh.");
                        return false;
                    }
                }
            }

            return true;
        }

        private static void CreateMeshAsset(Mesh mesh, string path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
        }

        private static void ConfigureBodyRenderer(SkinnedMeshRenderer renderer, Mesh lowerMesh)
        {
            renderer.sharedMesh = lowerMesh;
            renderer.localBounds = lowerMesh.bounds;
            renderer.updateWhenOffscreen = true;
        }

        private static void ConfigureUpperRenderer(SkinnedMeshRenderer bodyRenderer, Mesh upperMesh)
        {
            Transform parent = bodyRenderer.transform;
            Transform existing = parent.Find(UPPER_OBJECT_NAME);
            GameObject upperObject;
            if (existing == null)
            {
                upperObject = new GameObject(UPPER_OBJECT_NAME);
                upperObject.transform.SetParent(parent, false);
            }
            else
            {
                upperObject = existing.gameObject;
            }

            upperObject.layer = bodyRenderer.gameObject.layer;
            SkinnedMeshRenderer upperRenderer = upperObject.GetComponent<SkinnedMeshRenderer>();
            if (upperRenderer == null)
            {
                upperRenderer = upperObject.AddComponent<SkinnedMeshRenderer>();
            }

            upperRenderer.sharedMesh = upperMesh;
            upperRenderer.bones = bodyRenderer.bones;
            upperRenderer.rootBone = bodyRenderer.rootBone;
            upperRenderer.sharedMaterials = bodyRenderer.sharedMaterials;
            upperRenderer.localBounds = upperMesh.bounds;
            upperRenderer.updateWhenOffscreen = true;
            upperRenderer.quality = bodyRenderer.quality;
        }

        private static void RemoveWaistJoint(Transform bodyTransform)
        {
            Transform existing = bodyTransform.Find(WAIST_OBJECT_NAME);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            if (AssetDatabase.LoadAssetAtPath<Material>(WAIST_MATERIAL_PATH) != null)
            {
                AssetDatabase.DeleteAsset(WAIST_MATERIAL_PATH);
            }
        }
    }
}
