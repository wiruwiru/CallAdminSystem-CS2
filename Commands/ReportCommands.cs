using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using MenuManager;

using CallAdminSystem.Configs;
using CallAdminSystem.Services;
using CallAdminSystem.Models;

namespace CallAdminSystem.Commands;

public class ReportCommands
{
    private readonly BaseConfigs _config;
    private readonly CooldownService _cooldownService;
    private readonly ReportService _reportService;
    private readonly PersonTargetData?[] _selectedReason;
    private readonly IStringLocalizer _localizer;

    private IMenuApi? _menuApi;

    public ReportCommands(BaseConfigs config, CooldownService cooldownService, ReportService reportService, PersonTargetData?[] selectedReason, IStringLocalizer localizer)
    {
        _config = config;
        _cooldownService = cooldownService;
        _reportService = reportService;
        _selectedReason = selectedReason;
        _localizer = localizer;
    }

    public void SetMenuApi(IMenuApi? menuApi)
    {
        _menuApi = menuApi;
    }

    public void HandleReportCommand(CCSPlayerController? controller, CommandInfo commandInfo)
    {
        if (controller == null || !controller.IsValid) return;

        if (_menuApi == null)
        {
            return;
        }

        var players = Utilities.GetPlayers().Where(x => !x.IsBot && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
        if (players.Count < _config.Server.MinimumPlayers)
        {
            controller.PrintToChat($"{_localizer["Prefix"]} {_localizer["InsufficientPlayers"]}");
            return;
        }

        string playerId = controller.SteamID.ToString();
        if (!_cooldownService.CheckCooldown(playerId, out int secondsRemaining))
        {
            controller.PrintToChat($"{_localizer["Prefix"]} {_localizer["CommandCooldownMessage", secondsRemaining]}");
            return;
        }

        _cooldownService.SetCooldown(playerId);

        ShowPlayerSelectionMenu(controller, players);
    }

    private void ShowPlayerSelectionMenu(CCSPlayerController controller, List<CCSPlayerController> players)
    {
        if (_menuApi == null) return;

        var playerMenu = _menuApi.GetMenu(_localizer["SelectPlayerToReport"]);
        foreach (var player in players)
        {
            if (player == controller || player.Team == CsTeam.None) continue;

            var playerName = CleanPlayerName(player.PlayerName);
            var targetIndex = (int)player.Index;

            playerMenu.AddMenuOption($"{playerName} [#{targetIndex}]", (p, option) =>
            {
                ShowReasonSelectionMenu(p, targetIndex);
            });
        }

        playerMenu.Open(controller);
    }

    private void ShowReasonSelectionMenu(CCSPlayerController controller, int targetIndex)
    {
        if (_menuApi == null) return;

        var reasonMenu = _menuApi.GetMenu(_localizer["SelectReasonToReport"]);
        reasonMenu.AddMenuOption(_localizer["CustomReason"], (p, option) =>
        {
            HandleCustomReason(p, targetIndex);
        });

        var reasons = GetPredefinedReasons();
        foreach (var reason in reasons)
        {
            reasonMenu.AddMenuOption(reason, (p, option) =>
            {
                HandleReasonSelection(p, targetIndex, reason);
            });
        }

        reasonMenu.AddMenuOption("← " + _localizer["Back"], (p, option) =>
        {
            var players = Utilities.GetPlayers().Where(x => !x.IsBot && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
            ShowPlayerSelectionMenu(p, players);
        });

        reasonMenu.Open(controller);
    }

    private void HandleCustomReason(CCSPlayerController controller, int targetIndex)
    {
        _selectedReason[(int)controller.Index] = new PersonTargetData
        {
            Target = targetIndex,
            IsSelectedReason = true,
            CustomReason = true
        };

        controller.PrintToChat($"{_localizer["Prefix"]} {_localizer["WriteReason"]}");

        _menuApi?.CloseMenu(controller);

        SetCustomReasonTimeout(controller);
    }

    private void HandleReasonSelection(CCSPlayerController controller, int targetIndex, string reason)
    {
        var target = Utilities.GetPlayerFromIndex(targetIndex);
        if (target == null || !target.IsValid)
        {
            controller.PrintToChat($"{_localizer["Prefix"]} {_localizer["PlayerNotFound"]}");
            return;
        }

        _reportService.SendReport(controller, target, reason);

        _menuApi?.CloseMenu(controller);

        controller.PrintToChat($"{_localizer["Prefix"]} {_localizer["SendReport", CleanPlayerName(target.PlayerName)]}");
    }

    public HookResult HandlePlayerSay(CCSPlayerController player, CommandInfo commandinfo)
    {
        var playerIndex = (int)player.Index;

        if (_selectedReason[playerIndex] == null ||
            !_selectedReason[playerIndex]!.IsSelectedReason ||
            !_selectedReason[playerIndex]!.CustomReason)
        {
            return HookResult.Continue;
        }

        var msg = commandinfo.ArgString;
        if (msg.ToLower().Contains("cancel"))
        {
            player.PrintToChat($"{_localizer["Prefix"]} {_localizer["SubmissionCanceled"]}");
            _selectedReason[playerIndex]!.IsSelectedReason = false;
            return HookResult.Handled;
        }

        var targetIndex = _selectedReason[playerIndex]!.Target;
        var target = Utilities.GetPlayerFromIndex(targetIndex);
        if (target == null || !target.IsValid)
        {
            player.PrintToChat($"{_localizer["Prefix"]} {_localizer["PlayerNotFound"]}");
            _selectedReason[playerIndex]!.IsSelectedReason = false;
            return HookResult.Handled;
        }

        _reportService.SendReport(player, target, msg.Trim('"'));
        _selectedReason[playerIndex]!.IsSelectedReason = false;

        player.PrintToChat($"{_localizer["Prefix"]} {_localizer["SendReport", CleanPlayerName(target.PlayerName)]}");

        return HookResult.Handled;
    }

    private void SetCustomReasonTimeout(CCSPlayerController controller)
    {
        var playerIndex = (int)controller.Index;

        Server.NextFrame(() =>
        {
            var timer = new Timer(_ =>
            {
                try
                {
                    if (controller.IsValid && _selectedReason[playerIndex] != null && _selectedReason[playerIndex]!.IsSelectedReason && _selectedReason[playerIndex]!.CustomReason)
                    {
                        controller.PrintToChat($"{_localizer["Prefix"]} {_localizer["SubmissionCanceled"]}");
                        _selectedReason[playerIndex]!.IsSelectedReason = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CallAdminSystem] Error in timeout: {ex.Message}");
                }
            }, null, TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(-1));
        });
    }

    private List<string> GetPredefinedReasons()
    {
        try
        {
            var reasonsPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "plugins", "CallAdminSystem", "reasons.txt");
            if (File.Exists(reasonsPath))
            {
                return File.ReadAllLines(reasonsPath).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CallAdminSystem] Error reading reasons file: {ex.Message}");
        }

        return new List<string> { "Cheating", "Toxic Behavior", "Team Killing", "Griefing" };
    }

    private static string CleanPlayerName(string playerName)
    {
        return playerName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();
    }
}