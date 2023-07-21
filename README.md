# Tidal-Inject
Injects JavaScript code into either the Node process or web client of Tidal

This is can be used with the [Tidal-Song-API-Server
](https://github.com/ViperTools/Tidal-Song-API-Server) if using the correct JavaScript client. It is compatible with Windows and MacOS.

# Example

Here is a simple example of how to inject JS code into the client.

```cs
using TidalInject;

TidalClient tidal = new TidalClient();
string jsClient = File.ReadAllText("JSClient.js");

tidal.ClientProtocol.SendRequest(new JsonRequest
{
    Method = "Runtime.evaluate",
    Params =
    {
        { "expression", System.Web.HttpUtility.JavaScriptStringEncode(jsClient) }
    }
});
```
