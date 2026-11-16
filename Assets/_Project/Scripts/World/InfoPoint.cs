using SwordArt.Core;
using SwordArt.Interaction;
using UnityEngine;

namespace SwordArt.World
{
    /// <summary>Readable object (PC, poster, sign) that shows text in the dialogue window.</summary>
    public sealed class InfoPoint : InteractableBase
    {
        [SerializeField] private string title = "Информация";
        [SerializeField, TextArea(2, 5)] private string[] lines = { "..." };

        /// <summary>Configures the info point (used by scene builders).</summary>
        public void Configure(string prompt, string header, string[] text)
        {
            promptText = prompt;
            title = header;
            lines = text;
        }

        public override void Interact(Interactor interactor) => GameManager.Instance.UI.Dialogue.Show(title, lines);
    }
}
