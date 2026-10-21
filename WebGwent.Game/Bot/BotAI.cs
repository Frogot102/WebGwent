using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Game.Bot;

/// <summary>
/// Gwent opponent. On every turn it:
/// <list type="number">
/// <item>decides what it wants from the current round — win it, win it cheaply or give it up —
/// from the crystals left, the cards in both hands and who has passed;</item>
/// <item>plays every legal move out on a copy of the match and scores it as
/// board swing + value of cards gained − value of cards spent;</item>
/// <item>checks the most promising moves (and passing) by playing the whole match out many times
/// against different possible opponent hands, and keeps the move that wins most often.</item>
/// </list>
/// It never looks at the opponent's hand or at the order of either deck: cards it draws are valued
/// as an average draw, and the opponent's hand is re-dealt at random for every play-out.
/// </summary>
public sealed class BotAI
{
    private const int MaxPendingDepth = 3;

    // Look-ahead budget: ~30 ms per decision. More worlds/candidates barely change the results.
    private const int LookAheadWorlds = 32;
    private const int LookAheadCandidates = 10;
    /// <summary>How much better (in won play-outs) another move must be to overrule the strategy.</summary>
    private const double LookAheadMargin = 0.5;

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
            ResolvePending(match, botIndex, new TurnContext(match, botIndex));
            return;
        }

        if (match.Phase == MatchPhase.Mulligan)
        {
            Mulligan(match, botIndex);
            return;
        }

        if (match.Phase != MatchPhase.Playing || match.ActivePlayer != botIndex)
            return;
        if (match.Players[botIndex].HasPassed)
            return;

        var ctx = new TurnContext(match, botIndex);
        var outcomes = EvaluateMoves(match, botIndex, ctx);
        var move = ChooseMove(match, botIndex, ctx, outcomes);
        move = LookAhead(match, botIndex, ctx, outcomes, move);
        Execute(match, botIndex, ctx, move, outcomes);
    }

    // ───────────────────────────── look-ahead ─────────────────────────────

    /// <summary>
    /// Checks the strategy's choice against the other promising moves (and passing) by playing the match out
    /// many times with <see cref="RolloutPolicy"/>. Each play-out re-deals the opponent's hand from the cards
    /// still in their hand and deck and reshuffles our deck, so the bot never relies on cards it can't see.
    /// </summary>
    private Move LookAhead(MatchState match, int me, TurnContext ctx, List<Outcome> outcomes, Move proposed)
    {
        // Nothing to think about: the opponent passed and we already win the round.
        if (ctx.OppPassed && ctx.WinsIfEnded(ctx.Diff))
            return proposed;

        var candidates = outcomes
            .OrderByDescending(o => o.Score)
            .Select(o => o.Move)
            .Take(LookAheadCandidates)
            .Append(proposed)
            .Append(Move.Pass)
            .Distinct()
            .ToList();
        if (candidates.Count == 1)
            return proposed;

        var totals = new double[candidates.Count];
        for (var w = 0; w < LookAheadWorlds; w++)
        {
            var world = Determinize(match, me);
            var seed = _rng.Next();
            for (var i = 0; i < candidates.Count; i++)
            {
                var state = world.DeepClone();
                try
                {
                    Apply(state, me, candidates[i]);
                    ResolveOwnPendings(state, me, ctx, MaxPendingDepth - 1);
                }
                catch (InvalidOperationException)
                {
                    totals[i] = double.NegativeInfinity;
                    continue;
                }

                totals[i] += RolloutPolicy.PlayOut(_engine, state, me, new Random(seed));
            }
        }

        // The strategy's own pick wins ties.
        var bestIndex = candidates.IndexOf(proposed);
        for (var i = 0; i < candidates.Count; i++)
        {
            if (totals[i] > totals[bestIndex] + LookAheadMargin)
                bestIndex = i;
        }

        return candidates[bestIndex];
    }

    /// <summary>
    /// A copy of the match where the opponent's hand is re-dealt from their unseen cards (what is left of their
    /// deck list — as with the public preset decks) and our own deck is reshuffled.
    /// </summary>
    private MatchState Determinize(MatchState match, int me)
    {
        var world = match.DeepClone();
        var opp = world.Players[1 - me];
        var unseen = opp.Hand.Concat(opp.Deck).OrderBy(_ => _rng.Next()).ToList();
        opp.Hand = unseen.Take(opp.Hand.Count).ToList();
        opp.Deck = unseen.Skip(opp.Hand.Count).ToList();
        var mine = world.Players[me];
        mine.Deck = mine.Deck.OrderBy(_ => _rng.Next()).ToList();
        return world;
    }

    // ───────────────────────────── round strategy ─────────────────────────────

    private Move ChooseMove(MatchState match, int me, TurnContext ctx, List<Outcome> outcomes)
    {
        var best = outcomes.MaxBy(o => o.Score);
        if (best is null)
            return Move.Pass;

        var winningNow = ctx.WinsIfEnded(ctx.Diff);

        if (ctx.OppPassed)
        {
            if (winningNow)
                return Move.Pass;

            var plan = FindPlan(match, me, ctx, acceptTie: false);
            if (plan is not null)
            {
                // A deciding round is always worth winning; the opening round only if it doesn't
                // leave us behind on cards for the rest of the match.
                if (ctx.Decisive || ctx.MyHand + plan.HandDelta >= ctx.OppHand - 1)
                    return plan.FirstMove;
                return Move.Pass;
            }

            // Can't win. With one crystal each a draw still beats a loss.
            if (ctx.MyLives == 1 && ctx.OppLives == 1)
            {
                var tie = FindPlan(match, me, ctx, acceptTie: true);
                if (tie is not null)
                    return tie.FirstMove;
            }

            return Move.Pass;
        }

        if (ctx.MyLives == 1)
        {
            // Losing this round loses the match: keep fighting unless the lead can't be caught.
            if (winningNow && (ctx.OppHand == 0 || ctx.Diff > ctx.SafeLead))
                return Move.Pass;
            return best.Move;
        }

        if (ctx.OppLives == 1)
        {
            // We are a round up and the opponent must win this one. With at least as many cards
            // the classic answer is to pass straight away and take the card lead into the decider.
            var roundFresh = ctx.MySpent == 0 && ctx.OppSpent == 0 && !ctx.BoardHasCards;
            if (roundFresh && ctx.MyHand >= ctx.OppHand)
                return Move.Pass;
            if (winningNow)
                return ctx.MyHand >= ctx.OppHand - 1 ? Move.Pass : best.Move;

            var retake = CheapestLeadTaker(ctx, outcomes);
            if (retake is not null)
                return retake.Move;
            // Let them have it if we go into the decider with more cards.
            return ctx.MyHand > ctx.OppHand ? Move.Pass : best.Move;
        }

        // Opening round, two crystals each: play for card advantage.
        var free = outcomes.Where(o => o.HandDelta > 0 && o.Score > 0).MaxBy(o => o.Score);
        if (free is not null)
            return free.Move;

        var roundStarted = ctx.MySpent != 0 || ctx.OppSpent != 0 || ctx.BoardHasCards;
        if (winningNow && roundStarted)
        {
            // Pass once one card of theirs can't simply take the round back, or once we've spent more than them.
            if (ctx.Diff >= ctx.OppCardValue + 4 || ctx.MySpent >= ctx.OppSpent + 2)
                return Move.Pass;
            var extend = outcomes.Where(o => o.Score > -ctx.CardValue * 0.5).MaxBy(o => o.Score);
            return extend?.Move ?? Move.Pass;
        }

        // Trade card for card only while we haven't out-spent them, and not for long:
        // the opening round is not worth our best cards.
        var lead = ctx.MySpent < 4 || ctx.MySpent < ctx.OppSpent
            ? CheapestLeadTaker(ctx, outcomes, maxCost: ctx.CardValue * 0.5)
            : null;
        if (lead is not null)
            return lead.Move;

        if (roundStarted && ctx.MyHand >= ctx.OppHand)
            return Move.Pass; // concede the round, keep the card lead
        if (ctx.Diff < -2.5 * ctx.CardValue)
            return Move.Pass; // hopeless — don't throw cards after it

        return outcomes
            .OrderByDescending(o => ctx.WinsIfEnded(ctx.Diff + (int)o.Board) ? 1 : 0)
            .ThenByDescending(o => o.Score)
            .First().Move;
    }

    /// <summary>The best move that puts us ahead with a single card, if it doesn't cost more than <paramref name="maxCost"/>.</summary>
    private static Outcome? CheapestLeadTaker(TurnContext ctx, List<Outcome> outcomes, double? maxCost = null) =>
        outcomes
            .Where(o => ctx.WinsIfEnded(ctx.Diff + (int)o.Board) && o.Score > -(maxCost ?? ctx.CardValue))
            .MaxBy(o => o.Score);

    // ───────────────────────────── planning when the opponent passed ─────────────────────────────

    private sealed record Plan(Move FirstMove, double Cost, int HandDelta);

    /// <summary>
    /// Cheapest sequence of our moves that wins (or ties) the round, searched with iterative deepening
    /// up to three moves and a greedy fallback that throws in the strongest cards.
    /// </summary>
    private Plan? FindPlan(MatchState match, int me, TurnContext ctx, bool acceptTie)
    {
        bool Goal(MatchState s)
        {
            var d = Diff(s, me);
            return d > 0 || (d == 0 && (ctx.IWinTies || acceptTie));
        }

        for (var depth = 1; depth <= 3; depth++)
        {
            var plan = Search(match, me, ctx, depth, Goal, branching: depth == 1 ? 12 : 6);
            if (plan is not null)
                return plan;
        }

        return Greedy(match, me, ctx, Goal);
    }

    private Plan? Search(MatchState state, int me, TurnContext ctx, int depth, Func<MatchState, bool> goal, int branching)
    {
        Plan? best = null;
        var outcomes = EvaluateMoves(state, me, ctx)
            .OrderByDescending(o => o.Board)
            .Take(branching);

        foreach (var o in outcomes)
        {
            var cost = o.Board - o.Score;
            Plan? candidate = null;
            if (goal(o.After))
            {
                candidate = new Plan(o.Move, cost, o.HandDelta);
            }
            else if (depth > 1 && CanContinue(o.After, me))
            {
                var rest = Search(o.After, me, ctx, depth - 1, goal, Math.Max(4, branching - 2));
                if (rest is not null)
                    candidate = new Plan(o.Move, cost + rest.Cost, o.HandDelta + rest.HandDelta);
            }

            if (candidate is not null && (best is null || candidate.Cost < best.Cost))
                best = candidate;
        }

        return best;
    }

    private Plan? Greedy(MatchState state, int me, TurnContext ctx, Func<MatchState, bool> goal)
    {
        Move? first = null;
        double cost = 0;
        var handDelta = 0;
        for (var step = 0; step < 15; step++)
        {
            var o = EvaluateMoves(state, me, ctx).MaxBy(x => x.Board);
            if (o is null || o.Board <= 0)
                return null;
            first ??= o.Move;
            cost += o.Board - o.Score;
            handDelta += o.HandDelta;
            if (goal(o.After))
                return new Plan(first, cost, handDelta);
            if (!CanContinue(o.After, me))
                return null;
            state = o.After;
        }

        return null;
    }

    private static bool CanContinue(MatchState s, int me) =>
        s.Phase == MatchPhase.Playing && s.ActivePlayer == me && s.Pending is null && !s.Players[me].HasPassed;

    // ───────────────────────────── move evaluation ─────────────────────────────

    private enum MoveKind { Pass, Card, Leader }

    private sealed record Move(MoveKind Kind, Guid CardId = default, CombatRow? Row = null, Guid? Target = null)
    {
        public static readonly Move Pass = new(MoveKind.Pass);
        public static readonly Move Leader = new(MoveKind.Leader);
    }

    private sealed class Outcome
    {
        public required Move Move { get; init; }
        public required MatchState After { get; init; }
        /// <summary>Change of (our score − their score).</summary>
        public double Board { get; init; }
        /// <summary>Board swing plus cards gained minus the value of what we spent.</summary>
        public double Score { get; init; }
        /// <summary>Change of our hand size (−1 for a normal play, +1 for a spy).</summary>
        public int HandDelta { get; init; }
    }

    private List<Outcome> EvaluateMoves(MatchState match, int me, TurnContext ctx)
    {
        var result = new List<Outcome>();
        foreach (var move in GenerateMoves(match, me))
        {
            var outcome = Simulate(match, me, move, ctx);
            if (outcome is not null)
                result.Add(outcome);
        }

        return result;
    }

    private static IEnumerable<Move> GenerateMoves(MatchState match, int me)
    {
        var player = match.Players[me];
        var seen = new HashSet<string>();
        foreach (var card in player.Hand)
        {
            // Identical copies lead to identical outcomes — simulate one of them.
            if (!seen.Add($"{card.Name}|{card.BaseStrength}|{string.Join(',', card.Abilities)}"))
                continue;

            if (card.Type == CardType.Unit)
            {
                if (card.Abilities.Contains(Ability.Agile))
                {
                    yield return new Move(MoveKind.Card, card.InstanceId, CombatRow.Melee);
                    yield return new Move(MoveKind.Card, card.InstanceId, CombatRow.Ranged);
                }
                else
                {
                    yield return new Move(MoveKind.Card, card.InstanceId);
                }
            }
            else if (card.Abilities.Contains(Ability.Decoy))
            {
                foreach (var target in RulesEngine.DecoyTargets(player))
                    yield return new Move(MoveKind.Card, card.InstanceId, Target: target);
            }
            else if (card.Abilities.Contains(Ability.CommandersHorn))
            {
                foreach (var row in RulesEngine.BoardRows.Where(r => !player.HornRows.Contains(r)))
                    yield return new Move(MoveKind.Card, card.InstanceId, row);
            }
            else if (card.Abilities.Contains(Ability.Mardroeme))
            {
                foreach (var row in RulesEngine.BoardRows.Where(r => !player.MardroemeRows.Contains(r)))
                    yield return new Move(MoveKind.Card, card.InstanceId, row);
            }
            else
            {
                yield return new Move(MoveKind.Card, card.InstanceId);
            }
        }

        if (player.CanUseLeader)
            yield return Move.Leader;
    }

    private Outcome? Simulate(MatchState match, int me, Move move, TurnContext ctx, int depth = 0)
    {
        var after = match.DeepClone();
        try
        {
            Apply(after, me, move);
            ResolveOwnPendings(after, me, ctx, depth);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        return Score(match, after, me, move, ctx);
    }

    private void Apply(MatchState match, int me, Move move)
    {
        switch (move.Kind)
        {
            case MoveKind.Pass:
                _engine.Pass(match, me);
                break;
            case MoveKind.Card:
                _engine.PlayCard(match, me, move.CardId, move.Row, move.Target);
                break;
            case MoveKind.Leader:
                _engine.UseLeader(match, me);
                break;
        }
    }

    private Outcome Score(MatchState before, MatchState after, int me, Move move, TurnContext ctx)
    {
        var meBefore = before.Players[me];
        var meAfter = after.Players[me];

        double board = Diff(after, me) - Diff(before, me);

        var handBefore = meBefore.Hand.Select(c => c.InstanceId).ToHashSet();
        var handAfter = meAfter.Hand.Select(c => c.InstanceId).ToHashSet();
        var deckBefore = meBefore.Deck.Select(c => c.InstanceId).ToHashSet();

        var spent = meBefore.Hand.Where(c => !handAfter.Contains(c.InstanceId)).Sum(c => Keep(c, meBefore, ctx));
        // Cards from our deck count as an average draw: we know what is left, not the order.
        // A card coming back to hand (Decoy, Bringer of Death…) is worth what it will do when replayed this round
        // if nothing is kept for later, otherwise what it's worth to hold.
        var gained = meAfter.Hand.Where(c => !handBefore.Contains(c.InstanceId))
            .Sum(c => deckBefore.Contains(c.InstanceId) ? ctx.CardValue
                : ctx.KeepWeight < 1 ? ReplayValue(c, ctx)
                : Keep(c, meAfter, ctx));

        // Never burn the leader for nothing, even when it's otherwise free.
        var leaderCost = meBefore.CanUseLeader && !meAfter.CanUseLeader ? Math.Max(ctx.LeaderValue, 1) : 0;

        var score = board + gained - ctx.KeepWeight * spent - leaderCost + FutureEffects(before, after, me, ctx);

        if (move.Kind == MoveKind.Card && !ctx.OppPassed && ctx.OppHand > 0)
        {
            var card = meBefore.Hand.First(c => c.InstanceId == move.CardId);
            if (card.Type == CardType.Special && card.Abilities.Contains(Ability.Scorch))
                score -= 3; // Scorch is worth more once the opponent has committed
        }

        return new Outcome
        {
            Move = move,
            After = after,
            Board = board,
            Score = score,
            HandDelta = meAfter.Hand.Count - meBefore.Hand.Count
        };
    }

    /// <summary>
    /// Effects that outlast the move: weather and horns change what the cards still in hand will be worth this round.
    /// </summary>
    private static double FutureEffects(MatchState before, MatchState after, int me, TurnContext ctx)
    {
        // Only the part of the hand we'll actually play this round feels the effect.
        var share = ctx.PlayShare;
        var meAfter = after.Players[me];
        var units = meAfter.Hand
            .Where(c => c.Type == CardType.Unit && !c.IsHero && !c.Abilities.Contains(Ability.Spy))
            .ToList();
        double value = 0;

        foreach (var row in RulesEngine.BoardRows)
        {
            var mine = units.Where(c => c.PlacedRow == row && !c.Abilities.Contains(Ability.Agile)).ToList();

            var weatherBefore = ScoreCalculator.GetWeather(before, row) != WeatherEffect.None;
            var weatherAfter = ScoreCalculator.GetWeather(after, row) != WeatherEffect.None;
            if (weatherBefore != weatherAfter)
            {
                var sign = weatherAfter ? -1 : 1;
                var myLoss = mine.Sum(c => c.BaseStrength - Math.Min(c.BaseStrength, 1));
                if (meAfter.ActiveLeaderAbility == LeaderAbility.KingBran)
                    myLoss /= 2;
                value += sign * myLoss * share;
                if (!ctx.OppPassed)
                    value -= sign * ctx.OppHand * ctx.OppRowShare(row) * (ctx.OppCardValue - 1) * share * 0.6;
            }

            if (!before.Players[me].HornRows.Contains(row) && meAfter.HornRows.Contains(row))
                value += mine.Sum(c => c.BaseStrength) * share;
        }

        return value;
    }

    /// <summary>
    /// What a card is worth if we keep it for later — roughly half a card plus its expected strength.
    /// </summary>
    private static double Keep(CardInstance c, PlayerState owner, TurnContext ctx)
    {
        var avg = ctx.CardValue;
        if (c.Type == CardType.Special)
        {
            if (c.Abilities.Contains(Ability.Scorch)) return avg + 3;
            if (c.Abilities.Contains(Ability.CommandersHorn)) return avg + 4;
            if (c.Abilities.Contains(Ability.Decoy)) return avg;
            if (c.Abilities.Contains(Ability.Mardroeme))
                return owner.Hand.Concat(owner.Deck).Concat(owner.AllBoardCards()).Any(x => x.Abilities.Contains(Ability.Berserker))
                    ? avg
                    : 1;
            if (c.Abilities.Contains(Ability.WeatherClear)) return avg * 0.4;
            if (RulesEngine.IsWeatherCard(c)) return avg * 0.6;
            return avg * 0.5;
        }

        if (c.Abilities.Contains(Ability.Spy))
            return avg * 0.3; // spies are meant to be played

        double v = c.DefaultStrength > 0 ? c.DefaultStrength : c.BaseStrength;
        if (c.Abilities.Contains(Ability.Muster) && !string.IsNullOrEmpty(c.MusterGroup))
            v += owner.Deck.Where(x => x.MusterGroup == c.MusterGroup).Sum(x => x.DefaultStrength);
        if (c.Abilities.Contains(Ability.Medic)) v += avg * 0.6;
        if (c.Abilities.Contains(Ability.TightBond)
            && owner.Hand.Concat(owner.Deck).Any(x => x.InstanceId != c.InstanceId && x.Name == c.Name))
            v += v * 0.5;
        if (c.Abilities.Contains(Ability.MoraleBoost)) v += 2;
        if (c.Abilities.Contains(Ability.CommandersHorn)) v += 4;
        if (c.Abilities.Contains(Ability.Scorch)) v += 3;
        if (c.Abilities.Contains(Ability.Berserker)) v += 2;
        if (c.IsHero) v += 4; // immune to weather and Scorch — best saved for the decider
        return 0.3 * avg + v;
    }

    /// <summary>Points a card adds when it is played again later in the same round.</summary>
    private static double ReplayValue(CardInstance c, TurnContext ctx)
    {
        if (c.Type == CardType.Special)
            return c.Abilities.Contains(Ability.Scorch) || c.Abilities.Contains(Ability.CommandersHorn)
                ? ctx.CardValue
                : ctx.CardValue * 0.5;
        if (c.Abilities.Contains(Ability.Spy))
            return 2 * ctx.CardValue - c.DefaultStrength;
        return c.DefaultStrength + (c.Abilities.Contains(Ability.Medic) ? ctx.CardValue * 0.6 : 0);
    }

    private static int Diff(MatchState s, int me) =>
        ScoreCalculator.GetTotalScore(s.Players[me], s) - ScoreCalculator.GetTotalScore(s.Players[1 - me], s);

    // ───────────────────────────── execution and choices ─────────────────────────────

    private void Execute(MatchState match, int me, TurnContext ctx, Move chosen, List<Outcome> outcomes)
    {
        var fallbacks = new[] { chosen }
            .Concat(outcomes.OrderByDescending(o => o.Score).Select(o => o.Move))
            .Append(Move.Pass);

        foreach (var move in fallbacks)
        {
            try
            {
                Apply(match, me, move);
                ResolveOwnPendings(match, me, ctx, 0);
                return;
            }
            catch (InvalidOperationException)
            {
                if (match.Pending?.PlayerIndex == me)
                {
                    ResolvePendingAnyhow(match, me);
                    return;
                }
                if (match.ActivePlayer != me || match.Players[me].HasPassed || match.Phase != MatchPhase.Playing)
                    return;
            }
        }
    }

    /// <summary>Last resort when a choice failed: take the first legal option so the game never stalls.</summary>
    private void ResolvePendingAnyhow(MatchState match, int me)
    {
        var guard = 0;
        while (match.Pending is { } p && p.PlayerIndex == me && guard++ < 8)
        {
            try
            {
                if (p.CanSkip)
                    _engine.ResolvePending(match, me);
                else if (p.ValidRows.Count > 0)
                    _engine.ResolvePending(match, me, row: p.ValidRows[0]);
                else
                    _engine.ResolvePending(match, me, targetId: p.ValidTargets[0]);
            }
            catch (InvalidOperationException)
            {
                return;
            }
        }
    }

    private void ResolveOwnPendings(MatchState match, int me, TurnContext ctx, int depth)
    {
        var guard = 0;
        while (match.Pending?.PlayerIndex == me && guard++ < 8)
            ResolvePending(match, me, ctx, depth);
    }

    private void ResolvePending(MatchState match, int me, TurnContext ctx, int depth = 0)
    {
        var pending = match.Pending!;
        if (pending.Kind == PendingKind.ChooseWhoStarts)
        {
            // Going second gives the last word in the round.
            _engine.ChooseWhoStarts(match, me, 1 - me);
            return;
        }

        var (target, row) = BestPendingChoice(match, me, ctx, depth);
        _engine.ResolvePending(match, me, target, row);
    }

    private (Guid? Target, CombatRow? Row) BestPendingChoice(MatchState match, int me, TurnContext ctx, int depth)
    {
        var pending = match.Pending!;
        var player = match.Players[me];

        switch (pending.Kind)
        {
            case PendingKind.RevealOpponentCards:
                return (null, null);

            case PendingKind.ChooseHandDiscard:
                return (pending.ValidTargets
                    .Select(id => player.Hand.First(c => c.InstanceId == id))
                    .MinBy(c => Keep(c, player, ctx))!.InstanceId, null);

            case PendingKind.ChooseDeckCard:
            case PendingKind.ChooseOwnDiscardCard:
            case PendingKind.ChooseEnemyDiscardCard:
            {
                var pick = pending.ValidTargets
                    .Select(id => RulesEngine.FindCardAnywhere(match, id))
                    .Where(c => c is not null)
                    .MaxBy(c => Keep(c!, player, ctx));
                return (pick?.InstanceId, null);
            }
        }

        var options = new List<(Guid? Target, CombatRow? Row)>();
        options.AddRange(pending.ValidRows.Select(r => ((Guid?)null, (CombatRow?)r)));
        options.AddRange(pending.ValidTargets.Select(id => ((Guid?)id, (CombatRow?)null)));
        if (pending.CanSkip)
            options.Add((null, null));
        if (options.Count == 0)
            return (null, null);
        if (depth >= MaxPendingDepth || options.Count == 1)
            return options[0];

        var best = options[0];
        var bestScore = double.MinValue;
        foreach (var option in options)
        {
            var after = match.DeepClone();
            try
            {
                _engine.ResolvePending(after, me, option.Target, option.Row);
                ResolveOwnPendings(after, me, ctx, depth + 1);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            var score = Score(match, after, me, Move.Pass, ctx).Score;
            if (score > bestScore)
            {
                bestScore = score;
                best = option;
            }
        }

        return best;
    }

    // ───────────────────────────── mulligan ─────────────────────────────

    private void Mulligan(MatchState match, int me)
    {
        var bot = match.Players[me];
        while (bot.MulliganLeft > 0 && match.Phase == MatchPhase.Mulligan && bot.Hand.Count > 0)
        {
            var ctx = new TurnContext(match, me);
            var averageDraw = bot.Deck.Count == 0 ? 0 : bot.Deck.Average(c => Keep(c, bot, ctx));
            var (worst, value) = bot.Hand
                .Select(c => (Card: c, Value: MulliganValue(c, bot, ctx)))
                .MinBy(x => x.Value);
            if (value >= averageDraw - 1)
                break;
            _engine.Mulligan(match, me, worst.InstanceId);
        }

        if (match.Phase == MatchPhase.Mulligan && bot.MulliganLeft > 0)
            _engine.FinishMulligan(match, me);
        // Scoia'tael bot may receive ChooseWhoStarts in the same step — resolve immediately.
        if (match.Pending?.PlayerIndex == me)
            ResolvePending(match, me, new TurnContext(match, me));
    }

    private static double MulliganValue(CardInstance c, PlayerState bot, TurnContext ctx)
    {
        // Muster only pulls from the deck: a second copy in hand is just a lone weak unit.
        if (c.Abilities.Contains(Ability.Muster)
            && bot.Hand.Any(x => x.InstanceId != c.InstanceId && x.MusterGroup == c.MusterGroup)
            && bot.Hand.First(x => x.MusterGroup == c.MusterGroup).InstanceId != c.InstanceId)
            return 0.5 * ctx.CardValue + 0.6 * c.DefaultStrength - 2;

        // Duplicate weather is rarely needed twice.
        if (RulesEngine.IsWeatherCard(c)
            && bot.Hand.Any(x => x.InstanceId != c.InstanceId && x.Name == c.Name))
            return 1;

        return Keep(c, bot, ctx);
    }

    // ───────────────────────────── context ─────────────────────────────

    private sealed class TurnContext
    {
        public TurnContext(MatchState match, int me)
        {
            var my = match.Players[me];
            var opp = match.Players[1 - me];
            MyLives = my.Lives;
            OppLives = opp.Lives;
            MyHand = my.Hand.Count;
            OppHand = opp.Hand.Count;
            OppPassed = opp.HasPassed;
            Diff = BotAI.Diff(match, me);
            MySpent = my.HandAtRoundStart - my.Hand.Count;
            OppSpent = opp.HandAtRoundStart - opp.Hand.Count;
            BoardHasCards = match.Players.Any(p => p.AllBoardCards().Any());
            OppHasLeader = opp.CanUseLeader;
            IWinTies = my.Faction == Faction.Nilfgaard && opp.Faction != Faction.Nilfgaard;
            OppWinsTies = opp.Faction == Faction.Nilfgaard && my.Faction != Faction.Nilfgaard;

            var myUnits = my.Deck.Where(c => c.Type == CardType.Unit && !c.Abilities.Contains(Ability.Spy)).ToList();
            if (myUnits.Count == 0)
                myUnits = my.Hand.Where(c => c.Type == CardType.Unit && !c.Abilities.Contains(Ability.Spy)).ToList();
            CardValue = myUnits.Count == 0 ? 5 : Math.Clamp(myUnits.Average(c => c.DefaultStrength), 3, 9);

            var seen = opp.AllBoardCards().Concat(opp.Discard)
                .Where(c => c.Type == CardType.Unit && !c.Abilities.Contains(Ability.Spy))
                .ToList();
            OppCardValue = seen.Count == 0 ? 6 : Math.Clamp(seen.Average(c => c.DefaultStrength), 3, 10);
            // Where the opponent's units tend to go, smoothed towards an even split.
            foreach (var row in RulesEngine.BoardRows)
                _oppRowShare[row] = (seen.Count(c => c.DefaultRow == row) + 1.0) / (seen.Count + 3.0);

            // With one crystal each nothing is kept after this round, so held cards only matter for timing.
            var lastRound = MyLives == 1 && OppLives == 1;
            KeepWeight = lastRound ? 0.3 : 1.0;
            LeaderValue = lastRound ? 0 : Decisive ? CardValue : 2 * CardValue;
            PlayShare = lastRound ? 0.9 : Decisive ? 0.6 : 0.35;
        }

        public int MyLives { get; }
        public int OppLives { get; }
        public int MyHand { get; }
        public int OppHand { get; }
        public bool OppPassed { get; }
        /// <summary>Our score minus theirs.</summary>
        public int Diff { get; }
        /// <summary>Net cards each side has spent this round (spies make it negative).</summary>
        public int MySpent { get; }
        public int OppSpent { get; }
        public bool BoardHasCards { get; }
        public bool OppHasLeader { get; }
        public bool IWinTies { get; }
        public bool OppWinsTies { get; }
        /// <summary>Average strength of a unit we could still draw.</summary>
        public double CardValue { get; }
        /// <summary>Average strength of the opponent's units seen so far.</summary>
        public double OppCardValue { get; }
        public double KeepWeight { get; }
        public double LeaderValue { get; }
        /// <summary>Share of the hand we expect to play this round.</summary>
        public double PlayShare { get; }

        private readonly Dictionary<CombatRow, double> _oppRowShare = [];

        /// <summary>Estimated share of the opponent's remaining units that belong to <paramref name="row"/>.</summary>
        public double OppRowShare(CombatRow row) => _oppRowShare.GetValueOrDefault(row, 1 / 3.0);

        /// <summary>A round where someone is on their last crystal.</summary>
        public bool Decisive => MyLives == 1 || OppLives == 1;

        /// <summary>A lead the opponent can't realistically close with the cards they hold (horns, bonds and leader included).</summary>
        public double SafeLead => OppHand * (OppCardValue + 6) + (OppHasLeader ? 15 : 0) + 5;

        public bool WinsIfEnded(int diff) => diff > 0 || (diff == 0 && IWinTies);
    }
}
