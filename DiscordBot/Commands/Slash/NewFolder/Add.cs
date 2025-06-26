using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class AddStreamerSlashCommand : ISlashCommands
{
    public string CommandName => "add-streamer";

    private readonly DatabaseService _db;
    private readonly IConfiguration _config;

    public AddStreamerSlashCommand(IConfiguration config, DatabaseService db)
    {
        _db = db;
        _config = config;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var cmd = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Link a Twitch account to a Discord user.")
            .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
            .AddOption("twitch", ApplicationCommandOptionType.String, "Twitch username", true);

        await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_config["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var user = (SocketGuildUser)command.Data.Options.First(o => o.Name == "user").Value;
        var twitch = command.Data.Options.First(o => o.Name == "twitch").Value.ToString();

        bool success = await _db.AddStreamerAsync(user.Id, twitch);

        await command.RespondAsync(success
            ? $"✅ Linked {user.Mention} to Twitch **{twitch}**."
            : $"❌ Failed to add streamer (maybe already added).", ephemeral: true);
    }
}
