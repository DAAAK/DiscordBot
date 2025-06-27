using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class UpdateXpSlashCommand : ISlashCommands
{
    public string CommandName => "update-xp";
    private readonly DatabaseService _db;
    private readonly IConfiguration _configuration;

    public UpdateXpSlashCommand(IConfiguration configuration, DatabaseService db)
    {
        _configuration = configuration;
        _db = db;
    }
    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Manually update a user's XP")
            .AddOption("user", ApplicationCommandOptionType.User, "Select the user", isRequired: true)
            .AddOption("amount", ApplicationCommandOptionType.Integer, "New XP value", isRequired: true);
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

        var userOption = (SocketGuildUser)command.Data.Options.First(o => o.Name == "user").Value;
        var xpValue = (long)command.Data.Options.First(o => o.Name == "amount").Value;

        await _db.AddXPAsync(userOption.Id, userOption.DisplayName, (int)xpValue - await _db.GetUserXPAsync(userOption.Id));

        await command.RespondAsync($"✅ XP de {userOption.Mention} mis à jour à **{xpValue} XP**.", ephemeral: true);
    }
}
