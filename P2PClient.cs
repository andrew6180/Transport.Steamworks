using System;
using System.Threading.Tasks;
using Facepunch.Steamworks;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Transport.Steamworks
{
    public class P2PClient : P2PObject
    {
        private enum SteamClientConnection
        {
            Disconnected,
            Connecting,
            Connected
        }

        private SteamClientConnection _connectionState = SteamClientConnection.Disconnected;

        /// <summary>
        ///     Current host ID. 0 if none.
        /// </summary>
        public ulong HostSteamID { get; private set; }

        /// <summary>
        ///     returns true if the client is trying to connect.
        /// </summary>
        public bool Connecting
        {
            get => _connectionState == SteamClientConnection.Connecting;
            private set
            {
                if (!value)
                {
                    Debug.LogWarning("Connecting manually set to false has no effect.");
                    return;
                }

                var state = _connectionState;
                _connectionState = SteamClientConnection.Connecting;
            }
        }

        /// <summary>
        ///     Returns true if the client is connected to a host.
        /// </summary>
        public bool Connected
        {
            get => _connectionState == SteamClientConnection.Connected;
            private set
            {
                if (!value)
                {
                    Debug.LogWarning("Connected manually set to false has no effect.");
                    return;
                }

                var state = _connectionState;
                _connectionState = SteamClientConnection.Connected;
                if (state != SteamClientConnection.Connected) OnConnected?.Invoke();
            }
        }

        /// <summary>
        ///     Returns true if ConnectionState is Disconnected. Otherwise returns false.
        /// </summary>
        public bool Disconnected
        {
            get => _connectionState == SteamClientConnection.Disconnected;
            private set
            {
                if (!value)
                {
                    Debug.LogWarning("Disconnected manually set to false has no effect.");
                    return;
                }

                var state = _connectionState;

                _connectionState = SteamClientConnection.Disconnected;

                if (state != SteamClientConnection.Disconnected) OnDisconnect?.Invoke();
            }
        }

        /// <summary>
        ///     Called when this client first connects.
        /// </summary>
        public readonly UnityEvent OnConnected = new UnityEvent();

        /// <summary>
        ///     Called when this client disconnects.
        /// </summary>
        public readonly UnityEvent OnDisconnect = new UnityEvent();

        /// <summary>
        ///     called when this client receives data.
        /// </summary>
        public readonly UnityEventByteArray OnData = new UnityEventByteArray();

        /// <summary>
        ///     Called when the client encounters an error.
        /// </summary>
        public readonly UnityEventException OnError = new UnityEventException();


        /// <summary>
        ///     Sets up event listeners and sends a RequestConnectPacket to the address.
        ///     <para>Calls OnError if anything fails.</para>
        /// </summary>
        /// <param name="address">Host SteamID. Should be castable to ulong.</param>
        /// <remarks>Ensures address is a ulong. Mirror wants a string IP but steamworks wants a steamid64</remarks>
        internal void Connect(string address)
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            if (!Disconnected)
            {
                OnError?.Invoke(new SteamConnectionException("Tried to ClientConnect while connected."));
                return;
            }

            Debug.Log("Started connection to host." + address);
            Connecting = true;
            Debug.Log("Parsing steamID");

            if (!ulong.TryParse(address, out var steamID))
            {
                OnError?.Invoke(new ArgumentException("Steam ID is not a valid ulong."));
                Disconnect();
                return;
            }

            SetupEvents();
            HostSteamID = steamID;

            // Request connection
            // Expect to receive a OnConnectionAccepted Call from this.
            Debug.Log($"Sending connection request to {HostSteamID} to host.");
            if (SendInternalP2PPacket(HostSteamID, RequestConnectPacket))
                return;

            // connection failed, probably invalid steam ID or something.
            Debug.Log("Failed SendInternalP2PPacket. Probably invalid steam ID.");
            OnError?.Invoke(new SteamConnectionException($"Failed to send p2p packet to ID: {steamID}"));
            Disconnect();
        }

        /// <summary>
        ///     Cleanup event listeners.
        ///     <para>see P2PObject.SetupEvents()</para>
        /// </summary>
        protected override void SetupEvents()
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            client.Networking.OnConnectionFailed += ConnectionFailed;
            OnConnectionAccepted += ConnectionAccepted;
            OnRequestDisconnect += RequestDisconnect;
            base.SetupEvents();
        }

        protected override void OnGameData(ulong steamID, byte[] data)
        {
            if (steamID != HostSteamID) 
                return;
            
            OnData?.Invoke(data);
        }

        /// <summary>
        ///     Called by OnRequestDisconnect dispatched by the server.
        ///     <para>Usually called due to a kick or server shutdown</para>
        /// </summary>
        private void RequestDisconnect(ulong steamID)
        {
            if (steamID != HostSteamID)
            {
                OnError?.Invoke(new SteamConnectionException($"Disconnect requested by non-host {steamID}"));
                return;
            }

            Disconnect();
        }

        /// <summary>
        ///     Called by OnConnectionAccepted dispatched by the server.
        /// </summary>
        private void ConnectionAccepted(ulong steamID)
        {
            if (steamID != HostSteamID)
            {
                OnError?.Invoke(new SteamConnectionException($"Connection Accepted received from non-host {steamID}"));
                Disconnect();
                return;
            }

            Connected = true;
            Debug.Log($"Connected to {steamID}");
        }

        /// <summary>
        ///     Called when connecting failed at a Steamworks level.
        /// </summary>
        private void ConnectionFailed(ulong steamID, Networking.SessionError error)
        {
            OnError?.Invoke(
                new SteamConnectionException($"Failed to connect to host {steamID}. Reason: {error.ToString()}"));
            Disconnect();
        }

        /// <summary>
        ///     Disconnects the player from the host.
        /// </summary>
        /// <remarks>
        ///     Attempts to send AcceptedDisconnectPacket before closing the connection.
        /// </remarks>
        public async void Disconnect()
        {
            if (Disconnected)
                return;

            if (HostSteamID > 0)
            {
                SendInternalP2PPacket(HostSteamID, AcceptedDisconnectPacket);
                await Task.Delay(100); // let packet send before disconnecting.
                CloseSession(HostSteamID);
            }

            CloseEvents();
            Debug.Log("Disconnected from host.");
            Disconnected = true;
        }

        /// <summary>
        ///     Closes the active session with HostSteamID.
        /// </summary>
        private void CloseSession(ulong steamID)
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            if (HostSteamID == 0)
            {
                Debug.LogWarning("Tried to close session with HostSteamID = 0");
                return;
            }

            client.Networking.CloseSession(HostSteamID);
            Debug.Log($"Closed session with host {HostSteamID}");

            HostSteamID = 0;
        }

        /// <summary>
        ///     Called by ITransport to send game data usually.
        /// </summary>
        public bool Send(byte[] data, int channel)
        {
            return SendP2PPacket(HostSteamID, data, channel);
        }


        /// <summary>
        ///     Cleanup event listeners.
        ///     <para>see P2PObject.CloseEvents()</para>
        /// </summary>
        protected override void CloseEvents()
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            client.Networking.OnConnectionFailed -= ConnectionFailed;
            OnRequestDisconnect -= RequestDisconnect;
            OnConnectionAccepted -= ConnectionAccepted;
            base.CloseEvents();
        }
    }
}