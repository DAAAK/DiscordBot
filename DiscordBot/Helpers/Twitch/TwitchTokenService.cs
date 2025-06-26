using Microsoft.Extensions.Configuration;
using System.Text.Json;

public class TwitchTokenService
{
    private readonly IConfiguration _config;

    public TwitchTokenService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var clientId = _config["TwitchClientId"];
        var clientSecret = _config["TwitchClientSecret"];

        using var http = new HttpClient();
        var response = await http.PostAsync($"https://id.twitch.tv/oauth2/token" +
            $"?client_id={clientId}" +
            $"&client_secret={clientSecret}" +
            $"&grant_type=client_credentials", null);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString();
    }
}
