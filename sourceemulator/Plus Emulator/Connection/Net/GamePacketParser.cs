using Plus.Configuration;
using Plus.Connection.Connection;
using Plus.HabboHotel.GameClients;
using Plus.Messages;
using Plus.Messages.Parsers;
using System;

namespace Plus.Connection.Net
{
    /// <summary>
    /// Class GamePacketParser.
    /// </summary>
    public class GamePacketParser : IDataParser
    {
        /// <summary>
        /// The _current client
        /// </summary>
        private readonly GameClient _currentClient;

        /// <summary>
        /// The _con
        /// </summary>
        private ConnectionInformation _con;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamePacketParser"/> class.
        /// </summary>
        /// <param name="me">Me.</param>
        internal GamePacketParser(GameClient me)
        {
            _currentClient = me;
        }

        /// <summary>
        /// Delegate HandlePacket
        /// </summary>
        /// <param name="message">The message.</param>
        public delegate void HandlePacket(ClientMessage message);

        public event HandlePacket OnNewPacket;

        /// <summary>
        /// Sets the connection.
        /// </summary>
        /// <param name="con">The con.</param>
        public void SetConnection(ConnectionInformation con)
        {
            _con = con;
            OnNewPacket = null;
        }

        /// <summary>
        /// Handles the packet data.
        /// </summary>
        /// <param name="data">The data.</param>
        public void HandlePacketData(byte[] data)
        {
            var i = 0;
            if (data.Length == 0) return;
            while (i < data.Length)
            {
                if (data.Length - i < 6) return;
                short messageId = 0;
                try
                {
                    var length = HabboEncoding.DecodeInt32(new[]
                    {
                        data[i++],
                        data[i++],
                        data[i++],
                        data[i++]
                    });
                    if (length < 2 || length > 4096) return;
                    messageId = HabboEncoding.DecodeInt16(new[]
                    {
                        data[i++],
                        data[i++]
                    });
                    var packetContent = new byte[length - 2];
                    var num2 = 0;
                    while (num2 < packetContent.Length && i < data.Length)
                    {
                        packetContent[num2] = data[i++];
                        num2++;
                    }
                    if (OnNewPacket == null) continue;
                    using (var clientMessage = Factorys.GetClientMessage(messageId, packetContent)) OnNewPacket(clientMessage);
                }
                catch (Exception exception)
                {
                    Logging.HandleException(exception, string.Format("packet handling ----> {0}", messageId));
                    _con.Dispose();
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            OnNewPacket = null;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new GamePacketParser(_currentClient);
        }
    }
}