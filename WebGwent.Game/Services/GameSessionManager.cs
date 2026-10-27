using System.Collections.Concurrent;
using WebGwent.Game.Bot;
using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Game.Services;

public sealed class GameRoom
{
    public required string Code { get; init; }
    public required MatchState Match { get; set; }
    public string? HostConnectionId { get; set; }
    public string? GuestConnectionId { get; set; }
    public Guid? HostAccountId { get; set; }
    public Guid? GuestAccountId { get; set; }
    public Faction HostFaction { get; set; }
    public Faction GuestFaction { get; set; }
    public DeckList? HostDeck { get; set; }
    public DeckList? GuestDeck { get; set; }
    public bool GuestJoined { get; set; }
    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Monotonic board version for clients to ignore stale GetBoard pulls.</summary>
    public long StateRevision { get; set; }
}

public sealed class GameSessionManager
{
    private readonly ConcurrentDictionary<Guid, MatchState> _local = new();
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly CardCatalog _catalog;
    private readonly DeckLibrary _decks;
    private readonly MatchFactory _factory;
    private readonly RulesEngine _engine;
    private readonly ConcurrentDictionary<BotDifficulty, BotAI> _bots = new();
    private readonly object _lock = new();

    public GameSessionManager(string? deckStoragePath = null, CardCatalog? catalog = null)
    {
        _catalog = catalog ?? CardCatalog.LoadEmbedded();
        _decks = new DeckLibrary(_catalog, deckStoragePath);
        _engine = new RulesEngine();
        _factory = new MatchFactory(_catalog, _engine);
    }

    private BotAI BotFor(BotDifficulty difficulty) =>
        _bots.GetOrAdd(difficulty, d => new BotAI(_engine, difficulty: d));

    public CardCatalog Catalog => _catalog;
    public DeckLibrary Decks => _decks;
    public RulesEngine Engine => _engine;

    public MatchState CreateLocal(GameMode mode, Faction faction0, Faction faction1, string name0 = "Player 1", string name1 = "Player 2", BotDifficulty botDifficulty = BotDifficulty.High) =>
        CreateLocal(mode, PresetDecks.ForFaction(faction0), PresetDecks.ForFaction(faction1), name0, name1, botDifficulty);

    public MatchState CreateLocal(GameMode mode, DeckList deck0, DeckList deck1, string name0 = "Player 1", string name1 = "Player 2", BotDifficulty botDifficulty = BotDifficulty.High)
    {
        var match = _factory.Create(
            deck0,
            deck1,
            mode,
            name0,
            mode == GameMode.VsBot ? "Bot" : name1,
            player1IsBot: mode == GameMode.VsBot);
        match.BotDifficulty = botDifficulty;

        _local[match.MatchId] = match;

        if (mode == GameMode.VsBot)
            RunBotIfNeeded(match);

        return match;
    }

    public MatchState? GetLocal(Guid id) => _local.TryGetValue(id, out var m) ? m : null;

    public MatchView GetView(Guid matchId, int viewerIndex)
    {
        var match = GetLocal(matchId) ?? throw new KeyNotFoundException("Match not found.");
        return MatchView.From(match, viewerIndex);
    }

    public static readonly TimeSpan TurnTimeout = TimeSpan.FromMinutes(3);

    public MatchView ActLocal(Guid matchId, int playerIndex, Action<MatchState, RulesEngine> action, bool autoBot = true)
    {
        lock (_lock)
        {
            var match = GetLocal(matchId) ?? throw new KeyNotFoundException("Match not found.");
            ApplyTimeoutPass(match);
            action(match, _engine);
            if (autoBot)
                RunBotIfNeeded(match);
            return MatchView.From(match, match.Mode == GameMode.Hotseat ? match.ActivePlayer : playerIndex);
        }
    }

    /// <summary>
    /// Performs a single bot decision (one card / pass / pending resolve). Returns false when the bot is idle.
    /// </summary>
    public bool StepBotLocal(Guid matchId)
    {
        lock (_lock)
        {
            var match = GetLocal(matchId) ?? throw new KeyNotFoundException("Match not found.");
            return StepBotOnce(match);
        }
    }

    public MatchView ContinueAfterRoundLocal(Guid matchId, int viewerIndex)
    {
        lock (_lock)
        {
            var match = GetLocal(matchId) ?? throw new KeyNotFoundException("Match not found.");
            _engine.ContinueAfterRound(match);
            return MatchView.From(match, viewerIndex);
        }
    }

    /// <summary>
    /// If the active player exceeded the turn timeout, auto-pass them.
    /// </summary>
    public void ApplyTimeoutPass(MatchState match)
    {
        if (match.Phase != MatchPhase.Playing || match.Pending != null)
            return;
        if (DateTime.UtcNow - match.LastActionUtc < TurnTimeout)
            return;
        var p = match.CurrentPlayer;
        if (p.HasPassed) return;
        try
        {
            _engine.Pass(match, p.Index);
            match.AddLog($"{p.Name} auto-passed (turn timeout).");
        }
        catch
        {
            /* ignore */
        }
    }

    public string CreateRoom(Faction hostFaction, string hostConnectionId, string hostName, DeckList? hostDeck = null, Guid? hostAccountId = null)
    {
        var deck = hostDeck ?? PresetDecks.ForFaction(hostFaction);
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var code = GenerateCode();
            // Placeholder match until guest joins (replaced in JoinRoom).
            var placeholder = new MatchState
            {
                Mode = GameMode.Online,
                Phase = MatchPhase.Mulligan,
                Players =
                [
                    new PlayerState { Index = 0, Name = hostName, Faction = deck.Faction },
                    new PlayerState { Index = 1, Name = "Waiting...", Faction = Faction.Neutral }
                ]
            };
            var room = new GameRoom
            {
                Code = code,
                HostFaction = deck.Faction,
                HostConnectionId = hostConnectionId,
                HostDeck = deck,
                HostAccountId = hostAccountId,
                Match = placeholder
            };
            if (_rooms.TryAdd(code, room))
                return code;
        }
        throw new InvalidOperationException("Could not allocate room code.");
    }

    public GameRoom JoinRoom(string code, Faction guestFaction, string guestConnectionId, string guestName, DeckList? guestDeck = null, Guid? guestAccountId = null)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(code, out var room))
                throw new InvalidOperationException("Room not found.");
            if (room.GuestJoined)
                throw new InvalidOperationException("Room is full.");

            var gDeck = guestDeck ?? PresetDecks.ForFaction(guestFaction);
            var hDeck = room.HostDeck ?? PresetDecks.ForFaction(room.HostFaction);

            room.GuestFaction = gDeck.Faction;
            room.GuestDeck = gDeck;
            room.GuestConnectionId = guestConnectionId;
            room.GuestAccountId = guestAccountId;
            room.GuestJoined = true;
            room.Match = _factory.Create(
                hDeck,
                gDeck,
                GameMode.Online,
                room.Match.Players[0].Name,
                guestName);
            room.Match.Players[0].ConnectionId = room.HostConnectionId;
            room.Match.Players[1].ConnectionId = guestConnectionId;
            _local[room.Match.MatchId] = room.Match;
            room.StateRevision++;
            room.LastActivityUtc = DateTime.UtcNow;
            return room;
        }
    }

    public GameRoom? GetRoom(string code) =>
        _rooms.TryGetValue(code, out var r) ? r : null;

    public IReadOnlyCollection<GameRoom> AllRooms => _rooms.Values.ToArray();

    public GameRoom? FindRoomByConnection(string connectionId) =>
        _rooms.Values.FirstOrDefault(r =>
            r.HostConnectionId == connectionId || r.GuestConnectionId == connectionId);

    public void RebindConnection(string code, int playerIndex, string connectionId)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(code, out var room)) return;
            if (playerIndex == 0) room.HostConnectionId = connectionId;
            else room.GuestConnectionId = connectionId;
            if (room.Match.Players.Length > playerIndex)
                room.Match.Players[playerIndex].ConnectionId = connectionId;
            room.LastActivityUtc = DateTime.UtcNow;
        }
    }

    /// <summary>Clear a dropped SignalR connection. Returns affected seats (code, playerIndex).</summary>
    public List<(string Code, int PlayerIndex)> ClearConnection(string connectionId)
    {
        var hit = new List<(string, int)>();
        lock (_lock)
        {
            foreach (var room in _rooms.Values)
            {
                if (string.Equals(room.HostConnectionId, connectionId, StringComparison.Ordinal))
                {
                    room.HostConnectionId = null;
                    hit.Add((room.Code, 0));
                }
                if (string.Equals(room.GuestConnectionId, connectionId, StringComparison.Ordinal))
                {
                    room.GuestConnectionId = null;
                    hit.Add((room.Code, 1));
                }
            }
        }
        return hit;
    }

    public (bool HostOnline, bool GuestOnline) GetSeatOnline(string code)
    {
        var room = GetRoom(code);
        if (room is null) return (false, false);
        return (
            !string.IsNullOrEmpty(room.HostConnectionId),
            !string.IsNullOrEmpty(room.GuestConnectionId));
    }

    /// <summary>Advance RoundEnd → next round. Returns false if not in RoundEnd.</summary>
    public bool TryContinueRound(string code)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(code, out var room) || !room.GuestJoined)
                return false;
            if (room.Match.Phase != MatchPhase.RoundEnd)
                return false;
            _engine.ContinueAfterRound(room.Match);
            room.StateRevision++;
            room.LastActivityUtc = DateTime.UtcNow;
            return true;
        }
    }

    /// <summary>Apply AFK turn timeout if due. Returns true when state changed.</summary>
    public bool TryApplyTurnTimeout(string code)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(code, out var room) || !room.GuestJoined)
                return false;
            if (room.Match.Phase != MatchPhase.Playing || room.Match.Pending != null)
                return false;
            if (DateTime.UtcNow - room.Match.LastActionUtc < TurnTimeout)
                return false;
            var phaseBefore = room.Match.Phase;
            var activeBefore = room.Match.ActivePlayer;
            ApplyTimeoutPass(room.Match);
            if (room.Match.Phase == phaseBefore && room.Match.ActivePlayer == activeBefore)
                return false;
            room.StateRevision++;
            room.LastActivityUtc = DateTime.UtcNow;
            return true;
        }
    }

    public int PruneIdleRooms(TimeSpan maxIdle)
    {
        var removed = 0;
        var cutoff = DateTime.UtcNow - maxIdle;
        foreach (var kv in _rooms)
        {
            if (kv.Value.LastActivityUtc >= cutoff) continue;
            if (_rooms.TryRemove(kv.Key, out var room))
            {
                _local.TryRemove(room.Match.MatchId, out _);
                removed++;
            }
        }
        return removed;
    }

    public MatchView ActRoom(string code, int playerIndex, Action<MatchState, RulesEngine> action)
    {
        lock (_lock)
        {
            var room = GetRoom(code) ?? throw new InvalidOperationException("Room not found.");
            if (!room.GuestJoined)
                throw new InvalidOperationException("Waiting for opponent.");
            ApplyTimeoutPass(room.Match);
            action(room.Match, _engine);
            // Leave RoundEnd for clients (banner). Server RoomWatch continues after a short pause.
            room.StateRevision++;
            room.LastActivityUtc = DateTime.UtcNow;
            return MatchView.From(room.Match, playerIndex);
        }
    }

    private void RunBotIfNeeded(MatchState match)
    {
        var guard = 0;
        while (guard++ < 20 && StepBotOnce(match))
        {
            /* keep stepping until human turn / match end */
        }
    }

    private bool StepBotOnce(MatchState match)
    {
        if (match.Phase is MatchPhase.MatchEnd or MatchPhase.RoundEnd)
            return false;

        if (match.Pending != null)
        {
            var p = match.Players[match.Pending.PlayerIndex];
            if (!p.IsBot) return false;
            BotFor(match.BotDifficulty).TakeTurn(match, p.Index);
            return true;
        }

        if (match.Phase == MatchPhase.Mulligan)
        {
            var bot = match.Players.FirstOrDefault(p => p.IsBot && p.MulliganLeft > 0);
            if (bot is not null)
            {
                BotFor(match.BotDifficulty).TakeTurn(match, bot.Index);
                return true;
            }

            // Finish bot mulligan once human is done replacing.
            if (match.Players.All(p => p.MulliganLeft == 0 || p.IsBot))
            {
                var unfinished = match.Players.Where(p => p.IsBot).ToList();
                if (unfinished.Count == 0) return false;
                // FinishMulligan is idempotent enough after BotAI already called it when MulliganLeft hits 0;
                // only finish bots that still need the phase transition.
                foreach (var b in unfinished)
                {
                    try { _engine.FinishMulligan(match, b.Index); }
                    catch { /* already finished */ }
                }
                return match.Phase != MatchPhase.Mulligan || match.Pending != null;
            }

            return false;
        }

        if (match.Phase != MatchPhase.Playing)
            return false;

        var current = match.CurrentPlayer;
        if (!current.IsBot || current.HasPassed)
            return false;

        BotFor(match.BotDifficulty).TakeTurn(match, current.Index);
        return true;
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 5).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
