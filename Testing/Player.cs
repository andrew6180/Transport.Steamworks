using Facepunch.Steamworks;
using Facepunch.Steamworks.Unity;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using Client = Facepunch.Steamworks.Client;

namespace Transport.Steamworks.Testing
{
    [RequireComponent(typeof(SteamAvatar))]
    public class Player : NetworkBehaviour
    {
        private SteamAvatar _avatar;

        private RawImage _image;

        [SyncVar(hook = "UpdateSteamAvatar")] public ulong SteamID;

        [SyncVar] public string SteamName;

        public override void OnStartServer()
        {
            Steam.ServerConnectionMap.TryGetBySecond(connectionToClient.connectionId, out SteamID);
            SteamName = Client.Instance.Friends.GetName(SteamID);
            name = $"[{SteamName}] - {SteamID}";
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _avatar = GetComponent<SteamAvatar>();
            _image = GetComponent<RawImage>();

            UpdateSteamAvatar(SteamID);
        }

        private void UpdateSteamAvatar(ulong steamID)
        {
            if (!isServer) SteamID = steamID;
            Debug.Log($"SetSteamAvatar {steamID}");
            _avatar.SteamId = steamID;
            _avatar.Size = Friends.AvatarSize.Large;
            _avatar.Fetch(steamID);
        }

        private void OnGUI()
        {
            if (_image.texture == null) 
                return;
            var pos = isLocalPlayer ? 0 : 1;
            GUI.DrawTexture(new Rect(0, 64 * pos, 64, 64), _image.texture, ScaleMode.StretchToFill);
        }
    }
}