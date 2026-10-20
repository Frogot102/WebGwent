using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebGwent.Game.Models;

namespace WebGwent.Game.Catalog;

public sealed class CardCatalog
{
    private readonly Dictionary<string, CardDefinition> _byId;

    public CardCatalog(IEnumerable<CardDefinition> cards)
    {
        _byId = cards.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, CardDefinition> All => _byId;

    public CardDefinition Get(string id) =>
        _byId.TryGetValue(id, out var def)
            ? def
            : throw new KeyNotFoundException($"Unknown card '{id}'.");

    public IEnumerable<CardDefinition> ByFaction(Faction faction) =>
        _byId.Values.Where(c => c.Faction == faction || c.Faction == Faction.Neutral);

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

        var muster = d.MusterGroup;
        if (d.Type == CardType.Leader && leaderAbility != LeaderAbility.None)
            muster = $"Leader:{leaderAbility}";

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
            MusterGroup = muster,
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
