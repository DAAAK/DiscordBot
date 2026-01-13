using Discord;
using Discord.WebSocket;
using DiscordBot.Audio;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Music
{
    public class LeaveSlashCommand : ISlashCommands
    {
        public string CommandName => "leave";
        private readonly AudioService _audio;
        private readonly IConfiguration _configuration;

        public LeaveSlashCommand(IConfiguration configuration, AudioService audio)
        {
            _configuration = configuration;
            _audio = audio;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var cmd = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Leave the voice channel and stop playing music.");
            await client.Rest.CreateGuildCommand(cmd.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var guildId = ((SocketGuildChannel)command.Channel).Guild.Id;
            var user = (SocketGuildUser)command.User;

            if (!_audio.IsConnected(guildId))
            {
                await command.RespondAsync("❌ I'm not currently in a voice channel.");
                return;
            }

            try
            {
                await _audio.LeaveAsync(guildId);

                var embed = new EmbedBuilder()
                    .WithTitle("👋 Left Voice Channel")
                    .WithDescription("Left the voice channel and stopped playing music.")
                    .WithColor(Color.Blue)
                    .WithFooter(footer => footer.Text = $"Requested by {user.Username}")
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
