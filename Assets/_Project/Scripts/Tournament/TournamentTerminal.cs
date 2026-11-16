using SwordArt.Interaction;
using UnityEngine;

namespace SwordArt.Tournament
{
    /// <summary>Lobby terminal: register for the match or log out of GGO.</summary>
    public sealed class TournamentTerminal : InteractableBase
    {
        /// <summary>What the terminal does.</summary>
        public enum Mode
        {
            Register,
            Logout
        }

        [SerializeField] private Mode mode = Mode.Register;
        [SerializeField] private TournamentManager tournament;

        /// <summary>Configures the terminal (used by the scene builder).</summary>
        public void Configure(Mode terminalMode, TournamentManager manager)
        {
            mode = terminalMode;
            tournament = manager;
            promptText = mode == Mode.Register ? "Зарегистрироваться на Bullet of Bullets" : "Выйти из GGO (log out)";
        }

        public override bool CanInteract(Interactor interactor)
        {
            if (!base.CanInteract(interactor) || tournament == null) return false;
            return mode == Mode.Logout || tournament.State == MatchState.Lobby;
        }

        public override void Interact(Interactor interactor)
        {
            if (mode == Mode.Register) tournament.RegisterForMatch();
            else tournament.Logout();
        }
    }
}
