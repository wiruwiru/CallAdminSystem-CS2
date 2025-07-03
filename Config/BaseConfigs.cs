using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CallAdminSystem.Configs;

public class BaseConfigs : BasePluginConfig
{
    [JsonPropertyName("WebhookUrl")]
    public string WebhookUrl { get; set; } = "";

    [JsonPropertyName("IPandPORT")]
    public string IPandPORT { get; set; } = "45.235.99.18:27025";

    [JsonPropertyName("GetIPandPORTautomatic")]
    public bool GetIPandPORTautomatic { get; set; } = true;

    [JsonPropertyName("UseHostname")]
    public bool UseHostname { get; set; } = true;

    [JsonPropertyName("CustomDomain")]
    public string CustomDomain { get; set; } = "https://crisisgamer.com/connect";

    [JsonPropertyName("MentionRoleID")]
    public string MentionRoleID { get; set; } = "";

    [JsonPropertyName("ReportCommand")]
    public List<string> ReportCommands { get; set; } = new List<string> { "css_call", "css_report" };

    [JsonPropertyName("ClaimCommand")]
    public string ClaimCommand { get; set; } = "css_claim";

    [JsonPropertyName("ClaimCommandFlag")]
    public string ClaimCommandFlag { get; set; } = "@css/generic";

    [JsonPropertyName("CommandCooldownSeconds")]
    public int CommandCooldownSeconds { get; set; } = 120;

    [JsonPropertyName("MinimumPlayers")]
    public int MinimumPlayers { get; set; } = 2;

    [JsonPropertyName("ReportEmbedColor")]
    public string ReportEmbedColor { get; set; } = "#eb4034";

    [JsonPropertyName("ClaimEmbedColor")]
    public string ClaimEmbedColor { get; set; } = "#100c85";


}