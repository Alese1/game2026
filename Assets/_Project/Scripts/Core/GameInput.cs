using System;
using UnityEngine.InputSystem;

namespace SwordArt.Core
{
    /// <summary>
    /// All gameplay input actions, defined in code with the Input System.
    /// One instance is owned by <see cref="GameManager"/> and shared by every controller.
    /// </summary>
    public sealed class GameInput : IDisposable
    {
        private readonly InputActionMap _map;

        public GameAction Move { get; }
        public GameAction Look { get; }
        public GameAction Sprint { get; }
        public GameAction Jump { get; }
        public GameAction Interact { get; }
        public GameAction Pause { get; }
        public GameAction Fire { get; }
        public GameAction Aim { get; }
        public GameAction Reload { get; }
        public GameAction Stats { get; }
        public GameAction Weapon1 { get; }
        public GameAction Weapon2 { get; }
        public GameAction Weapon3 { get; }
        public GameAction ScrollWeapon { get; }
        public GameAction UseMedkit { get; }

        /// <summary>Creates and enables the gameplay action map.</summary>
        public GameInput()
        {
            _map = new InputActionMap("Gameplay");

            var move = _map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            Move = new GameAction(move);

            var look = _map.AddAction("Look", InputActionType.PassThrough, "<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=12,y=12)");
            Look = new GameAction(look);

            Sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            Jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            Fire = Button("Fire", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            Aim = Button("Aim", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            Reload = Button("Reload", "<Keyboard>/r", "<Gamepad>/buttonNorth");
            Stats = Button("Stats", "<Keyboard>/tab", "<Gamepad>/select");
            Weapon1 = Button("Weapon1", "<Keyboard>/1", null);
            Weapon2 = Button("Weapon2", "<Keyboard>/2", null);
            Weapon3 = Button("Weapon3", "<Keyboard>/3", null);
            UseMedkit = Button("UseMedkit", "<Keyboard>/h", "<Gamepad>/dpad/down");
            ScrollWeapon = new GameAction(_map.AddAction("ScrollWeapon", InputActionType.PassThrough, "<Mouse>/scroll/y"));

            _map.Enable();
        }

        private GameAction Button(string name, string binding, string gamepadBinding)
        {
            var action = _map.AddAction(name, InputActionType.Button, binding);
            if (!string.IsNullOrEmpty(gamepadBinding)) action.AddBinding(gamepadBinding);
            return new GameAction(action);
        }

        /// <summary>All actions (for clearing simulated input in tests).</summary>
        public System.Collections.Generic.IEnumerable<GameAction> All => new[]
        {
            Move, Look, Sprint, Jump, Interact, Pause, Fire, Aim, Reload, Stats, Weapon1, Weapon2, Weapon3, ScrollWeapon, UseMedkit
        };

        /// <summary>Clears simulated input on every action.</summary>
        public void ClearSimulation()
        {
            foreach (var a in All) a.ClearSimulation();
        }

        /// <summary>Disposes the underlying actions.</summary>
        public void Dispose()
        {
            _map.Disable();
            _map.Dispose();
        }
    }
}
