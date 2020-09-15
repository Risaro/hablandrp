using Plus.Connection.Connection;
using Plus.Connection.Net;
using System;

namespace Plus.Configuration
{
    public class ConnectionHandling
    {
        private readonly SocketManager manager;

        public ConnectionHandling(int port, int maxConnections, int connectionsPerIP, bool enabeNagles)
        {
            manager = new SocketManager();
            manager.init(port, maxConnections, connectionsPerIP, new InitialPacketParser(), !enabeNagles);
        }

        public void init()
        {
            manager.connectionEvent += manager_connectionEvent;
            manager.initializeConnectionRequests();
        }

        private void manager_connectionEvent(ConnectionInformation connection)
        {
            connection.connectionChanged += connectionChanged;
            Plus.GetGame().GetClientManager().CreateAndStartClient((uint)connection.getConnectionID(), connection);
        }

        private void connectionChanged(ConnectionInformation information, ConnectionState state)
        {
            if (state == ConnectionState.CLOSED)
            {
                CloseConnection(information);
            }
        }

        private void CloseConnection(ConnectionInformation Connection)
        {
            try
            {
                Connection.Dispose();
                Plus.GetGame().GetClientManager().DisposeConnection((uint)Connection.getConnectionID());
            }
            catch (Exception e)
            {
                Logging.LogException(e.ToString());
            }
        }

        public void Destroy()
        {
            manager.destroy();
        }
    }
}