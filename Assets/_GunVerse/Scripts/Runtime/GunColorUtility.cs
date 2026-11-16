using UnityEngine;

namespace GunVerse
{
    public static class GunColorUtility
    {
        private static readonly int BaseColorFactorId = Shader.PropertyToID("baseColorFactor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static Color GetMainColor(GameObject root)
        {
            MeshRenderer mainRenderer = GetMainRenderer(root);
            return GetMainColor(GetMainMaterial(mainRenderer));
        }

        public static MeshRenderer GetMainRenderer(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            MeshRenderer mainRenderer = null;
            float largestVolume = 0f;

            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                Vector3 scaledSize = Vector3.Scale(renderer.localBounds.size, renderer.transform.lossyScale);
                float volume = scaledSize.x * scaledSize.y * scaledSize.z;
                if (volume <= largestVolume)
                {
                    continue;
                }

                largestVolume = volume;
                mainRenderer = renderer;
            }

            return mainRenderer;
        }

        public static Material GetMainMaterial(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == 0)
            {
                return null;
            }

            if (materials.Length == 1 ||
                !renderer.TryGetComponent(out MeshFilter meshFilter) ||
                meshFilter.sharedMesh == null)
            {
                return materials[0];
            }

            Mesh mesh = meshFilter.sharedMesh;
            int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);
            int largestSubMesh = 0;
            int largestIndexCount = -1;

            for (int i = 0; i < subMeshCount; i++)
            {
                int indexCount = mesh.GetSubMesh(i).indexCount;
                if (indexCount <= largestIndexCount)
                {
                    continue;
                }

                largestIndexCount = indexCount;
                largestSubMesh = i;
            }

            return materials[largestSubMesh];
        }

        public static Color GetMainColor(Material material)
        {
            if (material == null)
            {
                return ArtPalette.Ui.Cream;
            }

            if (material.HasProperty(BaseColorFactorId))
            {
                return material.GetColor(BaseColorFactorId);
            }

            if (material.HasProperty(BaseColorId))
            {
                return material.GetColor(BaseColorId);
            }

            if (material.HasProperty(ColorId))
            {
                return material.GetColor(ColorId);
            }

            return ArtPalette.Ui.Cream;
        }
    }
}
