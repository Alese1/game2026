using SwordArt.Core;
using SwordArt.Interaction;
using SwordArt.UI;

namespace SwordArt.World
{
    /// <summary>Bed in the apartment: rest and save the game.</summary>
    public sealed class BedSavePoint : InteractableBase
    {
        private void Reset() => promptText = "Отдохнуть (сохранить игру)";

        public override void Interact(Interactor interactor)
        {
            var gm = GameManager.Instance;
            gm.SaveGame("bed");
            gm.UI.Notify("Вы отдохнули. Игра сохранена.", NotificationKind.Success);
        }
    }
}
