string apiUrl = "";
string authorizationToken = ""; // Generate a random token, make sure to set it for your server too
TidalClient client = new TidalClient();

string jsClient = File.ReadAllText("JSClient.js")
    .Replace("C#_AUTHORIZATION_TOKEN", authorizationToken)
    .Replace("C#_API_URL", apiUrl);

client.ClientProtocol.SendRequest($@"
{{
    ""id"": 0,
    ""method"": ""Runtime.evaluate"",
    ""params"": {{
        ""userGesture"": true,
        ""expression"": ""{System.Web.HttpUtility.JavaScriptStringEncode(NUglify.Uglify.Js(jsClient).Code.Replace("\"", "'")) }""
    }}
}}");