using Microsoft.Ajax.Utilities;
using System.Text;

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

client.Process.WaitForExit();

// Set status to null
HttpClient httpClient = new();
HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/status");
requestMessage.Headers.Add("Authorization", authorizationToken);
requestMessage.Content = new StringContent("null", Encoding.UTF8, "application/json");
httpClient.Send(requestMessage);

Console.WriteLine("Reset status");