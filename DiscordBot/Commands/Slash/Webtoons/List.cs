using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.Webtoons
{
    public class ListWebtoonsSlashCommand : ISlashCommands
    {
        public string CommandName => "list-webtoons";

        private readonly IConfiguration _configuration;
        private readonly DatabaseService _databaseService;

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
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var all = await _databaseService.GetAllWebtoonsAsync();
            var grouped = all.GroupBy(w => w.Status);

            var embed = new EmbedBuilder()
                .WithTitle("\uD83D\uDCDA Webtoons")
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
                        embed.AddField($"{fieldName} ({chunkIndex})", currentChunk.ToString());
                        currentChunk.Clear();
                        chunkIndex++;
                    }

                    currentChunk.AppendLine(entry);
                }

                if (currentChunk.Length > 0)
                {
                    embed.AddField(chunkIndex == 1 ? fieldName : $"{fieldName} ({chunkIndex})", currentChunk.ToString());
                }
            }


            var statusCounts = grouped
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

            summaryLines.Add("\u200B");

            embed.WithFooter(string.Join("\n", summaryLines));

            await command.RespondAsync(embed: embed.Build());
        }
    }

}
