using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

    public class PollSlashCommand : ISlashCommands
    {
        public string CommandName => "poll";

        private readonly IConfiguration _configuration;

        public PollSlashCommand(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var pollCommand = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Create a poll with up to 10 options.")
                .AddOption("question", ApplicationCommandOptionType.String, "The poll question", isRequired: true);

            for (int i = 1; i <= 10; i++)
            {
                pollCommand.AddOption($"option{i}", ApplicationCommandOptionType.String, $"Option {i}", isRequired: i <= 2);
            }

            await client.Rest.CreateGuildCommand(pollCommand.Build(), ulong.Parse(_configuration["GuildID"]));
        }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        string question = command.Data.Options.FirstOrDefault(o => o.Name == "question")?.Value?.ToString() ?? string.Empty;
        var options = command.Data.Options
            .Where(o => o.Name.StartsWith("option"))
            .OrderBy(o => o.Name)
            .Select(o => o.Value?.ToString() ?? string.Empty)
            .ToList();

        if (options.Count < 2)
        {
            await command.RespondAsync("❌ You must provide at least 2 options.", ephemeral: true);
            return;
        }

        var emojis = new[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };

        var builder = new EmbedBuilder()
            .WithTitle("📊 " + question)
            .WithColor(Color.Blue);

        for (int i = 0; i < options.Count && i < emojis.Length; i++)
        {
            builder.AddField($"{emojis[i]} {options[i]}", "\u200B", inline: false);
        }

        var embed = builder.Build();

        if (!ulong.TryParse(_configuration["PollChannelID"], out ulong pollChannelId))
        {
            await command.RespondAsync("❌ Poll channel ID is missing or invalid in configuration.", ephemeral: true);
            return;
        }

        var channel = client.GetChannel(pollChannelId) as IMessageChannel;
        if (channel == null)
        {
            await command.RespondAsync("❌ Could not find the poll channel.", ephemeral: true);
            return;
        }

        var message = await channel.SendMessageAsync(embed: embed);

        for (int i = 0; i < options.Count && i < emojis.Length; i++)
        {
            await message.AddReactionAsync(new Emoji(emojis[i]));
        }

        await command.RespondAsync($"✅ Poll created in {MentionUtils.MentionChannel(pollChannelId)}", ephemeral: true);
    }


}
