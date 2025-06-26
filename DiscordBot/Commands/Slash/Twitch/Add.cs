using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;

public class AddStreamerSlashCommand : ISlashCommands
{
    public string CommandName => "add-streamer";

    private readonly DatabaseService _db;
    private readonly IConfiguration _configuration;

    public AddStreamerSlashCommand(IConfiguration config, DatabaseService db)
    {
        _db = db;
        _configuration = config;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var cmd = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Link a Twitch account to a Discord user.")
            .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
            .AddOption("twitch", ApplicationCommandOptionType.String, "Twitch username", true);

        await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var executor = (SocketGuildUser)command.User;

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
        var twitchOption = command.Data.Options.FirstOrDefault(o => o.Name == "twitch");
        var twitch = twitchOption?.Value?.ToString();

        if (string.IsNullOrWhiteSpace(twitch))
        {
            await command.RespondAsync("❌ Twitch username cannot be null or empty.", ephemeral: true);
            return;
        }

        bool success = await _db.AddStreamerAsync(user.Id, twitch);

        await command.RespondAsync(success
            ? $"✅ Linked {user.Mention} to Twitch **{twitch}**."
            : $"❌ Failed to add streamer (maybe already added).", ephemeral: true);
    }
}
