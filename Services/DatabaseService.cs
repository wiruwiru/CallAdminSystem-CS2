using AnyBaseLib;
using AnyBaseLib.Bases;

using CallAdminSystem.Configs;
using CallAdminSystem.Utils;
using CallAdminSystem.Models;

namespace CallAdminSystem.Services;

public class DatabaseService
{
    private readonly DatabaseConfig _config;
    private readonly bool _enabled;
    private IAnyBase? _db;
    private bool _isInitialized = false;

    public DatabaseService(DatabaseConfig config)
    {
        _config = config;
        _enabled = config.Enabled;

        if (_enabled)
        {
            try
            {
                _db = CAnyBase.Base("mysql");
                Logger.LogInfo("DatabaseService", "DatabaseService initialized and enabled");
            }
            catch (Exception ex)
            {
                Logger.LogError("DatabaseService", $"Failed to load AnyBaseLib: {ex.Message}");
                Logger.LogWarning("DatabaseService", "Database functionality will be disabled. Make sure AnyBaseLib.dll is present.");
                _enabled = false;
            }
        }
        else
        {
            Logger.LogInfo("DatabaseService", "DatabaseService initialized but disabled in configuration");
        }
    }

    public async Task InitializeDatabase()
    {
        if (!_enabled || _db == null)
        {
            Logger.LogInfo("DatabaseService", "Database is disabled, skipping initialization");
            return;
        }

        try
        {
            _db.Set(
                CommitMode.AutoCommit,
                _config.DatabaseName,
                $"{_config.Host}:{_config.Port}",
                _config.User,
                _config.Password
            );

            if (!_db.Init())
            {
                throw new Exception("Failed to initialize database connection");
            }

            await Task.Run(CreateTable);
            _isInitialized = true;
            Logger.LogInfo("DatabaseService", "Database connection established and table created");
        }
        catch (Exception ex)
        {
            Logger.LogError("DatabaseService", $"Failed to initialize database: {ex.Message}");
            throw;
        }
    }

    private void CreateTable()
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

        _db?.Query(createTableQuery, new List<string>(), true);
        Logger.LogInfo("DatabaseService", "Admin reports table created/verified");
    }

    public async Task<bool> SaveReport(ReportRecord record)
    {
        if (!_enabled || _db == null || !_isInitialized)
        {
            return false;
        }

        try
        {
            var insertQuery = @"
                INSERT INTO calladmin_reports 
                (uuid, server_address, victim_name, victim_steamid, reported_name, reported_steamid, reason, timestamp)
                VALUES 
                ('{ARG}', '{ARG}', '{ARG}', '{ARG}', '{ARG}', '{ARG}', '{ARG}', '{ARG}')";

            var args = new List<string>
            {
                record.Uuid,
                record.ServerAddress,
                record.VictimName,
                record.VictimSteamId,
                record.ReportedName,
                record.ReportedSteamId,
                record.Reason,
                record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            };

            await Task.Run(() => _db.Query(insertQuery, args, true));

            Logger.LogInfo("DatabaseService", $"Report saved to database - UUID: {record.Uuid}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("DatabaseService", $"Error saving report to database: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ReportRecord>> GetRecentReports(int limit = 10)
    {
        if (!_enabled || _db == null || !_isInitialized)
        {
            return new List<ReportRecord>();
        }

        try
        {
            var selectQuery = @"
                SELECT uuid, server_address, victim_name, victim_steamid, reported_name, reported_steamid, reason, timestamp
                FROM calladmin_reports
                ORDER BY timestamp DESC
                LIMIT {ARG}";

            var args = new List<string> { limit.ToString() };

            var records = new List<ReportRecord>();
            await Task.Run(() =>
            {
                var rows = _db.Query(selectQuery, args);
                if (rows != null && rows.Count > 0)
                {
                    foreach (var row in rows)
                    {
                        records.Add(new ReportRecord
                        {
                            Uuid = row[0],
                            ServerAddress = row[1],
                            VictimName = row[2],
                            VictimSteamId = row[3],
                            ReportedName = row[4],
                            ReportedSteamId = row[5],
                            Reason = row[6],
                            Timestamp = DateTime.Parse(row[7])
                        });
                    }
                }
            });

            return records;
        }
        catch (Exception ex)
        {
            Logger.LogError("DatabaseService", $"Error retrieving reports from database: {ex.Message}");
            return new List<ReportRecord>();
        }
    }

    public async Task<int> GetReportCount()
    {
        if (!_enabled || _db == null || !_isInitialized)
        {
            return 0;
        }

        try
        {
            var countQuery = "SELECT COUNT(*) FROM calladmin_reports";

            int count = 0;
            await Task.Run(() =>
            {
                var result = _db.Query(countQuery, new List<string>());
                if (result != null && result.Count > 0 && result[0].Count > 0)
                {
                    count = int.Parse(result[0][0]);
                }
            });

            return count;
        }
        catch (Exception ex)
        {
            Logger.LogError("DatabaseService", $"Error getting report count: {ex.Message}");
            return 0;
        }
    }

    public async Task<List<ReportRecord>> GetReportsByPlayer(string steamId, int limit = 10)
    {
        if (!_enabled || _db == null || !_isInitialized)
        {
            return new List<ReportRecord>();
        }

        try
        {
            var selectQuery = @"
                SELECT uuid, server_address, victim_name, victim_steamid, reported_name, reported_steamid, reason, timestamp
                FROM calladmin_reports
                WHERE victim_steamid = '{ARG}' OR reported_steamid = '{ARG}'
                ORDER BY timestamp DESC
                LIMIT {ARG}";

            var args = new List<string> { steamId, steamId, limit.ToString() };

            var records = new List<ReportRecord>();
            await Task.Run(() =>
            {
                var rows = _db.Query(selectQuery, args);
                if (rows != null && rows.Count > 0)
                {
                    foreach (var row in rows)
                    {
                        records.Add(new ReportRecord
                        {
                            Uuid = row[0],
                            ServerAddress = row[1],
                            VictimName = row[2],
                            VictimSteamId = row[3],
                            ReportedName = row[4],
                            ReportedSteamId = row[5],
                            Reason = row[6],
                            Timestamp = DateTime.Parse(row[7])
                        });
                    }
                }
            });

            return records;
        }
        catch (Exception ex)
        {
            Logger.LogError("DatabaseService", $"Error retrieving player reports from database: {ex.Message}");
            return new List<ReportRecord>();
        }
    }

    public async Task<bool> TestConnection()
    {
        if (!_enabled || _db == null)
        {
            Logger.LogInfo("DatabaseService", "Database is disabled, skipping connection test");
            return false;
        }

        try
        {
            await Task.Run(() => _db.Query("SELECT 1", new List<string>()));
            Logger.LogInfo("DatabaseService", "Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("DatabaseService", $"Database connection test failed: {ex.Message}");
            return false;
        }
    }

    public bool IsEnabled() => _enabled && _db != null && _isInitialized;

    public void Dispose()
    {
        try
        {
            _db?.Close();
            Logger.LogInfo("DatabaseService", "Database connection closed");
        }
        catch (Exception ex)
        {
            Logger.LogError("DatabaseService", $"Error closing database connection: {ex.Message}");
        }
    }
}