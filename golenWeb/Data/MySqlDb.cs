using System.Data.Common;

namespace golenWeb.Data
{
    public class MySqlDb
    {
        private readonly DatabaseConnectionFactory _factory;

        public MySqlDb(DatabaseConnectionFactory factory)
        {
            _factory = factory;
        }

        public DbConnection GetConnection()
        {
            return _factory.CreateConnection();
        }

        public DbConnection GetServerConnection()
        {
            return _factory.CreateServerConnection();
        }

        public string GetDatabaseName()
        {
            return _factory.GetDatabaseName();
        }
    }
}
