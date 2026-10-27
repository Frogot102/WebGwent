using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebGwent.Game.Models;

namespace WebGwent.Game.Catalog;

public sealed class CardCatalog
{
    private readonly object _lock = new();
    private readonly List<CardDefinition> _embedded;
    private Dictionary<string, CardDefinition> _byId;

    public CardCatalog(IEnumerable<CardDefinition> cards)
    {
        _embedded = cards.ToList();
        _byId = _embedded.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, CardDefinition> All
    {
        get { lock (_lock) return _byId; }
    }

    public CardDefinition Get(string id)
    {
        lock (_lock)
        {
            return _byId.TryGetValue(id, out var def)
                ? def
                : throw new KeyNotFoundException($"Unknown card '{id}'.");
        }
    }

    public bool TryGet(string id, out CardDefinition? def)
    {
        lock (_lock)
            return _byId.TryGetValue(id, out def);
    }

    public IEnumerable<CardDefinition> ByFaction(Faction faction)
    {
        lock (_lock)
            return _byId.Values.Where(c => c.Faction == faction || c.Faction == Faction.Neutral).ToList();
    }

    /// <summary>Replace overlay custom cards; embedded TW3 set stays as the base.</summary>
    public void ApplyCustomOverlay(IEnumerable<CardDefinition> customCards)
    {
        lock (_lock)
        {
            _byId = _embedded.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var c in customCards)
            {
                if (string.IsNullOrWhiteSpace(c.Id)) continue;
                _byId[c.Id] = c;
            }
        }
    }

    public static CardCatalog LoadEmbedded()
    {
        var asm = typeof(CardCatalog).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("cards.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Embedded cards.json not found.");

        using var stream = asm.GetManifestResourceStream(name)!;
        var dto = JsonSerializer.Deserialize<List<CardDto>>(stream, JsonOptions)
                  ?? throw new InvalidOperationException("Failed to parse cards.json.");

        var cards = dto.Select(Map).ToList();
        return new CardCatalog(cards);
    }

    public static CardCatalog CreateDefault() => LoadEmbedded();

    public static CardDefinition ParseCardDto(
        string id,
        string name,
        CardType type,
        Faction faction,
        int strength,
        CombatRow row,
        IEnumerable<string>? abilities,
        string? leaderAbility,
        string? musterGroup,
        string? description)
    {
        var abs = (abilities ?? [])
            .Select(a => a.Trim())
            .Where(a => a.Length > 0)
            .Select(a => Enum.Parse<Ability>(a, ignoreCase: true))
            .ToList();

        var leader = LeaderAbility.None;
        if (!string.IsNullOrWhiteSpace(leaderAbility))
            leader = Enum.Parse<LeaderAbility>(leaderAbility, ignoreCase: true);

        return new CardDefinition
        {
            Id = id.Trim(),
            Name = name.Trim(),
            Type = type,
            Faction = faction,
            Strength = strength,
            Row = row,
            Abilities = abs,
            LeaderAbility = leader,
            MusterGroup = musterGroup,
            Description = description ?? ""
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static CardDefinition Map(CardDto d)
    {
        var abilities = (d.Abilities ?? [])
            .Select(a => Enum.Parse<Ability>(a, ignoreCase: true))
            .ToList();

        var leaderAbility = LeaderAbility.None;
        if (!string.IsNullOrEmpty(d.LeaderAbility))
            leaderAbility = Enum.Parse<LeaderAbility>(d.LeaderAbility, ignoreCase: true);

        return new CardDefinition
        {
            Id = d.Id,
            Name = d.Name,
            Type = d.Type,
            Faction = d.Faction,
            Strength = d.Strength,
            Row = d.Row,
            Abilities = abilities,
            LeaderAbility = leaderAbility,
            MusterGroup = d.MusterGroup,
            Description = d.Description ?? ""
        };
    }

    private sealed class CardDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public CardType Type { get; set; }
        public Faction Faction { get; set; }
        public int Strength { get; set; }
        public CombatRow Row { get; set; } = CombatRow.None;
        public List<string>? Abilities { get; set; }
        public string? LeaderAbility { get; set; }
        public string? MusterGroup { get; set; }
        public string? Description { get; set; }
    }
}
