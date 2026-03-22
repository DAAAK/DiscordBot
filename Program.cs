using Discord.WebSocket;
using DiscordBot.Audio;
using DiscordBot.Database;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public class Program
{
    private static void Main(string[] args) =>
        MainAsync(args).GetAwaiter().GetResult();

    private static async Task MainAsync(string[] args)
    {
        var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
    .AddEnvironmentVariables()
    .Build();

        Console.WriteLine($"DATABASE_URL exists: {!string.IsNullOrWhiteSpace(configuration["DATABASE_URL"])}");
        Console.WriteLine($"Default connection exists: {!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Default"))}");

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddScoped<IBot, Bot>()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<AudioService>()
            .AddSingleton<TwitchTokenService>()
            .AddSingleton<DatabaseService>()
            .BuildServiceProvider();

        var db = serviceProvider.GetRequiredService<DatabaseService>();
        var startupConnectionString =
    configuration["DATABASE_URL"]
    ?? configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(startupConnectionString))
            throw new InvalidOperationException("No database connection string found.");

        using var testConn = new NpgsqlConnection(startupConnectionString);
        await testConn.OpenAsync();
        Console.WriteLine("Connected to PostgreSQL database.");

        IBot bot = serviceProvider.GetRequiredService<IBot>();
        await bot.StartAsync(serviceProvider);

        Console.WriteLine("Connected to Discord");
        await Task.Delay(Timeout.Infinite);
    }
}
