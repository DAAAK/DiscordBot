using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class AddWebtoonSlashCommand : ISlashCommands
{
    public string CommandName => "add-webtoon";
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _db;

    public AddWebtoonSlashCommand(IConfiguration configuration, DatabaseService db)
    {
        _configuration = configuration;
        _db = db;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Add a new webtoon.")
            .AddOption("name", ApplicationCommandOptionType.String, "Webtoon name", true)
            .AddOption("chapter", ApplicationCommandOptionType.Integer, "Current chapter", true)
            .AddOption("status", ApplicationCommandOptionType.String, "Webtoon status", true);
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
        var chapterOption = command.Data.Options.FirstOrDefault(o => o.Name == "chapter");
        var statusOption = command.Data.Options.FirstOrDefault(o => o.Name == "status");

        if (nameOption?.Value == null || statusOption?.Value == null || chapterOption?.Value == null)
        {
            await command.RespondAsync("❌ Invalid input. Please ensure all required fields are provided.", ephemeral: true);
            return;
        }

        var name = nameOption.Value?.ToString() ?? string.Empty;
        var chapter = Convert.ToInt32(chapterOption.Value);
        var status = statusOption.Value?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(status))
        {
            await command.RespondAsync("❌ Invalid input. Name and status cannot be empty.", ephemeral: true);
            return;
        }

        bool success = await _db.AddWebtoonAsync(name, chapter, status);

        if (success)
        {
            await command.RespondAsync($"✅ Webtoon **{name}** added.", ephemeral: true);
            await WebtoonMessageUpdater.UpdateWebtoonMessageAsync(client, _db, _configuration);
        }
        else
        {
            await command.RespondAsync("❌ Failed to add the webtoon.", ephemeral: true);
        }
    }
}