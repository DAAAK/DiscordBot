using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

public class RPSSlashCommand : ISlashCommands
{
    public string CommandName => "rps";

    private readonly IConfiguration _configuration;
    private static readonly ConcurrentDictionary<(ulong, ulong), string> _choices = new(); // (Player1Id, Player2Id) => choice

    public RPSSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var command = new SlashCommandBuilder()
    .WithName(CommandName)
    .WithDescription("Play rock-paper-scissors with another user.")
    .AddOption("opponent", ApplicationCommandOptionType.User, "Your opponent", isRequired: true)
    .AddOption(new SlashCommandOptionBuilder()
        .WithName("choice")
        .WithDescription("Your move: rock, paper or scissors")
        .WithType(ApplicationCommandOptionType.String)
        .WithRequired(true)
        .AddChoice("🪨 Rock", "rock")
        .AddChoice("📄 Paper", "paper")
        .AddChoice("✂️ Scissors", "scissors"));



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

        var player1 = (SocketGuildUser)command.User;
        var player2 = (SocketGuildUser)command.Data.Options.First(o => o.Name == "opponent").Value;
        string player1Choice = command.Data.Options.First(o => o.Name == "choice").Value.ToString()?.ToLower();

        var key = (player1.Id, player2.Id);
        _choices[key] = player1Choice;

        var reverseKey = (player2.Id, player1.Id);
        if (_choices.TryGetValue(reverseKey, out var player2Choice))
        {
            string result = GetRpsResult(player1Choice, player2Choice);
            string outcome = result switch
            {
                "win" => $"🎉 {player1.Mention} **wins** against {player2.Mention}!",
                "lose" => $"😢 {player1.Mention} **loses** to {player2.Mention}.",
                _ => $"🤝 It's a **draw** between {player1.Mention} and {player2.Mention}!"
            };

            var embed = new EmbedBuilder()
                .WithTitle("🎮 Rock Paper Scissors")
                .AddField(player1.Username, player1Choice, true)
                .AddField(player2.Username, player2Choice, true)
                .WithDescription(outcome)
                .WithColor(Color.Blue)
                .Build();

            _choices.TryRemove(key, out _);
            _choices.TryRemove(reverseKey, out _);

            await command.RespondAsync(embed: embed);
        }
        else
        {
            await command.RespondAsync($"✅ Your choice has been saved. Waiting for {player2.Mention} to play.");
        }
    }

    private static string GetRpsResult(string p1, string p2)
    {
        if (p1 == p2) return "draw";

        return (p1, p2) switch
        {
            ("rock", "scissors") => "win",
            ("paper", "rock") => "win",
            ("scissors", "paper") => "win",
            _ => "lose"
        };
    }
}
