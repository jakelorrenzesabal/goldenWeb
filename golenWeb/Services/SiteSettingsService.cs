using System.Data.Common;
using golenWeb.Data;
using golenWeb.Models;

namespace golenWeb.Services
{
    public class SiteSettingsService
    {
        private readonly DatabaseConnectionFactory _factory;

        public SiteSettingsService(DatabaseConnectionFactory factory)
        {
            _factory = factory;
        }

        /// <summary>Returns all settings as a dictionary (key → value).</summary>
        public async Task<Dictionary<string, string>> GetAllAsync()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT `Key`, `Value` FROM SiteSettings";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dict[reader.GetString(0)] = reader.GetString(1);
            }
            return dict;
        }

        /// <summary>Returns the value for a single key, or a default if not found.</summary>
        public async Task<string> GetAsync(string key, string defaultValue = "")
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT `Value` FROM SiteSettings WHERE `Key` = @k LIMIT 1";
            AddParam(cmd, "@k", key);
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? defaultValue : result.ToString()!;
        }

        /// <summary>Upserts a single key-value setting.</summary>
        public async Task SetAsync(string key, string value)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            if (_factory.ProviderType == DbProviderType.Sqlite)
            {
                cmd.CommandText = @"
INSERT INTO SiteSettings (Key, Value) VALUES (@k, @v)
ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
            }
            else
            {
                cmd.CommandText = @"
INSERT INTO SiteSettings (`Key`, `Value`) VALUES (@k, @v)
ON DUPLICATE KEY UPDATE `Value` = VALUES(`Value`);";
            }

            AddParam(cmd, "@k", key);
            AddParam(cmd, "@v", value);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Bulk upserts a dictionary of settings.</summary>
        public async Task SetBulkAsync(Dictionary<string, string> settings)
        {
            foreach (var kv in settings)
            {
                await SetAsync(kv.Key, kv.Value);
            }
        }

        /// <summary>Builds a SiteSettingsViewModel from the database.</summary>
        public async Task<SiteSettingsViewModel> GetViewModelAsync()
        {
            var dict = await GetAllAsync();
            return new SiteSettingsViewModel
            {
                SiteTitle = dict.GetValueOrDefault("SiteTitle", "Golden Success College"),
                HeroTitle = dict.GetValueOrDefault("HeroTitle", "Welcome to Golden Success College"),
                HeroSubtitle = dict.GetValueOrDefault("HeroSubtitle", "Your path to excellence starts here"),
                Tagline = dict.GetValueOrDefault("Tagline", "Determination • Courage • Hardwork • Isa. 33:6"),
                AboutText = dict.GetValueOrDefault("AboutText", "Golden Success College is committed to providing quality education."),
                FooterText = dict.GetValueOrDefault("FooterText", "Golden Success College • All Rights Reserved"),
                PrimaryColor = dict.GetValueOrDefault("PrimaryColor", "#0b5e28"),
                AccentColor = dict.GetValueOrDefault("AccentColor", "#e6b800"),
                NavBackground = dict.GetValueOrDefault("NavBackground", "#073d1a"),
                ContactEmail = dict.GetValueOrDefault("ContactEmail", "info@goldensuccess.edu"),
                ContactPhone = dict.GetValueOrDefault("ContactPhone", ""),
                Address = dict.GetValueOrDefault("Address", ""),
                AnnouncementBanner = dict.GetValueOrDefault("AnnouncementBanner", ""),
                ShowAnnouncementBanner = dict.GetValueOrDefault("ShowAnnouncementBanner", "false") == "true"
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
