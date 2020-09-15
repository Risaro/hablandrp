using Plus.Configuration;
using System;
using System.Net;
using System.Net.Sockets;

namespace Plus.Connection.ServerManager
{
    /// <summary>
    /// Class DataSocket.
    /// </summary>
    internal class DataSocket
    {
        /// <summary>
        /// The _listener
        /// </summary>
        private static Socket _listener;

        /// <summary>
        /// The _connection req callback
        /// </summary>
        private static AsyncCallback _connectionReqCallback;

        /// <summary>
        /// Setups the listener.
        /// </summary>
        /// <param name="port">The port.</param>
        internal static void SetupListener(int port)
        {
            SessionManagement.Init();
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var localEp = new IPEndPoint(IPAddress.Any, port);
            _listener.Bind(localEp);
            Console.WriteLine(port);
            _listener.Listen(1000);
            _connectionReqCallback = ConnectionRequest;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        internal static void Start()
        {
            WaitForNextConnection();
        }

        /// <summary>
        /// Connections the request.
        /// </summary>
        /// <param name="iAr">The i ar.</param>
        private static void ConnectionRequest(IAsyncResult iAr)
        {
            try
            {
                var socket = iAr.AsyncState as Socket;
                if (socket != null)
                {
                    var pSock = socket.EndAccept(iAr);
                    new Session(pSock);
                }
            }
            catch
            {
            }
            WaitForNextConnection();
        }

        /// <summary>
        /// Waits for next connection.
        /// </summary>
        private static void WaitForNextConnection()
        {
            try
            {
                _listener.BeginAccept(_connectionReqCallback, _listener);
            }
            catch (Exception e)
            {
                Logging.HandleException(e, "DataSocket.WaitForNextConnection");
            }
        }
    }
}