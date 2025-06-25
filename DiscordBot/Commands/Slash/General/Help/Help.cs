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
            .WithTitle("📖 Here are the commands you can use")
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        int fieldLimit = 25;
        int count = 0;

        foreach (var (commandName, description) in commandDescriptions)
        {
            if (count >= fieldLimit)
                break;

            embedBuilder.AddField($"`/{commandName}`", description, inline: true);
            count++;
        }

        var helpEmbed = embedBuilder.Build();

        if (!ulong.TryParse(_configuration["CommandsChannelID"], out var commandsChannelId))
        {
            await command.RespondAsync("⚠️ Command channel not configured properly.", ephemeral: true);
            return;
        }

        var guild = (command.Channel as SocketGuildChannel)?.Guild;
        var commandsChannel = guild?.GetTextChannel(commandsChannelId);

        if (commandsChannel == null)
        {
            await command.RespondAsync("❌ Unable to find the commands channel.", ephemeral: true);
            return;
        }

        var existing = await commandsChannel.GetMessagesAsync(10).FlattenAsync();
        var alreadyPosted = existing.FirstOrDefault(m => m.Author.Id == client.CurrentUser.Id && m.Embeds.Any(e => e.Title?.Contains("commands") == true));

        if (alreadyPosted == null)
        {
            await commandsChannel.SendMessageAsync(embed: helpEmbed);
        }

        if (command.Channel.Id != commandsChannelId)
        {
            await command.RespondAsync($"ℹ️ I've posted the help in {commandsChannel.Mention}.", ephemeral: true);
        }
        else
        {
            await command.RespondAsync(embed: helpEmbed, ephemeral: true);
        }
    }


}
