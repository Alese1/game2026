using UnityEngine;

namespace SwordArt.Interaction
{
    /// <summary>
    /// Anything the player can use with the interact key (E).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Prompt text, e.g. "E — Войти".</summary>
        string Prompt { get; }

        /// <summary>Whether interaction is currently possible. Prompts are hidden otherwise.</summary>
        bool CanInteract(Interactor interactor);

        /// <summary>Executes the interaction.</summary>
        void Interact(Interactor interactor);

        /// <summary>Visual focus feedback when the player aims at the object.</summary>
        void SetFocused(bool focused);
    }

    /// <summary>
    /// Convenience base class: prompt text, enabled check and highlight handling.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] protected string promptText = "Взаимодействовать";

        private InteractableHighlight _highlight;

        public virtual string Prompt => "E — " + promptText;

        public virtual bool CanInteract(Interactor interactor) => isActiveAndEnabled;

        public abstract void Interact(Interactor interactor);

        public virtual void SetFocused(bool focused)
        {
            if (_highlight == null && !TryGetComponent(out _highlight)) _highlight = gameObject.AddComponent<InteractableHighlight>();
            _highlight.SetHighlighted(focused);
        }

        /// <summary>Sets the prompt verb at runtime / from editor builders.</summary>
        public void SetPrompt(string text) => promptText = text;
    }
}
