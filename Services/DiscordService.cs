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
    private const string CUSTOM_DOMAIN_DEFAULT = "https://crisisgamer.com/connect";

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
            if (string.IsNullOrEmpty(_config.Discord.WebhookUrl)) return;

            var reporterNameClean = CleanPlayerName(reporterName);
            var targetNameClean = CleanPlayerName(targetName);
            var reasonClean = reason.Trim('"');
            var customDomain = GetCustomDomain();

            string? mentionMessage = null;
            if (!string.IsNullOrEmpty(_config.Discord.MentionRoleID))
            {
                string mentionRoleIDMessage = $"<@&{_config.Discord.MentionRoleID}>";
                mentionMessage = _localizer["DiscordMention", mentionRoleIDMessage].Value;
            }

            var embed = new
            {
                title = hostname,
                description = _localizer["NewReportDescription"].Value,
                color = ConvertHexToColor(_config.Discord.ReportEmbedColor),
                fields = new[]
                {
                    new
                    {
                        name = $"🎯 {_localizer["Victim"]}",
                        value = $"**{_localizer["Name"]}** {reporterNameClean}\n**SteamID:** {reporterSteamId}\n**Steam:** [{_localizer["LinkToProfile"]}](https://steamcommunity.com/profiles/{reporterSteamId}/)",
                        inline = false
                    },
                    new
                    {
                        name = $"⚠️ {_localizer["Reported"]}",
                        value = $"**{_localizer["Name"]}** {targetNameClean}\n**SteamID:** {targetSteamId}\n**Steam:** [{_localizer["LinkToProfile"]}](https://steamcommunity.com/profiles/{targetSteamId}/)",
                        inline = false
                    },
                    new
                    {
                        name = $"📝 {_localizer["Reason"]}",
                        value = reasonClean,
                        inline = false
                    },
                    new
                    {
                        name = $"🔗 {_localizer["DirectConnect"]}",
                        value = $"[**`connect {serverInfo}`**]({customDomain}?ip={serverInfo}) {_localizer["ClickToConnect"]}",
                        inline = false
                    }
                }
            };

            await SendToDiscordWebhook(embed, mentionMessage);
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
            if (string.IsNullOrEmpty(_config.Discord.WebhookUrl)) return;

            var adminNameClean = CleanPlayerName(adminName);
            var customDomain = GetCustomDomain();

            var embed = new
            {
                title = hostname,
                description = _localizer["EmbedDescription"].Value,
                color = ConvertHexToColor(_config.Discord.ClaimEmbedColor),
                fields = new[]
                {
                    new
                    {
                        name = $"👮 {_localizer["Admin"]}",
                        value = adminNameClean,
                        inline = false
                    },
                    new
                    {
                        name = $"🔗 {_localizer["DirectConnect"]}",
                        value = $"[**`connect {serverInfo}`**]({customDomain}?ip={serverInfo}) {_localizer["ClickToConnect"]}",
                        inline = false
                    }
                }
            };

            await SendToDiscordWebhook(embed, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error sending claim to Discord: {ex.Message}");
        }
    }

    private async Task SendToDiscordWebhook(object embed, string? mentionMessage)
    {
        try
        {
            object payload;

            if (string.IsNullOrEmpty(mentionMessage))
            {
                payload = new { embeds = new[] { embed } };
            }
            else
            {
                payload = new { content = mentionMessage, embeds = new[] { embed } };
            }

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_config.Discord.WebhookUrl, content);

            Console.ForegroundColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"[CallAdminSystem] Discord webhook: {(response.IsSuccessStatusCode ? "Success" : $"Error: {response.StatusCode}")}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error sending to Discord: {ex.Message}");
        }
    }

    private string GetCustomDomain()
    {
        return string.IsNullOrWhiteSpace(_config.Server.CustomDomain) ? CUSTOM_DOMAIN_DEFAULT : _config.Server.CustomDomain;
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