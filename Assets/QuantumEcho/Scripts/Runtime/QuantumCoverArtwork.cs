using UnityEngine;

namespace QuantumEcho
{
    /// <summary>Keeps the generated title artwork crisp and free of runtime animation.</summary>
    [ExecuteAlways]
    public sealed class QuantumCoverArtwork : MonoBehaviour
    {
        public Texture2D artwork;

        void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            if (artwork == null) return;
            var renderer = GetComponent<Renderer>();
            if (renderer == null) return;
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture("_MainTex", artwork);
            propertyBlock.SetColor("_Color", Color.white);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
