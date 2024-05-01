using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class ListRolesCommand : Commands
{
    public ListRolesCommand(DiscordSocketClient client, IConfiguration configuration) : base(client, configuration)
    {
    }

    public override async Task HandleCommand(SocketSlashCommand command)
    {
        var guildUser = (SocketGuildUser)command.Data.Options.First().Value;

        var roleList = string.Join(",\n", guildUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

        var embedBuiler = new EmbedBuilder()
            .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
            .WithTitle("Roles")
            .WithDescription(roleList)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embedBuiler.Build());
    }

    public async Task RegisterSlashCommand()
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName("kkkkk")
            .WithDescription("Lists all roles of a user.")
            .AddOption("user", ApplicationCommandOptionType.User, "The user whose roles you want to list.", isRequired: true);

        await RegisterSlashCommand(guildCommand);
    }
}
