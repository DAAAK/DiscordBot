using Discord;
using Discord.WebSocket;
using DiscordBot.Audio;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Music
{
    public class NowPlayingSlashCommand : ISlashCommands
    {
        public string CommandName => "nowplaying";
        private readonly AudioService _audio;
        private readonly IConfiguration _configuration;

        public NowPlayingSlashCommand(IConfiguration configuration, AudioService audio)
        {
            _configuration = configuration;
            _audio = audio;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var cmd = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Show information about the currently playing song.");
            await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var guildId = ((SocketGuildChannel)command.Channel).Guild.Id;

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

            var status = _audio.IsPaused(guildId) ? "⏸️ Paused" : "▶️ Playing";
            var color = _audio.IsPaused(guildId) ? Color.Orange : Color.Green;

            var embed = new EmbedBuilder()
                .WithTitle("🎵 Now Playing")
                .WithDescription($"**{currentTrack.Title}**")
                .AddField("Status", status, true)
                .AddField("Requested By", currentTrack.RequestedBy, true)
                .AddField("URL", currentTrack.Url, false)
                .WithColor(color)
                .WithFooter(footer => footer.Text = "Music Bot")
                .Build();

            await command.RespondAsync(embed: embed);
        }
    }
}
