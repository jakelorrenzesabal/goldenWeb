using System.Data.Common;
using golenWeb.Data;
using golenWeb.Models;
using Microsoft.AspNetCore.Http;

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
            var sql = "SELECT BulletinId, Title, Content, Category, Priority, PublishDate, Author, ImageData, ImageContentType, ImageHash, CreatedByUserId FROM Bulletins WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += " AND Category = @c";
                AddParam(cmd, "@c", category);
            }

            sql += " ORDER BY PublishDate DESC, BulletinId DESC";
            cmd.CommandText = sql;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(ReadBulletin(reader));
            }

            return list;
        }

        /// <summary>
        /// Generates JSON DTO for Bulletin details modal.
        /// </summary>
        public async Task<BulletinJsonDto?> GetBulletinJsonDtoAsync(int id)
        {
            var b = await GetByIdAsync(id);
            if (b == null) return null;

            return new BulletinJsonDto
            {
                BulletinId = b.BulletinId,
                Title = b.Title,
                Content = b.Content,
                Category = b.Category,
                Priority = b.Priority,
                PublishDate = b.PublishDate.ToString("yyyy-MM-dd"),
                FormattedDate = b.PublishDate.ToString("MMMM dd, yyyy"),
                Author = b.Author,
                HasImage = b.ImageData != null && b.ImageData.Length > 0,
                ImageUrl = b.ImageData != null && b.ImageData.Length > 0 ? $"/Bulletins/Image/{b.BulletinId}" : null
            };
        }

        /// <summary>
        /// Returns default Bulletin pre-populated values.
        /// </summary>
        public Bulletin GetDefaultBulletin()
        {
            return new Bulletin
            {
                PublishDate = DateTime.Today,
                Category = "General Notice",
                Priority = "Normal",
                Author = "Office of Student Affairs"
            };
        }

        /// <summary>
        /// Handles file processing and creation for Bulletin entries.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> ProcessAndSaveBulletinAsync(Bulletin bulletin, IFormFile? imageFile, FileStorageService fileStorage)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var (data, contentType, hash, error) = await fileStorage.ProcessAndStoreImageInDbAsync(imageFile);
                if (!string.IsNullOrEmpty(error))
                {
                    return (false, error);
                }
                bulletin.ImageData = data;
                bulletin.ImageContentType = contentType;
                bulletin.ImageHash = hash;
            }

            await CreateAsync(bulletin);
            return (true, null);
        }

        /// <summary>
        /// Handles file processing and updating for Bulletin entries, preserving original images if omitted.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> ProcessAndUpdateBulletinAsync(int id, Bulletin bulletin, IFormFile? imageFile, FileStorageService fileStorage)
        {
            if (id != bulletin.BulletinId) return (false, "ID mismatch");

            if (imageFile != null && imageFile.Length > 0)
            {
                var (data, contentType, hash, error) = await fileStorage.ProcessAndStoreImageInDbAsync(imageFile);
                if (!string.IsNullOrEmpty(error))
                {
                    return (false, error);
                }
                bulletin.ImageData = data;
                bulletin.ImageContentType = contentType;
                bulletin.ImageHash = hash;
            }
            else
            {
                var existing = await GetByIdAsync(id);
                if (existing != null)
                {
                    bulletin.ImageData = existing.ImageData;
                    bulletin.ImageContentType = existing.ImageContentType;
                    bulletin.ImageHash = existing.ImageHash;
                    if (!bulletin.CreatedByUserId.HasValue)
                    {
                        bulletin.CreatedByUserId = existing.CreatedByUserId;
                    }
                }
            }

            var updated = await UpdateAsync(bulletin);
            return (updated, updated ? null : "Bulletin notice not found");
        }

        public async Task<Bulletin?> GetByIdAsync(int id)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT BulletinId, Title, Content, Category, Priority, PublishDate, Author, ImageData, ImageContentType, ImageHash, CreatedByUserId FROM Bulletins WHERE BulletinId = @id LIMIT 1";
            AddParam(cmd, "@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return ReadBulletin(reader);
        }

        public async Task<Bulletin> CreateAsync(Bulletin b)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            if (_factory.ProviderType == DbProviderType.Sqlite)
            {
                cmd.CommandText = @"
INSERT INTO Bulletins (Title, Content, Category, Priority, PublishDate, Author, ImageData, ImageContentType, ImageHash, CreatedByUserId)
VALUES (@t, @c, @cat, @pr, @pd, @a, @imgData, @imgType, @imgHash, @cb);
SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"
INSERT INTO Bulletins (Title, Content, Category, Priority, PublishDate, Author, ImageData, ImageContentType, ImageHash, CreatedByUserId)
VALUES (@t, @c, @cat, @pr, @pd, @a, @imgData, @imgType, @imgHash, @cb);
SELECT LAST_INSERT_ID();";
            }

            AddParam(cmd, "@t", b.Title);
            AddParam(cmd, "@c", b.Content);
            AddParam(cmd, "@cat", b.Category);
            AddParam(cmd, "@pr", b.Priority);
            AddParam(cmd, "@pd", b.PublishDate.ToString("yyyy-MM-dd"));
            AddParam(cmd, "@a", b.Author);
            AddParam(cmd, "@imgData", (object?)b.ImageData ?? DBNull.Value);
            AddParam(cmd, "@imgType", (object?)b.ImageContentType ?? DBNull.Value);
            AddParam(cmd, "@imgHash", (object?)b.ImageHash ?? DBNull.Value);
            AddParam(cmd, "@cb", (object?)b.CreatedByUserId ?? DBNull.Value);

            var idObj = await cmd.ExecuteScalarAsync();
            b.BulletinId = Convert.ToInt32(idObj);
            return b;
        }

        public async Task<bool> UpdateAsync(Bulletin b)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE Bulletins
SET Title=@t, Content=@c, Category=@cat, Priority=@pr, PublishDate=@pd, Author=@a, ImageData=@imgData, ImageContentType=@imgType, ImageHash=@imgHash, CreatedByUserId=@cb
WHERE BulletinId=@id";

            AddParam(cmd, "@t", b.Title);
            AddParam(cmd, "@c", b.Content);
            AddParam(cmd, "@cat", b.Category);
            AddParam(cmd, "@pr", b.Priority);
            AddParam(cmd, "@pd", b.PublishDate.ToString("yyyy-MM-dd"));
            AddParam(cmd, "@a", b.Author);
            AddParam(cmd, "@imgData", (object?)b.ImageData ?? DBNull.Value);
            AddParam(cmd, "@imgType", (object?)b.ImageContentType ?? DBNull.Value);
            AddParam(cmd, "@imgHash", (object?)b.ImageHash ?? DBNull.Value);
            AddParam(cmd, "@cb", (object?)b.CreatedByUserId ?? DBNull.Value);
            AddParam(cmd, "@id", b.BulletinId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Bulletins WHERE BulletinId = @id";
            AddParam(cmd, "@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static Bulletin ReadBulletin(DbDataReader reader)
        {
            return new Bulletin
            {
                BulletinId = reader.GetInt32(0),
                Title = reader.GetString(1),
                Content = reader.GetString(2),
                Category = reader.IsDBNull(3) ? "General" : reader.GetString(3),
                Priority = reader.IsDBNull(4) ? "Normal" : reader.GetString(4),
                PublishDate = Convert.ToDateTime(reader.GetValue(5)),
                Author = reader.IsDBNull(6) ? "Admin" : reader.GetString(6),
                ImageData = reader.FieldCount > 7 && !reader.IsDBNull(7) ? (byte[])reader.GetValue(7) : null,
                ImageContentType = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : null,
                ImageHash = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetString(9) : null,
                CreatedByUserId = reader.FieldCount > 10 && !reader.IsDBNull(10) ? Convert.ToInt32(reader.GetValue(10)) : null
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
