using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Tests;

public class AbilityTests
{
    private readonly CardCatalog _catalog = CardCatalog.LoadEmbedded();
    private readonly RulesEngine _engine = new(new Random(7));

    private MatchState ReadyMatch()
    {
        var match = new MatchFactory(_catalog, _engine).Create(
            PresetDecks.NorthernRealms(), PresetDecks.Scoiatael(), GameMode.Hotseat);
        _engine.FinishMulligan(match, 0);
        _engine.FinishMulligan(match, 1);
        if (match.Pending?.Kind == PendingKind.ChooseWhoStarts)
            _engine.ChooseWhoStarts(match, match.Pending.PlayerIndex, 0);
        return match;
    }

    [Fact]
    public void Muster_Pulls_Group_From_Deck_Only()
    {
        var match = ReadyMatch();
        var p = match.Players[0];
        match.ActivePlayer = 0;
        p.Faction = Faction.Scoiatael;
        p.Hand.Clear();
        p.Deck.Clear();

        var a = CardInstance.FromDefinition(_catalog.Get("st_havekar_smuggler1"));
        var b = CardInstance.FromDefinition(_catalog.Get("st_havekar_smuggler2"));
        var c = CardInstance.FromDefinition(_catalog.Get("st_havekar_smuggler3"));
        p.Hand.Add(a);
        p.Hand.Add(b);
        p.Deck.Add(c);

        _engine.PlayCard(match, 0, a.InstanceId);
        // TW3: the copy in the deck joins, the copy in hand stays in hand.
        Assert.Equal(2, p.Rows[CombatRow.Melee].Count(x => x.MusterGroup == "HavekarSmuggler"));
        Assert.Contains(p.Hand, x => x.InstanceId == b.InstanceId);
        Assert.Empty(p.Deck);
    }

    [Fact]
    public void Medic_Can_Revive_From_Discard()
    {
        var match = ReadyMatch();
        var p = match.Players[0];
        match.ActivePlayer = 0;
        p.Hand.Clear();
        var medic = CardInstance.FromDefinition(_catalog.Get("st_havekar_nurse1"));
        var dead = CardInstance.FromDefinition(_catalog.Get("st_milva"));
        dead.PlacedRow = CombatRow.Ranged;
        p.Discard.Add(dead);
        p.Hand.Add(medic);

        _engine.PlayCard(match, 0, medic.InstanceId);
        Assert.NotNull(match.Pending);
        Assert.Equal(PendingKind.ChooseMedicTarget, match.Pending!.Kind);
        _engine.ResolvePending(match, 0, targetId: dead.InstanceId);
        Assert.Contains(p.Rows[CombatRow.Ranged], c => c.InstanceId == dead.InstanceId);
    }

    [Fact]
    public void ClearWeather_Removes_Effects()
    {
        var match = ReadyMatch();
        match.MeleeWeather = WeatherEffect.Frost;
        match.RangedWeather = WeatherEffect.Fog;
        var p = match.Players[match.ActivePlayer];
        var clear = CardInstance.FromDefinition(_catalog.Get("n_clear"));
        p.Hand.Add(clear);
        _engine.PlayCard(match, p.Index, clear.InstanceId);
        Assert.Equal(WeatherEffect.None, match.MeleeWeather);
        Assert.Equal(WeatherEffect.None, match.RangedWeather);
    }

    [Fact]
    public void CommandersHorn_Doubles_Row()
    {
        var match = ReadyMatch();
        var p = match.Players[match.ActivePlayer];
        p.Hand.Clear();
        var unit = CardInstance.FromDefinition(_catalog.Get("nr_ves"));
        var horn = CardInstance.FromDefinition(_catalog.Get("n_horn1"));
        p.Hand.Add(unit);
        p.Hand.Add(horn);
        match.Players[1 - p.Index].HasPassed = true;

        _engine.PlayCard(match, p.Index, unit.InstanceId);
        match.ActivePlayer = p.Index;
        _engine.PlayCard(match, p.Index, horn.InstanceId);

        // The player picks the row.
        Assert.Equal(PendingKind.ChooseHornRow, match.Pending!.Kind);
        Assert.Equal(3, match.Pending.ValidRows.Count);
        _engine.ResolvePending(match, p.Index, row: CombatRow.Melee);

        Assert.Contains(CombatRow.Melee, p.HornRows);
        Assert.Equal(10, ScoreCalculator.GetCardStrength(unit, p, match));
        Assert.Contains(p.Discard, c => c.InstanceId == horn.InstanceId);
    }

    [Fact]
    public void Cerys_Musters_Shield_Maidens()
    {
        var match = ReadyMatch();
        var p = match.Players[0];
        match.ActivePlayer = 0;
        p.Faction = Faction.Skellige;
        p.Hand.Clear();
        p.Deck.Clear();

        var cerys = CardInstance.FromDefinition(_catalog.Get("sk_cerys"));
        var m1 = CardInstance.FromDefinition(_catalog.Get("sk_shieldmaiden1"));
        var m2 = CardInstance.FromDefinition(_catalog.Get("sk_shieldmaiden2"));
        p.Hand.Add(cerys);
        p.Hand.Add(m1);
        p.Deck.Add(m2);

        _engine.PlayCard(match, 0, cerys.InstanceId);
        // Only the Shield Maiden in the deck answers the call.
        Assert.Equal(2, p.Rows[CombatRow.Melee].Count(c => c.MusterGroup == "Cerys" || c.DefinitionId == "sk_cerys"));
        Assert.Contains(p.Hand, c => c.InstanceId == m1.InstanceId);
    }

    [Fact]
    public void Villentretenmerth_Scorches_Strong_Melee()
    {
        var match = ReadyMatch();
        var p = match.Players[match.ActivePlayer];
        var opp = match.Players[1 - p.Index];
        p.Hand.Clear();
        var beef = CardInstance.FromDefinition(_catalog.Get("nr_catapult1"));
        beef.BaseStrength = 10;
        beef.PlacedRow = CombatRow.Melee;
        opp.Rows[CombatRow.Melee].Add(beef);

        var villen = CardInstance.FromDefinition(_catalog.Get("n_villentretenmerth"));
        p.Hand.Add(villen);
        _engine.PlayCard(match, p.Index, villen.InstanceId);

        Assert.DoesNotContain(opp.Rows[CombatRow.Melee], c => c.InstanceId == beef.InstanceId);
        Assert.Contains(opp.Discard, c => c.InstanceId == beef.InstanceId);
    }

    [Fact]
    public void Kambi_Summons_Hemdall_When_Destroyed()
    {
        var match = ReadyMatch();
        var p = match.Players[match.ActivePlayer];
        match.Players[1 - p.Index].HasPassed = true;
        p.Hand.Clear();
        p.Deck.Clear();
        foreach (var pl in match.Players)
        {
            foreach (var row in pl.Rows.Keys.ToList())
                pl.Rows[row].Clear();
        }

        var kambi = CardInstance.FromDefinition(_catalog.Get("sk_kambi"));
        var hemdall = CardInstance.FromDefinition(_catalog.Get("sk_hemdall"));
        kambi.PlacedRow = CombatRow.Melee;
        p.Rows[CombatRow.Melee].Add(kambi);
        p.Deck.Add(hemdall);

        var scorch = CardInstance.FromDefinition(_catalog.Get("n_scorch"));
        p.Hand.Add(scorch);
        _engine.PlayCard(match, p.Index, scorch.InstanceId);

        Assert.Contains(p.Discard, c => c.DefinitionId == "sk_kambi");
        Assert.Contains(p.Rows[CombatRow.Melee], c => c.DefinitionId == "sk_hemdall");
    }

    [Fact]
    public void Decoy_Can_Take_Enemy_Spy_On_Your_Side()
    {
        var match = ReadyMatch();
        var p = match.Players[match.ActivePlayer];
        var opp = match.Players[1 - p.Index];
        p.Hand.Clear();
        foreach (var row in p.Rows.Keys.ToList())
            p.Rows[row].Clear();

        // Enemy spy sits on your melee (as after they played it).
        var spy = CardInstance.FromDefinition(_catalog.Get("ng_stefan"));
        spy.PlacedRow = CombatRow.Melee;
        p.Rows[CombatRow.Melee].Add(spy);

        var decoy = CardInstance.FromDefinition(_catalog.Get("n_decoy1"));
        p.Hand.Add(decoy);
        opp.HasPassed = true;

        _engine.PlayCard(match, p.Index, decoy.InstanceId);
        Assert.Equal(PendingKind.ChooseDecoyTarget, match.Pending!.Kind);
        Assert.Contains(spy.InstanceId, match.Pending.ValidTargets);

        _engine.ResolvePending(match, p.Index, targetId: spy.InstanceId);
        Assert.Contains(p.Hand, c => c.InstanceId == spy.InstanceId);
        Assert.DoesNotContain(p.Rows[CombatRow.Melee], c => c.InstanceId == spy.InstanceId);
    }

    [Fact]
    public void Decoy_Cannot_Target_Hero_Spy()
    {
        var match = ReadyMatch();
        var p = match.Players[match.ActivePlayer];
        p.Hand.Clear();
        foreach (var row in p.Rows.Keys.ToList())
            p.Rows[row].Clear();

        var heroSpy = CardInstance.FromDefinition(_catalog.Get("n_avallach"));
        heroSpy.PlacedRow = CombatRow.Melee;
        p.Rows[CombatRow.Melee].Add(heroSpy);

        var decoy = CardInstance.FromDefinition(_catalog.Get("n_decoy1"));
        p.Hand.Add(decoy);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.PlayCard(match, p.Index, decoy.InstanceId));
        Assert.Equal("NoDecoyTargets", ex.Message);
        Assert.Contains(p.Hand, c => c.InstanceId == decoy.InstanceId);
    }

    [Fact]
    public void EndRound_Adds_To_RoundHistory()
    {
        var match = ReadyMatch();
        match.ActivePlayer = 0;
        match.Players[0].HasPassed = true;
        match.Players[1].HasPassed = true;
        // Force end via a pass when both already passed isn't valid; play then both pass.
        match.Players[0].HasPassed = false;
        match.Players[1].HasPassed = false;
        match.Players[0].Hand.Clear();
        var unit = CardInstance.FromDefinition(_catalog.Get("nr_ves"));
        match.Players[0].Hand.Add(unit);
        match.Players[1].HasPassed = true;
        _engine.PlayCard(match, 0, unit.InstanceId);
        match.ActivePlayer = 0;
        _engine.Pass(match, 0);

        Assert.NotEmpty(match.RoundHistory);
        Assert.Equal(1, match.RoundHistory[0].RoundNumber);
        Assert.True(match.RoundHistory[0].Score0 >= 5);
        Assert.Equal(MatchPhase.RoundEnd, match.Phase);
        _engine.ContinueAfterRound(match);
        Assert.Equal(MatchPhase.Playing, match.Phase);
        Assert.Equal(2, match.RoundNumber);
    }

    [Fact]
    public void Hero_Medic_Can_Revive()
    {
        var match = ReadyMatch();
        var p = match.Players[match.ActivePlayer];
        p.Hand.Clear();
        var yenn = CardInstance.FromDefinition(_catalog.Get("n_yennefer"));
        var dead = CardInstance.FromDefinition(_catalog.Get("nr_ves"));
        dead.PlacedRow = CombatRow.Melee;
        p.Discard.Add(dead);
        p.Hand.Add(yenn);

        _engine.PlayCard(match, p.Index, yenn.InstanceId);
        Assert.Equal(PendingKind.ChooseMedicTarget, match.Pending!.Kind);
        _engine.ResolvePending(match, p.Index, targetId: dead.InstanceId);
        Assert.Contains(p.Rows[CombatRow.Melee], c => c.InstanceId == dead.InstanceId);
    }

    [Fact]
    public void Bot_Can_Finish_A_Match()
    {
        var engine = new RulesEngine(new Random(99));
        var factory = new MatchFactory(_catalog, engine);
        var match = factory.Create(PresetDecks.NorthernRealms(), PresetDecks.Nilfgaard(), GameMode.VsBot, player1IsBot: true);
        var bot = new Game.Bot.BotAI(engine, new Random(99));

        var guard = 0;
        while (match.Phase != MatchPhase.MatchEnd && guard++ < 400)
        {
            if (match.Phase == MatchPhase.RoundEnd)
            {
                engine.ContinueAfterRound(match);
                continue;
            }

            if (match.Pending != null)
            {
                bot.TakeTurn(match, match.Pending.PlayerIndex);
                continue;
            }

            if (match.Phase == MatchPhase.Mulligan)
            {
                foreach (var p in match.Players)
                {
                    if (p.MulliganLeft > 0)
                        engine.FinishMulligan(match, p.Index);
                }
                if (match.Pending?.Kind == PendingKind.ChooseWhoStarts)
                    engine.ChooseWhoStarts(match, match.Pending.PlayerIndex, 0);
                continue;
            }

            if (match.Phase != MatchPhase.Playing) break;

            var cur = match.CurrentPlayer;
            if (cur.IsBot)
            {
                bot.TakeTurn(match, cur.Index);
                continue;
            }

            if (cur.HasPassed || cur.Hand.Count == 0)
            {
                if (!cur.HasPassed)
                    engine.Pass(match, cur.Index);
                continue;
            }

            var card = cur.Hand.FirstOrDefault(c =>
                           c.Type == CardType.Unit
                           && !c.Abilities.Contains(Ability.Agile)
                           && !c.Abilities.Contains(Ability.Medic)
                           && !c.Abilities.Contains(Ability.Spy)
                           && !c.Abilities.Contains(Ability.Decoy))
                       ?? cur.Hand.FirstOrDefault(c => c.Type == CardType.Unit)
                       ?? cur.Hand[0];
            try
            {
                CombatRow? row = card.Abilities.Contains(Ability.Agile) ? CombatRow.Melee : null;
                engine.PlayCard(match, cur.Index, card.InstanceId, row);
                while (match.Pending?.PlayerIndex == cur.Index)
                {
                    if (match.Pending.Kind == PendingKind.ChooseAgileRow)
                        engine.ResolvePending(match, cur.Index, row: CombatRow.Melee);
                    else if (match.Pending.ValidTargets.Count > 0)
                        engine.ResolvePending(match, cur.Index, targetId: match.Pending.ValidTargets[0]);
                    else
                        engine.ResolvePending(match, cur.Index);
                }
            }
            catch
            {
                if (match.Pending is null && !cur.HasPassed)
                    engine.Pass(match, cur.Index);
            }
        }

        Assert.Equal(MatchPhase.MatchEnd, match.Phase);
        Assert.NotNull(match.Winner);
    }

    [Fact]
    public void Bot_Resolves_Scoiatael_ChooseWhoStarts_After_Mulligan()
    {
        var engine = new RulesEngine(new Random(3));
        var factory = new MatchFactory(_catalog, engine);
        // Human NR vs Bot Scoia'tael — after mulligan bot must pick who starts.
        var match = factory.Create(
            PresetDecks.NorthernRealms(),
            PresetDecks.Scoiatael(),
            GameMode.VsBot,
            player1IsBot: true);
        var bot = new Game.Bot.BotAI(engine, new Random(3));

        engine.FinishMulligan(match, 0);
        bot.TakeTurn(match, 1); // finish bot mulligan (+ choose start if pending)

        if (match.Pending?.Kind == PendingKind.ChooseWhoStarts)
            bot.TakeTurn(match, match.Pending.PlayerIndex);

        Assert.Null(match.Pending);
        Assert.Equal(MatchPhase.Playing, match.Phase);
        Assert.True(match.ScoiataelChoseStart);
    }
}
