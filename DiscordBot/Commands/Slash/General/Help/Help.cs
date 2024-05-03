using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class HelpSlashCommand : ISlashCommands
{
    public string CommandName => "help";

    private readonly IConfiguration _configuration;

    public HelpSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName("help")
            .WithDescription("Lists all the available commands.");

        await client.Rest.CreateGuildCommand(guildCommand.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var commandsSection = _configuration.GetSection("Commands");

        Dictionary<string, string> commandDescriptions = commandsSection.GetChildren()
            .ToDictionary(command => command.Key, command => command.Value);

        var embedBuilder = new EmbedBuilder()
        .WithTitle("Here are the commands you can use")
        .WithColor(Color.Green)
        .WithCurrentTimestamp();

        foreach (var (commandName, description) in commandDescriptions)
        {
            embedBuilder.AddField(commandName, description, inline: false);
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
