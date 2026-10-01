using System.Data.Common;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace golenWeb.Data
{
    public class DbInitializer
    {
        private readonly DatabaseConnectionFactory _factory;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(DatabaseConnectionFactory factory, ILogger<DbInitializer> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var isSqlite = _factory.ProviderType == DbProviderType.Sqlite;
            var dbName = _factory.GetDatabaseName();

            _logger.LogInformation("Initializing Golden Success College database '{dbName}' using provider '{provider}'...", dbName, _factory.ProviderType);

            try
            {
                if (!isSqlite)
                {
                    // MySQL: Ensure database exists
                    var createDbSql = $"CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;";
                    await using (var serverConn = _factory.CreateServerConnection())
                    {
                        await serverConn.OpenAsync();
                        await using var cmd = serverConn.CreateCommand();
                        cmd.CommandText = createDbSql;
                        await cmd.ExecuteNonQueryAsync();
                        _logger.LogInformation("Database '{dbName}' created or verified.", dbName);
                    }
                }

                await using var conn = _factory.CreateConnection();
                await conn.OpenAsync();

                // 0. Remove legacy Products table if present & migrate column names
                await DropProductsTableIfExistAsync(conn);
                await RenameLegacyIdColumnsIfExistAsync(conn, isSqlite);

                // 1. Create Users Table
                string createUsersTableSql = isSqlite
                    ? @"
CREATE TABLE IF NOT EXISTS Users (
  UserId INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT NOT NULL UNIQUE,
  Email TEXT,
  PasswordHash TEXT NOT NULL,
  Role TEXT NOT NULL DEFAULT 'User',
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
"
                    : @"
CREATE TABLE IF NOT EXISTS `Users` (
  `UserId` INT NOT NULL AUTO_INCREMENT,
  `Username` VARCHAR(100) NOT NULL,
  `Email` VARCHAR(200) NULL,
  `PasswordHash` VARCHAR(512) NOT NULL,
  `Role` VARCHAR(20) NOT NULL DEFAULT 'User',
  `CreatedAt` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `UX_Users_Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
                await ExecuteSqlAsync(conn, createUsersTableSql);
                _logger.LogInformation("Users table verified.");

                // Safe migration: Add Role column if it doesn't exist yet
                await AddRoleColumnIfMissingAsync(conn, isSqlite);

                // 2. Create Events Table (with Foreign Key relationship to Users)
                string createEventsTableSql = isSqlite
                    ? @"
CREATE TABLE IF NOT EXISTS Events (
  EventId INTEGER PRIMARY KEY AUTOINCREMENT,
  Title TEXT NOT NULL,
  Description TEXT,
  EventDate TEXT NOT NULL,
  StartTime TEXT,
  EndTime TEXT,
  Location TEXT,
  Organizer TEXT,
  Category TEXT,
  IsFeatured INTEGER DEFAULT 1,
  ImageData BLOB,
  ImageContentType TEXT,
  ImageHash TEXT,
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  CreatedByUserId INTEGER,
  FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId) ON DELETE SET NULL
);
"
                    : @"
CREATE TABLE IF NOT EXISTS `Events` (
  `EventId` INT NOT NULL AUTO_INCREMENT,
  `Title` VARCHAR(200) NOT NULL,
  `Description` TEXT NULL,
  `EventDate` VARCHAR(50) NOT NULL,
  `StartTime` VARCHAR(50) NULL,
  `EndTime` VARCHAR(50) NULL,
  `Location` VARCHAR(200) NULL,
  `Organizer` VARCHAR(100) NULL,
  `Category` VARCHAR(100) NULL,
  `IsFeatured` TINYINT(1) NOT NULL DEFAULT 1,
  `ImageData` LONGBLOB NULL,
  `ImageContentType` VARCHAR(100) NULL,
  `ImageHash` VARCHAR(64) NULL,
  `CreatedAt` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedByUserId` INT NULL,
  PRIMARY KEY (`EventId`),
  KEY `IX_Events_CreatedByUserId` (`CreatedByUserId`),
  CONSTRAINT `FK_Events_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
                await ExecuteSqlAsync(conn, createEventsTableSql);
                _logger.LogInformation("Events table verified with foreign key relationship to Users.");

                // 3. Create Bulletins Table (with Foreign Key relationship to Users)
                string createBulletinsTableSql = isSqlite
                    ? @"
CREATE TABLE IF NOT EXISTS Bulletins (
  BulletinId INTEGER PRIMARY KEY AUTOINCREMENT,
  Title TEXT NOT NULL,
  Content TEXT NOT NULL,
  Category TEXT,
  Priority TEXT,
  PublishDate TEXT,
  Author TEXT,
  ImageData BLOB,
  ImageContentType TEXT,
  ImageHash TEXT,
  CreatedByUserId INTEGER,
  FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId) ON DELETE SET NULL
);
"
                    : @"
CREATE TABLE IF NOT EXISTS `Bulletins` (
  `BulletinId` INT NOT NULL AUTO_INCREMENT,
  `Title` VARCHAR(200) NOT NULL,
  `Content` TEXT NOT NULL,
  `Category` VARCHAR(100) NULL,
  `Priority` VARCHAR(50) NULL,
  `PublishDate` VARCHAR(50) NULL,
  `Author` VARCHAR(100) NULL,
  `ImageData` LONGBLOB NULL,
  `ImageContentType` VARCHAR(100) NULL,
  `ImageHash` VARCHAR(64) NULL,
  `CreatedByUserId` INT NULL,
  PRIMARY KEY (`BulletinId`),
  KEY `IX_Bulletins_CreatedByUserId` (`CreatedByUserId`),
  CONSTRAINT `FK_Bulletins_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
                await ExecuteSqlAsync(conn, createBulletinsTableSql);
                _logger.LogInformation("Bulletins table verified with foreign key relationship to Users.");

                // Migration: Ensure image & relationship columns exist in Events & Bulletins tables
                await AddImageColumnsIfMissingAsync(conn, isSqlite);
                await AddCreatedByUserIdColumnIfMissingAsync(conn, isSqlite);

                // 4. Create SiteSettings Table
                string createSiteSettingsTableSql = isSqlite
                    ? @"
CREATE TABLE IF NOT EXISTS SiteSettings (
  SiteSettingId INTEGER PRIMARY KEY AUTOINCREMENT,
  Key TEXT NOT NULL UNIQUE,
  Value TEXT NOT NULL DEFAULT ''
);
"
                    : @"
CREATE TABLE IF NOT EXISTS `SiteSettings` (
  `SiteSettingId` INT NOT NULL AUTO_INCREMENT,
  `Key` VARCHAR(100) NOT NULL,
  `Value` TEXT NULL,
  PRIMARY KEY (`SiteSettingId`),
  UNIQUE KEY `UX_SiteSettings_Key` (`Key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
                await ExecuteSqlAsync(conn, createSiteSettingsTableSql);
                _logger.LogInformation("SiteSettings table verified.");

                // Seed initial data if empty
                await SeedUsersAsync(conn);
                await SeedEventsAsync(conn);
                await SeedBulletinsAsync(conn);
                await SeedSiteSettingsAsync(conn);

                _logger.LogInformation("Golden Success College Database initialization completed!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization error: {message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Safely migrates existing database tables with generic 'Id' columns to specific primary key column names (UserId, EventId, BulletinId, SiteSettingId).
        /// </summary>
        private async Task RenameLegacyIdColumnsIfExistAsync(DbConnection conn, bool isSqlite)
        {
            var tablesToRename = new (string TableName, string NewColName)[]
            {
                ("Users", "UserId"),
                ("Events", "EventId"),
                ("Bulletins", "BulletinId"),
                ("SiteSettings", "SiteSettingId")
            };

            foreach (var (tableName, newColName) in tablesToRename)
            {
                try
                {
                    if (isSqlite)
                    {
                        await using var checkCmd = conn.CreateCommand();
                        checkCmd.CommandText = $"PRAGMA table_info({tableName})";
                        bool hasOldId = false;
                        bool hasNewId = false;
                        await using (var reader = await checkCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var colName = reader.GetString(1);
                                if (colName.Equals("Id", StringComparison.OrdinalIgnoreCase)) hasOldId = true;
                                if (colName.Equals(newColName, StringComparison.OrdinalIgnoreCase)) hasNewId = true;
                            }
                        }

                        if (hasOldId && !hasNewId)
                        {
                            await ExecuteSqlAsync(conn, $"ALTER TABLE {tableName} RENAME COLUMN Id TO {newColName};");
                            _logger.LogInformation("Migrated SQLite table '{tableName}': renamed Id column to {newColName}.", tableName, newColName);
                        }
                    }
                    else
                    {
                        // MySQL migration
                        await using var checkOldCmd = conn.CreateCommand();
                        checkOldCmd.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = 'Id'";
                        var oldColCount = Convert.ToInt32(await checkOldCmd.ExecuteScalarAsync());

                        await using var checkNewCmd = conn.CreateCommand();
                        checkNewCmd.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{newColName}'";
                        var newColCount = Convert.ToInt32(await checkNewCmd.ExecuteScalarAsync());

                        if (oldColCount > 0 && newColCount == 0)
                        {
                            await ExecuteSqlAsync(conn, $"ALTER TABLE `{tableName}` CHANGE `Id` `{newColName}` INT NOT NULL AUTO_INCREMENT;");
                            _logger.LogInformation("Migrated MySQL table '{tableName}': renamed Id column to {newColName}.", tableName, newColName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not check or rename legacy Id column in table {tableName}.", tableName);
                }
            }
        }

        /// <summary>
        /// Drops legacy Products / products table if present in the database.
        /// </summary>
        private async Task DropProductsTableIfExistAsync(DbConnection conn)
        {
            try
            {
                await ExecuteSqlAsync(conn, "DROP TABLE IF EXISTS Products;");
                await ExecuteSqlAsync(conn, "DROP TABLE IF EXISTS products;");
                _logger.LogInformation("Cleaned legacy Products table from database.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not drop legacy Products table.");
            }
        }

        /// <summary>
        /// Safe migration: adds Role column to existing Users table if it doesn't exist.
        /// </summary>
        private async Task AddRoleColumnIfMissingAsync(DbConnection conn, bool isSqlite)
        {
            try
            {
                if (isSqlite)
                {
                    await using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "PRAGMA table_info(Users)";
                    bool hasRole = false;
                    await using var reader = await checkCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (reader.GetString(1).Equals("Role", StringComparison.OrdinalIgnoreCase))
                        {
                            hasRole = true;
                            break;
                        }
                    }
                    if (!hasRole)
                    {
                        await ExecuteSqlAsync(conn, "ALTER TABLE Users ADD COLUMN Role TEXT NOT NULL DEFAULT 'User';");
                        _logger.LogInformation("Added Role column to Users table (SQLite migration).");
                    }
                }
                else
                {
                    await using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = @"
SELECT COUNT(*) FROM information_schema.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'Role'";
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count == 0)
                    {
                        await ExecuteSqlAsync(conn, "ALTER TABLE `Users` ADD COLUMN `Role` VARCHAR(20) NOT NULL DEFAULT 'User';");
                        _logger.LogInformation("Added Role column to Users table (MySQL migration).");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check/add Role column — it may already exist.");
            }
        }

        private async Task AddCreatedByUserIdColumnIfMissingAsync(DbConnection conn, bool isSqlite)
        {
            try
            {
                if (isSqlite)
                {
                    // Events
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA table_info(Events)";
                        bool hasCol = false;
                        await using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetString(1).Equals("CreatedByUserId", StringComparison.OrdinalIgnoreCase))
                            {
                                hasCol = true;
                                break;
                            }
                        }
                        if (!hasCol)
                        {
                            await ExecuteSqlAsync(conn, "ALTER TABLE Events ADD COLUMN CreatedByUserId INTEGER REFERENCES Users(UserId);");
                            _logger.LogInformation("Added CreatedByUserId foreign key column to Events table (SQLite).");
                        }
                    }

                    // Bulletins
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA table_info(Bulletins)";
                        bool hasCol = false;
                        await using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetString(1).Equals("CreatedByUserId", StringComparison.OrdinalIgnoreCase))
                            {
                                hasCol = true;
                                break;
                            }
                        }
                        if (!hasCol)
                        {
                            await ExecuteSqlAsync(conn, "ALTER TABLE Bulletins ADD COLUMN CreatedByUserId INTEGER REFERENCES Users(UserId);");
                            _logger.LogInformation("Added CreatedByUserId foreign key column to Bulletins table (SQLite).");
                        }
                    }
                }
                else
                {
                    // MySQL Events
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Events' AND COLUMN_NAME = 'CreatedByUserId'";
                        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            await ExecuteSqlAsync(conn, "ALTER TABLE `Events` ADD COLUMN `CreatedByUserId` INT NULL, ADD CONSTRAINT `FK_Events_Users` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users`(`UserId`) ON DELETE SET NULL;");
                            _logger.LogInformation("Added CreatedByUserId foreign key to Events table (MySQL).");
                        }
                    }

                    // MySQL Bulletins
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Bulletins' AND COLUMN_NAME = 'CreatedByUserId'";
                        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            await ExecuteSqlAsync(conn, "ALTER TABLE `Bulletins` ADD COLUMN `CreatedByUserId` INT NULL, ADD CONSTRAINT `FK_Bulletins_Users` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users`(`UserId`) ON DELETE SET NULL;");
                            _logger.LogInformation("Added CreatedByUserId foreign key to Bulletins table (MySQL).");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check/add CreatedByUserId relationship columns.");
            }
        }

        private async Task AddImageColumnsIfMissingAsync(DbConnection conn, bool isSqlite)
        {
            try
            {
                if (isSqlite)
                {
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA table_info(Events)";
                        bool hasImage = false;
                        await using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetString(1).Equals("ImageData", StringComparison.OrdinalIgnoreCase))
                            {
                                hasImage = true;
                                break;
                            }
                        }
                        if (!hasImage)
                        {
                            await ExecuteSqlAsync(conn, "ALTER TABLE Events ADD COLUMN ImageData BLOB;");
                            await ExecuteSqlAsync(conn, "ALTER TABLE Events ADD COLUMN ImageContentType TEXT;");
                            await ExecuteSqlAsync(conn, "ALTER TABLE Events ADD COLUMN ImageHash TEXT;");
                            _logger.LogInformation("Added image columns to Events table (SQLite).");
                        }
                    }

                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA table_info(Bulletins)";
                        bool hasImage = false;
                        await using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetString(1).Equals("ImageData", StringComparison.OrdinalIgnoreCase))
                            {
                                hasImage = true;
                                break;
                            }
                        }
                        if (!hasImage)
                        {
                            await ExecuteSqlAsync(conn, "ALTER TABLE Bulletins ADD COLUMN ImageData BLOB;");
                            await ExecuteSqlAsync(conn, "ALTER TABLE Bulletins ADD COLUMN ImageContentType TEXT;");
                            await ExecuteSqlAsync(conn, "ALTER TABLE Bulletins ADD COLUMN ImageHash TEXT;");
                            _logger.LogInformation("Added image columns to Bulletins table (SQLite).");
                        }
                    }
                }
                else
                {
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Events' AND COLUMN_NAME = 'ImageData'";
                        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            await ExecuteSqlAsync(conn, "ALTER TABLE `Events` ADD COLUMN `ImageData` LONGBLOB NULL, ADD COLUMN `ImageContentType` VARCHAR(100) NULL, ADD COLUMN `ImageHash` VARCHAR(64) NULL;");
                            _logger.LogInformation("Added image columns to Events table (MySQL).");
                        }
                    }

                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Bulletins' AND COLUMN_NAME = 'ImageData'";
                        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            await ExecuteSqlAsync(conn, "ALTER TABLE `Bulletins` ADD COLUMN `ImageData` LONGBLOB NULL, ADD COLUMN `ImageContentType` VARCHAR(100) NULL, ADD COLUMN `ImageHash` VARCHAR(64) NULL;");
                            _logger.LogInformation("Added image columns to Bulletins table (MySQL).");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check/add image columns.");
            }
        }

        private static async Task ExecuteSqlAsync(DbConnection conn, string sql)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task SeedUsersAsync(DbConnection conn)
        {
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users";
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                _logger.LogInformation("Seeding default admin user...");
                var hash = HashPassword("Admin@123");
                await using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@u, @e, @p, @r);";
                AddParam(insertCmd, "@u", "admin");
                AddParam(insertCmd, "@e", "admin@goldensuccess.edu");
                AddParam(insertCmd, "@p", hash);
                AddParam(insertCmd, "@r", "Admin");
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task SeedEventsAsync(DbConnection conn)
        {
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Events";
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                _logger.LogInformation("Seeding Golden Success College campus events...");
                var todayStr = DateTime.Today.ToString("yyyy-MM-dd");
                var tomorrowStr = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                var in3DaysStr = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");

                var events = new[]
                {
                    ("Golden Success College Campus Convocation & Student Orientation", "Official general assembly for all new and continuing students. Program starts with college hymn.", todayStr, "08:30 AM", "12:00 PM", "College Main Auditorium", "Office of Student Affairs", "Ceremony", 1),
                    ("Departmental Technology & Innovation Forum", "Interactive keynote presentations from industry experts on Modern Web Architecture and Software Engineering.", todayStr, "01:30 PM", "05:00 PM", "Engineering Audio-Visual Room", "Department of Computer Studies", "Seminar", 1),
                    ("Annual Golden Eagles Intramural Sports Festival", "Opening ceremony and varsity team tryouts for basketball, volleyball, and badminton tournaments.", tomorrowStr, "08:00 AM", "05:00 PM", "GSC Athletic Gymnasium", "Sports Development Committee", "Sports", 1),
                    ("Career & IT Innovations Expo 2026", "Meet hiring partners, submit resumes, and attend mock interview workshops hosted by partner companies.", in3DaysStr, "09:00 AM", "04:00 PM", "College Convention Hall", "Guidance & Placement Office", "Academic", 1)
                };

                foreach (var e in events)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO Events (Title, Description, EventDate, StartTime, EndTime, Location, Organizer, Category, IsFeatured, CreatedByUserId) VALUES (@t,@d,@ed,@st,@et,@l,@o,@c,@f, 1);";
                    AddParam(cmd, "@t", e.Item1);
                    AddParam(cmd, "@d", e.Item2);
                    AddParam(cmd, "@ed", e.Item3);
                    AddParam(cmd, "@st", e.Item4);
                    AddParam(cmd, "@et", e.Item5);
                    AddParam(cmd, "@l", e.Item6);
                    AddParam(cmd, "@o", e.Item7);
                    AddParam(cmd, "@c", e.Item8);
                    AddParam(cmd, "@f", e.Item9);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task SeedBulletinsAsync(DbConnection conn)
        {
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Bulletins";
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                _logger.LogInformation("Seeding Golden Success College campus bulletins...");
                var todayStr = DateTime.Today.ToString("yyyy-MM-dd");

                var notices = new[]
                {
                    ("Official Notice: Midterm Examination Schedule Released", "The official midterm examination schedule for all college departments is now posted. Please check your student portal or visit the Registrar's Office for room assignments.", "Academic Notice", "High", todayStr, "Office of the Registrar"),
                    ("Library Extended Operating Hours During Finals Week", "To support student research and study groups, the Main Campus Library will remain open until 10:00 PM starting this Monday.", "General Notice", "Normal", todayStr, "GSC Library Services"),
                    ("Call for Student Council Officers Candidates", "Filing of certificates of candidacy for the Supreme Student Council is now officially open. Submit requirements at the Student Affairs office.", "Student Activity", "Normal", todayStr, "Commission on Student Elections")
                };

                foreach (var n in notices)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO Bulletins (Title, Content, Category, Priority, PublishDate, Author, CreatedByUserId) VALUES (@t,@c,@cat,@pr,@pd,@a, 1);";
                    AddParam(cmd, "@t", n.Item1);
                    AddParam(cmd, "@c", n.Item2);
                    AddParam(cmd, "@cat", n.Item3);
                    AddParam(cmd, "@pr", n.Item4);
                    AddParam(cmd, "@pd", n.Item5);
                    AddParam(cmd, "@a", n.Item6);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task SeedSiteSettingsAsync(DbConnection conn)
        {
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM SiteSettings";
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                _logger.LogInformation("Seeding default site settings...");
                var defaults = new Dictionary<string, string>
                {
                    ["SiteTitle"] = "Golden Success College",
                    ["HeroTitle"] = "Welcome to Golden Success College",
                    ["HeroSubtitle"] = "Your path to excellence starts here",
                    ["Tagline"] = "Determination • Courage • Hardwork • Isa. 33:6",
                    ["AboutText"] = "Golden Success College is committed to providing quality education and fostering academic excellence in a nurturing Christian environment.",
                    ["FooterText"] = "Golden Success College • All Rights Reserved",
                    ["PrimaryColor"] = "#0b5e28",
                    ["AccentColor"] = "#e6b800",
                    ["NavBackground"] = "#073d1a",
                    ["ContactEmail"] = "info@goldensuccess.edu",
                    ["ContactPhone"] = "",
                    ["Address"] = "",
                    ["AnnouncementBanner"] = "",
                    ["ShowAnnouncementBanner"] = "false"
                };

                foreach (var kv in defaults)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO `SiteSettings` (`Key`, `Value`) VALUES (@k, @v);";
                    AddParam(cmd, "@k", kv.Key);
                    AddParam(cmd, "@v", kv.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
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
    }
}
