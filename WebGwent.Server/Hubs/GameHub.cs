using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebGwent.Game.Catalog;
using WebGwent.Game.Models;
using WebGwent.Game.Services;
using WebGwent.Server.Accounts;

namespace WebGwent.Server.Hubs;

[Authorize]
public sealed class GameHub : Hub
{
    private readonly GameSessionManager _sessions;
    private readonly AccountStore _accounts;

    public GameHub(GameSessionManager sessions, AccountStore accounts)
    {
        _sessions = sessions;
        _accounts = accounts;
    }

    public async Task<RoomCreatedDto> CreateRoom(string faction, string playerName, string? deckId = null, string? accountId = null)
    {
        var deck = ResolveDeck(deckId, faction);
        Guid? acc = Guid.TryParse(accountId, out var g) ? g : null;
        var code = _sessions.CreateRoom(deck.Faction, Context.ConnectionId,
            string.IsNullOrWhiteSpace(playerName) ? "Host" : playerName, deck, acc);
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        return new RoomCreatedDto(code, 0);
    }

    public async Task<JoinedRoomDto> JoinRoom(string code, string faction, string playerName, string? deckId = null, string? accountId = null)
    {
        var deck = ResolveDeck(deckId, faction);
        Guid? acc = Guid.TryParse(accountId, out var g) ? g : null;
        var room = _sessions.JoinRoom(code.Trim().ToUpperInvariant(), deck.Faction, Context.ConnectionId,
            string.IsNullOrWhiteSpace(playerName) ? "Guest" : playerName, deck, acc);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

        await Clients.Client(room.HostConnectionId!).SendAsync("MatchUpdated", BuildUpdate(room, 0));
        await Clients.Caller.SendAsync("MatchUpdated", BuildUpdate(room, 1));
        return new JoinedRoomDto(room.Code, 1, room.Match.MatchId);
    }

    private Game.Engine.DeckList ResolveDeck(string? deckId, string factionFallback)
    {
        if (Guid.TryParse(deckId, out var id))
        {
            var saved = _sessions.Decks.Get(id)
                        ?? throw new HubException("Deck not found.");
            var validation = _sessions.Decks.Validate(saved);
            if (!validation.IsValid)
                throw new HubException("Deck invalid: " + string.Join("; ", validation.Errors));
            return saved.ToDeckList();
        }

        if (!Enum.TryParse<Faction>(factionFallback, true, out var f))
            throw new HubException("Invalid faction.");
        return PresetDecks.ForFaction(f);
    }

    public async Task Reconnect(string code, int playerIndex)
    {
        code = code.Trim().ToUpperInvariant();
        _sessions.RebindConnection(code, playerIndex, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        var room = _sessions.GetRoom(code);
        if (room?.GuestJoined == true)
            await Clients.Caller.SendAsync("MatchUpdated", BuildUpdate(room, playerIndex));
    }

    public async Task PlayCard(string code, int playerIndex, Guid cardId, string? row, Guid? targetId)
    {
        CombatRow? rowEnum = null;
        if (!string.IsNullOrEmpty(row) && Enum.TryParse<CombatRow>(row, true, out var r))
            rowEnum = r;

        await ActAndBroadcast(code, playerIndex, (m, e) => e.PlayCard(m, playerIndex, cardId, rowEnum, targetId));
    }

    public async Task Pass(string code, int playerIndex) =>
        await ActAndBroadcast(code, playerIndex, (m, e) => e.Pass(m, playerIndex));

    public async Task UseLeader(string code, int playerIndex) =>
        await ActAndBroadcast(code, playerIndex, (m, e) => e.UseLeader(m, playerIndex));

    public async Task Mulligan(string code, int playerIndex, Guid cardId) =>
        await ActAndBroadcast(code, playerIndex, (m, e) => e.Mulligan(m, playerIndex, cardId));

    public async Task FinishMulligan(string code, int playerIndex) =>
        await ActAndBroadcast(code, playerIndex, (m, e) => e.FinishMulligan(m, playerIndex));

    public async Task ResolvePending(string code, int playerIndex, Guid? targetId, string? row)
    {
        CombatRow? rowEnum = null;
        if (!string.IsNullOrEmpty(row) && Enum.TryParse<CombatRow>(row, true, out var r))
            rowEnum = r;
        await ActAndBroadcast(code, playerIndex, (m, e) => e.ResolvePending(m, playerIndex, targetId, rowEnum));
    }

    public async Task ChooseWhoStarts(string code, int playerIndex, int startingPlayer) =>
        await ActAndBroadcast(code, playerIndex, (m, e) => e.ChooseWhoStarts(m, playerIndex, startingPlayer));

    private async Task ActAndBroadcast(string code, int playerIndex, Action<MatchState, Game.Engine.RulesEngine> action)
    {
        code = code.Trim().ToUpperInvariant();
        try
        {
            _sessions.ActRoom(code, playerIndex, action);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
            return;
        }

        var room = _sessions.GetRoom(code)!;
        if (room.Match.Phase == MatchPhase.MatchEnd)
            _accounts.RecordOnlineMatch(room.HostAccountId, room.GuestAccountId, room.Match);

        if (room.HostConnectionId != null)
            await Clients.Client(room.HostConnectionId).SendAsync("MatchUpdated", BuildUpdate(room, 0));
        if (room.GuestConnectionId != null)
            await Clients.Client(room.GuestConnectionId).SendAsync("MatchUpdated", BuildUpdate(room, 1));
    }

    private MatchBoardUpdate BuildUpdate(GameRoom room, int viewerIndex)
    {
        var host = BoardProfile.FromAccountId(_accounts, room.HostAccountId, room.Match.Players[0].Name);
        var guest = BoardProfile.FromAccountId(_accounts, room.GuestAccountId, room.Match.Players[1].Name);
        var you = viewerIndex == 0 ? host : guest;
        var opp = viewerIndex == 0 ? guest : host;
        return new MatchBoardUpdate(MatchView.From(room.Match, viewerIndex), you, opp);
    }
}

public record RoomCreatedDto(string Code, int PlayerIndex);
public record JoinedRoomDto(string Code, int PlayerIndex, Guid MatchId);
public record MatchBoardUpdate(MatchView View, BoardProfile You, BoardProfile Opponent);
