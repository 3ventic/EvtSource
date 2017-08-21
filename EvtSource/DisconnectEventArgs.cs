using System;

namespace EvtSource
{
    public class DisconnectEventArgs : EventArgs
    {
        /// <summary>
        /// Reconnect delay requested by server
        /// </summary>
        public int ReconnectDelay { get; private set; }

        /// <summary>
        /// Exception that caused the disconnect, null if graceful disconnect.
        /// </summary>
        public Exception Exception { get; private set; }

        internal DisconnectEventArgs(int reconnectDelay, Exception exception)
        {
            ReconnectDelay = reconnectDelay;
            Exception = exception;
        }
    }
}