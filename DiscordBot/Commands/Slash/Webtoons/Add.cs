using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Webtoons
{
    using Discord;
    using Discord.WebSocket;
    using DiscordBot.Database;
    using Microsoft.Extensions.Configuration;

    public class AddWebtoonSlashCommand : ISlashCommands
    {
        public string CommandName => "add-webtoon";

        private readonly IConfiguration _configuration;
        private readonly DatabaseService _db;

        public AddWebtoonSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Add a new webtoon.")
                .AddOption("name", ApplicationCommandOptionType.String, "Webtoon name", true)
                .AddOption("chapter", ApplicationCommandOptionType.Integer, "Current chapter", true)
                .AddOption("status", ApplicationCommandOptionType.String, "Webtoon status", true);

            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var name = command.Data.Options.First(o => o.Name == "name").Value.ToString();
            var chapter = Convert.ToInt32(command.Data.Options.First(o => o.Name == "chapter").Value);
            var status = command.Data.Options.First(o => o.Name == "status").Value.ToString();

            await _db.AddWebtoonAsync(name, chapter, status);

            await command.RespondAsync($"✅ Webtoon **{name}** added.");
        }
    }

}
