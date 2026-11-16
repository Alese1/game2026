using System;
using SwordArt.Inventory;

namespace SwordArt.Economy
{
    /// <summary>Result of a purchase attempt.</summary>
    public enum PurchaseResult
    {
        Success,
        NotEnoughCredits,
        AlreadyOwned,
        InvalidItem,
        Busy
    }

    /// <summary>
    /// Validates and executes purchases: checks funds and ownership, charges once,
    /// grants the item and rolls back if the item cannot be added.
    /// </summary>
    public sealed class ShopSystem
    {
        private readonly ICurrencyService _currency;
        private readonly IInventoryService _inventory;
        private bool _busy;

        /// <summary>Raised after a successful purchase.</summary>
        public event Action<ShopItemData> Purchased;

        public ShopSystem(ICurrencyService currency, IInventoryService inventory)
        {
            _currency = currency;
            _inventory = inventory;
        }

        /// <summary>Checks whether <paramref name="product"/> can be bought right now.</summary>
        public PurchaseResult CanPurchase(ShopItemData product)
        {
            if (product == null || product.item == null) return PurchaseResult.InvalidItem;
            if (_busy) return PurchaseResult.Busy;
            if (!_inventory.CanAdd(product.item, product.quantity)) return PurchaseResult.AlreadyOwned;
            if (!_currency.CanAfford(product.price)) return PurchaseResult.NotEnoughCredits;
            return PurchaseResult.Success;
        }

        /// <summary>Attempts to buy <paramref name="product"/>. Credits are charged at most once.</summary>
        public PurchaseResult TryPurchase(ShopItemData product)
        {
            var check = CanPurchase(product);
            if (check != PurchaseResult.Success) return check;

            _busy = true;
            try
            {
                if (!_currency.TrySpend(product.price, "shop:" + product.id)) return PurchaseResult.NotEnoughCredits;
                if (!_inventory.Add(product.item, product.quantity))
                {
                    _currency.Add(product.price, "refund:" + product.id);
                    return PurchaseResult.AlreadyOwned;
                }
            }
            finally
            {
                _busy = false;
            }

            Purchased?.Invoke(product);
            return PurchaseResult.Success;
        }
    }
}
