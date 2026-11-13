using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WebGwent.Api.Accounts;
using WebGwent.Game.Services;

namespace WebGwent.Api.Data;

/// <summary>Одноразовый перенос App_Data/*.json → PostgreSQL.</summary>
public static class JsonToPostgresImporter
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    public static string ImportIfNeeded(WebGwentDbContext db, string appDataDir, bool force = false)
    {
        Directory.CreateDirectory(appDataDir);
        db.Database.EnsureCreated();

        var hasAccounts = db.Accounts.Any();
        if (hasAccounts && !force)
            return "skip: accounts already present";

        if (force)
        {
            db.Decks.ExecuteDelete();
            db.InviteRequests.ExecuteDelete();
            db.Invites.ExecuteDelete();
            db.Matches.ExecuteDelete();
            db.Accounts.ExecuteDelete();
            db.SaveChanges();
        }

        var accountsPath = Path.Combine(appDataDir, "accounts.json");
        var matchesPath = Path.Combine(appDataDir, "matches.json");
        var invitesPath = Path.Combine(appDataDir, "invites.json");
        var requestsPath = Path.Combine(appDataDir, "invite-requests.json");
        var decksPath = Path.Combine(appDataDir, "custom-decks.json");

        var accounts = ReadList<PlayerAccount>(accountsPath);
        var matches = ReadList<MatchHistoryEntry>(matchesPath);
        var invites = ReadList<InviteRecord>(invitesPath);
        var requests = ReadList<InviteRequestRecord>(requestsPath);
        var decks = ReadList<SavedDeck>(decksPath)
            .Where(d => !d.IsPreset)
            .ToList();

        foreach (var a in accounts)
        {
            // computed JSON props must not confuse tracking
            db.Accounts.Add(new PlayerAccount
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
                CreatedAtUtc = NormalizeUtc(a.CreatedAtUtc),
                LastLoginUtc = NormalizeUtc(a.LastLoginUtc),
                Role = a.Role,
                PlusUntilUtc = a.PlusUntilUtc is DateTime p ? NormalizeUtc(p) : null
            });
        }

        foreach (var m in matches)
        {
            db.Matches.Add(new MatchHistoryEntry
            {
                Id = m.Id,
                PlayedAtUtc = NormalizeUtc(m.PlayedAtUtc),
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
                BotDifficulty = m.BotDifficulty,
                RoundScores = m.RoundScores ?? []
            });
        }

        foreach (var i in invites)
        {
            db.Invites.Add(new InviteRecord
            {
                Id = string.IsNullOrWhiteSpace(i.Id) ? Guid.NewGuid().ToString("N")[..16] : i.Id,
                Code = i.Code,
                DurationDays = i.DurationDays,
                MaxActivations = i.MaxActivations,
                UsedCount = i.UsedCount,
                CreatedByAccountId = i.CreatedByAccountId,
                CreatedAtUtc = NormalizeUtc(i.CreatedAtUtc),
                ExpiresAtUtc = i.ExpiresAtUtc is DateTime e ? NormalizeUtc(e) : null,
                Revoked = i.Revoked,
                Redemptions = i.Redemptions ?? []
            });
        }

        foreach (var r in requests)
        {
            db.InviteRequests.Add(new InviteRequestRecord
            {
                Id = string.IsNullOrWhiteSpace(r.Id) ? Guid.NewGuid().ToString("N")[..16] : r.Id,
                AccountId = r.AccountId,
                Username = r.Username,
                DisplayName = r.DisplayName,
                RequestedDays = r.RequestedDays,
                Status = r.Status,
                CreatedAtUtc = NormalizeUtc(r.CreatedAtUtc),
                ResolvedAtUtc = r.ResolvedAtUtc is DateTime x ? NormalizeUtc(x) : null,
                ResolvedByAccountId = r.ResolvedByAccountId,
                GrantedInviteCode = r.GrantedInviteCode
            });
        }

        foreach (var d in decks)
        {
            db.Decks.Add(new SavedDeck
            {
                Id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id,
                Name = d.Name,
                Faction = d.Faction,
                LeaderId = d.LeaderId,
                CardIds = d.CardIds ?? [],
                IsPreset = false,
                OwnerAccountId = d.OwnerAccountId,
                UpdatedUtc = NormalizeUtc(d.UpdatedUtc)
            });
        }

        db.SaveChanges();
        return $"imported accounts={accounts.Count}, matches={matches.Count}, invites={invites.Count}, requests={requests.Count}, decks={decks.Count}";
    }

    private static List<T> ReadList<T>(string path)
    {
        if (!File.Exists(path)) return [];
        try
        {
            var raw = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<T>>(raw, Json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static DateTime NormalizeUtc(DateTime dt) =>
        dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };
}
