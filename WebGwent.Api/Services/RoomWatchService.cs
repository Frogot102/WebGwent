using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using WebGwent.Api.Accounts;
using WebGwent.Api.Hubs;
using WebGwent.Game.Models;
using WebGwent.Game.Services;

namespace WebGwent.Api.Services;

/// <summary>
/// Advances RoundEnd after a short pause and applies AFK turn timeouts without waiting for a client action.
/// </summary>
public sealed class RoomWatchService(
    GameSessionManager sessions,
    AccountStore accounts,
    IHubContext<GameHub> hubs,
    ILogger<RoomWatchService> log) : BackgroundService
{
    private static readonly ConcurrentDictionary<string, byte> PendingContinues = new(StringComparer.OrdinalIgnoreCase);
    public static readonly TimeSpan RoundEndPause = TimeSpan.FromMilliseconds(1800);
    public static readonly TimeSpan IdleRoomLifetime = TimeSpan.FromHours(2);

    public static void ScheduleRoundContinue(string code) =>
        PendingContinues.TryAdd(code.Trim().ToUpperInvariant(), 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "RoomWatch tick failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        foreach (var code in PendingContinues.Keys.ToArray())
        {
            if (ct.IsCancellationRequested) return;
            var room = sessions.GetRoom(code);
            if (room is null || room.Match.Phase != MatchPhase.RoundEnd)
            {
                PendingContinues.TryRemove(code, out _);
                continue;
            }

            // Wait until pause elapsed since last action (RoundEnd set LastActivity / LastActionUtc).
            if (DateTime.UtcNow - room.Match.LastActionUtc < RoundEndPause
                && DateTime.UtcNow - room.LastActivityUtc < RoundEndPause)
                continue;

            if (!sessions.TryContinueRound(code))
            {
                PendingContinues.TryRemove(code, out _);
                continue;
            }

            PendingContinues.TryRemove(code, out _);
            room = sessions.GetRoom(code);
            if (room is not null)
                await BroadcastAsync(room);
        }

        foreach (var room in sessions.AllRooms)
        {
            if (ct.IsCancellationRequested) return;
            if (!room.GuestJoined) continue;
            if (!sessions.TryApplyTurnTimeout(room.Code)) continue;

            var updated = sessions.GetRoom(room.Code);
            if (updated is null) continue;
            await BroadcastAsync(updated);
            if (updated.Match.Phase == MatchPhase.RoundEnd)
                ScheduleRoundContinue(updated.Code);
            if (updated.Match.Phase == MatchPhase.MatchEnd)
            {
                try { accounts.RecordOnlineMatch(updated.HostAccountId, updated.GuestAccountId, updated.Match); }
                catch { /* ignore */ }
            }
        }

        sessions.PruneIdleRooms(IdleRoomLifetime);
    }

    private async Task BroadcastAsync(GameRoom room)
    {
        if (room.HostConnectionId != null)
        {
            try
            {
                await hubs.Clients.Client(room.HostConnectionId)
                    .SendAsync("MatchUpdated", Build(room, 0));
            }
            catch { /* ignore */ }
        }
        if (room.GuestConnectionId != null)
        {
            try
            {
                await hubs.Clients.Client(room.GuestConnectionId)
                    .SendAsync("MatchUpdated", Build(room, 1));
            }
            catch { /* ignore */ }
        }
    }

    private MatchBoardUpdate Build(GameRoom room, int viewerIndex)
    {
        var host = BoardProfileFactory.FromAccountId(accounts, room.HostAccountId, room.Match.Players[0].Name);
        var guest = BoardProfileFactory.FromAccountId(accounts, room.GuestAccountId, room.Match.Players[1].Name);
        var you = viewerIndex == 0 ? host : guest;
        var opp = viewerIndex == 0 ? guest : host;
        return new MatchBoardUpdate(MatchView.From(room.Match, viewerIndex), you, opp, room.StateRevision);
    }
}
