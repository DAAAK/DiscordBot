using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Webtoons
{
    public class DeleteWebtoonSlashCommand : ISlashCommands
    {
        public string CommandName => "delete-webtoon";

        private readonly IConfiguration _config;
        private readonly DatabaseService _db;

        public DeleteWebtoonSlashCommand(IConfiguration config, DatabaseService db)
        {
            _config = config;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var guildId = ulong.Parse(_config["GuildID"]);

            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Delete a webtoon by name.")
                .AddOption("name", ApplicationCommandOptionType.String, "Webtoon name", true);

            await client.Rest.CreateGuildCommand(command.Build(), guildId);
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var name = command.Data.Options.First(o => o.Name == "name").Value.ToString();
            await _db.DeleteWebtoonAsync(name);
            await command.RespondAsync($"🗑️ Deleted **{name}** from your webtoons.");
        }
    }
}
