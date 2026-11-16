using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GunVerse.EditorTools
{
    public static class MapDesignTemplateBuilder
    {
        private const string DESIGN_SCENE_PATH = "Assets/_GunVerse/Scenes/Design/MapDesignTemplate.unity";
        private const string DESIGN_BLOCK_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Design/DesignBlock.prefab";
        private const string DESIGN_BLOCK_MATERIAL_PATH = "Assets/_GunVerse/Materials/DesignBlock.mat";
        private const string DESIGN_GRID_MATERIAL_PATH = "Assets/_GunVerse/Materials/DesignGrid.mat";
        private const string DESIGN_GRID_MESH_PATH = "Assets/_GunVerse/Meshes/Design/DesignGrid.asset";
        private const string TMP_FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const float GRID_HALF_EXTENT = 50f;
        private const float GRID_LINE_WIDTH = 0.012f;
        private const int GRID_LINE_COUNT = 101;

        [MenuItem("Tools/Gun Verse/Design/Build Map Design Template")]
        public static void Build()
        {
            EnsureFolder("Assets/_GunVerse/Scenes/Design");
            EnsureFolder("Assets/_GunVerse/Prefabs/Design");
            EnsureFolder("Assets/_GunVerse/Materials");
            EnsureFolder("Assets/_GunVerse/Meshes/Design");

            Material blockMaterial = GetOrCreateMaterial(
                DESIGN_BLOCK_MATERIAL_PATH,
                "Universal Render Pipeline/Lit",
                new Color(0.62f, 0.65f, 0.69f),
                0.2f);
            Material gridMaterial = GetOrCreateMaterial(
                DESIGN_GRID_MATERIAL_PATH,
                "Universal Render Pipeline/Unlit",
                new Color(0.28f, 0.31f, 0.35f),
                0.1f);

            BuildDesignBlockPrefab(blockMaterial);
            BuildTemplateScene(gridMaterial);
            ConfigureEditorSnap();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(DESIGN_SCENE_PATH, OpenSceneMode.Single);
            FocusSceneView();
            Debug.Log("[MapDesignTemplateBuilder] Built the reusable map design template.");
        }

        [MenuItem("Tools/Gun Verse/Design/Open Map Design Template")]
        public static void OpenTemplate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DESIGN_SCENE_PATH) == null)
            {
                Build();
                return;
            }

            EditorSceneManager.OpenScene(DESIGN_SCENE_PATH, OpenSceneMode.Single);
            FocusSceneView();
        }

        [MenuItem("Tools/Gun Verse/Design/Use 1m Snap")]
        public static void UseOneMeterSnap()
        {
            ConfigureEditorSnap();
            Debug.Log("[MapDesignTemplateBuilder] Set grid snapping to 1 m.");
        }

        private static GameObject BuildDesignBlockPrefab(Material blockMaterial)
        {
            GameObject root = new GameObject("DesignBlock");
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Block";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            MeshRenderer blockRenderer = cube.GetComponent<MeshRenderer>();
            blockRenderer.sharedMaterial = blockMaterial;

            TextMeshPro label = CreateWorldLabel(root.transform);
            DesignBlock designBlock = root.AddComponent<DesignBlock>();
            SerializedObject serializedObject = new SerializedObject(designBlock);
            serializedObject.FindProperty("blockLabel").stringValue = "FLOOR";
            serializedObject.FindProperty("labelText").objectReferenceValue = label;
            serializedObject.FindProperty("labelColor").colorValue = Color.black;
            serializedObject.FindProperty("blockRenderer").objectReferenceValue = blockRenderer;
            serializedObject.FindProperty("blockColor").colorValue = new Color(0.62f, 0.65f, 0.69f, 1f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, DESIGN_BLOCK_PREFAB_PATH);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(DESIGN_BLOCK_PREFAB_PATH);
        }

        private static TextMeshPro CreateWorldLabel(Transform parent)
        {
            GameObject labelObject = new GameObject("Label", typeof(TextMeshPro));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.56f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.28f;

            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TMP_FONT_PATH);
            if (font != null)
            {
                label.font = font;
            }

            label.text = "FLOOR";
            label.fontSize = 6f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.color = Color.black;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(6f, 3f);
            return label;
        }

        private static void BuildTemplateScene(Material gridMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.38f);

            GameObject workspace = new GameObject("DesignWorkspace");
            GameObject grid = CreateGridOverlay(gridMaterial);
            grid.transform.SetParent(workspace.transform, false);
            grid.transform.position = new Vector3(0f, 0.002f, 0f);

            GameObject lightObject = new GameObject("Design Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);

            GameObject cameraObject = new GameObject("Design Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.17f);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 80f, 0f), Quaternion.Euler(90f, 0f, 0f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DESIGN_SCENE_PATH);
        }

        private static GameObject CreateGridOverlay(Material material)
        {
            const int linesPerAxis = GRID_LINE_COUNT;
            List<Vector3> vertices = new List<Vector3>(linesPerAxis * 8);
            List<int> triangles = new List<int>(linesPerAxis * 12);
            float halfWidth = GRID_LINE_WIDTH * 0.5f;

            for (int index = 0; index < linesPerAxis; index++)
            {
                float coordinate = -GRID_HALF_EXTENT + index;
                AddGridQuad(vertices, triangles, new Vector3(coordinate - halfWidth, 0f, -GRID_HALF_EXTENT), new Vector3(halfWidth * 2f, 0f, GRID_HALF_EXTENT * 2f));
                AddGridQuad(vertices, triangles, new Vector3(-GRID_HALF_EXTENT, 0f, coordinate - halfWidth), new Vector3(GRID_HALF_EXTENT * 2f, 0f, halfWidth * 2f));
            }

            Mesh mesh = new Mesh { name = "DesignGrid" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, DESIGN_GRID_MESH_PATH);

            GameObject grid = new GameObject("GridOverlay", typeof(MeshFilter), typeof(MeshRenderer));
            grid.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = grid.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return grid;
        }

        private static void AddGridQuad(List<Vector3> vertices, List<int> triangles, Vector3 position, Vector3 size)
        {
            int start = vertices.Count;
            vertices.Add(position);
            vertices.Add(position + new Vector3(size.x, 0f, 0f));
            vertices.Add(position + new Vector3(size.x, 0f, size.z));
            vertices.Add(position + new Vector3(0f, 0f, size.z));
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start);
            triangles.Add(start + 3);
            triangles.Add(start + 2);
        }

        private static Material GetOrCreateMaterial(string path, string shaderName, Color color, float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogError("[MapDesignTemplateBuilder] Shader not found: " + shaderName);
                    return null;
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureEditorSnap()
        {
            EditorSnapSettings.gridSize = Vector3.one;
            EditorSnapSettings.gridSnapEnabled = true;
        }

        private static void FocusSceneView()
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.LookAt(Vector3.zero, Quaternion.Euler(90f, 0f, 0f), 55f);
                view.Repaint();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
