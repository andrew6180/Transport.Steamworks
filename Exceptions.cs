using System;

namespace Transport.Steamworks
{
    public class ConnectionTimeoutException : Exception
    {
        public ConnectionTimeoutException(string message) : base(message) { }

        public ConnectionTimeoutException() : base("The connection to the remote user timed out.") { }
    }

    public class NoSteamConnectionException : Exception
    {
        public NoSteamConnectionException(string message) : base(message) { }

        public NoSteamConnectionException() : base("Could not connect to steam.") { }
    }

    public class SteamConnectionException : Exception
    {
        public SteamConnectionException(string message) : base(message) { }

        public SteamConnectionException() : base("Unknown Steam Connection Error.") { }
    }

    public class BadMethodCallException : Exception
    {
        public BadMethodCallException(string message) : base(message) { }
        
        public BadMethodCallException() : base("Method called in an unintended way") { }
    }

    public class BadChannelException : Exception
    {
        public BadChannelException(string message) : base(message) { }

        public BadChannelException() : base("Tried to send packets to an unhandled channel") { }
    }
}