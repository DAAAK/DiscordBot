using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

public class DiceSlashCommand : ISlashCommands
{
    public string CommandName => "dice";

    private readonly IConfiguration _configuration;

    public DiceSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Roll a dice (1–6).");

        await client.Rest.CreateGuildCommand(command.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        ulong allowedChannelId = ulong.Parse(_configuration["GamesChannelID"]);
        if (command.Channel.Id != allowedChannelId)
        {
            await command.RespondAsync($"❌ You can only use this command in <#{allowedChannelId}>.", ephemeral: true);
            return;
        }

        var rng = new Random();
        int result = rng.Next(1, 7);

        var embed = new EmbedBuilder()
            .WithTitle("🎲 Dice Roll")
            .WithDescription($"{command.User.Mention} rolled a **{result}**!")
            .WithColor(Color.Blue)
            .Build();

        await command.RespondAsync(embed: embed);
    }
}
