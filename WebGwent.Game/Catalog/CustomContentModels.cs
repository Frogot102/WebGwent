using WebGwent.Game.Models;

namespace WebGwent.Game.Catalog;

public sealed class CustomContentDocument
{
    public List<CustomFactionRecord> Factions { get; set; } = [];
    public List<CustomCardRecord> Cards { get; set; } = [];
}

public sealed class CustomFactionRecord
{
    public Faction Slot { get; set; }
    public string NameEn { get; set; } = "";
    public string NameRu { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class CustomCardRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public CardType Type { get; set; } = CardType.Unit;
    public Faction Faction { get; set; } = Faction.NorthernRealms;
    public int Strength { get; set; }
    public CombatRow Row { get; set; } = CombatRow.Melee;
    public List<string> Abilities { get; set; } = [];
    public string? LeaderAbility { get; set; }
    public string? MusterGroup { get; set; }
    public string Description { get; set; } = "";

    public CardDefinition ToDefinition() =>
        CardCatalog.ParseCardDto(Id, Name, Type, Faction, Strength, Row, Abilities, LeaderAbility, MusterGroup, Description);
}
