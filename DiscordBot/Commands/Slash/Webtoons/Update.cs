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
    public class UpdateWebtoonSlashCommand : ISlashCommands
    {
        public string CommandName => "update-webtoon";

        private readonly IConfiguration _config;
        private readonly DatabaseService _db;

        public UpdateWebtoonSlashCommand(IConfiguration config, DatabaseService db)
        {
            _config = config;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var guildId = ulong.Parse(_config["GuildID"]);

            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Update a webtoon's chapter and status.")
                .AddOption("name", ApplicationCommandOptionType.String, "Webtoon name", true)
                .AddOption("chapter", ApplicationCommandOptionType.Integer, "New chapter", true)
                .AddOption("status", ApplicationCommandOptionType.String, "New status", true);

            await client.Rest.CreateGuildCommand(command.Build(), guildId);
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var name = command.Data.Options.First(o => o.Name == "name").Value.ToString();
            var chapter = Convert.ToInt32(command.Data.Options.First(o => o.Name == "chapter").Value);
            var status = command.Data.Options.First(o => o.Name == "status").Value.ToString();

            await _db.UpdateWebtoonAsync(name, chapter, status);
            await command.RespondAsync($"♻️ Updated **{name}** to Chapter {chapter} with status '{status}'.");
        }
    }

}
