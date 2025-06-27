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
        var clientId = _config["TwitchClientID"] ?? throw new Exception("Missing TwitchClientID in config");
        var clientSecret = _config["TwitchClientSecret"] ?? throw new Exception("Missing TwitchClientSecret in config");

        using var http = new HttpClient();

        var response = await http.PostAsync($"https://id.twitch.tv/oauth2/token" +
            $"?client_id={clientId}" +
            $"&client_secret={clientSecret}" +
            $"&grant_type=client_credentials", null);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("access_token", out var accessTokenElement) &&
            accessTokenElement.ValueKind == JsonValueKind.String)
        {
            return accessTokenElement.GetString()!;
        }

        throw new Exception("Access token not found in response.");
    }
}
