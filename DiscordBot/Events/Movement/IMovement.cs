using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

public interface IMovement
{
    Task HandleUserJoined(SocketGuildUser user);
    Task HandleUserLeft(SocketGuild guild, SocketUser user);
}
