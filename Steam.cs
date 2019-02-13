using System.Runtime.CompilerServices;
using Facepunch.Steamworks;
using Mirror;
using Transport.Steamworks.Helpers;
using UnityEngine;

namespace Transport.Steamworks
{
    public class Steam : MonoBehaviour
    {
        private static Steam _instance;

        public uint AppID = 480;

        public static Steam Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null) _instance = value;
            }
        }
        
        /// <summary>
        /// Returns the current connection map of clients.
        /// <para>Returns null in client or offline context.</para>
        /// </summary>
        public static BiDictionary<ulong, int> ServerConnectionMap
        {
            get
            {
                if (NetworkManager.singleton.transport is SteamworksTransport transport)
                    return transport.GetServerConnectionMap();
                return null;
            }
        }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup Facepunch.Steamworks
            Config.ForUnity(Application.platform.ToString());

            var client = new Client(AppID);

            if (Client.Instance == null || !client.IsValid)
                Debug.LogError("Failed to start steam.");
        }

        private void Update()
        {
            if (Client.Instance != null) Client.Instance.Update();
        }

        private void OnDestroy()
        {
            Client.Instance?.Dispose();
        }

        private void OnApplicationQuit()
        {
            Client.Instance?.Dispose();
        }

        /// <summary>
        ///     <para>Easily grab the client instance reference without having to null check.</para>
        ///     <para>Use as if (!GetClient(out var client)) return;</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Aggressive inlining for faster calls.
        public bool GetClient(out Client client)
        {
            client = Client.Instance;
            if (client != null && client.IsValid)
                return true;

            Debug.LogError("Tried to access client when it is null or invalid.");
            return false;
        }
    }
}