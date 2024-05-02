using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class ListRolesCommandModule : ICommands
{
    public string CommandName => "list-roles";

    public async Task RegisterCommandsAsync(DiscordSocketClient client, IConfiguration configuration)
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName("list-roles")
            .WithDescription("Lists all roles of a user.")
            .AddOption("user", ApplicationCommandOptionType.User, "The user whose roles you want to be listed", isRequired: true);
        await client.Rest.CreateGuildCommand(guildCommand.Build(), ulong.Parse(configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command)
    {
        var guildUser = (SocketGuildUser)command.Data.Options.First().Value;

        var roleList = string.Join(",\n", guildUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
            .WithTitle("Roles")
            .WithDescription(roleList)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
