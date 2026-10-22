using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;
using WebGwent.Game.Services;

namespace WebGwent.Tests;

/// <summary>Cards from the base game, Hearts of Stone and Blood and Wine, and how the match shows them.</summary>
public class Tw3CardsTests
{
    private readonly CardCatalog _catalog = CardCatalog.LoadEmbedded();
    private readonly RulesEngine _engine = new(new Random(21));

    private CardInstance Card(string id) => CardInstance.FromDefinition(_catalog.Get(id));

    private MatchState Ready(DeckList d0, DeckList d1)
    {
        var match = new MatchFactory(_catalog, _engine).Create(d0, d1, GameMode.Hotseat);
        _engine.FinishMulligan(match, 0);
        _engine.FinishMulligan(match, 1);
        if (match.Pending?.Kind == PendingKind.ChooseWhoStarts)
            _engine.ChooseWhoStarts(match, match.Pending.PlayerIndex, 0);
        match.ActivePlayer = 0;
        foreach (var p in match.Players)
        foreach (var row in p.Rows.Keys.ToList())
            p.Rows[row].Clear();
        return match;
    }

    private static CardInstance Put(PlayerState p, CardInstance card)
    {
        p.Rows[card.PlacedRow].Add(card);
        return card;
    }

    [Theory]
    [InlineData("Decoy", 3)]
    [InlineData("Commander's Horn", 3)]
    [InlineData("Scorch", 3)]
    [InlineData("Biting Frost", 3)]
    [InlineData("Impenetrable Fog", 3)]
    [InlineData("Torrential Rain", 3)]
    [InlineData("Clear Weather", 3)]
    [InlineData("Skellige Storm", 3)]
    [InlineData("Mardroeme", 3)]
    [InlineData("Poor Fucking Infantry", 4)]
    [InlineData("Ballista", 2)]
    [InlineData("Impera Brigade Guard", 4)]
    [InlineData("Mahakaman Defender", 5)]
    [InlineData("Dol Blathanna Scout", 3)]
    [InlineData("Havekar Healer", 3)]
    [InlineData("War Longship", 3)]
    [InlineData("Young Berserker", 3)]
    public void Catalog_Has_As_Many_Copies_As_TW3(string name, int copies)
    {
        Assert.Equal(copies, _catalog.All.Values.Count(c => c.Name == name));
    }

    [Theory]
    [InlineData("Dethmold")]
    [InlineData("Sigismund Dijkstra")]
    [InlineData("Morteisen")]
    [InlineData("Vreemde")]
    [InlineData("Siege Technician")]
    [InlineData("Zerrikanian Fire Scorpion")]
    [InlineData("Saesenthessis")]
    [InlineData("Endrega")]
    [InlineData("Cockatrice")]
    [InlineData("Gargoyle")]
    [InlineData("Grave Hag")]
    [InlineData("Ice Giant")]
    [InlineData("Fire Elemental")]
    [InlineData("Toad")]
    [InlineData("Clan Tordarroch Armorsmith")]
    [InlineData("Draig Bon-Dhu")]
    [InlineData("Emhyr var Emreis: Invader of the North")]
    [InlineData("Eredin Bréacc Glas: The Treacherous")]
    public void Catalog_Contains_Missing_TW3_Card(string name)
    {
        Assert.Contains(_catalog.All.Values, c => c.Name == name);
    }

    [Fact]
    public void Every_Faction_Has_Five_Leaders_Except_Skellige()
    {
        var leaders = _catalog.All.Values.Where(c => c.Type == CardType.Leader).GroupBy(c => c.Faction)
            .ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(5, leaders[Faction.NorthernRealms]);
        Assert.Equal(5, leaders[Faction.Nilfgaard]);
        Assert.Equal(5, leaders[Faction.Scoiatael]);
        Assert.Equal(5, leaders[Faction.Monsters]);
        Assert.Equal(2, leaders[Faction.Skellige]);
    }

    [Fact]
    public void Skellige_Storm_Hits_Ranged_And_Siege()
    {
        var match = Ready(PresetDecks.Skellige(), PresetDecks.NorthernRealms());
        var enemy = match.Players[1];
        var archer = Put(enemy, Card("nr_keira"));
        var ballista = Put(enemy, Card("nr_ballista"));
        var ves = Put(enemy, Card("nr_ves"));
        var storm = Card("n_storm1");
        match.Players[0].Hand.Add(storm);

        _engine.PlayCard(match, 0, storm.InstanceId);

        Assert.Equal(1, ScoreCalculator.GetCardStrength(archer, enemy, match));
        Assert.Equal(1, ScoreCalculator.GetCardStrength(ballista, enemy, match));
        Assert.Equal(5, ScoreCalculator.GetCardStrength(ves, enemy, match));
    }

    [Fact]
    public void Clan_Dimun_Pirate_Scorches_The_Battlefield_But_Not_Itself()
    {
        var match = Ready(PresetDecks.Skellige(), PresetDecks.NorthernRealms());
        var enemy = match.Players[1];
        var keira = Put(enemy, Card("nr_keira")); // 5
        var ves = Put(enemy, Card("nr_ves"));     // 5
        var yarpen = Put(enemy, Card("nr_yarpen"));
        var pirate = Card("sk_clan_hunter1");
        match.Players[0].Hand.Add(pirate);

        _engine.PlayCard(match, 0, pirate.InstanceId);

        Assert.DoesNotContain(enemy.AllBoardCards(), c => c.InstanceId == keira.InstanceId || c.InstanceId == ves.InstanceId);
        Assert.Contains(enemy.AllBoardCards(), c => c.InstanceId == yarpen.InstanceId);
        Assert.Contains(match.Players[0].AllBoardCards(), c => c.InstanceId == pirate.InstanceId);
    }

    [Fact]
    public void Toad_Burns_The_Enemy_Ranged_Row_When_It_Totals_Ten()
    {
        var match = Ready(PresetDecks.Monsters(), PresetDecks.NorthernRealms());
        var enemy = match.Players[1];
        var keira = Put(enemy, Card("nr_keira"));
        Put(enemy, Card("nr_sile"));
        var toad = Card("mo_toad");
        match.Players[0].Hand.Add(toad);

        _engine.PlayCard(match, 0, toad.InstanceId);

        Assert.DoesNotContain(enemy.AllBoardCards(), c => c.InstanceId == keira.InstanceId);
    }

    [Fact]
    public void Draig_Bon_Dhu_Horns_The_Siege_Row()
    {
        var match = Ready(PresetDecks.Skellige(), PresetDecks.NorthernRealms());
        var me = match.Players[0];
        var holger = Put(me, Card("sk_holger"));
        Put(me, Card("sk_draig"));

        Assert.Equal(8, ScoreCalculator.GetCardStrength(holger, me, match));
    }

    [Fact]
    public void The_Treacherous_Doubles_Spies_On_Both_Sides()
    {
        var monsters = PresetDecks.Monsters() with { LeaderId = "mo_leader_eredin_treacherous" };
        var match = Ready(monsters, PresetDecks.NorthernRealms());
        var stennis = Card("nr_prince_stennis");
        match.Players[1].Hand.Add(stennis);
        match.ActivePlayer = 1;

        _engine.PlayCard(match, 1, stennis.InstanceId);

        // The spy sits on the Monsters side and counts double for them.
        Assert.Equal(10, ScoreCalculator.GetCardStrength(stennis, match.Players[0], match));
    }

    [Fact]
    public void Invader_Of_The_North_Makes_Medics_Revive_At_Random()
    {
        var nilfgaard = PresetDecks.Nilfgaard() with { LeaderId = "ng_leader_emhyr_invader" };
        var match = Ready(PresetDecks.NorthernRealms(), nilfgaard);
        var me = match.Players[0];
        me.Discard.Add(Card("nr_ves"));
        me.Discard.Add(Card("nr_keira"));
        var medic = Card("nr_dun_banner");
        me.Hand.Add(medic);

        _engine.PlayCard(match, 0, medic.InstanceId);

        Assert.Null(match.Pending);
        Assert.Single(me.Discard.Where(c => c.Type == CardType.Unit));
    }

    [Fact]
    public void Hand_And_Rows_Are_Shown_From_Weakest_To_Strongest()
    {
        var match = Ready(PresetDecks.NorthernRealms(), PresetDecks.Monsters());
        var me = match.Players[0];
        me.Hand.Clear();
        me.Hand.AddRange([Card("n_geralt"), Card("n_scorch"), Card("nr_yarpen"), Card("nr_ves")]);
        Put(me, Card("nr_vernon_roche"));
        Put(me, Card("nr_redanian1"));
        Put(me, Card("nr_ves"));

        var view = MatchView.From(match, 0);

        Assert.Equal(["Scorch", "Yarpen Zigrin", "Ves", "Geralt of Rivia"], view.You.Hand.Select(c => c.Name));
        Assert.Equal([1, 5, 10], view.You.Melee.Select(c => c.CurrentStrength));
    }
}
