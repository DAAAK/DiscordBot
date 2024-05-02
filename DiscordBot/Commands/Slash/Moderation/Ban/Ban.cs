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
            .WithName("ban")
            .WithDescription("Ban selected user.")
            .AddOption("user", ApplicationCommandOptionType.User, "The user you want to ban", isRequired: true)
            .AddOption("reason", ApplicationCommandOptionType.String, "The reason for the ban.", isRequired: false);

        await client.Rest.CreateGuildCommand(guildCommand.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command)
    {
        var executor = (SocketGuildUser)command.User;

        if (!HasRequiredRole(executor))
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Permission Denied")
                .WithDescription($"You do not have permission to use this command.")
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

            await command.RespondAsync(embed: embedBuilder.Build());
        }
    }

    private bool HasRequiredRole(SocketGuildUser user)
    {
        if (_configuration == null || !_configuration.GetSection("RequiredRolesIDS").Exists())
        {
            return false;
        }

        var requiredRoleIds = _configuration.GetSection("RequiredRolesIDS").GetChildren().Select(x => ulong.Parse(x.Value)).ToArray();

        foreach (var roleId in requiredRoleIds)
        {
            if (user.Roles.Any(role => role.Id == roleId))
            {
                return true;
            }
        }

        return false;
    }
}
