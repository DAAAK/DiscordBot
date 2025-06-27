using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System.Threading.Channels;

public class ListStreamersSlashCommand : ISlashCommands
{
    public string CommandName => "streamers";

    private readonly DatabaseService _db;
    private readonly IConfiguration _config;

    public ListStreamersSlashCommand(IConfiguration config, DatabaseService db)
    {
        _db = db;
        _config = config;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var cmd = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("List all streamers linked to users.");
        await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_config["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var list = await _db.GetAllStreamersAsync();

        if (!list.Any())
        {
            await command.RespondAsync("📭 No streamers found.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("📺 Twitch Streamers")
            .WithColor(Color.Purple);

        var guild = (command.Channel as SocketGuildChannel)?.Guild;

        foreach (var (discordId, twitchName, _) in list)
        {
            var user = guild?.GetUser(discordId);
            embed.AddField(user?.DisplayName ?? $"UserID {discordId}", twitchName, inline: true);
        }

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}
