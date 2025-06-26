using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class DeleteStreamerSlashCommand : ISlashCommands
{
    public string CommandName => "delete-streamer";

    private readonly DatabaseService _db;
    private readonly IConfiguration _config;

    public DeleteStreamerSlashCommand(IConfiguration config, DatabaseService db)
    {
        _db = db;
        _config = config;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var cmd = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Remove a streamer from the list.")
            .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true);

        await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_config["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var user = (SocketGuildUser)command.Data.Options.First(o => o.Name == "user").Value;

        bool success = await _db.DeleteStreamerAsync(user.Id);

        await command.RespondAsync(success
            ? $"🗑️ Removed streamer linked to {user.Mention}."
            : $"❌ No streamer found for {user.Mention}.", ephemeral: true);
    }
}
