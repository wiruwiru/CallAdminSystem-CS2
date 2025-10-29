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

    [JsonPropertyName("Database")]
    public DatabaseConfig Database { get; set; } = new();
}