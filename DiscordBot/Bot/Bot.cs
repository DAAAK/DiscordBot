using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public class Bot : IBot
{
    private ServiceProvider? _serviceProvider;

    private readonly IConfiguration _configuration;
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;

    public Bot(IConfiguration configuration)
    {
        _configuration = configuration;

        DiscordSocketConfig config = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildVoiceStates | GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions | GatewayIntents.DirectMessageTyping,
        };

        _client = new DiscordSocketClient(config);
        _commands = new CommandService();

        _client.Ready += OnClientReady;
        _client.MessageReceived += HandleCommandAsync;

        _ = new Movement(_client, ulong.Parse(_configuration["MovementChannel"]));
    }

    public async Task StartAsync(ServiceProvider services)
    {
        string discordToken = _configuration["DiscordToken"] ?? throw new Exception("Missing Discord token");

        _serviceProvider = services;

        await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);

        await _client.LoginAsync(TokenType.Bot, discordToken);

        await _client.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_client != null)
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }
    }

    private Task OnClientReady()
    {
        Console.WriteLine($"Hello from {_client.CurrentUser.Username} !" ?? "");
        return Task.CompletedTask;
    }

    private async Task HandleCommandAsync(SocketMessage arg)
    {
        if (arg is not SocketUserMessage message || message.Author.IsBot)
        {
            return;
        }

        int position = 0;
        bool messageIsCommand = message.HasCharPrefix(_configuration["Prefix"][0], ref position);

        if (messageIsCommand)
        {
            await _commands.ExecuteAsync(
                new SocketCommandContext(_client, message),
                position,
                _serviceProvider);

            return;
        }
    }
}
