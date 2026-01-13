using Discord;
using Discord.WebSocket;
using DiscordBot.Audio;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Music
{
    public class SkipSlashCommand : ISlashCommands
    {
        public string CommandName => "skip";
        private readonly AudioService _audio;
        private readonly IConfiguration _configuration;

        public SkipSlashCommand(IConfiguration configuration, AudioService audio)
        {
            _configuration = configuration;
            _audio = audio;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var cmd = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Skip the currently playing song.");
            await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var guildId = ((SocketGuildChannel)command.Channel).Guild.Id;
            var user = (SocketGuildUser)command.User;

            if (user.VoiceChannel == null)
            {
                await command.RespondAsync("❌ You must be in a voice channel to use this command.");
                return;
            }

            if (!_audio.IsConnected(guildId))
            {
                await command.RespondAsync("❌ I'm not currently playing music in this server.");
                return;
            }

            var currentTrack = _audio.GetCurrentTrack(guildId);
            if (currentTrack == null)
            {
                await command.RespondAsync("❌ No music is currently playing.");
                return;
            }

            try
            {
                var skippedTrack = currentTrack;
                await _audio.SkipAsync(guildId);

                var embed = new EmbedBuilder()
                    .WithTitle("⏭️ Song Skipped")
                    .WithDescription($"**{skippedTrack.Title}**")
                    .WithColor(Color.Blue)
                    .WithFooter(footer => footer.Text = $"Skipped by {user.Username}")
                    .Build();

                await command.RespondAsync(embed: embed);
            }
            catch (Exception ex)
            {
                await command.RespondAsync($"❌ Error: {ex.Message}");
            }
        }
    }
}
