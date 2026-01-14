using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

public class HangmanSlashCommand : ISlashCommands
{
    public string CommandName => "hangman";

    private static readonly ConcurrentDictionary<ulong, HangmanGame> ActiveGames = new();

    private readonly IConfiguration _configuration;

    public HangmanSlashCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RegisterCommandsAsync(DiscordSocketClient client)
    {
        var group = new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Play a game of Hangman.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("start")
                .WithDescription("Start a new Hangman game with a word.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("word", ApplicationCommandOptionType.String, "The word to guess (only letters)", isRequired: true)
            )
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("guess")
                .WithDescription("Guess a letter or the full word.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("input", ApplicationCommandOptionType.String, "A letter (a-z) or the full word", isRequired: true)
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
                {
                    var word = subCommand.Options.First().Value.ToString()?.ToLower();
                    word = Regex.Replace(word?.Trim() ?? "", @"\s+", " ");

                    if (string.IsNullOrWhiteSpace(word) || !Regex.IsMatch(word, @"^[a-zA-Z ]+$"))
                    {
                        await command.RespondAsync("❌ The word must contain only letters (a–z).", ephemeral: true);
                        return;
                    }

                    var channelId = command.Channel.Id;

                    if (ActiveGames.ContainsKey(channelId))
                    {
                        await command.RespondAsync("❗ A game is already active in this channel.", ephemeral: true);
                        return;
                    }

                    ActiveGames[channelId] = new HangmanGame(word);
                    await command.RespondAsync($"🎮 A new Hangman game has started!\nWord: {ActiveGames[channelId].GetMaskedWord()}\nLives: {ActiveGames[channelId].Lives}");
                    break;
                }

            case "guess":
                {
                    var channelId = command.Channel.Id;

                    if (!ActiveGames.TryGetValue(channelId, out var game))
                    {
                        await command.RespondAsync("⚠️ No active Hangman game. Use `/hangman start` to begin.", ephemeral: true);
                        return;
                    }

                    var input = subCommand.Options.First().Value.ToString()?.ToLower()?.Trim();
                    input = Regex.Replace(input ?? "", @"\s+", " "); // optional: normalize spaces for guesses too

                    if (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input, @"^[a-zA-Z ]+$"))
                    {
                        await command.RespondAsync("❌ Please enter only letters (a–z).", ephemeral: true);
                        return;
                    }

                    // FULL WORD GUESS (contains space or multiple letters)
                    if (input.Length > 1)
                    {
                        bool correctWord = game.GuessWord(input);

                        if (correctWord)
                        {
                            ActiveGames.TryRemove(channelId, out _);
                            await command.RespondAsync($"🎉 Correct! ✅ The word was **{game.Word}**.\n🏆 You win!");
                            return;
                        }

                        if (game.Lives <= 0)
                        {
                            ActiveGames.TryRemove(channelId, out _);
                            await command.RespondAsync($"💀 `{input}` is not the word.\nNo lives left. The word was **{game.Word}**. Game over!");
                            return;
                        }

                        await command.RespondAsync(
                            $"❌ `{input}` is not the word.\nWord: `{game.GetMaskedWord()}`\nGuessed: {string.Join(", ", game.GuessedLetters)}\nLives: {game.Lives}"
                        );
                        return;
                    }

                    // SINGLE LETTER GUESS
                    char guess = input[0];

                    if (game.GuessedLetters.Contains(guess))
                    {
                        await command.RespondAsync($"🔁 The letter `{guess}` was already guessed.\n{game.GetMaskedWord()} | Lives: {game.Lives}", ephemeral: true);
                        return;
                    }

                    bool correct = game.GuessLetter(guess);

                    if (game.IsWon())
                    {
                        ActiveGames.TryRemove(channelId, out _);
                        await command.RespondAsync($"🎉 `{guess}` is correct!\n✅ The word was **{game.Word}**.\n🏆 You win!");
                    }
                    else if (game.Lives <= 0)
                    {
                        ActiveGames.TryRemove(channelId, out _);
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
            return Word.Where(char.IsLetter).All(c => GuessedLetters.Contains(c));
        }

        public string GetMaskedWord()
        {
            return string.Join(" ", Word.Select(c => c == ' ' ? ' ' : (GuessedLetters.Contains(c) ? c : '_')));
        }

        public bool GuessWord(string guess)
        {
            static string Normalize(string s) =>
                new string(s.Where(char.IsLetter).ToArray()).ToLower();

            if (Normalize(guess) == Normalize(Word))
            {
                foreach (var ch in Word.Where(char.IsLetter).Distinct())
                    GuessedLetters.Add(ch);

                return true;
            }

            Lives--;
            return false;
        }
    }
}
