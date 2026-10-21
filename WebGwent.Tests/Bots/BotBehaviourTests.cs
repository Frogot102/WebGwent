using WebGwent.Game.Bot;
using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Tests.Bots;

/// <summary>Small situations with one clearly right answer.</summary>
public class BotBehaviourTests
{
    private readonly CardCatalog _catalog = CardCatalog.LoadEmbedded();
    private readonly RulesEngine _engine = new(new Random(5));

    private CardInstance Card(string id) => CardInstance.FromDefinition(_catalog.Get(id));

    /// <summary>Bot is player 1, it's its turn, the board is empty and both hands are given.</summary>
    private MatchState Situation(int botLives, int oppLives, IEnumerable<string> botHand, int oppHandSize = 5)
    {
        var match = new MatchFactory(_catalog, _engine).Create(
            PresetDecks.NorthernRealms(), PresetDecks.NorthernRealms(), GameMode.VsBot, player1IsBot: true);
        _engine.FinishMulligan(match, 0);
        foreach (var p in match.Players)
        {
            foreach (var row in p.Rows.Keys.ToList())
                p.Rows[row].Clear();
            p.Hand.Clear();
            p.LeaderUsed = true;
        }

        match.Players[0].Lives = oppLives;
        match.Players[1].Lives = botLives;
        match.Players[1].Hand.AddRange(botHand.Select(Card));
        for (var i = 0; i < oppHandSize; i++)
            match.Players[0].Hand.Add(Card("nr_ves"));
        foreach (var p in match.Players)
            p.HandAtRoundStart = p.Hand.Count;
        match.ActivePlayer = 1;
        return match;
    }

    private static void Put(PlayerState p, CardInstance card) => p.Rows[card.PlacedRow].Add(card);

    [Fact]
    public void Passes_When_The_Opponent_Passed_And_The_Round_Is_Already_Won()
    {
        var match = Situation(1, 1, ["nr_ves", "n_geralt"]);
        Put(match.Players[1], Card("nr_sile"));
        match.Players[0].HasPassed = true;

        new BotAI(_engine, new Random(1)).TakeTurn(match, 1);

        Assert.True(match.Players[1].HasPassed);
        Assert.Equal(1, match.Winner);
        // Both cards kept (plus the Northern Realms draw for winning the round).
        Assert.Contains(match.Players[1].Hand, c => c.DefinitionId == "n_geralt");
        Assert.Contains(match.Players[1].Hand, c => c.DefinitionId == "nr_ves");
    }

    [Fact]
    public void Wins_The_Decider_With_The_Cheapest_Card_That_Is_Enough()
    {
        var match = Situation(1, 1, ["n_geralt", "nr_ves"]);
        Put(match.Players[0], Card("nr_redanian1"));
        Put(match.Players[0], Card("nr_yarpen")); // 3 points
        match.Players[0].HasPassed = true;

        new BotAI(_engine, new Random(1)).TakeTurn(match, 1);

        Assert.Contains(match.Players[1].Hand, c => c.DefinitionId == "n_geralt");
        Assert.DoesNotContain(match.Players[1].Hand, c => c.DefinitionId == "nr_ves");
    }

    [Fact]
    public void Does_Not_Chase_A_Lost_Opening_Round_With_Many_Cards()
    {
        // Opponent spent three cards for 30 points and passed; catching up would cost the bot its whole hand.
        var match = Situation(2, 2, ["nr_ves", "nr_sile", "nr_keira", "nr_yarpen", "nr_sabrina", "nr_sheldon"], oppHandSize: 7);
        foreach (var id in new[] { "nr_vernon_roche", "nr_john_natalis", "nr_esterad" })
            Put(match.Players[0], Card(id));
        match.Players[0].HandAtRoundStart = 10;
        match.Players[0].HasPassed = true;

        new BotAI(_engine, new Random(1)).TakeTurn(match, 1);

        Assert.True(match.Players[1].HasPassed);
        Assert.Equal(6, match.Players[1].Hand.Count);
    }

    [Fact]
    public void Medic_Brings_Back_A_Spy_For_Two_Extra_Cards()
    {
        var match = Situation(2, 2, ["nr_dun_banner"]);
        var me = match.Players[1];
        me.Discard.Add(Card("nr_ves"));
        me.Discard.Add(Card("nr_prince_stennis"));
        me.Deck.AddRange([Card("nr_sile"), Card("nr_keira")]);

        _engine.PlayCard(match, 1, me.Hand[0].InstanceId);
        new BotAI(_engine, new Random(1)).TakeTurn(match, 1);

        Assert.Contains(match.Players[0].Rows[CombatRow.Melee], c => c.DefinitionId == "nr_prince_stennis");
        Assert.Equal(2, me.Hand.Count);
    }

    [Fact]
    public void Lets_The_Opponent_Start_When_Choosing_For_Scoiatael()
    {
        var engine = new RulesEngine(new Random(3));
        var match = new MatchFactory(_catalog, engine).Create(
            PresetDecks.NorthernRealms(), PresetDecks.Scoiatael(), GameMode.VsBot, player1IsBot: true);
        engine.FinishMulligan(match, 0);
        var bot = new BotAI(engine, new Random(3));
        bot.TakeTurn(match, 1);
        if (match.Pending?.Kind == PendingKind.ChooseWhoStarts)
            bot.TakeTurn(match, 1);

        Assert.Equal(0, match.StartingPlayer); // going second gives the last word
    }
}
