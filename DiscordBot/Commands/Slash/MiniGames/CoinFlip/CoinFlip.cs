using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

public class CoinFlipSlashCommand : ISlashCommands
{
    public string CommandName => "coinflip";

    private readonly IConfiguration _configuration;

    public CoinFlipSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Flip a coin (Heads or Tails).");

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
        string result = rng.Next(2) == 0 ? "Heads" : "Tails";

        var embed = new EmbedBuilder()
            .WithTitle("🪙 Coin Flip")
            .WithDescription($"{command.User.Mention} flipped **{result}**!")
            .WithColor(Color.Gold)
            .Build();

        await command.RespondAsync(embed: embed);
    }
}
