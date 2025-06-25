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
    public class UseGifSlashCommand : ISlashCommands
    {
        public string CommandName => "gif";

        private readonly IConfiguration _configuration;
        private readonly DatabaseService _db;

        public UseGifSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Send a GIF by name.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("name")
                    .WithDescription("Name of the GIF")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true)
                    .WithAutocomplete(true));

            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var name = command.Data.Options.First(o => o.Name == "name").Value.ToString();

            var gifs = await _db.GetGifsAsync();
            var gif = gifs.FirstOrDefault(g => g.Name.ToLower() == name.ToLower());

            if (gif.Equals(default((string, string))))
            {
                await command.RespondAsync($"❌ No GIF found with the name: **{name}**.", ephemeral: true);
            }
            else
            {
                await command.RespondAsync(gif.Url);
            }
        }

        public async Task HandleAutocomplete(SocketAutocompleteInteraction interaction)
        {
            if (interaction.Data.Options.FirstOrDefault()?.Name == "name")
            {
                var gifs = await _db.GetGifsAsync();
                var search = interaction.Data.Current.Value?.ToString()?.ToLower() ?? "";

                var suggestions = gifs
                    .Where(g => g.Name.ToLower().Contains(search))
                    .Take(20)
                    .Select(g => new AutocompleteResult(g.Name, g.Name));

                await interaction.RespondAsync(suggestions);
            }
        }

    }
}
