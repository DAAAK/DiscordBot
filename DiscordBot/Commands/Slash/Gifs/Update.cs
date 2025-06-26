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

        var name = command.Data.Options.First(o => o.Name == "name").Value.ToString();
        var url = command.Data.Options.First(o => o.Name == "newurl").Value.ToString();

        await _db.UpdateGifAsync(name, url);
        await command.RespondAsync($"🔄 Updated **{name}** with new URL.", ephemeral: true);
    }
}
