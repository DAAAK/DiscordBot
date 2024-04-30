using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class Movement : IMovement
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _channelId;
    private readonly IConfiguration _configuration;


    public Movement(DiscordSocketClient client, IConfiguration configuration, ulong channelId)
    {
        _configuration = configuration;
        _client = client;
        _channelId = channelId;

        _client.UserJoined += HandleUserJoined;
        _client.UserLeft += HandleUserLeft;
    }

    public async Task HandleUserJoined(SocketGuildUser user)
    {
        try
        {
            var guild = user.Guild;
            var channel = guild.GetTextChannel(_channelId);

            if (channel != null)
            {
                var embed = new EmbedBuilder()
                .WithImageUrl(_configuration["JoiningImage"])
                .WithColor(Color.Green)
                .WithTitle("Welcome !")
                .WithDescription($"Welcome {user.Mention} to the server !")
                .Build();

                await channel.SendMessageAsync(embed: embed);
            }
            Console.WriteLine($"User {user.Username} joined the server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling UserJoined event: {ex}");
        }
    }

    public async Task HandleUserLeft(SocketGuild guild, SocketUser user)
    {
        try
        {
            var channel = guild.GetTextChannel(_channelId);

            if (channel != null)
            {
                var embed = new EmbedBuilder()
               .WithImageUrl(_configuration["LeavingImage"])
               .WithColor(Color.Red)
               .WithTitle("Bye !")
               .WithDescription($"Goodbye {user.Mention} !")
               .Build();

                await channel.SendMessageAsync(embed: embed);
            }
            Console.WriteLine($"User {user.Username} left the server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling UserLeft event: {ex}");
        }
    }
}
