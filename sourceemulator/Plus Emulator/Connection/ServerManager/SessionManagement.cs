using System.Collections.Generic;

namespace Plus.Connection.ServerManager
{
    /// <summary>
    /// Class SessionManagement.
    /// </summary>
    internal static class SessionManagement
    {
        /// <summary>
        /// The _sessions
        /// </summary>
        private static List<Session> _sessions;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        internal static void Init()
        {
            _sessions = new List<Session>();
        }

        /// <summary>
        /// Registers the session.
        /// </summary>
        /// <param name="session">The session.</param>
        internal static void RegisterSession(Session session)
        {
            if (!_sessions.Contains(session)) _sessions.Add(session);
        }

        /// <summary>
        /// Removes the session.
        /// </summary>
        /// <param name="session">The session.</param>
        internal static void RemoveSession(Session session)
        {
            _sessions.Remove(session);
        }

        /// <summary>
        /// Increases the error.
        /// </summary>
        internal static void IncreaseError()
        {
            {
                foreach (var current in _sessions) current.DisconnectionError++;
            }
        }

        /// <summary>
        /// Increases the disconnection.
        /// </summary>
        internal static void IncreaseDisconnection()
        {
            if (_sessions == null) return;

            {
                foreach (var current in _sessions) current.Disconnection++;
            }
        }
    }
}