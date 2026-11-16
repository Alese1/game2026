using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwordArt.Core
{
    /// <summary>
    /// Tracks which systems (UI windows, loading screens, pause) currently block gameplay input,
    /// and keeps the mouse cursor lock state consistent with that.
    /// </summary>
    public static class InputGate
    {
        private static readonly HashSet<object> Blockers = new HashSet<object>();
        private static bool _gameplayWantsCursorLock;

        /// <summary>True while at least one blocker is registered.</summary>
        public static bool IsBlocked => Blockers.Count > 0;

        /// <summary>Raised whenever the blocked state may have changed.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Blockers.Clear();
            _gameplayWantsCursorLock = false;
            Changed = null;
        }

        /// <summary>Registers <paramref name="owner"/> as an input blocker.</summary>
        public static void Block(object owner)
        {
            if (owner != null && Blockers.Add(owner)) Notify();
        }

        /// <summary>Removes <paramref name="owner"/> from the blockers.</summary>
        public static void Unblock(object owner)
        {
            if (owner != null && Blockers.Remove(owner)) Notify();
        }

        /// <summary>Removes all blockers (used on scene changes).</summary>
        public static void Clear()
        {
            Blockers.Clear();
            Notify();
        }

        /// <summary>Gameplay controllers call this to request a locked cursor while no UI is open.</summary>
        public static void SetGameplayCursorLock(bool wantsLock)
        {
            _gameplayWantsCursorLock = wantsLock;
            RefreshCursor();
        }

        private static void Notify()
        {
            RefreshCursor();
            Changed?.Invoke();
        }

        private static void RefreshCursor()
        {
            bool locked = _gameplayWantsCursorLock && !IsBlocked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
