using DiscordBot.Database;
using Microsoft.Data.SqlClient;
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
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddScoped<IBot, Bot>()
            .AddSingleton<DatabaseService>()
            .BuildServiceProvider();

        var db = serviceProvider.GetRequiredService<DatabaseService>();
        try
        {
            using var testConn = new SqlConnection(configuration.GetConnectionString("Default"));
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
