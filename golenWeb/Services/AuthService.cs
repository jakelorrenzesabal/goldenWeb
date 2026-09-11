using System.Security.Cryptography;
using System.Text;
using golenWeb.Data;
using golenWeb.Models;
using MySqlConnector;

namespace golenWeb.Services
{
    public class AuthService
    {
        private readonly MySqlDb _db;

        public AuthService(MySqlDb db)
        {
            _db = db;
        }

        // Create a user (username must be unique)
        public async Task<int> CreateUserAsync(string username, string email, string password)
        {
            var hash = HashPassword(password);
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Users (Username, Email, PasswordHash) VALUES (@u,@e,@p); SELECT LAST_INSERT_ID();";
            cmd.Parameters.Add(new MySqlParameter("@u", username));
            cmd.Parameters.Add(new MySqlParameter("@e", email));
            cmd.Parameters.Add(new MySqlParameter("@p", hash));
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Email, PasswordHash FROM Users WHERE Username = @u LIMIT 1";
            cmd.Parameters.Add(new MySqlParameter("@u", username));
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            var storedHash = reader.GetString("PasswordHash");
            if (!VerifyPassword(password, storedHash)) return null;
            return new User
            {
                Id = reader.GetInt32("Id"),
                Username = reader.GetString("Username"),
                Email = reader.GetString("Email"),
                PasswordHash = storedHash
            };
        }

        public async Task<List<User>> GetAllAsync()
        {
            var list = new List<User>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Email, PasswordHash FROM Users ORDER BY Id DESC";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new User
                {
                    Id = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    PasswordHash = reader.GetString("PasswordHash")
                });
            }
            return list;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Email, PasswordHash FROM Users WHERE Id = @id LIMIT 1";
            cmd.Parameters.Add(new MySqlParameter("@id", id));
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new User
            {
                Id = reader.GetInt32("Id"),
                Username = reader.GetString("Username"),
                Email = reader.GetString("Email"),
                PasswordHash = reader.GetString("PasswordHash")
            };
        }

        public async Task UpdateUserAsync(int id, string username, string email, string? newPassword = null)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            if (!string.IsNullOrEmpty(newPassword))
            {
                var hash = HashPassword(newPassword);
                cmd.CommandText = "UPDATE Users SET Username=@u, Email=@e, PasswordHash=@p WHERE Id=@id";
                cmd.Parameters.Add(new MySqlParameter("@p", hash));
            }
            else
            {
                cmd.CommandText = "UPDATE Users SET Username=@u, Email=@e WHERE Id=@id";
            }
            cmd.Parameters.Add(new MySqlParameter("@u", username));
            cmd.Parameters.Add(new MySqlParameter("@e", email));
            cmd.Parameters.Add(new MySqlParameter("@id", id));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id=@id";
            cmd.Parameters.Add(new MySqlParameter("@id", id));
            await cmd.ExecuteNonQueryAsync();
        }

        // Password hashing helpers (PBKDF2)
        private static string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[16];
            rng.GetBytes(salt);
            const int iterations = 100_000;
            using var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = derive.GetBytes(32);
            return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string stored)
        {
            try
            {
                var parts = stored.Split('.');
                if (parts.Length != 3) return false;
                var iterations = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var hash = Convert.FromBase64String(parts[2]);
                using var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
                var candidate = derive.GetBytes(hash.Length);
                return CryptographicOperations.FixedTimeEquals(candidate, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
