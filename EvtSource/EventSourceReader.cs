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
        public delegate void MessageReceivedHandler(object sender, EventSourceMessageEventArgs e);
        public delegate void DisconnectEventHandler(object sender, DisconnectEventArgs e);

        private static HttpClient Hc = new HttpClient();
        private Stream Stream = null;
        private Uri Uri;

        private volatile bool IsDisposed = false;
        private Task Reader = Task.CompletedTask;

        private int ReconnectDelay = 3000;

        public event MessageReceivedHandler MessageReceived;
        public event DisconnectEventHandler Disconnected;

        /// <summary>
        /// An instance of EventSourceReader
        /// </summary>
        /// <param name="url">URL to listen from</param>
        public EventSourceReader(Uri url) => Uri = url;


        /// <summary>
        /// Returns instantly and starts listening
        /// </summary>
        /// <returns>current instance</returns>
        /// <exception cref="ObjectDisposedException">Dispose() has been called</exception>
        public EventSourceReader Start()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("EventSourceReader");
            }
            if (Reader.Status == TaskStatus.RanToCompletion || Reader.Status == TaskStatus.Faulted)
            {
                // Only start a new one if one isn't already running
                Reader = ReaderAsync();
            }
            return this;
        }


        /// <summary>
        /// Stop and dispose of the EventSourceReader
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;
            Stream?.Dispose();
            Hc.CancelPendingRequests();
            Hc.Dispose();
        }


        private async Task ReaderAsync()
        {
            try
            {
                using (HttpResponseMessage response = await Hc.GetAsync(Uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Headers.TryGetValues("Content-Type", out IEnumerable<string> ctypes) || !ctypes.Contains("text/event-stream"))
                    {
                        throw new ArgumentException("Specified URI does not return server-sent events");
                    }

                    Stream = await response.Content.ReadAsStreamAsync();
                    using (StreamReader sr = new StreamReader(Stream))
                    {
                        string evt = string.Empty;
                        string id = string.Empty;
                        StringBuilder data = new StringBuilder(string.Empty);

                        while (!sr.EndOfStream)
                        {
                            string line = await sr.ReadLineAsync();
                            if (line == string.Empty)
                            {
                                // double newline, dispatch message and reset for next
                                MessageReceived?.Invoke(this, new EventSourceMessageEventArgs(data.ToString(), evt, id));
                                data.Clear();
                                id = string.Empty;
                                continue;
                            }
                            else if (line.First() == ':')
                            {
                                // Ignore comments
                                continue;
                            }

                            int dataIndex = line.IndexOf(':');
                            string field = line.Substring(0, dataIndex);
                            dataIndex += 2;

                            switch (field)
                            {
                                case "event":
                                    // Set event type
                                    evt = line.Substring(dataIndex);
                                    break;
                                case "data":
                                    // Append a line to data using a single \n as EOL
                                    data.Append($"{line.Substring(dataIndex)}\n");
                                    break;
                                case "retry":
                                    // Set reconnect delay for next disconnect
                                    int.TryParse(line.Substring(dataIndex), out ReconnectDelay);
                                    break;
                                case "id":
                                    // Set ID
                                    id = line.Substring(dataIndex);
                                    break;
                                default:
                                    // Ignore other fields
                                    break;
                            }
                        }
                    }
                    Disconnected?.Invoke(this, new DisconnectEventArgs(ReconnectDelay, null));
                }
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke(this, new DisconnectEventArgs(ReconnectDelay, ex));
            }
        }
    }
}
