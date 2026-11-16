using SwordArt.Core;
using SwordArt.Interaction;

namespace SwordArt.World
{
    /// <summary>Office workstation that starts the data-entry job mini-game.</summary>
    public sealed class JobTerminal : InteractableBase
    {
        private void Reset() => promptText = "Начать работу";

        public override void Interact(Interactor interactor) => GameManager.Instance.UI.Job.Show();
    }
}
