using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Game.Services;

/// <summary>
/// Client-safe projection of match state (hides opponent hand/deck contents).
/// </summary>
public sealed class MatchView
{
    public Guid MatchId { get; init; }
    public GameMode Mode { get; init; }
    public MatchPhase Phase { get; init; }
    public int RoundNumber { get; init; }
    public int ActivePlayer { get; init; }
    public int ViewerIndex { get; init; }
    public int? Winner { get; init; }
    public string? LastMessage { get; init; }
    public Guid? LastPlayedCardId { get; init; }
    public int? LastActionPlayer { get; init; }
    public CombatRow? LastPlayedRow { get; init; }
    public string? LastPlayedCardName { get; init; }
    public RoundResultView? LastRound { get; init; }
    public List<RoundResultView> RoundHistory { get; init; } = [];
    public WeatherEffect MeleeWeather { get; init; }
    public WeatherEffect RangedWeather { get; init; }
    public WeatherEffect SiegeWeather { get; init; }
    public PendingAction? Pending { get; init; }
    /// <summary>Cards the viewer can pick for their own pending action (discard, deck, revealed hand…).</summary>
    public List<CardView> PendingOptions { get; init; } = [];
    public PlayerView You { get; init; } = null!;
    public PlayerView Opponent { get; init; } = null!;
    public List<string> Log { get; init; } = [];
    public bool IsYourTurn { get; init; }
    public bool IsDraw => Phase == MatchPhase.MatchEnd && Winner is null;
    public bool LastActionByOpponent => LastActionPlayer is int a && a != ViewerIndex;

    public static MatchView From(MatchState match, int viewerIndex)
    {
        var you = match.Players[viewerIndex];
        var opp = match.Players[1 - viewerIndex];
        static RoundResultView ToRoundView(RoundResult rr, int viewerIndex) => new()
        {
            RoundNumber = rr.RoundNumber,
            YourScore = viewerIndex == 0 ? rr.Score0 : rr.Score1,
            OppScore = viewerIndex == 0 ? rr.Score1 : rr.Score0,
            YouWon = rr.WinnerIndex == viewerIndex,
            IsDraw = rr.WinnerIndex is null
        };

        RoundResultView? roundView = match.LastRoundResult is { } rr
            ? ToRoundView(rr, viewerIndex)
            : null;

        string? lastName = null;
        if (match.LastPlayedCardId is Guid id)
        {
            lastName = match.Players
                .SelectMany(p => p.AllBoardCards().Concat(p.Discard).Concat(p.Hand))
                .FirstOrDefault(c => c.InstanceId == id)?.Name;
        }

        return new MatchView
        {
            MatchId = match.MatchId,
            Mode = match.Mode,
            Phase = match.Phase,
            RoundNumber = match.RoundNumber,
            ActivePlayer = match.ActivePlayer,
            ViewerIndex = viewerIndex,
            Winner = match.Winner,
            LastMessage = match.LastMessage,
            LastPlayedCardId = match.LastPlayedCardId,
            LastActionPlayer = match.LastActionPlayer,
            LastPlayedRow = match.LastPlayedRow,
            LastPlayedCardName = lastName,
            LastRound = roundView,
            RoundHistory = match.RoundHistory.Select(r => ToRoundView(r, viewerIndex)).ToList(),
            MeleeWeather = match.MeleeWeather,
            RangedWeather = match.RangedWeather,
            SiegeWeather = match.SiegeWeather,
            Pending = match.Pending is { } pending && pending.PlayerIndex != viewerIndex
                ? new PendingAction { Kind = pending.Kind, PlayerIndex = pending.PlayerIndex, Remaining = pending.Remaining }
                : match.Pending?.Clone(),
            PendingOptions = BuildPendingOptions(match, viewerIndex),
            Log = [.. match.Log],
            IsYourTurn = match.Phase == MatchPhase.Playing && match.ActivePlayer == viewerIndex && match.Pending is null
                         || match.Pending?.PlayerIndex == viewerIndex
                         || match.Phase == MatchPhase.Mulligan,
            You = PlayerView.From(you, match, hideHand: false),
            Opponent = PlayerView.From(opp, match, hideHand: true)
        };
    }

    private static List<CardView> BuildPendingOptions(MatchState match, int viewerIndex)
    {
        if (match.Pending is not { } pending || pending.PlayerIndex != viewerIndex)
            return [];

        var options = new List<CardView>();
        foreach (var id in pending.ValidTargets)
        {
            foreach (var p in match.Players)
            {
                var card = p.Hand.Concat(p.Deck).Concat(p.Discard).Concat(p.AllBoardCards())
                    .FirstOrDefault(c => c.InstanceId == id);
                if (card is null) continue;
                options.Add(CardView.From(card, p, match));
                break;
            }
        }

        return options;
    }
}

public sealed class RoundResultView
{
    public int RoundNumber { get; init; }
    public int YourScore { get; init; }
    public int OppScore { get; init; }
    public bool YouWon { get; init; }
    public bool IsDraw { get; init; }
}

public sealed class PlayerView
{
    public int Index { get; init; }
    public string Name { get; init; } = "";
    public Faction Faction { get; init; }
    public int RoundWins { get; init; }
    public int Lives { get; init; }
    public bool HasPassed { get; init; }
    public bool LeaderUsed { get; init; }
    public bool LeaderCancelled { get; init; }
    public bool LeaderPassive { get; init; }
    public string? LeaderName { get; init; }
    public string? LeaderId { get; init; }
    public string? LeaderDescription { get; init; }
    public int HandCount { get; init; }
    public int DeckCount { get; init; }
    public int DiscardCount { get; init; }
    public int MulliganLeft { get; init; }
    public int TotalScore { get; init; }
    public int MeleeScore { get; init; }
    public int RangedScore { get; init; }
    public int SiegeScore { get; init; }
    public List<CardView> Hand { get; init; } = [];
    public List<CardView> Melee { get; init; } = [];
    public List<CardView> Ranged { get; init; } = [];
    public List<CardView> Siege { get; init; } = [];
    public List<CardView> Discard { get; init; } = [];
    public HashSet<CombatRow> HornRows { get; init; } = [];

    public int RowScore(CombatRow row) => row switch
    {
        CombatRow.Melee => MeleeScore,
        CombatRow.Ranged => RangedScore,
        CombatRow.Siege => SiegeScore,
        _ => 0
    };

    public static PlayerView From(PlayerState p, MatchState match, bool hideHand)
    {
        return new PlayerView
        {
            Index = p.Index,
            Name = p.Name,
            Faction = p.Faction,
            RoundWins = p.RoundWins,
            Lives = p.Lives,
            HasPassed = p.HasPassed,
            LeaderUsed = p.LeaderUsed,
            LeaderCancelled = p.LeaderCancelled,
            LeaderPassive = p.Leader is not null && LeaderAbilities.IsPassive(p.Leader.LeaderAbility),
            LeaderName = p.Leader?.Name,
            LeaderId = p.Leader?.DefinitionId,
            LeaderDescription = p.Leader?.Description,
            HandCount = p.Hand.Count,
            DeckCount = p.Deck.Count,
            DiscardCount = p.Discard.Count,
            MulliganLeft = p.MulliganLeft,
            TotalScore = ScoreCalculator.GetTotalScore(p, match),
            MeleeScore = ScoreCalculator.GetRowScore(p, CombatRow.Melee, match),
            RangedScore = ScoreCalculator.GetRowScore(p, CombatRow.Ranged, match),
            SiegeScore = ScoreCalculator.GetRowScore(p, CombatRow.Siege, match),
            Hand = hideHand ? [] : p.Hand.Select(c => CardView.From(c, p, match)).ToList(),
            Melee = p.Rows[CombatRow.Melee].Select(c => CardView.From(c, p, match)).ToList(),
            Ranged = p.Rows[CombatRow.Ranged].Select(c => CardView.From(c, p, match)).ToList(),
            Siege = p.Rows[CombatRow.Siege].Select(c => CardView.From(c, p, match)).ToList(),
            Discard = p.Discard.Select(c => CardView.From(c, p, match)).ToList(),
            HornRows = RulesEngine.BoardRows.Where(r => ScoreCalculator.HasHorn(p, r)).ToHashSet()
        };
    }
}

public sealed class CardView
{
    public Guid InstanceId { get; init; }
    public string DefinitionId { get; init; } = "";
    public string Name { get; init; } = "";
    public int BaseStrength { get; init; }
    public int CurrentStrength { get; init; }
    public CombatRow Row { get; init; }
    public bool IsHero { get; init; }
    public CardType Type { get; init; }
    public List<Ability> Abilities { get; init; } = [];
    public Faction Faction { get; init; }
    public string Description { get; init; } = "";

    public static CardView From(CardInstance c, PlayerState owner, MatchState match) => new()
    {
        InstanceId = c.InstanceId,
        DefinitionId = c.DefinitionId,
        Name = c.Name,
        BaseStrength = c.BaseStrength,
        CurrentStrength = c.Type == CardType.Unit ? ScoreCalculator.GetCardStrength(c, owner, match) : 0,
        Row = c.PlacedRow,
        IsHero = c.IsHero,
        Type = c.Type,
        Abilities = c.Abilities.ToList(),
        Faction = c.Faction,
        Description = c.Description
    };
}
