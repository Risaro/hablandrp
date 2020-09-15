using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Plus.Connection.ServerManager
{
    /// <summary>
    /// Class Session.
    /// </summary>
    internal class Session
    {
        /// <summary>
        /// The _sock
        /// </summary>
        private readonly Socket _sock;

        /// <summary>
        /// The _received callback
        /// </summary>
        private readonly AsyncCallback _receivedCallback;

        /// <summary>
        /// The _data buffer
        /// </summary>
        private readonly byte[] _dataBuffer;

        /// <summary>
        /// The _ip
        /// </summary>
        private readonly string _ip;

        /// <summary>
        /// The _long ip
        /// </summary>
        private readonly string _longIp;

        /// <summary>
        /// The _closed
        /// </summary>
        private bool _closed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="pSock">The p sock.</param>
        public Session(Socket pSock)
        {
            _sock = pSock;
            _dataBuffer = new byte[2048];
            _receivedCallback = BytesReceived;
            _closed = false;
            Out.WriteLine("Received connection", "Plus.Conn", ConsoleColor.DarkBlue);
            _ip = _sock.RemoteEndPoint.ToString().Split(':')[0];
            _longIp = pSock.RemoteEndPoint.ToString();
            SendData("authreq");
            ContinueListening();
        }

        /// <summary>
        /// Gets or sets the disconnection.
        /// </summary>
        /// <value>The disconnection.</value>
        internal int Disconnection { get; set; }

        /// <summary>
        /// Gets or sets the disconnection error.
        /// </summary>
        /// <value>The disconnection error.</value>
        internal int DisconnectionError { get; set; }

        /// <summary>
        /// Gets the ip.
        /// </summary>
        /// <value>The ip.</value>
        public string Ip
        {
            get { return _ip; }
        }

        /// <summary>
        /// Gets the get long ip.
        /// </summary>
        /// <value>The get long ip.</value>
        internal string GetLongIp
        {
            get { return _sock.RemoteEndPoint.ToString(); }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        internal void Close()
        {
            if (_closed) return;
            _closed = true;
            try
            {
                _sock.Close();
            }
            catch
            {
            }
            SessionManagement.RemoveSession(this);
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool GetState()
        {
            bool result;
            try
            {
                result = _sock.Connected;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Byteses the received.
        /// </summary>
        /// <param name="pIar">The p iar.</param>
        private void BytesReceived(IAsyncResult pIar)
        {
            try
            {
                var num = _sock.EndReceive(pIar);
                try
                {
                    var destinationArray = new byte[num];
                    Array.Copy(_dataBuffer, destinationArray, num);
                    var array = Encoding.Default.GetString(_dataBuffer, 0, num).Split('|');
                    var array2 = array;
                    if (
                        (from text in array2 where !string.IsNullOrEmpty(text) select text.Split(':')).Any(
                            array3 => array3[0].Length == 0))
                    {
                        Close();
                        return;
                    }
                    ContinueListening();
                }
                catch
                {
                    Close();
                }
            }
            catch
            {
                Close();
            }
        }

        /// <summary>
        /// Continues the listening.
        /// </summary>
        private void ContinueListening()
        {
            try
            {
                _sock.BeginReceive(_dataBuffer, 0, _dataBuffer.Length, SocketFlags.None, _receivedCallback, this);
            }
            catch
            {
                Close();
            }
        }

        /// <summary>
        /// Sends the data.
        /// </summary>
        /// <param name="pData">The p data.</param>
        private void SendData(string pData)
        {
            try
            {
                var bytes = Encoding.Default.GetBytes(string.Format("{0}|", pData));
                _sock.Send(bytes);
            }
            catch
            {
                Close();
            }
        }
    }
}