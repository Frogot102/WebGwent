using WebGwent.Game.Models;

namespace WebGwent.Server.Accounts;

public sealed class PlayerAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int Rating { get; set; } = 1000;
    public Faction FavoriteFaction { get; set; } = Faction.NorthernRealms;
    public string AvatarId { get; set; } = "wolf";
    /// <summary>Cache-buster for custom avatar image URL.</summary>
    public long AvatarVersion { get; set; }
    public int MatchesVsPlayers { get; set; }
    public int WinsVsPlayers { get; set; }
    public int LossesVsPlayers { get; set; }
    public int DrawsVsPlayers { get; set; }
    public int MatchesVsBot { get; set; }
    public int WinsVsBot { get; set; }
    public int LossesVsBot { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MatchHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime PlayedAtUtc { get; set; } = DateTime.UtcNow;
    public string Mode { get; set; } = "Bot"; // Bot | Online
    public Guid? PlayerAccountId { get; set; }
    public Guid? OpponentAccountId { get; set; }
    public string PlayerName { get; set; } = "";
    public string OpponentName { get; set; } = "";
    public Faction PlayerFaction { get; set; }
    public Faction OpponentFaction { get; set; }
    public string Result { get; set; } = "Draw"; // Win | Loss | Draw
    public int PlayerRoundWins { get; set; }
    public int OpponentRoundWins { get; set; }
    public int RatingBefore { get; set; }
    public int RatingDelta { get; set; }
    public Guid MatchId { get; set; }
}

public static class AvatarCatalog
{
    public const string CustomId = "custom";
    public const long MaxUploadBytes = 512 * 1024;

    public static readonly IReadOnlyList<(string Id, string LabelRu, string LabelEn, string Glyph)> All =
    [
        ("wolf", "Волк", "Wolf", "🐺"),
        ("griffin", "Грифон", "Griffin", "🦅"),
        ("sun", "Солнце", "Sun", "☀"),
        ("lily", "Лилия", "Lily", "⚜"),
        ("skull", "Череп", "Skull", "☠"),
        ("ship", "Корабль", "Ship", "⛵"),
        ("sword", "Меч", "Sword", "⚔"),
        ("crown", "Корона", "Crown", "♛"),
    ];

    public static bool IsCustom(string? id) =>
        CustomId.Equals(id, StringComparison.OrdinalIgnoreCase);

    public static bool IsPreset(string? id) =>
        !string.IsNullOrEmpty(id) && All.Any(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static string Glyph(string? id) =>
        All.FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).Glyph is { Length: > 0 } g
            ? g
            : "🐺";

    public static string ImageUrl(Guid accountId, long version = 0) =>
        version > 0 ? $"/api/avatars/{accountId}?v={version}" : $"/api/avatars/{accountId}";
}
