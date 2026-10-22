using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebGwent.Server.Accounts;

/// <summary>
/// Invite codes for WebGwent Plus. Pattern taken from WebOnlineGame (normalize, alphabet without O/0/I/1,
/// atomic temp-file write) and extended with max activations and custom duration in days.
/// </summary>
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
    public List<InviteRedemption> Redemptions { get; set; } = [];
}

public sealed class InviteRedemption
{
    public Guid AccountId { get; set; }
    public string Username { get; set; } = "";
    public DateTime RedeemedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class InviteStore
{
    public const int MaxActiveInvites = 50;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly string _path;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    private List<InviteRecord> _invites = [];

    public InviteStore(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "invites.json");
        Reload();
    }

    public void Reload()
    {
        lock (_lock)
        {
            if (!File.Exists(_path))
            {
                _invites = [];
                return;
            }

            try
            {
                var raw = File.ReadAllText(_path);
                _invites = JsonSerializer.Deserialize<List<InviteRecord>>(raw, _json) ?? [];
            }
            catch
            {
                _invites = [];
            }
        }
    }

    public IReadOnlyList<InviteRecord> List()
    {
        lock (_lock)
            return _invites.OrderByDescending(i => i.CreatedAtUtc).Select(Clone).ToList();
    }

    public InviteRecord Create(Guid adminId, int durationDays, int maxActivations, DateTime? expiresAtUtc = null)
    {
        if (durationDays is < 1 or > 3650)
            throw new InvalidOperationException("InviteDuration");
        if (maxActivations is < 1 or > 1000)
            throw new InvalidOperationException("InviteMaxUses");

        lock (_lock)
        {
            var active = _invites.Count(i => !i.Revoked && i.UsedCount < i.MaxActivations
                && (i.ExpiresAtUtc is null || i.ExpiresAtUtc > DateTime.UtcNow));
            if (active >= MaxActiveInvites)
                throw new InvalidOperationException("InviteLimit");

            var code = MakeCode();
            while (_invites.Any(i => Normalize(i.Code) == Normalize(code)))
                code = MakeCode();

            var invite = new InviteRecord
            {
                Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant(),
                Code = code,
                DurationDays = durationDays,
                MaxActivations = maxActivations,
                CreatedByAccountId = adminId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expiresAtUtc
            };
            _invites.Add(invite);
            Persist();
            return Clone(invite);
        }
    }

    public InviteRecord? Delete(string id)
    {
        lock (_lock)
        {
            var invite = _invites.FirstOrDefault(i => i.Id == id);
            if (invite is null) return null;
            if (invite.UsedCount > 0)
            {
                invite.Revoked = true;
                Persist();
                return Clone(invite);
            }

            _invites.Remove(invite);
            Persist();
            return Clone(invite);
        }
    }

    /// <summary>Returns duration days on success; throws with Invite* codes on failure.</summary>
    public int Redeem(string code, Guid accountId, string username)
    {
        var needle = Normalize(code);
        if (needle.Length < 8)
            throw new InvalidOperationException("InviteInvalid");

        lock (_lock)
        {
            var invite = _invites.FirstOrDefault(i => Normalize(i.Code) == needle)
                         ?? throw new InvalidOperationException("InviteInvalid");

            if (invite.Revoked)
                throw new InvalidOperationException("InviteInvalid");
            if (invite.ExpiresAtUtc is DateTime exp && exp < DateTime.UtcNow)
                throw new InvalidOperationException("InviteExpired");
            if (invite.UsedCount >= invite.MaxActivations)
                throw new InvalidOperationException("InviteExhausted");
            if (invite.Redemptions.Any(r => r.AccountId == accountId))
                throw new InvalidOperationException("InviteAlreadyUsed");

            invite.UsedCount++;
            invite.Redemptions.Add(new InviteRedemption
            {
                AccountId = accountId,
                Username = username,
                RedeemedAtUtc = DateTime.UtcNow
            });
            Persist();
            return invite.DurationDays;
        }
    }

    private void Persist()
    {
        var tmp = _path + "." + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)) + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_invites, _json));
        File.Move(tmp, _path, overwrite: true);
    }

    private static string Normalize(string code) =>
        new string(code.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string MakeCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(8);
        var body = new char[8];
        for (var i = 0; i < 8; i++)
            body[i] = Alphabet[bytes[i] % Alphabet.Length];
        return $"WGNT-{new string(body[..4])}-{new string(body[4..])}";
    }

    private static InviteRecord Clone(InviteRecord i) => new()
    {
        Id = i.Id,
        Code = i.Code,
        DurationDays = i.DurationDays,
        MaxActivations = i.MaxActivations,
        UsedCount = i.UsedCount,
        CreatedByAccountId = i.CreatedByAccountId,
        CreatedAtUtc = i.CreatedAtUtc,
        ExpiresAtUtc = i.ExpiresAtUtc,
        Revoked = i.Revoked,
        Redemptions = i.Redemptions.Select(r => new InviteRedemption
        {
            AccountId = r.AccountId,
            Username = r.Username,
            RedeemedAtUtc = r.RedeemedAtUtc
        }).ToList()
    };
}
