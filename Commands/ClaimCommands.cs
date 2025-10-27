using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Localization;

using CallAdminSystem.Configs;
using CallAdminSystem.Services;
using CallAdminSystem.Utils;

namespace CallAdminSystem.Commands;

public class ClaimCommands
{
    private readonly BaseConfigs _config;
    private readonly DiscordService _discordService;
    private readonly Func<string> _getServerInfo;
    private readonly Func<string> _getHostname;
    private readonly IStringLocalizer _localizer;

    public ClaimCommands(BaseConfigs config, DiscordService discordService, Func<string> getServerInfo, Func<string> getHostname, IStringLocalizer localizer)
    {
        _config = config;
        _discordService = discordService;
        _getServerInfo = getServerInfo;
        _getHostname = getHostname;
        _localizer = localizer;
    }

    public void HandleClaimCommand(CCSPlayerController? controller, CommandInfo commandInfo)
    {
        if (controller == null || !controller.IsValid) return;

        if (!CommandUtils.HasPermission(controller, _config.Permissions.ClaimCommandFlag))
        {
            controller.PrintToChat($"{_localizer["Prefix"]} {_localizer["NoPermissions"]}");
            return;
        }

        ExecuteClaim(controller);
    }

    private void ExecuteClaim(CCSPlayerController controller)
    {
        var clientName = CleanPlayerName(controller.PlayerName ?? _localizer["UnknownPlayer"]);
        var serverInfo = _getServerInfo();
        var hostname = _getHostname();

        Task.Run(async () =>
        {
            try
            {
                await _discordService.SendClaimToDiscord(clientName, serverInfo, hostname);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CallAdminSystem] Error sending claim to Discord: {ex.Message}");
            }
        });

        controller.PrintToChat($"{_localizer["Prefix"]} {_localizer["SendClaim"]}");
    }

    private static string CleanPlayerName(string playerName)
    {
        return playerName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();
    }
}