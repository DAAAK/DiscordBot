using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class UpdateCommandSlashCommand : ISlashCommands
{
    public string CommandName => "update-cmd";

    private readonly DatabaseService _db;
    private readonly IConfiguration _configuration;

    public UpdateCommandSlashCommand(IConfiguration configuration, DatabaseService db)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Update an existing command's description.")
            .AddOption("name", ApplicationCommandOptionType.String, "Name of the command", isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "New description", isRequired: true);

        await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        string name = command.Data.Options.First(o => o.Name == "name").Value.ToString();
        string description = command.Data.Options.First(o => o.Name == "description").Value.ToString();

        bool success = await _db.UpdateCommandAsync(name, description);
        if (success)
        {
            await command.RespondAsync($"✅ Command `/{name}` updated.", ephemeral: true);
            await HelpMessageUpdater.UpdateHelpMessageAsync(client, _db, _configuration);
        }
        else
        {
            await command.RespondAsync("❌ Failed to update the command.", ephemeral: true);
        }
    }
}