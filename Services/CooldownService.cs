using CallAdminSystem.Configs;

namespace CallAdminSystem.Services;

public class CooldownService : IDisposable
{
    private BaseConfigs _config;
    private readonly Dictionary<string, DateTime> _lastCommandTimes;
    private readonly Timer _cleanupTimer;

    public CooldownService(BaseConfigs config)
    {
        _config = config;
        _lastCommandTimes = new Dictionary<string, DateTime>();

        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public void UpdateConfig(BaseConfigs config)
    {
        _config = config;
    }

    public bool CheckCooldown(string playerId, out int secondsRemaining)
    {
        if (_lastCommandTimes.TryGetValue(playerId, out DateTime lastCommandTime))
        {
            var secondsSinceLastCommand = (int)(DateTime.Now - lastCommandTime).TotalSeconds;
            secondsRemaining = _config.CommandCooldownSeconds - secondsSinceLastCommand;
            return secondsRemaining <= 0;
        }

        secondsRemaining = 0;
        return true;
    }

    public void SetCooldown(string playerId)
    {
        _lastCommandTimes[playerId] = DateTime.Now;
    }

    public void RemoveCooldown(string playerId)
    {
        _lastCommandTimes.Remove(playerId);
    }

    public void ClearAllCooldowns()
    {
        _lastCommandTimes.Clear();
    }

    public int GetRemainingCooldown(string playerId)
    {
        if (CheckCooldown(playerId, out int secondsRemaining))
        {
            return 0;
        }
        return secondsRemaining;
    }

    public Dictionary<string, TimeSpan> GetActiveCooldowns()
    {
        var activeCooldowns = new Dictionary<string, TimeSpan>();
        var now = DateTime.Now;

        foreach (var kvp in _lastCommandTimes)
        {
            var elapsed = now - kvp.Value;
            var cooldownTime = TimeSpan.FromSeconds(_config.CommandCooldownSeconds);

            if (elapsed < cooldownTime)
            {
                activeCooldowns[kvp.Key] = cooldownTime - elapsed;
            }
        }

        return activeCooldowns;
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.Now;
        var expiredKeys = new List<string>();

        foreach (var kvp in _lastCommandTimes)
        {
            if ((now - kvp.Value).TotalSeconds > _config.CommandCooldownSeconds + 300)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _lastCommandTimes.Remove(key);
        }

        if (expiredKeys.Count > 0)
        {
            Console.WriteLine($"[CallAdminSystem] Cleaned up {expiredKeys.Count} expired cooldown entries");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _lastCommandTimes.Clear();
    }
}