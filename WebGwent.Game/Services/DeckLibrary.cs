using System.Text.Json;
using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Game.Services;

public sealed class SavedDeck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "My deck";
    public Faction Faction { get; set; }
    public string LeaderId { get; set; } = "";
    public List<string> CardIds { get; set; } = [];
    public bool IsPreset { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public DeckList ToDeckList() => new(Name, Faction, LeaderId, CardIds);
}

public sealed class DeckLibrary
{
    private readonly object _lock = new();
    private readonly string _path;
    private readonly CardCatalog _catalog;
    private List<SavedDeck> _custom = [];

    public DeckLibrary(CardCatalog catalog, string? storagePath = null)
    {
        _catalog = catalog;
        _path = storagePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebGwent", "custom-decks.json");
        Load();
    }

    public IReadOnlyList<SavedDeck> GetPresets() =>
        PresetDecks.All().Select(p => new SavedDeck
        {
            Id = StablePresetId(p.Faction),
            Name = p.Name,
            Faction = p.Faction,
            LeaderId = p.LeaderId,
            CardIds = p.CardIds.ToList(),
            IsPreset = true
        }).ToList();

    public IReadOnlyList<SavedDeck> GetCustom()
    {
        lock (_lock) return _custom.OrderByDescending(d => d.UpdatedUtc).ToList();
    }

    public IReadOnlyList<SavedDeck> GetAll() =>
        GetPresets().Concat(GetCustom()).ToList();

    public SavedDeck? Get(Guid id)
    {
        var preset = GetPresets().FirstOrDefault(p => p.Id == id);
        if (preset != null) return preset;
        lock (_lock) return _custom.FirstOrDefault(d => d.Id == id);
    }

    public SavedDeck Save(SavedDeck deck)
    {
        if (deck.IsPreset)
            throw new InvalidOperationException("Cannot overwrite presets.");

        var validation = DeckValidator.Validate(deck.ToDeckList(), _catalog);
        // Allow saving invalid drafts, but mark via UpdatedUtc
        deck.UpdatedUtc = DateTime.UtcNow;
        if (deck.Id == Guid.Empty)
            deck.Id = Guid.NewGuid();

        lock (_lock)
        {
            var idx = _custom.FindIndex(d => d.Id == deck.Id);
            if (idx >= 0) _custom[idx] = Clone(deck);
            else _custom.Add(Clone(deck));
            Persist();
        }

        return deck;
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            var removed = _custom.RemoveAll(d => d.Id == id) > 0;
            if (removed) Persist();
            return removed;
        }
    }

    public DeckValidationResult Validate(SavedDeck deck) =>
        DeckValidator.Validate(deck.ToDeckList(), _catalog);

    /// <summary>
    /// How many copies of this card name can still be added.
    /// </summary>
    public int RemainingCopies(Faction faction, string cardName, IReadOnlyList<string> currentIds)
    {
        var pool = _catalog.All.Values
            .Where(c => c.Type is CardType.Unit or CardType.Special)
            .Where(c => c.Faction == faction || c.Faction == Faction.Neutral)
            .Where(c => c.Name == cardName)
            .Select(c => c.Id)
            .ToList();

        var used = currentIds.Count(id =>
        {
            try { return _catalog.Get(id).Name == cardName; }
            catch { return false; }
        });

        var max = pool.FirstOrDefault() is { } anyId && _catalog.Get(anyId).IsHero
            ? 1
            : Math.Min(DeckValidator.MaxSameCard, pool.Count);

        return Math.Max(0, max - used);
    }

    public string? NextAvailableCardId(Faction faction, string cardName, IReadOnlyList<string> currentIds)
    {
        var pool = _catalog.All.Values
            .Where(c => c.Type is CardType.Unit or CardType.Special)
            .Where(c => c.Faction == faction || c.Faction == Faction.Neutral)
            .Where(c => c.Name == cardName)
            .Select(c => c.Id)
            .ToList();

        foreach (var id in pool)
        {
            if (!currentIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                return id;
        }

        // Allow reusing ids up to MaxSameCard for non-heroes if pool exhausted of unique ids
        var sample = pool.FirstOrDefault();
        if (sample is null) return null;
        var def = _catalog.Get(sample);
        if (def.IsHero) return null;
        var count = currentIds.Count(id =>
        {
            try { return _catalog.Get(id).Name == cardName; }
            catch { return false; }
        });
        return count < DeckValidator.MaxSameCard ? sample : null;
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var json = File.ReadAllText(_path);
            var list = JsonSerializer.Deserialize<List<SavedDeck>>(json, JsonOptions);
            if (list != null)
                _custom = list.Where(d => !d.IsPreset).ToList();
        }
        catch
        {
            _custom = [];
        }
    }

    private void Persist()
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_path, JsonSerializer.Serialize(_custom, JsonOptions));
    }

    private static SavedDeck Clone(SavedDeck d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Faction = d.Faction,
        LeaderId = d.LeaderId,
        CardIds = [.. d.CardIds],
        IsPreset = false,
        UpdatedUtc = d.UpdatedUtc
    };

    private static Guid StablePresetId(Faction f) =>
        Guid.Parse($"00000000-0000-0000-0000-00000000000{(int)f}");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}
