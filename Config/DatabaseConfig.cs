using System.Text.Json.Serialization;

namespace CallAdminSystem.Configs;

public class DatabaseConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("Host")]
    public string Host { get; set; } = "localhost";

    [JsonPropertyName("Port")]
    public uint Port { get; set; } = 3306;

    [JsonPropertyName("User")]
    public string User { get; set; } = "root";

    [JsonPropertyName("Password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("DatabaseName")]
    public string DatabaseName { get; set; } = "calladminsystem";
}