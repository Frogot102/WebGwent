using System.Text.Json;
using System.Text.Json.Serialization;
using WebGwent.Game.Models;

namespace WebGwent.Api.Accounts;

public sealed class AccountStore
{
    private readonly string _dir;
    private readonly string _avatarsDir;
    private readonly string _accountsPath;
    private readonly string _matchesPath;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private List<PlayerAccount> _accounts = [];
    private List<MatchHistoryEntry> _matches = [];

    public AccountStore(IWebHostEnvironment env, IConfiguration config)
    {
        _dir = Path.Combine(env.ContentRootPath, "App_Data");
        _avatarsDir = Path.Combine(_dir, "avatars");
        Directory.CreateDirectory(_dir);
        Directory.CreateDirectory(_avatarsDir);
        _accountsPath = Path.Combine(_dir, "accounts.json");
        _matchesPath = Path.Combine(_dir, "matches.json");
        Reload();
        EnsureSeedAdmin(config);
    }

    /// <summary>
    /// Ensures the built-in admin account exists with Admin role and the known coursework password.
    /// Config SeedAdmin can override username/password/display; defaults come from <see cref="BuiltInAdmin"/>.
    /// </summary>
    private void EnsureSeedAdmin(IConfiguration config)
    {
        lock (_lock)
        {
            var section = config.GetSection("SeedAdmin");
            var username = string.IsNullOrWhiteSpace(section["Username"])
                ? BuiltInAdmin.Username
                : section["Username"]!.Trim();
            var password = string.IsNullOrWhiteSpace(section["Password"])
                ? BuiltInAdmin.Password
                : section["Password"]!;
            var display = string.IsNullOrWhiteSpace(section["DisplayName"])
                ? BuiltInAdmin.DisplayName
                : section["DisplayName"]!.Trim();

            var (hash, salt) = PasswordHasher.Hash(password);
            var existing = _accounts.FirstOrDefault(a =>
                a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.Role = AccountRole.Admin;
                existing.DisplayName = display;
                existing.PasswordHash = hash;
                existing.PasswordSalt = salt;
                PersistAccounts();
                return;
            }

            _accounts.Add(new PlayerAccount
            {
                Username = username,
                DisplayName = display,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = AccountRole.Admin
            });
            PersistAccounts();
        }
    }

    public bool HasPlus(Guid accountId)
    {
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a => a.Id == accountId);
            return acc is not null && AccHasPlus(acc);
        }
    }

    private static bool AccHasPlus(PlayerAccount acc) =>
        acc.Role == AccountRole.Admin
        || (acc.PlusUntilUtc is DateTime until && until > DateTime.UtcNow);

    public PlayerAccount GrantPlusDays(Guid accountId, int days)
    {
        if (days < 1) throw new InvalidOperationException("PlusDays");
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a => a.Id == accountId)
                      ?? throw new InvalidOperationException("NotFound");
            var now = DateTime.UtcNow;
            var baseTime = acc.PlusUntilUtc is DateTime cur && cur > now ? cur : now;
            acc.PlusUntilUtc = baseTime.AddDays(days);
            PersistAccounts();
            return Clone(acc);
        }
    }

    public PlayerAccount PurchasePlusMonths(Guid accountId, int months)
    {
        var safe = months is 3 or 12 ? months : 1;
        return GrantPlusDays(accountId, safe * 30);
    }

    public IReadOnlyList<PlayerAccount> ListUsers()
    {
        lock (_lock)
            return _accounts.OrderBy(a => a.Username).Select(Clone).ToList();
    }

    public PlayerAccount SetRole(Guid actorId, Guid targetId, AccountRole role)
    {
        lock (_lock)
        {
            var actor = _accounts.FirstOrDefault(a => a.Id == actorId)
                        ?? throw new InvalidOperationException("Forbidden");
            if (actor.Role != AccountRole.Admin)
                throw new InvalidOperationException("Forbidden");

            var target = _accounts.FirstOrDefault(a => a.Id == targetId)
                         ?? throw new InvalidOperationException("NotFound");
            if (target.Id == actor.Id)
                throw new InvalidOperationException("Forbidden");

            if (target.Role == AccountRole.Admin && role != AccountRole.Admin)
            {
                var admins = _accounts.Count(a => a.Role == AccountRole.Admin);
                if (admins <= 1)
                    throw new InvalidOperationException("LastAdmin");
            }

            target.Role = role;
            PersistAccounts();
            return Clone(target);
        }
    }

    public void Reload()
    {
        lock (_lock)
        {
            _accounts = ReadList<PlayerAccount>(_accountsPath);
            _matches = ReadList<MatchHistoryEntry>(_matchesPath);
        }
    }

    public PlayerAccount? FindByUsername(string username)
    {
        lock (_lock)
        {
            return _accounts.FirstOrDefault(a =>
                a.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public PlayerAccount? FindById(Guid id)
    {
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a => a.Id == id);
            if (acc is null) return null;
            var before = acc.FavoriteFaction;
            RefreshFavoriteFaction(acc);
            if (acc.FavoriteFaction != before)
                PersistAccounts();
            return Clone(acc);
        }
    }

    public PlayerAccount Register(string username, string password, string displayName)
    {
        username = username.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName.Trim();
        if (username.Length < 3 || username.Length > 24)
            throw new InvalidOperationException("UsernameLength");
        if (password.Length < 4)
            throw new InvalidOperationException("PasswordLength");
        if (!username.All(c => char.IsLetterOrDigit(c) || c is '_' or '-'))
            throw new InvalidOperationException("UsernameChars");

        lock (_lock)
        {
            if (_accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("UsernameTaken");

            var (hash, salt) = PasswordHasher.Hash(password);
            var account = new PlayerAccount
            {
                Username = username,
                DisplayName = displayName,
                PasswordHash = hash,
                PasswordSalt = salt
            };
            _accounts.Add(account);
            PersistAccounts();
            return Clone(account);
        }
    }

    public PlayerAccount? TryLogin(string username, string password)
    {
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a =>
                a.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
            if (acc is null) return null;
            if (!PasswordHasher.Verify(password, acc.PasswordHash, acc.PasswordSalt))
                return null;
            acc.LastLoginUtc = DateTime.UtcNow;
            PersistAccounts();
            return Clone(acc);
        }
    }

    public PlayerAccount UpdateProfile(Guid id, string displayName, string avatarId)
    {
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a => a.Id == id)
                      ?? throw new InvalidOperationException("NotFound");
            displayName = displayName.Trim();
            if (displayName.Length is < 1 or > 24)
                throw new InvalidOperationException("DisplayName");

            if (AvatarCatalog.IsCustom(avatarId))
            {
                if (!File.Exists(AvatarPath(id)))
                    avatarId = "wolf";
            }
            else if (!AvatarCatalog.IsPreset(avatarId))
            {
                avatarId = "wolf";
            }

            acc.DisplayName = displayName;
            acc.AvatarId = avatarId;
            RefreshFavoriteFaction(acc);
            PersistAccounts();
            return Clone(acc);
        }
    }

    public PlayerAccount SaveCustomAvatar(Guid id, Stream data, string contentType)
    {
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a => a.Id == id)
                      ?? throw new InvalidOperationException("NotFound");

            var ext = contentType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => throw new InvalidOperationException("AvatarType")
            };

            // Keep a single file per user; wipe previous extensions.
            foreach (var old in Directory.EnumerateFiles(_avatarsDir, id + ".*"))
                File.Delete(old);

            var path = Path.Combine(_avatarsDir, id + ext);
            using (var fs = File.Create(path))
                data.CopyTo(fs);

            if (new FileInfo(path).Length is 0 or > AvatarCatalog.MaxUploadBytes)
            {
                File.Delete(path);
                throw new InvalidOperationException("AvatarSize");
            }

            acc.AvatarId = AvatarCatalog.CustomId;
            acc.AvatarVersion = DateTime.UtcNow.Ticks;
            PersistAccounts();
            return Clone(acc);
        }
    }

    public (string Path, string ContentType)? FindAvatarFile(Guid id)
    {
        lock (_lock)
        {
            var path = Directory.EnumerateFiles(_avatarsDir, id + ".*").FirstOrDefault();
            if (path is null) return null;
            var contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
            return (path, contentType);
        }
    }

    private string AvatarPath(Guid id) =>
        Directory.EnumerateFiles(_avatarsDir, id + ".*").FirstOrDefault() ?? "";

    /// <summary>How many matches the account played with its current favorite faction.</summary>
    public int FavoriteFactionMatchCount(Guid accountId)
    {
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a => a.Id == accountId);
            if (acc is null) return 0;
            RefreshFavoriteFaction(acc);
            return CountFactionMatches(accountId, acc.FavoriteFaction);
        }
    }

    public IReadOnlyList<MatchHistoryEntry> GetMatchesFor(Guid accountId, int take = 50)
    {
        lock (_lock)
        {
            // One row per match from this player's perspective (online writes two sides).
            return _matches
                .Where(m => m.PlayerAccountId == accountId)
                .OrderByDescending(m => m.PlayedAtUtc)
                .Take(take)
                .Select(CloneMatch)
                .ToList();
        }
    }

    public bool HasMatch(Guid matchId)
    {
        lock (_lock)
            return _matches.Any(m => m.MatchId == matchId);
    }

    public MatchHistoryEntry? GetMatchForPlayer(Guid matchId, Guid accountId)
    {
        lock (_lock)
        {
            var m = _matches.FirstOrDefault(x => x.MatchId == matchId && x.PlayerAccountId == accountId);
            return m is null ? null : CloneMatch(m);
        }
    }

    public void RecordBotMatch(Guid accountId, MatchState match, int viewerIndex)
    {
        lock (_lock)
        {
            if (_matches.Any(m => m.MatchId == match.MatchId))
                return;

            var acc = _accounts.FirstOrDefault(a => a.Id == accountId);
            if (acc is null) return;

            var you = match.Players[viewerIndex];
            var opp = match.Players[1 - viewerIndex];
            var won = match.Winner == viewerIndex;
            var draw = match.Winner is null;

            acc.MatchesVsBot++;
            if (won) acc.WinsVsBot++;
            else if (!draw) acc.LossesVsBot++;
            // Rating only changes in online PvP — bot matches never affect Elo.
            var before = acc.Rating;

            _matches.Add(new MatchHistoryEntry
            {
                MatchId = match.MatchId,
                Mode = "Bot",
                PlayerAccountId = acc.Id,
                OpponentAccountId = null,
                PlayerName = you.Name,
                OpponentName = opp.Name,
                PlayerFaction = you.Faction,
                OpponentFaction = opp.Faction,
                Result = draw ? "Draw" : won ? "Win" : "Loss",
                PlayerRoundWins = you.RoundWins,
                OpponentRoundWins = opp.RoundWins,
                RatingBefore = before,
                RatingDelta = 0,
                BotDifficulty = match.BotDifficulty.ToString()
            });

            RefreshFavoriteFaction(acc);
            PersistAccounts();
            PersistMatches();
        }
    }

    public void RecordOnlineMatch(Guid? hostAccountId, Guid? guestAccountId, MatchState match)
    {
        lock (_lock)
        {
            if (_matches.Any(m => m.MatchId == match.MatchId))
                return;

            void Side(Guid? accountId, int index)
            {
                if (accountId is null) return;
                var acc = _accounts.FirstOrDefault(a => a.Id == accountId.Value);
                if (acc is null) return;

                var you = match.Players[index];
                var opp = match.Players[1 - index];
                var won = match.Winner == index;
                var draw = match.Winner is null;
                var oppAcc = index == 0
                    ? (guestAccountId is Guid g ? _accounts.FirstOrDefault(a => a.Id == g) : null)
                    : (hostAccountId is Guid h ? _accounts.FirstOrDefault(a => a.Id == h) : null);

                var delta = ComputeEloDelta(acc.Rating, oppAcc?.Rating ?? 1000, won, draw);

                acc.MatchesVsPlayers++;
                if (draw) acc.DrawsVsPlayers++;
                else if (won) acc.WinsVsPlayers++;
                else acc.LossesVsPlayers++;

                var before = acc.Rating;
                acc.Rating = Math.Max(100, acc.Rating + delta);

                _matches.Add(new MatchHistoryEntry
                {
                    MatchId = match.MatchId,
                    Mode = "Online",
                    PlayerAccountId = acc.Id,
                    OpponentAccountId = oppAcc?.Id,
                    PlayerName = you.Name,
                    OpponentName = opp.Name,
                    PlayerFaction = you.Faction,
                    OpponentFaction = opp.Faction,
                    Result = draw ? "Draw" : won ? "Win" : "Loss",
                    PlayerRoundWins = you.RoundWins,
                    OpponentRoundWins = opp.RoundWins,
                    RatingBefore = before,
                    RatingDelta = delta
                });

                RefreshFavoriteFaction(acc);
            }

            Side(hostAccountId, 0);
            Side(guestAccountId, 1);
            PersistAccounts();
            PersistMatches();
        }
    }

    private void RefreshFavoriteFaction(PlayerAccount acc)
    {
        var top = _matches
            .Where(m => m.PlayerAccountId == acc.Id && m.PlayerFaction != Faction.Neutral)
            .GroupBy(m => m.PlayerFaction)
            .Select(g => (Faction: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Faction)
            .FirstOrDefault();

        if (top.Count > 0)
            acc.FavoriteFaction = top.Faction;
    }

    private int CountFactionMatches(Guid accountId, Faction faction) =>
        _matches.Count(m => m.PlayerAccountId == accountId && m.PlayerFaction == faction);

    private static int ComputeEloDelta(int rating, int oppRating, bool won, bool draw)
    {
        if (draw) return 0;
        var expected = 1.0 / (1.0 + Math.Pow(10, (oppRating - rating) / 400.0));
        var score = won ? 1.0 : 0.0;
        return (int)Math.Round(32 * (score - expected));
    }

    private List<T> ReadList<T>(string path)
    {
        if (!File.Exists(path)) return [];
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<T>>(json, _json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void PersistAccounts()
    {
        var tmp = _accountsPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_accounts, _json));
        File.Move(tmp, _accountsPath, overwrite: true);
    }

    private void PersistMatches()
    {
        var tmp = _matchesPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_matches, _json));
        File.Move(tmp, _matchesPath, overwrite: true);
    }

    private static PlayerAccount Clone(PlayerAccount a) => new()
    {
        Id = a.Id,
        Username = a.Username,
        DisplayName = a.DisplayName,
        PasswordHash = a.PasswordHash,
        PasswordSalt = a.PasswordSalt,
        Rating = a.Rating,
        FavoriteFaction = a.FavoriteFaction,
        AvatarId = a.AvatarId,
        AvatarVersion = a.AvatarVersion,
        MatchesVsPlayers = a.MatchesVsPlayers,
        WinsVsPlayers = a.WinsVsPlayers,
        LossesVsPlayers = a.LossesVsPlayers,
        DrawsVsPlayers = a.DrawsVsPlayers,
        MatchesVsBot = a.MatchesVsBot,
        WinsVsBot = a.WinsVsBot,
        LossesVsBot = a.LossesVsBot,
        CreatedAtUtc = a.CreatedAtUtc,
        LastLoginUtc = a.LastLoginUtc,
        Role = a.Role,
        PlusUntilUtc = a.PlusUntilUtc
    };

    private static MatchHistoryEntry CloneMatch(MatchHistoryEntry m) => new()
    {
        Id = m.Id,
        PlayedAtUtc = m.PlayedAtUtc,
        Mode = m.Mode,
        PlayerAccountId = m.PlayerAccountId,
        OpponentAccountId = m.OpponentAccountId,
        PlayerName = m.PlayerName,
        OpponentName = m.OpponentName,
        PlayerFaction = m.PlayerFaction,
        OpponentFaction = m.OpponentFaction,
        Result = m.Result,
        PlayerRoundWins = m.PlayerRoundWins,
        OpponentRoundWins = m.OpponentRoundWins,
        RatingBefore = m.RatingBefore,
        RatingDelta = m.RatingDelta,
        MatchId = m.MatchId,
        BotDifficulty = m.BotDifficulty
    };
}
