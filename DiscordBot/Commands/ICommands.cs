using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public interface ICommands
{
    string CommandName { get; }

    Task RegisterCommandsAsync(DiscordSocketClient client, IConfiguration configuration);
    Task HandleCommand(SocketSlashCommand command);
}
