using WebGwent.Game.Models;

namespace WebGwent.Game.Engine;

public static class ScoreCalculator
{
    /// <summary>
    /// TW3 order: weather → Tight Bond → Morale Boost → Commander's Horn. Heroes ignore all of it.
    /// </summary>
    public static int GetCardStrength(CardInstance card, PlayerState owner, MatchState match)
    {
        if (card.Type != CardType.Unit)
            return 0;
        if (card.IsHero)
            return card.BaseStrength;

        // Hand / deck / discard keep printed strength — weather, horn, bond and morale
        // only apply once the card is actually on a board row (TW3 behaviour).
        var row = card.PlacedRow;
        if (row is CombatRow.None
            || !owner.Rows.TryGetValue(row, out var rowCards)
            || rowCards.All(c => c.InstanceId != card.InstanceId))
            return card.BaseStrength;

        var strength = card.BaseStrength;
        if (GetWeather(match, row) != WeatherEffect.None)
        {
            // King Bran: units only lose half their strength in bad weather.
            strength = owner.ActiveLeaderAbility == LeaderAbility.KingBran
                ? (strength + 1) / 2
                : Math.Min(strength, 1);
        }

        // Eredin Bréacc Glas: The Treacherous doubles every spy, on both sides.
        if (card.Abilities.Contains(Ability.Spy) && LeaderAbilities.IsActiveAnywhere(match, LeaderAbility.EredinTreacherous))
            strength *= 2;

        if (card.Abilities.Contains(Ability.TightBond))
        {
            var bondCount = rowCards.Count(c => c.Name == card.Name && c.Abilities.Contains(Ability.TightBond));
            if (bondCount > 1)
                strength *= bondCount;
        }

        strength += rowCards.Count(c =>
            c.InstanceId != card.InstanceId && c.Type == CardType.Unit && c.Abilities.Contains(Ability.MoraleBoost));

        if (HasHorn(owner, row, exceptCard: card))
            strength *= 2;

        return Math.Max(0, strength);
    }

    /// <summary>
    /// A row is horned by the Commander's Horn special/leader or by a horn unit (Dandelion) other than the card itself.
    /// </summary>
    public static bool HasHorn(PlayerState owner, CombatRow row, CardInstance? exceptCard = null) =>
        owner.HornRows.Contains(row)
        || (owner.Rows.TryGetValue(row, out var cards)
            && cards.Any(c => c.Type == CardType.Unit
                              && c.Abilities.Contains(Ability.CommandersHorn)
                              && c.InstanceId != exceptCard?.InstanceId));

    public static int GetRowScore(PlayerState player, CombatRow row, MatchState match)
    {
        return player.Rows[row].Sum(c => GetCardStrength(c, player, match));
    }

    public static int GetTotalScore(PlayerState player, MatchState match)
    {
        return GetRowScore(player, CombatRow.Melee, match)
               + GetRowScore(player, CombatRow.Ranged, match)
               + GetRowScore(player, CombatRow.Siege, match);
    }

    public static WeatherEffect GetWeather(MatchState match, CombatRow row) => row switch
    {
        CombatRow.Melee => match.MeleeWeather,
        CombatRow.Ranged => match.RangedWeather,
        CombatRow.Siege => match.SiegeWeather,
        _ => WeatherEffect.None
    };
}
