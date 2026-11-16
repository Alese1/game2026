using SwordArt.Core;
using SwordArt.Interaction;
using UnityEngine;

namespace SwordArt.World
{
    /// <summary>Door that saves the game and loads another scene at a named spawn point.</summary>
    public sealed class SceneDoor : InteractableBase
    {
        [SerializeField] private string targetScene = SceneNames.City;
        [SerializeField] private string targetSpawnId = SpawnIds.Default;
        [SerializeField] private string loadingTitle = "Загрузка";

        /// <summary>Configures the door (used by scene builders).</summary>
        public void Configure(string scene, string spawnId, string prompt, string title)
        {
            targetScene = scene;
            targetSpawnId = spawnId;
            promptText = prompt;
            loadingTitle = title;
        }

        public override bool CanInteract(Interactor interactor)
            => base.CanInteract(interactor) && GameManager.Instance != null && !GameManager.Instance.SceneLoader.IsLoading;

        public override void Interact(Interactor interactor)
        {
            var gm = GameManager.Instance;
            gm.SaveGame("door");
            gm.SceneLoader.LoadScene(targetScene, targetSpawnId, loadingTitle);
        }
    }
}
