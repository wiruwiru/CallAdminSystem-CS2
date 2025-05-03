using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CallAdminSystem;

public class CallAdminSystem : BasePlugin, IPluginConfig<BaseConfigs>
{
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleVersion => "v1.1.0";
    public override string ModuleName => "CallAdminSystem";
    public override string ModuleDescription => "Allows players to report another user who is breaking the community rules, this report is sent as an embed message to Discord so that administrators can respond.";

    private Dictionary<string, DateTime> _lastCommandTimes = new Dictionary<string, DateTime>();
    private string? cachedIPandPort;
    private string? _hostname;
    private PersonTargetData?[] _selectedReason = new PersonTargetData?[65];

    public override void Load(bool hotReload)
    {
        var mapsFilePath = Path.Combine(ModuleDirectory, "reasons.txt");
        if (!File.Exists(mapsFilePath))
            File.WriteAllText(mapsFilePath, "");

        RegisterListener<Listeners.OnClientConnected>(slot => _selectedReason[slot + 1] = new PersonTargetData { Target = -1, IsSelectedReason = false });
        RegisterListener<Listeners.OnClientDisconnectPost>(slot => _selectedReason[slot + 1] = null);

        foreach (var command in Config.ReportCommands)
        {
            AddCommand(command, "", (controller, info) =>
            {
                if (controller == null) return;

                var players = Utilities.GetPlayers().Where(x => !x.IsBot && x.Connected == PlayerConnectedState.PlayerConnected);

                if (players.Count() < Config.MinimumPlayers)
                {
                    controller.PrintToChat(Localizer["Prefix"] + " " + Localizer["InsufficientPlayers"]);
                    return;
                }

                string playerId = controller.SteamID.ToString();

                int secondsRemaining;
                if (!CheckCommandCooldown(playerId, out secondsRemaining))
                {
                    controller.PrintToChat(Localizer["Prefix"] + " " + Localizer["CommandCooldownMessage", secondsRemaining]);
                    return;
                }

                var reportMenu = new ChatMenu(Localizer["SelectPlayerToReport"]);
                reportMenu.MenuOptions.Clear();

                foreach (var player in players)
                {
                    if (player == controller) continue;

                    var playerName = player.PlayerName;
                    playerName = playerName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();

                    reportMenu.AddMenuOption($"{playerName} [#{player.Index}]", HandleMenu);
                }

                _lastCommandTimes[playerId] = DateTime.Now;
                MenuManager.OpenChatMenu(controller, reportMenu);
            });
        }

        AddCommand(Config.ClaimCommand, "", (controller, info) =>
        {
            if (controller == null) return;

            var validador = new RequiresPermissions(Config.ClaimCommandFlag);
            validador.Command = Config.ClaimCommand;
            if (!validador.CanExecuteCommand(controller))
            {
                controller.PrintToChat(Localizer["Prefix"] + " " + Localizer["NoPermissions"]);
                return;
            }

            ClaimCommand(controller, controller.PlayerName ?? Localizer["UnknownPlayer"]);

            controller.PrintToChat(Localizer["Prefix"] + " " + Localizer["SendClaim"]);
        });

        AddCommandListener("say", Listener_Say);
        AddCommandListener("say_team", Listener_Say);

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
    public required BaseConfigs Config { get; set; }

    public void OnConfigParsed(BaseConfigs config)
    {
        Config = config;
    }

    private bool CheckCommandCooldown(string playerId, out int secondsRemaining)
    {
        if (_lastCommandTimes.TryGetValue(playerId, out DateTime lastCommandTime))
        {
            var secondsSinceLastCommand = (int)(DateTime.Now - lastCommandTime).TotalSeconds;
            secondsRemaining = Config.CommandCooldownSeconds - secondsSinceLastCommand;
            return secondsRemaining <= 0;
        }
        else
        {
            secondsRemaining = 0;
            return true;
        }
    }

    private int ConvertHexToColor(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex[1..];
        }
        return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/generic")]
    public void ClaimCommand(CCSPlayerController? caller, string clientName)
    {
        if (caller == null) return;
        clientName = clientName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();

        var embed = new
        {
            // title = Localizer["EmbedTitle"],
            title = _hostname,
            description = Localizer["EmbedDescription"],
            color = ConvertHexToColor(Config.ClaimEmbedColor),
            fields = new[]
        {
            new
            {
                name = Localizer["Admin"],
                value = $"{clientName}",
                inline = false
            },
            new
            {
                name = Localizer["DirectConnect"],
                value = $"[**`connect {cachedIPandPort}`**]({GetCustomDomain()}?ip={cachedIPandPort})  {Localizer["ClickToConnect"]}",
                inline = false
            }
        }
        };

        Task.Run(() => SendEmbedToDiscord(embed));
    }

    private ChatMenuOption? _selectedMenuOption;

    private HookResult Listener_Say(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player == null) return HookResult.Continue;

        if (_selectedReason[player.Index] != null && _selectedReason[player.Index]!.IsSelectedReason && _selectedReason[player.Index]!.CustomReason && _selectedMenuOption != null)
        {
            var msg = commandinfo.ArgString;

            if (msg.ToLower().Contains("cancel"))
            {
                player.PrintToChat(Localizer["Prefix"] + " " + Localizer["SubmissionCanceled"]);
                _selectedReason[player.Index]!.IsSelectedReason = false;
                return HookResult.Handled;
            }

            var parts = _selectedMenuOption.Text.Split('[', ']');
            var lastPart = parts[^2];
            var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

            var target = Utilities.GetPlayerFromIndex(int.Parse(numbersOnly.Trim()));
            var playerName = player.PlayerName;
            var playerSid = player.SteamID.ToString();
            var targetName = target.PlayerName;
            var targetSid = target.SteamID.ToString();
            var ip = target.SteamID.ToString();

            Task.Run(() => SendMessageToDiscord(playerName, playerSid, targetName, targetSid, msg));

            string PlayerReportedName = target.PlayerName;

            _selectedReason[player.Index]!.IsSelectedReason = false;
            player.PrintToChat(Localizer["Prefix"] + " " + Localizer["SendReport", PlayerReportedName]);

            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private void HandleMenu2CustomReason(CCSPlayerController controller, ChatMenuOption option)
    {
        _selectedReason[controller.Index] = new PersonTargetData { IsSelectedReason = true, CustomReason = true };

        controller.PrintToChat(Localizer["Prefix"] + " " + Localizer["WriteReason"]);

        AddTimer(20.0f, () =>
        {
            if (_selectedReason[controller.Index] != null && _selectedReason[controller.Index]!.IsSelectedReason && _selectedReason[controller.Index]!.CustomReason)
            {
                controller.PrintToChat(Localizer["Prefix"] + " " + Localizer["SubmissionCanceled"]);
                _selectedReason[controller.Index]!.IsSelectedReason = false;
            }
        });
    }


    private void HandleMenu(CCSPlayerController controller, ChatMenuOption option)
    {
        var parts = option.Text.Split('[', ']');
        var lastPart = parts[^2];
        var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

        _selectedMenuOption = option;

        var index = int.Parse(numbersOnly.Trim());
        var reason = File.ReadAllLines(Path.Combine(ModuleDirectory, "reasons.txt"));
        var reasonMenu = new ChatMenu(Localizer["SelectReasonToReport"]);
        reasonMenu.MenuOptions.Clear();

        reasonMenu.AddMenuOption($"{Localizer["CustomReason"]} [{index}]", HandleMenu2CustomReason);

        foreach (var a in reason)
        {
            reasonMenu.AddMenuOption($"{a} [{index}]", HandleMenu2);
        }
        MenuManager.OpenChatMenu(controller, reasonMenu);
    }

    private void HandleMenu2(CCSPlayerController controller, ChatMenuOption option)
    {
        var parts = option.Text.Split('[', ']');
        var lastPart = parts[^2];
        var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));

        var target = Utilities.GetPlayerFromIndex(int.Parse(numbersOnly.Trim()));
        if (target == null)
        {
            controller.PrintToChat(Localizer["Prefix"] + " " + Localizer["PlayerNotFound"]);
            return;
        }
        var playerName = controller.PlayerName;
        var playerSid = controller.SteamID.ToString();
        var targetName = target.PlayerName;
        var targetSid = target.SteamID.ToString();
        var ip = target.SteamID.ToString();

        string PlayerReportedName = target.PlayerName;

        Task.Run(() => SendMessageToDiscord(playerName, playerSid, targetName, targetSid, parts[0]));

        controller.PrintToChat(Localizer["Prefix"] + " " + Localizer["SendReport", PlayerReportedName]);
    }

    private async void SendMessageToDiscord(string clientName, string clientSteamId, string targetName, string targetSteamId, string msg)
    {
        try
        {
            var webhookUrl = GetWebhook();

            if (string.IsNullOrEmpty(webhookUrl)) return;

            var httpClient = new HttpClient();

            if (string.IsNullOrEmpty(msg)) return;
            clientName = clientName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();
            targetName = targetName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();

            msg = msg.Trim('"');

            string mentionRoleIDMessage = $"<@&{MentionRoleID()}>";
            string MentionMessage = Localizer["DiscordMention", mentionRoleIDMessage];

            var payload = new
            {

                content = MentionMessage,
                embeds = new[]
                {
                    new
                    {
                        // title = Localizer["NewReport"],
                        title = _hostname,
                        description = Localizer["NewReportDescription"],
                        color = ConvertHexToColor(Config.ReportEmbedColor),
                        fields = new[]
                        {
                            new
                            {
                                name = Localizer["Victim"],
                                value =
                                    $"**{Localizer["Name"]}** {clientName}\n**SteamID:** {clientSteamId}\n**Steam:** [{Localizer["LinkToProfile"]}](https://steamcommunity.com/profiles/{clientSteamId}/)",
                                inline = false
                            },
                            new
                            {
                                name = Localizer["Reported"],
                                value =
                                    $"**{Localizer["Name"]}** {targetName}\n**SteamID:** {targetSteamId}\n**Steam:** [{Localizer["LinkToProfile"]}](https://steamcommunity.com/profiles/{targetSteamId}/)",
                                inline = false
                            },
                            new
                            {
                                name = Localizer["Reason"],
                                value = msg,
                                inline = false
                            },
                            new
                            {
                                name = Localizer["DirectConnect"],
                                value = $"[**`connect {cachedIPandPort}`**]({GetCustomDomain()}?ip={cachedIPandPort})  {Localizer["ClickToConnect"]}",
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
            throw;
        }

    }

    private async Task SendEmbedToDiscord(object embed)
    {
        try
        {
            var webhookUrl = GetWebhook();

            if (string.IsNullOrEmpty(webhookUrl)) return;

            var httpClient = new HttpClient();

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
            throw;
        }
    }

    private string GetWebhook()
    {
        return Config.WebhookUrl;
    }
    private string GetCustomDomain()
    {
        return Config.CustomDomain;
    }
    private string MentionRoleID()
    {
        return Config.MentionRoleID;
    }
}

public class PersonTargetData
{
    public int Target { get; set; }
    public bool IsSelectedReason { get; set; }
    public bool CustomReason { get; set; }
}