using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Localization;

using CallAdminSystem.Configs;

namespace CallAdminSystem.Services;

public class ReportService : IDisposable
{
    private BaseConfigs _config;
    private readonly DiscordService _discordService;
    private readonly IStringLocalizer _localizer;
    private string? _cachedServerInfo;
    private string? _cachedHostname;

    public ReportService(BaseConfigs config, DiscordService discordService, IStringLocalizer localizer)
    {
        _config = config;
        _discordService = discordService;
        _localizer = localizer;
        UpdateServerInfo();
    }

    public void UpdateConfig(BaseConfigs config)
    {
        _config = config;
        UpdateServerInfo();
    }

    public void SendReport(CCSPlayerController reporter, CCSPlayerController target, string reason)
    {
        if (reporter == null || !reporter.IsValid || target == null || !target.IsValid)
        {
            Console.WriteLine("[CallAdminSystem] Invalid players in SendReport");
            return;
        }

        try
        {
            var reporterName = reporter.PlayerName;
            var reporterSteamId = reporter.SteamID.ToString();
            var targetName = target.PlayerName;
            var targetSteamId = target.SteamID.ToString();

            Task.Run(async () =>
            {
                await _discordService.SendReportToDiscord(
                    reporterName,
                    reporterSteamId,
                    targetName,
                    targetSteamId,
                    reason,
                    GetServerInfo(),
                    GetHostname()
                );
            });

            Console.WriteLine($"[CallAdminSystem] Report sent: {CleanPlayerName(reporterName)} reported {CleanPlayerName(targetName)} for: {reason}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error in SendReport: {ex.Message}");
        }
    }

    public void SendClaim(CCSPlayerController admin)
    {
        if (admin == null || !admin.IsValid)
        {
            Console.WriteLine("[CallAdminSystem] Invalid admin in SendClaim");
            return;
        }

        try
        {
            var adminName = admin.PlayerName ?? _localizer["UnknownPlayer"];

            Task.Run(async () =>
            {
                await _discordService.SendClaimToDiscord(
                    adminName,
                    GetServerInfo(),
                    GetHostname()
                );
            });

        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error in SendClaim: {ex.Message}");
        }
    }

    private void UpdateServerInfo()
    {
        try
        {
            if (_config.Server.GetIPandPORTautomatic)
            {
                string? ip = ConVar.Find("ip")?.StringValue;
                string? port = ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString();
                _cachedServerInfo = !string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port) ? $"{ip}:{port}" : _config.Server.IPandPORT;
            }
            else
            {
                _cachedServerInfo = _config.Server.IPandPORT;
            }

            _cachedHostname = _config.Server.UseHostname ? (ConVar.Find("hostname")?.StringValue ?? _localizer["NewReport"]) : _localizer["NewReport"];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error updating server info: {ex.Message}");
            _cachedServerInfo = _config.Server.IPandPORT;
            _cachedHostname = _localizer["NewReport"];
        }
    }

    public string GetServerInfo()
    {
        return _cachedServerInfo ?? _config.Server.IPandPORT;
    }

    public string GetHostname()
    {
        return _cachedHostname ?? _localizer["NewReport"];
    }

    private static string CleanPlayerName(string playerName)
    {
        return playerName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();
    }

    public void Dispose()
    {
        // Nothing to dispose currently
    }
}