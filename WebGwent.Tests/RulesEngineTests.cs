using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Tests;

public class RulesEngineTests
{
    private readonly CardCatalog _catalog = CardCatalog.LoadEmbedded();
    private readonly RulesEngine _engine = new(new Random(42));

    private MatchState NewMatch(Faction f0 = Faction.NorthernRealms, Faction f1 = Faction.Nilfgaard)
    {
        var factory = new MatchFactory(_catalog, _engine);
        return factory.Create(PresetDecks.ForFaction(f0), PresetDecks.ForFaction(f1), GameMode.Hotseat);
    }

    private static void SkipMulligan(RulesEngine engine, MatchState match)
    {
        engine.FinishMulligan(match, 0);
        engine.FinishMulligan(match, 1);
        if (match.Pending?.Kind == PendingKind.ChooseWhoStarts)
            engine.ChooseWhoStarts(match, match.Pending.PlayerIndex, 0);
    }

    [Fact]
    public void Catalog_Loads_AllFactions()
    {
        Assert.True(_catalog.All.Count > 100);
        Assert.Contains(_catalog.All.Values, c => c.Faction == Faction.Skellige);
        Assert.Contains(_catalog.All.Values, c => c.Type == CardType.Leader);
    }

    [Fact]
    public void PresetDecks_AreValid()
    {
        foreach (var deck in PresetDecks.All())
        {
            var result = DeckValidator.Validate(deck, _catalog);
            Assert.True(result.IsValid, string.Join("; ", result.Errors));
        }
    }

    [Fact]
    public void Match_Starts_In_Mulligan()
    {
        var match = NewMatch();
        Assert.Equal(MatchPhase.Mulligan, match.Phase);
        Assert.Equal(10, match.Players[0].Hand.Count);
        Assert.Equal(10, match.Players[1].Hand.Count);
    }

    [Fact]
    public void Playing_Unit_Adds_To_Row_And_Score()
    {
        var match = NewMatch();
        SkipMulligan(_engine, match);
        Assert.Equal(MatchPhase.Playing, match.Phase);

        var player = match.CurrentPlayer;
        var unit = player.Hand.First(c => c.Type == CardType.Unit && !c.Abilities.Contains(Ability.Spy) && !c.Abilities.Contains(Ability.Agile) && !c.Abilities.Contains(Ability.Medic));
        var row = unit.PlacedRow == CombatRow.None ? CombatRow.Melee : unit.PlacedRow;
        _engine.PlayCard(match, player.Index, unit.InstanceId, row);

        Assert.Contains(match.Players[player.Index].Rows[row], c => c.InstanceId == unit.InstanceId);
        Assert.True(ScoreCalculator.GetTotalScore(match.Players[player.Index], match) > 0);
    }

    [Fact]
    public void Weather_Frost_Sets_Melee_To_One()
    {
        var match = NewMatch();
        SkipMulligan(_engine, match);
        var p = match.CurrentPlayer;
        var unit = p.Hand.First(c => c.Type == CardType.Unit && c.PlacedRow == CombatRow.Melee && !c.IsHero && !c.Abilities.Contains(Ability.Spy));
        _engine.PlayCard(match, p.Index, unit.InstanceId);

        // Opponent plays frost if available, else inject
        var opp = match.CurrentPlayer;
        var frost = opp.Hand.FirstOrDefault(c => c.Abilities.Contains(Ability.WeatherFrost));
        if (frost is null)
        {
            frost = CardInstance.FromDefinition(_catalog.Get("n_frost"));
            opp.Hand.Add(frost);
        }
        _engine.PlayCard(match, opp.Index, frost.InstanceId);

        Assert.Equal(WeatherEffect.Frost, match.MeleeWeather);
        var owner = match.Players[p.Index];
        var onBoard = owner.Rows[CombatRow.Melee].First(c => c.InstanceId == unit.InstanceId);
        Assert.Equal(1, ScoreCalculator.GetCardStrength(onBoard, owner, match));
    }

    [Fact]
    public void TightBond_Doubles_With_Two_Copies()
    {
        var match = NewMatch();
        SkipMulligan(_engine, match);
        var p = match.Players[match.ActivePlayer];

        // Clear hand and inject two Blue Stripes
        p.Hand.Clear();
        var a = CardInstance.FromDefinition(_catalog.Get("nr_blue_stripes1"));
        var b = CardInstance.FromDefinition(_catalog.Get("nr_blue_stripes2"));
        p.Hand.Add(a);
        p.Hand.Add(b);
        p.Faction = Faction.NorthernRealms;

        _engine.PlayCard(match, p.Index, a.InstanceId);
        // may switch turn
        match.ActivePlayer = p.Index;
        p.HasPassed = false;
        match.Players[1 - p.Index].HasPassed = true;
        _engine.PlayCard(match, p.Index, b.InstanceId);

        var score = ScoreCalculator.GetRowScore(p, CombatRow.Melee, match);
        Assert.Equal(16, score); // 4*2 + 4*2
    }

    [Fact]
    public void Spy_Draws_Two_And_Plays_On_Enemy()
    {
        var match = NewMatch();
        SkipMulligan(_engine, match);
        var p = match.CurrentPlayer;
        p.Hand.Clear();
        var spy = CardInstance.FromDefinition(_catalog.Get("nr_prince_stennis"));
        p.Hand.Add(spy);
        // Ensure deck has cards
        while (p.Deck.Count < 2)
            p.Deck.Add(CardInstance.FromDefinition(_catalog.Get("nr_ves")));

        var handBefore = p.Hand.Count;
        var deckBefore = p.Deck.Count;
        _engine.PlayCard(match, p.Index, spy.InstanceId);

        Assert.DoesNotContain(p.Rows[CombatRow.Melee], c => c.InstanceId == spy.InstanceId);
        Assert.Contains(match.Players[1 - p.Index].Rows[CombatRow.Melee], c => c.InstanceId == spy.InstanceId);
        Assert.Equal(deckBefore - 2, p.Deck.Count);
        Assert.Equal(handBefore - 1 + 2, p.Hand.Count);
    }

    [Fact]
    public void Both_Pass_Ends_Round()
    {
        var match = NewMatch();
        SkipMulligan(_engine, match);
        var first = match.ActivePlayer;
        _engine.Pass(match, first);
        _engine.Pass(match, match.ActivePlayer);
        Assert.True(
            match.Phase is MatchPhase.RoundEnd or MatchPhase.MatchEnd
            || match.RoundNumber >= 2
            || match.Players.Any(p => p.RoundWins > 0));
        if (match.Phase == MatchPhase.RoundEnd)
            _engine.ContinueAfterRound(match);
    }

    [Fact]
    public void Nilfgaard_Wins_Round_Draw()
    {
        var match = NewMatch(Faction.NorthernRealms, Faction.Nilfgaard);
        SkipMulligan(_engine, match);
        // Both pass with empty boards -> draw, Nilfgaard wins
        match.ActivePlayer = 0;
        _engine.Pass(match, 0);
        _engine.Pass(match, 1);
        Assert.Equal(1, match.Players[1].RoundWins);
        Assert.Equal(0, match.Players[0].RoundWins);
    }
}
