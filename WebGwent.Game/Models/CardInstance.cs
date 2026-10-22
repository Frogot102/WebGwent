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
    public LeaderAbility LeaderAbility { get; set; }

    // Printed values: a card that leaves the board (Decoy, transformed Berserker…) returns to them.
    public int DefaultStrength { get; init; }
    public CombatRow DefaultRow { get; init; } = CombatRow.None;
    public string DefaultName { get; init; } = "";
    public IReadOnlyList<Ability> DefaultAbilities { get; init; } = [];

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
            PlacedRow = def.Row,
            LeaderAbility = def.LeaderAbility,
            DefaultStrength = def.Strength,
            DefaultRow = def.Row,
            DefaultName = def.Name,
            DefaultAbilities = def.Abilities.ToList()
        };
    }

    /// <summary>
    /// Restores printed strength, name, abilities and row (used when a card goes to the discard pile or back to hand).
    /// </summary>
    public void ResetToDefault()
    {
        if (string.IsNullOrEmpty(DefaultName))
            return;
        BaseStrength = DefaultStrength;
        PlacedRow = DefaultRow;
        Name = DefaultName;
        Abilities = DefaultAbilities.ToList();
        Transformed = false;
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
            Transformed = Transformed,
            LeaderAbility = LeaderAbility,
            DefaultStrength = DefaultStrength,
            DefaultRow = DefaultRow,
            DefaultName = DefaultName,
            DefaultAbilities = DefaultAbilities
        };
    }
}
