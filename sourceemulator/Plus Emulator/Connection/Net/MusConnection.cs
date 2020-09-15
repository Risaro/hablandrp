using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Plus.HabboHotel.GameClients;

namespace Plus.Connection.Net
{
    internal class MusConnection
    {
        private Socket _socket;
        private byte[] _buffer = new byte[1024];

        internal MusConnection(Socket socket)
        {
            _socket = socket;
            try
            {
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnEvent_RecieveData, _socket);
            }
            catch
            {
                TryClose();
            }
        }

        internal void TryClose()
        {
            try
            {
                if (_socket == null) return;
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _socket.Dispose();
            }
            catch
            {
            }
            _socket = null;
            _buffer = null;
        }

        internal void OnEvent_RecieveData(IAsyncResult iAr)
        {
            try
            {
                int lenght;
                try
                {
                    lenght = _socket.EndReceive(iAr);
                }
                catch
                {
                    TryClose();
                    return;
                }
                var @string = Encoding.Default.GetString(_buffer, 0, lenght);
                if (@string.Contains(((char)0).ToString()))
                {
                    var strArray = @string.Split((char)0);
                    foreach (var str in strArray) ProcessCommand(@str);
                }
            }
            catch (Exception value)
            {
                Writer.Writer.LogException(value.ToString());
            }
            TryClose();
        }

        internal void ProcessCommand(string data)
        {
            if (!data.Contains(((char)1).ToString())) return;

            var parts = data.Split((char)1);
            var header = parts[1].ToLower();
            if (header == string.Empty) return;
            parts = parts.Skip(1).ToArray();

            switch (header)
            {
                //Disabled

                case "updatecredits":
                    {
                        break;
                    }
            }

        }
    }
}