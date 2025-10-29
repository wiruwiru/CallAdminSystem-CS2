using System.Text.Json.Serialization;

namespace CallAdminSystem.Configs;

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