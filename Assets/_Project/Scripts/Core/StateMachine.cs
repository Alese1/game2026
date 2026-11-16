using System;

namespace SwordArt.Core
{
    /// <summary>
    /// A single state of a <see cref="StateMachine"/>.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when the machine switches into this state.</summary>
        void Enter();

        /// <summary>Called every frame while the state is active.</summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        void Tick(float deltaTime);

        /// <summary>Called once when the machine leaves this state.</summary>
        void Exit();
    }

    /// <summary>
    /// Minimal finite state machine used by NPCs and combat bots.
    /// </summary>
    public sealed class StateMachine
    {
        /// <summary>Currently active state (may be null before the first transition).</summary>
        public IState Current { get; private set; }

        /// <summary>Seconds spent in the current state.</summary>
        public float TimeInState { get; private set; }

        /// <summary>Raised after a transition with the new state.</summary>
        public event Action<IState> StateChanged;

        /// <summary>Switches to <paramref name="next"/>. Re-entering the current state is ignored.</summary>
        public void ChangeState(IState next)
        {
            if (next == Current) return;
            Current?.Exit();
            Current = next;
            TimeInState = 0f;
            Current?.Enter();
            StateChanged?.Invoke(Current);
        }

        /// <summary>Ticks the active state.</summary>
        public void Tick(float deltaTime)
        {
            TimeInState += deltaTime;
            Current?.Tick(deltaTime);
        }
    }
}
