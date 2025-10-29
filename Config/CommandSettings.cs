using System.Text.Json.Serialization;

namespace CallAdminSystem.Configs;

public class CommandSettings
{
    [JsonPropertyName("ReportCommands")]
    public List<string> ReportCommands { get; set; } = new List<string> { "css_call", "css_report" };

    [JsonPropertyName("ClaimCommands")]
    public List<string> ClaimCommands { get; set; } = new List<string> { "css_claim" };

    [JsonPropertyName("CommandCooldownSeconds")]
    public int CommandCooldownSeconds { get; set; } = 120;
}