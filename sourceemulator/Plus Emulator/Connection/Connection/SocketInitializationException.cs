using System;

namespace Plus.Connection.Connection
{
    /// <summary>
    /// Class SocketInitializationException.
    /// </summary>
    public class SocketInitializationException : Exception
    {
        public SocketInitializationException(string message)
            : base(message)
        {
        }
    }
}