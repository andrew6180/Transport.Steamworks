using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Transport.Steamworks.Helpers;
using UnityEditorInternal;
using UnityEngine;

namespace Transport.Steamworks
{
    /// <summary>
    /// Reliable = 0
    /// Unreliable = 1
    /// UnreliableNoDelay = 2
    /// Internal = 3
    /// </summary>
    public enum SteamPacketChannel : int
    {
        Reliable = Channels.DefaultReliable,
        Unreliable = Channels.DefaultUnreliable,   
        UnreliableNoDelay = 2,
        Internal = 3
    }
    
    public class SteamworksTransport : Mirror.Transport
    {
        [Tooltip("How long to wait without receiving a packet before disconnect.\nIf host, this is per client.")]
        public float TimeoutSeconds = 30;
        
        private readonly P2PClient _client;
        private readonly P2PServer _server;

        private ulong _hostSteamID;

        private Dictionary<ulong, TimeSince> _timeouts;
        public SteamworksTransport()
        {
            _client = new P2PClient();
            _server = new P2PServer();

            _client.OnConnected.AddListener(() => OnClientConnected?.Invoke());
            _client.OnData.AddListener(bytes => { OnClientDataReceived?.Invoke(bytes); });
            _client.OnError.AddListener(exception => { OnClientError?.Invoke(exception); });
            _client.OnDisconnect.AddListener(() => { OnClientDisconnected?.Invoke(); });

            _server.OnConnect.AddListener(id => { OnServerConnected?.Invoke(id); });
            _server.OnData.AddListener((id, data) => { OnServerDataReceived?.Invoke(id, data); });
            _server.OnError.AddListener((id, exception) => { OnServerError?.Invoke(id, exception); });
            _server.OnDisconnect.AddListener(id => { OnServerDisconnected?.Invoke(id); });
        }

        private void ClientUpdateHostTimeout(byte[] data)
        {
            if (_timeouts.ContainsKey(_client.HostSteamID))
            {
                _timeouts[_client.HostSteamID] = 0;
            }
            else
            {
                Debug.LogWarning("[SteamworksTransport]: Tried to check timeout for unset HostSteamID");
            }
        }

        private void ClientAddHostTimeout()
        {
            if (_timeouts != null)
                _timeouts.Clear();
            else
                _timeouts = new Dictionary<ulong, TimeSince>();

            _timeouts.Add(_client.HostSteamID, 0);
        }

        public override bool ClientConnected()
        {
            return _client.Connected;
        }

        public override void ClientConnect(string address)
        {
            // setup events for timeouts
            _client.OnConnected.AddListener(ClientAddHostTimeout);
            _client.OnData.AddListener(ClientUpdateHostTimeout);
            
            // connect
            _client.Connect(address);
        }

        public override bool ClientSend(int channelId, byte[] data)
        {
            return _client.Send(data, channelId);
        }

        public override void ClientDisconnect()
        {
            _client.OnConnected.RemoveListener(ClientAddHostTimeout);
            _client.OnData.RemoveListener(ClientUpdateHostTimeout);

            _timeouts = null;
            
            _client.Disconnect();
        }

        public override bool ServerActive()
        {
            return _server.Listening;
        }

        public override void ServerStart()
        {
            _server.OnConnect.AddListener(ServerAddClientTimeout);
            _server.OnData.AddListener(ServerUpdateClientTimeout);
            _server.OnDisconnect.AddListener(ServerDisconnectTimeout);
            _server.Start();
        }

        private void ServerDisconnectTimeout(int clientID)
        {
            if (!GetServerConnectionMap().TryGetBySecond(clientID, out var steamID)) 
                return;

            if (!_timeouts.ContainsKey(steamID)) 
                return;
            
            _timeouts.Remove(steamID);
        }

        private void ServerUpdateClientTimeout(int clientID, byte[] _)
        {
            if (clientID <= 0)
                return;
            
            if (!GetServerConnectionMap().TryGetBySecond(clientID, out var steamID))
            {
                Debug.LogError("[SteamworksTransport] Tried to update a timeout time for an unmapped client.");
                return;
            }

            if (_timeouts.ContainsKey(steamID))
                _timeouts[steamID] = 0;
            
            else
                _timeouts.Add(steamID, 0);
        }

        private void ServerAddClientTimeout(int clientID)
        {
            if (clientID <= 0)
                return;
            
            if (_timeouts == null)
                _timeouts = new Dictionary<ulong, TimeSince>();

            if (!GetServerConnectionMap().TryGetBySecond(clientID, out var steamID))
            {
                Debug.LogError("[SteamworksTransport] Tried to give a timeout time to an unmapped client.");
                return;
            }
                
            if (_timeouts.ContainsKey(steamID))
                _timeouts[steamID] = 0;
                
            else
                _timeouts.Add(steamID, 0);
        }


        public override bool ServerSend(int connectionId, int channelId, byte[] data)
        {
            return _server.Send(connectionId, channelId, data);
        }

        public override bool ServerDisconnect(int connectionId)
        {
            return _server.Disconnect(connectionId);
        }

        public override bool GetConnectionInfo(int connectionId, out string address)
        {
            return _server.GetConnectionInfo(connectionId, out address);
        }

        public override void ServerStop()
        {
            _server.OnConnect.RemoveListener(ServerAddClientTimeout);
            _server.OnData.RemoveListener(ServerUpdateClientTimeout);
            _server.OnDisconnect.RemoveListener(ServerDisconnectTimeout);
            _timeouts?.Clear();
            _server.Stop();
        }

        public override void Shutdown()
        {
            if (_server.Listening)
                ServerStop();

            if (_client.Connected)
                ClientDisconnect();
        }

        public override int GetMaxPacketSize(int channelId = Channels.DefaultReliable)
        {
            switch (channelId)
            {
                case (int)SteamPacketChannel.Unreliable:
                case (int)SteamPacketChannel.UnreliableNoDelay:
                    return 1200; // max MTU Size

                case (int)SteamPacketChannel.Reliable:
                case (int)SteamPacketChannel.Internal:
                    return 1048576; // Max Reliable up to 1MB

                default:
                    Debug.LogError("Tried to get max packet size on an unhandled channel. Returned 0.");
                    return 0;
            }
        }

        public BiDictionary<ulong, int> GetServerConnectionMap()
        {
            if (_server.Listening) 
                return _server.ConnectionMap;
            
            Debug.LogWarning("Trying to access ServerConnectionMap while server is offline or in client context.");
            return null;
        }

        public override string ToString()
        {
            if (_server.Listening)
                return "Listening for connections.";

            if (_client.Connected)
                return $"Connected to {_client.HostSteamID}";
            
            if (_client.Connecting)
                return $"Connecting to {_client.HostSteamID}";
            
            return "Steamworks Transport Idle";
        }

        private void FixedUpdate()
        {
            if (_server.Listening)
            {
                if (_timeouts == null) 
                    return;
                foreach (var client in _timeouts)
                {
                    if (client.Value < TimeoutSeconds)
                        continue;

                    if (!GetServerConnectionMap().TryGetByFirst(client.Key, out var clientID))
                    {
                        OnServerError?.Invoke(-1, new SteamConnectionException("Tried to timeout an unmapped client."));
                        _timeouts.Remove(client.Key);
                        return;
                    }

                    OnServerError?.Invoke(clientID, new ConnectionTimeoutException($"Connection to {client.Key} timed out."));
                    ServerDisconnect(clientID);
                    _timeouts.Remove(client.Key);
                    return;
                }
            }
            else if (_client.Connected)
            {
                var host = _timeouts.First();
                
                if (host.Value < TimeoutSeconds) 
                    return;
                
                OnClientError?.Invoke(new ConnectionTimeoutException("Connection to host timed out."));
                ClientDisconnect();
            }
        }
    }
}