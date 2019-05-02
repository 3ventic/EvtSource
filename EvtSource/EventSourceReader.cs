using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EvtSource
{
    public class EventSourceReader : IDisposable
    {
        private const string DefaultEventType = "message";

        public delegate void MessageReceivedHandler(object sender, EventSourceMessageEventArgs e);
        public delegate void DisconnectEventHandler(object sender, DisconnectEventArgs e);

        private HttpClient Hc;
        private Stream Stream = null;
        private readonly Uri Uri;

        private volatile bool _IsDisposed = false;
        public bool IsDisposed => _IsDisposed;

        private volatile bool IsReading = false;
        private readonly object StartLock = new object();

        private int ReconnectDelay = 3000;
        private string LastEventId = string.Empty;

        public event MessageReceivedHandler MessageReceived;
        public event DisconnectEventHandler Disconnected;

        /// <summary>
        /// An instance of EventSourceReader
        /// </summary>
        /// <param name="url">URL to listen from</param>
        /// <param name="handler">An optional custom handler for HttpClient</param>
        public EventSourceReader(Uri url, HttpMessageHandler handler = null)
        {
            Uri = url;
            Hc = new HttpClient(handler ?? new HttpClientHandler());
        }


        /// <summary>
        /// Returns instantly and starts listening
        /// </summary>
        /// <returns>current instance</returns>
        /// <exception cref="ObjectDisposedException">Dispose() has been called</exception>
        public EventSourceReader Start()
        {
            if (_IsDisposed)
            {
                throw new ObjectDisposedException("EventSourceReader");
            }
            lock (StartLock)
            {
                if (IsReading == false)
                {
                    IsReading = true;
                    // Only start a new one if one isn't already running
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    ReaderAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
            return this;
        }


        /// <summary>
        /// Stop and dispose of the EventSourceReader
        /// </summary>
        public void Dispose()
        {
            _IsDisposed = true;
            Stream?.Dispose();
            Hc.CancelPendingRequests();
            Hc.Dispose();
        }


        private async Task ReaderAsync()
        {
            try
            {
                if (string.Empty != LastEventId)
                {
                    if (Hc.DefaultRequestHeaders.Contains("Last-Event-Id"))
                    {
                        Hc.DefaultRequestHeaders.Remove("Last-Event-Id");
                    }
                    
                    Hc.DefaultRequestHeaders.TryAddWithoutValidation("Last-Event-Id", LastEventId);
                }
                using (HttpResponseMessage response = await Hc.GetAsync(Uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Headers.TryGetValues("content-type", out IEnumerable<string> ctypes) || ctypes?.Contains("text/event-stream") == false)
                    {
                        throw new ArgumentException("Specified URI does not return server-sent events");
                    }

                    Stream = await response.Content.ReadAsStreamAsync();
                    using (var sr = new StreamReader(Stream))
                    {
                        string evt = DefaultEventType;
                        string id = string.Empty;
                        var data = new StringBuilder(string.Empty);

                        while (true)
                        {
                            string line = await sr.ReadLineAsync();
                            if (line == string.Empty)
                            {
                                // double newline, dispatch message and reset for next
                                if (data.Length > 0)
                                {
                                    MessageReceived?.Invoke(this, new EventSourceMessageEventArgs(data.ToString().Trim(), evt, id));
                                }
                                data.Clear();
                                id = string.Empty;
                                evt = DefaultEventType;
                                continue;
                            }
                            else if (line.First() == ':')
                            {
                                // Ignore comments
                                continue;
                            }

                            int dataIndex = line.IndexOf(':');
                            string field;
                            if (dataIndex == -1)
                            {
                                dataIndex = line.Length;
                                field = line;
                            }
                            else
                            {
                                field = line.Substring(0, dataIndex);
                                dataIndex += 1;
                            }

                            string value = line.Substring(dataIndex).Trim();

                            switch (field)
                            {
                                case "event":
                                    // Set event type
                                    evt = value;
                                    break;
                                case "data":
                                    // Append a line to data using a single \n as EOL
                                    data.Append($"{value}\n");
                                    break;
                                case "retry":
                                    // Set reconnect delay for next disconnect
                                    int.TryParse(value, out ReconnectDelay);
                                    break;
                                case "id":
                                    // Set ID
                                    LastEventId = value;
                                    id = LastEventId;
                                    break;
                                default:
                                    // Ignore other fields
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Disconnect(ex);
            }
        }

        private void Disconnect(Exception ex)
        {
            IsReading = false;
            Disconnected?.Invoke(this, new DisconnectEventArgs(ReconnectDelay, ex));
        }
    }
}
