using System.Collections.Generic;
using UnityEngine;

namespace SwordArt.Audio
{
    /// <summary>Built-in sound effects (synthesised procedurally at startup).</summary>
    public enum Sfx
    {
        UIClick,
        UIHover,
        UIError,
        Coin,
        Interact,
        GunshotPistol,
        GunshotRifle,
        GunshotSniper,
        Reload,
        DryFire,
        HitMarker,
        KillConfirm,
        ImpactHard,
        ZoneWarning,
        Countdown,
        CountdownGo,
        LinkStart,
        Heal,
        Footstep
    }

    /// <summary>
    /// Pooled 2D/3D sound playback. All clips are generated in code so the prototype has
    /// working audio without external sound assets; real clips can override them later.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const int PoolSize = 24;

        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private AudioSource _ui;
        private AudioSource _ambient;
        private int _next;

        private void Awake()
        {
            _ui = gameObject.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _ui.spatialBlend = 0f;

            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.loop = true;
            _ambient.playOnAwake = false;
            _ambient.spatialBlend = 0f;
            _ambient.volume = 0.25f;

            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("SfxSource" + i);
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 2f;
                src.maxDistance = 120f;
                src.dopplerLevel = 0f;
                _pool.Add(src);
            }
            GenerateClips();
        }

        /// <summary>Plays a non-positional sound (UI, own weapon).</summary>
        public void Play2D(Sfx sfx, float volume = 1f, float pitch = 1f)
        {
            if (!_clips.TryGetValue(sfx, out var clip)) return;
            _ui.pitch = pitch;
            _ui.PlayOneShot(clip, volume);
        }

        /// <summary>Plays a positional sound at <paramref name="position"/>.</summary>
        public void PlayAt(Sfx sfx, Vector3 position, float volume = 1f, float pitch = 1f, float maxDistance = 120f)
        {
            if (!_clips.TryGetValue(sfx, out var clip)) return;
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Count;
            src.transform.position = position;
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch * Random.Range(0.96f, 1.04f);
            src.maxDistance = maxDistance;
            src.Play();
        }

        /// <summary>Starts a looping ambience: 0 = none, 1 = city, 2 = wasteland wind.</summary>
        public void SetAmbience(int kind)
        {
            AudioClip clip = kind switch
            {
                1 => _cityAmbience ??= MakeAmbience(false),
                2 => _windAmbience ??= MakeAmbience(true),
                _ => null
            };
            if (_ambient.clip == clip && _ambient.isPlaying) return;
            _ambient.Stop();
            _ambient.clip = clip;
            if (clip != null) _ambient.Play();
        }

        private AudioClip _cityAmbience;
        private AudioClip _windAmbience;

        private void GenerateClips()
        {
            _clips[Sfx.UIClick] = Tone("ui_click", 0.05f, 1400f, 0.4f, 60f);
            _clips[Sfx.UIHover] = Tone("ui_hover", 0.03f, 2200f, 0.15f, 90f);
            _clips[Sfx.UIError] = Sweep("ui_error", 0.22f, 320f, 180f, 0.35f, true);
            _clips[Sfx.Coin] = Chime("coin", new[] { 1318.5f, 1975.5f }, 0.35f);
            _clips[Sfx.Interact] = Chime("interact", new[] { 880f, 1320f }, 0.18f);
            _clips[Sfx.GunshotPistol] = Gunshot("pistol", 0.28f, 0.9f, 22f);
            _clips[Sfx.GunshotRifle] = Gunshot("rifle", 0.32f, 1f, 18f);
            _clips[Sfx.GunshotSniper] = Gunshot("sniper", 0.9f, 1f, 6f);
            _clips[Sfx.Reload] = Clicks("reload", new[] { 0f, 0.35f, 0.62f });
            _clips[Sfx.DryFire] = Clicks("dry", new[] { 0f });
            _clips[Sfx.HitMarker] = Tone("hit", 0.06f, 2600f, 0.35f, 50f);
            _clips[Sfx.KillConfirm] = Chime("kill", new[] { 1046.5f, 1568f, 2093f }, 0.4f);
            _clips[Sfx.ImpactHard] = Gunshot("impact", 0.12f, 0.35f, 40f);
            _clips[Sfx.ZoneWarning] = Sweep("zone", 0.6f, 440f, 660f, 0.3f, false);
            _clips[Sfx.Countdown] = Tone("count", 0.15f, 880f, 0.4f, 12f);
            _clips[Sfx.CountdownGo] = Tone("go", 0.45f, 1320f, 0.45f, 5f);
            _clips[Sfx.LinkStart] = Sweep("link", 1.4f, 220f, 1760f, 0.35f, false);
            _clips[Sfx.Heal] = Sweep("heal", 0.5f, 600f, 1200f, 0.25f, false);
            _clips[Sfx.Footstep] = Gunshot("step", 0.08f, 0.15f, 60f);
        }

        private static AudioClip Create(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Tone(string name, float length, float freq, float volume, float decay)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                d[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * decay) * volume;
            }
            return Create(name, d);
        }

        private static AudioClip Chime(string name, float[] freqs, float length)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float step = length / (freqs.Length + 1);
            for (int k = 0; k < freqs.Length; k++)
            {
                int start = (int)(k * step * SampleRate);
                for (int i = start; i < n; i++)
                {
                    float t = (i - start) / (float)SampleRate;
                    d[i] += Mathf.Sin(2f * Mathf.PI * freqs[k] * t) * Mathf.Exp(-t * 9f) * 0.25f;
                }
            }
            return Create(name, d);
        }

        private static AudioClip Sweep(string name, float length, float f0, float f1, float volume, bool square)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float f = Mathf.Lerp(f0, f1, k);
                phase += 2f * Mathf.PI * f / SampleRate;
                float s = Mathf.Sin(phase);
                if (square) s = Mathf.Sign(s) * 0.6f;
                float env = Mathf.Min(1f, k * 20f) * (1f - k);
                d[i] = s * env * volume;
            }
            return Create(name, d);
        }

        private static AudioClip Gunshot(string name, float length, float volume, float decay)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low = Mathf.Lerp(low, noise, 0.18f);
                float body = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 25f);
                float env = Mathf.Exp(-t * decay);
                d[i] = Mathf.Clamp((noise * 0.35f + low * 0.9f + body * 0.8f) * env * volume, -1f, 1f);
            }
            return Create(name, d);
        }

        private static AudioClip Clicks(string name, float[] times)
        {
            float length = times[times.Length - 1] + 0.08f;
            int n = (int)(length * SampleRate);
            var d = new float[n];
            var rng = new System.Random(7);
            foreach (float time in times)
            {
                int start = (int)(time * SampleRate);
                for (int i = 0; i < 0.06f * SampleRate && start + i < n; i++)
                {
                    float t = i / (float)SampleRate;
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    d[start + i] += (noise * 0.5f + Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.5f) * Mathf.Exp(-t * 80f) * 0.6f;
                }
            }
            return Create(name, d);
        }

        private static AudioClip MakeAmbience(bool wind)
        {
            const float length = 8f;
            int n = (int)(length * SampleRate);
            var d = new float[n];
            var rng = new System.Random(wind ? 11 : 23);
            float low = 0f, lower = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low = Mathf.Lerp(low, noise, wind ? 0.02f : 0.05f);
                lower = Mathf.Lerp(lower, low, 0.05f);
                float gust = wind ? 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / length * 2f) : 1f;
                float hum = wind ? 0f : Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.02f;
                d[i] = (low * 2.5f + lower * 2f) * gust * 0.5f + hum;
            }
            // Cross-fade the loop seam.
            int fade = SampleRate / 2;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                d[i] = d[i] * k + d[n - fade + i] * (1f - k);
            }
            System.Array.Resize(ref d, n - fade);
            return Create(wind ? "amb_wind" : "amb_city", d);
        }
    }
}
