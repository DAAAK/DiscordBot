using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

public abstract class Commands : ICommands
{
    protected readonly DiscordSocketClient _client;
    protected readonly IConfiguration _configuration;

    public Commands(DiscordSocketClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public abstract Task HandleCommand(SocketSlashCommand command);

    protected async Task RegisterSlashCommand(SlashCommandBuilder commandBuilder)
    {
        var guildId = ulong.Parse(_configuration["GuildID"]);

        try
        {
            await _client.Rest.CreateGuildCommand(commandBuilder.Build(), guildId);
        }
        catch (ApplicationCommandException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
}
