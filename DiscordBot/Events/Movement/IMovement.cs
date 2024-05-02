using Discord.WebSocket;

public interface IMovement
{
    Task HandleUserJoined(SocketGuildUser user);
    Task HandleUserLeft(SocketGuild guild, SocketUser user);
}
