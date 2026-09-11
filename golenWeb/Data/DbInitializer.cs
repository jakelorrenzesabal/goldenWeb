using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace golenWeb.Data
{
    public class DbInitializer
    {
        private readonly MySqlDb _db;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(MySqlDb db, ILogger<DbInitializer> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var dbName = _db.GetDatabaseName();
            if (string.IsNullOrWhiteSpace(dbName))
            {
                _logger.LogWarning("No database name found in connection string; skipping initialization.");
                return;
            }

            var createDbSql = $"CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;";

            try
            {
                // Ensure database exists by connecting at server level (no Database specified)
                await using (var serverConn = _db.GetServerConnection())
                {
                    await serverConn.OpenAsync();
                    await using var cmd = serverConn.CreateCommand();
                    cmd.CommandText = createDbSql;
                    await cmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("Ensured database '{dbName}' exists.", dbName);
                }

                // Now ensure required tables exist inside the database
                var createUsersTable = @"
CREATE TABLE IF NOT EXISTS `Users` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Username` VARCHAR(100) NOT NULL,
  `Email` VARCHAR(200) NULL,
  `PasswordHash` VARCHAR(512) NOT NULL,
  `CreatedAt` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Users_Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";

                await using var conn = _db.GetConnection();
                await conn.OpenAsync();
                await using var cmd2 = conn.CreateCommand();
                cmd2.CommandText = createUsersTable;
                await cmd2.ExecuteNonQueryAsync();
                _logger.LogInformation("Ensured Users table exists in database '{dbName}'.", dbName);
            }
            catch (MySqlException mex)
            {
                _logger.LogError(mex, "MySQL error while initializing database.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while initializing database.");
                throw;
            }
        }
    }
}
