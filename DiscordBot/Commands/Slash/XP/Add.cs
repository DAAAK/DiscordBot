using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

    public class AddXpSlashCommand : ISlashCommands
    {
        public string CommandName => "add-xp";
        private readonly DatabaseService _db;
        private readonly IConfiguration _configuration;

        public AddXpSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Ajoute de l'XP à un utilisateur.")
                .AddOption("user", ApplicationCommandOptionType.User, "Utilisateur ciblé", isRequired: true)
                .AddOption("amount", ApplicationCommandOptionType.Integer, "XP à ajouter", isRequired: true);

            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
        }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var executor = (SocketGuildUser)command.User;

        if (!HasRequiredRole(executor))
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
        var xpToAdd = (int)(long)command.Data.Options.First(o => o.Name == "amount").Value;

        var (leveledUp, newLevel, newXP) = await _db.AddXPAsync(user.Id, user.DisplayName, xpToAdd);

        string confirmation = $"✅ {user.Mention} a reçu **{xpToAdd} XP**";

        if (leveledUp)
        {
            ulong levelChannelId = ulong.Parse(_configuration["LevelChannelID"]);
            var levelChannel = client.GetChannel(levelChannelId) as IMessageChannel;

            if (levelChannel != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("🎉 Level Up!")
                    .WithDescription($"{user.Mention} has reached **Level {newLevel}**!")
                    .WithColor(Color.Gold)
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .WithFooter(footer => footer.Text = $"XP: {newXP}")
                    .Build();

                await levelChannel.SendMessageAsync(embed: embed);
            }
        }

        await command.RespondAsync(confirmation, ephemeral: true);
    }

    private bool HasRequiredRole(SocketGuildUser user)
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
