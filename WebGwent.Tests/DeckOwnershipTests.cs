using WebGwent.Game.Catalog;
using WebGwent.Game.Models;
using WebGwent.Game.Services;

namespace WebGwent.Tests;

public class DeckOwnershipTests
{
    [Fact]
    public void Save_And_List_Are_Scoped_To_Owner()
    {
        var path = Path.Combine(Path.GetTempPath(), "webgwent-decks-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var catalog = CardCatalog.LoadEmbedded();
            var lib = new DeckLibrary(catalog, path);
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var preset = PresetDecks.NorthernRealms();

            var deckA = lib.Save(new SavedDeck
            {
                Name = "A",
                Faction = preset.Faction,
                LeaderId = preset.LeaderId,
                CardIds = preset.CardIds.ToList()
            }, a);

            var deckB = lib.Save(new SavedDeck
            {
                Name = "B",
                Faction = preset.Faction,
                LeaderId = preset.LeaderId,
                CardIds = preset.CardIds.ToList()
            }, b);

            var visibleA = lib.GetVisibleTo(a);
            Assert.Contains(visibleA, d => d.Id == deckA.Id);
            Assert.DoesNotContain(visibleA, d => d.Id == deckB.Id);
            Assert.Contains(visibleA, d => d.IsPreset);

            Assert.Throws<UnauthorizedAccessException>(() => lib.Delete(deckB.Id, a));
            Assert.True(lib.Delete(deckA.Id, a));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
