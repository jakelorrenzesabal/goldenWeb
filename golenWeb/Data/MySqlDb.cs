using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace golenWeb.Data
{
    public class MySqlDb
    {
        private readonly string _connectionString;

        public MySqlDb(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        // Return a connection that does not include the Database part of the connection string
        public MySqlConnection GetServerConnection()
        {
            var builder = new MySqlConnector.MySqlConnectionStringBuilder(_connectionString);
            // clear the Database so we can connect to server level and create the database
            builder.Database = string.Empty;
            return new MySqlConnection(builder.ConnectionString);
        }

        public string GetDatabaseName()
        {
            var builder = new MySqlConnector.MySqlConnectionStringBuilder(_connectionString);
            return builder.Database ?? string.Empty;
        }
    }
}
