using Discord;
using Discord.WebSocket;
using DiscordBot.Audio;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Music
{
    public class QueueSlashCommand : ISlashCommands
    {
        public string CommandName => "queue";
        private readonly AudioService _audio;
        private readonly IConfiguration _configuration;

        public QueueSlashCommand(IConfiguration configuration, AudioService audio)
        {
            _configuration = configuration;
            _audio = audio;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var cmd = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Show the current music queue.");
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
            var queue = _audio.GetQueue(guildId);

            if (currentTrack == null && queue.Count == 0)
            {
                await command.RespondAsync("❌ No music is currently playing and the queue is empty.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎵 Music Queue")
                .WithColor(Color.Purple);

            if (currentTrack != null)
            {
                embed.AddField("🎶 Now Playing", $"**{currentTrack.Title}**\nRequested by: {currentTrack.RequestedBy}", false);
            }

            if (queue.Count > 0)
            {
                var queueList = queue.Take(10).Select((track, index) => 
                    $"{index + 1}. **{track.Title}** - {track.RequestedBy}").ToList();

                if (queue.Count > 10)
                {
                    queueList.Add($"... and {queue.Count - 10} more songs");
                }

                embed.AddField("📋 Up Next", string.Join("\n", queueList), false);
            }

            embed.WithFooter(footer => footer.Text = $"Total songs in queue: {queue.Count}");

            await command.RespondAsync(embed: embed.Build());
        }
    }
}
