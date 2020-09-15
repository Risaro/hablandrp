using Plus.Connection.Connection.LoggingSystem;
using Plus.Database.Manager.Database;
using System;
using System.Collections;
using System.Text;

namespace Plus.Messages
{
    /// <summary>
    /// Class MessageLoggerManager.
    /// </summary>
    internal class MessageLoggerManager
    {
        /// <summary>
        /// The _logged messages
        /// </summary>
        private static Queue _loggedMessages;

        /// <summary>
        /// The _enabled
        /// </summary>
        private static bool _enabled;

        /// <summary>
        /// The _time since last packet
        /// </summary>
        private static DateTime _timeSinceLastPacket;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MessageLoggerManager"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        internal static bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (_enabled)
                    _loggedMessages = new Queue();
            }
        }

        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="state">The state.</param>
        internal static void AddMessage(byte[] data, int connectionId, LogState state)
        {
            if (!_enabled)
                return;
            string data2;
            switch (state)
            {
                case LogState.ConnectionOpen:
                    data2 = "CONCLOSE";
                    break;

                case LogState.ConnectionClose:
                    data2 = "CONOPEN";
                    break;

                default:
                    data2 = Encoding.Default.GetString(data);
                    break;
            }
            lock (_loggedMessages.SyncRoot)
            {
                var message = new Message(connectionId, GenerateTimestamp(), data2);
                _loggedMessages.Enqueue(message);
            }
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        internal static void Save()
        {
            if (!_enabled)
                return;
            lock (_loggedMessages.SyncRoot)
            {
                if (_loggedMessages.Count <= 0)
                    return;
                var databaseManager = new DatabaseManager(1u, 1u);
                using (var queryReactor = databaseManager.GetQueryReactor())
                    while (_loggedMessages.Count > 0)
                    {
                        var message = (Message)_loggedMessages.Dequeue();
                        queryReactor.SetQuery(
                            "INSERT INTO system_packetlog (connectionid, timestamp, data) VALUES @connectionid @timestamp, @data");
                        queryReactor.AddParameter("connectionid", message.ConnectionId);
                        queryReactor.AddParameter("timestamp", message.GetTimestamp);
                        queryReactor.AddParameter("data", message.GetData);
                        queryReactor.RunQuery();
                    }
            }
        }

        /// <summary>
        /// Generates the timestamp.
        /// </summary>
        /// <returns>System.Int32.</returns>
        private static int GenerateTimestamp()
        {
            var now = DateTime.Now;
            var timeSpan = now - _timeSinceLastPacket;
            _timeSinceLastPacket = now;
            return ((int)timeSpan.TotalMilliseconds);
        }
    }
}