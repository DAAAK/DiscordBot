using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DiscordBot.Database
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration["DATABASE_URL"] ?? throw new ArgumentNullException(nameof(configuration), "DATABASE_URL configuration is missing.");
        }

        public async Task<List<(string Name, int Chapter, string Status)>> GetAllWebtoonsAsync()
        {
            var webtoons = new List<(string, int, string)>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(
                "SELECT Name, Chapter, Status FROM Webtoons", connection);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                webtoons.Add((reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
            }

            return webtoons;
        }

        public async Task<bool> AddWebtoonAsync(string name, int chapter, string status)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(
                "INSERT INTO Webtoons (Name, Chapter, Status) VALUES (@name, @chapter, @status)",
                connection);

            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@chapter", chapter);
            command.Parameters.AddWithValue("@status", status);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteWebtoonAsync(string name)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(
                "DELETE FROM Webtoons WHERE Name = @name",
                connection);

            command.Parameters.AddWithValue("@name", name);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateWebtoonAsync(string name, int chapter, string status)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(
                "UPDATE Webtoons SET Chapter = @chapter, Status = @status WHERE Name = @name",
                connection);

            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@chapter", chapter);
            command.Parameters.AddWithValue("@status", status);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task AddGifAsync(string name, string url)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(
                "INSERT INTO Gifs (Name, Url) VALUES (@Name, @Url)",
                connection);

            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Url", url);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteGifAsync(string name)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(
                "DELETE FROM Gifs WHERE Name = @Name",
                connection);

            command.Parameters.AddWithValue("@Name", name);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<(string Name, string Url)>> GetGifsAsync()
        {
            var gifs = new List<(string, string)>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(
                "SELECT Name, Url FROM Gifs",
                connection);

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                gifs.Add((reader.GetString(0), reader.GetString(1)));
            }

            return gifs;
        }

        public async Task UpdateGifAsync(string name, string newUrl)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(
                "UPDATE Gifs SET Url = @Url WHERE Name = @Name",
                connection);

            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Url", newUrl);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> GetUserXPAsync(ulong userId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(
                "SELECT XP FROM UserXP WHERE UserId = @UserId",
                conn);

            cmd.Parameters.AddWithValue("@UserId", (long)userId);

            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<(bool leveledUp, int newLevel, int newXP)> AddXPAsync(ulong userId, string username, int xpToAdd = 1)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var checkCmd = new NpgsqlCommand(
                    "SELECT XP, Level FROM UserXP WHERE UserId = @UserId",
                    conn);

                checkCmd.Parameters.AddWithValue("@UserId", (long)userId);

                await using var reader = await checkCmd.ExecuteReaderAsync();

                int currentXP = 0;
                int currentLevel = 0;

                if (await reader.ReadAsync())
                {
                    currentXP = reader.GetInt32(0);
                    currentLevel = reader.GetInt32(1);
                }

                await reader.CloseAsync();

                int newXP = currentXP + xpToAdd;
                int newLevel = (int)Math.Floor(Math.Sqrt(newXP / 10.0));
                bool leveledUp = newLevel > currentLevel;

                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO UserXP (UserId, UserName, XP, Level)
                    VALUES (@UserId, @UserName, @XP, @Level)
                    ON CONFLICT (UserId)
                    DO UPDATE SET
                        XP = EXCLUDED.XP,
                        UserName = EXCLUDED.UserName,
                        Level = EXCLUDED.Level;",
                    conn);

                cmd.Parameters.AddWithValue("@UserId", (long)userId);
                cmd.Parameters.AddWithValue("@UserName", username);
                cmd.Parameters.AddWithValue("@XP", newXP);
                cmd.Parameters.AddWithValue("@Level", newLevel);

                await cmd.ExecuteNonQueryAsync();

                return (leveledUp, newLevel, newXP);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DB error for {username}: {ex.Message}");
                return (false, 0, 0);
            }
        }

        public async Task<List<(ulong UserId, string Name, int Level, int XP)>> GetLeaderboardAsync()
        {
            const string query = "SELECT UserId, UserName, Level, XP FROM UserXP ORDER BY Level DESC, XP DESC";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            var list = new List<(ulong, string, int, int)>();

            try
            {
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var userId = (ulong)reader.GetInt64(0);
                    var userName = reader.GetString(1);
                    var level = reader.GetInt32(2);
                    var xp = reader.GetInt32(3);

                    list.Add((userId, userName, level, xp));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return list;
        }

        public async Task<int> GetUserLevelAsync(ulong userId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(
                "SELECT Level FROM UserXP WHERE UserId = @UserId",
                conn);

            cmd.Parameters.AddWithValue("@UserId", (long)userId);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            return result != null ? Convert.ToInt32(result) : 0;
        }

        public (int Level, int CurrentXP, int XPForNext, int XPRemaining) GetXPStats(int xp, int level)
        {
            int nextLevelXP = (int)((level + 1) * (level + 1) * 10);
            int xpRemaining = nextLevelXP - xp;

            return (level, xp, nextLevelXP, xpRemaining);
        }

        public async Task<int> GetUserRankAsync(ulong userId)
        {
            const string query = @"
                SELECT COUNT(*) + 1
                FROM UserXP
                WHERE (Level > (SELECT Level FROM UserXP WHERE UserId = @userId))
                   OR (Level = (SELECT Level FROM UserXP WHERE UserId = @userId)
                       AND XP > (SELECT XP FROM UserXP WHERE UserId = @userId))";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@userId", (long)userId);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> AddCommandAsync(string name, string description)
        {
            const string query = "INSERT INTO Commands (Name, Description) VALUES (@Name, @Description)";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Description", description);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateCommandAsync(string name, string description)
        {
            const string query = "UPDATE Commands SET Description = @Description WHERE Name = @Name";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Description", description);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteCommandAsync(string name)
        {
            const string query = "DELETE FROM Commands WHERE Name = @Name";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Name", name);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<(string Name, string Description)>> GetAllCommandsAsync()
        {
            const string query = "SELECT Name, Description FROM Commands ORDER BY Name";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            await conn.OpenAsync();

            var list = new List<(string, string)>();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var desc = reader.GetString(1);
                list.Add((name, desc));
            }

            return list;
        }

        public async Task<List<(ulong DiscordUserId, string TwitchUsername, int LastLiveStatus)>> GetAllStreamersAsync()
        {
            const string query = "SELECT DiscordUserId, TwitchUsername, LastLiveStatus FROM Streamers";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            await conn.OpenAsync();

            var list = new List<(ulong, string, int)>();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = (ulong)reader.GetInt64(0);
                var twitch = reader.GetString(1);
                var status = reader.GetInt32(2);
                list.Add((id, twitch, status));
            }

            return list;
        }

        public async Task UpdateLiveStatus(ulong discordUserId, bool isLive)
        {
            const string query = "UPDATE Streamers SET LastLiveStatus = @status WHERE DiscordUserId = @id";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", (long)discordUserId);
            cmd.Parameters.AddWithValue("@status", isLive ? 1 : 0);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> AddStreamerAsync(ulong discordId, string twitchName)
        {
            const string query = @"
                INSERT INTO Streamers (DiscordUserId, TwitchUsername, LastLiveStatus)
                VALUES (@id, @twitch, 0)
                ON CONFLICT (DiscordUserId) DO NOTHING";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", (long)discordId);
            cmd.Parameters.AddWithValue("@twitch", twitchName);

            await conn.OpenAsync();
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteStreamerAsync(ulong discordId)
        {
            const string query = "DELETE FROM Streamers WHERE DiscordUserId = @id";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", (long)discordId);

            await conn.OpenAsync();
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> UpdateStreamerAsync(ulong discordId, string newTwitchName)
        {
            const string query = "UPDATE Streamers SET TwitchUsername = @twitch WHERE DiscordUserId = @id";

            await using var conn = new NpgsqlConnection(_connectionString);
            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", (long)discordId);
            cmd.Parameters.AddWithValue("@twitch", newTwitchName);

            await conn.OpenAsync();
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}