using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.NewFolder
{
    public class AddGifSlashCommand : ISlashCommands
    {
        public string CommandName => "add-gif";

        private readonly IConfiguration _configuration;
        private readonly DatabaseService _db;

        public AddGifSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Add a new GIF.")
                .AddOption("name", ApplicationCommandOptionType.String, "GIF name", true)
                .AddOption("url", ApplicationCommandOptionType.String, "GIF URL", true);

            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var name = command.Data.Options.First(o => o.Name == "name").Value.ToString();
            var url = command.Data.Options.First(o => o.Name == "url").Value.ToString();

            await _db.AddGifAsync(name, url);

            await command.RespondAsync($"✅ GIF **{name}** added.", ephemeral: true);
        }
    }
}
