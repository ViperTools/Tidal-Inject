using Microsoft.Ajax.Utilities;

string apiUrl = "https://tidal.viper.tools";
string authorizationToken = ""; // Generate a random token, make sure to set it for your server too
TidalClient client = new TidalClient();

string jsClient = File.ReadAllText("JSClient.js")
    .Replace("C#_AUTHORIZATION_TOKEN", authorizationToken)
    .Replace("C#_API_URL", apiUrl);

client.DevToolsProtocol.SendRequest(new()
{
    Method = "Runtime.evaluate",
    Params = new Dictionary<string, string>
    {
        { "expression", new Minifier().MinifyJavaScript(jsClient).Replace("\"", "'") }
    }
});