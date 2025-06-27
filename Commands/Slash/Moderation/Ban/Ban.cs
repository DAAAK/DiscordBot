using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class BanSlashCommand : ISlashCommands
{
    public string CommandName => "ban";

    private readonly IConfiguration _configuration;

    public BanSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Ban selected user.")
            .AddOption("user", ApplicationCommandOptionType.User, "The user you want to ban", isRequired: true)
            .AddOption("reason", ApplicationCommandOptionType.String, "The reason for the ban.", isRequired: false);

        await client.Rest.CreateGuildCommand(guildCommand.Build(), ulong.Parse(_configuration["GuildID"]));
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

        var guildUser = (SocketGuildUser)command.Data.Options.First().Value;
        var reason = command.Data.Options.FirstOrDefault(o => o.Name == "reason")?.Value?.ToString() ?? "No reason specified";

        try
        {
            await guildUser.BanAsync(reason: reason);

            var embedBuilder = new EmbedBuilder()
                .WithTitle("User Banned")
                .WithDescription($"**{guildUser.DisplayName}** has been banned.")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build());
        }
        catch (Exception ex)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"Failed to ban the user: {ex.Message}")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
        }
    }
}
