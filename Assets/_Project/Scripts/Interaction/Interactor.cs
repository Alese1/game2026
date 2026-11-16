using SwordArt.Audio;
using SwordArt.Core;
using UnityEngine;

namespace SwordArt.Interaction
{
    /// <summary>
    /// Finds the interactable under the screen centre (camera raycast) within reach of the
    /// player, shows the context prompt and triggers the interaction on E.
    /// Falls back to the best-facing interactable right in front of the player so third-person
    /// camera offsets never make objects hard to use.
    /// </summary>
    public sealed class Interactor : MonoBehaviour
    {
        [Tooltip("Max distance from the reach origin to the interaction point.")]
        [SerializeField] private float reach = 2.6f;
        [Tooltip("Camera ray length (must exceed camera distance + reach).")]
        [SerializeField] private float rayLength = 12f;
        [Tooltip("Point the reach is measured from (chest). Defaults to this transform + 1m.")]
        [SerializeField] private Transform reachOrigin;
        [SerializeField] private LayerMask mask = ~0;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Collider[] _overlap = new Collider[16];
        private IInteractable _current;
        private string _shownPrompt;

        /// <summary>Currently focused interactable (may be null).</summary>
        public IInteractable Current => _current;

        private Vector3 Origin => reachOrigin != null ? reachOrigin.position : transform.position + Vector3.up;

        private void OnDisable() => SetFocus(null);

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || InputGate.IsBlocked || Camera.main == null)
            {
                SetFocus(null);
                return;
            }

            SetFocus(FindTarget());

            if (_current != null)
            {
                string prompt = _current.Prompt;
                if (prompt != _shownPrompt)
                {
                    _shownPrompt = prompt;
                    gm.UI.Prompt.Show(prompt);
                }
                if (gm.Input.Interact.WasPressedThisFrame())
                {
                    var target = _current;
                    gm.UI.Prompt.Pulse();
                    gm.Audio.Play2D(Sfx.Interact, 0.5f);
                    if (target is Component c && c.TryGetComponent<InteractableHighlight>(out var h)) h.Flash();
                    target.Interact(this);
                }
            }
        }

        private IInteractable FindTarget()
        {
            var cam = Camera.main;
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.RaycastNonAlloc(ray, _hits, rayLength, mask, QueryTriggerInteraction.Collide);
            System.Array.Sort(_hits, 0, count, HitComparer.Instance);
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable == null)
                {
                    if (hit.collider.isTrigger) continue;
                    break;
                }
                if (Vector3.Distance(Origin, hit.point) <= reach && interactable.CanInteract(this)) return interactable;
                break;
            }

            // Fallback: something right in front of the player that the camera roughly faces.
            int n = Physics.OverlapSphereNonAlloc(Origin + transform.forward * 0.8f, 1.1f, _overlap, mask, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestScore = 0.5f;
            Vector3 camFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            for (int i = 0; i < n; i++)
            {
                var col = _overlap[i];
                if (col.transform.IsChildOf(transform)) continue;
                var interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract(this)) continue;
                Vector3 to = col.ClosestPoint(Origin) - Origin;
                if (to.magnitude > reach) continue;
                to.y = 0f;
                float score = to.sqrMagnitude < 0.01f ? 1f : Vector3.Dot(camFwd, to.normalized);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = interactable;
                }
            }
            return best;
        }

        private void SetFocus(IInteractable target)
        {
            if (ReferenceEquals(target, _current)) return;
            if (_current != null && !(_current is Object o && o == null)) _current.SetFocused(false);
            _current = target;
            _shownPrompt = null;
            var ui = GameManager.Instance != null ? GameManager.Instance.UI : null;
            if (_current != null)
            {
                _current.SetFocused(true);
            }
            else if (ui != null) ui.Prompt.Hide();
        }

        private sealed class HitComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitComparer Instance = new HitComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
