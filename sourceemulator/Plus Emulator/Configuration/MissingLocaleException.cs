using System;

namespace Azure.Configuration
{
    /// <summary>
    /// Class MissingLocaleException.
    /// </summary>
    internal class MissingLocaleException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Exception" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MissingLocaleException(string message) : base(message)
        {
        }
    }
}