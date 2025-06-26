using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DeleteCommandSlashCommand : ISlashCommands
{
    public string CommandName => "delete-cmd";

    private readonly DatabaseService _db;
    private readonly IConfiguration _configuration;

    public DeleteCommandSlashCommand(IConfiguration configuration, DatabaseService db)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Delete a command from the list.")
            .AddOption("name", ApplicationCommandOptionType.String, "Name of the command", isRequired: true);

        await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var executor = (SocketGuildUser)command.User;

        var roleChecker = new RequiredRoles(_configuration);

        if (!roleChecker.HasRequiredRole(executor))
        {
            var embedBuilder = new EmbedBuilder()
                    .WithTitle("Permission Refusée")
                    .WithDescription("Vous n'avez pas la permission d'utiliser cette commande.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        string? name = command.Data.Options.FirstOrDefault(o => o.Name == "name")?.Value?.ToString();

        if (string.IsNullOrEmpty(name))
        {
            await command.RespondAsync("❌ Command name is missing or invalid.", ephemeral: true);
            return;
        }

        bool success = await _db.DeleteCommandAsync(name);
        
        if (success)
        {
            await command.RespondAsync($"🗑️ Command `/{name}` deleted.", ephemeral: true);
            await HelpMessageUpdater.UpdateHelpMessageAsync(client, _db, _configuration);
        }
        else
        {
            await command.RespondAsync("❌ Failed to delete the command.", ephemeral: true);
        }
    }
}