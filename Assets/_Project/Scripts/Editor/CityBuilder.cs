using SwordArt.Combat;
using SwordArt.Core;
using SwordArt.World;
using UnityEditor;
using UnityEngine;
using static SwordArt.EditorTools.SceneBuilderUtils;

namespace SwordArt.EditorTools
{
    /// <summary>
    /// Builds the real-world Japanese city district: crossroads, four blocks (apartment, shop,
    /// office, park), street furniture, enterable shop and office lobby, NPCs, lighting, NavMesh.
    /// Run manually: SwordArt/Build/City.
    /// </summary>
    public static class CityBuilder
    {
        public const string ScenePath = ProjectSetup.Root + "Scenes/RealWorld/City.unity";
        private const float Half = 58f;

        [MenuItem("SwordArt/Build/City", priority = 10)]
        public static void Build()
        {
            var scene = NewScene();
            var env = Group("Environment").transform;
            var props = Group("Props").transform;
            var interactables = Group("Interactables").transform;
            var npcs = Group("NPCs").transform;
            var spawns = Group("SpawnPoints").transform;

            SetupLighting(new Vector3(48f, -35f, 0f), new Color(1f, 0.95f, 0.86f), 1.35f, new Color(0.45f, 0.55f, 0.7f), new Color(0.4f, 0.38f, 0.36f), 0.9f,
                new Color(0.62f, 0.7f, 0.82f), new Color(0.55f, 0.55f, 0.55f), new Color(0.3f, 0.28f, 0.26f), new Color(0.72f, 0.78f, 0.86f), 0.004f, "Sky_City");
            CreateVolume("Volume_City", 0.55f, 0.15f, 8f, 8f, new Color(1f, 0.98f, 0.95f), 0.2f);

            BuildGround(env);
            BuildBuildings(env, interactables);
            BuildStreetProps(props, interactables);
            BuildShopInterior(props, interactables);
            BuildOfficeInterior(props, interactables);
            BuildPark(props, interactables);

            // Boundaries (keep the player inside the district).
            InvisibleWall("Bound_N", new Vector3(0, 5, Half), new Vector3(Half * 2, 12, 1), env);
            InvisibleWall("Bound_S", new Vector3(0, 5, -Half), new Vector3(Half * 2, 12, 1), env);
            InvisibleWall("Bound_E", new Vector3(Half, 5, 0), new Vector3(1, 12, Half * 2), env);
            InvisibleWall("Bound_W", new Vector3(-Half, 5, 0), new Vector3(1, 12, Half * 2), env);

            var defaultSpawn = Spawn(SpawnIds.Default, new Vector3(8f, 0.05f, 6f), 200f, spawns);
            Spawn(SpawnIds.CityApartmentEntrance, new Vector3(22f, 0.05f, 8.2f), 180f, spawns);

            var nav = BakeNavMesh(ProjectSetup.Root + "Scenes/RealWorld", "City", new Vector3(0, 5, 0), new Vector3(Half * 2, 20, Half * 2));
            nav.transform.SetParent(env);

            BuildNpcs(npcs);

            var player = CreateThirdPersonPlayer(defaultSpawn.transform.position, 200f, 3.6f);
            CreateContext(WorldType.RealWorld, player.transform, defaultSpawn);

            Save(scene, ScenePath);
        }

        private static void BuildGround(Transform env)
        {
            var g = Group("Ground", env).transform;
            Slab("Base", new Vector3(-140, -1, -140), new Vector3(140, 0, 140), "M_Sidewalk", g);
            Slab("Road_EW", new Vector3(-140, -0.1f, -4), new Vector3(140, 0.01f, 4), "M_Asphalt", g);
            Slab("Road_NS", new Vector3(-4, -0.1f, -140), new Vector3(4, 0.012f, 140), "M_Asphalt", g);
            // Lane markings (dashed centre lines, stop lines, crosswalks)
            for (float x = -130; x < 130; x += 6)
                if (Mathf.Abs(x) > 9) Prim(PrimitiveType.Cube, "Lane", new Vector3(x, 0.015f, 0), new Vector3(3f, 0.01f, 0.15f), "M_RoadLine", g, false);
            for (float z = -130; z < 130; z += 6)
                if (Mathf.Abs(z) > 9) Prim(PrimitiveType.Cube, "Lane", new Vector3(0, 0.017f, z), new Vector3(0.15f, 0.01f, 3f), "M_RoadLine", g, false);
            for (int side = -1; side <= 1; side += 2)
            {
                for (float k = -3.3f; k <= 3.3f; k += 0.9f)
                {
                    Prim(PrimitiveType.Cube, "Crosswalk", new Vector3(side * 6f, 0.016f, k), new Vector3(2.2f, 0.01f, 0.45f), "M_RoadLine", g, false);
                    Prim(PrimitiveType.Cube, "Crosswalk", new Vector3(k, 0.018f, side * 6f), new Vector3(0.45f, 0.01f, 2.2f), "M_RoadLine", g, false);
                }
            }
            // Curbs (gaps at the crossing)
            foreach (int s in new[] { -1, 1 })
            {
                Slab("Curb", new Vector3(-140, 0, s * 4f - 0.15f), new Vector3(-7.5f, 0.12f, s * 4f + 0.15f), "M_Concrete", g);
                Slab("Curb", new Vector3(7.5f, 0, s * 4f - 0.15f), new Vector3(140, 0.12f, s * 4f + 0.15f), "M_Concrete", g);
                Slab("Curb", new Vector3(s * 4f - 0.15f, 0, -140), new Vector3(s * 4f + 0.15f, 0.12f, -7.5f), "M_Concrete", g);
                Slab("Curb", new Vector3(s * 4f - 0.15f, 0, 7.5f), new Vector3(s * 4f + 0.15f, 0.12f, 140), "M_Concrete", g);
            }
            // Park lawn
            Slab("ParkLawn", new Vector3(-50, 0, -50), new Vector3(-9, 0.04f, -9), "M_Grass", g);
            Slab("ParkPath_EW", new Vector3(-50, 0, -31), new Vector3(-9, 0.05f, -28), "M_Sidewalk", g);
            Slab("ParkPath_NS", new Vector3(-31, 0, -50), new Vector3(-28, 0.051f, -9), "M_Sidewalk", g);
        }

        private static void BuildBuildings(Transform env, Transform interactables)
        {
            var b = Group("Buildings", env).transform;
            // Player's apartment block (front faces the main road, -Z).
            var apt = Place("Buildings/Building_Apartment", new Vector3(22, 0, 16), 180f, b, ColliderMode.Mesh, SurfaceType.Concrete);
            apt.name = "ApartmentBuilding_Home";
            var door = new GameObject("Door_Apartment");
            door.transform.SetParent(interactables, false);
            door.transform.position = new Vector3(22f, 1.2f, 9.85f);
            var bc = door.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.2f, 2.4f, 0.4f);
            door.AddComponent<SceneDoor>().Configure(SceneNames.Apartment, SpawnIds.ApartmentDoor, "Войти домой", "Квартира");

            Place("Buildings/Building_Shop", new Vector3(-20, 0, 14), 180f, b, ColliderMode.Mesh, SurfaceType.Concrete);
            Place("Buildings/Building_Office", new Vector3(22, 0, -18), 0f, b, ColliderMode.Mesh, SurfaceType.Concrete);

            Place("Buildings/House_A", new Vector3(44, 0, 15), 180f, b);
            Place("Buildings/House_B", new Vector3(-44, 0, 15), 180f, b);
            Place("Buildings/Building_Tower_A", new Vector3(44, 0, 40), 180f, b);
            Place("Buildings/Building_Tower_B", new Vector3(20, 0, 42), 180f, b);
            Place("Buildings/House_B", new Vector3(-20, 0, 40), 180f, b);
            Place("Buildings/Building_Tower_A", new Vector3(-44, 0, 42), 180f, b);
            Place("Buildings/House_A", new Vector3(46, 0, -16), 0f, b);
            Place("Buildings/Building_Tower_B", new Vector3(44, 0, -42), 0f, b);
            Place("Buildings/House_B", new Vector3(20, 0, -44), 0f, b);
            Place("Buildings/House_A", new Vector3(-50, 0, -55) + new Vector3(0, 0, 70), 0f, b).name = "House_A_West";

            // Distant skyline (outside the playable bounds)
            var sky = Group("Skyline", env).transform;
            int i = 0;
            for (float t = -100; t <= 100; t += 20, i++)
            {
                string m = i % 2 == 0 ? "Buildings/Building_Tower_A" : "Buildings/Building_Tower_B";
                Place(m, new Vector3(t, 0, 80 + (i % 3) * 6), 180f, sky, ColliderMode.None, null, true, 1.2f + (i % 3) * 0.2f);
                Place(m, new Vector3(t + 10, 0, -80 - (i % 2) * 8), 0f, sky, ColliderMode.None, null, true, 1.1f + (i % 4) * 0.2f);
                Place(m, new Vector3(80 + (i % 2) * 8, 0, t), -90f, sky, ColliderMode.None, null, true, 1.3f + (i % 3) * 0.15f);
                Place(m, new Vector3(-80 - (i % 3) * 6, 0, t + 10), 90f, sky, ColliderMode.None, null, true, 1.2f + (i % 2) * 0.3f);
            }
        }

        private static void BuildStreetProps(Transform props, Transform interactables)
        {
            var lamps = Group("StreetLamps", props).transform;
            for (float x = -54; x <= 54; x += 12)
            {
                if (Mathf.Abs(x) < 8) continue;
                Place("Props/StreetLamp", new Vector3(x, 0, 7.3f), 180f, lamps, ColliderMode.Box, SurfaceType.Metal);
                Place("Props/StreetLamp", new Vector3(x + 6, 0, -7.3f), 0f, lamps, ColliderMode.Box, SurfaceType.Metal);
            }
            for (float z = -54; z <= 54; z += 12)
            {
                if (Mathf.Abs(z) < 8) continue;
                Place("Props/StreetLamp", new Vector3(7.3f, 0, z + 6), -90f, lamps, ColliderMode.Box, SurfaceType.Metal);
                Place("Props/StreetLamp", new Vector3(-7.3f, 0, z), 90f, lamps, ColliderMode.Box, SurfaceType.Metal);
            }
            var lights = Group("LampLights", props).transform;
            foreach (var p in new[] { new Vector3(-6.5f, 4.8f, 6.5f), new Vector3(6.5f, 4.8f, -6.5f), new Vector3(22, 3f, 8.8f), new Vector3(-20, 3.4f, 8f) })
                PointLight("StreetLight", p, new Color(1f, 0.85f, 0.6f), 2.5f, 12f, lights);

            var misc = Group("StreetFurniture", props).transform;
            Place("Props/RoadSign_Stop", new Vector3(6.4f, 0, 6.4f), 180f, misc, ColliderMode.Box, SurfaceType.Metal);
            Place("Props/RoadSign_Stop", new Vector3(-6.4f, 0, -6.4f), 0f, misc, ColliderMode.Box, SurfaceType.Metal);
            Place("Props/RoadSign_Stop", new Vector3(6.4f, 0, -6.4f), -90f, misc, ColliderMode.Box, SurfaceType.Metal);
            Place("Props/RoadSign_Stop", new Vector3(-6.4f, 0, 6.4f), 90f, misc, ColliderMode.Box, SurfaceType.Metal);
            foreach (float z in new[] { -40f, -20f, 20f, 40f })
                Place("Props/UtilityPole", new Vector3(5.6f, 0, z + 3), 90f, misc, ColliderMode.Box);
            Place("Props/TrashBins", new Vector3(-13.5f, 0, 8.2f), 180f, misc, ColliderMode.Box);
            Place("Props/TrashBins", new Vector3(14f, 0, -9f), 0f, misc, ColliderMode.Box);
            Place("Props/Planter", new Vector3(17f, 0, 9.2f), 180f, misc, ColliderMode.Box);
            Place("Props/Planter", new Vector3(27f, 0, 9.2f), 180f, misc, ColliderMode.Box);
            Place("Props/Planter", new Vector3(30f, 0, -9.6f), 0f, misc, ColliderMode.Box);
            foreach (float x in new[] { -50f, -36f, 36f, 50f })
            {
                Place("Props/Tree_A", new Vector3(x, 0, 8.6f), 0f, misc, ColliderMode.None);
                Place("Props/Tree_B", new Vector3(x + 5, 0, -8.6f), 0f, misc, ColliderMode.None);
            }

            var bus = Place("Props/BusStop", new Vector3(-40f, 0, -6.6f), 180f, misc, ColliderMode.Mesh);
            var ad = new GameObject("BusStopPoster");
            ad.transform.SetParent(interactables, false);
            ad.transform.position = new Vector3(-41.45f, 1.3f, -6.6f);
            ad.AddComponent<BoxCollider>().size = new Vector3(1.1f, 1.6f, 0.3f);
            ad.AddComponent<InfoPoint>().Configure("Прочитать афишу", "Афиша", new[]
            {
                "GUN GALE ONLINE — 3-й турнир BULLET OF BULLETS!\nКоролевская битва: выживает только один. Регистрация открыта в лобби турнира.",
                "Призовой фонд: 500 кредитов победителю, награды за каждое устранение.\nОборудование: AmuSphere / FullDive совместимые устройства."
            });

            var vend1 = Place("Props/VendingMachine", new Vector3(-12.2f, 0, -8.9f), 0f, interactables, ColliderMode.Box);
            MakeInteractable<VendingMachine>(vend1, "Купить напиток");
            var vend2 = Place("Props/VendingMachine", new Vector3(12.5f, 0, 8.9f), 180f, interactables, ColliderMode.Box);
            MakeInteractable<VendingMachine>(vend2, "Купить напиток");
        }

        private static void BuildShopInterior(Transform props, Transform interactables)
        {
            var shop = Group("ShopInterior", props).transform;
            var counter = Place("Props/ShopCounter", new Vector3(-20f, 0.1f, 16.6f), 180f, interactables, ColliderMode.Box);
            counter.name = "ShopCounter";
            MakeInteractable<ShopCounter>(counter, "Купить");
            for (int i = 0; i < 3; i++)
            {
                Place("Props/ShopShelf", new Vector3(-26.3f, 0.1f, 11.5f + i * 2.4f), 90f, shop, ColliderMode.Box, SurfaceType.Metal);
                Place("Props/ShopShelf", new Vector3(-13.7f, 0.1f, 11.5f + i * 2.4f), -90f, shop, ColliderMode.Box, SurfaceType.Metal);
            }
            Place("Props/WeaponRack", new Vector3(-23.5f, 0.1f, 18.5f), 180f, shop, ColliderMode.Box, SurfaceType.Metal);
            Place("Props/WeaponRack", new Vector3(-16.5f, 0.1f, 18.5f), 180f, shop, ColliderMode.Box, SurfaceType.Metal);
            PointLight("ShopLight_A", new Vector3(-22f, 4f, 13.5f), new Color(1f, 0.97f, 0.92f), 3f, 9f, shop);
            PointLight("ShopLight_B", new Vector3(-18f, 4f, 16.5f), new Color(1f, 0.97f, 0.92f), 3f, 9f, shop);
        }

        private static void BuildOfficeInterior(Transform props, Transform interactables)
        {
            var office = Group("OfficeInterior", props).transform;
            Place("Props/ReceptionDesk", new Vector3(22f, 0.1f, -14.5f), 0f, office, ColliderMode.Box);
            for (int i = 0; i < 3; i++)
            {
                var desk = Place("Props/OfficeDesk", new Vector3(17f + i * 5f, 0.1f, -21.5f), 0f, i == 1 ? interactables : office, ColliderMode.Box);
                if (i == 1)
                {
                    desk.name = "JobTerminal";
                    MakeInteractable<JobTerminal>(desk, "Начать работу (ввод данных)");
                }
            }
            Place("Props/OfficePlant", new Vector3(14f, 0.1f, -12.3f), 0f, office, ColliderMode.Box);
            Place("Props/OfficePlant", new Vector3(30f, 0.1f, -12.3f), 0f, office, ColliderMode.Box);
            Place("Props/OfficePlant", new Vector3(30f, 0.1f, -24f), 0f, office, ColliderMode.Box);
            PointLight("OfficeLight_A", new Vector3(19f, 4f, -16f), new Color(0.95f, 0.98f, 1f), 3f, 10f, office);
            PointLight("OfficeLight_B", new Vector3(25f, 4f, -20f), new Color(0.95f, 0.98f, 1f), 3f, 10f, office);
        }

        private static void BuildPark(Transform props, Transform interactables)
        {
            var park = Group("Park", props).transform;
            Place("Props/Fountain", new Vector3(-29.5f, 0.04f, -29.5f), 0f, park, ColliderMode.Mesh, SurfaceType.Concrete);
            float[] angles = { 45, 135, 225, 315 };
            foreach (float a in angles)
            {
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Place("Props/Bench", new Vector3(-29.5f, 0.04f, -29.5f) + dir * 6.5f, a, park, ColliderMode.Box, SurfaceType.Wood);
            }
            var rnd = new System.Random(7);
            for (int i = 0; i < 16; i++)
            {
                var p = new Vector3(-48 + (float)rnd.NextDouble() * 37, 0.04f, -48 + (float)rnd.NextDouble() * 37);
                if (Mathf.Abs(p.x + 29.5f) < 4.5f || Mathf.Abs(p.z + 29.5f) < 4.5f) continue;
                Place(i % 2 == 0 ? "Props/Tree_A" : "Props/Tree_B", p, (float)rnd.NextDouble() * 360f, park, ColliderMode.None);
            }
            Place("Props/TrashBins", new Vector3(-24f, 0.04f, -26.5f), 0f, park, ColliderMode.Box);
        }

        private static void BuildNpcs(Transform npcs)
        {
            CreateNpc("NPC_Ayano", "Character_Female", new Vector3(-24.5f, 0.05f, -24.8f), 225f, npcs, null);
            CreateNpc("NPC_Kenta", "Character_Male", new Vector3(-20f, 0.1f, 18f), 180f, npcs, null);
            CreateNpc("NPC_Misaki", "Character_Female", new Vector3(18f, 0.05f, -9.5f), 0f, npcs,
                Route("Misaki", npcs, new Vector3(18, 0, -9.5f), new Vector3(9, 0, -9.5f), new Vector3(22, 0, -13f)));
            CreateNpc("NPC_Jun", "Character_Male", new Vector3(-30f, 0.05f, 6f), 90f, npcs,
                Route("Jun", npcs, new Vector3(-45, 0, 6), new Vector3(-10, 0, 6), new Vector3(-6, 0, 30), new Vector3(-10, 0, 6)));
            CreateNpc("NPC_Hana", "Character_Female", new Vector3(-33.6f, 0.05f, -33.6f), 45f, npcs, null);
            CreateNpc("NPC_Ryo", "Character_Male", new Vector3(30f, 0.05f, 6f), 270f, npcs,
                Route("Ryo", npcs, new Vector3(45, 0, 6), new Vector3(6, 0, 6), new Vector3(6, 0, -40), new Vector3(6, 0, 6)));
            CreateNpc("NPC_Yuki", "Character_Female", new Vector3(-12f, 0.05f, -11f), 20f, npcs,
                Route("Yuki", npcs, new Vector3(-12, 0, -11), new Vector3(-20, 0, -20), new Vector3(-12, 0, -11), new Vector3(-6, 0, -20)));
        }
    }
}
