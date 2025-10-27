using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace CallAdminSystem.Utils;

public static class CommandUtils
{
    public static bool HasPermission(CCSPlayerController? player, string permission)
    {
        if (player == null || !player.IsValid)
        {
            return false;
        }

        return AdminManager.PlayerHasPermissions(player, permission);
    }

    public static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null &&
               player.IsValid &&
               !player.IsBot &&
               player.Connected == PlayerConnectedState.PlayerConnected;
    }

    public static string CleanPlayerName(string playerName)
    {
        return playerName
            .Replace("[Ready]", "")
            .Replace("[No Ready]", "")
            .Replace("[READY]", "")
            .Replace("[NOT READY]", "")
            .Trim();
    }

    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return input
            .Trim()
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ");
    }

    public static bool IsValidSteamId(string steamId)
    {
        return !string.IsNullOrEmpty(steamId) &&
               ulong.TryParse(steamId, out ulong result) &&
               result > 0;
    }
}