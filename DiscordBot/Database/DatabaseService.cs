using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Database
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }

        public async Task<List<(string Name, int Chapter, string Status)>> GetAllWebtoonsAsync()
        {
            var webtoons = new List<(string, int, string)>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT Name, Chapter, Status FROM Webtoons", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                webtoons.Add((reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
            }

            return webtoons;
        }

        public async Task AddWebtoonAsync(string name, int chapter, string status)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("INSERT INTO Webtoons (Name, Chapter, Status) VALUES (@name, @chapter, @status)", connection);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@chapter", chapter);
            command.Parameters.AddWithValue("@status", status);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteWebtoonAsync(string name)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("DELETE FROM Webtoons WHERE Name = @name", connection);
            command.Parameters.AddWithValue("@name", name);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateWebtoonAsync(string name, int chapter, string status)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Webtoons SET Chapter = @chapter, Status = @status WHERE Name = @name", connection);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@chapter", chapter);
            command.Parameters.AddWithValue("@status", status);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task AddGifAsync(string name, string url)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("INSERT INTO Gifs (Name, Url) VALUES (@Name, @Url)", connection);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Url", url);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteGifAsync(string name)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("DELETE FROM Gifs WHERE Name = @Name", connection);
            command.Parameters.AddWithValue("@Name", name);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<(string Name, string Url)>> GetGifsAsync()
        {
            var gifs = new List<(string, string)>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT Name, Url FROM Gifs", connection);
            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                gifs.Add((reader.GetString(0), reader.GetString(1)));
            }

            return gifs;
        }

        public async Task UpdateGifAsync(string name, string newUrl)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Gifs SET Url = @Url WHERE Name = @Name", connection);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Url", newUrl);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> GetUserXPAsync(ulong userId)
        {
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("SELECT XP FROM UserXP WHERE UserId = @UserId", conn);

            cmd.Parameters.Add("@UserId", System.Data.SqlDbType.BigInt).Value = unchecked((long)userId);

            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();

            return result != null ? Convert.ToInt32(result) : 0;

        }

        public async Task<(bool leveledUp, int newLevel, int newXP)> AddXPAsync(ulong userId, string username, int xpToAdd = 1)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var checkCmd = new SqlCommand("SELECT XP, Level FROM UserXP WHERE UserId = @UserId", conn);
                checkCmd.Parameters.AddWithValue("@UserId", unchecked((long)userId));

                await conn.OpenAsync();
                var reader = await checkCmd.ExecuteReaderAsync();

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

                var cmd = new SqlCommand(@"
            MERGE UserXP AS target
            USING (SELECT @UserId AS UserId) AS source
            ON target.UserId = source.UserId
            WHEN MATCHED THEN
                UPDATE SET XP = @XP, UserName = @UserName, Level = @Level
            WHEN NOT MATCHED THEN
                INSERT (UserId, UserName, XP, Level) VALUES (@UserId, @UserName, @XP, @Level);", conn);

                cmd.Parameters.AddWithValue("@UserId", unchecked((long)userId));
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
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);

            var list = new List<(ulong, string, int, int)>();

            try
            {
                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

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
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("SELECT Level FROM UserXP WHERE UserId = @UserId", conn);
            cmd.Parameters.Add("@UserId", System.Data.SqlDbType.BigInt).Value = unchecked((long)userId);

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

        public async Task<bool> AddCommandAsync(string name, string description)
        {
            const string query = "INSERT INTO Commands (Name, Description) VALUES (@Name, @Description)";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Description", description);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateCommandAsync(string name, string description)
        {
            const string query = "UPDATE Commands SET Description = @Description WHERE Name = @Name";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Description", description);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteCommandAsync(string name)
        {
            const string query = "DELETE FROM Commands WHERE Name = @Name";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Name", name);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<(string Name, string Description)>> GetAllCommandsAsync()
        {
            const string query = "SELECT Name, Description FROM Commands ORDER BY Name";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            await conn.OpenAsync();

            var list = new List<(string, string)>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var desc = reader.GetString(1);
                list.Add((name, desc));
            }

            return list;
        }
    }
}
