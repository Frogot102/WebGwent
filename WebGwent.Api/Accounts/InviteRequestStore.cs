using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WebGwent.Api.Data;

namespace WebGwent.Api.Accounts;

public enum InviteRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public sealed class InviteRequestRecord
{
    public string Id { get; set; } = "";
    public Guid AccountId { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int RequestedDays { get; set; } = 1;
    public InviteRequestStatus Status { get; set; } = InviteRequestStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByAccountId { get; set; }
    public string? GrantedInviteCode { get; set; }
}

public sealed class InviteRequestStore
{
    private readonly IDbContextFactory<WebGwentDbContext> _dbFactory;
    private readonly object _lock = new();
    private List<InviteRequestRecord> _items = [];

    public InviteRequestStore(IDbContextFactory<WebGwentDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        Reload();
    }

    public void Reload()
    {
        lock (_lock)
        {
            using var db = _dbFactory.CreateDbContext();
            _items = db.InviteRequests.AsNoTracking().ToList();
        }
    }

    public IReadOnlyList<InviteRequestRecord> ListPending()
    {
        lock (_lock)
            return _items.Where(i => i.Status == InviteRequestStatus.Pending)
                .OrderBy(i => i.CreatedAtUtc)
                .Select(Clone)
                .ToList();
    }

    public IReadOnlyList<InviteRequestRecord> ListAll(int take = 50)
    {
        lock (_lock)
            return _items.OrderByDescending(i => i.CreatedAtUtc).Take(take).Select(Clone).ToList();
    }

    public InviteRequestRecord? GetLatestFor(Guid accountId)
    {
        lock (_lock)
            return _items.Where(i => i.AccountId == accountId)
                .OrderByDescending(i => i.CreatedAtUtc)
                .Select(Clone)
                .FirstOrDefault();
    }

    public InviteRequestRecord Create(Guid accountId, string username, string displayName, int days = 1)
    {
        lock (_lock)
        {
            if (_items.Any(i => i.AccountId == accountId && i.Status == InviteRequestStatus.Pending))
                throw new InvalidOperationException("InviteRequestPending");

            var item = new InviteRequestRecord
            {
                Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant(),
                AccountId = accountId,
                Username = username,
                DisplayName = displayName,
                RequestedDays = days < 1 ? 1 : days,
                Status = InviteRequestStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
            _items.Add(item);
            Persist();
            return Clone(item);
        }
    }

    public InviteRequestRecord Approve(string id, Guid adminId, string inviteCode)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => i.Id == id)
                       ?? throw new InvalidOperationException("InviteRequestNotFound");
            if (item.Status != InviteRequestStatus.Pending)
                throw new InvalidOperationException("InviteRequestResolved");

            item.Status = InviteRequestStatus.Approved;
            item.ResolvedAtUtc = DateTime.UtcNow;
            item.ResolvedByAccountId = adminId;
            item.GrantedInviteCode = inviteCode;
            Persist();
            return Clone(item);
        }
    }

    public InviteRequestRecord Reject(string id, Guid adminId)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => i.Id == id)
                       ?? throw new InvalidOperationException("InviteRequestNotFound");
            if (item.Status != InviteRequestStatus.Pending)
                throw new InvalidOperationException("InviteRequestResolved");

            item.Status = InviteRequestStatus.Rejected;
            item.ResolvedAtUtc = DateTime.UtcNow;
            item.ResolvedByAccountId = adminId;
            Persist();
            return Clone(item);
        }
    }

    private void Persist()
    {
        using var db = _dbFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();
        db.InviteRequests.ExecuteDelete();
        db.InviteRequests.AddRange(_items.Select(Clone));
        db.SaveChanges();
        tx.Commit();
    }

    private static InviteRequestRecord Clone(InviteRequestRecord i) => new()
    {
        Id = i.Id,
        AccountId = i.AccountId,
        Username = i.Username,
        DisplayName = i.DisplayName,
        RequestedDays = i.RequestedDays,
        Status = i.Status,
        CreatedAtUtc = i.CreatedAtUtc,
        ResolvedAtUtc = i.ResolvedAtUtc,
        ResolvedByAccountId = i.ResolvedByAccountId,
        GrantedInviteCode = i.GrantedInviteCode
    };
}
