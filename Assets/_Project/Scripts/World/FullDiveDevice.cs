using SwordArt.Core;
using SwordArt.Interaction;
using UnityEngine;

namespace SwordArt.World
{
    /// <summary>
    /// The AmuSphere-style FullDive headset. Saves the game and connects to Gun Gale Online
    /// (async load of the GGO scene, player appears in the tournament lobby).
    /// </summary>
    public sealed class FullDiveDevice : InteractableBase
    {
        [SerializeField] private Light statusLight;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private Color glowColor = new Color(0.3f, 0.85f, 1f);

        private MaterialPropertyBlock _block;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Reset() => promptText = "Подключиться к Gun Gale Online";

        public override bool CanInteract(Interactor interactor)
            => base.CanInteract(interactor) && !GameManager.Instance.SceneLoader.IsLoading;

        public override void Interact(Interactor interactor)
        {
            var gm = GameManager.Instance;
            gm.SaveGame("fulldive");
            gm.SceneLoader.LoadScene(SceneNames.GGO, SpawnIds.GGOLobby, "LINK START");
        }

        private void Update()
        {
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 2.2f);
            if (statusLight != null) statusLight.intensity = 0.6f + pulse * 0.8f;
            if (glowRenderers == null || glowRenderers.Length == 0) return;
            _block ??= new MaterialPropertyBlock();
            foreach (var r in glowRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(EmissionColor, glowColor * (1.5f + pulse * 2.5f));
                r.SetPropertyBlock(_block);
            }
        }
    }
}
