namespace CallAdminSystem.Models;

public class PersonTargetData
{
    public int Target { get; set; } = -1;
    public bool IsSelectedReason { get; set; } = false;
    public bool CustomReason { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? LastReason { get; set; }

    public bool IsExpired(int timeoutSeconds = 30)
    {
        return (DateTime.Now - CreatedAt).TotalSeconds > timeoutSeconds;
    }

    public void Reset()
    {
        Target = -1;
        IsSelectedReason = false;
        CustomReason = false;
        CreatedAt = DateTime.Now;
        LastReason = null;
    }
}