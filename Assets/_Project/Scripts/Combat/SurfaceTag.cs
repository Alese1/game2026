using UnityEngine;

namespace SwordArt.Combat
{
    /// <summary>Physical surface kind used to choose bullet impact effects.</summary>
    public enum SurfaceType
    {
        Concrete,
        Metal,
        Sand,
        Wood,
        Avatar
    }

    /// <summary>Marks a collider hierarchy with a <see cref="SurfaceType"/>.</summary>
    public sealed class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceType surface = SurfaceType.Concrete;

        public SurfaceType Surface
        {
            get => surface;
            set => surface = value;
        }

        /// <summary>Resolves the surface of a collider (Avatar for damageables, Concrete by default).</summary>
        public static SurfaceType Resolve(Collider collider)
        {
            if (collider == null) return SurfaceType.Concrete;
            if (collider.GetComponentInParent<IDamageable>() != null) return SurfaceType.Avatar;
            var tag = collider.GetComponentInParent<SurfaceTag>();
            return tag != null ? tag.surface : SurfaceType.Concrete;
        }
    }
}
