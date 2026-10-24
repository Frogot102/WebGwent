namespace WebGwent.Server.Accounts;

public sealed class InviteRecord
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public int DurationDays { get; set; } = 30;
    public int MaxActivations { get; set; } = 1;
    public int UsedCount { get; set; }
    public Guid CreatedByAccountId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public bool Revoked { get; set; }
}
