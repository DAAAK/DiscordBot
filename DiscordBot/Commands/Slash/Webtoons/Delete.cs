using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class DeleteWebtoonSlashCommand : ISlashCommands
{
    public string CommandName => "delete-webtoon";

    private readonly IConfiguration _configuration;
    private readonly DatabaseService _db;

    public DeleteWebtoonSlashCommand(IConfiguration config, DatabaseService db)
    {
        _configuration = config;
        _db = db;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var guildId = ulong.Parse(_configuration["GuildID"]);

        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Delete a webtoon by name.")
            .AddOption("name", ApplicationCommandOptionType.String, "Webtoon name", true);

        await client.Rest.CreateGuildCommand(command.Build(), guildId);
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
            var embedBuilder = new EmbedBuilder()
                    .WithTitle("Invalid Input")
                    .WithDescription("The webtoon name provided is invalid or missing.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        await _db.DeleteWebtoonAsync(name);
        await command.RespondAsync($"🗑️ Deleted **{name}** from your webtoons.", ephemeral: true);
    }
}
