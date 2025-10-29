namespace CallAdminSystem.Models;

public class ReportRecord
{
    public string Uuid { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public string VictimName { get; set; } = string.Empty;
    public string VictimSteamId { get; set; } = string.Empty;
    public string ReportedName { get; set; } = string.Empty;
    public string ReportedSteamId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}