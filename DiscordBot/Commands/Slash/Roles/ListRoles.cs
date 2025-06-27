using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class ListRolesSlashCommand : ISlashCommands
{
    public string CommandName => "roles";

    private readonly IConfiguration _configuration;

    public ListRolesSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Lists all roles of a user.")
            .AddOption("user", ApplicationCommandOptionType.User, "The user whose roles you want to be listed", isRequired: true);
        await client.Rest.CreateGuildCommand(guildCommand.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var guildUser = (SocketGuildUser)command.Data.Options.First().Value;

        var roleList = string.Join(",\n", guildUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(guildUser.DisplayName.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
            .WithTitle("Roles")
            .WithDescription(roleList)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
