using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebSocketSharp;

class WebSocketTarget
{
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("devtoolsFrontendUrl")]
    public string DevtoolsFrontendUrl { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("webSocketDebuggerUrl")]
    public string WebSocketDebuggerUrl { get; set; }
}

class JsonResponse {
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("result")]
    public JsonElement Result { get; set; }
}

class JsonRequest
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 1;

    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, string> Params { get; set; } 
}

class MessageReceivedEventArgs : EventArgs
{
    public string Data;
}

internal class DevToolsProtocol
{
    public event EventHandler<MessageEventArgs> MessageReceived;

    public WebSocketTarget? Target
    {
        get { return target; }
        set
        {
            socket?.Close();
            target = value;
            socket = null;

            if (target != null)
            {
                socket = new WebSocket(target.WebSocketDebuggerUrl);
                socket.OnMessage += (sender, e) => OnMessageReceived(e);
                socket.Connect();
            }
        }
    }

    private WebSocketTarget? target;
    private static readonly HttpClient httpClient = new HttpClient();
    private int port;
    private WebSocket? socket;
    private ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    private string? lastResponse;

    public DevToolsProtocol(int port)
    {
        this.port = port;
    }

    public WebSocketTarget[]? GetTargets()
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{port}/json");
        HttpResponseMessage response = httpClient.Send(request);

        return response.Content.ReadFromJsonAsync<WebSocketTarget[]>().Result;
    }

    public JsonResponse? SendRequest(JsonRequest request)
    {
        if (target == null || socket == null)
        {
            throw new Exception("No target has been set");
        }

        socket.Send(JsonSerializer.Serialize(request));

        while (lastResponse == null)
        {
            manualResetEvent.WaitOne();
            manualResetEvent.Reset();
        }

        JsonResponse? res = JsonSerializer.Deserialize<JsonResponse>(lastResponse);

        lastResponse = null;

        return res;
    }

    protected virtual void OnMessageReceived(MessageEventArgs e)
    {
        lastResponse = e.Data;
        manualResetEvent.Set();

        if (MessageReceived != null)
        {
            MessageReceived(this, e);
        }
    }
}