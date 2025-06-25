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
    public class ShowXpSlashCommand : ISlashCommands
    {
        public string CommandName => "show-xp";
        private readonly DatabaseService _db;
        private readonly IConfiguration _configuration;

        public ShowXpSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }


        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            Console.WriteLine($"🔧 Registering command: {CommandName}");

            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Affiche ton XP.");
            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));

        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            int xp = await _db.GetUserXPAsync(command.User.Id);
            int level = await _db.GetUserLevelAsync(command.User.Id);
            var (_, currentXP, nextLevelXP, xpRemaining) = _db.GetXPStats(xp, level);

            var embed = new EmbedBuilder()
                .WithTitle($"📊 Statistiques XP pour {command.User.Username}")
                .WithColor(Color.DarkPurple)
                .AddField("🔢 Niveau", level, true)
                .AddField("💠 XP actuel", currentXP, true)
                .AddField("🆙 XP restant", xpRemaining, true)
                .Build();

            await command.RespondAsync(embed: embed, ephemeral: true);
        }
    }

}
