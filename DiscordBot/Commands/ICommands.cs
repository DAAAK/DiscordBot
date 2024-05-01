
using Discord.WebSocket;

public interface ICommands
{
    public Task HandleCommand(SocketSlashCommand command);
}
