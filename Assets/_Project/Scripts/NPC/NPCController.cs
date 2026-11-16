using SwordArt.Core;
using SwordArt.Interaction;
using SwordArt.Player;
using UnityEngine;
using UnityEngine.AI;

namespace SwordArt.NPC
{
    /// <summary>
    /// City NPC with a small state machine (Idle → Walking along waypoints → Idle; Interacting while
    /// talking to the player). Implements <see cref="IInteractable"/> to start a dialogue.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NPCController : InteractableBase
    {
        [SerializeField] private NPCData data;
        [Tooltip("Optional patrol route. Without waypoints the NPC stays idle.")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private Vector2 idleTimeRange = new Vector2(2f, 6f);
        [SerializeField] private ProceduralCharacterAnimator characterAnimator;

        private readonly StateMachine _machine = new StateMachine();
        private NavMeshAgent _agent;
        private IdleState _idle;
        private WalkingState _walking;
        private InteractingState _interacting;
        private int _waypointIndex;
        private int _dialogueIndex;

        public NPCData Data => data;
        public string CurrentStateName => _machine.Current?.GetType().Name ?? "None";

        public override string Prompt => "E — Поговорить" + (data != null ? $" ({data.displayName})" : "");

        /// <summary>Configures the NPC (used by scene builders).</summary>
        public void Configure(NPCData npcData, Transform[] route, ProceduralCharacterAnimator anim)
        {
            data = npcData;
            waypoints = route;
            characterAnimator = anim;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _idle = new IdleState(this);
            _walking = new WalkingState(this);
            _interacting = new InteractingState(this);
        }

        private void Start()
        {
            if (data != null) _agent.speed = data.walkSpeed;
            _agent.angularSpeed = 360f;
            _agent.acceleration = 6f;
            _agent.stoppingDistance = 0.2f;
            _machine.ChangeState(_idle);
        }

        private void Update() => _machine.Tick(Time.deltaTime);

        public override bool CanInteract(Interactor interactor) => base.CanInteract(interactor) && _machine.Current != _interacting;

        public override void Interact(Interactor interactor)
        {
            _interacting.Partner = interactor.transform;
            _machine.ChangeState(_interacting);
            var lines = data != null && data.dialogues.Count > 0 ? data.dialogues[_dialogueIndex % data.dialogues.Count].lines : null;
            _dialogueIndex++;
            string speaker = data != null ? data.displayName : name;
            GameManager.Instance.UI.Dialogue.Show(speaker, lines, () => _machine.ChangeState(_idle));
        }

        private bool AgentReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;

        private sealed class IdleState : IState
        {
            private readonly NPCController _npc;
            private float _duration;
            public IdleState(NPCController npc) => _npc = npc;

            public void Enter()
            {
                _duration = Random.Range(_npc.idleTimeRange.x, _npc.idleTimeRange.y);
                if (_npc.AgentReady) _npc._agent.isStopped = true;
            }

            public void Tick(float dt)
            {
                if (_npc.waypoints != null && _npc.waypoints.Length > 0 && _npc._machine.TimeInState >= _duration)
                    _npc._machine.ChangeState(_npc._walking);
            }

            public void Exit() { }
        }

        private sealed class WalkingState : IState
        {
            private readonly NPCController _npc;
            public WalkingState(NPCController npc) => _npc = npc;

            public void Enter()
            {
                if (!_npc.AgentReady)
                {
                    _npc._machine.ChangeState(_npc._idle);
                    return;
                }
                var target = _npc.waypoints[_npc._waypointIndex % _npc.waypoints.Length];
                _npc._waypointIndex++;
                _npc._agent.isStopped = false;
                _npc._agent.SetDestination(target.position);
            }

            public void Tick(float dt)
            {
                var agent = _npc._agent;
                if (!_npc.AgentReady) return;
                bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
                if (arrived || _npc._machine.TimeInState > 40f) _npc._machine.ChangeState(_npc._idle);
            }

            public void Exit() { }
        }

        private sealed class InteractingState : IState
        {
            private readonly NPCController _npc;
            public Transform Partner;
            public InteractingState(NPCController npc) => _npc = npc;

            public void Enter()
            {
                if (_npc.AgentReady)
                {
                    _npc._agent.isStopped = true;
                    _npc._agent.velocity = Vector3.zero;
                }
            }

            public void Tick(float dt)
            {
                if (Partner == null) return;
                Vector3 dir = Partner.position - _npc.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) return;
                var look = Quaternion.LookRotation(dir);
                _npc.transform.rotation = Quaternion.RotateTowards(_npc.transform.rotation, look, 360f * dt);
            }

            public void Exit() { }
        }
    }
}
