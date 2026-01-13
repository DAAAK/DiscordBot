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
            .WithDescription("Play a song by URL. The bot will automatically join your voice channel.")
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
            await command.DeferAsync();
            
            if (!_audio.IsConnected(user.Guild.Id))
            {
                await command.FollowupAsync("🔌 Connecting to your voice channel...");
                await _audio.JoinAsync(user);
            }
            
            await command.FollowupAsync("🎵 Adding song to queue...");
            await _audio.PlayAsync(user, url!);

            var embed = new EmbedBuilder()
                .WithTitle("🎵 Added to Queue")
                .WithDescription($"**{url}**")
                .WithColor(Color.Green)
                .WithFooter(footer => footer.Text = $"Requested by {user.Username}")
                .Build();

            await command.FollowupAsync(embed: embed);
        }
        catch (InvalidOperationException ex)
        {
            await command.FollowupAsync($"❌ **Connection Error:** {ex.Message}\n\n**Troubleshooting:**\n• Make sure you're in a voice channel\n• Check if the bot has permission to join and speak\n• Try moving to a different voice channel");
        }
        catch (TimeoutException ex)
        {
            await command.FollowupAsync($"⏰ **Timeout Error:** {ex.Message}\n\n**Troubleshooting:**\n• Check your internet connection\n• Try again in a few seconds\n• Make sure the voice channel isn't full");
        }
        catch (Exception ex)
        {
            await command.FollowupAsync($"❌ **Unexpected Error:** {ex.Message}\n\nPlease try again or contact an administrator if the problem persists.");
        }
    }
}
