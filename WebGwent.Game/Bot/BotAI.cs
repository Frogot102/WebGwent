using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Game.Bot;

public sealed class BotAI
{
    private readonly RulesEngine _engine;
    private readonly Random _rng;

    public BotAI(RulesEngine engine, Random? rng = null)
    {
        _engine = engine;
        _rng = rng ?? Random.Shared;
    }

    public void TakeTurn(MatchState match, int botIndex)
    {
        // Pending (incl. Scoia'tael choose-who-starts) must win over Mulligan:
        // after both sides finish mulligan Phase stays Mulligan until the choice is resolved.
        if (match.Pending?.PlayerIndex == botIndex)
        {
            ResolvePending(match, botIndex);
            return;
        }

        if (match.Phase == MatchPhase.Mulligan)
        {
            while (match.Players[botIndex].MulliganLeft > 0 && match.Players[botIndex].Hand.Count > 0)
            {
                var weak = match.Players[botIndex].Hand
                    .Where(c => c.Type == CardType.Unit && !c.Abilities.Contains(Ability.Spy) && c.BaseStrength <= 2)
                    .OrderBy(c => c.BaseStrength)
                    .FirstOrDefault();
                if (weak is null) break;
                _engine.Mulligan(match, botIndex, weak.InstanceId);
            }
            _engine.FinishMulligan(match, botIndex);
            // Scoia'tael bot may receive ChooseWhoStarts in the same step — resolve immediately.
            if (match.Pending?.PlayerIndex == botIndex)
                ResolvePending(match, botIndex);
            return;
        }

        if (match.Phase != MatchPhase.Playing || match.ActivePlayer != botIndex)
            return;

        var bot = match.Players[botIndex];
        if (bot.HasPassed) return;

        var myScore = ScoreCalculator.GetTotalScore(bot, match);
        var oppScore = ScoreCalculator.GetTotalScore(match.Players[1 - botIndex], match);
        var oppPassed = match.Players[1 - botIndex].HasPassed;
        var lead = myScore - oppScore;
        var round = match.RoundNumber;

        if (bot.Hand.Count == 0 || (oppPassed && lead > 0))
        {
            _engine.Pass(match, botIndex);
            return;
        }

        // Bleed less in early rounds: pass while modestly ahead to save power
        if (!oppPassed && round < 3 && lead >= 8 && bot.Hand.Count <= 6 && _rng.NextDouble() < 0.55)
        {
            _engine.Pass(match, botIndex);
            return;
        }

        if (!oppPassed && lead >= 14 && bot.Hand.Count <= 5)
        {
            _engine.Pass(match, botIndex);
            return;
        }

        // Clear weather if it hurts us more than opponent
        var clear = bot.Hand.FirstOrDefault(c => c.Abilities.Contains(Ability.WeatherClear));
        if (clear != null && WeatherHurts(bot, match) && !WeatherHurts(match.Players[1 - botIndex], match))
        {
            TryPlay(match, botIndex, clear);
            return;
        }

        var weather = PickWeather(match, bot);
        if (weather != null && (lead < 0 || oppPassed))
        {
            TryPlay(match, botIndex, weather);
            return;
        }

        // Spies early for card advantage, especially round 1-2
        var spy = bot.Hand.Where(c => c.Abilities.Contains(Ability.Spy))
            .OrderBy(c => c.BaseStrength)
            .FirstOrDefault();
        if (spy != null && round < 3 && bot.Hand.Count <= 9 && (lead <= 6 || bot.Hand.Count <= 5))
        {
            TryPlay(match, botIndex, spy);
            return;
        }

        // Scorch / horn when valuable
        var scorch = bot.Hand.FirstOrDefault(c => c.Abilities.Contains(Ability.Scorch) && c.Type == CardType.Special);
        if (scorch != null && ShouldScorch(match, botIndex))
        {
            TryPlay(match, botIndex, scorch);
            return;
        }

        var horn = bot.Hand.FirstOrDefault(c => c.Abilities.Contains(Ability.CommandersHorn));
        if (horn != null && BestOwnRowCount(bot) >= 3 && lead <= 4)
        {
            TryPlay(match, botIndex, horn);
            return;
        }

        // Score candidate units (prefer muster / bonds when copies available)
        var unit = bot.Hand
            .Where(c => c.Type == CardType.Unit && !c.Abilities.Contains(Ability.Spy))
            .Select(c => (Card: c, Score: UnitPlayScore(c, bot, match, lead, oppPassed)))
            .OrderByDescending(x => x.Score)
            .Select(x => x.Card)
            .FirstOrDefault();

        if (unit != null && (oppPassed || lead < 10 || unit.BaseStrength <= 6 || unit.Abilities.Contains(Ability.Muster)))
        {
            TryPlay(match, botIndex, unit);
            return;
        }

        if (!bot.LeaderUsed && bot.Leader != null && (lead < 0 || oppPassed && lead <= 2))
        {
            try { _engine.UseLeader(match, botIndex); return; }
            catch { /* ignore */ }
        }

        if (unit != null)
        {
            TryPlay(match, botIndex, unit);
            return;
        }

        var special = bot.Hand.FirstOrDefault(c => c.Type == CardType.Special);
        if (special != null)
        {
            TryPlay(match, botIndex, special);
            return;
        }

        _engine.Pass(match, botIndex);
    }

    private static double UnitPlayScore(CardInstance c, PlayerState bot, MatchState match, int lead, bool oppPassed)
    {
        double score = c.BaseStrength;
        if (c.Abilities.Contains(Ability.Muster))
            score += 6 + bot.Hand.Concat(bot.Deck).Count(x => x.MusterGroup == c.MusterGroup && x.InstanceId != c.InstanceId);
        if (c.Abilities.Contains(Ability.TightBond))
            score += 3 * bot.Hand.Count(x => x.Name == c.Name);
        if (c.Abilities.Contains(Ability.MoraleBoost))
            score += 2;
        if (c.Abilities.Contains(Ability.Medic) && bot.Discard.Any(d => d.Type == CardType.Unit && !d.IsHero))
            score += 5;
        if (c.IsHero) score += 1;
        if (!oppPassed && lead > 5) score -= c.BaseStrength * 0.35; // avoid overcommit
        if (oppPassed && lead < 0) score += c.BaseStrength * 0.25; // need points now
        _ = match;
        return score;
    }

    private static bool WeatherHurts(PlayerState p, MatchState match)
    {
        var hurt = 0;
        if (match.MeleeWeather != WeatherEffect.None)
            hurt += p.Rows[CombatRow.Melee].Count(c => !c.IsHero && c.BaseStrength > 1);
        if (match.RangedWeather != WeatherEffect.None)
            hurt += p.Rows[CombatRow.Ranged].Count(c => !c.IsHero && c.BaseStrength > 1);
        if (match.SiegeWeather != WeatherEffect.None)
            hurt += p.Rows[CombatRow.Siege].Count(c => !c.IsHero && c.BaseStrength > 1);
        return hurt >= 2;
    }

    private static int BestOwnRowCount(PlayerState bot) =>
        new[] { CombatRow.Melee, CombatRow.Ranged, CombatRow.Siege }.Max(r => bot.Rows[r].Count);

    private static bool ShouldScorch(MatchState match, int botIndex)
    {
        var all = match.Players.SelectMany(p => p.AllBoardCards().Select(c => (p, c)))
            .Where(x => !x.c.IsHero && x.c.Type == CardType.Unit).ToList();
        if (all.Count == 0) return false;
        var max = all.Max(x => ScoreCalculator.GetCardStrength(x.c, x.p, match));
        if (max < 8) return false;
        var victims = all.Where(x => ScoreCalculator.GetCardStrength(x.c, x.p, match) == max).ToList();
        var enemyLoss = victims.Count(v => v.p.Index != botIndex);
        var selfLoss = victims.Count(v => v.p.Index == botIndex);
        return enemyLoss > selfLoss;
    }

    private void TryPlay(MatchState match, int botIndex, CardInstance card)
    {
        CombatRow? row = null;
        if (card.Abilities.Contains(Ability.Agile))
        {
            var bot = match.Players[botIndex];
            var melee = ScoreCalculator.GetRowScore(bot, CombatRow.Melee, match);
            var ranged = ScoreCalculator.GetRowScore(bot, CombatRow.Ranged, match);
            row = melee <= ranged ? CombatRow.Melee : CombatRow.Ranged;
        }

        try
        {
            _engine.PlayCard(match, botIndex, card.InstanceId, row);
            if (match.Pending?.PlayerIndex == botIndex)
                ResolvePending(match, botIndex);
        }
        catch
        {
            if (!match.Players[botIndex].HasPassed && match.Pending is null)
                _engine.Pass(match, botIndex);
        }
    }

    private void ResolvePending(MatchState match, int botIndex)
    {
        var pending = match.Pending!;
        switch (pending.Kind)
        {
            case PendingKind.ChooseAgileRow:
                _engine.ResolvePending(match, botIndex, row: CombatRow.Melee);
                break;
            case PendingKind.ChooseMedicTarget:
                // Revive strongest discarded unit
                var best = pending.ValidTargets
                    .Select(id => match.Players[botIndex].Discard.FirstOrDefault(c => c.InstanceId == id))
                    .Where(c => c != null)
                    .OrderByDescending(c => c!.BaseStrength)
                    .FirstOrDefault();
                if (best != null)
                    _engine.ResolvePending(match, botIndex, targetId: best.InstanceId);
                else
                    _engine.ResolvePending(match, botIndex);
                break;
            case PendingKind.ChooseDecoyTarget:
                // Pull highest non-hero back to hand
                var decoy = pending.ValidTargets
                    .Select(id => match.Players[botIndex].AllBoardCards().FirstOrDefault(c => c.InstanceId == id))
                    .Where(c => c != null)
                    .OrderByDescending(c => c!.BaseStrength)
                    .FirstOrDefault();
                if (decoy != null)
                    _engine.ResolvePending(match, botIndex, targetId: decoy.InstanceId);
                break;
            case PendingKind.ChooseWhoStarts:
                _engine.ChooseWhoStarts(match, botIndex, botIndex);
                break;
        }
    }

    private static CardInstance? PickWeather(MatchState match, PlayerState bot)
    {
        var opp = match.Players[1 - bot.Index];
        var melee = ScoreCalculator.GetRowScore(opp, CombatRow.Melee, match);
        var ranged = ScoreCalculator.GetRowScore(opp, CombatRow.Ranged, match);
        var siege = ScoreCalculator.GetRowScore(opp, CombatRow.Siege, match);

        if (melee >= 10 && match.MeleeWeather == WeatherEffect.None)
            return bot.Hand.FirstOrDefault(c => c.Abilities.Contains(Ability.WeatherFrost));
        if (ranged >= 10 && match.RangedWeather == WeatherEffect.None)
            return bot.Hand.FirstOrDefault(c => c.Abilities.Contains(Ability.WeatherFog));
        if (siege >= 10 && match.SiegeWeather == WeatherEffect.None)
            return bot.Hand.FirstOrDefault(c => c.Abilities.Contains(Ability.WeatherRain));
        return null;
    }
}
