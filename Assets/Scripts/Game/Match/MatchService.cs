using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Game.Network;
using NetworkPlayer = Game.Network.NetworkPlayer;
using Game.Player;
using System;
using System.Linq;

namespace Game.Match
{
    public class MatchPlayer
    {
        public int playerId;
        public int connectionId;
        public bool isHost;
        public FPSPlayerController playerController;
        public NetworkConnection connection;
    }
    public class MatchService : NetworkBehaviour
    {
        [SerializeField][SyncVar] private int maxRoundsNumber = 5;
        [SerializeField][SyncVar] private float maxRoundSeconds = 10f;
        [Space]
        [SerializeField][SyncVar] private int scoreByHit = 1;

        private GameNetworkRoomManager _roomManager;

        [SyncVar] private int _roundNumber = 0;
        [SyncVar] private float _roundTimer = 0;
        [SyncVar] private bool _isRoundTimerPaused = false;
        [SyncVar] private bool _isMatchFinished = false; 

        private Dictionary<int, MatchPlayer> _matchPlayers = new Dictionary<int, MatchPlayer>();
        private SyncDictionary<int, int> _playerScores = new SyncDictionary<int, int>();

        public event Action<float, float> OnClientRoundTimerChanged;
        public event Action<int, int> OnClientRoundChanged;
        public event Action<int, int> OnClientPlayerScoreChanged;
        public event Action<int, int> OnClientScoreResult;
        public event Action OnClientScoreResultClosed;

        public int MaxRounds
        {
            get => maxRoundsNumber;
        }

        public int Round
        {
            get => _roundNumber;
        }

        public override void OnStartServer()
        {
            if(!TryGetNetworkRoomManager(out var roomManager))
            {
                return;
            }

            _roomManager = roomManager;

            _roomManager.OnPlayerConnected += PlayerConnected;
            _roomManager.OnPlayerDisconnect += PlayerDisconnect;

            ResetMatch();
        }

        public override void OnStopServer()
        {
            if(_roomManager == null)
            {
                return;
            }

            _roomManager.OnPlayerConnected -= PlayerConnected;
            _roomManager.OnPlayerDisconnect -= PlayerDisconnect;
        }

        private bool TryGetNetworkRoomManager(out GameNetworkRoomManager networkRoomManager)
        {
            var instance = NetworkManager.singleton;
            if(instance is GameNetworkRoomManager gameRoomManager)
            {
                networkRoomManager = gameRoomManager;
                return true;
            }

            networkRoomManager = null;
            return false;
        }

        private void PlayerConnected(NetworkPlayer networkPlayer)
        {
            var connectionId = networkPlayer.connection.connectionId;
            if (_matchPlayers.ContainsKey(connectionId))
            {
                return;
            }

            var playerGameObject = networkPlayer.roomPlayer;

            if (!playerGameObject.TryGetComponent<FPSPlayerController>(out var playerController))
            {
                return;
            }

            var playerId = _matchPlayers.Count;

            var matchPlayer = new MatchPlayer
            {
                connectionId = connectionId,
                isHost = networkPlayer.isHost,
                playerController = playerController,
                playerId = playerId,
                connection = networkPlayer.connection
            };

            _matchPlayers.Add(connectionId, matchPlayer);
            _playerScores.Add(playerId, 0);

            PlayerScoreChanged(playerId, 0);
            RpcCloseScoreResults();

            playerController.OnPlayerHit += PlayerHit;
        }

        private void PlayerDisconnect(NetworkPlayer networkPlayer)
        {
            var connectionId = networkPlayer.connection.connectionId;
            if (!_matchPlayers.TryGetValue(connectionId, out var matchPlayer))
            {
                return;
            }

            var playerController = matchPlayer.playerController;
            playerController.OnPlayerHit -= PlayerHit;

            if (_playerScores.ContainsKey(matchPlayer.playerId))
            {
                _playerScores.Remove(matchPlayer.playerId);
            }

            _matchPlayers.Remove(connectionId);
        }

        private void ResetMatch()
        {
            SetIsPlayersInputEnabled(false);

            StartRound(0);
            _isMatchFinished = false;
        }

        private void FinishMatch()
        {
            _isMatchFinished = true;
            SetIsPlayersInputEnabled(false);

            ShowScoreResults();
        }

        private void RespawnPlayers()
        {
            foreach (var player in _matchPlayers.Values)
            {
                var playerController = player.playerController;
                playerController.RpcRespawnPlayer();
            }

            Debug.Log("Respawning players");
        }

        private void SetIsPlayersInputEnabled(bool isPlayersInputEnabled)
        {
            foreach (var player in _matchPlayers.Values)
            {
                var playerController = player.playerController;
                playerController.RpcSetIsPlayerInputEnabled(isPlayersInputEnabled);
            }
        }

        private bool TryGetMatchPlayerByController(FPSPlayerController playerController, out MatchPlayer matchPlayer)
        {
            foreach (var player in _matchPlayers.Values)
            {
                if(player.playerController == playerController)
                {
                    matchPlayer = player;
                    return true;
                }
            }

            matchPlayer = null;
            return false;
        }

        private bool TryGetMatchPlayerByPlayerId(int playerId, out MatchPlayer matchPlayer)
        {
            foreach (var player in _matchPlayers.Values)
            {
                if (player.playerId == playerId)
                {
                    matchPlayer = player;
                    return true;
                }
            }

            matchPlayer = null;
            return false;
        }

        private void Update()
        {
            if (!isServer)
            {
                return;
            }

            var delta = Time.deltaTime;
            UpdateRoundTimer(delta);
        }

        private void StartRound(int roundNumber)
        {
            _roundNumber = roundNumber;
            _roundTimer = 0;

            RpcClientRoundUpdated(roundNumber, maxRoundsNumber);
            RpcClientRoundTimerUpdated(_roundTimer, maxRoundSeconds);

            RespawnPlayers();
            SetIsPlayersInputEnabled(true);
            SetIsRoundTimerPaused(false);
        }

        private void FinishRound()
        {
            SetIsRoundTimerPaused(true);
            SetIsPlayersInputEnabled(false);

            var nextRound = _roundNumber += 1;
            if (nextRound > maxRoundsNumber)
            {
                FinishMatch();
                return;
            }

            StartRound(nextRound);
        }

        [ClientRpc]
        private void RpcClientRoundUpdated(int roundNumber, int maxRoundNumber)
        {
            OnClientRoundChanged?.Invoke(roundNumber, maxRoundNumber);
        }

        #region RoundTimer

        private void SetIsRoundTimerPaused(bool isTimerPaused)
        {
            _isRoundTimerPaused = isTimerPaused;
        }

        private void UpdateRoundTimer(float delta)
        {
            if (_isRoundTimerPaused)
            {
                return;
            }

            _roundTimer += delta;
            RpcClientRoundTimerUpdated(_roundTimer, maxRoundSeconds);

            if (_roundTimer >= maxRoundSeconds)
            {
                RoundTimerFinished();
            }
        }

        private void RoundTimerFinished()
        {
            FinishRound();
        }

        [ClientRpc]
        private void RpcClientRoundTimerUpdated(float roundSeconds, float maxRoundSeconds)
        {
            OnClientRoundTimerChanged?.Invoke(roundSeconds, maxRoundSeconds);
        }

        #endregion

        private bool TryGetPlayerScore(int playerId, out int score)
        {
            return _playerScores.TryGetValue(playerId, out score);
        }

        private void SetPlayerScore(int playerId, int score)
        {
            if (!_playerScores.ContainsKey(playerId))
            {
                return;
            }

            _playerScores[playerId] = score;

            PlayerScoreChanged(playerId, score);
        }

        private void PlayerScoreChanged(int playerId, int score)
        {
            if(!TryGetMatchPlayerByPlayerId(playerId, out var matchPlayer))
            {
                return;
            }

            RpcPlayerScoreChanged(matchPlayer.connection, playerId, score);
        }

        [TargetRpc]
        private void RpcPlayerScoreChanged(NetworkConnection connection, int playerId, int score)
        {
            OnClientPlayerScoreChanged?.Invoke(playerId, score);
        }

        private void PlayerHit(FPSPlayerController playerController)
        {
            if(!TryGetMatchPlayerByController(playerController, out var matchPlayer))
            {
                return;
            }

            if (!TryGetPlayerScore(matchPlayer.playerId, out var playerScore))
            {
                return;
            }

            var newScore = playerScore + scoreByHit;
            SetPlayerScore(matchPlayer.playerId, newScore);
        }

        private void ShowScoreResults()
        {
            if(_playerScores.Count <= 0)
            {
                return;
            }

            var sortedScore = _playerScores.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            var bestScore = sortedScore.Last();

            RpcShowScoreResults(bestScore.Key, bestScore.Value);
        }

        [ClientRpc]
        private void RpcShowScoreResults(int bestPlayerId, int bestScore) 
        {
            OnClientScoreResult?.Invoke(bestPlayerId, bestScore);
        }

        [ClientRpc]
        private void RpcCloseScoreResults()
        {
            OnClientScoreResultClosed?.Invoke();
        }
    }
}
