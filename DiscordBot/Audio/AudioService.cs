using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DiscordBot.Audio
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();

        public async Task JoinAndPlayAsync(SocketGuildUser user, string url)
        {
            var guildId = user.Guild.Id;

            if (_connectedChannels.TryGetValue(guildId, out var existingClient))
            {
                await existingClient.StopAsync();
                _connectedChannels.TryRemove(guildId, out _);
            }

            if (!_connectedChannels.TryGetValue(guildId, out var client))
            {
                Console.WriteLine("🔌 Attempting voice channel connect...");

                try
                {
                    var connectTask = user.VoiceChannel.ConnectAsync();

                    if (await Task.WhenAny(connectTask, Task.Delay(7000)) == connectTask)
                    {
                        client = connectTask.Result;
                        Console.WriteLine("✅ Voice connected.");
                        _connectedChannels[guildId] = client;
                    }
                    else
                    {
                        Console.WriteLine("❌ Timeout while trying to connect to voice channel.");
                        throw new TimeoutException("Connection to voice channel timed out.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Exception during voice connect: {ex.GetType().Name} - {ex.Message}");
                    throw;
                }
            }

            using var output = CreateStream(url);


            output.Start();

            using var stream = client.CreatePCMStream(AudioApplication.Music);
            Console.WriteLine($"▶️ Streaming from: {url}");
            try
            {
                Console.WriteLine($"▶️ Streaming from: {url}");
                await output.StandardOutput.BaseStream.CopyToAsync(stream);
                Console.WriteLine("✅ Finished streaming audio.");
            }
            finally
            {
                await stream.FlushAsync();
                output.Dispose();
            }
        }

        private Process CreateStream(string youtubeUrl)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"\"C:\\Users\\daphi\\Downloads\\yt-dlp.exe\" -f bestaudio -o - \"{youtubeUrl}\" | " +
                                $"\"C:\\Users\\daphi\\ffmpeg\\ffmpeg-2025-06-23-git-e6298e0759-full_build\\bin\\ffmpeg.exe\" " +
                                "-hide_banner -loglevel error -i pipe:0 -ac 2 -ar 48000 -f s16le pipe:1\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }
    }
}
