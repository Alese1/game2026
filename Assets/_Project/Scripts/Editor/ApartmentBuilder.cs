using SwordArt.Core;
using SwordArt.World;
using UnityEditor;
using UnityEngine;
using static SwordArt.EditorTools.SceneBuilderUtils;

namespace SwordArt.EditorTools
{
    /// <summary>
    /// Builds the hero's apartment: room shell, furniture, FullDive headset (connects to GGO),
    /// bed (save point), PC news terminal, exit door to the city and a city view behind the window.
    /// Run manually: SwordArt/Build/Apartment.
    /// </summary>
    public static class ApartmentBuilder
    {
        public const string ScenePath = ProjectSetup.Root + "Scenes/Apartment/Apartment.unity";

        [MenuItem("SwordArt/Build/Apartment", priority = 11)]
        public static void Build()
        {
            var scene = NewScene();
            var env = Group("Environment").transform;
            var furniture = Group("Furniture").transform;
            var interactables = Group("Interactables").transform;
            var spawns = Group("SpawnPoints").transform;

            SetupLighting(new Vector3(30f, 80f, 0f), new Color(1f, 0.88f, 0.72f), 1.1f, new Color(0.55f, 0.5f, 0.6f), new Color(0.35f, 0.33f, 0.3f), 1.1f,
                new Color(0.55f, 0.55f, 0.62f), new Color(0.45f, 0.42f, 0.4f), new Color(0.25f, 0.22f, 0.2f), new Color(0.75f, 0.72f, 0.7f), 0.002f, "Sky_Apartment");
            CreateVolume("Volume_Apartment", 0.7f, 0.25f, 6f, 6f, new Color(1f, 0.96f, 0.9f), 0.28f);

            // Room shell (door on the +Z wall at x = 3, window on the -X wall).
            Place("Props/Apartment_Room", Vector3.zero, 0f, env, ColliderMode.Mesh);

            // City view outside the window (the apartment is on the 3rd floor).
            var view = Group("WindowView", env).transform;
            Slab("OutsideGround", new Vector3(-80, -9, -60), new Vector3(-6, -8, 60), "M_Asphalt", view, false);
            Place("Buildings/Building_Tower_A", new Vector3(-30, -8, -12), 90f, view, ColliderMode.None);
            Place("Buildings/Building_Tower_B", new Vector3(-34, -8, 10), 90f, view, ColliderMode.None);
            Place("Buildings/Building_Office", new Vector3(-48, -8, -30), 90f, view, ColliderMode.None);
            Place("Props/Tree_B", new Vector3(-12, -8, 2), 0f, view, ColliderMode.None, null, true, 1.6f);

            Place("Props/Bed", new Vector3(-3.2f, 0f, -2.3f), 0f, furniture, ColliderMode.Box, SwordArt.Combat.SurfaceType.Wood);
            Place("Props/Nightstand", new Vector3(-1.95f, 0f, -3.1f), 0f, furniture, ColliderMode.Box);
            Place("Props/PCDesk", new Vector3(3.3f, 0f, -3.05f), 0f, interactables, ColliderMode.Box).name = "PCDesk";
            Place("Props/Bookshelf", new Vector3(1.4f, 0f, -3.3f), 0f, furniture, ColliderMode.Box);
            Place("Props/Kitchenette", new Vector3(4.15f, 0f, -0.2f), -90f, furniture, ColliderMode.Box);
            Place("Props/Rug", new Vector3(-2.2f, 0f, 1.6f), 90f, furniture, ColliderMode.None);
            Place("Props/Sofa", new Vector3(-3.95f, 0f, 1.6f), 90f, furniture, ColliderMode.Box);
            Place("Props/TVStand", new Vector3(-0.4f, 0f, 1.6f), -90f, furniture, ColliderMode.Box);
            Place("Props/OfficePlant", new Vector3(-4.0f, 0f, 3.1f), 0f, furniture, ColliderMode.Box);
            Place("Props/OfficePlant", new Vector3(1.4f, 0f, 3.1f), 0f, furniture, ColliderMode.Box);

            // FullDive headset on the nightstand.
            var device = Place("Props/FullDive_Headset", new Vector3(-2.0f, 0.5f, -3.1f), 20f, interactables, ColliderMode.None);
            device.name = "FullDive_Headset";
            var trigger = device.AddComponent<BoxCollider>();
            trigger.center = new Vector3(0f, 0.15f, 0f);
            trigger.size = new Vector3(0.45f, 0.35f, 0.45f);
            var fullDive = device.AddComponent<FullDiveDevice>();
            fullDive.SetPrompt("Подключиться к Gun Gale Online");
            var glow = PointLight("DeviceGlow", device.transform.position + Vector3.up * 0.25f, new Color(0.35f, 0.85f, 1f), 1.2f, 2.5f, device.transform);
            var glowRenderers = new System.Collections.Generic.List<Object>();
            foreach (var r in device.GetComponentsInChildren<Renderer>())
                if (r.sharedMaterial != null && r.sharedMaterial.name == "M_DeviceGlow") glowRenderers.Add(r);
            SetField(fullDive, "statusLight", glow);
            SetArray(fullDive, "glowRenderers", glowRenderers);

            // PC: news about the tournament.
            var pc = GameObject.Find("PCDesk");
            pc.AddComponent<InfoPoint>().Configure("Проверить новости на ПК", "MMO Stream — новости", new[]
            {
                "MMO STREAM: «Третий турнир Bullet of Bullets в Gun Gale Online стартует сегодня! Самые меткие стрелки виртуального мира сойдутся в пустошах».",
                "«Совет новичкам: красная линия прицеливания противника — ваш лучший друг. Двигайтесь, когда видите её».",
                "Личные сообщения: «Купи хотя бы автомат перед турниром. С одним пистолетом будет тяжело. — Дзюн»"
            });

            var bed = GameObject.Find("Bed");
            bed.transform.SetParent(interactables, true);
            bed.AddComponent<BedSavePoint>().SetPrompt("Отдохнуть (сохранить игру)");

            var door = new GameObject("Door_Exit");
            door.transform.SetParent(interactables, false);
            door.transform.position = new Vector3(3f, 1.1f, 3.5f);
            door.AddComponent<BoxCollider>().size = new Vector3(1.2f, 2.2f, 0.5f);
            door.AddComponent<SceneDoor>().Configure(SceneNames.City, SpawnIds.CityApartmentEntrance, "Выйти на улицу", "Город");

            PointLight("CeilingLight", new Vector3(0f, 2.55f, 0f), new Color(1f, 0.9f, 0.75f), 2.2f, 10f, env, true);
            PointLight("DeskLight", new Vector3(3.2f, 1.6f, -2.6f), new Color(0.6f, 0.8f, 1f), 0.8f, 3f, env);
            PointLight("WindowFill", new Vector3(-3.8f, 1.8f, 0f), new Color(1f, 0.92f, 0.8f), 1.2f, 6f, env);

            var defaultSpawn = Spawn(SpawnIds.ApartmentDoor, new Vector3(3f, 0.05f, 2.4f), 180f, spawns);
            Spawn(SpawnIds.ApartmentFullDive, new Vector3(-1.3f, 0.05f, -1.6f), 230f, spawns);
            Spawn(SpawnIds.Default, new Vector3(0f, 0.05f, 0.5f), 180f, spawns);

            BakeNavMesh(ProjectSetup.Root + "Scenes/Apartment", "Apartment", new Vector3(0, 1.5f, 0), new Vector3(10, 4, 8)).transform.SetParent(env);

            var player = CreateThirdPersonPlayer(defaultSpawn.transform.position, 180f, 2.4f);
            CreateContext(WorldType.RealWorld, player.transform, defaultSpawn);
            Save(scene, ScenePath);
        }
    }
}
