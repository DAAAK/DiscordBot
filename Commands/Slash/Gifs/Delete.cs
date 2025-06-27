using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class DeleteGifSlashCommand : ISlashCommands
{
    public string CommandName => "delete-gif";

    private readonly IConfiguration _configuration;
    private readonly DatabaseService _db;

    public DeleteGifSlashCommand(IConfiguration configuration, DatabaseService db)
    {
        _configuration = configuration;
        _db = db;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Delete a GIF by name.")
            .AddOption("name", ApplicationCommandOptionType.String, "GIF name", true);

        await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var executor = (SocketGuildUser)command.User;

        var roleChecker = new RequiredRoles(_configuration);

        if (!roleChecker.HasRequiredRole(executor))
        {
            var embedBuilder = new EmbedBuilder()
                    .WithTitle("Permission Refusée")
                    .WithDescription("Vous n'avez pas la permission d'utiliser cette commande.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        var nameOption = command.Data.Options.FirstOrDefault(o => o.Name == "name");

        if (nameOption?.Value is not string name || string.IsNullOrWhiteSpace(name))
        {
            await command.RespondAsync("❌ Invalid GIF name provided.", ephemeral: true);
            return;
        }

        await _db.DeleteGifAsync(name);

        await command.RespondAsync($"🗑️ GIF **{name}** deleted.", ephemeral: true);
    }
}
