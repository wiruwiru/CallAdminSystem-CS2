using MySqlConnector;

using CallAdminSystem.Configs;
using CallAdminSystem.Models;

namespace CallAdminSystem.Services;

public class DatabaseService
{
    private readonly DatabaseConfig _config;
    private readonly bool _enabled;

    public DatabaseService(DatabaseConfig config)
    {
        _config = config;
        _enabled = config.Enabled;

        if (_enabled)
        {
            LogInfo("DatabaseService initialized and enabled");
        }
        else
        {
            LogInfo("DatabaseService initialized but disabled in configuration");
        }
    }

    public async Task InitializeDatabase()
    {
        if (!_enabled)
        {
            LogInfo("Database is disabled, skipping initialization");
            return;
        }

        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            await CreateTable(connection);
            LogInfo("Database connection established and table created");
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize database: {ex.Message}");
            throw;
        }
    }

    private async Task CreateTable(MySqlConnection connection)
    {
        var createTableQuery = @"
            CREATE TABLE IF NOT EXISTS calladmin_reports (
                uuid VARCHAR(36) PRIMARY KEY UNIQUE NOT NULL,
                server_address VARCHAR(64) NOT NULL,
                victim_name VARCHAR(128) NOT NULL,
                victim_steamid VARCHAR(20) NOT NULL,
                reported_name VARCHAR(128) NOT NULL,
                reported_steamid VARCHAR(20) NOT NULL,
                reason TEXT NOT NULL,
                timestamp DATETIME NOT NULL,
                INDEX idx_timestamp (timestamp),
                INDEX idx_server (server_address),
                INDEX idx_victim_steamid (victim_steamid),
                INDEX idx_reported_steamid (reported_steamid)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

        using var cmd = new MySqlCommand(createTableQuery, connection);
        await cmd.ExecuteNonQueryAsync();
        LogInfo("Admin reports table created/verified");
    }

    public async Task<bool> SaveReport(ReportRecord record)
    {
        if (!_enabled)
        {
            return false;
        }

        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var insertQuery = @"
                INSERT INTO calladmin_reports 
                (uuid, server_address, victim_name, victim_steamid, reported_name, reported_steamid, reason, timestamp)
                VALUES 
                (@uuid, @server_address, @victim_name, @victim_steamid, @reported_name, @reported_steamid, @reason, @timestamp)";

            using var cmd = new MySqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("@uuid", record.Uuid);
            cmd.Parameters.AddWithValue("@server_address", record.ServerAddress);
            cmd.Parameters.AddWithValue("@victim_name", record.VictimName);
            cmd.Parameters.AddWithValue("@victim_steamid", record.VictimSteamId);
            cmd.Parameters.AddWithValue("@reported_name", record.ReportedName);
            cmd.Parameters.AddWithValue("@reported_steamid", record.ReportedSteamId);
            cmd.Parameters.AddWithValue("@reason", record.Reason);
            cmd.Parameters.AddWithValue("@timestamp", record.Timestamp);

            await cmd.ExecuteNonQueryAsync();
            LogInfo($"Report saved to database - UUID: {record.Uuid}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error saving report to database: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ReportRecord>> GetRecentReports(int limit = 10)
    {
        if (!_enabled)
        {
            return new List<ReportRecord>();
        }

        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var selectQuery = @"
                SELECT uuid, server_address, victim_name, victim_steamid, reported_name, reported_steamid, reason, timestamp
                FROM calladmin_reports
                ORDER BY timestamp DESC
                LIMIT @limit";

            using var cmd = new MySqlCommand(selectQuery, connection);
            cmd.Parameters.AddWithValue("@limit", limit);

            var records = new List<ReportRecord>();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new ReportRecord
                {
                    Uuid = reader.GetString("uuid"),
                    ServerAddress = reader.GetString("server_address"),
                    VictimName = reader.GetString("victim_name"),
                    VictimSteamId = reader.GetString("victim_steamid"),
                    ReportedName = reader.GetString("reported_name"),
                    ReportedSteamId = reader.GetString("reported_steamid"),
                    Reason = reader.GetString("reason"),
                    Timestamp = reader.GetDateTime("timestamp")
                });
            }

            return records;
        }
        catch (Exception ex)
        {
            LogError($"Error retrieving reports from database: {ex.Message}");
            return new List<ReportRecord>();
        }
    }

    public async Task<int> GetReportCount()
    {
        if (!_enabled)
        {
            return 0;
        }

        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var countQuery = "SELECT COUNT(*) FROM calladmin_reports";
            using var cmd = new MySqlCommand(countQuery, connection);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            LogError($"Error getting report count: {ex.Message}");
            return 0;
        }
    }

    public async Task<List<ReportRecord>> GetReportsByPlayer(string steamId, int limit = 10)
    {
        if (!_enabled)
        {
            return new List<ReportRecord>();
        }

        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var selectQuery = @"
                SELECT uuid, server_address, victim_name, victim_steamid, reported_name, reported_steamid, reason, timestamp
                FROM calladmin_reports
                WHERE victim_steamid = @steamid OR reported_steamid = @steamid
                ORDER BY timestamp DESC
                LIMIT @limit";

            using var cmd = new MySqlCommand(selectQuery, connection);
            cmd.Parameters.AddWithValue("@steamid", steamId);
            cmd.Parameters.AddWithValue("@limit", limit);

            var records = new List<ReportRecord>();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new ReportRecord
                {
                    Uuid = reader.GetString("uuid"),
                    ServerAddress = reader.GetString("server_address"),
                    VictimName = reader.GetString("victim_name"),
                    VictimSteamId = reader.GetString("victim_steamid"),
                    ReportedName = reader.GetString("reported_name"),
                    ReportedSteamId = reader.GetString("reported_steamid"),
                    Reason = reader.GetString("reason"),
                    Timestamp = reader.GetDateTime("timestamp")
                });
            }

            return records;
        }
        catch (Exception ex)
        {
            LogError($"Error retrieving player reports from database: {ex.Message}");
            return new List<ReportRecord>();
        }
    }

    public async Task<bool> TestConnection()
    {
        if (!_enabled)
        {
            LogInfo("Database is disabled, skipping connection test");
            return false;
        }

        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            LogInfo("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Database connection test failed: {ex.Message}");
            return false;
        }
    }

    private MySqlConnection GetConnection()
    {
        if (_config == null)
        {
            throw new InvalidOperationException("Database configuration is null");
        }

        var builder = new MySqlConnectionStringBuilder
        {
            Server = _config.Host,
            Port = _config.Port,
            UserID = _config.User,
            Database = _config.DatabaseName,
            Password = _config.Password,
            Pooling = true,
            SslMode = MySqlSslMode.Preferred
        };

        return new MySqlConnection(builder.ConnectionString);
    }

    private void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[CallAdminSystem Database] {message}");
        Console.ResetColor();
    }

    private void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[CallAdminSystem Database] {message}");
        Console.ResetColor();
    }

    public bool IsEnabled() => _enabled;
}