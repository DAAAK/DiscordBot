using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System.Text;

public class ListWebtoonsSlashCommand : ISlashCommands
{
    public string CommandName => "webtoons";
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _databaseService;
    private bool _buttonHandlerRegistered = false;

    public ListWebtoonsSlashCommand(IConfiguration configuration, DatabaseService databaseService)
    {
        _configuration = configuration;
        _databaseService = databaseService;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Lists all webtoons grouped by status.");

        await client.Rest.CreateGuildCommand(guildCommand.Build(), ulong.Parse(_configuration["GuildID"]));

        if (!_buttonHandlerRegistered)
        {
            client.ButtonExecuted += HandleButtonAsync;
            _buttonHandlerRegistered = true;
        }
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var all = await _databaseService.GetAllWebtoonsAsync();

        if (all == null || all.Count == 0)
        {
            await command.RespondAsync("⚠️ No webtoons found.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(_configuration["WebtoonChannelID"], out var webtoonChannelId))
        {
            await command.RespondAsync("⚠️ Webtoon channel not configured properly.", ephemeral: true);
            return;
        }

        var guild = (command.Channel as SocketGuildChannel)?.Guild;
        var webtoonChannel = guild?.GetTextChannel(webtoonChannelId);

        if (webtoonChannel == null)
        {
            await command.RespondAsync("❌ Unable to find the webtoon channel.", ephemeral: true);
            return;
        }

        var pages = BuildPages(all);

        if (pages.Count == 0)
        {
            await command.RespondAsync("⚠️ No webtoon pages could be created.", ephemeral: true);
            return;
        }

        var firstPageIndex = 0;
        var embed = BuildPageEmbed(pages[firstPageIndex], firstPageIndex, pages.Count, all);
        var components = BuildButtons(firstPageIndex, pages.Count);

        var existing = await webtoonChannel.GetMessagesAsync(20).FlattenAsync();
        var alreadyPosted = existing.FirstOrDefault(m =>
            m.Author.Id == client.CurrentUser.Id &&
            m.Embeds.Any(e => e.Title?.Contains("Webtoons") == true));

        if (alreadyPosted == null)
        {
            await webtoonChannel.SendMessageAsync(embed: embed, components: components);
        }
        else
        {
            if (alreadyPosted is IUserMessage userMessage)
            {
                await userMessage.ModifyAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = components;
                });
            }
            else
            {
                await webtoonChannel.SendMessageAsync(embed: embed, components: components);
            }
        }

        if (command.Channel.Id != webtoonChannelId)
        {
            await command.RespondAsync($"ℹ️ I've posted the webtoon list in {webtoonChannel.Mention}.", ephemeral: true);
        }
        else
        {
            await command.RespondAsync("✅ Webtoon list updated.", ephemeral: true);
        }
    }

    private class WebtoonPage
    {
        public string StatusKey { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private List<WebtoonPage> BuildPages(List<(string Name, int Chapter, string Status)> all)
    {
        var emojiMap = new Dictionary<string, string>
        {
            { "To Read", "🔴" },
            { "On Going", "🟠" },
            { "Up to Date", "🟡" },
            { "Done", "🟢" }
        };

        var statusOrder = new List<string>
        {
            "To Read",
            "On Going",
            "Up to Date",
            "Done"
        };

        var grouped = all
            .GroupBy(w => w.Status)
            .OrderBy(g =>
            {
                var index = statusOrder.IndexOf(g.Key);
                return index == -1 ? int.MaxValue : index;
            });

        var pages = new List<WebtoonPage>();
        const int maxDescriptionLength = 3500;

        foreach (var group in grouped)
        {
            var entries = group
                .OrderBy(w => w.Name)
                .Select(w => $"**{w.Name}** (Chap. {w.Chapter})")
                .ToList();

            var chunks = new List<string>();
            var currentChunk = new StringBuilder();

            foreach (var entry in entries)
            {
                if (currentChunk.Length + entry.Length + Environment.NewLine.Length > maxDescriptionLength)
                {
                    chunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                }

                currentChunk.AppendLine(entry);
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
            }

            var emoji = emojiMap.TryGetValue(group.Key, out var foundEmoji) ? foundEmoji : "";
            var statusDisplay = $"{emoji} {group.Key}".Trim();

            for (int i = 0; i < chunks.Count; i++)
            {
                pages.Add(new WebtoonPage
                {
                    StatusKey = group.Key,
                    StatusDisplay = chunks.Count == 1
                        ? statusDisplay
                        : $"{statusDisplay} — Page {i + 1}/{chunks.Count}",
                    Content = chunks[i]
                });
            }
        }

        return pages;
    }

    private Embed BuildPageEmbed(
        WebtoonPage page,
        int pageIndex,
        int totalPages,
        List<(string Name, int Chapter, string Status)> all)
    {
        var emojiMap = new Dictionary<string, string>
        {
            { "To Read", "🔴" },
            { "On Going", "🟠" },
            { "Up to Date", "🟡" },
            { "Done", "🟢" }
        };

        var statusOrder = new List<string>
        {
            "To Read",
            "On Going",
            "Up to Date",
            "Done"
        };

        var statusCounts = all
            .GroupBy(w => w.Status)
            .OrderBy(g =>
            {
                var index = statusOrder.IndexOf(g.Key);
                return index == -1 ? int.MaxValue : index;
            })
            .ToDictionary(g => g.Key, g => g.Count());

        var total = all.Count;
        var summaryLines = new List<string>
        {
            $"• Total: {total} webtoons"
        };

        foreach (var kvp in statusCounts)
        {
            var emoji = emojiMap.TryGetValue(kvp.Key, out var e) ? e : "";
            summaryLines.Add($"• {emoji} {kvp.Key}: {kvp.Value}");
        }

        var embed = new EmbedBuilder()
    .WithTitle("📚 Webtoons")
    .WithDescription($"**{page.StatusDisplay}**\n\n{page.Content}")
    .WithColor(Color.Blue)
    .WithFooter($"Page {pageIndex + 1}/{totalPages}\n{string.Join("\n", summaryLines)}")
    .WithCurrentTimestamp();

        return embed.Build();
    }

    private MessageComponent BuildButtons(int pageIndex, int totalPages)
    {
        return new ComponentBuilder()
            .WithButton("⬅ Previous", $"webtoons_prev:{pageIndex}", ButtonStyle.Primary, disabled: pageIndex == 0)
            .WithButton("Next ➡", $"webtoons_next:{pageIndex}", ButtonStyle.Primary, disabled: pageIndex >= totalPages - 1)
            .Build();
    }

    private async Task HandleButtonAsync(SocketMessageComponent component)
    {
        if (!component.Data.CustomId.StartsWith("webtoons_"))
            return;

        var all = await _databaseService.GetAllWebtoonsAsync();

        if (all == null || all.Count == 0)
        {
            await component.RespondAsync("⚠️ No webtoons found.", ephemeral: true);
            return;
        }

        var pages = BuildPages(all);

        if (pages.Count == 0)
        {
            await component.RespondAsync("⚠️ No webtoon pages found.", ephemeral: true);
            return;
        }

        var parts = component.Data.CustomId.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var currentIndex))
        {
            await component.RespondAsync("⚠️ Invalid pagination state.", ephemeral: true);
            return;
        }

        var action = parts[0];
        var newIndex = currentIndex;

        if (action == "webtoons_prev")
        {
            newIndex--;
        }
        else if (action == "webtoons_next")
        {
            newIndex++;
        }

        newIndex = Math.Clamp(newIndex, 0, pages.Count - 1);

        var embed = BuildPageEmbed(pages[newIndex], newIndex, pages.Count, all);
        var components = BuildButtons(newIndex, pages.Count);

        await component.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });
    }
}