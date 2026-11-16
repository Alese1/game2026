using UnityEngine;

namespace SwordArt.Weapons
{
    /// <summary>
    /// Builds a simple box-based weapon model with correct proportions when a WeaponData has
    /// no imported model yet (functional stand-in).
    /// </summary>
    public static class WeaponPlaceholder
    {
        /// <summary>Creates the placeholder under <paramref name="parent"/>.</summary>
        public static GameObject Create(WeaponData data, Transform parent)
        {
            var root = new GameObject("Placeholder_" + data.id);
            root.transform.SetParent(parent, false);
            float length = data.weaponType switch
            {
                WeaponType.Pistol => 0.22f,
                WeaponType.SniperRifle => 1.1f,
                _ => 0.75f
            };
            var dark = MakeMaterial(new Color(0.12f, 0.12f, 0.13f));
            var accent = MakeMaterial(data.accentColor * 0.6f);
            Box(root.transform, "Body", new Vector3(0f, 0f, length * 0.35f), new Vector3(0.05f, 0.09f, length * 0.6f), dark);
            Box(root.transform, "Barrel", new Vector3(0f, 0.02f, length * 0.8f), new Vector3(0.025f, 0.025f, length * 0.5f), dark);
            Box(root.transform, "Grip", new Vector3(0f, -0.08f, 0.02f), new Vector3(0.04f, 0.12f, 0.05f), accent);
            if (data.weaponType != WeaponType.Pistol)
            {
                Box(root.transform, "Magazine", new Vector3(0f, -0.1f, length * 0.32f), new Vector3(0.035f, 0.13f, 0.06f), accent);
                Box(root.transform, "Stock", new Vector3(0f, -0.01f, -0.12f), new Vector3(0.045f, 0.09f, 0.22f), dark);
            }
            if (data.hasScope) Box(root.transform, "Scope", new Vector3(0f, 0.08f, length * 0.4f), new Vector3(0.04f, 0.04f, 0.3f), dark);
            return root;
        }

        private static Material MakeMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.4f);
            m.SetFloat("_Metallic", 0.6f);
            return m;
        }

        private static void Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
