using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class RequiredRoles
{
    private readonly IConfiguration _configuration;

    public RequiredRoles(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool HasRequiredRole(SocketGuildUser user)
    {
        if (_configuration == null || !_configuration.GetSection("RequiredRolesIDS").Exists())
            return false;

        var requiredRoleIds = _configuration.GetSection("RequiredRolesIDS")
                                            .GetChildren()
                                            .Select(x => ulong.Parse(x.Value))
                                            .ToArray();

        return user.Roles.Any(role => requiredRoleIds.Contains(role.Id));
    }
}
