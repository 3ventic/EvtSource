using System;

namespace EvtSource
{
    public class EventSourceMessageEventArgs : EventArgs
    {
        /// <summary>
        /// ID of the event, empty string if not present
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Event type, empty string if not present
        /// </summary>
        public string Event { get; private set; }

        /// <summary>
        /// Event data
        /// </summary>
        public string Message { get; private set; }

        internal EventSourceMessageEventArgs(string data, string type, string id)
        {
            Message = data;
            Event = type;
            Id = id;
        }
    }
}
