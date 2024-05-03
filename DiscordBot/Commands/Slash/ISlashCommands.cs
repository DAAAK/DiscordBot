using Discord.WebSocket;

public interface ISlashCommands
{
    string CommandName { get; }

    Task RegisterCommandsAsync(DiscordSocketClient client);
    Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client);
}
