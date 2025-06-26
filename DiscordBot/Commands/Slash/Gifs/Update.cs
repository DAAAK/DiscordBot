using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class UpdateGifSlashCommand : ISlashCommands
{
    public string CommandName => "update-gif";

    private readonly IConfiguration _configuration;
    private readonly DatabaseService _db;

    public UpdateGifSlashCommand(IConfiguration configuration, DatabaseService db)
    {
        _configuration = configuration;
        _db = db;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Update an existing GIF URL.")
            .AddOption("name", ApplicationCommandOptionType.String, "GIF name", true)
            .AddOption("newurl", ApplicationCommandOptionType.String, "New GIF URL", true);

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

        var nameOption = command.Data.Options.FirstOrDefault(o => o.Name == "name")?.Value?.ToString();
        var urlOption = command.Data.Options.FirstOrDefault(o => o.Name == "newurl")?.Value?.ToString();

        if (string.IsNullOrWhiteSpace(nameOption) || string.IsNullOrWhiteSpace(urlOption))
        {
            var embedBuilder = new EmbedBuilder()
                    .WithTitle("Invalid Input")
                    .WithDescription("Both 'name' and 'newurl' options must be provided and non-empty.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        await _db.UpdateGifAsync(nameOption, urlOption);
        await command.RespondAsync($"🔄 Updated **{nameOption}** with new URL.", ephemeral: true);
    }
}
