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
    private readonly Dictionary<ulong, string> _displayNameCache = new();
    private static readonly string[] WelcomeMessages = new[]
{
    "🎉 Welcome {mention}! Let the fun begin!",
    "👋 Glad you're here, {mention}!",
    "🌟 A wild {mention} appeared!",
    "🔥 {mention} just landed. Say hi!",
    "🎈 {mention}, welcome to the party!",
    "😎 Yo {mention}, welcome aboard!",
    "🍕 {mention} brought snacks, right?",
    "🌈 {mention} joined. Things just got better!",
    "📢 Make way for {mention}!",
    "💥 Boom! {mention} is here!"
};

    private static readonly string[] GoodbyeMessages = new[]
{
    "😢 {mention} has left the building.",
    "💔 Farewell, {mention}. We'll miss you.",
    "👋 Goodbye {mention}! Hope to see you again!",
    "🚪 {mention} walked out the door.",
    "🎭 Curtain call for {mention}.",
    "🌌 {mention} has drifted into the stars.",
    "📉 Server fun down 10% – {mention} left.",
    "🥀 {mention} is no longer with us (in this server).",
    "🎬 That's a wrap for {mention}.",
    "👻 {mention} vanished without a trace..."
};

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

            var random = new Random();
            string welcomeMessage = WelcomeMessages[random.Next(WelcomeMessages.Length)]
                .Replace("{mention}", user.Mention);

            if (channel != null && File.Exists(joinImagePath))
            {
                var userAvatarUrl = user.GetAvatarUrl(size: 256) ?? user.GetDefaultAvatarUrl();

                _displayNameCache[user.Id] = user.DisplayName;

                var customImagePath = await CreateImage(joinImagePath, userAvatarUrl, true);

                var embed = new EmbedBuilder()
                    .WithDescription(welcomeMessage)
                    .WithImageUrl("attachment://welcome.png")
                    .WithColor(Discord.Color.Green)
                    .Build();

                await channel.SendFileAsync(customImagePath, embed: embed);

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
            var random = new Random();
            string goodbyeMessage = GoodbyeMessages[random.Next(GoodbyeMessages.Length)]
                .Replace("{mention}", user.Mention);

            _displayNameCache.TryGetValue(user.Id, out var displayName);
            displayName ??= user.Username;

            if (channel != null && File.Exists(leaveImagePath))
            {
                var userAvatarUrl = user.GetAvatarUrl(size: 256) ?? user.GetDefaultAvatarUrl();
                var customImagePath = await CreateImage(leaveImagePath, userAvatarUrl, false);

                var embed = new EmbedBuilder()
                    .WithDescription(goodbyeMessage)
                    .WithImageUrl("attachment://goodbye.png")
                    .WithColor(Discord.Color.Red)
                    .Build();

                await channel.SendFileAsync(customImagePath, embed: embed);

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

    private async Task<string> CreateImage(string backgroundImagePath, string avatarUrl, bool isWelcome)
    {
        try
        {
            using var backgroundImage = System.Drawing.Image.FromFile(backgroundImagePath);

            var avatarBytes = await _httpClient.GetByteArrayAsync(avatarUrl);
            using var avatarStream = new MemoryStream(avatarBytes);
            using var avatarImage = System.Drawing.Image.FromStream(avatarStream);

            int newWidth = (int)(backgroundImage.Width * 2.0);
            int newHeight = backgroundImage.Height;

            using var bitmap = new Bitmap(newWidth, newHeight);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            int backgroundX = (newWidth - backgroundImage.Width) / 2;
            graphics.DrawImage(backgroundImage, 0, 0, newWidth, newHeight);

            int avatarSize = Math.Min(backgroundImage.Width, backgroundImage.Height) / 4;
            int avatarX = (newWidth - avatarSize) / 2;
            int avatarY = (newHeight - avatarSize) / 2 + 300;

            using var avatarBitmap = new Bitmap(avatarSize, avatarSize);
            using var avatarGraphics = Graphics.FromImage(avatarBitmap);
            avatarGraphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, avatarSize, avatarSize);
            avatarGraphics.SetClip(path);
            
            avatarGraphics.DrawImage(avatarImage, 0, 0, avatarSize, avatarSize);

            graphics.DrawImage(avatarBitmap, avatarX, avatarY);

            using var borderPen = new Pen(System.Drawing.Color.White, 4);
            graphics.DrawEllipse(borderPen, avatarX, avatarY, avatarSize, avatarSize);

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

    private string GetAssetPath(string relativePath)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var executablePath = Path.Combine(baseDirectory, relativePath);

        if (File.Exists(executablePath))
        {
            return executablePath;
        }

        var projectDirectory = GetProjectDirectory();
        var projectPath = Path.Combine(projectDirectory, relativePath);

        if (File.Exists(projectPath))
        {
            return projectPath;
        }

        return executablePath;
    }

    private string GetProjectDirectory()
    {
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectDir = new DirectoryInfo(currentDirectory);

        while (projectDir != null && !projectDir.GetFiles("*.csproj").Any())
        {
            projectDir = projectDir.Parent;
        }

        return projectDir?.FullName ?? currentDirectory;
    }
}