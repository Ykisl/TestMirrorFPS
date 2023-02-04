using Game.Player;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Network
{
    public class NetworkPlayer
    {
        public NetworkConnection connection;
        public GameObject roomPlayer;
        public bool isHost;
    }

    public class GameNetworkRoomManager : NetworkRoomManager
    {
        public event Action<NetworkPlayer> OnPlayerConnected;
        public event Action<NetworkPlayer> OnPlayerDisconnect;

        private List<NetworkPlayer> _networkPlayers = new List<NetworkPlayer>();

        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient connection, GameObject roomPlayer, GameObject gamePlayer)
        {
            PlayerConnected(connection, gamePlayer);
            return base.OnRoomServerSceneLoadedForPlayer(connection, roomPlayer, gamePlayer);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            PlayerDisconnected(connection);
            base.OnServerDisconnect(connection);
        }

        public override void OnClientDisconnect()
        {
            if (NetworkClient.activeHost)
            {
                var connection = NetworkClient.connection;
                PlayerDisconnected(connection);
            }

            base.OnClientDisconnect();
        }

        public bool TryGetPlayerByConnection(NetworkConnection connection, out NetworkPlayer player)
        {
            player = null;

            foreach (var networkPlayer in _networkPlayers)
            {
                if (networkPlayer.connection.connectionId == connection.connectionId)
                {
                    player = networkPlayer;
                    return true;
                }
            }

            return false;
        }

        private void PlayerConnected(NetworkConnection connection, GameObject roomPlayer)
        {
            if(TryGetPlayerByConnection(connection, out var player))
            {
                return;
            }

            var isHost = NetworkClient.activeHost && NetworkClient.connection.connectionId == connection.connectionId;

            var networkPlayer = new NetworkPlayer
            {
                connection = connection,
                roomPlayer = roomPlayer,
                isHost = isHost
            };

            _networkPlayers.Add(networkPlayer);

            OnPlayerConnected?.Invoke(networkPlayer);
        }

        private void PlayerDisconnected(NetworkConnection connection)
        {
            if (!TryGetPlayerByConnection(connection, out var player))
            {
                return;
            }

            _networkPlayers.Remove(player);

            OnPlayerDisconnect?.Invoke(player);
        }
    }
}
