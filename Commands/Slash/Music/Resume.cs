using Discord;
using Discord.WebSocket;
using DiscordBot.Audio;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Music
{
    public class ResumeSlashCommand : ISlashCommands
    {
        public string CommandName => "resume";
        private readonly AudioService _audio;
        private readonly IConfiguration _configuration;

        public ResumeSlashCommand(IConfiguration configuration, AudioService audio)
        {
            _configuration = configuration;
            _audio = audio;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var cmd = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Resume the paused music.");
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

            if (!_audio.IsPaused(guildId))
            {
                await command.RespondAsync("▶️ Music is not paused.");
                return;
            }

            try
            {
                await _audio.ResumeAsync(guildId);

                var embed = new EmbedBuilder()
                    .WithTitle("▶️ Music Resumed")
                    .WithDescription($"**{currentTrack.Title}**")
                    .WithColor(Color.Green)
                    .WithFooter(footer => footer.Text = $"Requested by {currentTrack.RequestedBy}")
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
