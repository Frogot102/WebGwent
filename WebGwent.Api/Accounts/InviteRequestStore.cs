using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private readonly string _path;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    private List<InviteRequestRecord> _items = [];

    public InviteRequestStore(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "invite-requests.json");
        Reload();
    }

    public void Reload()
    {
        lock (_lock)
        {
            if (!File.Exists(_path))
            {
                _items = [];
                return;
            }
            try
            {
                _items = JsonSerializer.Deserialize<List<InviteRequestRecord>>(File.ReadAllText(_path), _json) ?? [];
            }
            catch
            {
                _items = [];
            }
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
        var tmp = _path + "." + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)) + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_items, _json));
        File.Move(tmp, _path, overwrite: true);
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
