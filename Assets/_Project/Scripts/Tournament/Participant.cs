using System;
using SwordArt.Combat;
using UnityEngine;

namespace SwordArt.Tournament
{
    /// <summary>
    /// A tournament entrant (player or bot): name, health, kill count and placement.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class Participant : MonoBehaviour
    {
        [SerializeField] private string displayName = "Участник";
        [SerializeField] private bool isPlayer;
        [Tooltip("Chest height used for aiming and line-of-sight checks.")]
        [SerializeField] private float chestHeight = 1.3f;

        private Health _health;

        /// <summary>Raised when this participant dies: (victim, killer or null).</summary>
        public event Action<Participant, Participant> Eliminated;

        public string DisplayName => displayName;
        public bool IsPlayer => isPlayer;
        public Health Health => _health != null ? _health : _health = GetComponent<Health>();
        public bool IsAlive => Health.IsAlive;
        public int Kills { get; set; }
        public int Placement { get; set; }
        public Vector3 ChestPosition => transform.position + Vector3.up * chestHeight;

        /// <summary>Sets the identity (used by spawners).</summary>
        public void Configure(string participantName, bool player)
        {
            displayName = participantName;
            isPlayer = player;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= OnDied;
        }

        private void OnDied(DamageInfo info)
        {
            Participant killer = info.Source != null ? info.Source.GetComponentInParent<Participant>() : null;
            if (killer == this) killer = null;
            Eliminated?.Invoke(this, killer);
        }
    }
}
