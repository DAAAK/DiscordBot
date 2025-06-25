using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Slash.NewFolder1
{
    public class UpdateXpSlashCommand : ISlashCommands
    {
        public string CommandName => "update-xp";
        private readonly DatabaseService _db;
        private readonly IConfiguration _configuration;

        public UpdateXpSlashCommand(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }
        public async Task RegisterCommandsAsync(DiscordSocketClient client)
        {
            var command = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Manually update a user's XP")
                .AddOption("user", ApplicationCommandOptionType.User, "Select the user", isRequired: true)
                .AddOption("amount", ApplicationCommandOptionType.Integer, "New XP value", isRequired: true);
            await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
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

            var userOption = (SocketGuildUser)command.Data.Options.First(o => o.Name == "user").Value;
            var xpValue = (long)command.Data.Options.First(o => o.Name == "amount").Value;

            await _db.AddXPAsync(userOption.Id, userOption.DisplayName, (int)xpValue - await _db.GetUserXPAsync(userOption.Id));

            await command.RespondAsync($"✅ XP de {userOption.Mention} mis à jour à **{xpValue} XP**.", ephemeral: true);
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
