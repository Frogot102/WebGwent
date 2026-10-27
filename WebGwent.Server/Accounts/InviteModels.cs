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

public enum InviteRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public sealed class InviteRequestDto
{
    public string Id { get; set; } = "";
    public Guid AccountId { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int RequestedDays { get; set; } = 1;
    public InviteRequestStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? GrantedInviteCode { get; set; }
}
