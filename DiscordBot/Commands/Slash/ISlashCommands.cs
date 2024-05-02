using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public interface ISlashCommands
{
    string CommandName { get; }

    Task RegisterCommandsAsync(DiscordSocketClient client);
    Task HandleCommand(SocketSlashCommand command);
}
