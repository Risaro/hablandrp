using System.Collections.Concurrent;

namespace Plus.Messages.Parsers
{
    /// <summary>
    /// Class Factorys.
    /// </summary>
    internal static class Factorys
    {
        /// <summary>
        /// The free objects
        /// </summary>
        private static readonly ConcurrentQueue<ClientMessage> FreeObjects = new ConcurrentQueue<ClientMessage>();

        /// <summary>
        /// Gets the client message.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="body">The body.</param>
        /// <returns>ClientMessage.</returns>
        public static ClientMessage GetClientMessage(int messageId, byte[] body)
        {
            ClientMessage clientMessage;
            if (!FreeObjects.TryDequeue(out clientMessage))
                return new ClientMessage(messageId, body);
            clientMessage.Init(messageId, body);
            return clientMessage;
        }

        /// <summary>
        /// Objects the callback.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void ObjectCallback(ClientMessage message)
        {
            FreeObjects.Enqueue(message);
        }
    }
}