using System.Text.Json;
using System.Text.Json.Serialization;
using WebGwent.Game.Catalog;
using WebGwent.Game.Models;

namespace WebGwent.Api.Services;

public sealed class CustomContentStore
{
    private readonly object _lock = new();
    private readonly string _path;
    private readonly CardCatalog _catalog;
    private CustomContentDocument _doc = new();

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public CustomContentStore(CardCatalog catalog, string path)
    {
        _catalog = catalog;
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Load();
        Apply();
    }

    public CustomContentDocument Snapshot()
    {
        lock (_lock)
        {
            return new CustomContentDocument
            {
                Factions = _doc.Factions.Select(f => new CustomFactionRecord
                {
                    Slot = f.Slot,
                    NameEn = f.NameEn,
                    NameRu = f.NameRu,
                    Description = f.Description
                }).ToList(),
                Cards = _doc.Cards.Select(c => new CustomCardRecord
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type,
                    Faction = c.Faction,
                    Strength = c.Strength,
                    Row = c.Row,
                    Abilities = c.Abilities.ToList(),
                    LeaderAbility = c.LeaderAbility,
                    MusterGroup = c.MusterGroup,
                    Description = c.Description
                }).ToList()
            };
        }
    }

    public void ReplaceAll(CustomContentDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        lock (_lock)
        {
            Validate(doc);
            _doc = doc;
            SaveUnlocked();
            ApplyUnlocked();
        }
    }

    private void Validate(CustomContentDocument doc)
    {
        var slots = doc.Factions.Select(f => f.Slot).ToList();
        if (slots.Any(s => !CustomFactionSlots.IsCustom(s)))
            throw new InvalidOperationException("InvalidFactionSlot");
        if (slots.Count != slots.Distinct().Count())
            throw new InvalidOperationException("DuplicateFactionSlot");
        if (slots.Count > 5)
            throw new InvalidOperationException("TooManyFactions");

        var ids = doc.Cards.Select(c => c.Id.Trim()).Where(id => id.Length > 0).ToList();
        if (ids.Count != ids.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidOperationException("DuplicateCardId");

        foreach (var c in doc.Cards)
        {
            if (string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Name))
                throw new InvalidOperationException("CardIncomplete");
            // Ensure abilities parse
            _ = c.ToDefinition();
        }
    }

    private void Load()
    {
        if (!File.Exists(_path))
        {
            _doc = new CustomContentDocument();
            return;
        }
        try
        {
            var json = File.ReadAllText(_path);
            _doc = JsonSerializer.Deserialize<CustomContentDocument>(json, Json) ?? new CustomContentDocument();
        }
        catch
        {
            _doc = new CustomContentDocument();
        }
    }

    private void SaveUnlocked()
    {
        var json = JsonSerializer.Serialize(_doc, Json);
        File.WriteAllText(_path, json);
    }

    private void Apply()
    {
        lock (_lock) ApplyUnlocked();
    }

    private void ApplyUnlocked()
    {
        var cards = _doc.Cards.Select(c => c.ToDefinition()).ToList();
        _catalog.ApplyCustomOverlay(cards);
    }
}
