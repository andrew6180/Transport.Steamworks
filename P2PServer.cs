using System;
using System.Data;
using System.Threading.Tasks;
using Facepunch.Steamworks;
using Mirror;
using Transport.Steamworks.Helpers;
using UnityEngine;

namespace Transport.Steamworks
{
    public class P2PServer : P2PObject
    {
        /// <summary>
        ///     Timeout after sending a disconnect message before dropping the connection.
        ///     Connection will be dropped sooner if the client responds to the disconnect packet.
        /// </summary>
        private const int DISCONNECT_MESSAGE_TIMEOUT_MS = 500;

        public readonly BiDictionary<ulong, int> ConnectionMap = new BiDictionary<ulong, int>();

        /// <summary>
        ///     Last connID mapped by the server.
        /// </summary>
        private int _lastConnectionID = -1;

        /// <summary>
        ///     Current state of the server. See <seealso cref="SteamServerState" />
        /// </summary>
        private SteamServerState _serverState = SteamServerState.Offline;

        /// <summary>
        ///     Returns if the server is listening for connections.
        /// </summary>
        public bool Listening
        {
            get => _serverState == SteamServerState.Listening;
            private set
            {
                if (!value)
                {
                    Debug.LogWarning("Listening manually set to false has no effect.");
                    return;
                }

                _serverState = SteamServerState.Listening;
            }
        }

        /// <summary>
        ///     Returns if the server is currently offline.
        /// </summary>
        public bool Offline
        {
            get => _serverState == SteamServerState.Offline;
            private set
            {
                if (!value)
                {
                    Debug.LogWarning("Offline manually set to false has no effect.");
                    return;
                }

                _serverState = SteamServerState.Offline;
            }
        }

        /// <summary>
        ///     Called when data is received by the server.
        /// </summary>
        public readonly UnityEventIntByteArray OnData = new UnityEventIntByteArray();

        /// <summary>
        ///     Called when a client connects.
        /// </summary>
        public readonly UnityEventInt OnConnect = new UnityEventInt();

        /// <summary>
        ///     Called when the server has an error.
        /// </summary>
        public UnityEventIntException OnError = new UnityEventIntException();

        /// <summary>
        ///     Called when a player is disconnected.
        /// </summary>
        public UnityEventInt OnDisconnect = new UnityEventInt();

        /// <summary>
        ///     Sets up events and maps the local clients steamID as the first connection ID.
        ///     <para>Sets Listening = true</para>
        /// </summary>
        public void Start()
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            ConnectionMap.Add(client.SteamId, ++_lastConnectionID);
            SetupEvents();
            Listening = true;
        }

        /// <summary>
        ///     Cleanup event listeners.
        ///     <para>see P2PObject.SetupEvents()</para>
        /// </summary>
        protected override void SetupEvents()
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            client.Networking.OnIncomingConnection += IncomingConnection;
            OnRequestConnect += RequestConnect;
            OnDisconnectAccepted += DisconnectAccepted;
            base.SetupEvents();
        }

        /// <summary>
        ///     Closes the connection with a user.
        /// </summary>
        private void DisconnectAccepted(ulong steamID)
        {
            Debug.Log($"Disconnecting player: {steamID}");
            CloseSession(steamID);
        }

        /// <summary>
        ///     After connection is accepted, send back an accepted packet so the client can setup.
        /// </summary>
        private void RequestConnect(ulong steamID)
        {
            Debug.Log($"Sending Accepted Connection to {steamID}");
            SendInternalP2PPacket(steamID, AcceptedConnectionPacket);

            if (!ConnectionMap.TryGetByFirst(steamID, out var connID))
            {
                OnError?.Invoke(connID, new MissingPrimaryKeyException("Steam ID was not mapped before connecting."));
                Disconnect(steamID);
            }

            OnConnect?.Invoke(connID);
        }

        /// <summary>
        ///     Removes the steamID from the connection map and closes their session.
        /// </summary>
        public void CloseSession(ulong steamID)
        {
            if (ConnectionMap.TryGetByFirst(steamID, out var connID))
            {
                OnDisconnect?.Invoke(connID);
            }
            ConnectionMap.TryRemoveByFirst(steamID);
            Client.Instance.Networking.CloseSession(steamID);
        }

        protected override void OnGameData(ulong steamID, byte[] data)
        {
            if (!ConnectionMap.TryGetByFirst(steamID, out var connID))
                return;

            OnData?.Invoke(connID, data);
        }

        /// <summary>
        ///     Called before the first packet from a user is processed.
        /// </summary>
        /// <param name="steamID"></param>
        /// <returns>should we accept this connection</returns>
        private bool IncomingConnection(ulong steamID)
        {
            Debug.Log($"Received connection request from {steamID}");
            if (!Listening)
                return false;

            ConnectionMap.Add(steamID, ++_lastConnectionID);

            Debug.Log($"Mapped Connection: [{steamID}]<->[{_lastConnectionID}]");
            return true;
        }


        /// <summary>
        ///     Called from SteamworksTransport. Converts connectionID to steamID and sends a p2ppacket.
        /// </summary>
        public bool Send(int connectionID, int channelID, byte[] data)
        {
            if (!ConnectionMap.TryGetBySecond(connectionID, out var steamID))
            {
                OnError?.Invoke(connectionID, new MissingReferenceException($"Tried to access an unmapped connection. {connectionID}"));
                return false;
            }

            return SendP2PPacket(steamID, data, channelID);
        }

        /// <summary>
        ///     Kicks a client from the server with connectionID
        /// </summary>
        public bool Disconnect(int connectionID)
        {
            if (ConnectionMap.TryGetBySecond(connectionID, out var steamID))
                return Disconnect(steamID, connectionID);

            OnError?.Invoke(connectionID,
                new MissingReferenceException($"Tried to disconnect an unmapped connection. {connectionID}"));
            return false;
        }

        /// <summary>
        ///     Sends a disconnect message to a client with steamID. Calls <see cref="WaitForDisconnect" />
        /// </summary>
        private bool Disconnect(ulong steamID, int connectionID = 0)
        {
            if (steamID == Client.Instance.SteamId)
            {
                Debug.Log("Disconnecting local client.");
                return true;
            }

            if (!SendInternalP2PPacket(steamID, DisconnectPacket))
            {
                OnError?.Invoke(connectionID,
                    new SteamConnectionException("Could not send disconnect packet to client."));
                return false;
            }

            WaitForDisconnect(steamID);
            return true;
        }

        /// <summary>
        ///     Uses Task.Delay to wait for a timeout before closing a session with a player.
        /// </summary>
        /// <remarks>
        ///     Clients will usually respond with a "AcceptedDisconnect" packet. This is if the connection is interrupted.
        /// </remarks>
        private async void WaitForDisconnect(ulong steamID)
        {
            await Task.Delay(DISCONNECT_MESSAGE_TIMEOUT_MS);

            if (ConnectionMap.TryGetByFirst(steamID, out var conID)) OnDisconnect?.Invoke(conID);
            CloseSession(steamID);
        }

        public string GetConnectionInfo(int connectionId)
        {
            var exists = ConnectionMap.TryGetBySecond(connectionId, out var steamID);
            return exists ? steamID.ToString() : "MISSING_STEAM_ID";
        }

        /// <summary>
        ///     Calls CloseEvents() and cleans up connection mapping. Sets server status to offline.
        /// </summary>
        public void Stop()
        {
            CloseEvents();
            ConnectionMap.Clear();
            Offline = true;
            _lastConnectionID = -1;
        }

        /// <summary>
        ///     Cleanup event listeners.
        ///     <para>see P2PObject.CloseEvents()</para>
        /// </summary>
        protected override void CloseEvents()
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            client.Networking.OnIncomingConnection -= IncomingConnection;
            base.CloseEvents();
        }

        private enum SteamServerState
        {
            Offline,
            Listening
        }
    }
}