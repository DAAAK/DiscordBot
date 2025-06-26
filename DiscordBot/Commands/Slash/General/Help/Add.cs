using Discord;
using Discord.WebSocket;
using DiscordBot.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AddCommandSlashCommand : ISlashCommands
{
    public string CommandName => "add-cmd";

    private readonly DatabaseService _db;
    private readonly IConfiguration _configuration;

    public AddCommandSlashCommand(IConfiguration configuration, DatabaseService db)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Add a new command to the list.")
            .AddOption("name", ApplicationCommandOptionType.String, "Name of the command", isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "Description of the command", isRequired: true);

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

        string name = command.Data.Options.First(o => o.Name == "name")?.Value?.ToString() ?? string.Empty;
        string description = command.Data.Options.First(o => o.Name == "description")?.Value?.ToString() ?? string.Empty;

        bool success = await _db.AddCommandAsync(name, description);
        if (success)
        {
            await command.RespondAsync($"✅ Command `/{name}` added successfully.", ephemeral: true);
            await HelpMessageUpdater.UpdateHelpMessageAsync(client, _db, _configuration);
        }
        else
        {
            await command.RespondAsync("❌ Failed to add the command. It may already exist.", ephemeral: true);
        }
    }
}