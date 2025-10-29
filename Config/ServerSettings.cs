using System.Text.Json.Serialization;

namespace CallAdminSystem.Configs;

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