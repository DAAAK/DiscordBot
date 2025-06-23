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
    }
}
