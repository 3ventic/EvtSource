# EvtSource

A .net core client library for [server-sent events](https://html.spec.whatwg.org/multipage/server-sent-events.html).

Available from nuget as `3v.EvtSource`.

## Usage Example

```cs
var evt = new EventSourceReader(new Uri("https://example.com/some/url/to/SSE")).Start();
evt.MessageReceived += (object sender, EventSourceMessageEventArgs) => Console.WriteLine($"{e.Event} : {e.Message}");
evt.Disconnected += async (object sender, DisconnectEventArgs e) => {
    Console.WriteLine($"Retry: {e.ReconnectDelay} - Error: {e.Message}");
    await Task.Delay(e.ReconnectDelay);
    evt.Start(); // Reconnect to the same URL
};
```
