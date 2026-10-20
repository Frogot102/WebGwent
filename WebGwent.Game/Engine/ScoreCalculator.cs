using WebGwent.Game.Models;

namespace WebGwent.Game.Engine;

public static class ScoreCalculator
{
    public static int GetCardStrength(CardInstance card, PlayerState owner, MatchState match)
    {
        if (card.Type != CardType.Unit)
            return 0;

        var row = card.PlacedRow;
        var weather = GetWeather(match, row);
        var strength = card.BaseStrength;

        if (!card.IsHero && weather != WeatherEffect.None)
            strength = 1;

        if (!card.IsHero)
        {
            var bondCount = owner.Rows[row].Count(c =>
                c.Name == card.Name && c.Abilities.Contains(Ability.TightBond));
            if (card.Abilities.Contains(Ability.TightBond) && bondCount > 1)
                strength *= bondCount;
        }

        if (!card.IsHero)
        {
            var morale = owner.Rows[row].Count(c =>
                c.InstanceId != card.InstanceId && c.Abilities.Contains(Ability.MoraleBoost));
            strength += morale;
        }

        if (!card.IsHero && owner.HornRows.Contains(row))
            strength *= 2;

        return Math.Max(0, strength);
    }

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
