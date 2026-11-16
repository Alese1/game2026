using System.Collections.Generic;
using SwordArt.Core;
using UnityEngine;

namespace SwordArt.Combat
{
    /// <summary>
    /// Pooled combat visuals built from a handful of particle systems and line renderers:
    /// muzzle flashes, tracers, surface-specific impacts and the polygon-shatter death effect.
    /// One runner is created lazily per scene.
    /// </summary>
    public sealed class CombatFX : MonoBehaviour
    {
        private static CombatFX _instance;

        private ParticleSystem _sparks;
        private ParticleSystem _dust;
        private ParticleSystem _debris;
        private ParticleSystem _shards;
        private ParticleSystem _flash;
        private readonly List<LineRenderer> _tracers = new List<LineRenderer>();
        private readonly List<float> _tracerTimes = new List<float>();
        private readonly List<Light> _lights = new List<Light>();
        private readonly List<float> _lightTimes = new List<float>();
        private int _nextTracer;
        private int _nextLight;

        private static CombatFX Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[CombatFX]");
                _instance = go.AddComponent<CombatFX>();
                return _instance;
            }
        }

        private void Awake()
        {
            var config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            Material additive = config != null ? config.particleAdditive : null;
            Material alpha = config != null ? config.particleAlpha : null;
            Material tracer = config != null ? config.tracerMaterial : null;
            Material shard = config != null && config.shardMaterial != null ? config.shardMaterial : additive;

            _sparks = CreateSystem("Sparks", additive, 0.35f, 1.2f, true);
            _dust = CreateSystem("Dust", alpha, 1.1f, -0.05f, false);
            _debris = CreateSystem("Debris", alpha, 0.8f, 1.5f, false);
            _shards = CreateSystem("Shards", shard, 1.2f, -0.1f, false);
            _flash = CreateSystem("Flash", additive, 0.06f, 0f, false);

            for (int i = 0; i < 24; i++)
            {
                var go = new GameObject("Tracer" + i);
                go.transform.SetParent(transform);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = tracer;
                lr.positionCount = 2;
                lr.widthMultiplier = 0.03f;
                lr.numCapVertices = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.enabled = false;
                _tracers.Add(lr);
                _tracerTimes.Add(0f);
            }
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("FlashLight" + i);
                go.transform.SetParent(transform);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 6f;
                light.intensity = 0f;
                light.shadows = LightShadows.None;
                light.enabled = false;
                _lights.Add(light);
                _lightTimes.Add(0f);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private ParticleSystem CreateSystem(string systemName, Material material, float lifetime, float gravity, bool stretch)
        {
            var go = new GameObject(systemName);
            go.transform.SetParent(transform);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.gravityModifier = gravity;
            main.maxParticles = 1500;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, systemName == "Dust" ? 2.2f : 0.3f));
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            r.velocityScale = stretch ? 0.04f : 0f;
            r.lengthScale = stretch ? 1.5f : 2f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (systemName == "Shards")
            {
                main.startRotation3D = false;
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.dampen = 0.08f;
                limit.limit = 0.5f;
            }
            ps.Play();
            return ps;
        }

        private static void Emit(ParticleSystem ps, Vector3 position, Vector3 normal, int count, float speedMin, float speedMax,
            float coneSpread, float sizeMin, float sizeMax, Color color, float lifetime)
        {
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = (normal + Random.insideUnitSphere * coneSpread).normalized;
                ep.position = position + normal * 0.02f;
                ep.velocity = dir * Random.Range(speedMin, speedMax);
                ep.startSize = Random.Range(sizeMin, sizeMax);
                ep.startColor = color;
                ep.startLifetime = lifetime * Random.Range(0.7f, 1.2f);
                ep.rotation = Random.Range(0f, 360f);
                ps.Emit(ep, 1);
            }
        }

        /// <summary>Bullet impact effect matching the surface.</summary>
        public static void Impact(Vector3 point, Vector3 normal, SurfaceType surface)
        {
            var fx = Instance;
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            switch (surface)
            {
                case SurfaceType.Metal:
                    Emit(fx._sparks, point, normal, 14, 4f, 10f, 0.7f, 0.03f, 0.06f, new Color(1f, 0.75f, 0.35f), 0.35f);
                    fx.FlashLight(point + normal * 0.2f, new Color(1f, 0.7f, 0.3f), 1.5f, 0.05f);
                    audio?.PlayAt(Audio.Sfx.ImpactHard, point, 0.5f, 1.8f, 40f);
                    break;
                case SurfaceType.Sand:
                    Emit(fx._dust, point, normal, 6, 0.4f, 1.6f, 0.6f, 0.3f, 0.6f, new Color(0.78f, 0.62f, 0.42f, 0.7f), 1.1f);
                    Emit(fx._debris, point, normal, 6, 2f, 4f, 0.5f, 0.03f, 0.06f, new Color(0.6f, 0.48f, 0.3f), 0.6f);
                    break;
                case SurfaceType.Wood:
                    Emit(fx._debris, point, normal, 10, 2f, 5f, 0.6f, 0.03f, 0.07f, new Color(0.55f, 0.38f, 0.22f), 0.7f);
                    audio?.PlayAt(Audio.Sfx.ImpactHard, point, 0.4f, 1.2f, 30f);
                    break;
                case SurfaceType.Avatar:
                    Emit(fx._shards, point, normal, 10, 1.5f, 4f, 0.9f, 0.05f, 0.12f, new Color(1f, 0.15f, 0.25f), 0.5f);
                    break;
                default:
                    Emit(fx._dust, point, normal, 4, 0.3f, 1.2f, 0.6f, 0.2f, 0.4f, new Color(0.6f, 0.6f, 0.6f, 0.6f), 0.9f);
                    Emit(fx._debris, point, normal, 8, 2f, 5f, 0.6f, 0.03f, 0.06f, new Color(0.5f, 0.5f, 0.5f), 0.6f);
                    Emit(fx._sparks, point, normal, 3, 3f, 6f, 0.6f, 0.02f, 0.04f, new Color(1f, 0.8f, 0.5f), 0.2f);
                    audio?.PlayAt(Audio.Sfx.ImpactHard, point, 0.35f, 1f, 30f);
                    break;
            }
        }

        /// <summary>Muzzle flash particle + light.</summary>
        public static void MuzzleFlash(Vector3 position, Vector3 forward, Color color)
        {
            var fx = Instance;
            Emit(fx._flash, position, forward, 3, 0.5f, 2f, 0.15f, 0.25f, 0.45f, color * 1.5f, 0.05f);
            Emit(fx._sparks, position, forward, 3, 6f, 12f, 0.25f, 0.02f, 0.04f, color, 0.12f);
            fx.FlashLight(position, color, 4f, 0.05f);
        }

        /// <summary>Short-lived bullet tracer.</summary>
        public static void Tracer(Vector3 from, Vector3 to, Color color)
        {
            var fx = Instance;
            int i = fx._nextTracer;
            fx._nextTracer = (fx._nextTracer + 1) % fx._tracers.Count;
            var lr = fx._tracers[i];
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0.15f);
            lr.enabled = true;
            fx._tracerTimes[i] = 0.06f;
        }

        /// <summary>SAO-style polygon shatter when an avatar dies.</summary>
        public static void Shatter(Vector3 center, Color color)
        {
            var fx = Instance;
            Emit(fx._shards, center, Vector3.up, 70, 0.5f, 3.5f, 1.6f, 0.06f, 0.2f, color, 1.4f);
            fx.FlashLight(center, color, 3f, 0.25f);
        }

        private void FlashLight(Vector3 position, Color color, float intensity, float duration)
        {
            int i = _nextLight;
            _nextLight = (_nextLight + 1) % _lights.Count;
            var light = _lights[i];
            light.transform.position = position;
            light.color = color;
            light.intensity = intensity;
            light.enabled = true;
            _lightTimes[i] = duration;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _tracers.Count; i++)
            {
                if (!_tracers[i].enabled) continue;
                _tracerTimes[i] -= dt;
                if (_tracerTimes[i] <= 0f) _tracers[i].enabled = false;
            }
            for (int i = 0; i < _lights.Count; i++)
            {
                if (!_lights[i].enabled) continue;
                _lightTimes[i] -= dt;
                if (_lightTimes[i] <= 0f) _lights[i].enabled = false;
            }
        }
    }
}
