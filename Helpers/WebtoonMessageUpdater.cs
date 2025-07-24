using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System.Text;

public static class WebtoonMessageUpdater
{
    public static async Task UpdateWebtoonMessageAsync(
        DiscordSocketClient client,
        DatabaseService db,
        IConfiguration config)
    {
        if (!ulong.TryParse(config["WebtoonChannelID"], out var channelId)) return;

        var channel = client.GetChannel(channelId) as SocketTextChannel;
        if (channel == null) return;

        var all = await db.GetAllWebtoonsAsync();
        var grouped = all.GroupBy(w => w.Status);

        var embedBuilder = new EmbedBuilder()
            .WithTitle("📚 Webtoons")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();

        var emojiMap = new Dictionary<string, string>
        {
            { "To Read", "🔴" },
            { "On Going", "🟠" },
            { "Up to Date", "🟡" },
            { "Done", "🟢" }
        };

        foreach (var group in grouped)
        {
            var entries = group.Select(w => $"**{w.Name}** (Chap. {w.Chapter})").ToList();
            var fieldName = $"{emojiMap.GetValueOrDefault(group.Key, "")} {group.Key}";
            var maxLength = 1024;
            var currentChunk = new StringBuilder();
            var chunkIndex = 1;

            foreach (var entry in entries)
            {
                if (currentChunk.Length + entry.Length + 1 > maxLength)
                {
                    embedBuilder.AddField($"{fieldName} ({chunkIndex})", currentChunk.ToString());
                    currentChunk.Clear();
                    chunkIndex++;
                }
                currentChunk.AppendLine(entry);
            }

            if (currentChunk.Length > 0)
            {
                embedBuilder.AddField(chunkIndex == 1 ? fieldName : $"{fieldName} ({chunkIndex})", currentChunk.ToString());
            }
        }

        var statusCounts = grouped.ToDictionary(g => g.Key, g => g.Count());
        var total = all.Count;
        var summaryLines = new List<string> { $"• Total: {total} webtoons" };

        foreach (var kvp in statusCounts)
        {
            var emoji = emojiMap.TryGetValue(kvp.Key, out var e) ? e : "";
            summaryLines.Add($"• {emoji} {kvp.Key}: {kvp.Value}");
        }
        summaryLines.Add("\u200B");
        embedBuilder.WithFooter(string.Join("\n", summaryLines));

        var embed = embedBuilder.Build();

        var messages = await channel.GetMessagesAsync(10).FlattenAsync();
        var webtoonMsg = messages.FirstOrDefault(m =>
            m.Author.Id == client.CurrentUser.Id &&
            m.Embeds.Any(e => e.Title?.Contains("Webtoons") == true));

        if (webtoonMsg is IUserMessage userMessage)
        {
            await userMessage.ModifyAsync(msg => msg.Embed = embed);
        }
        else
        {
            await channel.SendMessageAsync(embed: embed);
        }
    }
}