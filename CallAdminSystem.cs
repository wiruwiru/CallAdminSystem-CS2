using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace CallAdminSystem;
public class CallAdminSystem : BasePlugin
{
    public override string ModuleAuthor => "luca";
    public override string ModuleName => "CallAdminSystem";
    public override string ModuleVersion => "v1.0.2";

    private Translator _translator;
    private Dictionary<string, DateTime> _lastCommandTimes = new Dictionary<string, DateTime>();
    private Config _config = null!;
    private PersonTargetData[] _selectedReason = new PersonTargetData[65];

    public CallAdminSystem(IStringLocalizer localizer)
    {
        _translator = new Translator(localizer);
    }

    public override void Load(bool hotReload)
    {
        _config = LoadConfig();
        
        var mapsFilePath = Path.Combine(ModuleDirectory, "reasons.txt");
        if (!File.Exists(mapsFilePath))
            File.WriteAllText(mapsFilePath, "");
        
        RegisterListener<Listeners.OnClientConnected>(slot => _selectedReason[slot + 1] = new PersonTargetData{Target = -1, IsSelectedReason = false});
        RegisterListener<Listeners.OnClientDisconnectPost>(slot => _selectedReason[slot + 1] = null);

        AddCommand("css_report", "", (controller, info) =>
        {
            if (controller == null) return;

            string playerId = controller.SteamID.ToString();

            int secondsRemaining;
            if (!CheckCommandCooldown(playerId, out secondsRemaining))
            {
                controller.PrintToChat(_translator["Prefix"] + " " + _translator["CommandCooldownMessage", secondsRemaining]);
                return;
            }

            var reportMenu = new ChatMenu(_translator["SelectPlayerToReport"]);
            reportMenu.MenuOptions.Clear();

            var players = Utilities.GetPlayers().Where(x => !x.IsBot && x.Connected == PlayerConnectedState.PlayerConnected);
            foreach (var player in players)
            {
                if (player == controller) continue;

                var playerName = player.PlayerName;
                playerName = playerName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();

                reportMenu.AddMenuOption($"{playerName} [#{player.Index}]", HandleMenu);
            }

            _lastCommandTimes[playerId] = DateTime.Now;

            ChatMenus.OpenMenu(controller, reportMenu);
        });

        AddCommand("css_call", "", (controller, info) =>
        {
            if (controller == null) return;

            string playerId = controller.SteamID.ToString();

            int secondsRemaining;
            if (!CheckCommandCooldown(playerId, out secondsRemaining))
            {
                controller.PrintToChat(_translator["Prefix"] + " " + _translator["CommandCooldownMessage", secondsRemaining]);
                return;
            }

            var reportMenu = new ChatMenu(_translator["SelectPlayerToReport"]);
            reportMenu.MenuOptions.Clear();

            var players = Utilities.GetPlayers().Where(x => !x.IsBot && x.Connected == PlayerConnectedState.PlayerConnected);
            foreach (var player in players)
            {
                if (player == controller) continue;

                var playerName = player.PlayerName;
                playerName = playerName.Replace("[Ready]", "").Replace("[No Ready]", "").Trim();

                reportMenu.AddMenuOption($"{playerName} [#{player.Index}]", HandleMenu);
            }

            _lastCommandTimes[playerId] = DateTime.Now;

            ChatMenus.OpenMenu(controller, reportMenu);
        });


        AddCommand("css_claim", "", (controller, info) =>
        {
            if (controller == null) return;

            var validador = new RequiresPermissions("@css/generic");
            validador.Command = "css_claim";
            if (!validador.CanExecuteCommand(controller))
            {
                controller.PrintToChat(_translator["Prefix"] + " " + _translator["NoPermissions"]);
                return;
            }
            
            ClaimCommand(controller, info, controller?.PlayerName ?? "Desconocido");

            controller.PrintToChat(_translator["Prefix"] + " " + _translator["SendClaim"]);
        });


        AddCommandListener("say", Listener_Say);
        AddCommandListener("say_team", Listener_Say);
    }

    private bool CheckCommandCooldown(string playerId, out int secondsRemaining)
    {
        if (_lastCommandTimes.TryGetValue(playerId, out DateTime lastCommandTime))
        {
            var secondsSinceLastCommand = (int)(DateTime.Now - lastCommandTime).TotalSeconds;
            secondsRemaining = _config.CommandCooldownSeconds - secondsSinceLastCommand;
            return secondsRemaining <= 0;
        }
        else
        {
            secondsRemaining = 0;
            return true;
        }
    }


    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/generic")]
    public void ClaimCommand(CCSPlayerController? caller, CommandInfo command, string clientName)
    {
        if (caller == null) return;

        var embed = new
        {
            title = _translator["EmbedTitle"],
            description = _translator["EmbedDescription"],
            color = 3093237,
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
                value = $"[**`connect {GetIP()}`**]({GetCustomDomain()}?ip={GetIP()})  {_translator["ClickToConnect"]}",
                inline = false
            }
        }
        };

        Task.Run(() => SendEmbedToDiscord(embed));
    }

    private HookResult Listener_Say(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player == null) return HookResult.Continue;

        if (_selectedReason[player.Index] != null && _selectedReason[player.Index]!.IsSelectedReason)
        {
            var msg = GetTextInsideQuotes(commandinfo.ArgString);
            var target = Utilities.GetPlayerFromIndex(_selectedReason[player.Index]!.Target);
            switch (msg)
            {
                case "cancel":
                    _selectedReason[player.Index]!.IsSelectedReason = false;
                    return HookResult.Handled;
                default:
                    string PlayerReportedName = target.PlayerName;

                    Task.Run(() => SendMessageToDiscord(player.PlayerName, new SteamID(player.SteamID).SteamId2, target.PlayerName,
                        new SteamID(target.SteamID).SteamId2, commandinfo.ArgString, $"{ConVar.Find("hostname")!.StringValue}\n{ConVar.Find("ip")!.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}"));
                    _selectedReason[player.Index]!.IsSelectedReason = false;
                    player.PrintToChat(_translator["Prefix"] + " " + _translator["SendReport", PlayerReportedName]);
                    return HookResult.Handled;
            }
        }
        return HookResult.Continue;
    }

    private void HandleMenu(CCSPlayerController controller, ChatMenuOption option)
    {
        var parts = option.Text.Split('[', ']');
        var lastPart = parts[^2];
        var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));
        
        var index = int.Parse(numbersOnly.Trim());
        var reason = File.ReadAllLines(Path.Combine(ModuleDirectory, "reasons.txt"));
        var reasonMenu = new ChatMenu(_translator["SelectReasonToReport"]);
        reasonMenu.MenuOptions.Clear();

        foreach (var a in reason)
        {
            reasonMenu.AddMenuOption($"{a}", HandleMenu2);
        }
            
        ChatMenus.OpenMenu(controller, reasonMenu);
    }

    private void HandleMenu2(CCSPlayerController controller, ChatMenuOption option)
    {
        var parts = option.Text.Split('[', ']');
        var lastPart = parts[^2];
        var numbersOnly = string.Join("", lastPart.Where(char.IsDigit));
        
        var target = Utilities.GetPlayerFromIndex(int.Parse(numbersOnly.Trim()));
        var playerName = controller.PlayerName;
        var playerSid = controller.SteamID.ToString();
        var targetName = target.PlayerName;
        var targetSid = target.SteamID.ToString();
        var ip = target.SteamID.ToString();

        string PlayerReportedName = controller.PlayerName;

        Task.Run(() => SendMessageToDiscord(playerName, playerSid, targetName,
            targetSid, parts[0], ip));
        
        controller.PrintToChat(_translator["Prefix"] + " " + _translator["SendReport", PlayerReportedName]);
    }

    private async void SendMessageToDiscord(string clientName, string clientSteamId, string targetName,
        string targetSteamId, string msg, string ip)
    {
        try
        {
            var webhookUrl = GetWebhook();

            if (string.IsNullOrEmpty(webhookUrl)) return;

            var httpClient = new HttpClient();

            if (msg == "") return;

            string mentionRoleIDMessage = $"<@&{MentionRoleID()}>";
            string MentionMessage = _translator["DiscordMention", mentionRoleIDMessage];

            var payload = new
            {

                content = MentionMessage,
                embeds = new[]
                {
                    new
                    {
                        title = _translator["NewReport"],
                        description = _translator["NewReportDescription"],
                        color = 16711680,
                        fields = new[]
                        {
                            new
                            {
                                name = _translator["Victim"],
                                value =
                                    $"**{_translator["Name"]}** {clientName}\n**SteamID:** {clientSteamId}\n**Steam:** [{_translator["LinkToProfile"]}](https://steamcommunity.com/profiles/{targetSteamId}/)",
                                inline = false
                            },
                            new
                            {
                                name = _translator["Reported"],
                                value =
                                    $"**{_translator["Name"]}** {targetName}\n**SteamID:** {targetSteamId}\n**Steam:** [{_translator["LinkToProfile"]}](https://steamcommunity.com/profiles/{targetSteamId}/)",
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
                                value = $"[**`connect {GetIP()}`**]({GetCustomDomain()}?ip={GetIP()})  {_translator["ClickToConnect"]}",
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
            Console.WriteLine(response.IsSuccessStatusCode
                ? "Success"
                : $"Error: {response.StatusCode}");
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
            Console.WriteLine(response.IsSuccessStatusCode
                ? "Success"
                : $"Error: {response.StatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private Config LoadConfig()
    {
        var configPath = Path.Combine(ModuleDirectory, "config.json");

        if (!File.Exists(configPath)) return CreateConfig(configPath);

        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath))!;

        return config;
    }

    private Config CreateConfig(string configPath)
    {
        var config = new Config
        {
            WebhookUrl = "", // Debes crearlo en el canal donde enviaras los avisos.
            IPandPORT = "45.235.99.18:27025", // Remplaza por la direcci�n IP de tu servidor.
            CustomDomain = "https://crisisgamer.com/redirect/connect.php", // Si quieres usar tu propio dominio para rediregir las conexiones, debes remplazar esto.
            MentionRoleID = "", // Debes tener activado el modo desarrollador de discord, click derecho en el rol y copias su ID.
            CommandCooldownSeconds = 120 // Tiempo de enfriamiento para que el usuario pueda volver a usar el comando (en segundos)
        };

        File.WriteAllText(configPath,
            JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("[CallAdminSystem] The configuration was successfully saved to a file: " + configPath);
        Console.ResetColor();

        return config;
    }
    private string GetWebhook()
    {
        return _config.WebhookUrl;
    }
    private string GetIP()
    {
        return _config.IPandPORT;
    }
    private string GetCustomDomain()
    {
        return _config.CustomDomain;
    }
    private string MentionRoleID()
    {
        return _config.MentionRoleID;
    }

    private string GetTextInsideQuotes(string input)
    {
        var startIndex = input.IndexOf('"');
        var endIndex = input.LastIndexOf('"');

        if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
        {
            return input.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

        return string.Empty;
    }
}
public class Config
{
    public string WebhookUrl { get; set; } = "";
    public string IPandPORT { get; set; } = "";
    public string CustomDomain { get; set; } = "https://crisisgamer.com/redirect/connect.php";
    public string MentionRoleID { get; set; } = "";
    public int CommandCooldownSeconds { get; set; } = 120;
}

public class PersonTargetData
{
    public int Target { get; set; }
    public bool IsSelectedReason { get; set; }
}