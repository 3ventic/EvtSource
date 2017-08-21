# DisconnectEventArgs

Contains disconnect data

## Properties

| Property | Type | Description |
| --- | --- | --- |
| ReconnectDelay | int | Time to wait before attempting to reconnect to the same URL in milliseconds, as requested by the server or the default 3000 |
| Exception | Exception | If the disconnect was caused by an exception, contains this exception, otherwise null |
