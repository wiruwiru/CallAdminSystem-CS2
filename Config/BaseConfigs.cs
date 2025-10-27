using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CallAdminSystem.Configs;

public class BaseConfigs : BasePluginConfig
{
    [JsonPropertyName("ServerSettings")]
    public ServerSettings Server { get; set; } = new();

    [JsonPropertyName("DiscordSettings")]
    public DiscordSettings Discord { get; set; } = new();

    [JsonPropertyName("CommandSettings")]
    public CommandSettings Commands { get; set; } = new();

    [JsonPropertyName("PermissionSettings")]
    public PermissionSettings Permissions { get; set; } = new();
}

public class ServerSettings
{
    [JsonPropertyName("IPandPORT")]
    public string IPandPORT { get; set; } = "45.235.99.18:27025";

    [JsonPropertyName("GetIPandPORTautomatic")]
    public bool GetIPandPORTautomatic { get; set; } = true;

    [JsonPropertyName("UseHostname")]
    public bool UseHostname { get; set; } = true;

    [JsonPropertyName("CustomDomain")]
    public string CustomDomain { get; set; } = "https://crisisgamer.com/connect";

    [JsonPropertyName("MinimumPlayers")]
    public int MinimumPlayers { get; set; } = 2;
}

public class DiscordSettings
{
    [JsonPropertyName("WebhookUrl")]
    public string WebhookUrl { get; set; } = "";

    [JsonPropertyName("MentionRoleID")]
    public string MentionRoleID { get; set; } = "";

    [JsonPropertyName("ReportEmbedColor")]
    public string ReportEmbedColor { get; set; } = "#eb4034";

    [JsonPropertyName("ClaimEmbedColor")]
    public string ClaimEmbedColor { get; set; } = "#100c85";
}

public class CommandSettings
{
    [JsonPropertyName("ReportCommands")]
    public List<string> ReportCommands { get; set; } = new List<string> { "css_call", "css_report" };

    [JsonPropertyName("ClaimCommands")]
    public List<string> ClaimCommands { get; set; } = new List<string> { "css_claim" };

    [JsonPropertyName("CommandCooldownSeconds")]
    public int CommandCooldownSeconds { get; set; } = 120;
}

public class PermissionSettings
{
    [JsonPropertyName("ClaimCommandFlag")]
    public string ClaimCommandFlag { get; set; } = "@css/generic";
}