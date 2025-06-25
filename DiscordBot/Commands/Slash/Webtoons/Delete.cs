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
    public class DeleteWebtoonSlashCommand : ISlashCommands
    {
        public string CommandName => "delete-webtoon";

        private readonly IConfiguration _configuration;
        private readonly DatabaseService _db;

        public DeleteWebtoonSlashCommand(IConfiguration config, DatabaseService db)
        {
            _configuration = config;
            _db = db;
        }

        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var guildId = ulong.Parse(_configuration["GuildID"]);

            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Delete a webtoon by name.")
                .AddOption("name", ApplicationCommandOptionType.String, "Webtoon name", true);

            await client.Rest.CreateGuildCommand(command.Build(), guildId);
        }

        public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
        {
            var executor = (SocketGuildUser)command.User;

            if (!HasRequiredRole(executor))
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle("Permission Refusée")
                    .WithDescription("Vous n'avez pas la permission d'utiliser cette commande.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

                await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            var name = command.Data.Options.First(o => o.Name == "name").Value.ToString();
            await _db.DeleteWebtoonAsync(name);
            await command.RespondAsync($"🗑️ Deleted **{name}** from your webtoons.", ephemeral: true);
        }

        private bool HasRequiredRole(SocketGuildUser user)
        {
            if (_configuration == null || !_configuration.GetSection("RequiredRolesIDS").Exists())
                return false;

            var requiredRoleIds = _configuration.GetSection("RequiredRolesIDS")
                                                .GetChildren()
                                                .Select(x => ulong.Parse(x.Value))
                                                .ToArray();

            return user.Roles.Any(role => requiredRoleIds.Contains(role.Id));
        }
    }
}
