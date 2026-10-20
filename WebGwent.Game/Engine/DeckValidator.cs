using WebGwent.Game.Catalog;
using WebGwent.Game.Models;

namespace WebGwent.Game.Engine;

public sealed record DeckList(
    string Name,
    Faction Faction,
    string LeaderId,
    IReadOnlyList<string> CardIds);

public sealed class DeckValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
}

public static class DeckValidator
{
    public const int MinUnitCards = 22;
    public const int MaxSpecialCards = 10;
    public const int MaxSameCard = 3; // TW3 allows duplicates for non-unique; heroes unique

    public static DeckValidationResult Validate(DeckList deck, CardCatalog catalog)
    {
        var errors = new List<string>();
        CardDefinition leader;
        try
        {
            leader = catalog.Get(deck.LeaderId);
        }
        catch
        {
            return new DeckValidationResult { Errors = [$"Unknown leader '{deck.LeaderId}'."] };
        }

        if (leader.Type != CardType.Leader)
            errors.Add("LeaderId must point to a leader card.");
        if (leader.Faction != deck.Faction && leader.Faction != Faction.Neutral)
            errors.Add("Leader faction mismatch.");

        var defs = new List<CardDefinition>();
        foreach (var id in deck.CardIds)
        {
            try { defs.Add(catalog.Get(id)); }
            catch { errors.Add($"Unknown card '{id}'."); }
        }

        var units = defs.Where(d => d.Type == CardType.Unit).ToList();
        var specials = defs.Where(d => d.Type == CardType.Special).ToList();

        if (units.Count < MinUnitCards)
            errors.Add($"Need at least {MinUnitCards} unit cards (have {units.Count}).");
        if (specials.Count > MaxSpecialCards)
            errors.Add($"At most {MaxSpecialCards} special cards (have {specials.Count}).");

        foreach (var g in defs.GroupBy(d => d.Id))
        {
            if (g.Key.Contains("hero", StringComparison.OrdinalIgnoreCase) || g.First().IsHero)
            {
                if (g.Count() > 1)
                    errors.Add($"Hero '{g.First().Name}' must be unique.");
            }
            else if (g.Count() > MaxSameCard)
                errors.Add($"Too many copies of '{g.First().Name}'.");
        }

        foreach (var d in defs)
        {
            if (d.Type == CardType.Leader)
                errors.Add("Deck cannot contain extra leaders.");
            if (d.Faction != deck.Faction && d.Faction != Faction.Neutral)
                errors.Add($"'{d.Name}' is not legal for {deck.Faction}.");
        }

        return new DeckValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }
}
