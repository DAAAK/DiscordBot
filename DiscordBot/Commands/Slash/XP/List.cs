using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.NewFolder1
{
    public class ListXpSlashCommand : ISlashCommands
    {
        public string CommandName => "list-xp";

        private readonly IConfiguration _configuration;
        private readonly DatabaseService _db;
        public ListXpSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Classement des utilisateurs par XP.");
            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var leaderboard = await _db.GetLeaderboardAsync();
            if (leaderboard.Count == 0)
            {
                await command.RespondAsync("Aucun utilisateur n'a encore d'XP.");
                return;
            }

            var guild = (command.Channel as SocketGuildChannel)?.Guild;
            if (guild == null)
            {
                await command.RespondAsync("❌ Impossible de récupérer le serveur.");
                return;
            }

            var sb = new StringBuilder("🏆 **Classement XP**\n");
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var (userId, xp) = leaderboard[i];
                var user = guild.GetUser(userId);
                var displayName = user?.DisplayName ?? $"<Inconnu {userId}>";

                sb.AppendLine($"{i + 1}. **{displayName}** — {xp} XP");
            }

            await command.RespondAsync(sb.ToString());
        }
    }

}
