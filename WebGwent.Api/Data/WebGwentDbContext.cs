using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WebGwent.Api.Accounts;
using WebGwent.Game.Services;

namespace WebGwent.Api.Data;

public sealed class WebGwentDbContext(DbContextOptions<WebGwentDbContext> options) : DbContext(options)
{
    public DbSet<PlayerAccount> Accounts => Set<PlayerAccount>();
    public DbSet<MatchHistoryEntry> Matches => Set<MatchHistoryEntry>();
    public DbSet<InviteRecord> Invites => Set<InviteRecord>();
    public DbSet<InviteRequestRecord> InviteRequests => Set<InviteRequestRecord>();
    public DbSet<SavedDeck> Decks => Set<SavedDeck>();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerAccount>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.Ignore(x => x.HasPlus);
            e.Ignore(x => x.ShowAds);
            e.Ignore(x => x.CanPlayOnline);
            e.Property(x => x.Username).HasMaxLength(64);
            e.Property(x => x.DisplayName).HasMaxLength(64);
            e.Property(x => x.FavoriteFaction).HasConversion<string>().HasMaxLength(64);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.AvatarId).HasMaxLength(32);
        });

        modelBuilder.Entity<MatchHistoryEntry>(e =>
        {
            e.ToTable("match_history");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MatchId);
            e.HasIndex(x => x.PlayerAccountId);
            e.Property(x => x.Mode).HasMaxLength(32);
            e.Property(x => x.Result).HasMaxLength(16);
            e.Property(x => x.PlayerFaction).HasConversion<string>().HasMaxLength(64);
            e.Property(x => x.OpponentFaction).HasConversion<string>().HasMaxLength(64);
            e.Property(x => x.RoundScores).HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? new List<RoundScoreEntry>(), JsonOpts),
                    v => JsonSerializer.Deserialize<List<RoundScoreEntry>>(v, JsonOpts) ?? new List<RoundScoreEntry>())
                .Metadata.SetValueComparer(new ValueComparer<List<RoundScoreEntry>>(
                    (a, b) => JsonSerializer.Serialize(a, JsonOpts) == JsonSerializer.Serialize(b, JsonOpts),
                    v => JsonSerializer.Serialize(v, JsonOpts).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<RoundScoreEntry>>(JsonSerializer.Serialize(v, JsonOpts), JsonOpts)
                         ?? new List<RoundScoreEntry>()));

            e.HasOne<PlayerAccount>()
                .WithMany()
                .HasForeignKey(x => x.PlayerAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_match_player");

            e.HasOne<PlayerAccount>()
                .WithMany()
                .HasForeignKey(x => x.OpponentAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_match_opponent");
        });

        modelBuilder.Entity<InviteRecord>(e =>
        {
            e.ToTable("invites");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Redemptions).HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? new List<InviteRedemption>(), JsonOpts),
                    v => JsonSerializer.Deserialize<List<InviteRedemption>>(v, JsonOpts) ?? new List<InviteRedemption>())
                .Metadata.SetValueComparer(new ValueComparer<List<InviteRedemption>>(
                    (a, b) => JsonSerializer.Serialize(a, JsonOpts) == JsonSerializer.Serialize(b, JsonOpts),
                    v => JsonSerializer.Serialize(v, JsonOpts).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<InviteRedemption>>(JsonSerializer.Serialize(v, JsonOpts), JsonOpts)
                         ?? new List<InviteRedemption>()));

            e.HasOne<PlayerAccount>()
                .WithMany()
                .HasForeignKey(x => x.CreatedByAccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_invites_created_by");
        });

        modelBuilder.Entity<InviteRequestRecord>(e =>
        {
            e.ToTable("invite_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Username).HasMaxLength(64);
            e.Property(x => x.DisplayName).HasMaxLength(64);

            e.HasOne<PlayerAccount>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_invite_requests_account");

            e.HasOne<PlayerAccount>()
                .WithMany()
                .HasForeignKey(x => x.ResolvedByAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_invite_requests_resolved_by");
        });

        modelBuilder.Entity<SavedDeck>(e =>
        {
            e.ToTable("decks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OwnerAccountId);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.LeaderId).HasMaxLength(128);
            e.Property(x => x.Faction).HasConversion<string>().HasMaxLength(64);
            e.Ignore(x => x.IsPreset);
            e.Property(x => x.CardIds).HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? new List<string>(), JsonOpts),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOpts) ?? new List<string>())
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => JsonSerializer.Serialize(a, JsonOpts) == JsonSerializer.Serialize(b, JsonOpts),
                    v => JsonSerializer.Serialize(v, JsonOpts).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(v, JsonOpts), JsonOpts)
                         ?? new List<string>()));

            e.HasOne<PlayerAccount>()
                .WithMany()
                .HasForeignKey(x => x.OwnerAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_decks_owner");
        });
    }
}
