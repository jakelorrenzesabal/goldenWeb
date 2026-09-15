using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace golenWeb.Data
{
    public enum DbProviderType
    {
        Sqlite,
        MySql
    }

    public class DatabaseConnectionFactory
    {
        private readonly IConfiguration _config;
        private readonly DbProviderType _providerType;
        private readonly string _connectionString;

        public DatabaseConnectionFactory(IConfiguration config)
        {
            _config = config;
            var providerStr = config["Database:Provider"] ?? "Sqlite";
            
            if (providerStr.Equals("MySql", StringComparison.OrdinalIgnoreCase))
            {
                _providerType = DbProviderType.MySql;
                _connectionString = config.GetConnectionString("MySqlConnection") 
                    ?? config.GetConnectionString("DefaultConnection") 
                    ?? "Server=127.0.0.1;Port=3306;Database=golenweb_db;User=root;Password=;";
            }
            else
            {
                _providerType = DbProviderType.Sqlite;
                _connectionString = config.GetConnectionString("SqliteConnection") 
                    ?? config.GetConnectionString("DefaultConnection") 
                    ?? "Data Source=golenWeb.db;";
            }
        }

        public DbProviderType ProviderType => _providerType;
        public string ConnectionString => _connectionString;

        public DbConnection CreateConnection()
        {
            if (_providerType == DbProviderType.MySql)
            {
                return new MySqlConnection(_connectionString);
            }
            else
            {
                return new SqliteConnection(_connectionString);
            }
        }

        // Return connection without database parameter (for MySQL database creation)
        public DbConnection CreateServerConnection()
        {
            if (_providerType == DbProviderType.MySql)
            {
                var builder = new MySqlConnectionStringBuilder(_connectionString);
                builder.Database = string.Empty;
                return new MySqlConnection(builder.ConnectionString);
            }
            else
            {
                return new SqliteConnection(_connectionString);
            }
        }

        public string GetDatabaseName()
        {
            if (_providerType == DbProviderType.MySql)
            {
                var builder = new MySqlConnectionStringBuilder(_connectionString);
                return builder.Database ?? "golenweb_db";
            }
            else
            {
                return "golenWeb.db";
            }
        }
    }
}
