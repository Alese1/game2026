using UnityEngine;
using UnityEngine.InputSystem;

namespace SwordArt.Core
{
    /// <summary>
    /// Wrapper around an <see cref="InputAction"/> exposing the subset of its API used by gameplay
    /// code, plus an optional simulated state for automated tests (the editor drops real device
    /// input while it is in the background). Without simulation it behaves exactly like the action.
    /// </summary>
    public sealed class GameAction
    {
        private bool _simHeld;
        private bool _simPendingPress;
        private int _simPressFrame = -1;
        private Vector2 _simValue;
        private bool _simHasValue;

        /// <summary>The underlying Input System action.</summary>
        public InputAction Action { get; }

        public GameAction(InputAction action) => Action = action;

        /// <summary>True on the frame the button went down.</summary>
        public bool WasPressedThisFrame()
        {
            if (_simPendingPress)
            {
                _simPendingPress = false;
                _simPressFrame = Time.frameCount;
            }
            return _simPressFrame == Time.frameCount || Action.WasPressedThisFrame();
        }

        /// <summary>True while the button is held.</summary>
        public bool IsPressed() => _simHeld || Action.IsPressed();

        /// <summary>Reads the action value (Vector2 or float).</summary>
        public T ReadValue<T>() where T : struct
        {
            if (_simHasValue)
            {
                object v = typeof(T) == typeof(float) ? (object)_simValue.y : _simValue;
                if (v is T t) return t;
            }
            return Action.ReadValue<T>();
        }

        // ---- Simulation (tests) ----

        /// <summary>Simulates a single press (true for exactly one frame).</summary>
        public void SimulatePress() => _simPendingPress = true;

        /// <summary>Simulates holding the button.</summary>
        public void SimulateHold(bool held) => _simHeld = held;

        /// <summary>Simulates a value (Vector2; floats use y). Pass null to clear.</summary>
        public void SimulateValue(Vector2? value)
        {
            _simHasValue = value.HasValue;
            _simValue = value ?? Vector2.zero;
        }

        /// <summary>Clears all simulated state.</summary>
        public void ClearSimulation()
        {
            _simHeld = false;
            _simPendingPress = false;
            _simHasValue = false;
            _simPressFrame = -1;
        }
    }
}
