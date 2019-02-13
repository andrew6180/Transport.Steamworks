namespace Transport.Steamworks.Helpers
{
    public static class SteamPacketChannelExt
    {
        public static int Int(this SteamPacketChannel channel)
        {
            return (int) channel;
        }
    }
}