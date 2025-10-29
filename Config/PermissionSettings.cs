using System.Text.Json.Serialization;

namespace CallAdminSystem.Configs;

public class PermissionSettings
{
    [JsonPropertyName("ClaimCommandFlag")]
    public string ClaimCommandFlag { get; set; } = "@css/generic";
}