using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public static class HelpMessageUpdater
{
    public static async Task UpdateHelpMessageAsync(
        DiscordSocketClient _client,
        DatabaseService _db,
        IConfiguration _config)
    {
        if (!ulong.TryParse(_config["CommandsChannelID"], out var channelId)) return;

        var channel = _client.GetChannel(channelId) as SocketTextChannel;
        if (channel == null) return;

        var commandDescriptions = await _db.GetAllCommandsAsync();

        var embedBuilder = new EmbedBuilder()
            .WithTitle("📖 Here are the commands you can use")
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        int count = 0;
        foreach (var (name, desc) in commandDescriptions)
        {
            if (count++ >= 25) break;
            embedBuilder.AddField($"`/{name}`", desc, inline: true);
        }

        var embed = embedBuilder.Build();

        var messages = await channel.GetMessagesAsync(10).FlattenAsync();
        var helpMsg = messages.FirstOrDefault(m =>
            m.Author.Id == _client.CurrentUser.Id &&
            m.Embeds.Any(e => e.Title?.Contains("commands") == true));

        if (helpMsg is IUserMessage userMessage)
        {
            await userMessage.ModifyAsync(msg => msg.Embed = embed);
        }
        else
        {
            await channel.SendMessageAsync(embed: embed);
        }
    }
}
