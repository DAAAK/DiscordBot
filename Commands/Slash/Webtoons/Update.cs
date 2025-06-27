using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class UpdateWebtoonSlashCommand : ISlashCommands
{
    public string CommandName => "update-webtoon";

    private readonly IConfiguration _configuration;
    private readonly DatabaseService _db;

    public UpdateWebtoonSlashCommand(IConfiguration config, DatabaseService db)
    {
        _configuration = config;
        _db = db;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var guildId = ulong.Parse(_configuration["GuildID"]);

        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Update a webtoon's chapter and status.")
            .AddOption("name", ApplicationCommandOptionType.String, "Webtoon name", true)
            .AddOption("chapter", ApplicationCommandOptionType.Integer, "New chapter", true)
            .AddOption("status", ApplicationCommandOptionType.String, "New status", true);

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

        var nameOption = command.Data.Options.FirstOrDefault(o => o.Name == "name")?.Value?.ToString();
        var statusOption = command.Data.Options.FirstOrDefault(o => o.Name == "status")?.Value?.ToString();

        if (string.IsNullOrWhiteSpace(nameOption) || string.IsNullOrWhiteSpace(statusOption))
        {
            var embedBuilder = new EmbedBuilder()
                    .WithTitle("Invalid Input")
                    .WithDescription("The 'name' and 'status' options cannot be null or empty.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        var name = nameOption;
        var chapter = Convert.ToInt32(command.Data.Options.First(o => o.Name == "chapter").Value);
        var status = statusOption;

        await _db.UpdateWebtoonAsync(name, chapter, status);
        await command.RespondAsync($"♻️ Updated **{name}** to Chapter {chapter} with status '{status}'.", ephemeral: true);
    }
}
