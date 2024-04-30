using Discord.WebSocket;

public class Movement : IMovement
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _channelId;

    public Movement(DiscordSocketClient client, ulong channelId)
    {
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
                await channel.SendMessageAsync($"Welcome {user.Mention} to the server!");
            }
            Console.WriteLine($"User {user.Mention} joined the server");
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
                await channel.SendMessageAsync($"Goodbye {user.Mention}! We'll miss you.");
            }
            Console.WriteLine($"User {user.Mention} left the server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling UserLeft event: {ex}");
        }
    }
}
