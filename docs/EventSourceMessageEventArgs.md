# EventSourceMessageEventArgs

Contains a single event data

## Properties

| Property | Type | Description |
| --- | --- | --- |
| Id | string | ID of the event. Empty if none was sent by the server |
| Event | string | Type of the event. `message` if none was sent by the server |
| Message | string | The meat of the event, data received in this event |
