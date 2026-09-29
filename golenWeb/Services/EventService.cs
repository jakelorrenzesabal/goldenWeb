using System.Data.Common;
using golenWeb.Data;
using golenWeb.Models;

namespace golenWeb.Services
{
    public class EventService
    {
        private readonly DatabaseConnectionFactory _factory;

        public EventService(DatabaseConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<EventModel>> GetAllAsync(string? search = null, string? category = null, DateTime? date = null)
        {
            var list = new List<EventModel>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            var sql = "SELECT Id, Title, Description, EventDate, StartTime, EndTime, Location, Organizer, Category, IsFeatured, CreatedAt, ImageData, ImageContentType, ImageHash FROM Events WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (Title LIKE @s OR Description LIKE @s OR Location LIKE @s)";
                AddParam(cmd, "@s", $"%{search}%");
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += " AND Category = @c";
                AddParam(cmd, "@c", category);
            }

            if (date.HasValue)
            {
                sql += " AND DATE(EventDate) = DATE(@d)";
                AddParam(cmd, "@d", date.Value.ToString("yyyy-MM-dd"));
            }

            sql += " ORDER BY EventDate ASC, StartTime ASC";
            cmd.CommandText = sql;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(ReadEvent(reader));
            }

            return list;
        }

        public async Task<List<EventModel>> GetTodayEventsAsync()
        {
            return await GetAllAsync(date: DateTime.Today);
        }

        public async Task<EventModel?> GetByIdAsync(int id)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Description, EventDate, StartTime, EndTime, Location, Organizer, Category, IsFeatured, CreatedAt, ImageData, ImageContentType, ImageHash FROM Events WHERE Id = @id LIMIT 1";
            AddParam(cmd, "@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return ReadEvent(reader);
        }

        public async Task<EventModel> CreateAsync(EventModel ev)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            if (_factory.ProviderType == DbProviderType.Sqlite)
            {
                cmd.CommandText = @"
INSERT INTO Events (Title, Description, EventDate, StartTime, EndTime, Location, Organizer, Category, IsFeatured, CreatedAt, ImageData, ImageContentType, ImageHash)
VALUES (@t, @d, @ed, @st, @et, @loc, @org, @cat, @feat, @dt, @imgData, @imgType, @imgHash);
SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"
INSERT INTO Events (Title, Description, EventDate, StartTime, EndTime, Location, Organizer, Category, IsFeatured, CreatedAt, ImageData, ImageContentType, ImageHash)
VALUES (@t, @d, @ed, @st, @et, @loc, @org, @cat, @feat, @dt, @imgData, @imgType, @imgHash);
SELECT LAST_INSERT_ID();";
            }

            AddParam(cmd, "@t", ev.Title);
            AddParam(cmd, "@d", (object?)ev.Description ?? DBNull.Value);
            AddParam(cmd, "@ed", ev.EventDate.ToString("yyyy-MM-dd"));
            AddParam(cmd, "@st", ev.StartTime);
            AddParam(cmd, "@et", ev.EndTime);
            AddParam(cmd, "@loc", ev.Location);
            AddParam(cmd, "@org", ev.Organizer);
            AddParam(cmd, "@cat", ev.Category);
            AddParam(cmd, "@feat", ev.IsFeatured ? 1 : 0);
            AddParam(cmd, "@dt", DateTime.UtcNow);
            AddParam(cmd, "@imgData", (object?)ev.ImageData ?? DBNull.Value);
            AddParam(cmd, "@imgType", (object?)ev.ImageContentType ?? DBNull.Value);
            AddParam(cmd, "@imgHash", (object?)ev.ImageHash ?? DBNull.Value);

            var idObj = await cmd.ExecuteScalarAsync();
            ev.Id = Convert.ToInt32(idObj);
            return ev;
        }

        public async Task<bool> UpdateAsync(EventModel ev)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE Events
SET Title=@t, Description=@d, EventDate=@ed, StartTime=@st, EndTime=@et, Location=@loc, Organizer=@org, Category=@cat, IsFeatured=@feat, ImageData=@imgData, ImageContentType=@imgType, ImageHash=@imgHash
WHERE Id=@id";

            AddParam(cmd, "@t", ev.Title);
            AddParam(cmd, "@d", (object?)ev.Description ?? DBNull.Value);
            AddParam(cmd, "@ed", ev.EventDate.ToString("yyyy-MM-dd"));
            AddParam(cmd, "@st", ev.StartTime);
            AddParam(cmd, "@et", ev.EndTime);
            AddParam(cmd, "@loc", ev.Location);
            AddParam(cmd, "@org", ev.Organizer);
            AddParam(cmd, "@cat", ev.Category);
            AddParam(cmd, "@feat", ev.IsFeatured ? 1 : 0);
            AddParam(cmd, "@imgData", (object?)ev.ImageData ?? DBNull.Value);
            AddParam(cmd, "@imgType", (object?)ev.ImageContentType ?? DBNull.Value);
            AddParam(cmd, "@imgHash", (object?)ev.ImageHash ?? DBNull.Value);
            AddParam(cmd, "@id", ev.Id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Events WHERE Id = @id";
            AddParam(cmd, "@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> DeleteAllAsync()
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Events";
            return await cmd.ExecuteNonQueryAsync();
        }

        private static EventModel ReadEvent(DbDataReader r)
        {
            return new EventModel
            {
                Id = r.GetInt32(0),
                Title = r.GetString(1),
                Description = r.IsDBNull(2) ? null : r.GetString(2),
                EventDate = Convert.ToDateTime(r.GetValue(3)),
                StartTime = r.IsDBNull(4) ? "09:00 AM" : r.GetString(4),
                EndTime = r.IsDBNull(5) ? "04:00 PM" : r.GetString(5),
                Location = r.IsDBNull(6) ? "Main Campus" : r.GetString(6),
                Organizer = r.IsDBNull(7) ? "GSC Admin" : r.GetString(7),
                Category = r.IsDBNull(8) ? "Academic" : r.GetString(8),
                IsFeatured = r.IsDBNull(9) ? true : Convert.ToBoolean(r.GetValue(9)),
                CreatedAt = r.IsDBNull(10) ? DateTime.UtcNow : Convert.ToDateTime(r.GetValue(10)),
                ImageData = r.FieldCount > 11 && !r.IsDBNull(11) ? (byte[])r.GetValue(11) : null,
                ImageContentType = r.FieldCount > 12 && !r.IsDBNull(12) ? r.GetString(12) : null,
                ImageHash = r.FieldCount > 13 && !r.IsDBNull(13) ? r.GetString(13) : null
            };
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
