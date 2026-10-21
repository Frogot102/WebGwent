using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Tests;

/// <summary>
/// Rules that follow The Witcher 3 Gwent: crystals, leaders, row Scorch, Muster, Mardroeme, Decoy.
/// </summary>
public class Tw3RulesTests
{
    private readonly CardCatalog _catalog = CardCatalog.LoadEmbedded();
    private readonly RulesEngine _engine = new(new Random(11));

    private MatchState Ready(DeckList d0, DeckList d1)
    {
        var match = new MatchFactory(_catalog, _engine).Create(d0, d1, GameMode.Hotseat);
        _engine.FinishMulligan(match, 0);
        _engine.FinishMulligan(match, 1);
        if (match.Pending?.Kind == PendingKind.ChooseWhoStarts)
            _engine.ChooseWhoStarts(match, match.Pending.PlayerIndex, 0);
        match.ActivePlayer = 0;
        return match;
    }

    private MatchState Ready() => Ready(PresetDecks.NorthernRealms(), PresetDecks.Monsters());

    private CardInstance Card(string id) => CardInstance.FromDefinition(_catalog.Get(id));

    private static void ClearBoard(MatchState match)
    {
        foreach (var p in match.Players)
        foreach (var row in p.Rows.Keys.ToList())
            p.Rows[row].Clear();
    }

    private static CardInstance Put(PlayerState p, CardInstance card, CombatRow? row = null)
    {
        card.PlacedRow = row ?? card.PlacedRow;
        p.Rows[card.PlacedRow].Add(card);
        return card;
    }

    [Fact]
    public void Drawn_Round_Costs_Both_Players_A_Crystal()
    {
        var match = Ready();
        _engine.Pass(match, 0);
        _engine.Pass(match, 1);

        Assert.Equal(1, match.Players[0].Lives);
        Assert.Equal(1, match.Players[1].Lives);
        Assert.Equal(MatchPhase.RoundEnd, match.Phase);
        Assert.Null(match.RoundHistory[0].WinnerIndex);
    }

    [Fact]
    public void Match_Ends_In_A_Draw_When_Both_Lose_The_Last_Crystal()
    {
        var match = Ready();
        match.Players[0].Lives = 1;
        match.Players[1].Lives = 1;
        _engine.Pass(match, 0);
        _engine.Pass(match, 1);

        Assert.Equal(MatchPhase.MatchEnd, match.Phase);
        Assert.Null(match.Winner);
        Assert.True(match.IsDraw);
    }

    [Fact]
    public void Match_Never_Goes_Past_Three_Rounds()
    {
        var match = Ready();
        // Round 1 draw, round 2 won by player 0 → player 1 is out.
        _engine.Pass(match, 0);
        _engine.Pass(match, 1);
        _engine.ContinueAfterRound(match);

        var p0 = match.Players[0];
        p0.Hand.Add(Card("nr_ves"));
        match.ActivePlayer = 0;
        _engine.PlayCard(match, 0, p0.Hand[^1].InstanceId);
        _engine.Pass(match, 1);
        _engine.Pass(match, 0);

        Assert.Equal(MatchPhase.MatchEnd, match.Phase);
        Assert.Equal(0, match.Winner);
        Assert.Equal(2, match.RoundNumber);
    }

    [Fact]
    public void Villentretenmerth_Only_Burns_The_Enemy_Row_When_It_Totals_Ten()
    {
        var match = Ready();
        ClearBoard(match);
        var me = match.Players[0];
        var enemy = match.Players[1];
        var myBig = Put(me, Card("nr_vernon_roche"));            // hero, untouchable anyway
        var myUnit = Put(me, Card("nr_ves"));                    // own 5 must survive
        var enemyWeak = Put(enemy, Card("mo_fiend"));            // 6 < 10 → nothing happens

        var villen = Card("n_villentretenmerth");
        me.Hand.Add(villen);
        _engine.PlayCard(match, 0, villen.InstanceId);
        Assert.Contains(enemy.Rows[CombatRow.Melee], c => c.InstanceId == enemyWeak.InstanceId);
        Assert.Contains(me.Rows[CombatRow.Melee], c => c.InstanceId == myUnit.InstanceId);

        // Now the enemy row is 6 + 5 = 11: the 6 burns, own units stay.
        Put(enemy, Card("mo_griffin"));
        var villen2 = Card("n_villentretenmerth");
        me.Hand.Add(villen2);
        match.ActivePlayer = 0;
        _engine.PlayCard(match, 0, villen2.InstanceId);
        Assert.DoesNotContain(enemy.Rows[CombatRow.Melee], c => c.InstanceId == enemyWeak.InstanceId);
        Assert.Contains(me.Rows[CombatRow.Melee], c => c.InstanceId == myUnit.InstanceId);
        Assert.Contains(me.Rows[CombatRow.Melee], c => c.InstanceId == myBig.InstanceId);
    }

    [Fact]
    public void Steel_Forged_Needs_Ten_Strength_On_The_Enemy_Siege_Row()
    {
        var deck = PresetDecks.NorthernRealms() with { LeaderId = "nr_leader_foltest_iron" };
        var match = Ready(deck, PresetDecks.Monsters());
        ClearBoard(match);
        var enemy = match.Players[1];
        var elemental = Put(enemy, Card("mo_earth_elemental")); // 6 siege

        _engine.UseLeader(match, 0);
        Assert.Contains(enemy.Rows[CombatRow.Siege], c => c.InstanceId == elemental.InstanceId);
        Assert.True(match.Players[0].LeaderUsed);
    }

    [Fact]
    public void Dandelion_Horns_The_Other_Units_In_His_Row()
    {
        var match = Ready();
        ClearBoard(match);
        var me = match.Players[0];
        var ves = Put(me, Card("nr_ves"));
        var dandelion = Put(me, Card("nr_dandelion"));

        Assert.Equal(10, ScoreCalculator.GetCardStrength(ves, me, match));
        Assert.Equal(2, ScoreCalculator.GetCardStrength(dandelion, me, match));
    }

    [Fact]
    public void Decoy_Returns_To_Discard_As_A_Special_And_Cannot_Be_Revived()
    {
        var match = Ready();
        ClearBoard(match);
        var me = match.Players[0];
        me.Hand.Clear();
        me.Discard.Clear();
        var ves = Put(me, Card("nr_ves"));
        var decoy = Card("n_decoy1");
        me.Hand.Add(decoy);

        _engine.PlayCard(match, 0, decoy.InstanceId, targetId: ves.InstanceId);
        Assert.Contains(me.Hand, c => c.InstanceId == ves.InstanceId);
        Assert.Contains(me.Rows[CombatRow.Melee], c => c.InstanceId == decoy.InstanceId);
        Assert.Equal(CardType.Special, decoy.Type);

        _engine.Pass(match, 1);
        _engine.Pass(match, 0);
        _engine.ContinueAfterRound(match);
        Assert.Contains(me.Discard, c => c.InstanceId == decoy.InstanceId);

        var medic = Card("nr_dun_banner");
        me.Hand.Add(medic);
        match.ActivePlayer = 0;
        _engine.PlayCard(match, 0, medic.InstanceId);
        Assert.True(match.Pending is null || !match.Pending.ValidTargets.Contains(decoy.InstanceId));
    }

    [Fact]
    public void Mardroeme_Turns_A_Berserker_Into_Vildkaarl()
    {
        var match = Ready(PresetDecks.Skellige(), PresetDecks.Monsters());
        ClearBoard(match);
        var me = match.Players[0];
        var berserker = Put(me, Card("sk_berserker1"));
        var mardroeme = Card("n_mardroeme");
        me.Hand.Add(mardroeme);

        _engine.PlayCard(match, 0, mardroeme.InstanceId, CombatRow.Melee);

        Assert.Equal("Vildkaarl", berserker.Name);
        Assert.Equal(14, ScoreCalculator.GetCardStrength(berserker, me, match));
        Assert.Contains(Ability.MoraleBoost, berserker.Abilities);
    }

    [Fact]
    public void Ermion_Transforms_Young_Berserkers_In_His_Row()
    {
        var match = Ready(PresetDecks.Skellige(), PresetDecks.Monsters());
        ClearBoard(match);
        var me = match.Players[0];
        var young1 = Put(me, Card("sk_young_berserker1"));
        var young2 = Put(me, Card("sk_young_berserker2"));
        var ermion = Card("sk_ermion");
        me.Hand.Add(ermion);

        _engine.PlayCard(match, 0, ermion.InstanceId);

        Assert.Equal("Young Vildkaarl", young1.Name);
        // Two Young Vildkaarls bond: 8 × 2 each.
        Assert.Equal(16, ScoreCalculator.GetCardStrength(young1, me, match));
        Assert.Equal(16, ScoreCalculator.GetCardStrength(young2, me, match));
    }

    [Fact]
    public void Transformed_Berserker_Reverts_In_The_Discard_Pile()
    {
        var match = Ready(PresetDecks.Skellige(), PresetDecks.Monsters());
        ClearBoard(match);
        var me = match.Players[0];
        var berserker = Put(me, Card("sk_berserker1"));
        me.MardroemeRows.Add(CombatRow.Melee);
        var trigger = Card("sk_berserker2");
        me.Hand.Add(trigger);
        _engine.PlayCard(match, 0, trigger.InstanceId);
        Assert.Equal(14, berserker.BaseStrength);

        _engine.Pass(match, 1);
        _engine.Pass(match, 0);
        _engine.ContinueAfterRound(match);
        var inDiscard = me.Discard.First(c => c.InstanceId == berserker.InstanceId);
        Assert.Equal("Berserker", inDiscard.Name);
        Assert.Equal(4, inDiscard.BaseStrength);
    }

    [Fact]
    public void Daisy_Of_The_Valley_Starts_With_Eleven_Cards()
    {
        var match = new MatchFactory(_catalog, _engine).Create(PresetDecks.Scoiatael(), PresetDecks.Monsters(), GameMode.Hotseat);
        Assert.Equal(11, match.Players[0].Hand.Count);
        Assert.True(match.Players[0].LeaderUsed); // passive — no button
    }

    [Fact]
    public void White_Flame_Cancels_The_Opponents_Leader_Including_Passives()
    {
        var nilfgaard = PresetDecks.Nilfgaard() with { LeaderId = "ng_leader_emhyr_white" };
        var match = new MatchFactory(_catalog, _engine).Create(nilfgaard, PresetDecks.Scoiatael(), GameMode.Hotseat);

        Assert.True(match.Players[1].LeaderCancelled);
        Assert.False(match.Players[1].CanUseLeader);
        Assert.Equal(10, match.Players[1].Hand.Count); // Daisy cancelled
    }

    [Fact]
    public void King_Of_Temeria_Plays_Fog_From_The_Deck()
    {
        var deck = PresetDecks.NorthernRealms() with { LeaderId = "nr_leader_foltest_king" };
        var match = Ready(deck, PresetDecks.Monsters());
        var me = match.Players[0];
        me.Deck.Add(Card("n_fog"));

        _engine.UseLeader(match, 0);

        Assert.Equal(WeatherEffect.Fog, match.RangedWeather);
        Assert.DoesNotContain(me.Deck, c => c.Abilities.Contains(Ability.WeatherFog));
        Assert.Contains(me.Discard, c => c.Abilities.Contains(Ability.WeatherFog));
    }

    [Fact]
    public void Crach_An_Craite_Shuffles_Both_Discard_Piles_Back()
    {
        var match = Ready(PresetDecks.Skellige(), PresetDecks.Monsters());
        match.Players[0].Discard.Add(Card("sk_olaf"));
        match.Players[1].Discard.Add(Card("mo_fiend"));

        _engine.UseLeader(match, 0);

        Assert.Empty(match.Players[0].Discard);
        Assert.Empty(match.Players[1].Discard);
        Assert.Contains(match.Players[1].Deck, c => c.DefinitionId == "mo_fiend");
    }

    [Fact]
    public void King_Bran_Halves_Weather_Instead_Of_Setting_It_To_One()
    {
        var deck = PresetDecks.Skellige() with { LeaderId = "sk_leader_bran" };
        var match = Ready(deck, PresetDecks.Monsters());
        ClearBoard(match);
        var olaf = Put(match.Players[0], Card("sk_olaf"));
        var fiend = Put(match.Players[1], Card("mo_fiend"));
        match.MeleeWeather = WeatherEffect.Frost;

        Assert.Equal(6, ScoreCalculator.GetCardStrength(olaf, match.Players[0], match));
        Assert.Equal(1, ScoreCalculator.GetCardStrength(fiend, match.Players[1], match));
    }

    [Fact]
    public void Destroyer_Of_Worlds_Discards_Two_Then_Picks_From_The_Deck()
    {
        var deck = PresetDecks.Monsters() with { LeaderId = "mo_leader_eredin_destroyer" };
        var match = Ready(deck, PresetDecks.NorthernRealms());
        var me = match.Players[0];
        var handBefore = me.Hand.Count;
        var wanted = me.Deck[^1];

        _engine.UseLeader(match, 0);
        Assert.Equal(PendingKind.ChooseHandDiscard, match.Pending!.Kind);
        _engine.ResolvePending(match, 0, targetId: me.Hand[0].InstanceId);
        Assert.Equal(PendingKind.ChooseHandDiscard, match.Pending!.Kind);
        _engine.ResolvePending(match, 0, targetId: me.Hand[0].InstanceId);
        Assert.Equal(PendingKind.ChooseDeckCard, match.Pending!.Kind);
        _engine.ResolvePending(match, 0, targetId: wanted.InstanceId);

        Assert.Null(match.Pending);
        Assert.Equal(handBefore - 1, me.Hand.Count);
        Assert.Contains(me.Hand, c => c.InstanceId == wanted.InstanceId);
        Assert.Equal(1, match.ActivePlayer);
    }

    [Fact]
    public void Medic_Reviving_A_Spy_Plays_It_On_The_Enemy_Side_And_Draws_Two()
    {
        var match = Ready();
        ClearBoard(match);
        var me = match.Players[0];
        me.Hand.Clear();
        var spy = Card("nr_prince_stennis");
        me.Discard.Add(spy);
        var medic = Card("nr_dun_banner");
        me.Hand.Add(medic);
        var deckBefore = me.Deck.Count;

        _engine.PlayCard(match, 0, medic.InstanceId);
        _engine.ResolvePending(match, 0, targetId: spy.InstanceId);

        Assert.Contains(match.Players[1].Rows[CombatRow.Melee], c => c.InstanceId == spy.InstanceId);
        Assert.Equal(deckBefore - 2, me.Deck.Count);
        Assert.Equal(2, me.Hand.Count);
    }

    [Fact]
    public void Skellige_Brings_Back_Two_Units_In_Round_Three()
    {
        var match = Ready(PresetDecks.Skellige(), PresetDecks.Monsters());
        var me = match.Players[0];
        me.Discard.AddRange([Card("sk_olaf"), Card("sk_donar"), Card("sk_coral"), Card("n_decoy1")]);
        match.RoundNumber = 2;
        match.Players[1].Hand.Add(Card("mo_fiend"));
        match.ActivePlayer = 1;
        _engine.PlayCard(match, 1, match.Players[1].Hand[^1].InstanceId);
        _engine.Pass(match, 0);
        _engine.Pass(match, 1);
        ClearBoard(match);
        _engine.ContinueAfterRound(match);

        Assert.Equal(3, match.RoundNumber);
        Assert.Equal(2, me.AllBoardCards().Count(c => c.Type == CardType.Unit));
    }

    [Fact]
    public void Leader_Horn_Does_Not_Stack_With_A_Horn_Already_On_The_Row()
    {
        var match = Ready(PresetDecks.Monsters(), PresetDecks.NorthernRealms());
        ClearBoard(match);
        var me = match.Players[0];
        var fiend = Put(me, Card("mo_fiend"));
        me.HornRows.Add(CombatRow.Melee);

        _engine.UseLeader(match, 0); // Commander of the Red Riders

        Assert.Equal(12, ScoreCalculator.GetCardStrength(fiend, me, match));
    }
}
