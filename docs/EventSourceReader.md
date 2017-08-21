# EventSourceReader

Implements `IDisposable`

## Constructor

Prepares the HTTP client.

#### Arguments

| arg name | description |
| --- | --- |
| url | _(required)_ URL to a text/event-stream URL |

## Methods

### Start()

Opens the HTTP request and starts listening to events

#### Arguments

None

#### Returns

The current instance, so you can write `var evt = new EventSourceReader(...).Start();`

### Dispose()

Stops listening and cleans up resources.
