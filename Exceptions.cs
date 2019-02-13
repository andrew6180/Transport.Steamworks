using System;

namespace Transport.Steamworks
{
    public class ConnectionTimeoutException : Exception
    {
        public ConnectionTimeoutException(string message) : base(message) { }
    }

    public class NoSteamConnectionException : Exception
    {
        public NoSteamConnectionException(string message) : base(message) { }
    }

    public class SteamConnectionException : Exception
    {
        public SteamConnectionException(string message) : base(message) { }
    }

    public class BadMethodCallException : Exception
    {
        public BadMethodCallException(string message) : base(message) { }
    }

    public class BadChannelException : Exception
    {
        public BadChannelException(string message) : base(message) { }

        public BadChannelException() : base("Tried to send packets to an unhandled channel") { }
    }
}