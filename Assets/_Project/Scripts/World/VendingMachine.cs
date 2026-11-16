using SwordArt.Core;
using SwordArt.Interaction;
using SwordArt.UI;
using UnityEngine;

namespace SwordArt.World
{
    /// <summary>Street vending machine: spends a few credits on a drink (small credit sink).</summary>
    public sealed class VendingMachine : InteractableBase
    {
        [SerializeField] private int price = 10;
        [SerializeField] private string[] drinks = { "холодный зелёный чай", "банку кофе", "лимонад «Ramune»", "энергетик «FullCharge»" };

        public override string Prompt => $"E — Купить напиток ({price} кр.)";

        public override void Interact(Interactor interactor)
        {
            var gm = GameManager.Instance;
            if (gm.Currency.TrySpend(price, "vending"))
            {
                gm.UI.Notify($"Вы купили {drinks[Random.Range(0, drinks.Length)]}. −{price} кр.", NotificationKind.Info);
                gm.SaveGame("vending");
            }
            else gm.UI.Notify("Недостаточно кредитов", NotificationKind.Error);
        }
    }
}
