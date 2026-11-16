using SwordArt.Core;
using SwordArt.Interaction;
using UnityEngine;

namespace SwordArt.World
{
    /// <summary>Shop counter / cashier that opens the shop window.</summary>
    public sealed class ShopCounter : InteractableBase
    {
        [SerializeField] private string shopTitle = "МАГАЗИН «ARMS & GEAR 24»";

        private void Reset() => promptText = "Купить";

        public override void Interact(Interactor interactor) => GameManager.Instance.UI.Shop.Show(shopTitle);
    }
}
