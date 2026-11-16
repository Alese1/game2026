using System.Collections.Generic;
using SwordArt.AI;
using SwordArt.Combat;
using SwordArt.Core;
using SwordArt.Interaction;
using SwordArt.Player;
using SwordArt.Tournament;
using SwordArt.UI;
using SwordArt.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using static SwordArt.EditorTools.SceneBuilderUtils;

namespace SwordArt.EditorTools
{
    /// <summary>
    /// Builds the Gun Gale Online scene: desert arena with points of interest and cover, tournament
    /// lobby (registration / logout terminals, shooting range), first-person player rig, bot prefab,
    /// safe-zone dome, tournament systems, HUD, lighting and NavMesh. Run manually: SwordArt/Build/GGO Arena.
    /// </summary>
    public static class GGOBuilder
    {
        public const string ScenePath = ProjectSetup.Root + "Scenes/GGO/GGO_Arena.unity";
        public const string BotPrefabPath = ProjectSetup.Root + "Prefabs/Characters/Bot.prefab";
        private static readonly Vector3 LobbyOrigin = new Vector3(0f, 0f, -300f);

        private static readonly List<GameObject> CoverObjects = new List<GameObject>();

        [MenuItem("SwordArt/Build/GGO Arena", priority = 12)]
        public static void Build()
        {
            CoverObjects.Clear();
            var botPrefab = BuildBotPrefab();

            var scene = NewScene();
            var env = Group("Arena").transform;
            var lobbyRoot = Group("Lobby").transform;
            var systems = Group("Systems").transform;

            SetupLighting(new Vector3(22f, 135f, 0f), new Color(1f, 0.8f, 0.6f), 1.7f, new Color(0.95f, 0.55f, 0.35f), new Color(0.45f, 0.35f, 0.25f), 2.3f,
                new Color(0.75f, 0.6f, 0.5f), new Color(0.6f, 0.48f, 0.38f), new Color(0.35f, 0.28f, 0.2f), new Color(0.8f, 0.62f, 0.44f), 0.0045f, "Sky_GGO");
            CreateVolume("Volume_GGO", 0.9f, 0.1f, 18f, -6f, new Color(1f, 0.93f, 0.84f), 0.32f);

            var terrain = Place("Environment/GGO_Terrain", Vector3.zero, 0f, env, ColliderMode.Mesh, SurfaceType.Sand);
            terrain.name = "Terrain";
            Physics.SyncTransforms();

            BuildPointsOfInterest(env);
            BuildScatter(env);
            BuildLobby(lobbyRoot, out var lobbySpawn, out var registerTerminal, out var logoutTerminal);

            var nav = BakeNavMesh(ProjectSetup.Root + "Scenes/GGO", "GGO_Arena", new Vector3(0, 15, 0), new Vector3(236, 70, 236));
            nav.transform.SetParent(env);
            CreateCoverPoints(env);

            var spawns = Group("SpawnPoints", systems).transform;
            var botSpawns = new List<Transform>();
            var playerSpawns = new List<Transform>();
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f + 0.2f;
                float r = 38f + (i % 3) * 22f;
                botSpawns.Add(NavSpawn("BotSpawn" + i, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), spawns));
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f + 0.6f;
                float r = 62f + (i % 2) * 14f;
                playerSpawns.Add(NavSpawn("PlayerSpawn" + i, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), spawns));
            }

            var zone = BuildZone(systems);
            var player = BuildPlayer(lobbySpawn.transform);

            var tmGo = new GameObject("TournamentManager");
            tmGo.transform.SetParent(systems, false);
            var tm = tmGo.AddComponent<TournamentManager>();
            var profiles = new List<BotProfile>();
            foreach (var n in new[] { "Bot_Rifleman", "Bot_Rusher", "Bot_Sniper", "Bot_Gunslinger" })
                profiles.Add(AssetDatabase.LoadAssetAtPath<BotProfile>(ProjectSetup.Root + "ScriptableObjects/Bots/" + n + ".asset"));
            tm.Configure(player, zone, botPrefab, profiles, botSpawns.ToArray(), playerSpawns.ToArray(), 7);
            registerTerminal.Configure(TournamentTerminal.Mode.Register, tm);
            logoutTerminal.Configure(TournamentTerminal.Mode.Logout, tm);

            var hud = tmGo.AddComponent<GGOHUD>();
            hud.Configure(tm, Vector3.zero, 118f);

            CreateContext(WorldType.Virtual, player.transform, lobbySpawn);
            Save(scene, ScenePath);
        }

        private static Transform NavSpawn(string name, Vector3 p, Transform parent)
        {
            var g = Ground(p, 80f);
            if (NavMesh.SamplePosition(g, out var hit, 12f, NavMesh.AllAreas)) g = hit.position;
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = g + Vector3.up * 0.05f;
            t.rotation = Quaternion.LookRotation(new Vector3(-p.x, 0, -p.z).normalized);
            return t;
        }

        private static GameObject Cover(string model, Vector3 xz, float yaw, Transform parent, SurfaceType surface, float yOffset = 0f)
        {
            var go = Place(model, Ground(xz, 80f) + Vector3.up * yOffset, yaw, parent, ColliderMode.Mesh, surface);
            CoverObjects.Add(go);
            Physics.SyncTransforms();
            return go;
        }

        private static void BuildPointsOfInterest(Transform env)
        {
            var town = Group("POI_Town", env).transform;
            Cover("Environment/Ruin_A", new Vector3(-9, 0, 7), 0f, town, SurfaceType.Concrete);
            Cover("Environment/Ruin_A", new Vector3(11, 0, -9), 90f, town, SurfaceType.Concrete);
            Cover("Environment/Ruin_B", new Vector3(13, 0, 13), 200f, town, SurfaceType.Concrete);
            Cover("Environment/Ruin_C", new Vector3(-15, 0, -13), 30f, town, SurfaceType.Concrete);
            Cover("Environment/Container_Red", new Vector3(2, 0, -1), 15f, town, SurfaceType.Metal);
            Cover("Environment/Container_Blue", new Vector3(-3, 0, 20), 90f, town, SurfaceType.Metal);
            Cover("Environment/Container_Blue", new Vector3(22, 0, 2), 0f, town, SurfaceType.Metal);
            Cover("Environment/Barrier", new Vector3(5, 0, 9), 20f, town, SurfaceType.Concrete);
            Cover("Environment/Barrier", new Vector3(-4, 0, -9), -30f, town, SurfaceType.Concrete);
            Cover("Environment/CrateStack", new Vector3(-20, 0, 2), 45f, town, SurfaceType.Wood);
            Cover("Environment/Crate", new Vector3(7, 0, -18), 10f, town, SurfaceType.Wood);
            Cover("Environment/Girders", new Vector3(-24, 0, 26), 30f, town, SurfaceType.Metal);

            var outpost = Group("POI_Outpost", env).transform;
            Cover("Environment/Platform_Tower", new Vector3(0, 0, 62), 180f, outpost, SurfaceType.Metal);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + 30f;
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Cover("Environment/SandbagWall", new Vector3(0, 0, 62) + d * 10f, a + 180f, outpost, SurfaceType.Sand);
            }
            Cover("Environment/CrateStack", new Vector3(8, 0, 56), 10f, outpost, SurfaceType.Wood);
            Cover("Environment/Container_Red", new Vector3(12, 0, 72), 70f, outpost, SurfaceType.Metal);
            Cover("Environment/Barrier", new Vector3(-10, 0, 52), 0f, outpost, SurfaceType.Concrete);

            var convoy = Group("POI_Convoy", env).transform;
            Cover("Environment/WreckedTank", new Vector3(60, 0, -10), 30f, convoy, SurfaceType.Metal);
            Cover("Environment/WreckedTank", new Vector3(73, 0, 2), 200f, convoy, SurfaceType.Metal);
            Cover("Environment/Container_Red", new Vector3(66, 0, -22), 80f, convoy, SurfaceType.Metal);
            Cover("Environment/Container_Blue", new Vector3(54, 0, 6), 120f, convoy, SurfaceType.Metal);
            Cover("Environment/Barrier", new Vector3(62, 0, 10), 90f, convoy, SurfaceType.Concrete);
            Cover("Environment/CrateStack", new Vector3(76, 0, -14), 0f, convoy, SurfaceType.Wood);

            var hill = Group("POI_AntennaHill", env).transform;
            Place("Environment/AntennaMast", Ground(new Vector3(-55, 0, -50), 80f), 0f, hill, ColliderMode.Box, SurfaceType.Metal);
            Cover("Environment/Rock_B", new Vector3(-47, 0, -44), 40f, hill, SurfaceType.Concrete);
            Cover("Environment/Rock_A", new Vector3(-62, 0, -40), 100f, hill, SurfaceType.Concrete);
            Cover("Environment/Ruin_B", new Vector3(-40, 0, -30), 150f, hill, SurfaceType.Concrete);
            Cover("Environment/SandbagWall", new Vector3(-52, 0, -56), 200f, hill, SurfaceType.Sand);

            var west = Group("POI_Rocks", env).transform;
            Cover("Environment/Rock_B", new Vector3(-72, 0, 22), 10f, west, SurfaceType.Concrete);
            Cover("Environment/Rock_B", new Vector3(-80, 0, 34), 120f, west, SurfaceType.Concrete);
            Cover("Environment/Rock_A", new Vector3(-66, 0, 36), 200f, west, SurfaceType.Concrete);
            Cover("Environment/Ruin_C", new Vector3(-60, 0, 8), 260f, west, SurfaceType.Concrete);

            var ne = Group("POI_NorthEast", env).transform;
            Cover("Environment/Platform_Tower", new Vector3(42, 0, 52), 225f, ne, SurfaceType.Metal);
            Cover("Environment/Rock_A", new Vector3(32, 0, 62), 60f, ne, SurfaceType.Concrete);
            Cover("Environment/SandbagWall", new Vector3(48, 0, 44), 45f, ne, SurfaceType.Sand);

            var se = Group("POI_SouthEast", env).transform;
            Cover("Environment/Ruin_B", new Vector3(66, 0, -58), 300f, se, SurfaceType.Concrete);
            Cover("Environment/Girders", new Vector3(58, 0, -46), 80f, se, SurfaceType.Metal);
            Cover("Environment/Rock_B", new Vector3(78, 0, -70), 15f, se, SurfaceType.Concrete);

            var south = Group("POI_South", env).transform;
            Cover("Environment/Ruin_A", new Vector3(5, 0, -60), 160f, south, SurfaceType.Concrete);
            Cover("Environment/Container_Blue", new Vector3(-10, 0, -70), 40f, south, SurfaceType.Metal);
            Cover("Environment/CrateStack", new Vector3(14, 0, -72), 0f, south, SurfaceType.Wood);
        }

        private static void BuildScatter(Transform env)
        {
            var scatter = Group("Scatter", env).transform;
            var rnd = new System.Random(1234);
            string[] models = { "Environment/Rock_A", "Environment/Rock_C", "Environment/Rock_C", "Environment/Barrier", "Environment/Crate", "Environment/SandbagWall" };
            var poi = new[] { new Vector2(0, 0), new Vector2(0, 62), new Vector2(65, -8), new Vector2(-55, -50), new Vector2(-70, 25), new Vector2(42, 52), new Vector2(66, -58), new Vector2(3, -65) };
            int placed = 0;
            for (int i = 0; i < 400 && placed < 60; i++)
            {
                var p = new Vector2((float)rnd.NextDouble() * 200f - 100f, (float)rnd.NextDouble() * 200f - 100f);
                if (p.magnitude > 104f) continue;
                bool near = false;
                foreach (var q in poi)
                    if (Vector2.Distance(p, q) < 18f) near = true;
                if (near) continue;
                string m = models[rnd.Next(models.Length)];
                var surface = m.Contains("Crate") ? SurfaceType.Wood : m.Contains("Sandbag") ? SurfaceType.Sand : SurfaceType.Concrete;
                Cover(m, new Vector3(p.x, 0, p.y), (float)rnd.NextDouble() * 360f, scatter, surface);
                placed++;
            }
        }

        private static void CreateCoverPoints(Transform env)
        {
            var root = Group("CoverPoints", env).transform;
            int n = 0;
            foreach (var go in CoverObjects)
            {
                var b = LocalBounds(go);
                if (b.size.y < 0.8f) continue;
                var dirs = new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
                foreach (var d in dirs)
                {
                    Vector3 ext = Vector3.Scale(b.extents, new Vector3(Mathf.Abs(d.x), 0, Mathf.Abs(d.z)));
                    Vector3 local = b.center + d * (ext.magnitude + 0.9f);
                    Vector3 world = go.transform.TransformPoint(new Vector3(local.x, 0, local.z));
                    if (!NavMesh.SamplePosition(world, out var hit, 1.2f, NavMesh.AllAreas)) continue;
                    var cp = new GameObject("Cover").AddComponent<CoverPoint>();
                    cp.transform.SetParent(root, false);
                    cp.transform.position = hit.position;
                    n++;
                }
            }
            Debug.Log($"[GGOBuilder] Cover points: {n}");
        }

        private static void BuildLobby(Transform root, out SwordArt.World.SpawnPoint spawn, out TournamentTerminal register, out TournamentTerminal logout)
        {
            var o = LobbyOrigin;
            Place("Environment/Lobby_Hall", o, 0f, root, ColliderMode.Mesh, SurfaceType.Metal);
            spawn = Spawn(SpawnIds.GGOLobby, o + new Vector3(0f, 0.05f, 6f), 180f, root);

            var reg = Place("Props/Terminal", o + new Vector3(-3.5f, 0f, -1.5f), 0f, root, ColliderMode.Box, SurfaceType.Metal);
            reg.name = "Terminal_Register";
            register = reg.AddComponent<TournamentTerminal>();
            reg.AddComponent<WorldNameTag>().Configure("РЕГИСТРАЦИЯ", "Bullet of Bullets", new Color(1f, 0.65f, 0.25f), 2.1f, 20f);
            var exit = Place("Props/Terminal", o + new Vector3(3.5f, 0f, -1.5f), 0f, root, ColliderMode.Box, SurfaceType.Metal);
            exit.name = "Terminal_Logout";
            logout = exit.AddComponent<TournamentTerminal>();
            exit.AddComponent<WorldNameTag>().Configure("LOG OUT", "Выход из GGO", new Color(0.4f, 0.9f, 1f), 2.1f, 20f);

            Place("Props/WeaponRack", o + new Vector3(14.6f, 0f, 0f), -90f, root, ColliderMode.Box, SurfaceType.Metal);
            Place("Environment/CrateStack", o + new Vector3(11f, 0f, 8f), 20f, root, ColliderMode.Mesh, SurfaceType.Wood);

            // Shooting range
            for (int i = 0; i < 4; i++)
            {
                var d = Place("Props/TargetDummy", o + new Vector3(-12.5f, 0f, -7f + i * 4f), 90f, root, ColliderMode.Box, SurfaceType.Metal, false);
                d.name = "TargetDummy" + i;
                var h = d.AddComponent<Health>();
                SetField(h, "autoRegenerate", true);
                SetField(h, "maxHealth", 150f);
                d.AddComponent<WorldNameTag>().Configure("Мишень", "", new Color(1f, 0.85f, 0.4f), 2.35f, 25f);
            }
            var info = new GameObject("RangeInfo");
            info.transform.SetParent(root, false);
            info.transform.position = o + new Vector3(-6f, 1f, 9.5f);
            info.AddComponent<BoxCollider>().size = new Vector3(1.5f, 2f, 0.6f);
            info.AddComponent<SwordArt.World.InfoPoint>().Configure("Инструктаж", "Инструктор BoB", new[]
            {
                "Добро пожаловать в лобби Bullet of Bullets! ЛКМ — огонь, ПКМ — прицел, R — перезарядка, 1/2/3 и колесо — смена оружия, H — аптечка.",
                "Круг в центре экрана — «bullet circle»: пуля попадёт внутрь него. Он сужается, когда вы стоите и целитесь.",
                "Красная линия — «bullet line» противника: так он предупреждает вас о выстреле. Уходите с линии!",
                "Купол безопасной зоны сжимается. Снаружи вы теряете здоровье. Последний выживший получает 500 кредитов."
            });
            Place("Props/Terminal", info.transform.position - Vector3.up, 180f, root, ColliderMode.None, SurfaceType.Metal);

            var lights = Group("LobbyLights", root).transform;
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z += 2)
                PointLight("LobbyLight", o + new Vector3(x * 9f, 6.5f, z * 5f), new Color(0.75f, 0.85f, 1f), 3.5f, 16f, lights);
            PointLight("BannerLight", o + new Vector3(0f, 4f, -8f), new Color(1f, 0.6f, 0.25f), 3f, 10f, lights);
        }

        private static SafeZone BuildZone(Transform systems)
        {
            var zoneGo = new GameObject("SafeZone");
            zoneGo.transform.SetParent(systems, false);
            var zone = zoneGo.AddComponent<SafeZone>();
            var dome = Prim(PrimitiveType.Sphere, "ZoneDome", Vector3.zero, Vector3.one * 236f, "FX_EnergyDome", zoneGo.transform, false);
            GameObjectUtility.SetStaticEditorFlags(dome, 0);
            dome.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var marker = new GameObject("NextZoneMarker");
            marker.transform.SetParent(zoneGo.transform, false);
            var quad = Prim(PrimitiveType.Quad, "Ring", Vector3.zero, Vector3.one, "FX_ZoneRing", marker.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObjectUtility.SetStaticEditorFlags(quad, 0);
            zone.Configure(118f, new List<ZonePhase>
            {
                new ZonePhase { waitTime = 45f, shrinkTime = 30f, targetRadius = 80f, damagePerSecond = 2f },
                new ZonePhase { waitTime = 35f, shrinkTime = 25f, targetRadius = 50f, damagePerSecond = 4f },
                new ZonePhase { waitTime = 30f, shrinkTime = 20f, targetRadius = 28f, damagePerSecond = 6f },
                new ZonePhase { waitTime = 25f, shrinkTime = 20f, targetRadius = 12f, damagePerSecond = 10f },
                new ZonePhase { waitTime = 20f, shrinkTime = 15f, targetRadius = 3f, damagePerSecond = 15f }
            }, dome.transform, marker.transform);
            return zone;
        }

        private static GGOPlayer BuildPlayer(Transform spawn)
        {
            int playerLayer = LayerMask.NameToLayer(ProjectSetup.PlayerLayer);
            int weaponLayer = LayerMask.NameToLayer(ProjectSetup.WeaponLayer);

            var go = new GameObject("GGOPlayer");
            go.tag = "Player";
            go.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 50f;

            var cameraRoot = new GameObject("CameraRoot").transform;
            cameraRoot.SetParent(go.transform, false);
            cameraRoot.localPosition = new Vector3(0, 1.65f, 0);
            var cam = CreateMainCamera(cameraRoot, Vector3.zero, 72f, ~((1 << weaponLayer) | (1 << playerLayer)));

            var weaponCamGo = new GameObject("WeaponCamera");
            weaponCamGo.transform.SetParent(cam.transform, false);
            var weaponCam = weaponCamGo.AddComponent<Camera>();
            weaponCam.cullingMask = 1 << weaponLayer;
            weaponCam.fieldOfView = 62f;
            weaponCam.nearClipPlane = 0.01f;
            weaponCam.farClipPlane = 10f;
            var weaponData = weaponCam.GetUniversalAdditionalCameraData();
            weaponData.renderType = CameraRenderType.Overlay;
            weaponData.renderShadows = false;
            cam.GetUniversalAdditionalCameraData().cameraStack.Add(weaponCam);

            var holder = new GameObject("WeaponHolder").transform;
            holder.SetParent(cam.transform, false);
            holder.gameObject.layer = weaponLayer;

            var fps = go.AddComponent<FirstPersonController>();
            SetField(fps, "cameraRoot", cameraRoot);
            SetField(fps, "playerCamera", cam);
            go.AddComponent<Health>();
            var participant = go.AddComponent<Participant>();
            participant.Configure("Вы", true);
            var weapons = go.AddComponent<PlayerWeaponController>();
            SetField(weapons, "motor", fps);
            SetField(weapons, "playerCamera", cam);
            SetField(weapons, "weaponHolder", holder);
            SetField(weapons, "hitMask", (LayerMask)~((1 << weaponLayer) | (1 << 2)));
            var ggo = go.AddComponent<GGOPlayer>();
            ggo.Configure(fps, weapons);
            var interactor = go.AddComponent<Interactor>();
            SetField(interactor, "reachOrigin", cameraRoot);
            SetField(interactor, "reach", 2.8f);

            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.layer != weaponLayer) t.gameObject.layer = playerLayer;
            return ggo;
        }

        /// <summary>Creates/updates the bot prefab asset.</summary>
        [MenuItem("SwordArt/Build/Bot Prefab", priority = 30)]
        public static BotController BuildBotPrefab()
        {
            int layer = LayerMask.NameToLayer(ProjectSetup.CharacterLayer);
            var go = new GameObject("Bot");
            var model = CharacterModel("GGO_Soldier", go.transform);
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.speed = 3.4f;
            agent.angularSpeed = 540f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.62f;
            col.radius = 0.32f;
            col.center = new Vector3(0, 0.81f, 0);
            var health = go.AddComponent<Health>();
            go.AddComponent<Participant>();
            var anim = go.AddComponent<ProceduralCharacterAnimator>();
            anim.Configure(model.transform, true);

            var head = new GameObject("HeadHitbox");
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, 1.74f, 0);
            head.AddComponent<SphereCollider>().radius = 0.17f;
            head.AddComponent<Hitbox>().Configure(health, 2f);

            var eye = new GameObject("Eye").transform;
            eye.SetParent(go.transform, false);
            eye.localPosition = new Vector3(0, 1.66f, 0.12f);
            var mount = new GameObject("WeaponMount").transform;
            mount.SetParent(go.transform, false);
            mount.localPosition = new Vector3(0.12f, 1.36f, 0.38f);

            var lineGo = new GameObject("BulletLine");
            lineGo.transform.SetParent(go.transform, false);
            var line = lineGo.AddComponent<LineRenderer>();
            line.sharedMaterial = Mat("FX_Tracer");
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.widthMultiplier = 1f;
            line.widthCurve = AnimationCurve.Linear(0f, 0.03f, 1f, 0.006f);
            line.startColor = new Color(1f, 0.15f, 0.1f, 0.55f);
            line.endColor = new Color(1f, 0.15f, 0.1f, 0.25f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;

            var tag = go.AddComponent<WorldNameTag>();
            var bot = go.AddComponent<BotController>();
            int mask = ~((1 << LayerMask.NameToLayer(ProjectSetup.WeaponLayer)) | (1 << 2));
            bot.ConfigureReferences(eye, mount, anim, line, tag, mask);
            SetLayerRecursive(go, layer);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BotPrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, BotPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<BotController>();
        }
    }
}
