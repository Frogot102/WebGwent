using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;
using WebGwent.Game.Services;

namespace WebGwent.Server.Services;

/// <summary>Client-side deck helpers (catalog + validation). Persistence goes through the API.</summary>
public sealed class DeckUiService(CardCatalog catalog)
{
    private readonly DeckLibrary _helpers = new(catalog, Path.Combine(Path.GetTempPath(), "webgwent-ui-noop-decks.json"));

    public CardCatalog Catalog => catalog;

    public IReadOnlyList<SavedDeck> Presets() => _helpers.GetPresets();

    public DeckValidationResult Validate(SavedDeck deck) => _helpers.Validate(deck);

    public int RemainingCopies(Faction faction, string cardName, IReadOnlyList<string> currentIds) =>
        _helpers.RemainingCopies(faction, cardName, currentIds);

    public string? NextAvailableCardId(Faction faction, string cardName, IReadOnlyList<string> currentIds) =>
        _helpers.NextAvailableCardId(faction, cardName, currentIds);

    public static SavedDeck FromDto(DeckDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Faction = d.Faction,
        LeaderId = d.LeaderId,
        CardIds = d.CardIds.ToList(),
        IsPreset = d.IsPreset
    };
}
