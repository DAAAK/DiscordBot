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
    private readonly CommandService _prefixCommands;
    private readonly List<ISlashCommands> _slashCommands;


    public Bot(IConfiguration configuration)
    {
        _configuration = configuration;

        DiscordSocketConfig config = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildVoiceStates | GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions | GatewayIntents.DirectMessageTyping,
        };

        _client = new DiscordSocketClient(config);
        _prefixCommands = new CommandService();

        _client.Ready += OnClientReady;
        _client.MessageReceived += PrefixCommandHandler;
        _client.SlashCommandExecuted += SlashCommandHandler;

        _ = new Movement(_client, _configuration);

        _slashCommands = new List<ISlashCommands>
        {
            new ListRolesSlashCommand(_configuration),
            new BanSlashCommand(_configuration),
            new KickSlashCommand(_configuration),
            new PurgeSlashCommand(_configuration),
            new HelpSlashCommand(_configuration),
            // Add more command modules here for each command
        };
    }

    public async Task StartAsync(ServiceProvider services)
    {
        string discordToken = _configuration["DiscordToken"] ?? throw new Exception("Missing Discord token");

        _serviceProvider = services;

        await _prefixCommands.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);

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

    private async Task<Task> OnClientReady()
    {
        Console.WriteLine($"Hello from {_client.CurrentUser.Username} !" ?? "");

        foreach (var module in _slashCommands)
        {
            await module.RegisterCommandsAsync(_client);
        }

        return Task.CompletedTask;
    }

    private async Task PrefixCommandHandler(SocketMessage arg)
    {
        if (arg is not SocketUserMessage message || message.Author.IsBot)
        {
            return;
        }

        int position = 0;
        bool messageIsCommand = message.HasCharPrefix(_configuration["Prefix"][0], ref position);

        if (messageIsCommand)
        {
            await _prefixCommands.ExecuteAsync(
                new SocketCommandContext(_client, message),
                position,
                _serviceProvider);

            return;
        }
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        var module = _slashCommands.FirstOrDefault(m =>
                string.Equals(m.CommandName, command.Data.Name, StringComparison.OrdinalIgnoreCase));

        if (module != null)
        {
            await module.HandleCommand(command, _client);

        }
    }
}
