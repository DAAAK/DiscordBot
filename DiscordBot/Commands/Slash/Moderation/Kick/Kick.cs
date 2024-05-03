using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Configuration;
public class KickSlashCommand : ISlashCommands
{
    public string CommandName => "kick";
    private readonly IConfiguration _configuration;

    public KickSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName("kick")
            .WithDescription("Kick a user from the server.")
            .AddOption("user", ApplicationCommandOptionType.User, "The user to kick.", isRequired: true)
            .AddOption("reason", ApplicationCommandOptionType.String, "The reason for the kick.", isRequired: false);

        await client.Rest.CreateGuildCommand(guildCommand.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
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
            await guildUser.KickAsync(reason);

            var embedBuilder = new EmbedBuilder()
                .WithTitle("User Kicked")
                .WithDescription($"**{guildUser.DisplayName}** has been kicked.")
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build());
        }
        catch (Exception ex)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"Failed to kick the user: {ex.Message}")
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
