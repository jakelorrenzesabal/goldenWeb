using System.Data.Common;
using System.Security.Cryptography;
using golenWeb.Data;
using golenWeb.Models;

namespace golenWeb.Services
{
    public class AuthService
    {
        private readonly DatabaseConnectionFactory _factory;

        public AuthService(DatabaseConnectionFactory factory)
        {
            _factory = factory;
        }

        // Create a user (username must be unique)
        public async Task<int> CreateUserAsync(string username, string email, string password, string role = "User")
        {
            var hash = HashPassword(password);
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            if (_factory.ProviderType == DbProviderType.Sqlite)
            {
                cmd.CommandText = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@u,@e,@p,@r); SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@u,@e,@p,@r); SELECT LAST_INSERT_ID();";
            }

            AddParam(cmd, "@u", username);
            AddParam(cmd, "@e", email);
            AddParam(cmd, "@p", hash);
            AddParam(cmd, "@r", role);

            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Email, PasswordHash, Role FROM Users WHERE Username = @u LIMIT 1";
            AddParam(cmd, "@u", username);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            var storedHash = reader.GetString(3);
            if (!VerifyPassword(password, storedHash)) return null;

            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                PasswordHash = storedHash,
                Role = reader.IsDBNull(4) ? "User" : reader.GetString(4)
            };
        }

        public async Task<List<User>> GetAllAsync()
        {
            var list = new List<User>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Email, PasswordHash, Role FROM Users ORDER BY Id DESC";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    Role = reader.IsDBNull(4) ? "User" : reader.GetString(4)
                });
            }
            return list;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Email, PasswordHash, Role FROM Users WHERE Id = @id LIMIT 1";
            AddParam(cmd, "@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                PasswordHash = reader.GetString(3),
                Role = reader.IsDBNull(4) ? "User" : reader.GetString(4)
            };
        }

        public async Task UpdateUserAsync(int id, string username, string email, string role, string? newPassword = null)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            if (!string.IsNullOrEmpty(newPassword))
            {
                var hash = HashPassword(newPassword);
                cmd.CommandText = "UPDATE Users SET Username=@u, Email=@e, PasswordHash=@p, Role=@r WHERE Id=@id";
                AddParam(cmd, "@p", hash);
            }
            else
            {
                cmd.CommandText = "UPDATE Users SET Username=@u, Email=@e, Role=@r WHERE Id=@id";
            }
            AddParam(cmd, "@u", username);
            AddParam(cmd, "@e", email);
            AddParam(cmd, "@r", role);
            AddParam(cmd, "@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateRoleAsync(int id, string role)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET Role=@r WHERE Id=@id";
            AddParam(cmd, "@r", role);
            AddParam(cmd, "@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id=@id";
            AddParam(cmd, "@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> GetUserCountAsync()
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        private static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            const int iterations = 100_000;
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
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
                byte[] candidate = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hash.Length);
                return CryptographicOperations.FixedTimeEquals(candidate, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
