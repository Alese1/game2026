using System.Collections.Generic;
using SwordArt.Combat;
using SwordArt.Core;
using SwordArt.Interaction;
using SwordArt.NPC;
using SwordArt.Player;
using SwordArt.UI;
using SwordArt.World;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SwordArt.EditorTools
{
    /// <summary>How colliders are generated for a placed model.</summary>
    public enum ColliderMode
    {
        None,
        Mesh,
        Box
    }

    /// <summary>
    /// Shared helpers for the deterministic scene builders: model placement with colliders,
    /// primitives, lighting, post-processing, NavMesh baking and player rigs.
    /// </summary>
    public static class SceneBuilderUtils
    {
        public const string Generated = ProjectSetup.Root + "Generated/";

        /// <summary>Creates and opens an empty scene.</summary>
        public static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        /// <summary>Saves the active scene to <paramref name="path"/>.</summary>
        public static void Save(Scene scene, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Builder] Saved " + path);
        }

        public static Material Mat(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.MaterialsDir + name + ".mat");
            if (m == null) m = AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.FxDir + name + ".mat");
            if (m == null) Debug.LogWarning("[Builder] Missing material " + name);
            return m;
        }

        public static GameObject Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Instantiates a generated model (Generated/&lt;rel&gt;.fbx) with colliders.</summary>
        public static GameObject Place(string rel, Vector3 position, float yaw, Transform parent, ColliderMode colliders = ColliderMode.Mesh,
            SurfaceType? surface = null, bool isStatic = true, float scale = 1f)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Generated + rel + ".fbx");
            if (model == null)
            {
                Debug.LogError("[Builder] Missing model " + rel);
                return Group("MISSING_" + rel, parent);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            AddColliders(go, colliders);
            if (surface.HasValue) go.AddComponent<SurfaceTag>().Surface = surface.Value;
            if (isStatic) SetStatic(go);
            return go;
        }

        public static void AddColliders(GameObject go, ColliderMode mode)
        {
            if (mode == ColliderMode.Mesh)
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
            else if (mode == ColliderMode.Box)
            {
                var b = LocalBounds(go);
                var bc = go.AddComponent<BoxCollider>();
                bc.center = b.center;
                bc.size = b.size;
            }
        }

        /// <summary>Bounds of all renderers in the root's local space.</summary>
        public static Bounds LocalBounds(GameObject go)
        {
            var inv = go.transform.worldToLocalMatrix;
            bool init = false;
            var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = inv * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2f - Vector3.one);
                    var p = m.MultiplyPoint3x4(c);
                    if (!init) { b = new Bounds(p, Vector3.zero); init = true; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        public static void SetStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                                                                     StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
        }

        /// <summary>Unity primitive with material (and collider unless removed).</summary>
        public static GameObject Prim(PrimitiveType type, string name, Vector3 position, Vector3 scale, string material, Transform parent,
            bool collider = true, float yaw = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = scale;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        /// <summary>Box spanning [min, max] (world).</summary>
        public static GameObject Slab(string name, Vector3 min, Vector3 max, string material, Transform parent, bool collider = true)
        {
            return Prim(PrimitiveType.Cube, name, (min + max) * 0.5f, max - min, material, parent, collider);
        }

        /// <summary>Invisible blocking wall.</summary>
        public static GameObject InvisibleWall(string name, Vector3 center, Vector3 size, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>Configures sun, procedural sky, trilight ambient and fog.</summary>
        public static Light SetupLighting(Vector3 sunEuler, Color sunColor, float sunIntensity, Color skyTint, Color ground, float atmosphere,
            Color ambientSky, Color ambientEquator, Color ambientGround, Color fogColor, float fogDensity, string skyboxName)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.rotation = Quaternion.Euler(sunEuler);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = sunColor;
            sun.intensity = sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;

            string skyPath = ProjectSetup.Root + "Settings/" + skyboxName + ".mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            sky.SetFloat("_SunSize", 0.04f);
            sky.SetFloat("_AtmosphereThickness", atmosphere);
            sky.SetColor("_SkyTint", skyTint);
            sky.SetColor("_GroundColor", ground);
            sky.SetFloat("_Exposure", 1.2f);
            EditorUtility.SetDirty(sky);

            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.fog = fogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.position = new Vector3(0f, 6f, 0f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(400f, 120f, 400f);
            probe.resolution = 128;
            probe.importance = 0;
            return sun;
        }

        /// <summary>Global post-processing volume (bloom, ACES, colour adjustments, vignette).</summary>
        public static Volume CreateVolume(string profileName, float bloomIntensity, float exposure, float contrast, float saturation,
            Color colorFilter, float vignette)
        {
            string path = ProjectSetup.Root + "Settings/" + profileName + ".asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(bloomIntensity);
            bloom.scatter.Override(0.65f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(exposure);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);
            color.colorFilter.Override(colorFilter);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(vignette);
            vig.smoothness.Override(0.45f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var go = new GameObject("PostProcessVolume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return volume;
        }

        /// <summary>Main camera with URP post-processing enabled.</summary>
        public static Camera CreateMainCamera(Transform parent, Vector3 position, float fov, int cullingMask = ~0)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1200f;
            cam.cullingMask = cullingMask;
            go.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return cam;
        }

        /// <summary>Bakes a NavMesh from physics colliders (characters excluded) and stores it as an asset.</summary>
        public static NavMeshSurface BakeNavMesh(string sceneAssetDir, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject("NavMesh");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.center = center;
            surface.size = size;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            int exclude = (1 << LayerMask.NameToLayer(ProjectSetup.PlayerLayer)) | (1 << LayerMask.NameToLayer(ProjectSetup.CharacterLayer)) |
                          (1 << LayerMask.NameToLayer(ProjectSetup.WeaponLayer)) | (1 << 2);
            surface.layerMask = ~exclude;
            surface.BuildNavMesh();
            string path = sceneAssetDir + "/" + name + "_NavMesh.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            surface.RemoveData();
            surface.AddData();
            return surface;
        }

        /// <summary>Snaps a point to the ground below (raycast against colliders).</summary>
        public static Vector3 Ground(Vector3 p, float from = 60f)
        {
            Physics.SyncTransforms();
            if (Physics.Raycast(new Vector3(p.x, from, p.z), Vector3.down, out var hit, from * 2f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point;
            return p;
        }

        public static SpawnPoint Spawn(string id, Vector3 pos, float yaw, Transform parent)
        {
            var go = new GameObject("Spawn_" + id);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var sp = go.AddComponent<SpawnPoint>();
            var so = new SerializedObject(sp);
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return sp;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        /// <summary>Sets a private serialized field by name.</summary>
        public static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[Builder] Field {field} not found on {target.GetType().Name}");
                return;
            }
            switch (value)
            {
                case null: p.objectReferenceValue = null; break;
                case Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Vector3 v: p.vector3Value = v; break;
                case Color c: p.colorValue = c; break;
                case LayerMask lm: p.intValue = lm.value; break;
                default: Debug.LogError("[Builder] Unsupported field type " + value); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Sets an object-reference array field.</summary>
        public static void SetArray(Object target, string field, IList<Object> values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Instantiates a segmented character model under a new "Model" child.</summary>
        public static GameObject CharacterModel(string modelRel, Transform parent)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Generated + "Characters/" + modelRel + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            go.name = "Model";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        /// <summary>
        /// Real-world third-person player: CharacterController, controller, interactor, procedural
        /// animation, Cinemachine third-person camera with obstacle avoidance.
        /// </summary>
        public static GameObject CreateThirdPersonPlayer(Vector3 position, float yaw, float cameraDistance)
        {
            int playerLayer = LayerMask.NameToLayer(ProjectSetup.PlayerLayer);
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.03f;

            var model = CharacterModel("Character_Male", player.transform);
            var appearance = player.AddComponent<CharacterAppearance>();
            appearance.Configure(new Color(0.12f, 0.13f, 0.16f), new Color(0.2f, 0.22f, 0.28f), new Color(0.05f, 0.04f, 0.04f), new Color(0.95f, 0.8f, 0.68f));
            var anim = player.AddComponent<ProceduralCharacterAnimator>();
            anim.Configure(model.transform, false);

            var cameraTarget = new GameObject("CameraTarget").transform;
            cameraTarget.SetParent(player.transform, false);
            cameraTarget.localPosition = new Vector3(0f, 1.55f, 0f);
            var chest = new GameObject("ReachOrigin").transform;
            chest.SetParent(player.transform, false);
            chest.localPosition = new Vector3(0f, 1.1f, 0f);

            var rig = player.AddComponent<ThirdPersonCameraRig>();
            SetField(rig, "cameraTarget", cameraTarget);
            SetField(rig, "character", player.transform);
            var controller = player.AddComponent<ThirdPersonController>();
            SetField(controller, "cameraRig", rig);
            var groundMask = ~((1 << playerLayer) | (1 << LayerMask.NameToLayer(ProjectSetup.CharacterLayer)));
            SetField(controller, "groundMask", (LayerMask)groundMask);
            var interactor = player.AddComponent<Interactor>();
            SetField(interactor, "reachOrigin", chest);

            SetLayerRecursive(player, playerLayer);

            var cam = CreateMainCamera(null, position + new Vector3(0f, 2f, -4f), 60f);
            cam.gameObject.AddComponent<CinemachineBrain>();

            var vcamGo = new GameObject("CM ThirdPerson");
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Follow = cameraTarget;
            vcam.Lens.FieldOfView = 60f;
            vcam.Lens.NearClipPlane = 0.05f;
            vcam.Lens.FarClipPlane = 1200f;
            var follow = vcamGo.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset = new Vector3(0.45f, 0.15f, 0f);
            follow.VerticalArmLength = 0.25f;
            follow.CameraSide = 0.75f;
            follow.CameraDistance = cameraDistance;
            follow.Damping = new Vector3(0.1f, 0.25f, 0.3f);
            var avoid = follow.AvoidObstacles;
            avoid.Enabled = true;
            avoid.CollisionFilter = ~((1 << playerLayer) | (1 << LayerMask.NameToLayer(ProjectSetup.CharacterLayer)) | (1 << 2) |
                                      (1 << LayerMask.NameToLayer(ProjectSetup.WeaponLayer)));
            avoid.IgnoreTag = "Player";
            avoid.CameraRadius = 0.2f;
            avoid.DampingIntoCollision = 0.05f;
            avoid.DampingFromCollision = 0.4f;
            follow.AvoidObstacles = avoid;
            return player;
        }

        /// <summary>City/apartment NPC with NavMeshAgent, state machine, appearance and name tag.</summary>
        public static NPCController CreateNpc(string dataAsset, string model, Vector3 position, float yaw, Transform parent, Transform[] route)
        {
            var data = AssetDatabase.LoadAssetAtPath<NPCData>(ProjectSetup.Root + "ScriptableObjects/NPC/" + dataAsset + ".asset");
            var go = new GameObject("NPC_" + data.displayName);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var m = CharacterModel(model, go.transform);
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.8f;
            agent.speed = data.walkSpeed;
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.3f;
            col.center = new Vector3(0f, 0.9f, 0f);
            var anim = go.AddComponent<ProceduralCharacterAnimator>();
            anim.Configure(m.transform, false);
            go.AddComponent<CharacterAppearance>().Configure(data.shirtColor, data.pantsColor, data.hairColor, data.skinColor);
            var npc = go.AddComponent<NPCController>();
            npc.Configure(data, route, anim);
            go.AddComponent<WorldNameTag>().Configure(data.displayName, data.role, new Color(0.85f, 0.95f, 1f), 2.15f, 14f);
            SetLayerRecursive(go, LayerMask.NameToLayer(ProjectSetup.CharacterLayer));
            return npc;
        }

        /// <summary>Creates waypoint transforms for an NPC route.</summary>
        public static Transform[] Route(string name, Transform parent, params Vector3[] points)
        {
            var root = Group("Route_" + name, parent).transform;
            var list = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var t = new GameObject("WP" + i).transform;
                t.SetParent(root, false);
                t.position = points[i];
                list[i] = t;
            }
            return list;
        }

        /// <summary>Scene entry component.</summary>
        public static SceneContext CreateContext(WorldType world, Transform player, SpawnPoint defaultSpawn)
        {
            var go = new GameObject("SceneContext");
            var ctx = go.AddComponent<SceneContext>();
            SetField(ctx, "worldType", (int)world);
            SetField(ctx, "player", player);
            SetField(ctx, "defaultSpawn", defaultSpawn);
            return ctx;
        }

        /// <summary>Adds an interactable component with a box trigger sized to the object.</summary>
        public static T MakeInteractable<T>(GameObject go, string prompt) where T : InteractableBase
        {
            var c = go.AddComponent<T>();
            c.SetPrompt(prompt);
            if (go.GetComponent<Collider>() == null) AddColliders(go, ColliderMode.Box);
            return c;
        }

        /// <summary>Point light helper.</summary>
        public static Light PointLight(string name, Vector3 pos, Color color, float intensity, float range, Transform parent, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }
    }
}
