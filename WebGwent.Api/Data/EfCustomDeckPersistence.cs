using Microsoft.EntityFrameworkCore;
using WebGwent.Game.Services;

namespace WebGwent.Api.Data;

public sealed class EfCustomDeckPersistence(IDbContextFactory<WebGwentDbContext> dbFactory) : ICustomDeckPersistence
{
    public List<SavedDeck> Load()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Decks.AsNoTracking().ToList();
    }

    public void SaveAll(IReadOnlyList<SavedDeck> decks)
    {
        using var db = dbFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();
        db.Decks.ExecuteDelete();
        foreach (var d in decks.Where(x => !x.IsPreset))
        {
            db.Decks.Add(new SavedDeck
            {
                Id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id,
                Name = d.Name,
                Faction = d.Faction,
                LeaderId = d.LeaderId,
                CardIds = [.. d.CardIds],
                IsPreset = false,
                OwnerAccountId = d.OwnerAccountId,
                UpdatedUtc = d.UpdatedUtc
            });
        }
        db.SaveChanges();
        tx.Commit();
    }
}
