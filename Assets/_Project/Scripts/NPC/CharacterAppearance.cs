using UnityEngine;

namespace SwordArt.NPC
{
    /// <summary>
    /// Tints the shared character materials per instance (shirt, pants, hair, skin) with
    /// MaterialPropertyBlocks, so many NPCs can share one model and material set.
    /// </summary>
    public sealed class CharacterAppearance : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color shirt = new Color(0.25f, 0.35f, 0.55f);
        [SerializeField] private Color pants = new Color(0.14f, 0.14f, 0.18f);
        [SerializeField] private Color hair = new Color(0.08f, 0.06f, 0.05f);
        [SerializeField] private Color skin = new Color(0.95f, 0.78f, 0.66f);

        /// <summary>Sets the colours and applies them.</summary>
        public void Configure(Color shirtColor, Color pantsColor, Color hairColor, Color skinColor)
        {
            shirt = shirtColor;
            pants = pantsColor;
            hair = hairColor;
            skin = skinColor;
            Apply();
        }

        private void Start() => Apply();

        /// <summary>Applies the colours to all child renderers.</summary>
        public void Apply()
        {
            var block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var mat = r.sharedMaterial;
                if (mat == null) continue;
                string n = mat.name;
                Color? c = n.StartsWith("M_Shirt") ? shirt
                    : n.StartsWith("M_Pants") ? pants
                    : n.StartsWith("M_Hair") ? hair
                    : n.StartsWith("M_Skin") ? skin
                    : (Color?)null;
                if (c == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, c.Value);
                r.SetPropertyBlock(block);
            }
        }
    }
}
