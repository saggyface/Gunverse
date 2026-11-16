using TMPro;
using UnityEngine;

namespace GunVerse
{
    [ExecuteAlways]
    public sealed class DesignBlock : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private string blockLabel = "BLOCK";
        [SerializeField] private TextMeshPro labelText;
        [SerializeField] private Color labelColor = Color.black;
        [SerializeField] private MeshRenderer blockRenderer;
        [SerializeField] private Color blockColor = new Color(0.62f, 0.65f, 0.69f, 1f);

        private MaterialPropertyBlock colorBlock;

        public string Label => blockLabel;

        private void OnEnable()
        {
            ApplyAppearance();
        }

        private void OnValidate()
        {
            ApplyAppearance();
        }

        public void SetLabel(string newLabel)
        {
            blockLabel = newLabel;
            ApplyLabel();
        }

        public void SetBlockColor(Color color)
        {
            blockColor = color;
            ApplyBlockColor();
        }

        private void ApplyAppearance()
        {
            ApplyLabel();
            ApplyBlockColor();
        }

        private void ApplyLabel()
        {
            if (labelText == null)
            {
                labelText = GetComponentInChildren<TextMeshPro>(true);
            }

            if (labelText == null)
            {
                return;
            }

            labelText.text = blockLabel;
            labelText.color = labelColor;
        }

        private void ApplyBlockColor()
        {
            if (blockRenderer == null)
            {
                blockRenderer = FindBlockRenderer();
            }

            if (blockRenderer == null)
            {
                return;
            }

            if (colorBlock == null)
            {
                colorBlock = new MaterialPropertyBlock();
            }

            blockRenderer.GetPropertyBlock(colorBlock);
            colorBlock.SetColor(BaseColorId, blockColor);
            blockRenderer.SetPropertyBlock(colorBlock);
        }

        private MeshRenderer FindBlockRenderer()
        {
            Transform block = transform.Find("Block");
            return block != null ? block.GetComponent<MeshRenderer>() : null;
        }
    }
}
