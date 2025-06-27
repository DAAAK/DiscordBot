using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordBot.Audio;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Music
{
    public class PlaySlashCommand : ISlashCommands
    {
        public string CommandName => "play";
        private readonly AudioService _audio;

        private readonly IConfiguration _configuration;

        public PlaySlashCommand(IConfiguration configuration, AudioService audio)
        {
            _configuration = configuration;
            _audio = audio;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var cmd = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Play a song by URL.")
                .AddOption("url", ApplicationCommandOptionType.String, "YouTube URL or MP3", isRequired: true);
            await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var url = command.Data.Options.First().Value.ToString();
            var user = (SocketGuildUser)command.User;

            if (user.VoiceChannel == null)
            {
                await command.RespondAsync("❌ You must be in a voice channel to use this command.");
                return;
            }


            try
            {

                await _audio.JoinAndPlayAsync(user, url!);

                await command.RespondAsync($"🎶 Playing: {url}");

            }
            catch (Exception ex)
            {

                await command.RespondAsync($"❌ {ex.Message}");
            }
        }
    }
}
