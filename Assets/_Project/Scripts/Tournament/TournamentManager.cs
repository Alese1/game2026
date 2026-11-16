using System;
using System.Collections;
using System.Collections.Generic;
using SwordArt.AI;
using SwordArt.Audio;
using SwordArt.Core;
using SwordArt.UI;
using UnityEngine;

namespace SwordArt.Tournament
{
    /// <summary>Match lifecycle state.</summary>
    public enum MatchState
    {
        Lobby,
        Countdown,
        InProgress,
        Finished
    }

    /// <summary>Summary of a finished match, including the credit reward breakdown.</summary>
    public sealed class MatchResult
    {
        public bool Won;
        public int Placement;
        public int Participants;
        public int Kills;
        public float Duration;
        public int ParticipationReward;
        public int KillReward;
        public int PlacementReward;
        public int Total => ParticipationReward + KillReward + PlacementReward;
        public string WinnerName;
        /// <summary>Who eliminated the player ("Зона" for zone damage), null if the player survived.</summary>
        public string KillerName;
    }

    /// <summary>
    /// Bullet of Bullets tournament: lobby → registration → bot spawning → locked countdown →
    /// battle with a shrinking safe zone → last survivor wins → rewards and save.
    /// </summary>
    public sealed class TournamentManager : MonoBehaviour
    {
        private static readonly string[] DefaultBotNames =
        {
            "Viper", "Kestrel", "Dust Devil", "Hound", "Nova", "Raze", "Wraith", "Scorch", "Mirage", "Bolt", "Talon", "Rust"
        };

        [SerializeField] private GGOPlayer player;
        [SerializeField] private SafeZone zone;
        [SerializeField] private BotController botPrefab;
        [SerializeField] private List<BotProfile> botProfiles = new List<BotProfile>();
        [SerializeField] private Transform[] botSpawnPoints;
        [SerializeField] private Transform[] playerSpawnPoints;
        [SerializeField, Range(1, 11)] private int botCount = 7;
        [SerializeField] private float countdownSeconds = 5f;

        private readonly List<Participant> _participants = new List<Participant>();
        private readonly List<BotController> _bots = new List<BotController>();
        private float _matchStart;
        private string _playerKiller;

        /// <summary>Raised when the match state changes.</summary>
        public event Action<MatchState> StateChanged;

        /// <summary>Raised on each countdown second (0 = GO).</summary>
        public event Action<int> CountdownTick;

        /// <summary>Raised when someone is eliminated: (victim, killer or null).</summary>
        public event Action<Participant, Participant> ParticipantEliminated;

        /// <summary>Raised once with the final result.</summary>
        public event Action<MatchResult> MatchFinished;

        public MatchState State { get; private set; } = MatchState.Lobby;
        public SafeZone Zone => zone;
        public GGOPlayer Player => player;
        public IReadOnlyList<Participant> Participants => _participants;
        public IReadOnlyList<BotController> Bots => _bots;
        public MatchResult Result { get; private set; }
        public float MatchTime => State == MatchState.InProgress ? Time.time - _matchStart : Result?.Duration ?? 0f;

        /// <summary>Number of participants still alive.</summary>
        public int AliveCount
        {
            get
            {
                int n = 0;
                foreach (var p in _participants)
                    if (p != null && p.IsAlive) n++;
                return n;
            }
        }

        /// <summary>Configures the manager (used by the scene builder).</summary>
        public void Configure(GGOPlayer ggoPlayer, SafeZone safeZone, BotController prefab, List<BotProfile> profiles,
            Transform[] botSpawns, Transform[] playerSpawns, int bots)
        {
            player = ggoPlayer;
            zone = safeZone;
            botPrefab = prefab;
            botProfiles = profiles;
            botSpawnPoints = botSpawns;
            playerSpawnPoints = playerSpawns;
            botCount = bots;
        }

        private void Start()
        {
            if (player != null)
            {
                _participants.Add(player.Participant);
                player.Participant.Eliminated += OnEliminated;
                player.EnterLobby();
            }
            SetState(MatchState.Lobby);
            UI.PauseMenu.LogoutRequested += Logout;
        }

        private void OnDestroy() => UI.PauseMenu.LogoutRequested -= Logout;

        /// <summary>Registers the player and starts the match sequence (lobby terminal).</summary>
        public void RegisterForMatch()
        {
            if (State != MatchState.Lobby) return;
            StartCoroutine(MatchRoutine());
        }

        private IEnumerator MatchRoutine()
        {
            SpawnBots();
            var spawn = PickPlayerSpawn();
            player.PrepareForMatch(spawn.position, spawn.rotation);
            player.SetLocked(true);
            SetState(MatchState.Countdown);

            var audio = GameManager.Instance.Audio;
            for (int i = Mathf.CeilToInt(countdownSeconds); i > 0; i--)
            {
                CountdownTick?.Invoke(i);
                audio.Play2D(Sfx.Countdown, 0.7f);
                yield return new WaitForSeconds(1f);
            }
            CountdownTick?.Invoke(0);
            audio.Play2D(Sfx.CountdownGo, 0.8f);

            player.SetLocked(false);
            player.Health.Invulnerable = false;
            _matchStart = Time.time;
            SetState(MatchState.InProgress);
            if (zone != null) zone.Begin(_participants);
            foreach (var bot in _bots) bot.Activate();
            GameManager.Instance.Data.progress.tournamentsPlayed++;
        }

        private void SpawnBots()
        {
            if (botPrefab == null || botProfiles.Count == 0 || botSpawnPoints == null || botSpawnPoints.Length == 0)
            {
                Debug.LogError("[Tournament] Bot prefab, profiles or spawn points are missing.");
                return;
            }
            var names = new List<string>(DefaultBotNames);
            var spawns = new List<Transform>(botSpawnPoints);
            for (int i = 0; i < botCount; i++)
            {
                Transform spawn;
                if (spawns.Count > 0)
                {
                    int s = UnityEngine.Random.Range(0, spawns.Count);
                    spawn = spawns[s];
                    spawns.RemoveAt(s);
                }
                else spawn = botSpawnPoints[i % botSpawnPoints.Length];

                var bot = Instantiate(botPrefab, spawn.position, spawn.rotation);
                int n = UnityEngine.Random.Range(0, names.Count);
                string botName = names[n];
                names.RemoveAt(n);
                bot.name = "Bot_" + botName;
                bot.Initialize(this, botProfiles[i % botProfiles.Count], botName);
                _bots.Add(bot);
                _participants.Add(bot.Self);
                bot.Self.Eliminated += OnEliminated;
            }
        }

        private Transform PickPlayerSpawn()
        {
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0) return player.transform;
            Transform best = playerSpawnPoints[0];
            float bestScore = -1f;
            foreach (var sp in playerSpawnPoints)
            {
                float nearest = float.MaxValue;
                foreach (var b in _bots) nearest = Mathf.Min(nearest, Vector3.Distance(sp.position, b.transform.position));
                float score = nearest + UnityEngine.Random.Range(0f, 10f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = sp;
                }
            }
            return best;
        }

        private void OnEliminated(Participant victim, Participant killer)
        {
            if (State != MatchState.InProgress) return;
            if (killer != null) killer.Kills++;
            victim.Placement = AliveCount + 1;
            ParticipantEliminated?.Invoke(victim, killer);

            if (victim.IsPlayer)
            {
                _playerKiller = killer != null ? killer.DisplayName : "Зона";
                StartCoroutine(EndAfterDelay(false, 2.5f));
                return;
            }
            if (AliveCount <= 1)
            {
                Participant winner = null;
                foreach (var p in _participants)
                    if (p != null && p.IsAlive) winner = p;
                if (winner != null) winner.Placement = 1;
                StartCoroutine(EndAfterDelay(winner != null && winner.IsPlayer, 1.5f));
            }
        }

        private IEnumerator EndAfterDelay(bool won, float delay)
        {
            SetState(MatchState.Finished);
            if (zone != null) zone.Stop();
            foreach (var bot in _bots)
                if (bot != null) bot.Deactivate();
            Result = BuildResult(won);
            ApplyRewards(Result);
            yield return new WaitForSeconds(delay);
            MatchFinished?.Invoke(Result);
        }

        private MatchResult BuildResult(bool won)
        {
            var economy = GameManager.Instance.Config.economy;
            var me = player.Participant;
            int placement = won ? 1 : Mathf.Max(1, me.Placement);
            Participant winner = null;
            foreach (var p in _participants)
                if (p != null && p.IsAlive) winner = p;
            return new MatchResult
            {
                Won = won,
                Placement = placement,
                Participants = _participants.Count,
                Kills = me.Kills,
                Duration = Time.time - _matchStart,
                ParticipationReward = economy.participationReward,
                KillReward = me.Kills * economy.rewardPerKill,
                PlacementReward = won ? economy.winReward : placement <= 3 ? economy.podiumReward : 0,
                WinnerName = won ? me.DisplayName : winner != null ? winner.DisplayName : "—",
                KillerName = _playerKiller
            };
        }

        private static void ApplyRewards(MatchResult result)
        {
            var gm = GameManager.Instance;
            var progress = gm.Data.progress;
            progress.totalKills += result.Kills;
            if (result.Won) progress.tournamentsWon++;
            if (progress.bestPlacement == 0 || result.Placement < progress.bestPlacement) progress.bestPlacement = result.Placement;
            gm.Currency.Add(result.Total, "tournament");
            gm.UI.Notify($"Награда за турнир: +{result.Total} кр.", NotificationKind.Credits);
            gm.SaveGame("tournament");
        }

        /// <summary>Reloads the GGO scene to start over from the lobby.</summary>
        public void ReturnToLobby()
        {
            var gm = GameManager.Instance;
            gm.SaveGame("lobby");
            gm.SceneLoader.LoadScene(SceneNames.GGO, SpawnIds.GGOLobby, "Лобби Bullet of Bullets");
        }

        /// <summary>Leaves GGO and wakes up in the apartment. An unfinished match counts as a forfeit.</summary>
        public void Logout()
        {
            var gm = GameManager.Instance;
            if (State == MatchState.InProgress || State == MatchState.Countdown)
                gm.UI.Notify("Матч покинут — награда не начислена", NotificationKind.Warning);
            gm.SaveGame("logout");
            gm.SceneLoader.LoadScene(SceneNames.Apartment, SpawnIds.ApartmentFullDive, "Выход из FullDive");
        }

        private void SetState(MatchState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
