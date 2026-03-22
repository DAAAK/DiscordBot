using Discord.WebSocket;
using DiscordBot.Audio;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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

        try
        {
            var startupConnectionString = BuildPostgresConnectionString(configuration);

            using var testConn = new NpgsqlConnection(startupConnectionString);
            await testConn.OpenAsync();
            Console.WriteLine("Connected to PostgreSQL database.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
            Environment.Exit(-1);
            return;
        }

        try
        {
            IBot bot = serviceProvider.GetRequiredService<IBot>();

            await bot.StartAsync(serviceProvider);

            Console.WriteLine("Connected to Discord");

            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            Environment.Exit(-1);
        }
    }

    public static string BuildPostgresConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':', 2);

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port,
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
                Database = uri.AbsolutePath.Trim('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };

            return builder.ConnectionString;
        }

        var fallback = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback;

        throw new InvalidOperationException("No database connection string found.");
    }
}