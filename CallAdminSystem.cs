using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;

using CallAdminSystem.Configs;
using CallAdminSystem.Commands;
using CallAdminSystem.Services;
using CallAdminSystem.Models;

namespace CallAdminSystem;

[MinimumApiVersion(363)]
public class CallAdminSystem : BasePlugin, IPluginConfig<BaseConfigs>
{
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleVersion => "2.1.1";
    public override string ModuleName => "CallAdminSystem";
    public override string ModuleDescription => "Allows players to report users with Discord integration and MySQL database";

    public required BaseConfigs Config { get; set; }

    // Services
    private CooldownService? _cooldownService;
    private DiscordService? _discordService;
    private DatabaseService? _databaseService;
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

        _ = InitializeDatabaseAsync();
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
        _ = InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            if (_databaseService != null && Config.Database.Enabled)
            {
                await _databaseService.InitializeDatabase();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[CallAdminSystem] Database initialized successfully!");
                Console.ResetColor();
            }
            else if (_databaseService != null && !Config.Database.Enabled)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[CallAdminSystem] Database is disabled in configuration");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CallAdminSystem] Failed to initialize database: {ex.Message}");
            Console.ResetColor();
        }
    }

    private void InitializeServices()
    {
        _cooldownService = new CooldownService(Config);
        _discordService = new DiscordService(Config, Localizer);
        _databaseService = new DatabaseService(Config.Database);
        _reportService = new ReportService(Config, _discordService, _databaseService, Localizer);
    }

    private void InitializeCommands()
    {
        if (_cooldownService == null || _reportService == null) return;

        _reportCommands = new ReportCommands(Config, _cooldownService, _reportService, _selectedReason, Localizer);

        _claimCommands = new ClaimCommands(Config, _discordService!, () => _cachedIPandPort ?? "", () => _hostname ?? "", Localizer);

        foreach (var command in Config.Commands.ReportCommands)
        {
            AddCommand(command, "Report a player", _reportCommands.HandleReportCommand);
        }

        foreach (var command in Config.Commands.ClaimCommands)
        {
            AddCommand(command, "Claim admin presence", _claimCommands.HandleClaimCommand);
        }
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
        if (Config.Server.GetIPandPORTautomatic)
        {
            Server.NextWorldUpdate(() =>
            {
                string? ip = ConVar.Find("ip")?.StringValue;
                string? port = ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString();
                _cachedIPandPort = !string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port) ? $"{ip}:{port}" : Config.Server.IPandPORT;
            });
        }
        else
        {
            _cachedIPandPort = Config.Server.IPandPORT;
        }

        _hostname = Config.Server.UseHostname ? (ConVar.Find("hostname")?.StringValue ?? Localizer["NewReport"]) : Localizer["NewReport"];
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
        _cooldownService?.Dispose();
        _reportService?.Dispose();
        _discordService?.Dispose();
    }
}