
for use with [Vis2k Mirror 2018 Branch](https://github.com/vis2k/Mirror/tree/2018), [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks) and Unity 2018.

**Requires using .NET 4.x in Unity**

# Transport.Steamworks

A transport for Mirror using SteamworksP2P Networking with Facepunch.Steamworks. 

# Quick Start

1. Place the prefab `Prefabs/SteamworksNetworkManager` into the scene.

2. Set the target network address to the host user's steamid64

3. Set your App ID.

That's it.

# How to use your own prefab

1. Create a new gameobject with `SteamworksNetworkManager.cs` and `SteamworksTransport.cs` attached.

2. If your NetworkManager is marked as DontDestroyOnLoad, attach `Steam.cs` as well.

3. If not, create a new gameobject and attach `Steam.cs` and set your App ID if not using the testing app (480)

# Accessing Facepunch.Steamworks.Client

You could use Client.Instance, then check if null, and IsValid.

There is also `Steam.Instance.GetClient(our var client)` which can be used like so

```csharp
if (Steam.Instance.GetClient(out var client)) 
{
    // do something with client
}
```

# Getting Connection ID by steamID, or vice versa

use 

```csharp
// Get ConnectionID from SteamID
if (Steam.Instance.ServerConnectionMap.TryGetByFirst(steamID, out var connectionID)
{
    // do something with connectionID
}
// Get SteamID from ConnectionID
if (Steam.Instance.ServerConnectionMap.TryGetBySecond(connectionID, out var steamID))
{
    // do something with steamID
}
```

# Features

You can use Unreliable, Reliable, or UnreliableNoDelay channels via

```csharp
[Command(channel = (int) SteamPacketChannel.Unreliable)]
```

not specifying a channel is the same as
```csharp
[Command(channel = (int) SteamPacketChannel.Reliable)]
```
*note that `SteamPacketChannel.Internal` is for the transport to communicate connections / disconnects. Anything sent on this channel will be discarded.*


