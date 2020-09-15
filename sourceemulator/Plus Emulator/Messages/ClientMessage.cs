using Plus.Messages.Parsers;
using System;
using System.Globalization;
using System.Text;

namespace Plus.Messages
{
    /// <summary>
    /// Class ClientMessage.
    /// </summary>
    public class ClientMessage : IDisposable
    {
        /// <summary>
        /// The _body
        /// </summary>
        private byte[] _body;

        /// <summary>
        /// The _pointer
        /// </summary>
        private int _pointer;

        /// <summary>
        /// The length
        /// </summary>
        internal int Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientMessage"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="body">The body.</param>
        internal ClientMessage(int messageId, byte[] body)
        {
            Init(messageId, body);
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        internal int Id { get; private set; }

        /// <summary>
        /// Gets the length of the remaining.
        /// </summary>
        /// <value>The length of the remaining.</value>
        internal int RemainingLength
        {
            get { return (_body.Length - _pointer); }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Factorys.ObjectCallback(this);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            var str = string.Empty;
            str += Encoding.Default.GetString(_body);
            for (var i = 0; i < 13; i++)
                str = str.Replace(char.ToString(Convert.ToChar(i)), string.Format("[{0}]", i));
            return str;
        }

        /// <summary>
        /// Initializes the specified message identifier.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="body">The body.</param>
        internal void Init(int messageId, byte[] body)
        {
            if (body == null)
                body = new byte[0];
            Id = messageId;
            _body = body;
            Length = body.Length;
            _pointer = 0;
        }

        /// <summary>
        /// Reads the bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>System.Byte[].</returns>
        internal byte[] ReadBytes(int bytes)
        {
            if (bytes > RemainingLength)
                bytes = RemainingLength;
            var array = new byte[bytes];

            {
                for (var i = 0; i < bytes; i++)
                    array[i] = _body[(_pointer++)];

                return array;
            }
        }

        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>System.Byte[].</returns>
        internal byte[] GetBytes(int bytes)
        {
            if (bytes > RemainingLength)
                bytes = RemainingLength;
            var array = new byte[bytes];
            var i = 0;
            var num = _pointer;

            {
                while (i < bytes)
                {
                    array[i] = _body[num];
                    i++;
                    num++;
                }
                return array;
            }
        }

        /// <summary>
        /// Gets the next.
        /// </summary>
        /// <returns>System.Byte[].</returns>
        internal byte[] GetNext()
        {
            int bytes = HabboEncoding.DecodeInt16(ReadBytes(2));
            return ReadBytes(bytes);
        }

        /// <summary>
        /// Gets the string.
        /// </summary>
        /// <returns>System.String.</returns>
        internal string GetString()
        {
            return GetString(Encoding.UTF8);
        }

        /// <summary>
        /// Gets the string.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>System.String.</returns>
        internal string GetString(Encoding encoding)
        {
            return encoding.GetString(GetNext());
        }

        /// <summary>
        /// Gets the integer from string.
        /// </summary>
        /// <returns>System.Int32.</returns>
        internal int GetIntegerFromString()
        {
            int result;
            var s = GetString(Encoding.ASCII);
            int.TryParse(s, out result);
            return result;
        }

        /// <summary>
        /// Gets the bool.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool GetBool()
        {
            return RemainingLength > 0 && _body[_pointer++] == Convert.ToChar(1);
        }

        /// <summary>
        /// Gets the integer16.
        /// </summary>
        /// <returns>System.Int16.</returns>
        internal short GetInteger16()
        {
            return short.Parse(GetInteger().ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Gets the integer.
        /// </summary>
        /// <returns>System.Int32.</returns>
        internal int GetInteger()
        {
            if (RemainingLength < 1)
                return 0;
            var v = GetBytes(4);
            var result = HabboEncoding.DecodeInt32(v);

            {
                _pointer += 4;
                return result;
            }
        }

        /// <summary>
        /// Gets the u integer.
        /// </summary>
        /// <returns>System.UInt32.</returns>
        internal uint GetUInteger()
        {
            return Convert.ToUInt32(GetInteger());
        }

        /// <summary>
        /// Gets the u integer16.
        /// </summary>
        /// <returns>System.UInt16.</returns>
        internal ushort GetUInteger16()
        {
            return Convert.ToUInt16(GetInteger());
        }
    }
}