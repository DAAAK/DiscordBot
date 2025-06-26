using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class UpdateStreamerSlashCommand : ISlashCommands
{
    public string CommandName => "update-streamer";

    private readonly DatabaseService _db;
    private readonly IConfiguration _configuration;

    public UpdateStreamerSlashCommand(IConfiguration config, DatabaseService db)
    {
        _db = db;
        _configuration = config;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var cmd = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Update a streamer's Twitch username.")
            .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
            .AddOption("twitch", ApplicationCommandOptionType.String, "New Twitch username", true);

        await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {

        var roleChecker = new RequiredRoles(_configuration);

        if (!roleChecker.HasRequiredRole(executor))
        {
            var embedBuilder = new EmbedBuilder()
                    .WithTitle("Permission Refusée")
                    .WithDescription("Vous n'avez pas la permission d'utiliser cette commande.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        var user = (SocketGuildUser)command.Data.Options.First(o => o.Name == "user").Value;
        var twitch = command.Data.Options.First(o => o.Name == "twitch").Value.ToString();

        bool success = await _db.UpdateStreamerAsync(user.Id, twitch);

        await command.RespondAsync(success
            ? $"🔁 Updated {user.Mention} to **{twitch}**."
            : $"❌ No streamer entry found to update.", ephemeral: true);
    }
}
