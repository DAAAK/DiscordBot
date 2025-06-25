using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    public class ListXpSlashCommand : ISlashCommands
    {
        public string CommandName => "leaderboard";

        private readonly IConfiguration _configuration;
        private readonly DatabaseService _db;
        public ListXpSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Classement des utilisateurs par XP.");
            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var leaderboard = (await _db.GetLeaderboardAsync()).Take(5).ToList();
            if (leaderboard.Count == 0)
            {
                await command.RespondAsync("Aucun utilisateur n'a encore d'XP.", ephemeral: true);
                return;
            }

            var guild = (command.Channel as SocketGuildChannel)?.Guild;
            if (guild == null)
            {
                await command.RespondAsync("❌ Impossible de récupérer le serveur.", ephemeral: true);
                return;
            }

            var backgroundPath = GetAssetPath(_configuration["LeaderboardImage"]);
            var imagePath = await GenerateLeaderboardImage(leaderboard, guild, backgroundPath);

            var embed = new EmbedBuilder()
                .WithTitle("🏆 Classement")
                .WithImageUrl("attachment://leaderboard.png")
                .WithColor(Discord.Color.Red)
                .Build();

            await command.RespondWithFileAsync(imagePath, "leaderboard.png", embed: embed);
            File.Delete(imagePath);
        }

        private async Task<string> GenerateLeaderboardImage(List<(ulong UserId, int Level, int XP)> leaderboard, SocketGuild guild, string backgroundPath)
        {
            using var backgroundImage = System.Drawing.Image.FromFile(backgroundPath);

            const int lineHeight = 60;
            const int padding = 40;
            int width = backgroundImage.Width;
            int height = backgroundImage.Height;

            using var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            g.DrawImage(backgroundImage, 0, 0, width, height);

            using var font = new Font("Arial", 24, FontStyle.Bold);
            using var brush = new SolidBrush(System.Drawing.Color.White);

            for (int i = 0; i < leaderboard.Count; i++)
            {
                var (userId, level, xp) = leaderboard[i];
                var user = guild.GetUser(userId);
                var name = user?.DisplayName ?? $"Unknown {userId}";

                string line = $"{i + 1}. {name} — Level {level} ({xp} XP)";
                g.DrawString(line, font, brush, padding, padding + i * lineHeight);
            }

            string tempPath = Path.Combine(Path.GetTempPath(), $"leaderboard_{Guid.NewGuid()}.png");
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
            return tempPath;
        }


        private string GetAssetPath(string relativePath)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = Path.Combine(baseDirectory, relativePath);

            if (File.Exists(fullPath))
                return fullPath;

            var dir = new DirectoryInfo(baseDirectory);
            while (dir != null && !dir.GetFiles("*.csproj").Any())
            {
                dir = dir.Parent;
            }

            return dir != null
                ? Path.Combine(dir.FullName, relativePath)
                : fullPath;
        }

    }
