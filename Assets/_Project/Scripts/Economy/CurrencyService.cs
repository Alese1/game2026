using System;
using SwordArt.SaveSystem;

namespace SwordArt.Economy
{
    /// <summary>Read/write access to the single credits balance shared by both worlds.</summary>
    public interface ICurrencyService
    {
        /// <summary>Current balance.</summary>
        int Credits { get; }

        /// <summary>Raised with (newBalance, delta, reason) after every change.</summary>
        event Action<int, int, string> CreditsChanged;

        /// <summary>Adds credits. Non-positive amounts are ignored.</summary>
        void Add(int amount, string reason);

        /// <summary>True if the balance covers <paramref name="amount"/>.</summary>
        bool CanAfford(int amount);

        /// <summary>Removes credits if affordable. Returns false and changes nothing otherwise.</summary>
        bool TrySpend(int amount, string reason);
    }

    /// <summary>
    /// Credits stored directly in the bound <see cref="SaveData"/> so that the balance
    /// can never diverge between scenes or between memory and the save file.
    /// </summary>
    public sealed class CurrencyService : ICurrencyService
    {
        private SaveData _data;

        public event Action<int, int, string> CreditsChanged;

        public int Credits => _data?.credits ?? 0;

        /// <summary>Points the service at a (new or loaded) save.</summary>
        public void Bind(SaveData data)
        {
            _data = data;
            CreditsChanged?.Invoke(Credits, 0, "bind");
        }

        public void Add(int amount, string reason)
        {
            if (_data == null || amount <= 0) return;
            _data.credits += amount;
            _data.progress.totalCreditsEarned += amount;
            CreditsChanged?.Invoke(_data.credits, amount, reason);
        }

        public bool CanAfford(int amount) => _data != null && amount >= 0 && _data.credits >= amount;

        public bool TrySpend(int amount, string reason)
        {
            if (!CanAfford(amount)) return false;
            if (amount == 0) return true;
            _data.credits -= amount;
            CreditsChanged?.Invoke(_data.credits, -amount, reason);
            return true;
        }
    }
}
