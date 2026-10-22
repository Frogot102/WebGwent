using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Game.Bot;

/// <summary>
/// A fast, simulation-free player used to play matches out to the end when the bot looks ahead.
/// It follows the usual Gwent habits — spies first, pass when the round is won, don't chase a lost
/// opening round — but decides from card strengths alone, so thousands of moves cost almost nothing.
/// </summary>
internal static class RolloutPolicy
{
    /// <summary>Plays the match from <paramref name="match"/> to the end and returns 1 / 0.5 / 0 for <paramref name="me"/>.</summary>
    public static double PlayOut(RulesEngine engine, MatchState match, int me, Random rng)
    {
        for (var guard = 0; guard < 300 && match.Phase != MatchPhase.MatchEnd; guard++)
        {
            if (match.Phase == MatchPhase.RoundEnd)
            {
                engine.ContinueAfterRound(match);
                continue;
            }

            var actor = match.Pending?.PlayerIndex ?? match.ActivePlayer;
            var lastLog = match.Log.LastOrDefault();
            try
            {
                Act(engine, match, actor, rng);
            }
            catch (InvalidOperationException)
            {
                // fall through to the forced pass below
            }

            if (ReferenceEquals(lastLog, match.Log.LastOrDefault()) && match.Phase == MatchPhase.Playing)
            {
                if (match.Pending is not null)
                    ResolvePending(engine, match, match.Pending.PlayerIndex);
                else
                    engine.Pass(match, match.ActivePlayer);
            }
        }

        return match.Winner == me ? 1 : match.Winner is null ? 0.5 : 0;
    }

    private static void Act(RulesEngine engine, MatchState m, int me, Random rng)
    {
        if (m.Pending?.PlayerIndex == me)
        {
            ResolvePending(engine, m, me);
            return;
        }

        if (m.Phase != MatchPhase.Playing || m.ActivePlayer != me)
            return;

        var p = m.Players[me];
        var o = m.Players[1 - me];
        var diff = ScoreCalculator.GetTotalScore(p, m) - ScoreCalculator.GetTotalScore(o, m);
        var winsTies = p.Faction == Faction.Nilfgaard && o.Faction != Faction.Nilfgaard;
        var ahead = diff > 0 || (diff == 0 && winsTies);
        var decisive = p.Lives == 1 || o.Lives == 1;

        var units = p.Hand.Where(c => c.Type == CardType.Unit).ToList();
        if (units.Count == 0 && !p.Hand.Any(c => UsefulSpecial(m, p, c)))
        {
            engine.Pass(m, me);
            return;
        }

        if (o.HasPassed)
        {
            if (ahead)
            {
                engine.Pass(m, me);
                return;
            }

            // Cheapest single card that wins, else commit only if the round matters.
            var winner = units
                .Where(c => !c.Abilities.Contains(Ability.Spy) && Gain(m, p, c) > -diff - (winsTies ? 1 : 0))
                .MinBy(c => Gain(m, p, c));
            if (winner is not null)
            {
                Play(engine, m, me, winner);
                return;
            }

            if (decisive || p.Hand.Count > o.Hand.Count + 2)
            {
                PlayStrongest(engine, m, me);
                return;
            }

            engine.Pass(m, me);
            return;
        }

        var spy = units.Where(c => c.Abilities.Contains(Ability.Spy)).MinBy(c => c.BaseStrength);
        if (spy is not null && (!decisive || p.Hand.Count <= o.Hand.Count))
        {
            Play(engine, m, me, spy);
            return;
        }

        var spent = p.HandAtRoundStart - p.Hand.Count;
        var oppSpent = o.HandAtRoundStart - o.Hand.Count;

        if (!decisive)
        {
            if (ahead && (spent >= oppSpent || diff >= 10) && spent + oppSpent > 0)
            {
                engine.Pass(m, me);
                return;
            }

            if (!ahead && spent >= 2 && p.Hand.Count >= o.Hand.Count)
            {
                engine.Pass(m, me);
                return;
            }

            PlayMedium(engine, m, me, rng);
            return;
        }

        if (p.Lives == 2 && o.Lives == 1)
        {
            // A round up: let them spend.
            if (ahead || p.Hand.Count > o.Hand.Count)
            {
                engine.Pass(m, me);
                return;
            }
        }
        else if (ahead && o.Hand.Count == 0)
        {
            engine.Pass(m, me);
            return;
        }

        PlayMedium(engine, m, me, rng);
    }

    /// <summary>Rough strength a unit adds when placed: weather and horn on its row, nothing else.</summary>
    private static int Gain(MatchState m, PlayerState p, CardInstance c)
    {
        if (c.Abilities.Contains(Ability.Spy))
            return -c.BaseStrength;
        var row = c.Abilities.Contains(Ability.Agile) ? BestAgileRow(m) : c.PlacedRow;
        var s = c.BaseStrength;
        if (!c.IsHero && ScoreCalculator.GetWeather(m, row) != WeatherEffect.None)
            s = Math.Min(s, 1);
        if (!c.IsHero && p.HornRows.Contains(row))
            s *= 2;
        if (c.Abilities.Contains(Ability.Muster))
            s += p.Deck.Where(x => x.MusterGroup == c.MusterGroup).Sum(x => x.BaseStrength);
        return s;
    }

    private static CombatRow BestAgileRow(MatchState m) =>
        m.MeleeWeather != WeatherEffect.None && m.RangedWeather == WeatherEffect.None ? CombatRow.Ranged : CombatRow.Melee;

    private static bool UsefulSpecial(MatchState m, PlayerState p, CardInstance c)
    {
        if (c.Type != CardType.Special) return false;
        var o = m.Players[1 - p.Index];
        if (c.Abilities.Contains(Ability.CommandersHorn))
            return RulesEngine.BoardRows.Any(r => !p.HornRows.Contains(r) && ScoreCalculator.GetRowScore(p, r, m) >= 8);
        if (c.Abilities.Contains(Ability.Scorch))
        {
            var all = m.Players.SelectMany(x => x.AllBoardCards().Where(u => u.Type == CardType.Unit && !u.IsHero).Select(u => (x, u))).ToList();
            if (all.Count == 0) return false;
            var max = all.Max(t => ScoreCalculator.GetCardStrength(t.u, t.x, m));
            return all.Where(t => ScoreCalculator.GetCardStrength(t.u, t.x, m) == max).Sum(t => t.x.Index == p.Index ? -max : max) > 0;
        }
        if (c.Abilities.Contains(Ability.WeatherClear))
            return RulesEngine.BoardRows.Any(r => ScoreCalculator.GetWeather(m, r) != WeatherEffect.None
                                                 && p.Rows[r].Count(u => !u.IsHero) > o.Rows[r].Count(u => !u.IsHero));
        var weatherRow = c.Abilities.Contains(Ability.WeatherFrost) ? CombatRow.Melee
            : c.Abilities.Contains(Ability.WeatherFog) ? CombatRow.Ranged
            : c.Abilities.Contains(Ability.WeatherRain) ? CombatRow.Siege
            : CombatRow.None;
        if (weatherRow != CombatRow.None)
            return ScoreCalculator.GetWeather(m, weatherRow) == WeatherEffect.None
                   && ScoreCalculator.GetRowScore(o, weatherRow, m) - o.Rows[weatherRow].Count
                   > ScoreCalculator.GetRowScore(p, weatherRow, m) - p.Rows[weatherRow].Count + 4;
        return false;
    }

    private static void PlayStrongest(RulesEngine engine, MatchState m, int me)
    {
        var p = m.Players[me];
        var special = p.Hand.FirstOrDefault(c => UsefulSpecial(m, p, c));
        if (special is not null)
        {
            Play(engine, m, me, special);
            return;
        }

        var unit = p.Hand.Where(c => c.Type == CardType.Unit).MaxBy(c => Gain(m, p, c));
        if (unit is null)
            engine.Pass(m, me);
        else
            Play(engine, m, me, unit);
    }

    private static void PlayMedium(RulesEngine engine, MatchState m, int me, Random rng)
    {
        var p = m.Players[me];
        var special = p.Hand.FirstOrDefault(c => UsefulSpecial(m, p, c));
        if (special is not null && rng.NextDouble() < 0.6)
        {
            Play(engine, m, me, special);
            return;
        }

        var units = p.Hand.Where(c => c.Type == CardType.Unit && !c.Abilities.Contains(Ability.Spy))
            .OrderBy(c => Gain(m, p, c))
            .ToList();
        if (units.Count == 0)
        {
            PlayStrongest(engine, m, me);
            return;
        }

        // Musters and bonds first, otherwise something from the middle of the hand.
        var synergy = units.FirstOrDefault(c => c.Abilities.Contains(Ability.Muster)
                                                || (c.Abilities.Contains(Ability.TightBond)
                                                    && p.AllBoardCards().Any(b => b.Name == c.Name)));
        Play(engine, m, me, synergy ?? units[rng.Next(units.Count / 2, units.Count)]);
    }

    private static void Play(RulesEngine engine, MatchState m, int me, CardInstance card)
    {
        var p = m.Players[me];
        CombatRow? row = null;
        Guid? target = null;
        if (card.Abilities.Contains(Ability.Agile))
            row = BestAgileRow(m);
        else if (card.Abilities.Contains(Ability.CommandersHorn) && card.Type == CardType.Special)
            row = RulesEngine.BoardRows.Where(r => !p.HornRows.Contains(r)).MaxBy(r => ScoreCalculator.GetRowScore(p, r, m));
        else if (card.Abilities.Contains(Ability.Mardroeme))
            row = RulesEngine.BoardRows.Where(r => !p.MardroemeRows.Contains(r))
                .MaxBy(r => p.Rows[r].Count(c => c.Abilities.Contains(Ability.Berserker)));
        else if (card.Abilities.Contains(Ability.Decoy))
        {
            var spy = p.AllBoardCards().FirstOrDefault(c => c.Abilities.Contains(Ability.Spy) && !c.IsHero);
            if (spy is null)
            {
                engine.Pass(m, me);
                return;
            }
            target = spy.InstanceId;
        }

        engine.PlayCard(m, me, card.InstanceId, row, target);
        var guard = 0;
        while (m.Pending?.PlayerIndex == me && guard++ < 6)
            ResolvePending(engine, m, me);
    }

    private static void ResolvePending(RulesEngine engine, MatchState m, int me)
    {
        var pending = m.Pending!;
        var p = m.Players[me];
        switch (pending.Kind)
        {
            case PendingKind.ChooseWhoStarts:
                engine.ChooseWhoStarts(m, me, 1 - me);
                return;
            case PendingKind.ChooseMedicTarget:
            {
                var best = pending.ValidTargets
                    .Select(id => p.Discard.First(c => c.InstanceId == id))
                    .OrderByDescending(c => c.Abilities.Contains(Ability.Spy) ? 100 : c.BaseStrength)
                    .First();
                engine.ResolvePending(m, me, best.InstanceId);
                return;
            }
            case PendingKind.ChooseHandDiscard:
                engine.ResolvePending(m, me, p.Hand.Where(c => pending.ValidTargets.Contains(c.InstanceId))
                    .MinBy(c => c.BaseStrength)!.InstanceId);
                return;
            case PendingKind.ChooseDeckCard:
            case PendingKind.ChooseOwnDiscardCard:
            case PendingKind.ChooseEnemyDiscardCard:
                engine.ResolvePending(m, me, pending.ValidTargets
                    .Select(id => RulesEngine.FindCardAnywhere(m, id)!)
                    .MaxBy(c => c.BaseStrength)!.InstanceId);
                return;
        }

        if (pending.ValidRows.Count > 0)
            engine.ResolvePending(m, me, row: pending.ValidRows[0]);
        else if (pending.CanSkip)
            engine.ResolvePending(m, me);
        else
            engine.ResolvePending(m, me, pending.ValidTargets[0]);
    }
}
