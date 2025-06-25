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



        public async Task<List<(ulong UserId, int Level, int XP)>> GetLeaderboardAsync()
        {
            var list = new List<(ulong, int, int)>();
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("SELECT TOP 10 UserId, Level, XP FROM UserXP ORDER BY Level DESC, XP DESC", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(((ulong)(long)reader["UserId"], (int)reader["Level"], (int)reader["XP"]));
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
    }
}
