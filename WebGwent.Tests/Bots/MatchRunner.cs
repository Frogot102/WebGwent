using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Tests.Bots;

/// <summary>
/// Plays a whole match between two automated players (each is just "take a turn for seat N").
/// </summary>
public static class MatchRunner
{
    public static MatchState Play(
        CardCatalog catalog,
        RulesEngine engine,
        DeckList deck0,
        DeckList deck1,
        Action<MatchState, int> seat0,
        Action<MatchState, int> seat1,
        int startingPlayer,
        Action<MatchState>? onRoundEnd = null)
    {
        var match = new MatchFactory(catalog, engine).Create(deck0, deck1, GameMode.Hotseat);
        // Both seats are driven from here, so the engine must not auto-finish anyone's mulligan.
        match.Players[0].IsBot = false;
        match.Players[1].IsBot = false;
        match.StartingPlayer = startingPlayer;
        match.ActivePlayer = startingPlayer;

        var seats = new[] { seat0, seat1 };
        for (var guard = 0; guard < 2000 && match.Phase != MatchPhase.MatchEnd; guard++)
        {
            if (match.Phase == MatchPhase.RoundEnd)
            {
                onRoundEnd?.Invoke(match);
                engine.ContinueAfterRound(match);
                continue;
            }

            var actor = match.Pending?.PlayerIndex
                        ?? (match.Phase == MatchPhase.Mulligan
                            ? Array.FindIndex(match.Players, p => p.MulliganLeft > 0)
                            : match.ActivePlayer);

            var lastLog = match.Log.LastOrDefault();
            try
            {
                seats[actor](match, actor);
            }
            catch (InvalidOperationException)
            {
                // A seat that trips over a rule loses its turn instead of hanging the match.
            }

            if (ReferenceEquals(lastLog, match.Log.LastOrDefault()))
                ForceProgress(engine, match, actor);
        }

        return match;
    }

    private static void ForceProgress(RulesEngine engine, MatchState match, int actor)
    {
        if (match.Phase == MatchPhase.Mulligan && match.Pending is null)
        {
            engine.FinishMulligan(match, actor);
            return;
        }

        if (match.Pending is { } p)
        {
            if (p.Kind == PendingKind.ChooseWhoStarts)
                engine.ChooseWhoStarts(match, p.PlayerIndex, p.PlayerIndex);
            else if (p.CanSkip)
                engine.ResolvePending(match, p.PlayerIndex);
            else if (p.ValidRows.Count > 0)
                engine.ResolvePending(match, p.PlayerIndex, row: p.ValidRows[0]);
            else
                engine.ResolvePending(match, p.PlayerIndex, targetId: p.ValidTargets[0]);
            return;
        }

        if (match.Phase == MatchPhase.Playing)
            engine.Pass(match, match.ActivePlayer);
    }
}
