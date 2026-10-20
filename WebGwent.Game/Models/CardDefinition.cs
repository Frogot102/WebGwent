namespace WebGwent.Game.Models;

public sealed class CardDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public CardType Type { get; init; }
    public Faction Faction { get; init; }
    public int Strength { get; init; }
    public CombatRow Row { get; init; } = CombatRow.None;
    public IReadOnlyList<Ability> Abilities { get; init; } = [];
    public LeaderAbility LeaderAbility { get; init; } = LeaderAbility.None;
    public string? MusterGroup { get; init; }
    public string Description { get; init; } = "";
    public bool IsHero => Abilities.Contains(Ability.Hero);
}
