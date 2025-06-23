using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net.Http;

public class Movement : IMovement
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public Movement(DiscordSocketClient client, IConfiguration configuration)
    {
        _configuration = configuration;
        _client = client;
        _httpClient = new HttpClient();
        _client.UserJoined += HandleUserJoined;
        _client.UserLeft += HandleUserLeft;
    }

    public async Task HandleUserJoined(SocketGuildUser user)
    {
        try
        {
            var guild = user.Guild;
            var channel = guild.GetTextChannel(ulong.Parse(_configuration["MovementChannelID"]));
            var joinImagePath = GetAssetPath(_configuration["JoiningImage"]);

            if (channel != null && File.Exists(joinImagePath))
            {
                var userAvatarUrl = user.GetAvatarUrl(size: 256) ?? user.GetDefaultAvatarUrl();
                var customImagePath = await CreateImage(joinImagePath, userAvatarUrl, user.DisplayName, true);

                var embed = new EmbedBuilder()
                    .WithImageUrl("attachment://welcome.png")
                    .Build();

                await channel.SendFileAsync(customImagePath, embed: embed);

                // Clean up the temporary file
                File.Delete(customImagePath);
            }
            else if (!File.Exists(joinImagePath))
            {
                Console.WriteLine($"Warning: Join image not found at {joinImagePath}");
            }

            Console.WriteLine($"User {user.Username} joined the server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling UserJoined event: {ex}");
        }
    }

    public async Task HandleUserLeft(SocketGuild guild, SocketUser user)
    {
        try
        {
            var channel = guild.GetTextChannel(ulong.Parse(_configuration["MovementChannelID"]));
            var leaveImagePath = GetAssetPath(_configuration["LeavingImage"]);

            string displayName = (user as SocketGuildUser)?.DisplayName ?? user.Username;

            if (channel != null && File.Exists(leaveImagePath))
            {
                var userAvatarUrl = user.GetAvatarUrl(size: 256) ?? user.GetDefaultAvatarUrl();
                var customImagePath = await CreateImage(leaveImagePath, userAvatarUrl, displayName, false);

                var embed = new EmbedBuilder()
                    .WithImageUrl("attachment://goodbye.png")
                    .Build();

                await channel.SendFileAsync(customImagePath, embed: embed);

                // Clean up the temporary file
                File.Delete(customImagePath);
            }
            else if (!File.Exists(leaveImagePath))
            {
                Console.WriteLine($"Warning: Leave image not found at {leaveImagePath}");
            }

            Console.WriteLine($"User {user.Username} left the server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling UserLeft event: {ex}");
        }
    }

    private async Task<string> CreateImage(string backgroundImagePath, string avatarUrl, string username, bool isWelcome)
    {
        try
        {
            // Load background image
            using var backgroundImage = System.Drawing.Image.FromFile(backgroundImagePath);

            // Download user avatar
            var avatarBytes = await _httpClient.GetByteArrayAsync(avatarUrl);
            using var avatarStream = new MemoryStream(avatarBytes);
            using var avatarImage = System.Drawing.Image.FromStream(avatarStream);

            // Create a new bitmap with the same size as background
            int newWidth = (int)(backgroundImage.Width * 1.5); // 150% width
            int newHeight = backgroundImage.Height;

            using var bitmap = new Bitmap(newWidth, newHeight);
            using var graphics = Graphics.FromImage(bitmap);

            // Set high quality rendering
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            // Draw background
            int backgroundX = (newWidth - backgroundImage.Width) / 2;
            graphics.DrawImage(backgroundImage, backgroundX, 0, backgroundImage.Width, backgroundImage.Height);

            // Calculate avatar size and position (center of image)
            int avatarSize = Math.Min(backgroundImage.Width, backgroundImage.Height) / 4;
            int avatarX = (newWidth - avatarSize) / 2;
            int avatarY = (newHeight - avatarSize) / 2 + 300;

            // Create circular avatar
            using var avatarBitmap = new Bitmap(avatarSize, avatarSize);
            using var avatarGraphics = Graphics.FromImage(avatarBitmap);
            avatarGraphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Create circular clipping path
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, avatarSize, avatarSize);
            avatarGraphics.SetClip(path);

            // Draw avatar in circle
            avatarGraphics.DrawImage(avatarImage, 0, 0, avatarSize, avatarSize);

            // Draw the circular avatar on main image
            graphics.DrawImage(avatarBitmap, avatarX, avatarY);

            // Add border around avatar (optional)
            using var borderPen = new Pen(System.Drawing.Color.White, 4);
            graphics.DrawEllipse(borderPen, avatarX, avatarY, avatarSize, avatarSize);

            // Draw username below avatar
            var textY = avatarY + avatarSize + 10;
            DrawCenteredText(graphics, $"{(isWelcome ? "Welcome" : "Goodbye")} {username}", avatarX + (avatarSize / 1.5), textY, 50, System.Drawing.Color.White);

            // Save to temporary file
            var tempPath = Path.Combine(Path.GetTempPath(), $"{(isWelcome ? "welcome" : "goodbye")}_{Guid.NewGuid()}.png");
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

            return tempPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating welcome image: {ex}");
            throw;
        }
    }

    private void DrawCenteredText(Graphics graphics, string text, double regionCenterX, int y, int fontSize, System.Drawing.Color color)
    {
        using var font = new Font("Arial", fontSize, FontStyle.Bold);
        using var brush = new SolidBrush(color);
        using var outlinePen = new Pen(System.Drawing.Color.Black, 2);

        // Measure text size
        var textSize = graphics.MeasureString(text, font);
        float x = (float)(regionCenterX - (textSize.Width / 2));

        // Create text path for outline effect
        using var path = new GraphicsPath();
        path.AddString(text, font.FontFamily, (int)font.Style, fontSize, new PointF(x, y), StringFormat.GenericDefault);

        // Draw outline
        graphics.DrawPath(outlinePen, path);

        // Draw fill
        graphics.FillPath(brush, path);
    }


    private string GetAssetPath(string relativePath)
    {
        // Try the executable directory first (for deployed builds)
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var executablePath = Path.Combine(baseDirectory, relativePath);

        if (File.Exists(executablePath))
        {
            return executablePath;
        }

        // Fall back to project directory (for development)
        var projectDirectory = GetProjectDirectory();
        var projectPath = Path.Combine(projectDirectory, relativePath);

        if (File.Exists(projectPath))
        {
            return projectPath;
        }

        // Return the executable path anyway (will fail but shows expected location)
        return executablePath;
    }

    private string GetProjectDirectory()
    {
        // Navigate up from bin\Debug\net6.0 to project root
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectDir = new DirectoryInfo(currentDirectory);

        // Go up until we find the project root (contains .csproj file)
        while (projectDir != null && !projectDir.GetFiles("*.csproj").Any())
        {
            projectDir = projectDir.Parent;
        }

        return projectDir?.FullName ?? currentDirectory;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}