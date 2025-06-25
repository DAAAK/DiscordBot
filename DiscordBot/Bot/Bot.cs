using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Audio;
using DiscordBot.Commands.Slash.Music;
using DiscordBot.Database;
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
    private readonly Dictionary<ulong, DateTime> _xpCooldowns = new();
    private readonly TimeSpan _xpCooldownDuration = TimeSpan.FromMinutes(2);

    public Bot(IConfiguration configuration)
    {
        _configuration = configuration;

        DiscordSocketConfig config = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildVoiceStates | GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions | GatewayIntents.DirectMessageTyping | GatewayIntents.GuildVoiceStates,
        };

        _client = new DiscordSocketClient(config);
        _prefixCommands = new CommandService();

        _client.Ready += OnClientReady;
        _client.MessageReceived += PrefixCommandHandler;
        _client.UserJoined += OnUserJoined;
        _client.SlashCommandExecuted += SlashCommandHandler;
        _client.AutocompleteExecuted += AutocompleteHandler;

        _ = new Movement(_client, _configuration);

        _slashCommands = new List<ISlashCommands>
        {
            new ListRolesSlashCommand(_configuration),
            new BanSlashCommand(_configuration),
            new KickSlashCommand(_configuration),
            new PurgeSlashCommand(_configuration),
            new PollSlashCommand(_configuration),
            new DiceSlashCommand(_configuration),
            new CoinFlipSlashCommand(_configuration),
            new RPSSlashCommand(_configuration),
            new HangmanSlashCommand(_configuration),
        };
    }

    public async Task StartAsync(ServiceProvider services)
    {
        _serviceProvider = services;

        string discordToken = _configuration["DiscordToken"] ?? throw new Exception("Missing Discord token");
        var client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
        var db = _serviceProvider.GetRequiredService<DatabaseService>();


        _client.MessageReceived += async (message) =>
        {
            if (message.Author.IsBot || message.Channel is not SocketTextChannel textChannel) return;

            if (textChannel.Guild.Id != ulong.Parse(_configuration["GuildID"]))
                return;

            if (_xpCooldowns.TryGetValue(message.Author.Id, out DateTime lastTime))
            {
                if (DateTime.UtcNow - lastTime < _xpCooldownDuration)
                    return;
            }

            _xpCooldowns[message.Author.Id] = DateTime.UtcNow;

            if (message.Author is not SocketGuildUser user) return;

            var db = _serviceProvider!.GetRequiredService<DatabaseService>();
            var (leveledUp, newLevel, newXP) = await db.AddXPAsync(user.Id, user.DisplayName, xpToAdd: 5);

            if (leveledUp)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("🎉 Level Up!")
                    .WithDescription($"{user.Mention} has reached **Level {newLevel}**!")
                    .WithColor(Color.Gold)
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .WithFooter(footer => footer.Text = $"XP: {newXP}")
                    .Build();

                var channelId = ulong.Parse(_configuration["LevelChannelID"]);
                var channel = _client.GetChannel(channelId) as IMessageChannel;

                if (channel != null)
                    await channel.SendMessageAsync(embed: embed);
            }
        };


        _slashCommands.AddRange(new List<ISlashCommands>
        {
            
            new AddGifSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new UpdateGifSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new DeleteGifSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new UseGifSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),

            new AddWebtoonSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new UpdateWebtoonSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new DeleteWebtoonSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new ListWebtoonsSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),

            new ShowXpSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new ListXpSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new AddXpSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new UpdateXpSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),

            new AddCommandSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new UpdateCommandSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new DeleteCommandSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),
            new ListCommandsSlashCommand(_configuration, _serviceProvider.GetRequiredService<DatabaseService>()),

            new PlaySlashCommand(_configuration, _serviceProvider.GetRequiredService<AudioService>())
        });

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
        Console.WriteLine("OnClientReady triggered");

        var db = _serviceProvider!.GetRequiredService<DatabaseService>();

        string targetedGuild = _configuration["GuildID"] ?? throw new Exception("Missing Discord token");
        ulong guildId = ulong.Parse(targetedGuild);
        var guild = _client.GetGuild(guildId);

        if (guild == null)
        {
            Console.WriteLine($"Bot is not in guild with ID: {guildId}");
            return Task.CompletedTask;
        }

        await guild.DownloadUsersAsync();


        foreach (var user in guild.Users)
        {
            if (!user.IsBot)
            {
                Console.WriteLine($"Registering user {user.Username} ({user.Id})");
                await db.AddXPAsync(user.Id, user.DisplayName, 0);
            }
        }

        foreach (var module in _slashCommands)
        {
            Console.WriteLine($"Registering command: {module.CommandName}");
            await module.RegisterCommandsAsync(_client);
        }

        return Task.CompletedTask;
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        if (user.IsBot) return;

        var db = _serviceProvider!.GetRequiredService<DatabaseService>();

        Console.WriteLine($"👋 New user joined: {user.Username} ({user.Id})");

        await db.AddXPAsync(user.Id, user.Username, 0);
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


    private async Task AutocompleteHandler(SocketAutocompleteInteraction interaction)
    {
        var module = _slashCommands.FirstOrDefault(m =>
            string.Equals(m.CommandName, interaction.Data.CommandName, StringComparison.OrdinalIgnoreCase));

        if (module is UseGifSlashCommand gifModule)
        {
            await gifModule.HandleAutocomplete(interaction);
        }
    }
}
