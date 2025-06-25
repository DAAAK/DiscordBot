using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

public class HangmanSlashCommand : ISlashCommands
{
    public string CommandName => "hangman";

    private static readonly ConcurrentDictionary<ulong, HangmanGame> ActiveGames = new(); // GuildId -> Game

    private readonly IConfiguration _configuration;

    public HangmanSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var group = new SlashCommandBuilder()
            .WithName("hangman")
            .WithDescription("Play a game of Hangman.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("start")
                .WithDescription("Start a new Hangman game with a word.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("word", ApplicationCommandOptionType.String, "The word to guess (only letters)", isRequired: true)
            )
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("guess")
                .WithDescription("Guess a letter.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("letter", ApplicationCommandOptionType.String, "Your letter guess", isRequired: true)
            );

        await client.Rest.CreateGuildCommand(group.Build(), ulong.Parse(_configuration["GuildID"]));
    }

    public async Task HandleCommand(SocketSlashCommand command, DiscordSocketClient client)
    {

        ulong allowedChannelId = ulong.Parse(_configuration["GamesChannelID"]);
        if (command.Channel.Id != allowedChannelId)
        {
            await command.RespondAsync($"❌ You can only use this command in <#{allowedChannelId}>.", ephemeral: true);
            return;
        }

        var subCommand = command.Data.Options.First();
        var guildId = (command.Channel as SocketGuildChannel)?.Guild.Id;

        if (guildId == null)
        {
            await command.RespondAsync("This command must be used in a server.", ephemeral: true);
            return;
        }

        switch (subCommand.Name)
        {
            case "start":
                var word = subCommand.Options.First().Value.ToString()?.ToLower();
                if (string.IsNullOrWhiteSpace(word) || !Regex.IsMatch(word, @"^[a-zA-Z]+$"))
                {
                    await command.RespondAsync("❌ The word must contain only letters (a–z).", ephemeral: true);
                    return;
                }

                if (ActiveGames.ContainsKey(guildId.Value))
                {
                    await command.RespondAsync("❗ A game is already active. Finish it before starting a new one.", ephemeral: true);
                    return;
                }

                ActiveGames[guildId.Value] = new HangmanGame(word);
                await command.RespondAsync($"🎮 A new Hangman game has started!\nWord: {ActiveGames[guildId.Value].GetMaskedWord()}\nLives: {ActiveGames[guildId.Value].Lives}");
                break;

            case "guess":
                if (!ActiveGames.TryGetValue(guildId.Value, out var game))
                {
                    await command.RespondAsync("⚠️ No active Hangman game. Use `/hangman start` to begin.", ephemeral: true);
                    return;
                }

                var letterStr = subCommand.Options.First().Value.ToString()?.ToLower();
                if (string.IsNullOrWhiteSpace(letterStr) || letterStr.Length != 1 || !char.IsLetter(letterStr[0]))
                {
                    await command.RespondAsync("❌ Please enter a **single letter (a–z)**.", ephemeral: true);
                    return;
                }

                var guess = letterStr[0];
                if (game.GuessedLetters.Contains(guess))
                {
                    await command.RespondAsync($"🔁 The letter `{guess}` was already guessed.\n{game.GetMaskedWord()} | Lives: {game.Lives}", ephemeral: true);
                    return;
                }

                bool correct = game.GuessLetter(guess);

                if (game.IsWon())
                {
                    ActiveGames.TryRemove(guildId.Value, out _);
                    await command.RespondAsync($"🎉 `{guess}` is correct!\n✅ The word was **{game.Word}**.\n🏆 You win!");
                }
                else if (game.Lives <= 0)
                {
                    ActiveGames.TryRemove(guildId.Value, out _);
                    await command.RespondAsync($"💀 No lives left. The word was **{game.Word}**. Game over!");
                }
                else
                {
                    string response = correct
                        ? $"✅ Good guess! `{guess}` is in the word."
                        : $"❌ Oops! `{guess}` is not in the word.";

                    await command.RespondAsync($"{response}\nWord: `{game.GetMaskedWord()}`\nGuessed: {string.Join(", ", game.GuessedLetters)}\nLives: {game.Lives}");
                }

                break;
        }
    }

    private class HangmanGame
    {
        public string Word { get; }
        public int Lives { get; private set; } = 6;
        public HashSet<char> GuessedLetters { get; } = new();

        public HangmanGame(string word)
        {
            Word = word.ToLower();
        }

        public bool GuessLetter(char c)
        {
            GuessedLetters.Add(c);
            if (!Word.Contains(c))
            {
                Lives--;
                return false;
            }
            return true;
        }

        public bool IsWon()
        {
            return Word.All(c => GuessedLetters.Contains(c));
        }

        public string GetMaskedWord()
        {
            return string.Join(" ", Word.Select(c => GuessedLetters.Contains(c) ? c : '_'));
        }
    }
}
