using WebGwent.Game.Catalog;
using WebGwent.Game.Models;

namespace WebGwent.Game.Engine;

public sealed class MatchFactory
{
    private readonly CardCatalog _catalog;
    private readonly RulesEngine _engine;

    public MatchFactory(CardCatalog catalog, RulesEngine? engine = null)
    {
        _catalog = catalog;
        _engine = engine ?? new RulesEngine();
    }

    public MatchState Create(DeckList deck0, DeckList deck1, GameMode mode, string name0 = "Player 1", string name1 = "Player 2", bool player1IsBot = false)
    {
        var v0 = DeckValidator.Validate(deck0, _catalog);
        var v1 = DeckValidator.Validate(deck1, _catalog);
        if (!v0.IsValid)
            throw new InvalidOperationException("Deck 1 invalid: " + string.Join("; ", v0.Errors));
        if (!v1.IsValid)
            throw new InvalidOperationException("Deck 2 invalid: " + string.Join("; ", v1.Errors));

        var p0 = BuildPlayer(0, name0, deck0, isBot: false);
        var p1 = BuildPlayer(1, name1, deck1, isBot: player1IsBot || mode == GameMode.VsBot);

        _engine.Shuffle(p0.Deck);
        _engine.Shuffle(p1.Deck);
        RulesEngine.Draw(p0, 10);
        RulesEngine.Draw(p1, 10);

        var start = Random.Shared.Next(2);
        var match = new MatchState
        {
            Mode = mode,
            Phase = MatchPhase.Mulligan,
            Players = [p0, p1],
            StartingPlayer = start,
            ActivePlayer = start
        };
        match.AddLog($"Mulligan phase. Each player may replace up to 2 cards.");
        return match;
    }

    private PlayerState BuildPlayer(int index, string name, DeckList deck, bool isBot)
    {
        var leaderDef = _catalog.Get(deck.LeaderId);
        var cards = deck.CardIds.Select(id => CardInstance.FromDefinition(_catalog.Get(id))).ToList();
        return new PlayerState
        {
            Index = index,
            Name = name,
            Faction = deck.Faction,
            Leader = CardInstance.FromDefinition(leaderDef),
            Deck = cards,
            IsBot = isBot,
            MulliganLeft = 2
        };
    }
}
