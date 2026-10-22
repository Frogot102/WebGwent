namespace WebGwent.Game.Models;

public sealed class MatchState
{
    public Guid MatchId { get; init; } = Guid.NewGuid();
    public GameMode Mode { get; set; } = GameMode.Hotseat;
    public BotDifficulty BotDifficulty { get; set; } = BotDifficulty.High;
    public MatchPhase Phase { get; set; } = MatchPhase.Mulligan;
    public int RoundNumber { get; set; } = 1;
    public int ActivePlayer { get; set; }
    public int StartingPlayer { get; set; }
    public PlayerState[] Players { get; set; } = [];
    public WeatherEffect MeleeWeather { get; set; } = WeatherEffect.None;
    public WeatherEffect RangedWeather { get; set; } = WeatherEffect.None;
    public WeatherEffect SiegeWeather { get; set; } = WeatherEffect.None;
    public int? Winner { get; set; }
    public string? LastMessage { get; set; }
    public Guid? LastPlayedCardId { get; set; }
    public int? LastActionPlayer { get; set; }
    public CombatRow? LastPlayedRow { get; set; }
    public RoundResult? LastRoundResult { get; set; }
    public List<RoundResult> RoundHistory { get; set; } = [];
    public PendingAction? Pending { get; set; }
    public List<string> Log { get; set; } = [];
    public bool ScoiataelChoseStart { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActionUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Both players lost their last crystal in the same round.</summary>
    public bool IsDraw => Phase == MatchPhase.MatchEnd && Winner is null;

    public PlayerState CurrentPlayer => Players[ActivePlayer];
    public PlayerState Opponent => Players[1 - ActivePlayer];

    public void AddLog(string message)
    {
        LastMessage = message;
        Log.Add($"[R{RoundNumber}] {message}");
        if (Log.Count > 80)
            Log.RemoveAt(0);
        LastActionUtc = DateTime.UtcNow;
    }

    public MatchState DeepClone()
    {
        return new MatchState
        {
            MatchId = MatchId,
            Mode = Mode,
            BotDifficulty = BotDifficulty,
            Phase = Phase,
            RoundNumber = RoundNumber,
            ActivePlayer = ActivePlayer,
            StartingPlayer = StartingPlayer,
            Players = Players.Select(p => p.DeepClone()).ToArray(),
            MeleeWeather = MeleeWeather,
            RangedWeather = RangedWeather,
            SiegeWeather = SiegeWeather,
            Winner = Winner,
            LastMessage = LastMessage,
            LastPlayedCardId = LastPlayedCardId,
            LastActionPlayer = LastActionPlayer,
            LastPlayedRow = LastPlayedRow,
            LastRoundResult = LastRoundResult?.Clone(),
            RoundHistory = RoundHistory.Select(r => r.Clone()).ToList(),
            Pending = Pending?.Clone(),
            Log = [.. Log],
            ScoiataelChoseStart = ScoiataelChoseStart,
            CreatedUtc = CreatedUtc,
            LastActionUtc = LastActionUtc
        };
    }
}

public sealed class RoundResult
{
    public int RoundNumber { get; init; }
    public int Score0 { get; init; }
    public int Score1 { get; init; }
    public int? WinnerIndex { get; init; }

    public RoundResult Clone() => new()
    {
        RoundNumber = RoundNumber,
        Score0 = Score0,
        Score1 = Score1,
        WinnerIndex = WinnerIndex
    };
}

/// <summary>
/// When a card needs a follow-up choice (medic target, decoy target, agile row, etc.).
/// </summary>
public sealed class PendingAction
{
    public required PendingKind Kind { get; init; }
    public int PlayerIndex { get; init; }
    public Guid SourceCardId { get; init; }
    public List<Guid> ValidTargets { get; set; } = [];
    public List<CombatRow> ValidRows { get; set; } = [];
    /// <summary>How many more picks this action needs (Eredin: Destroyer of Worlds discards two).</summary>
    public int Remaining { get; set; } = 1;

    /// <summary>Whether the player may resolve it without picking anything.</summary>
    public bool CanSkip => Kind is PendingKind.ChooseMedicTarget
        or PendingKind.RevealOpponentCards
        or PendingKind.ChooseOwnDiscardCard
        or PendingKind.ChooseEnemyDiscardCard
        or PendingKind.ChooseDeckCard
        or PendingKind.ChooseDeckWeather;

    public PendingAction Clone() => new()
    {
        Kind = Kind,
        PlayerIndex = PlayerIndex,
        SourceCardId = SourceCardId,
        ValidTargets = [.. ValidTargets],
        ValidRows = [.. ValidRows],
        Remaining = Remaining
    };
}

public enum PendingKind
{
    ChooseAgileRow,
    ChooseMedicTarget,
    ChooseDecoyTarget,
    ChooseWhoStarts,
    ChooseHornRow,
    ChooseMardroemeRow,
    ChooseOwnDiscardCard,    // Eredin: Bringer of Death
    ChooseEnemyDiscardCard,  // Emhyr: The Relentless
    ChooseHandDiscard,       // Eredin: Destroyer of Worlds, step 1
    ChooseDeckCard,          // Eredin: Destroyer of Worlds, step 2
    ChooseDeckWeather,       // Eredin: King of the Wild Hunt
    RevealOpponentCards      // Emhyr: Emperor of Nilfgaard
}
