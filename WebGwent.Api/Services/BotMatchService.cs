using System.Collections.Concurrent;
using WebGwent.Api.Accounts;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;
using WebGwent.Game.Services;

namespace WebGwent.Api.Services;

public sealed class BotMatchService(GameSessionManager sessions, AccountStore accounts)
{
    private readonly ConcurrentDictionary<Guid, Guid?> _owners = new();

    public MatchBoardDto Create(
        Guid? accountId,
        Guid deck0Id,
        Guid deck1Id,
        BotDifficulty difficulty,
        string? youName,
        string botName)
    {
        if (BotDifficultyRules.RequiresPlus(difficulty)
            && (accountId is null || !accounts.HasPlus(accountId.Value)))
            throw new InvalidOperationException("PlusRequired");

        var d0 = sessions.Decks.Get(deck0Id) ?? throw new InvalidOperationException("DeckMissing");
        var d1 = sessions.Decks.Get(deck1Id) ?? throw new InvalidOperationException("DeckMissing");
        var v0 = sessions.Decks.Validate(d0);
        var v1 = sessions.Decks.Validate(d1);
        if (!v0.IsValid || !v1.IsValid)
            throw new InvalidOperationException("DeckInvalid:" + string.Join("; ", v0.Errors.Concat(v1.Errors)));

        var you = string.IsNullOrWhiteSpace(youName) ? "Player" : youName.Trim();
        var match = sessions.CreateLocal(GameMode.VsBot, d0.ToDeckList(), d1.ToDeckList(), you, botName, difficulty);
        _owners[match.MatchId] = accountId;

        return BuildDto(match, accountId, difficulty, you, botName);
    }

    public MatchBoardDto Get(Guid matchId, Guid? accountId) =>
        BuildDto(RequireOwned(matchId, accountId), accountId);

    public MatchBoardDto Act(Guid matchId, Guid? accountId, Action<MatchState, RulesEngine> action, bool autoBot = false)
    {
        var match = RequireOwned(matchId, accountId);
        sessions.ActLocal(matchId, 0, action, autoBot: autoBot);
        match = sessions.GetLocal(matchId)!;
        if (match.Phase == MatchPhase.MatchEnd && accountId is Guid id)
            accounts.RecordBotMatch(id, match, 0);
        return BuildDto(match, accountId);
    }

    public MatchBoardDto ContinueAfterRound(Guid matchId, Guid? accountId)
    {
        RequireOwned(matchId, accountId);
        sessions.ContinueAfterRoundLocal(matchId, 0);
        return BuildDto(sessions.GetLocal(matchId)!, accountId);
    }

    public bool StepBot(Guid matchId, Guid? accountId)
    {
        RequireOwned(matchId, accountId);
        return sessions.StepBotLocal(matchId);
    }

    private MatchState RequireOwned(Guid matchId, Guid? accountId)
    {
        var match = sessions.GetLocal(matchId) ?? throw new KeyNotFoundException("MatchNotFound");
        if (!_owners.TryGetValue(matchId, out var owner))
            throw new KeyNotFoundException("MatchNotFound");
        if (owner is Guid oid && accountId != oid)
            throw new UnauthorizedAccessException("Forbidden");
        // Guest matches (owner null): anyone with the id may act (id is unguessable GUID).
        if (owner is null && accountId is not null)
        {
            // First authenticated touch claims the match.
            _owners[matchId] = accountId;
        }
        return match;
    }

    private MatchBoardDto BuildDto(MatchState match, Guid? accountId, BotDifficulty? difficulty = null, string? youName = null, string? botName = null)
    {
        difficulty ??= match.BotDifficulty;
        var you = accountId is Guid id && accounts.FindById(id) is { } acc
            ? BoardProfileFactory.FromAccount(acc)
            : BoardProfile.Guest(youName ?? match.Players[0].Name);
        var bot = BoardProfile.Bot(botName ?? match.Players[1].Name, difficulty);
        return new MatchBoardDto(MatchView.From(match, 0), you, bot);
    }
}

public record MatchBoardDto(MatchView View, BoardProfile You, BoardProfile Opponent);
