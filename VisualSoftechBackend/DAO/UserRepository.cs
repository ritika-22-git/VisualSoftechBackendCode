using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using VisualSoftechBackend.Models;

namespace VisualSoftechBackend.DAO
{
    public class UserRepository : IUserRepository
    {
        private readonly string _conn;
        public UserRepository(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection");
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            const string sql = "SELECT Id, Username, PasswordHash, DisplayName, CreatedAt FROM dbo.Users WHERE Username = @Username";
            await using var cn = new SqlConnection(_conn);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Username", username);
            await cn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await reader.ReadAsync()) return null;
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }

        public async Task<int> CreateAsync(User user)
        {
            const string sql = @"INSERT INTO dbo.Users (Username, PasswordHash, DisplayName) 
                             VALUES (@Username, @PasswordHash, @DisplayName);
                             SELECT CAST(SCOPE_IDENTITY() as int);";
            await using var cn = new SqlConnection(_conn);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Username", user.Username);
            cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@DisplayName", (object?)user.DisplayName ?? DBNull.Value);
            await cn.OpenAsync();
            var id = (int)await cmd.ExecuteScalarAsync();
            return id;
        }
    }
}
