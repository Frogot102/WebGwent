using WebGwent.Game.Catalog;
using WebGwent.Game.Models;

namespace WebGwent.Server.Services;

/// <summary>Holds admin-defined faction display names and keeps the UI CardCatalog in sync.</summary>
public sealed class CustomFactionRegistry(CardCatalog catalog)
{
    private readonly object _lock = new();
    private Dictionary<Faction, CustomFactionRecord> _bySlot = new();

    public IReadOnlyList<CustomFactionRecord> Factions
    {
        get { lock (_lock) return _bySlot.Values.OrderBy(f => f.Slot).ToList(); }
    }

    public IReadOnlyList<Faction> PlayableFactions()
    {
        lock (_lock)
        {
            var list = new List<Faction>
            {
                Faction.NorthernRealms, Faction.Nilfgaard, Faction.Scoiatael, Faction.Monsters, Faction.Skellige
            };
            list.AddRange(_bySlot.Keys.OrderBy(f => f));
            return list;
        }
    }

    public string? DisplayName(Faction faction, bool russian)
    {
        lock (_lock)
        {
            if (!_bySlot.TryGetValue(faction, out var rec)) return null;
            var name = russian ? rec.NameRu : rec.NameEn;
            if (string.IsNullOrWhiteSpace(name))
                name = russian ? rec.NameEn : rec.NameRu;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }

    public void Apply(CustomContentDocument doc)
    {
        lock (_lock)
        {
            _bySlot = doc.Factions
                .Where(f => CustomFactionSlots.IsCustom(f.Slot))
                .GroupBy(f => f.Slot)
                .ToDictionary(g => g.Key, g => g.First());
            catalog.ApplyCustomOverlay(doc.Cards.Select(c => c.ToDefinition()));
        }
    }
}
