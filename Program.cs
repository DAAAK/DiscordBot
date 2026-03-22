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

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddScoped<IBot, Bot>()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<AudioService>()
            .AddSingleton<TwitchTokenService>()
            .AddSingleton<DatabaseService>()
            .BuildServiceProvider();

        Console.WriteLine(configuration["DiscordToken"]);

        var db = serviceProvider.GetRequiredService<DatabaseService>();
        try
        {
            using var testConn = new NpgsqlConnection(configuration["DATABASE_URL"]);
            await testConn.OpenAsync();
            Console.WriteLine("Connected to SQL Server database.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
        }

        try
        {
            IBot bot = serviceProvider.GetRequiredService<IBot>();


            await bot.StartAsync(serviceProvider);

            Console.WriteLine("Connected to Discord");

            do
            {
                var keyInfo = Console.ReadKey();

                if (keyInfo.Key == ConsoleKey.Q)
                {
                    Console.WriteLine("\nShutting down!");

                    await bot.StopAsync();
                    return;
                }
            } while (true);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            Environment.Exit(-1);
        }
    }
}
