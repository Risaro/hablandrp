namespace Plus.Connection.Connection.LoggingSystem
{
    /// <summary>
    /// Struct Message
    /// </summary>
    internal struct Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="timeStamp">The time stamp.</param>
        /// <param name="data">The data.</param>
        public Message(int connectionId, int timeStamp, string data) : this()
        {
            ConnectionId = connectionId;
            GetTimestamp = timeStamp;
            GetData = data;
        }

        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        /// <value>The connection identifier.</value>
        internal int ConnectionId { get; private set; }

        /// <summary>
        /// Gets the get timestamp.
        /// </summary>
        /// <value>The get timestamp.</value>
        internal int GetTimestamp { get; private set; }

        /// <summary>
        /// Gets the get data.
        /// </summary>
        /// <value>The get data.</value>
        internal string GetData { get; private set; }
    }
}