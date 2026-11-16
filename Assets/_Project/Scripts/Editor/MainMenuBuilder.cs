using SwordArt.Core;
using SwordArt.UI;
using UnityEditor;
using UnityEngine;
using static SwordArt.EditorTools.SceneBuilderUtils;

namespace SwordArt.EditorTools
{
    /// <summary>
    /// Builds the start menu scene: an orbiting showcase of the FullDive headset under neon light
    /// with the menu UI. Also provides "Build All". Run manually: SwordArt/Build/Main Menu.
    /// </summary>
    public static class MainMenuBuilder
    {
        public const string ScenePath = ProjectSetup.Root + "Scenes/Bootstrap/MainMenu.unity";

        [MenuItem("SwordArt/Build/Main Menu", priority = 13)]
        public static void Build()
        {
            var scene = NewScene();
            SetupLighting(new Vector3(40f, 200f, 0f), new Color(0.6f, 0.75f, 1f), 0.4f, new Color(0.1f, 0.12f, 0.2f), new Color(0.05f, 0.05f, 0.07f), 0.4f,
                new Color(0.12f, 0.15f, 0.25f), new Color(0.08f, 0.08f, 0.12f), new Color(0.03f, 0.03f, 0.05f), new Color(0.03f, 0.04f, 0.07f), 0.03f, "Sky_Menu");
            CreateVolume("Volume_Menu", 1.4f, 0f, 15f, 5f, new Color(0.92f, 0.95f, 1f), 0.4f);

            var stage = Group("Stage").transform;
            Prim(PrimitiveType.Cylinder, "Pedestal", new Vector3(0, -0.15f, 0), new Vector3(1.4f, 0.15f, 1.4f), "M_LobbyFloor", stage);
            Prim(PrimitiveType.Cylinder, "PedestalGlow", new Vector3(0, -0.01f, 0), new Vector3(1.45f, 0.01f, 1.45f), "M_NeonBlue", stage, false);
            Prim(PrimitiveType.Plane, "Floor", new Vector3(0, -0.3f, 0), new Vector3(10, 1, 10), "M_LobbyFloor", stage);
            var headset = Place("Props/FullDive_Headset", new Vector3(0, 0f, 0), 0f, stage, ColliderMode.None, null, false, 3.2f);
            headset.name = "Headset";
            var spin = headset.AddComponent<MenuSpin>();
            spin.speed = 12f;

            PointLight("KeyLight", new Vector3(1.2f, 1.6f, -1.2f), new Color(0.4f, 0.85f, 1f), 6f, 6f, stage, true);
            PointLight("RimLight", new Vector3(-1.5f, 1.2f, 1.2f), new Color(1f, 0.4f, 0.7f), 5f, 6f, stage);
            PointLight("Fill", new Vector3(0f, 2.5f, 0f), new Color(0.8f, 0.85f, 1f), 1.5f, 6f, stage);

            var pivot = new GameObject("CameraPivot").transform;
            pivot.position = new Vector3(0f, 0.5f, 0f);
            var cam = CreateMainCamera(pivot, new Vector3(-0.9f, 0.55f, -2.7f), 42f);
            cam.transform.LookAt(new Vector3(-0.7f, 0.45f, 0f));
            cam.clearFlags = CameraClearFlags.Skybox;

            var menu = new GameObject("MainMenu");
            var controller = menu.AddComponent<MainMenuController>();
            SetField(controller, "cameraPivot", pivot);
            CreateContext(WorldType.Menu, null, null);
            Save(scene, ScenePath);
        }

        [MenuItem("SwordArt/Build/ALL (setup + all scenes)", priority = 0)]
        public static void BuildAll()
        {
            ProjectSetup.RunAll();
            CityBuilder.Build();
            ApartmentBuilder.Build();
            GGOBuilder.Build();
            Build();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
