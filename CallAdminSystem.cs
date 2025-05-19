using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using CS2ScreenMenuAPI;

namespace CallAdminSystem;

public class CallAdminSystem : BasePlugin, IPluginConfig<BaseConfigs>
{
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleVersion => "v2.0.0";
    public override string ModuleName => "CallAdminSystem";
    public override string ModuleDescription => "Allows players to report another user who is breaking the community rules, this report is sent as an embed message to Discord so that administrators can respond.";

    private Translator _translator;
    private Dictionary<string, DateTime> _lastCommandTimes = new();
    private string? cachedIPandPort;
    private string? _hostname;
    private PersonTargetData?[] _selectedReason = new PersonTargetData?[65];
    private IMenuOption? _selectedMenuOption;
    public BaseConfigs Config { get; set; } = new();

    public void OnConfigParsed(BaseConfigs config)
    {
        Config = config;
    }

    public CallAdminSystem(IStringLocalizer localizer)
    {
        _translator = new Translator(localizer);
    }

    public override void Load(bool hotReload)
    {
        var mapsFilePath = Path.Combine(ModuleDirectory, "reasons.txt");
        if (!File.Exists(mapsFilePath))
            File.WriteAllText(mapsFilePath, "");

        RegisterListener<Listeners.OnClientConnected>(slot => _selectedReason[slot + 1] = new PersonTargetData { Target = -1, IsSelectedReason = false });
        RegisterListener<Listeners.OnClientDisconnectPost>(slot => _selectedReason[slot + 1] = null);

        foreach (var command in Config.ReportCommands)
        {
            AddCommand(command, "", OnReportCommand);
        }

        AddCommand(Config.ClaimCommand, "", OnClaimCommand);

        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);

        if (Config.GetIPandPORTautomatic)
        {
            string? ip = ConVar.Find("ip")?.StringValue;
            string? port = ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString();

            cachedIPandPort = !string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port) ? $"{ip}:{port}" : Config.IPandPORT;
        }
        else
        {
            cachedIPandPort = Config.IPandPORT;
        }

        _hostname = Config.UseHostname ? (ConVar.Find("hostname")?.StringValue ?? Localizer["NewReport"]) : Localizer["NewReport"];
    }

    private void OnReportCommand(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null) return;

        var players = Utilities.GetPlayers().Where(x => !x.IsBot && x.Connected == PlayerConnectedState.PlayerConnected);

        if (players.Count() < Config.MinimumPlayers)
        {
            controller.PrintToChat($"{_translator["Prefix"]} {_translator["InsufficientPlayers"]}");
            return;
        }

        string playerId = controller.SteamID.ToString();

        if (!CheckCommandCooldown(playerId, out int secondsRemaining))
        {
            controller.PrintToChat($"{_translator["Prefix"]} {_translator["CommandCooldownMessage", secondsRemaining]}");
            return;
        }

        var reportMenu = new Menu(controller, this)
        {
            Title = _translator["SelectPlayerToReport"],
            HasExitButon = true,
            PostSelect = PostSelect.Nothing
        };

        foreach (var player in players)
        {
            if (player == controller || player.Team == CsTeam.None) continue;

            var playerName = player.PlayerName;
            playerName = playerName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();

            reportMenu.AddItem($"{playerName} [#{player.Index}]", (p, option) =>
            {
                HandlePlayerSelection(p, option);
            });
        }

        _lastCommandTimes[playerId] = DateTime.Now;
        reportMenu.Display();
    }

    private void OnClaimCommand(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null) return;

        var validator = new RequiresPermissions(Config.ClaimCommandFlag);
        validator.Command = Config.ClaimCommand;
        if (!validator.CanExecuteCommand(controller))
        {
            controller.PrintToChat($"{_translator["Prefix"]} {_translator["NoPermissions"]}");
            return;
        }

        ExecuteClaimCommand(controller, controller.PlayerName ?? _translator["UnknownPlayer"]);
        controller.PrintToChat($"{_translator["Prefix"]} {_translator["SendClaim"]}");
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || _selectedReason[player.Index] == null ||
            !_selectedReason[player.Index]!.IsSelectedReason ||
            !_selectedReason[player.Index]!.CustomReason ||
            _selectedMenuOption == null)
        {
            return HookResult.Continue;
        }

        var msg = commandInfo.ArgString;

        if (msg.ToLower().Contains("cancel"))
        {
            player.PrintToChat($"{_translator["Prefix"]} {_translator["SubmissionCanceled"]}");
            _selectedReason[player.Index]!.IsSelectedReason = false;
            return HookResult.Handled;
        }

        var parts = _selectedMenuOption.Text.Split('[', ']');
        var lastPart = parts[^2];
        var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

        var target = Utilities.GetPlayerFromIndex(int.Parse(numbersOnly.Trim()));
        if (target == null)
        {
            player.PrintToChat($"{_translator["Prefix"]} {_translator["PlayerNotFound"]}");
            return HookResult.Handled;
        }

        _ = SendMessageToDiscord(
            player.PlayerName,
            player.SteamID.ToString(),
            target.PlayerName,
            target.SteamID.ToString(),
            msg
        );

        _selectedReason[player.Index]!.IsSelectedReason = false;
        player.PrintToChat($"{_translator["Prefix"]} {_translator["SendReport", target.PlayerName]}");

        return HookResult.Handled;
    }

    private void HandlePlayerSelection(CCSPlayerController controller, IMenuOption option)
    {
        _selectedMenuOption = option;
        var parts = option.Text.Split('[', ']');
        var lastPart = parts[^2];
        var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

        var index = int.Parse(numbersOnly.Trim());
        var reasons = File.ReadAllLines(Path.Combine(ModuleDirectory, "reasons.txt"));
        var reasonMenu = new Menu(controller, this)
        {
            Title = _translator["SelectReasonToReport"],
            HasExitButon = true,
            PostSelect = PostSelect.Nothing
        };

        reasonMenu.AddItem($"{_translator["CustomReason"]} [{index}]", (p, opt) =>
        {
            _selectedReason[p.Index] = new PersonTargetData { IsSelectedReason = true, CustomReason = true };
            p.PrintToChat($"{_translator["Prefix"]} {_translator["WriteReason"]}");
            reasonMenu.Close(p);

            AddTimer(20.0f, () =>
            {
                if (_selectedReason[p.Index] != null && _selectedReason[p.Index]!.IsSelectedReason && _selectedReason[p.Index]!.CustomReason)
                {
                    p.PrintToChat($"{_translator["Prefix"]} {_translator["SubmissionCanceled"]}");
                    _selectedReason[p.Index]!.IsSelectedReason = false;
                }
            });
        });

        foreach (var reason in reasons)
        {
            reasonMenu.AddItem($"{reason} [{index}]", (p, opt) =>
            {
                HandleReasonSelection(p, opt);
                reasonMenu.Close(p);
            });
        }

        reasonMenu.Display();
    }

    private void HandleReasonSelection(CCSPlayerController controller, IMenuOption option)
    {
        var parts = option.Text.Split('[', ']');
        var lastPart = parts[^2];
        var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

        var target = Utilities.GetPlayerFromIndex(int.Parse(numbersOnly.Trim()));
        if (target == null)
        {
            controller.PrintToChat($"{_translator["Prefix"]} {_translator["PlayerNotFound"]}");
            return;
        }

        _ = SendMessageToDiscord(
            controller.PlayerName,
            controller.SteamID.ToString(),
            target.PlayerName,
            target.SteamID.ToString(),
            parts[0]
        );

        controller.PrintToChat($"{_translator["Prefix"]} {_translator["SendReport", target.PlayerName]}");
    }

    private bool CheckCommandCooldown(string playerId, out int secondsRemaining)
    {
        if (_lastCommandTimes.TryGetValue(playerId, out DateTime lastCommandTime))
        {
            var secondsSinceLastCommand = (int)(DateTime.Now - lastCommandTime).TotalSeconds;
            secondsRemaining = Config.CommandCooldownSeconds - secondsSinceLastCommand;
            return secondsRemaining <= 0;
        }

        secondsRemaining = 0;
        return true;
    }

    private int ConvertHexToColor(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex[1..];
        }
        return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
    }

    private void ExecuteClaimCommand(CCSPlayerController caller, string clientName)
    {
        if (caller == null) return;
        clientName = clientName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();

        var embed = new
        {
            title = _hostname,
            description = _translator["EmbedDescription"],
            color = ConvertHexToColor(Config.ClaimEmbedColor),
            fields = new[]
            {
                new
                {
                    name = _translator["Admin"],
                    value = $"{clientName}",
                    inline = false
                },
                new
                {
                    name = _translator["DirectConnect"],
                    value = $"[**`connect {cachedIPandPort}`**]({GetCustomDomain()}?ip={cachedIPandPort})  {_translator["ClickToConnect"]}",
                    inline = false
                }
            }
        };

        _ = SendEmbedToDiscord(embed);
    }

    private async Task SendMessageToDiscord(string clientName, string clientSteamId, string targetName, string targetSteamId, string msg)
    {
        try
        {
            var webhookUrl = GetWebhook();
            if (string.IsNullOrEmpty(webhookUrl)) return;

            using var httpClient = new HttpClient();

            clientName = clientName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();
            targetName = targetName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();
            msg = msg.Trim('"');

            string mentionRoleIDMessage = $"<@&{MentionRoleID()}>";
            string mentionMessage = _translator["DiscordMention", mentionRoleIDMessage];

            var payload = new
            {
                content = mentionMessage,
                embeds = new[]
                {
                    new
                    {
                        title = _hostname,
                        description = _translator["NewReportDescription"],
                        color = ConvertHexToColor(Config.ReportEmbedColor),
                        fields = new[]
                        {
                            new
                            {
                                name = _translator["Victim"],
                                value = $"**{_translator["Name"]}** {clientName}\n**SteamID:** {clientSteamId}\n**Steam:** [{_translator["LinkToProfile"]}](https://steamcommunity.com/profiles/{clientSteamId}/)",
                                inline = false
                            },
                            new
                            {
                                name = _translator["Reported"],
                                value = $"**{_translator["Name"]}** {targetName}\n**SteamID:** {targetSteamId}\n**Steam:** [{_translator["LinkToProfile"]}](https://steamcommunity.com/profiles/{targetSteamId}/)",
                                inline = false
                            },
                            new
                            {
                                name = _translator["Reason"],
                                value = msg,
                                inline = false
                            },
                            new
                            {
                                name = _translator["DirectConnect"],
                                value = $"[**`connect {cachedIPandPort}`**]({GetCustomDomain()}?ip={cachedIPandPort})  {_translator["ClickToConnect"]}",
                                inline = false
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(webhookUrl, content);

            Console.ForegroundColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(response.IsSuccessStatusCode ? "Success" : $"Error: {response.StatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task SendEmbedToDiscord(object embed)
    {
        try
        {
            var webhookUrl = GetWebhook();
            if (string.IsNullOrEmpty(webhookUrl)) return;

            using var httpClient = new HttpClient();

            var payload = new
            {
                embeds = new[] { embed }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(webhookUrl, content);

            Console.ForegroundColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(response.IsSuccessStatusCode ? "Success" : $"Error: {response.StatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private string GetWebhook() => Config.WebhookUrl;
    private string GetCustomDomain() => Config.CustomDomain;
    private string MentionRoleID() => Config.MentionRoleID;
}

public class PersonTargetData
{
    public int Target { get; set; }
    public bool IsSelectedReason { get; set; }
    public bool CustomReason { get; set; }
}