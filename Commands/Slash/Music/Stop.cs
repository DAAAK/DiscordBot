using Discord;
using Discord.WebSocket;
using DiscordBot.Audio;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Music
{
    public class StopSlashCommand : ISlashCommands
    {
        public string CommandName => "stop";
        private readonly AudioService _audio;
        private readonly IConfiguration _configuration;

        public StopSlashCommand(IConfiguration configuration, AudioService audio)
        {
            _configuration = configuration;
            _audio = audio;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var cmd = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Stop playing music and clear the queue.");
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

            try
            {
                await _audio.StopAsync(guildId);

                var embed = new EmbedBuilder()
                    .WithTitle("⏹️ Music Stopped")
                    .WithDescription("Music playback has been stopped and the queue has been cleared.")
                    .WithColor(Color.Red)
                    .WithFooter(footer => footer.Text = $"Stopped by {user.Username}")
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
