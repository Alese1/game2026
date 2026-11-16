using System.Collections.Generic;
using UnityEngine;

namespace SwordArt.Interaction
{
    /// <summary>
    /// Emissive tint applied to an object's renderers while it is focused, plus a short flash
    /// when it is used. Materials are instanced lazily on first focus.
    /// </summary>
    public sealed class InteractableHighlight : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Color highlightColor = new Color(0.35f, 0.75f, 1f);
        [SerializeField] private float intensity = 0.35f;

        private struct Entry
        {
            public Material Material;
            public Color Original;
            public bool HadEmission;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private bool _initialised;
        private bool _highlighted;
        private float _flash;
        private float _current;

        /// <summary>Turns the highlight on or off.</summary>
        public void SetHighlighted(bool on)
        {
            if (!_initialised) Init();
            _highlighted = on;
            enabled = true;
        }

        /// <summary>Flashes brightly once (interaction feedback).</summary>
        public void Flash()
        {
            if (!_initialised) Init();
            _flash = 1f;
            enabled = true;
        }

        private void Init()
        {
            _initialised = true;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                foreach (var m in r.materials)
                {
                    if (m == null || !m.HasProperty(EmissionColor)) continue;
                    _entries.Add(new Entry { Material = m, Original = m.GetColor(EmissionColor), HadEmission = m.IsKeywordEnabled("_EMISSION") });
                }
            }
        }

        private void Update()
        {
            float target = (_highlighted ? intensity * (0.8f + 0.2f * Mathf.Sin(Time.time * 6f)) : 0f) + _flash;
            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 3f);
            _current = Mathf.MoveTowards(_current, target, Time.deltaTime * 4f);
            foreach (var e in _entries)
            {
                if (e.Material == null) continue;
                if (_current > 0.001f) e.Material.EnableKeyword("_EMISSION");
                else if (!e.HadEmission) e.Material.DisableKeyword("_EMISSION");
                e.Material.SetColor(EmissionColor, e.Original + highlightColor * _current);
            }
            if (!_highlighted && _flash <= 0f && _current <= 0f) enabled = false;
        }
    }
}
