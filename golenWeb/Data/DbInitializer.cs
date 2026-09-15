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

                // 1. Create Users Table
                string createUsersTableSql = isSqlite
                    ? @"
CREATE TABLE IF NOT EXISTS Users (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT NOT NULL UNIQUE,
  Email TEXT,
  PasswordHash TEXT NOT NULL,
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
"
                    : @"
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
                await ExecuteSqlAsync(conn, createUsersTableSql);
                _logger.LogInformation("Users table verified.");

                // 2. Create Events Table
                string createEventsTableSql = isSqlite
                    ? @"
CREATE TABLE IF NOT EXISTS Events (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Title TEXT NOT NULL,
  Description TEXT,
  EventDate TEXT NOT NULL,
  StartTime TEXT,
  EndTime TEXT,
  Location TEXT,
  Organizer TEXT,
  Category TEXT,
  IsFeatured INTEGER DEFAULT 1,
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
"
                    : @"
CREATE TABLE IF NOT EXISTS `Events` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Title` VARCHAR(200) NOT NULL,
  `Description` TEXT NULL,
  `EventDate` VARCHAR(50) NOT NULL,
  `StartTime` VARCHAR(50) NULL,
  `EndTime` VARCHAR(50) NULL,
  `Location` VARCHAR(200) NULL,
  `Organizer` VARCHAR(100) NULL,
  `Category` VARCHAR(100) NULL,
  `IsFeatured` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
                await ExecuteSqlAsync(conn, createEventsTableSql);
                _logger.LogInformation("Events table verified.");

                // 3. Create Bulletins Table
                string createBulletinsTableSql = isSqlite
                    ? @"
CREATE TABLE IF NOT EXISTS Bulletins (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Title TEXT NOT NULL,
  Content TEXT NOT NULL,
  Category TEXT,
  Priority TEXT,
  PublishDate TEXT,
  Author TEXT
);
"
                    : @"
CREATE TABLE IF NOT EXISTS `Bulletins` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Title` VARCHAR(200) NOT NULL,
  `Content` TEXT NOT NULL,
  `Category` VARCHAR(100) NULL,
  `Priority` VARCHAR(50) NULL,
  `PublishDate` VARCHAR(50) NULL,
  `Author` VARCHAR(100) NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
                await ExecuteSqlAsync(conn, createBulletinsTableSql);
                _logger.LogInformation("Bulletins table verified.");

                // Seed initial data if empty
                await SeedUsersAsync(conn);
                await SeedEventsAsync(conn);
                await SeedBulletinsAsync(conn);

                _logger.LogInformation("Golden Success College Database initialization completed!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization error: {message}", ex.Message);
                throw;
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
                insertCmd.CommandText = "INSERT INTO Users (Username, Email, PasswordHash) VALUES (@u, @e, @p);";
                AddParam(insertCmd, "@u", "admin");
                AddParam(insertCmd, "@e", "admin@goldensuccess.edu");
                AddParam(insertCmd, "@p", hash);
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
                    cmd.CommandText = "INSERT INTO Events (Title, Description, EventDate, StartTime, EndTime, Location, Organizer, Category, IsFeatured) VALUES (@t,@d,@ed,@st,@et,@l,@o,@c,@f);";
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
                    cmd.CommandText = "INSERT INTO Bulletins (Title, Content, Category, Priority, PublishDate, Author) VALUES (@t,@c,@cat,@pr,@pd,@a);";
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

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            const int iterations = 100_000;
            using var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = derive.GetBytes(32);
            return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }
}
