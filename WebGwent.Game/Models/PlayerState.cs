namespace WebGwent.Game.Models;

public sealed class PlayerState
{
    public int Index { get; init; }
    public string Name { get; set; } = "Player";
    public Faction Faction { get; set; }
    public CardInstance? Leader { get; set; }
    public bool LeaderUsed { get; set; }
    public List<CardInstance> Deck { get; set; } = [];
    public List<CardInstance> Hand { get; set; } = [];
    public List<CardInstance> Discard { get; set; } = [];
    public Dictionary<CombatRow, List<CardInstance>> Rows { get; set; } = new()
    {
        [CombatRow.Melee] = [],
        [CombatRow.Ranged] = [],
        [CombatRow.Siege] = []
    };
    public HashSet<CombatRow> HornRows { get; set; } = [];
    public int RoundWins { get; set; }
    public bool HasPassed { get; set; }
    public int MulliganLeft { get; set; } = 2;
    public bool IsBot { get; set; }
    public string? ConnectionId { get; set; }

    public IEnumerable<CardInstance> AllBoardCards() =>
        Rows.Values.SelectMany(r => r);

    public void ClearBoardToDiscard(bool keepOneForMonsters, Random rng)
    {
        var kept = (CardInstance?)null;
        if (keepOneForMonsters)
        {
            var candidates = AllBoardCards().Where(c => !c.IsHero).ToList();
            if (candidates.Count > 0)
                kept = candidates[rng.Next(candidates.Count)];
        }

        foreach (var row in Rows.Keys.ToList())
        {
            foreach (var card in Rows[row].ToList())
            {
                if (kept != null && card.InstanceId == kept.InstanceId)
                    continue;
                Discard.Add(card);
            }

            Rows[row] = kept != null && kept.PlacedRow == row
                ? [kept]
                : [];
        }

        HornRows.Clear();
    }

    public PlayerState DeepClone()
    {
        return new PlayerState
        {
            Index = Index,
            Name = Name,
            Faction = Faction,
            Leader = Leader?.Clone(),
            LeaderUsed = LeaderUsed,
            Deck = Deck.Select(c => c.Clone()).ToList(),
            Hand = Hand.Select(c => c.Clone()).ToList(),
            Discard = Discard.Select(c => c.Clone()).ToList(),
            Rows = Rows.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(c => c.Clone()).ToList()),
            HornRows = [.. HornRows],
            RoundWins = RoundWins,
            HasPassed = HasPassed,
            MulliganLeft = MulliganLeft,
            IsBot = IsBot,
            ConnectionId = ConnectionId
        };
    }
}
