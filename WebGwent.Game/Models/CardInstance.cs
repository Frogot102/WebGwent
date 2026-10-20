namespace WebGwent.Game.Models;

public sealed class CardInstance
{
    public Guid InstanceId { get; init; } = Guid.NewGuid();
    public required string DefinitionId { get; init; }
    public int BaseStrength { get; set; }
    public CombatRow PlacedRow { get; set; } = CombatRow.None;
    public bool IsHero { get; set; }
    public IReadOnlyList<Ability> Abilities { get; set; } = [];
    public string? MusterGroup { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Faction Faction { get; set; }
    public CardType Type { get; set; }
    public bool Transformed { get; set; }

    public static CardInstance FromDefinition(CardDefinition def)
    {
        return new CardInstance
        {
            DefinitionId = def.Id,
            BaseStrength = def.Strength,
            IsHero = def.IsHero,
            Abilities = def.Abilities.ToList(),
            MusterGroup = def.MusterGroup,
            Name = def.Name,
            Description = def.Description ?? "",
            Faction = def.Faction,
            Type = def.Type,
            PlacedRow = def.Row
        };
    }

    public CardInstance Clone()
    {
        return new CardInstance
        {
            InstanceId = InstanceId,
            DefinitionId = DefinitionId,
            BaseStrength = BaseStrength,
            PlacedRow = PlacedRow,
            IsHero = IsHero,
            Abilities = Abilities.ToList(),
            MusterGroup = MusterGroup,
            Name = Name,
            Description = Description,
            Faction = Faction,
            Type = Type,
            Transformed = Transformed
        };
    }
}
