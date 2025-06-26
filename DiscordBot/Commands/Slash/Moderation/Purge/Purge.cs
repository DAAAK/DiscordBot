using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class PurgeSlashCommand : ISlashCommands
{
    public string CommandName => "purge";

    private readonly IConfiguration _configuration;

    public PurgeSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName("purge")
            .WithDescription("Clear all messages from a text channel.")
            .AddOption("message_count", ApplicationCommandOptionType.Integer, "Number of messages to delete.", isRequired: true);

        await client.Rest.CreateGuildCommand(guildCommand.Build(), ulong.Parse(_configuration["GuildID"]));
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

        try
        {
            var messageCountOption = command.Data.Options.FirstOrDefault(o => o.Name == "message_count");

            if (!int.TryParse(messageCountOption.Value.ToString(), out int messageCount) || messageCount <= 0)
            {
                var invalidCountBuilder = new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription("Invalid message count specified.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

                await command.RespondAsync(embed: invalidCountBuilder.Build());
                return;
            }

            if (command.Channel is not ITextChannel channel)
            {
                var channelNotFoundBuilder = new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription("Command was not executed in a text channel.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

                await command.RespondAsync(embed: channelNotFoundBuilder.Build());
                return;
            }

            var messages = await channel.GetMessagesAsync(messageCount).FlattenAsync();
            var botMessages = messages.Where(m => m.Author.Id == client.CurrentUser.Id);

            var messagesToDelete = messages.ToList();

            if (messagesToDelete.Count == 0)
            {
                var noMessagesBuilder = new EmbedBuilder()
                    .WithTitle("No Messages to Delete")
                    .WithDescription("There are no messages to delete in this channel.")
                    .WithColor(Color.Orange)
                    .WithCurrentTimestamp();

                await command.RespondAsync(embed: noMessagesBuilder.Build());
                return;
            }
            else if (messagesToDelete.Count < messageCount)
            {
                var insufficientMessagesBuilder = new EmbedBuilder()
                    .WithTitle("Insufficient Messages")
                    .WithDescription($"There are not enough messages in this channel to delete {messageCount} messages.")
                    .WithColor(Color.Orange)
                    .WithCurrentTimestamp();

                await command.RespondAsync(embed: insufficientMessagesBuilder.Build());
                return;
            }

            await channel.DeleteMessagesAsync(messagesToDelete);

            var successBuilder = new EmbedBuilder()
             .WithTitle("Chat Cleared")
             .WithDescription($"Successfully deleted {messageCount} messages from #{channel.Name}.")
             .WithColor(Color.Green)
             .WithCurrentTimestamp();

            await command.RespondAsync(embed: successBuilder.Build());
        }
        catch (Exception ex)
        {
            var failedToClearBuilder = new EmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"Failed to clear chat: {ex.Message}")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();

            await command.RespondAsync(embed: failedToClearBuilder.Build());
        }
    }
}
