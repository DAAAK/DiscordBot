using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DiscordBot.Audio
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();
        private readonly ConcurrentDictionary<ulong, Queue<AudioTrack>> _musicQueues = new();
        private readonly ConcurrentDictionary<ulong, AudioTrack> _currentTracks = new();
        private readonly ConcurrentDictionary<ulong, bool> _isPaused = new();
        private readonly ConcurrentDictionary<ulong, Process> _currentProcesses = new();
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _pauseTokens = new();

        public class AudioTrack
        {
            public string Title { get; set; } = "";
            public string Url { get; set; } = "";
            public string RequestedBy { get; set; } = "";
            public TimeSpan Duration { get; set; }
        }

        public async Task JoinAsync(SocketGuildUser user)
        {
            var guildId = user.Guild.Id;

            if (_connectedChannels.TryGetValue(guildId, out var existingClient))
            {
                return; // Already connected
            }

            Console.WriteLine("🔌 Attempting voice channel connect...");

            try
            {
                var connectTask = user.VoiceChannel.ConnectAsync();

                if (await Task.WhenAny(connectTask, Task.Delay(7000)) == connectTask)
                {
                    var client = connectTask.Result;
                    Console.WriteLine("✅ Voice connected.");
                    _connectedChannels[guildId] = client;
                    _musicQueues[guildId] = new Queue<AudioTrack>();
                    _isPaused[guildId] = false;
                    _pauseTokens[guildId] = new CancellationTokenSource();
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

        public async Task PlayAsync(SocketGuildUser user, string url)
        {
            var guildId = user.Guild.Id;

            // Ensure we're connected (this should be called after JoinAsync from the command)
            if (!_connectedChannels.TryGetValue(guildId, out var client))
            {
                throw new InvalidOperationException("Bot is not connected to a voice channel. Please use the play command which will automatically join.");
            }

            // Create track info
            var track = new AudioTrack
            {
                Title = await GetVideoTitleAsync(url),
                Url = url,
                RequestedBy = user.Username,
                Duration = TimeSpan.Zero // Could implement duration detection later
            };

            // Add to queue
            if (!_musicQueues.ContainsKey(guildId))
                _musicQueues[guildId] = new Queue<AudioTrack>();

            _musicQueues[guildId].Enqueue(track);

            // If nothing is currently playing, start playing
            if (!_currentTracks.ContainsKey(guildId) || _currentTracks[guildId] == null)
            {
                await PlayNextAsync(guildId);
            }
        }

        public async Task PlayNextAsync(ulong guildId)
        {
            if (!_connectedChannels.TryGetValue(guildId, out var client))
                return;

            if (!_musicQueues.ContainsKey(guildId) || _musicQueues[guildId].Count == 0)
            {
                _currentTracks.TryRemove(guildId, out _);
                return;
            }

            var track = _musicQueues[guildId].Dequeue();
            _currentTracks[guildId] = track;
            _isPaused[guildId] = false;

            // Stop current process if any
            if (_currentProcesses.TryGetValue(guildId, out var currentProcess))
            {
                currentProcess.Kill();
                _currentProcesses.TryRemove(guildId, out _);
            }

            try
            {
                var process = CreateStream(track.Url);
                _currentProcesses[guildId] = process;

                process.Start();
                var stream = client.CreatePCMStream(AudioApplication.Music);

                await process.StandardOutput.BaseStream.CopyToAsync(stream);

                // Play next track when current one finishes
                await PlayNextAsync(guildId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing track: {ex.Message}");
                await PlayNextAsync(guildId);
            }
        }

        public async Task PauseAsync(ulong guildId)
        {
            if (_currentProcesses.TryGetValue(guildId, out var process))
            {
                process.Kill();
                _currentProcesses.TryRemove(guildId, out _);
                _isPaused[guildId] = true;
            }
        }

        public async Task ResumeAsync(ulong guildId)
        {
            if (_isPaused.TryGetValue(guildId, out var paused) && paused)
            {
                var currentTrack = _currentTracks[guildId];
                if (currentTrack != null)
                {
                    _isPaused[guildId] = false;
                    await PlayNextAsync(guildId);
                }
            }
        }

        public async Task SkipAsync(ulong guildId)
        {
            if (_currentProcesses.TryGetValue(guildId, out var process))
            {
                process.Kill();
                _currentProcesses.TryRemove(guildId, out _);
            }
            await PlayNextAsync(guildId);
        }

        public async Task StopAsync(ulong guildId)
        {
            if (_currentProcesses.TryGetValue(guildId, out var process))
            {
                process.Kill();
                _currentProcesses.TryRemove(guildId, out _);
            }

            if (_connectedChannels.TryGetValue(guildId, out var client))
            {
                await client.StopAsync();
                _connectedChannels.TryRemove(guildId, out _);
            }

            _musicQueues.TryRemove(guildId, out _);
            _currentTracks.TryRemove(guildId, out _);
            _isPaused.TryRemove(guildId, out _);
            
            if (_pauseTokens.TryGetValue(guildId, out var tokenSource))
            {
                tokenSource.Cancel();
                _pauseTokens.TryRemove(guildId, out _);
            }
        }

        public async Task LeaveAsync(ulong guildId)
        {
            await StopAsync(guildId);
        }

        public Queue<AudioTrack> GetQueue(ulong guildId)
        {
            return _musicQueues.TryGetValue(guildId, out var queue) ? queue : new Queue<AudioTrack>();
        }

        public AudioTrack? GetCurrentTrack(ulong guildId)
        {
            return _currentTracks.TryGetValue(guildId, out var track) ? track : null;
        }

        public bool IsPaused(ulong guildId)
        {
            return _isPaused.TryGetValue(guildId, out var paused) && paused;
        }

        public bool IsConnected(ulong guildId)
        {
            return _connectedChannels.ContainsKey(guildId);
        }

        private async Task<string> GetVideoTitleAsync(string url)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C \"\"C:\\Users\\daphi\\Downloads\\yt-dlp.exe\" --get-title \"{url}\"\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var title = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return title.Trim();
            }
            catch
            {
                return "Unknown Title";
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
