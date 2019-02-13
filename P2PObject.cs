using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Steamworks;
using Transport.Steamworks.Helpers;
using UnityEngine;

namespace Transport.Steamworks
{
    public class P2PObject
    {
        private static readonly Dictionary<int, Networking.SendType> SendTypeFromChannel =
            new Dictionary<int, Networking.SendType>()
            {
                {(int)SteamPacketChannel.Internal, Networking.SendType.Reliable},
                {(int)SteamPacketChannel.Reliable, Networking.SendType.Reliable},
                {(int)SteamPacketChannel.Unreliable, Networking.SendType.Unreliable},
                {(int)SteamPacketChannel.UnreliableNoDelay, Networking.SendType.UnreliableNoDelay}
            };

        internal static readonly byte[] RequestConnectPacket = {(byte) SteamInternalPackets.RequestConnect};
        internal static readonly byte[] DisconnectPacket = {(byte) SteamInternalPackets.Disconnect};
        internal static readonly byte[] AcceptedConnectionPacket = {(byte) SteamInternalPackets.AcceptedConnection};
        internal static readonly byte[] AcceptedDisconnectPacket = {(byte) SteamInternalPackets.AcceptedDisconnect};

        protected event Action<ulong> OnConnectionAccepted;
        protected event Action<ulong> OnDisconnectAccepted;
        protected event Action<ulong> OnRequestDisconnect;
        protected event Action<ulong> OnRequestConnect;

        protected virtual void SetupEvents()
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            client.Networking.OnP2PData += OnP2PData;
            client.Networking.SetListenChannel(SteamPacketChannel.Internal.Int(), true);
            client.Networking.SetListenChannel(SteamPacketChannel.Unreliable.Int(), true);
            client.Networking.SetListenChannel(SteamPacketChannel.Reliable.Int(), true);
            client.Networking.SetListenChannel(SteamPacketChannel.UnreliableNoDelay.Int(), true);
            Debug.Log("Subscribed to event messages.");
        }

        protected virtual void CloseEvents()
        {
            if (!Steam.Instance.GetClient(out var client))
                return;

            client.Networking.OnP2PData -= OnP2PData;
            client.Networking.SetListenChannel(SteamPacketChannel.Internal.Int(), false);
            client.Networking.SetListenChannel(SteamPacketChannel.Unreliable.Int(), false);
            client.Networking.SetListenChannel(SteamPacketChannel.UnreliableNoDelay.Int(), false);
            client.Networking.SetListenChannel(SteamPacketChannel.Reliable.Int(), false);
            Debug.Log("Unsubscribed from event messages.");
        }

        private void OnP2PData(ulong steamID, byte[] data, int datalength, int channel)
        {
            var truncData = new byte[datalength];
            Array.Copy(data, truncData, datalength);

            switch (channel)
            {
                case (int) SteamPacketChannel.Internal:
                    OnInternalData(steamID, truncData);
                    break;

                case (int) SteamPacketChannel.Unreliable:
                case (int) SteamPacketChannel.UnreliableNoDelay:
                case (int) SteamPacketChannel.Reliable:
                    OnGameData(steamID, truncData);
                    break;

                default:
                    Debug.LogWarning($"Discarding message on unhandled channel {channel}. Sent by {steamID}");
                    break;
            }
        }

        protected virtual void OnGameData(ulong steamID, byte[] data)
        {
        }

        private void OnInternalData(ulong steamID, byte[] data)
        {
            if (data.SequenceEqual(RequestConnectPacket))
            {
                Debug.Log($"Request Connect Received from {steamID}");
                OnRequestConnect?.Invoke(steamID);
            }
            else if (data.SequenceEqual(AcceptedConnectionPacket))
            {
                Debug.Log($"Accepted Connection Received from {steamID}");
                OnConnectionAccepted?.Invoke(steamID);
            }
            else if (data.SequenceEqual(DisconnectPacket))
            {
                Debug.Log($"Received Disconnect request from {steamID}");
                OnRequestDisconnect?.Invoke(steamID);
            }
            else if (data.SequenceEqual(AcceptedDisconnectPacket))
            {
                Debug.Log($"Accepted Disconnect Received from {steamID}");
                OnDisconnectAccepted?.Invoke(steamID);
            }
        }

        protected bool SendInternalP2PPacket(ulong steamID, byte[] data)
        {
            if (!Steam.Instance.GetClient(out var client))
                return false;
            var channel = SteamPacketChannel.Internal.Int();
            return client.Networking.SendP2PPacket(steamID, data, data.Length, SendTypeFromChannel[channel], channel);
        }

        protected bool SendP2PPacket(ulong steamID, byte[] data, int channel = 0)
        {
            return Steam.Instance.GetClient(out var client) &&
                   client.Networking.SendP2PPacket(steamID, data, data.Length, SendTypeFromChannel[channel], channel);
        }

        private enum SteamInternalPackets : byte
        {
            RequestConnect,
            Disconnect,
            AcceptedConnection,
            AcceptedDisconnect
        }
    }
}