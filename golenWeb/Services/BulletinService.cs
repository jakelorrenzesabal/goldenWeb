using System.Data.Common;
using golenWeb.Data;
using golenWeb.Models;

namespace golenWeb.Services
{
    public class BulletinService
    {
        private readonly DatabaseConnectionFactory _factory;

        public BulletinService(DatabaseConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<Bulletin>> GetAllAsync(string? category = null)
        {
            var list = new List<Bulletin>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            var sql = "SELECT Id, Title, Content, Category, Priority, PublishDate, Author, ImageUrl FROM Bulletins WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += " AND Category = @c";
                AddParam(cmd, "@c", category);
            }

            sql += " ORDER BY Id DESC";
            cmd.CommandText = sql;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Bulletin
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    Category = reader.IsDBNull(3) ? "General" : reader.GetString(3),
                    Priority = reader.IsDBNull(4) ? "Normal" : reader.GetString(4),
                    PublishDate = Convert.ToDateTime(reader.GetValue(5)),
                    Author = reader.IsDBNull(6) ? "Admin" : reader.GetString(6),
                    ImageUrl = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : null
                });
            }

            return list;
        }

        public async Task<Bulletin?> GetByIdAsync(int id)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Content, Category, Priority, PublishDate, Author, ImageUrl FROM Bulletins WHERE Id = @id LIMIT 1";
            AddParam(cmd, "@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Bulletin
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Content = reader.GetString(2),
                Category = reader.IsDBNull(3) ? "General" : reader.GetString(3),
                Priority = reader.IsDBNull(4) ? "Normal" : reader.GetString(4),
                PublishDate = Convert.ToDateTime(reader.GetValue(5)),
                Author = reader.IsDBNull(6) ? "Admin" : reader.GetString(6),
                ImageUrl = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : null
            };
        }

        public async Task<Bulletin> CreateAsync(Bulletin b)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            if (_factory.ProviderType == DbProviderType.Sqlite)
            {
                cmd.CommandText = @"
INSERT INTO Bulletins (Title, Content, Category, Priority, PublishDate, Author, ImageUrl)
VALUES (@t, @c, @cat, @pr, @pd, @a, @img);
SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"
INSERT INTO Bulletins (Title, Content, Category, Priority, PublishDate, Author, ImageUrl)
VALUES (@t, @c, @cat, @pr, @pd, @a, @img);
SELECT LAST_INSERT_ID();";
            }

            AddParam(cmd, "@t", b.Title);
            AddParam(cmd, "@c", b.Content);
            AddParam(cmd, "@cat", b.Category);
            AddParam(cmd, "@pr", b.Priority);
            AddParam(cmd, "@pd", b.PublishDate.ToString("yyyy-MM-dd"));
            AddParam(cmd, "@a", b.Author);
            AddParam(cmd, "@img", (object?)b.ImageUrl ?? DBNull.Value);

            var idObj = await cmd.ExecuteScalarAsync();
            b.Id = Convert.ToInt32(idObj);
            return b;
        }

        public async Task<bool> UpdateAsync(Bulletin b)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE Bulletins
SET Title=@t, Content=@c, Category=@cat, Priority=@pr, PublishDate=@pd, Author=@a, ImageUrl=@img
WHERE Id=@id";

            AddParam(cmd, "@t", b.Title);
            AddParam(cmd, "@c", b.Content);
            AddParam(cmd, "@cat", b.Category);
            AddParam(cmd, "@pr", b.Priority);
            AddParam(cmd, "@pd", b.PublishDate.ToString("yyyy-MM-dd"));
            AddParam(cmd, "@a", b.Author);
            AddParam(cmd, "@img", (object?)b.ImageUrl ?? DBNull.Value);
            AddParam(cmd, "@id", b.Id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Bulletins WHERE Id = @id";
            AddParam(cmd, "@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}
