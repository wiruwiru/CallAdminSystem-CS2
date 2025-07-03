using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using CallAdminSystem.Configs;

namespace CallAdminSystem.Services;

public class DiscordService : IDisposable
{
    private BaseConfigs _config;
    private readonly HttpClient _httpClient;
    private readonly IStringLocalizer _localizer;

    public DiscordService(BaseConfigs config, IStringLocalizer localizer)
    {
        _config = config;
        _localizer = localizer;
        _httpClient = new HttpClient();
    }

    public void UpdateConfig(BaseConfigs config)
    {
        _config = config;
    }

    public async Task SendReportToDiscord(string reporterName, string reporterSteamId, 
        string targetName, string targetSteamId, string reason, string serverInfo, string hostname)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.WebhookUrl)) return;

            var reporterNameClean = CleanPlayerName(reporterName);
            var targetNameClean = CleanPlayerName(targetName);
            var reasonClean = reason.Trim('"');

            string mentionMessage = "";
            if (!string.IsNullOrEmpty(_config.MentionRoleID))
            {
                mentionMessage = $"<@&{_config.MentionRoleID}> has been called to the server!";
            }

            var payload = new
            {
                content = mentionMessage,
                embeds = new[]
                {
                    new
                    {
                        title = hostname,
                        description = "There is a new report on the server",
                        color = ConvertHexToColor(_config.ReportEmbedColor),
                        fields = new[]
                        {
                            new
                            {
                                name = "🎯 Victim",
                                value = $"**Name:** {reporterNameClean}\n**SteamID:** {reporterSteamId}\n**Steam:** [Link to profile](https://steamcommunity.com/profiles/{reporterSteamId}/)",
                                inline = false
                            },
                            new
                            {
                                name = "⚠️ Reported",
                                value = $"**Name:** {targetNameClean}\n**SteamID:** {targetSteamId}\n**Steam:** [Link to profile](https://steamcommunity.com/profiles/{targetSteamId}/)",
                                inline = false
                            },
                            new
                            {
                                name = "📝 Reason",
                                value = reasonClean,
                                inline = false
                            },
                            new
                            {
                                name = "🔗 Direct Connect",
                                value = $"[**`connect {serverInfo}`**]({_config.CustomDomain}?ip={serverInfo}) [Click to join]",
                                inline = false
                            }
                        }
                    }
                }
            };

            await SendToDiscord(payload);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error sending report to Discord: {ex.Message}");
        }
    }

    public async Task SendClaimToDiscord(string adminName, string serverInfo, string hostname)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.WebhookUrl)) return;

            var adminNameClean = CleanPlayerName(adminName);

            var embed = new
            {
                title = hostname,
                description = "An administrator is handling reports on this server.",
                color = ConvertHexToColor(_config.ClaimEmbedColor),
                fields = new[]
                {
                    new
                    {
                        name = "👮 Administrator",
                        value = adminNameClean,
                        inline = false
                    },
                    new
                    {
                        name = "🔗 Direct Connect",
                        value = $"[**`connect {serverInfo}`**]({_config.CustomDomain}?ip={serverInfo}) [Click to join]",
                        inline = false
                    }
                }
            };

            var payload = new
            {
                embeds = new[] { embed }
            };

            await SendToDiscord(payload);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error sending claim to Discord: {ex.Message}");
        }
    }

    private async Task SendToDiscord(object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_config.WebhookUrl, content);

            Console.ForegroundColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"[CallAdminSystem] Discord webhook: {(response.IsSuccessStatusCode ? "Success" : $"Error: {response.StatusCode}")}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error sending to Discord: {ex.Message}");
        }
    }

    private static int ConvertHexToColor(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex[1..];
        }
        return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
    }

    private static string CleanPlayerName(string playerName)
    {
        return playerName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}