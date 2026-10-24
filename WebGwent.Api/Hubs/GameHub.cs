using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebGwent.Api.Accounts;
using WebGwent.Api.Services;
using WebGwent.Game.Catalog;
using WebGwent.Game.Models;
using WebGwent.Game.Services;

namespace WebGwent.Api.Hubs;

[Authorize]
public sealed class GameHub(GameSessionManager sessions, AccountStore accounts) : Hub
{
    private Guid RequireUserId()
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out var id))
            throw new HubException("Unauthorized");
        return id;
    }

    private void RequirePlus(Guid accountId)
    {
        if (!accounts.HasPlus(accountId))
            throw new HubException("PlusRequired");
    }

    /// <summary>Authorize by account id for the seat, then rebind this connection (survives reconnect).</summary>
    private void RequireSeat(string code, int playerIndex)
    {
        var accountId = RequireUserId();
        var room = sessions.GetRoom(code) ?? throw new HubException("Room not found.");
        var expected = playerIndex == 0 ? room.HostAccountId : room.GuestAccountId;
        if (expected != accountId)
            throw new HubException("Forbidden");
        sessions.RebindConnection(code, playerIndex, Context.ConnectionId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        sessions.ClearConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task<RoomCreatedDto> CreateRoom(string faction, string playerName, string? deckId = null)
    {
        var accountId = RequireUserId();
        RequirePlus(accountId);
        var deck = ResolveDeck(deckId, faction);
        var code = sessions.CreateRoom(deck.Faction, Context.ConnectionId,
            string.IsNullOrWhiteSpace(playerName) ? "Host" : playerName, deck, accountId);
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        return new RoomCreatedDto(code, 0);
    }

    public async Task<JoinedRoomDto> JoinRoom(string code, string faction, string playerName, string? deckId = null)
    {
        var accountId = RequireUserId();
        RequirePlus(accountId);
        var deck = ResolveDeck(deckId, faction);
        var room = sessions.JoinRoom(code.Trim().ToUpperInvariant(), deck.Faction, Context.ConnectionId,
            string.IsNullOrWhiteSpace(playerName) ? "Guest" : playerName, deck, accountId);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

        await Clients.Group(room.Code).SendAsync("OpponentJoined", room.Code);
        await BroadcastBoard(room);
        return new JoinedRoomDto(room.Code, 1, room.Match.MatchId);
    }

    public async Task LeaveRoom(string code)
    {
        code = code.Trim().ToUpperInvariant();
        sessions.ClearConnection(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
    }

    /// <summary>Pull current board for a seated player (host lobby → match, or after remount).</summary>
    public async Task<MatchBoardUpdate?> GetBoard(string code, int playerIndex)
    {
        var accountId = RequireUserId();
        RequirePlus(accountId);
        code = code.Trim().ToUpperInvariant();
        var room = sessions.GetRoom(code) ?? throw new HubException("Room not found.");
        var expectedAccount = playerIndex == 0 ? room.HostAccountId : room.GuestAccountId;
        if (expectedAccount != accountId)
            throw new HubException("Forbidden");

        sessions.RebindConnection(code, playerIndex, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, code);

        if (!room.GuestJoined)
            return null;
        return BuildUpdate(room, playerIndex);
    }

    private Game.Engine.DeckList ResolveDeck(string? deckId, string factionFallback)
    {
        if (Guid.TryParse(deckId, out var id))
        {
            var saved = sessions.Decks.Get(id)
                        ?? throw new HubException("Deck not found.");
            var validation = sessions.Decks.Validate(saved);
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
        var accountId = RequireUserId();
        RequirePlus(accountId);
        code = code.Trim().ToUpperInvariant();
        var room = sessions.GetRoom(code)
                   ?? throw new HubException("Room not found.");
        var expectedAccount = playerIndex == 0 ? room.HostAccountId : room.GuestAccountId;
        if (expectedAccount != accountId)
            throw new HubException("Forbidden");

        sessions.RebindConnection(code, playerIndex, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        if (room.GuestJoined)
            await Clients.Caller.SendAsync("MatchUpdated", BuildUpdate(room, playerIndex));
    }

    public Task PlayCard(string code, int playerIndex, Guid cardId, string? row, Guid? targetId)
    {
        CombatRow? rowEnum = null;
        if (!string.IsNullOrEmpty(row) && Enum.TryParse<CombatRow>(row, true, out var r))
            rowEnum = r;
        return ActAndBroadcast(code, playerIndex, (m, e) => e.PlayCard(m, playerIndex, cardId, rowEnum, targetId));
    }

    public Task Pass(string code, int playerIndex) =>
        ActAndBroadcast(code, playerIndex, (m, e) => e.Pass(m, playerIndex));

    public Task UseLeader(string code, int playerIndex) =>
        ActAndBroadcast(code, playerIndex, (m, e) => e.UseLeader(m, playerIndex));

    public Task Mulligan(string code, int playerIndex, Guid cardId) =>
        ActAndBroadcast(code, playerIndex, (m, e) => e.Mulligan(m, playerIndex, cardId));

    public Task FinishMulligan(string code, int playerIndex) =>
        ActAndBroadcast(code, playerIndex, (m, e) => e.FinishMulligan(m, playerIndex));

    public Task ResolvePending(string code, int playerIndex, Guid? targetId, string? row)
    {
        CombatRow? rowEnum = null;
        if (!string.IsNullOrEmpty(row) && Enum.TryParse<CombatRow>(row, true, out var r))
            rowEnum = r;
        return ActAndBroadcast(code, playerIndex, (m, e) => e.ResolvePending(m, playerIndex, targetId, rowEnum));
    }

    public Task ChooseWhoStarts(string code, int playerIndex, int startingPlayer) =>
        ActAndBroadcast(code, playerIndex, (m, e) => e.ChooseWhoStarts(m, playerIndex, startingPlayer));

    /// <summary>Client may call; server RoomWatch also advances RoundEnd after a pause.</summary>
    public Task ContinueRound(string code, int playerIndex) =>
        ActAndBroadcast(code, playerIndex, (m, e) =>
        {
            if (m.Phase == MatchPhase.RoundEnd)
                e.ContinueAfterRound(m);
        });

    private async Task ActAndBroadcast(string code, int playerIndex, Action<MatchState, Game.Engine.RulesEngine> action)
    {
        code = code.Trim().ToUpperInvariant();
        try
        {
            RequireSeat(code, playerIndex);
            sessions.ActRoom(code, playerIndex, action);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
            return;
        }

        var room = sessions.GetRoom(code)!;
        if (room.Match.Phase == MatchPhase.MatchEnd)
        {
            try { accounts.RecordOnlineMatch(room.HostAccountId, room.GuestAccountId, room.Match); }
            catch { /* never block broadcast */ }
        }

        await BroadcastBoard(room);

        // Server-owned RoundEnd → next round after banner pause (avoids dual-client races).
        if (room.Match.Phase == MatchPhase.RoundEnd)
            RoomWatchService.ScheduleRoundContinue(code);
    }

    internal async Task BroadcastBoard(GameRoom room)
    {
        if (room.HostConnectionId != null)
        {
            try { await Clients.Client(room.HostConnectionId).SendAsync("MatchUpdated", BuildUpdate(room, 0)); }
            catch { /* host hub dropped */ }
        }
        if (room.GuestConnectionId != null)
        {
            try { await Clients.Client(room.GuestConnectionId).SendAsync("MatchUpdated", BuildUpdate(room, 1)); }
            catch { /* guest hub dropped */ }
        }
    }

    private MatchBoardUpdate BuildUpdate(GameRoom room, int viewerIndex)
    {
        var host = BoardProfileFactory.FromAccountId(accounts, room.HostAccountId, room.Match.Players[0].Name);
        var guest = BoardProfileFactory.FromAccountId(accounts, room.GuestAccountId, room.Match.Players[1].Name);
        var you = viewerIndex == 0 ? host : guest;
        var opp = viewerIndex == 0 ? guest : host;
        return new MatchBoardUpdate(MatchView.From(room.Match, viewerIndex), you, opp, room.StateRevision);
    }
}

public record RoomCreatedDto(string Code, int PlayerIndex);
public record JoinedRoomDto(string Code, int PlayerIndex, Guid MatchId);
public record MatchBoardUpdate(MatchView View, BoardProfile You, BoardProfile Opponent, long Revision = 0);
