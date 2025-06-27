using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

public class TwitchMonitor
{
    private readonly IConfiguration _config;
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly Timer _timer;

    private readonly TwitchTokenService _tokenService;

    public TwitchMonitor(IConfiguration config, DiscordSocketClient client, DatabaseService db, TwitchTokenService tokenService)
    {
        _config = config;
        _client = client;
        _db = db;
        _tokenService = tokenService;
        _timer = new Timer(async _ => await CheckLiveStreams(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private async Task CheckLiveStreams()
    {
        var streamers = await _db.GetAllStreamersAsync();
        foreach (var (discordId, twitchName, lastStatus) in streamers)
        {
            bool isLive = await IsTwitchUserLive(twitchName);
            if (isLive && lastStatus == 0)
            {
                var user = _client.GetUser(discordId);
                var channelId = ulong.Parse(_config["TwitchNotificationsChannelID"]);
                if (_client.GetChannel(channelId) is IMessageChannel channel)
                {
                    await channel.SendMessageAsync($"🔴 @everyone, {user?.Mention} is now live on Twitch!\nhttps://twitch.tv/{twitchName}");
                }
                await _db.UpdateLiveStatus(discordId, true);
            }
            else if (!isLive && lastStatus == 1)
            {
                await _db.UpdateLiveStatus(discordId, false);
            }
        }
    }

    private async Task<bool> IsTwitchUserLive(string username)
    {
        var clientId = _config["TwitchClientID"];
        var accessToken = await _tokenService.GetAccessTokenAsync();

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Client-ID", clientId);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var response = await client.GetStringAsync($"https://api.twitch.tv/helix/streams?user_login={username}");
        using var doc = JsonDocument.Parse(response);
        return doc.RootElement.GetProperty("data").GetArrayLength() > 0;
    }

}
