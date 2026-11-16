using System.Collections.Generic;
using SwordArt.Audio;
using SwordArt.Core;
using SwordArt.Economy;
using SwordArt.Inventory;
using SwordArt.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>
    /// Shop interface: product list with icon, description, price and a buy button,
    /// current balance, ownership state and purchase feedback.
    /// </summary>
    public sealed class ShopWindow : UIWindow
    {
        private sealed class Row
        {
            public ShopItemData Product;
            public Button Buy;
            public Text Status;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _balance;
        private Text _shopTitle;
        private string _title = "МАГАЗИН";
        private float _cooldownUntil;

        /// <summary>Opens the shop with a custom title.</summary>
        public void Show(string title)
        {
            _title = title;
            Open();
            _shopTitle.text = _title;
            RefreshAll();
        }

        protected override void Build(UITheme theme)
        {
            _rows.Clear();
            var panel = CreateDialogFrame(theme, new Vector2(1180, 860), _title);
            _shopTitle = panel.Find("Title").GetComponent<Text>();

            _balance = UIKit.Text(panel, "Balance", "", 26, new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleRight, FontStyle.Bold);
            _balance.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(-64, 52));

            var list = UIKit.Rect(panel, "List");
            list.Place(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(-64, -200));
            UIKit.VerticalLayout(list, 10, new RectOffset(0, 0, 0, 0));

            var db = GameManager.Instance.Config != null ? GameManager.Instance.Config.itemDatabase : null;
            if (db != null)
                foreach (var product in db.shopItems)
                    if (product != null) _rows.Add(CreateRow(list, product, theme));

            var close = UIKit.Button(panel, "Закрыть [Esc]", theme, Close);
            ((RectTransform)close.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(300, 54));
        }

        private Row CreateRow(Transform parent, ShopItemData product, UITheme theme)
        {
            var bg = UIKit.Image(parent, "Row_" + product.id, new Color(1f, 1f, 1f, 0.04f), UIKit.Rounded);
            UIKit.Height(bg, 104);

            var item = product.item;
            Color accent = item != null ? item.accentColor : theme.Accent;
            var iconBg = UIKit.Image(bg.transform, "Icon", new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 1f), UIKit.Rounded);
            iconBg.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(72, 72));
            var sprite = product.DisplayIcon;
            if (sprite != null)
            {
                var icon = UIKit.Image(iconBg.transform, "Sprite", Color.white, sprite, false);
                icon.preserveAspect = true;
                icon.rectTransform.Stretch(4);
            }
            else
            {
                var glyph = UIKit.Text(iconBg.transform, "Glyph", Glyph(product.itemType, item), 26, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                glyph.rectTransform.Stretch();
            }

            var name = UIKit.Text(bg.transform, "Name", product.displayName, 26, theme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            name.rectTransform.Place(new Vector2(0, 1), new Vector2(0.62f, 1), new Vector2(0, 1), new Vector2(100, -10), new Vector2(0, 34));
            var type = UIKit.Text(bg.transform, "Type", TypeLabel(product.itemType, item), 15, accent, TextAnchor.UpperRight);
            type.rectTransform.Place(new Vector2(0.62f, 1), new Vector2(0.77f, 1), new Vector2(1, 1), new Vector2(0, -12), new Vector2(0, 40));
            var desc = UIKit.Text(bg.transform, "Desc", product.description + StatsLine(item), 17, theme.TextDim, TextAnchor.UpperLeft);
            desc.rectTransform.Place(new Vector2(0, 0), new Vector2(0.62f, 1), new Vector2(0, 0), new Vector2(100, 4), new Vector2(-104, -46));

            var price = UIKit.Text(bg.transform, "Price", product.price + " кр.", 26, new Color(1f, 0.84f, 0.3f), TextAnchor.LowerRight, FontStyle.Bold);
            price.rectTransform.Place(new Vector2(0.62f, 0), new Vector2(0.77f, 1), new Vector2(1, 0.5f), new Vector2(0, -8), new Vector2(0, -40));

            var status = UIKit.Text(bg.transform, "Status", "", 16, theme.TextDim, TextAnchor.LowerCenter);
            status.rectTransform.Place(new Vector2(0.79f, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-6, 2), new Vector2(-12, 22));

            var row = new Row { Product = product, Status = status };
            row.Buy = UIKit.Button(bg.transform, "Купить", theme, () => OnBuy(row), 22);
            ((RectTransform)row.Buy.transform).Place(new Vector2(0.79f, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6, 8), new Vector2(-24, 48));
            return row;
        }

        private void OnBuy(Row row)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            _cooldownUntil = Time.unscaledTime + 0.35f;

            var gm = GameManager.Instance;
            var result = gm.Shop.TryPurchase(row.Product);
            switch (result)
            {
                case PurchaseResult.Success:
                    gm.Audio.Play2D(Sfx.Coin, 0.8f);
                    gm.UI.Notify($"Куплено: {row.Product.displayName} (−{row.Product.price} кр.)", NotificationKind.Success);
                    break;
                case PurchaseResult.NotEnoughCredits:
                    gm.UI.Notify("Недостаточно кредитов", NotificationKind.Error);
                    break;
                case PurchaseResult.AlreadyOwned:
                    gm.UI.Notify("Предмет уже есть (лимит)", NotificationKind.Warning);
                    break;
                default:
                    gm.UI.Notify("Покупка невозможна", NotificationKind.Error);
                    break;
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            var gm = GameManager.Instance;
            _balance.text = "Баланс: " + gm.Currency.Credits.ToString("N0") + " кр.";
            foreach (var row in _rows)
            {
                var check = gm.Shop.CanPurchase(row.Product);
                int owned = row.Product.item != null ? gm.Inventory.GetCount(row.Product.item.id) : 0;
                bool unique = row.Product.item != null && row.Product.item.IsUnique;
                row.Buy.interactable = check == PurchaseResult.Success;
                UIKit.SetButtonLabel(row.Buy, check == PurchaseResult.AlreadyOwned && unique ? "Куплено" : "Купить");
                row.Status.text = check == PurchaseResult.NotEnoughCredits ? "Не хватает средств"
                    : unique ? (owned > 0 ? "В инвентаре" : "")
                    : $"В инвентаре: {owned}/{row.Product.item.maxStack}";
            }
        }

        private static string Glyph(ItemType type, ItemData item)
        {
            if (item is WeaponData w)
                return w.weaponType switch { WeaponType.Pistol => "PST", WeaponType.SniperRifle => "SNP", _ => "AR" };
            return type switch { ItemType.Equipment => "ARM", ItemType.Consumable => "+", _ => "*" };
        }

        private static string TypeLabel(ItemType type, ItemData item)
        {
            if (item is WeaponData w)
                return w.weaponType switch { WeaponType.Pistol => "Оружие GGO · пистолет", WeaponType.SniperRifle => "Оружие GGO · снайперская", _ => "Оружие GGO · автомат" };
            return type switch
            {
                ItemType.Equipment => "Экипировка GGO",
                ItemType.Consumable => "Расходник",
                ItemType.Cosmetic => "Косметика",
                _ => ""
            };
        }

        private static string StatsLine(ItemData item)
        {
            if (item is WeaponData w)
                return $"\nУрон {w.damage:0} · {w.fireRate:0.#} выстр/с · магазин {w.magazineSize} · дальность {w.range:0} м";
            if (item is EquipmentData e)
                return $"\n+{e.bonusMaxHealth:0} HP · поглощение {e.damageReduction * 100:0}% урона";
            return "";
        }
    }
}
