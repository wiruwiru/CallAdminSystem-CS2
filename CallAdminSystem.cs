using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using MenuManager;

using CallAdminSystem.Configs;
using CallAdminSystem.Commands;
using CallAdminSystem.Services;
using CallAdminSystem.Models;

namespace CallAdminSystem;

public class CallAdminSystem : BasePlugin, IPluginConfig<BaseConfigs>
{
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleVersion => "v2.0.0";
    public override string ModuleName => "CallAdminSystem";
    public override string ModuleDescription => "Allows players to report users with Discord integration and MenuManager support";

    public required BaseConfigs Config { get; set; }

    // MenuManager capability
    private IMenuApi? _menuApi;
    private readonly PluginCapability<IMenuApi?> _menuCapability = new("menu:nfcore");

    // Services
    private CooldownService? _cooldownService;
    private DiscordService? _discordService;
    private ReportService? _reportService;

    // Commands
    private ReportCommands? _reportCommands;
    private ClaimCommands? _claimCommands;

    // Data storage
    private PersonTargetData?[] _selectedReason = new PersonTargetData?[65];
    private string? _cachedIPandPort;
    private string? _hostname;

    public override void Load(bool hotReload)
    {
        InitializeServices();
        InitializeCommands();
        SetupListeners();
        InitializeServerInfo();
        CreateReasonsFileIfNotExists();
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _menuApi = _menuCapability.Get();

        if (_menuApi == null)
        {
            Console.WriteLine("[CallAdminSystem] MenuManager API not found");
        }
        else
        {
            if (_reportCommands != null)
            {
                _reportCommands.SetMenuApi(_menuApi);
            }
        }
    }

    public void OnConfigParsed(BaseConfigs config)
    {
        Config = config;

        if (_discordService != null)
        {
            _discordService.UpdateConfig(config);
        }
        if (_cooldownService != null)
        {
            _cooldownService.UpdateConfig(config);
        }
        if (_reportService != null)
        {
            _reportService.UpdateConfig(config);
        }

        InitializeServerInfo();
    }

    private void InitializeServices()
    {
        _cooldownService = new CooldownService(Config);
        _discordService = new DiscordService(Config, Localizer);
        _reportService = new ReportService(Config, _discordService, Localizer);
    }

    private void InitializeCommands()
    {
        if (_cooldownService == null || _reportService == null) return;

        _reportCommands = new ReportCommands(Config, _cooldownService, _reportService, _selectedReason, Localizer);

        _claimCommands = new ClaimCommands(Config, _discordService!, () => _cachedIPandPort ?? "", () => _hostname ?? "", Localizer);

        foreach (var command in Config.ReportCommands)
        {
            AddCommand(command, "Report a player", _reportCommands.HandleReportCommand);
        }

        AddCommand(Config.ClaimCommand, "Claim admin presence", _claimCommands.HandleClaimCommand);
    }

    private void SetupListeners()
    {
        RegisterListener<Listeners.OnClientConnected>(slot => _selectedReason[(int)slot + 1] = new PersonTargetData { Target = -1, IsSelectedReason = false });

        RegisterListener<Listeners.OnClientDisconnectPost>(slot => _selectedReason[(int)slot + 1] = null);

        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
    }

    private void InitializeServerInfo()
    {
        if (Config.GetIPandPORTautomatic)
        {
            string? ip = ConVar.Find("ip")?.StringValue;
            string? port = ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString();
            _cachedIPandPort = !string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port) ? $"{ip}:{port}" : Config.IPandPORT;
        }
        else
        {
            _cachedIPandPort = Config.IPandPORT;
        }

        _hostname = Config.UseHostname ? (ConVar.Find("hostname")?.StringValue ?? Localizer["NewReport"]) : Localizer["NewReport"];
    }

    private void CreateReasonsFileIfNotExists()
    {
        var reasonsFilePath = Path.Combine(ModuleDirectory, "reasons.txt");
        if (!File.Exists(reasonsFilePath))
        {
            File.WriteAllText(reasonsFilePath, "Cheating\nToxic Behavior\nTeam Killing\nGriefing\n");
        }
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player == null || _reportCommands == null) return HookResult.Continue;

        return _reportCommands.HandlePlayerSay(player, commandinfo);
    }

    public override void Unload(bool hotReload)
    {
        if (_menuApi != null)
        {
            foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid))
            {
                _menuApi.CloseMenu(player);
            }
        }

        _menuApi = null;
        _cooldownService?.Dispose();
        _reportService?.Dispose();
        _discordService?.Dispose();
    }
}