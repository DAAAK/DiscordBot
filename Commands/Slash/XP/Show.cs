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

    public class ShowXpSlashCommand : ISlashCommands
    {
        public string CommandName => "rank";
        private readonly DatabaseService _db;
        private readonly IConfiguration _configuration;

        public ShowXpSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }


        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Affiche ton XP.");
            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));

        }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var user = command.User;

        var guildUser = command.User as SocketGuildUser;

        if (guildUser == null)
        {
            await command.RespondAsync("Error: Unable to retrieve guild user information.", ephemeral: true);
            return;
        }

        int xp = await _db.GetUserXPAsync(user.Id);
        int level = await _db.GetUserLevelAsync(user.Id);
        int rank = await _db.GetUserRankAsync(user.Id);
        var (_, currentXP, nextLevelXP, _) = _db.GetXPStats(xp, level);

        var backgroundPath = GetAssetPath(_configuration["RankImage"]);
        var imagePath = await GenerateRankCard(user, guildUser.DisplayName, guildUser.Status, xp, level, rank, currentXP, nextLevelXP, backgroundPath);

        await command.RespondWithFileAsync(imagePath, "leaderboard.jpg");
        File.Delete(imagePath);
    }


    public async Task<string> GenerateRankCard(SocketUser user, string displayName, UserStatus status, int xp, int level, int rank, int currentXP, int nextLevelXP, string backgroundPath)
    {
        using var background = System.Drawing.Image.FromFile(backgroundPath);

        int width = background.Width;
        int height = background.Height - 200;
        var bitmap = new Bitmap(width, height);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        g.DrawImage(background, 0, 0, width, height);

        using var usernameFont = new Font("Segoe UI", 36, FontStyle.Bold);
        using var smallFont = new Font("Segoe UI", 20);
        using var whiteBrush = new SolidBrush(System.Drawing.Color.White);
        using var redBrush = new SolidBrush(System.Drawing.Color.Red);
        using var grayBrush = new SolidBrush(System.Drawing.Color.FromArgb(60, 60, 60));
        using var greenBrush = new SolidBrush(System.Drawing.Color.LightGreen);

        int margin = 40;
        int avatarSize = 150;
        int barWidth = 500;
        int barHeight = 28;
        int baseY = height / 2 - avatarSize / 2;

        // Avatar (rounded)
        var avatarStream = await new HttpClient().GetStreamAsync(user.GetAvatarUrl(ImageFormat.Png, 256) ?? user.GetDefaultAvatarUrl());
        var avatar = System.Drawing.Image.FromStream(avatarStream);
        var avatarRect = new Rectangle(margin, baseY, avatarSize, avatarSize);

        using (var path = new System.Drawing.Drawing2D.GraphicsPath())
        {
            path.AddEllipse(avatarRect);
            g.SetClip(path);
            g.DrawImage(avatar, avatarRect);
            g.ResetClip();
        }

        var statusColor = status switch
        {
            UserStatus.Online => System.Drawing.Color.FromArgb(67, 181, 129),
            UserStatus.Idle => System.Drawing.Color.FromArgb(250, 166, 26),
            UserStatus.DoNotDisturb => System.Drawing.Color.FromArgb(240, 71, 71),
            UserStatus.Offline => System.Drawing.Color.FromArgb(116, 127, 141),
            _ => System.Drawing.Color.Gray
        };

        int dotSize = 28;
        int dotX = avatarRect.Right - 50;
        int dotY = avatarRect.Bottom - dotSize + 10;
        using var statusBrush = new SolidBrush(statusColor);
        g.FillEllipse(statusBrush, dotX, dotY, dotSize, dotSize);
        g.DrawEllipse(Pens.Black, dotX, dotY, dotSize, dotSize);

        int textX = avatarRect.Right + 40;
        int nameY = baseY;
        int rankY = nameY + 50;
        int barY = rankY + 60;

        g.DrawString(displayName, usernameFont, whiteBrush, textX, nameY);
        g.DrawString($"RANK #{rank}", smallFont, whiteBrush, textX, rankY);
        g.DrawString($"LEVEL {level}", smallFont, redBrush, textX + 250, rankY);

        float percent = (float)currentXP / nextLevelXP;
        int filledWidth = (int)(barWidth * percent);

        // Shadow behind XP bar
        int shadowOffset = 2;
        using var shadowBrush = new SolidBrush(System.Drawing.Color.FromArgb(100, 0, 0, 0));
        g.FillRectangle(shadowBrush, textX + shadowOffset, barY + shadowOffset, barWidth, barHeight);

        // Background bar
        using (var barBgPath = new System.Drawing.Drawing2D.GraphicsPath())
        {
            barBgPath.AddArc(textX, barY, barHeight, barHeight, 90, 180);
            barBgPath.AddArc(textX + barWidth - barHeight, barY, barHeight, barHeight, 270, 180);
            barBgPath.CloseFigure();
            g.FillPath(grayBrush, barBgPath);
            g.DrawPath(Pens.Black, barBgPath); // Border
        }

        // Filled portion of XP bar
        if (filledWidth > 0)
        {
            using var barFillPath = new System.Drawing.Drawing2D.GraphicsPath();
            if (filledWidth >= barHeight)
            {
                barFillPath.AddArc(textX, barY, barHeight, barHeight, 90, 180);
                barFillPath.AddArc(textX + filledWidth - barHeight, barY, barHeight, barHeight, 270, 180);
            }
            else
            {
                barFillPath.AddRectangle(new Rectangle(textX, barY, filledWidth, barHeight));
            }

            barFillPath.CloseFigure();
            g.FillPath(greenBrush, barFillPath); // Use DeepSkyBlue
        }

        g.DrawString($"{currentXP:N0} / {nextLevelXP:N0} XP", smallFont, whiteBrush, textX, barY + 32);

        string tempPath = Path.Combine(Path.GetTempPath(), $"rank_{user.Id}_{Guid.NewGuid()}.png");
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