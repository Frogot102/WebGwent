using WebGwent.Game.Models;

namespace WebGwent.Game.Engine;

public sealed class RulesEngine
{
    private readonly Random _rng;

    public RulesEngine(Random? rng = null) => _rng = rng ?? Random.Shared;

    public void Mulligan(MatchState match, int playerIndex, Guid cardInstanceId)
    {
        EnsurePhase(match, MatchPhase.Mulligan);
        var player = match.Players[playerIndex];
        if (player.MulliganLeft <= 0)
            throw new InvalidOperationException("No mulligans left.");

        var card = player.Hand.FirstOrDefault(c => c.InstanceId == cardInstanceId)
                   ?? throw new InvalidOperationException("Card not in hand.");

        player.Hand.Remove(card);
        player.Deck.Add(card);
        Shuffle(player.Deck);
        Draw(player, 1);
        player.MulliganLeft--;
        match.AddLog($"{player.Name} replaced a card ({player.MulliganLeft} left).");

        TryFinishMulligan(match);
    }

    public void FinishMulligan(MatchState match, int playerIndex)
    {
        EnsurePhase(match, MatchPhase.Mulligan);
        match.Players[playerIndex].MulliganLeft = 0;
        match.AddLog($"{match.Players[playerIndex].Name} finished mulligan.");
        TryFinishMulligan(match);
    }

    public void ChooseWhoStarts(MatchState match, int playerIndex, int startingPlayer)
    {
        if (match.Pending?.Kind != PendingKind.ChooseWhoStarts || match.Pending.PlayerIndex != playerIndex)
            throw new InvalidOperationException("Not waiting for start choice.");

        match.StartingPlayer = startingPlayer;
        match.ActivePlayer = startingPlayer;
        match.ScoiataelChoseStart = true;
        match.Pending = null;
        match.Phase = MatchPhase.Playing;
        match.AddLog($"{match.Players[playerIndex].Name} lets {(startingPlayer == playerIndex ? "themselves" : "the opponent")} start.");
    }

    public void PlayCard(MatchState match, int playerIndex, Guid cardInstanceId, CombatRow? rowOverride = null, Guid? targetId = null)
    {
        if (match.Phase != MatchPhase.Playing)
            throw new InvalidOperationException("Not in playing phase.");
        if (match.ActivePlayer != playerIndex)
            throw new InvalidOperationException("Not your turn.");
        if (match.Pending != null)
            throw new InvalidOperationException("Resolve pending action first.");

        var player = match.Players[playerIndex];
        if (player.HasPassed)
            throw new InvalidOperationException("Already passed.");

        var card = player.Hand.FirstOrDefault(c => c.InstanceId == cardInstanceId)
                   ?? throw new InvalidOperationException("Card not in hand.");

        match.LastRoundResult = null;
        match.LastPlayedCardId = card.InstanceId;
        match.LastActionPlayer = playerIndex;
        match.LastPlayedRow = null;
        player.Hand.Remove(card);

        if (card.Type == CardType.Special)
        {
            ResolveSpecial(match, player, card, targetId);
        }
        else
        {
            ResolveUnit(match, player, card, rowOverride, targetId);
        }

        if (match.Pending != null)
            return;

        AfterAction(match, playerIndex);
    }

    public void ResolvePending(MatchState match, int playerIndex, Guid? targetId = null, CombatRow? row = null)
    {
        var pending = match.Pending ?? throw new InvalidOperationException("No pending action.");
        if (pending.PlayerIndex != playerIndex)
            throw new InvalidOperationException("Not your pending action.");

        var player = match.Players[playerIndex];
        var source = FindCardAnywhere(match, pending.SourceCardId)
                     ?? throw new InvalidOperationException("Source card missing.");

        switch (pending.Kind)
        {
            case PendingKind.ChooseAgileRow:
                if (row is null || !pending.ValidRows.Contains(row.Value))
                    throw new InvalidOperationException("Invalid row.");
                player.Discard.RemoveAll(c => c.InstanceId == source.InstanceId);
                PlaceUnit(match, player, source, row.Value, isSpy: source.Abilities.Contains(Ability.Spy));
                match.Pending = null;
                TriggerPostPlace(match, player, source);
                break;

            case PendingKind.ChooseMedicTarget:
                if (targetId is null)
                {
                    match.Pending = null;
                    match.AddLog($"{player.Name} skipped medic revive.");
                    break;
                }
                ReviveMedic(match, player, targetId.Value);
                match.Pending = null;
                break;

            case PendingKind.ChooseDecoyTarget:
                if (targetId is null || !pending.ValidTargets.Contains(targetId.Value))
                    throw new InvalidOperationException("Invalid decoy target.");
                ApplyDecoy(match, player, source, targetId.Value);
                match.Pending = null;
                break;

            default:
                throw new InvalidOperationException("Unsupported pending kind.");
        }

        AfterAction(match, playerIndex);
    }

    public void Pass(MatchState match, int playerIndex)
    {
        if (match.Phase != MatchPhase.Playing)
            throw new InvalidOperationException("Not in playing phase.");
        if (match.ActivePlayer != playerIndex)
            throw new InvalidOperationException("Not your turn.");
        if (match.Pending != null)
            throw new InvalidOperationException("Resolve pending action first.");

        var player = match.Players[playerIndex];
        match.LastRoundResult = null;
        match.LastPlayedCardId = null;
        match.LastPlayedRow = null;
        match.LastActionPlayer = playerIndex;
        player.HasPassed = true;
        match.AddLog($"{player.Name} passes.");
        AfterAction(match, playerIndex);
    }

    public void UseLeader(MatchState match, int playerIndex)
    {
        if (match.Phase != MatchPhase.Playing)
            throw new InvalidOperationException("Not in playing phase.");
        if (match.ActivePlayer != playerIndex)
            throw new InvalidOperationException("Not your turn.");
        if (match.Pending != null)
            throw new InvalidOperationException("Resolve pending action first.");

        var player = match.Players[playerIndex];
        if (player.LeaderUsed || player.Leader is null)
            throw new InvalidOperationException("Leader already used or missing.");

        var ability = GetLeaderAbility(player.Leader);
        match.LastPlayedCardId = null;
        match.LastPlayedRow = null;
        match.LastActionPlayer = playerIndex;
        ApplyLeader(match, player, ability);
        player.LeaderUsed = true;
        match.AddLog($"{player.Name} used leader ability.");
        AfterAction(match, playerIndex);
    }

    private LeaderAbility GetLeaderAbility(CardInstance leader)
    {
        // Leader ability stored via Abilities empty + Name mapping in catalog through Description field pattern.
        // We encode LeaderAbility in MusterGroup for leaders as "Leader:<enum>"
        if (leader.MusterGroup?.StartsWith("Leader:") == true
            && Enum.TryParse<LeaderAbility>(leader.MusterGroup["Leader:".Length..], out var la))
            return la;
        return LeaderAbility.None;
    }

    private void ApplyLeader(MatchState match, PlayerState player, LeaderAbility ability)
    {
        switch (ability)
        {
            case LeaderAbility.ClearWeather:
                ClearWeather(match);
                break;
            case LeaderAbility.FoltestStorm:
                ScorchRow(match, CombatRow.Siege, opponentOnly: true, player.Index);
                break;
            case LeaderAbility.FoltestFog:
                match.RangedWeather = WeatherEffect.Fog;
                break;
            case LeaderAbility.FoltestRain:
                match.SiegeWeather = WeatherEffect.Rain;
                break;
            case LeaderAbility.FoltestSiegeMaster:
                player.HornRows.Add(CombatRow.Siege);
                break;
            case LeaderAbility.FoltestSonOfMedell:
                ScorchRow(match, CombatRow.Ranged, opponentOnly: true, player.Index);
                break;
            case LeaderAbility.EmhyrSpy:
                Draw(player, 1);
                break;
            case LeaderAbility.EmhyrLookout:
                // Look at 3 opponent cards — informational; draw nothing mechanically relevant for AI
                match.AddLog($"{player.Name} looked at 3 opponent cards.");
                break;
            case LeaderAbility.EmhyrImperial:
                // Cancel opponent leader if unused — mark as used
                var opp = match.Players[1 - player.Index];
                if (!opp.LeaderUsed)
                {
                    opp.LeaderUsed = true;
                    match.AddLog($"{opp.Name}'s leader was cancelled.");
                }
                break;
            case LeaderAbility.EmhyrWhiteFlame:
                // Draw until hand has same number of cards as opponent (max +3 typically simplified: draw 1)
                var deficit = match.Players[1 - player.Index].Hand.Count - player.Hand.Count;
                if (deficit > 0)
                    Draw(player, Math.Min(deficit, 3));
                break;
            case LeaderAbility.EmhyrRelentless:
            {
                var enemy = match.Players[1 - player.Index];
                var steal = enemy.Discard.Where(c => c.Type == CardType.Unit && !c.IsHero)
                    .OrderByDescending(c => c.BaseStrength).FirstOrDefault();
                if (steal != null)
                {
                    enemy.Discard.Remove(steal);
                    player.Hand.Add(steal);
                    match.AddLog($"{player.Name} took {steal.Name} from enemy discard.");
                }
                break;
            }
            case LeaderAbility.FrancescaHope:
                match.MeleeWeather = WeatherEffect.Frost;
                break;
            case LeaderAbility.FrancescaDaisy:
                Draw(player, 1);
                break;
            case LeaderAbility.FrancescaQueen:
                ScorchRow(match, CombatRow.Melee, opponentOnly: true, player.Index);
                break;
            case LeaderAbility.EredinKing:
                player.HornRows.Add(CombatRow.Melee);
                break;
            case LeaderAbility.EredinCommander:
                // Double spy strength — simplified: draw 1 when spy on board
                Draw(player, 1);
                break;
            case LeaderAbility.EredinDestroyer:
            {
                // Discard up to 2 from hand, then draw 1
                var trash = player.Hand.Where(c => c.Type == CardType.Unit && !c.IsHero && c.BaseStrength <= 4)
                    .OrderBy(c => c.BaseStrength).Take(2).ToList();
                foreach (var t in trash)
                {
                    player.Hand.Remove(t);
                    player.Discard.Add(t);
                }
                Draw(player, 1);
                break;
            }
            case LeaderAbility.CrachAnCraite:
                ShuffleDiscardIntoDeck(player);
                break;
            case LeaderAbility.KingBran:
                // Units under weather keep half strength — tracked via flag in message; apply by clearing weather immunity partial:
                // Simplified: clear weather for own side by removing weather
                ClearWeather(match);
                break;
            default:
                break;
        }
    }

    private void ResolveUnit(MatchState match, PlayerState player, CardInstance card, CombatRow? rowOverride, Guid? targetId)
    {
        var isSpy = card.Abilities.Contains(Ability.Spy);
        var isAgile = card.Abilities.Contains(Ability.Agile);

        CombatRow row;
        if (isAgile)
        {
            if (rowOverride is CombatRow.Melee or CombatRow.Ranged)
            {
                row = rowOverride.Value;
            }
            else
            {
                // Park card in discard until row is chosen (removed again on resolve).
                player.Discard.Add(card);
                match.Pending = new PendingAction
                {
                    Kind = PendingKind.ChooseAgileRow,
                    PlayerIndex = player.Index,
                    SourceCardId = card.InstanceId,
                    ValidRows = [CombatRow.Melee, CombatRow.Ranged]
                };
                match.AddLog($"{player.Name} must choose a row for {card.Name}.");
                return;
            }
        }
        else
        {
            row = rowOverride ?? card.PlacedRow;
            if (row is CombatRow.None)
                throw new InvalidOperationException("Unit has no row.");
        }

        PlaceUnit(match, player, card, row, isSpy);
        TriggerPostPlace(match, player, card);

        if (card.Abilities.Contains(Ability.Medic))
        {
            var reviveTargets = player.Discard
                .Where(c => c.Type == CardType.Unit && !c.IsHero)
                .Select(c => c.InstanceId)
                .ToList();
            if (reviveTargets.Count > 0)
            {
                match.Pending = new PendingAction
                {
                    Kind = PendingKind.ChooseMedicTarget,
                    PlayerIndex = player.Index,
                    SourceCardId = card.InstanceId,
                    ValidTargets = reviveTargets
                };
                match.AddLog($"{player.Name} may revive a unit with Medic.");
            }
        }
    }

    private void PlaceUnit(MatchState match, PlayerState owner, CardInstance card, CombatRow row, bool isSpy)
    {
        var targetPlayer = isSpy ? match.Players[1 - owner.Index] : owner;
        card.PlacedRow = row;
        targetPlayer.Rows[row].Add(card);
        match.LastPlayedCardId = card.InstanceId;
        match.LastActionPlayer = owner.Index;
        match.LastPlayedRow = row;
        match.AddLog(isSpy
            ? $"{owner.Name} played spy {card.Name} on enemy {row}."
            : $"{owner.Name} played {card.Name} on {row}.");

        if (isSpy)
            Draw(owner, 2);

        if (card.Abilities.Contains(Ability.Muster))
            Muster(match, owner, card);
    }

    private void TriggerPostPlace(MatchState match, PlayerState player, CardInstance card)
    {
        if (card.Abilities.Contains(Ability.Scorch) && card.Type == CardType.Unit)
        {
            // Villentretenmerth (Melee) / Schirrú (Siege): scorch strongest on that row if >= 10
            var row = card.PlacedRow;
            if (row is CombatRow.None) return;
            var units = match.Players.SelectMany(p => p.Rows[row])
                .Where(c => !c.IsHero && c.InstanceId != card.InstanceId)
                .ToList();
            if (units.Count == 0) return;
            var max = units.Max(c => ScoreCalculator.GetCardStrength(c, FindOwner(match, c)!, match));
            if (max >= 10)
            {
                DestroyUnits(match, units.Where(c =>
                    ScoreCalculator.GetCardStrength(c, FindOwner(match, c)!, match) == max).ToList());
                match.AddLog($"Scorch destroyed the strongest {row} unit(s).");
            }
        }
    }

    private void ResolveSpecial(MatchState match, PlayerState player, CardInstance card, Guid? targetId)
    {
        if (card.Abilities.Contains(Ability.WeatherFrost))
        {
            match.MeleeWeather = WeatherEffect.Frost;
            match.LastPlayedRow = CombatRow.Melee;
            player.Discard.Add(card);
            match.AddLog($"{player.Name} played Biting Frost.");
        }
        else if (card.Abilities.Contains(Ability.WeatherFog))
        {
            match.RangedWeather = WeatherEffect.Fog;
            match.LastPlayedRow = CombatRow.Ranged;
            player.Discard.Add(card);
            match.AddLog($"{player.Name} played Impenetrable Fog.");
        }
        else if (card.Abilities.Contains(Ability.WeatherRain))
        {
            match.SiegeWeather = WeatherEffect.Rain;
            match.LastPlayedRow = CombatRow.Siege;
            player.Discard.Add(card);
            match.AddLog($"{player.Name} played Torrential Rain.");
        }
        else if (card.Abilities.Contains(Ability.WeatherClear))
        {
            ClearWeather(match);
            match.LastPlayedRow = null;
            player.Discard.Add(card);
            match.AddLog($"{player.Name} played Clear Weather.");
        }
        else if (card.Abilities.Contains(Ability.CommandersHorn))
        {
            // Apply to row with most units of own, or require row — use row with highest potential
            var best = new[] { CombatRow.Melee, CombatRow.Ranged, CombatRow.Siege }
                .OrderByDescending(r => player.Rows[r].Count)
                .First();
            player.HornRows.Add(best);
            match.LastPlayedRow = best;
            player.Discard.Add(card);
            match.AddLog($"{player.Name} played Commander's Horn on {best}.");
        }
        else if (card.Abilities.Contains(Ability.Scorch))
        {
            player.Discard.Add(card);
            GlobalScorch(match);
            match.AddLog($"{player.Name} played Scorch.");
        }
        else if (card.Abilities.Contains(Ability.Decoy))
        {
            // Own side only — includes enemy spies sitting on your rows. Heroes cannot be Decoyed (TW3).
            var targets = player.AllBoardCards().Where(c => !c.IsHero).Select(c => c.InstanceId).ToList();
            if (targets.Count == 0)
            {
                player.Hand.Add(card); // refund
                throw new InvalidOperationException("NoDecoyTargets");
            }

            if (targetId is null || !targets.Contains(targetId.Value))
            {
                match.Pending = new PendingAction
                {
                    Kind = PendingKind.ChooseDecoyTarget,
                    PlayerIndex = player.Index,
                    SourceCardId = card.InstanceId,
                    ValidTargets = targets
                };
                // Park decoy card — store in discard then remove when applied? Keep off lists:
                // Temporarily hold by not adding anywhere — store via finding. Put in Discard as parking.
                // Use Hand parking:
                // Card was removed from hand. Keep reference only via Pending SourceCardId — need storage.
                player.Discard.Add(card); // temporary; ApplyDecoy will remove decoy from discard and leave it in discard properly
                match.AddLog($"{player.Name} must choose a Decoy target.");
                return;
            }

            ApplyDecoy(match, player, card, targetId.Value);
        }
        else if (card.Abilities.Contains(Ability.Mardroeme))
        {
            player.Discard.Add(card);
            TransformBerserkers(match, player);
            match.AddLog($"{player.Name} played Mardroeme.");
        }
        else
        {
            player.Discard.Add(card);
            match.AddLog($"{player.Name} played special {card.Name}.");
        }
    }

    private void ApplyDecoy(MatchState match, PlayerState player, CardInstance decoy, Guid targetId)
    {
        // Decoy may be parked in discard
        player.Discard.RemoveAll(c => c.InstanceId == decoy.InstanceId);

        CardInstance? target = null;
        CombatRow foundRow = CombatRow.None;
        foreach (var row in player.Rows.Keys)
        {
            target = player.Rows[row].FirstOrDefault(c => c.InstanceId == targetId);
            if (target != null)
            {
                foundRow = row;
                break;
            }
        }

        if (target is null)
            throw new InvalidOperationException("Decoy target not found.");

        player.Rows[foundRow].Remove(target);
        player.Hand.Add(target);
        decoy.PlacedRow = foundRow;
        decoy.BaseStrength = 0;
        decoy.Type = CardType.Unit;
        player.Rows[foundRow].Add(decoy);
        match.AddLog($"{player.Name} decoyed {target.Name} back to hand.");
    }

    private void ReviveMedic(MatchState match, PlayerState player, Guid targetId)
    {
        var card = player.Discard.FirstOrDefault(c => c.InstanceId == targetId)
                   ?? throw new InvalidOperationException("Revive target missing.");
        if (card.IsHero || card.Type != CardType.Unit)
            throw new InvalidOperationException("Cannot revive that card.");

        player.Discard.Remove(card);
        // Replay without medic chain recursion for simplicity (no second medic trigger)
        var row = card.Abilities.Contains(Ability.Agile) ? CombatRow.Melee : card.PlacedRow;
        if (row == CombatRow.None) row = CombatRow.Melee;
        var isSpy = card.Abilities.Contains(Ability.Spy);
        PlaceUnit(match, player, card, row, isSpy);
        match.AddLog($"{player.Name} revived {card.Name}.");
    }

    private void Muster(MatchState match, PlayerState owner, CardInstance card)
    {
        if (string.IsNullOrEmpty(card.MusterGroup))
            return;

        var group = card.MusterGroup;
        var pulled = new List<CardInstance>();

        foreach (var c in owner.Hand.Where(c => c.MusterGroup == group && c.InstanceId != card.InstanceId).ToList())
        {
            owner.Hand.Remove(c);
            pulled.Add(c);
        }

        foreach (var c in owner.Deck.Where(c => c.MusterGroup == group).ToList())
        {
            owner.Deck.Remove(c);
            pulled.Add(c);
        }

        foreach (var c in pulled)
        {
            var row = c.PlacedRow == CombatRow.None ? card.PlacedRow : c.PlacedRow;
            if (c.Abilities.Contains(Ability.Agile))
                row = card.PlacedRow;
            var target = c.Abilities.Contains(Ability.Spy) ? match.Players[1 - owner.Index] : owner;
            c.PlacedRow = row;
            target.Rows[row].Add(c);
        }

        if (pulled.Count > 0)
            match.AddLog($"Muster summoned {pulled.Count} more card(s).");
    }

    private void SummonAvenger(MatchState match, PlayerState boardOwner, CardInstance card)
    {
        // Cow → Bovine / Kambi → Hemdall: MusterGroup on the destroyed card names the summon group.
        if (string.IsNullOrEmpty(card.MusterGroup)) return;
        var group = card.MusterGroup;
        var avenger = boardOwner.Deck.FirstOrDefault(c =>
            c.MusterGroup == group && !c.Abilities.Contains(Ability.SummonAvenger));
        if (avenger is null) return;
        boardOwner.Deck.Remove(avenger);
        var row = avenger.PlacedRow is CombatRow.Melee or CombatRow.Ranged or CombatRow.Siege
            ? avenger.PlacedRow
            : card.PlacedRow is CombatRow.Melee or CombatRow.Ranged or CombatRow.Siege
                ? card.PlacedRow
                : CombatRow.Melee;
        avenger.PlacedRow = row;
        boardOwner.Rows[row].Add(avenger);
        match.AddLog($"{avenger.Name} was summoned.");
    }

    private void TransformBerserkers(MatchState match, PlayerState player)
    {
        foreach (var row in player.Rows.Keys.ToList())
        {
            foreach (var card in player.Rows[row].Where(c => c.Abilities.Contains(Ability.Berserker) && !c.Transformed).ToList())
            {
                card.Transformed = true;
                card.BaseStrength = Math.Max(card.BaseStrength * 2, card.BaseStrength + 4);
                card.Name += " (Transformed)";
            }
        }
    }

    private void GlobalScorch(MatchState match)
    {
        var all = match.Players.SelectMany(p => p.AllBoardCards().Select(c => (p, c)))
            .Where(x => !x.c.IsHero && x.c.Type == CardType.Unit)
            .ToList();
        if (all.Count == 0) return;

        var max = all.Max(x => ScoreCalculator.GetCardStrength(x.c, x.p, match));
        var victims = all.Where(x => ScoreCalculator.GetCardStrength(x.c, x.p, match) == max)
            .Select(x => x.c).ToList();
        DestroyUnits(match, victims);
    }

    private void ScorchRow(MatchState match, CombatRow row, bool opponentOnly, int playerIndex)
    {
        var players = opponentOnly
            ? [match.Players[1 - playerIndex]]
            : match.Players;

        var units = players.SelectMany(p => p.Rows[row]).Where(c => !c.IsHero).ToList();
        if (units.Count == 0) return;
        var max = units.Max(c => ScoreCalculator.GetCardStrength(c, FindOwner(match, c)!, match));
        DestroyUnits(match, units.Where(c =>
            ScoreCalculator.GetCardStrength(c, FindOwner(match, c)!, match) == max).ToList());
    }

    private void DestroyUnits(MatchState match, List<CardInstance> units)
    {
        foreach (var unit in units)
        {
            var owner = FindOwner(match, unit);
            if (owner is null) continue;
            owner.Rows[unit.PlacedRow].Remove(unit);
            owner.Discard.Add(unit);
            if (unit.Abilities.Contains(Ability.SummonAvenger))
                SummonAvenger(match, owner, unit);
        }
    }

    private PlayerState? FindOwner(MatchState match, CardInstance card)
    {
        return match.Players.FirstOrDefault(p => p.AllBoardCards().Any(c => c.InstanceId == card.InstanceId));
    }

    private CardInstance? FindCardAnywhere(MatchState match, Guid id)
    {
        foreach (var p in match.Players)
        {
            var c = p.Hand.FirstOrDefault(x => x.InstanceId == id)
                    ?? p.Deck.FirstOrDefault(x => x.InstanceId == id)
                    ?? p.Discard.FirstOrDefault(x => x.InstanceId == id)
                    ?? p.AllBoardCards().FirstOrDefault(x => x.InstanceId == id);
            if (c != null) return c;
        }
        return null;
    }

    private void ClearWeather(MatchState match)
    {
        match.MeleeWeather = WeatherEffect.None;
        match.RangedWeather = WeatherEffect.None;
        match.SiegeWeather = WeatherEffect.None;
    }

    private void AfterAction(MatchState match, int playerIndex)
    {
        if (match.Pending != null)
            return;

        if (match.Players.All(p => p.HasPassed))
        {
            EndRound(match);
            return;
        }

        // Switch turn to other player if they haven't passed
        var other = 1 - playerIndex;
        if (!match.Players[other].HasPassed)
            match.ActivePlayer = other;
        else if (!match.Players[playerIndex].HasPassed)
            match.ActivePlayer = playerIndex;
        else
            EndRound(match);

        // Bot auto-play is handled by session layer
    }

    private void EndRound(MatchState match)
    {
        var s0 = ScoreCalculator.GetTotalScore(match.Players[0], match);
        var s1 = ScoreCalculator.GetTotalScore(match.Players[1], match);
        match.AddLog($"Round {match.RoundNumber} ended: {s0} vs {s1}.");

        int? roundWinner;
        if (s0 > s1) roundWinner = 0;
        else if (s1 > s0) roundWinner = 1;
        else
        {
            // Nilfgaard wins draws
            if (match.Players[0].Faction == Faction.Nilfgaard && match.Players[1].Faction != Faction.Nilfgaard)
                roundWinner = 0;
            else if (match.Players[1].Faction == Faction.Nilfgaard && match.Players[0].Faction != Faction.Nilfgaard)
                roundWinner = 1;
            else
                roundWinner = null; // both get a gem? In TW3 both lose a gem / neither wins — treat as both "win" count no — actually both lose the round gem (no one gets gem). So neither RoundWins++.
        }

        if (roundWinner is int w)
        {
            match.Players[w].RoundWins++;
            match.AddLog($"{match.Players[w].Name} wins the round.");
            if (match.Players[w].Faction == Faction.NorthernRealms)
            {
                Draw(match.Players[w], 1);
                match.AddLog($"{match.Players[w].Name} draws (Northern Realms).");
            }
        }
        else
        {
            match.AddLog("Round is a draw — no gem awarded.");
        }

        var result = new RoundResult
        {
            RoundNumber = match.RoundNumber,
            Score0 = s0,
            Score1 = s1,
            WinnerIndex = roundWinner
        };
        match.LastRoundResult = result;
        match.RoundHistory.Add(result);
        match.LastPlayedCardId = null;

        if (match.Players[0].RoundWins >= 2 || match.Players[1].RoundWins >= 2)
        {
            match.Phase = MatchPhase.MatchEnd;
            match.Winner = match.Players[0].RoundWins >= 2 ? 0 : 1;
            // If somehow both 2 — higher rounds already decided
            if (match.Players[0].RoundWins >= 2 && match.Players[1].RoundWins >= 2)
                match.Winner = s0 >= s1 ? 0 : 1;
            match.AddLog($"{match.Players[match.Winner.Value].Name} wins the match!");
            return;
        }

        // Keep cards on board for UI sweep animation; client calls ContinueAfterRound.
        match.Phase = MatchPhase.RoundEnd;
    }

    /// <summary>
    /// Clears the board into discard piles and starts the next round after RoundEnd UI.
    /// </summary>
    public void ContinueAfterRound(MatchState match)
    {
        if (match.Phase != MatchPhase.RoundEnd)
            return;

        match.RoundNumber++;
        foreach (var p in match.Players)
        {
            var keepMonster = p.Faction == Faction.Monsters;
            p.ClearBoardToDiscard(keepMonster, _rng);
            p.HasPassed = false;
        }

        ClearWeather(match);

        if (match.RoundNumber == 3)
        {
            foreach (var p in match.Players.Where(p => p.Faction == Faction.Skellige))
                SkelligeRevive(match, p);
        }

        match.StartingPlayer = 1 - match.StartingPlayer;
        match.ActivePlayer = match.StartingPlayer;
        match.Phase = MatchPhase.Playing;
        match.LastRoundResult = null;
        match.AddLog($"Round {match.RoundNumber} begins. {match.CurrentPlayer.Name} starts.");
    }

    private void SkelligeRevive(MatchState match, PlayerState player)
    {
        var units = player.Discard.Where(c => c.Type == CardType.Unit && !c.IsHero)
            .OrderByDescending(c => c.BaseStrength)
            .Take(2)
            .ToList();
        foreach (var u in units)
        {
            player.Discard.Remove(u);
            var row = u.PlacedRow == CombatRow.None ? CombatRow.Melee : u.PlacedRow;
            u.PlacedRow = row;
            player.Rows[row].Add(u);
        }

        if (units.Count > 0)
            match.AddLog($"Skellige revived {units.Count} unit(s).");
    }

    private void TryFinishMulligan(MatchState match)
    {
        if (match.Players.Any(p => p.MulliganLeft > 0 && !p.IsBot))
            return;

        // Auto-finish bot mulligans
        foreach (var bot in match.Players.Where(p => p.IsBot && p.MulliganLeft > 0))
            bot.MulliganLeft = 0;

        if (match.Players.Any(p => p.MulliganLeft > 0))
            return;

        // Scoia'tael chooses who starts
        var scoia = match.Players.FirstOrDefault(p => p.Faction == Faction.Scoiatael);
        if (scoia != null && !match.ScoiataelChoseStart
            && match.Players.Count(p => p.Faction == Faction.Scoiatael) == 1)
        {
            match.Pending = new PendingAction
            {
                Kind = PendingKind.ChooseWhoStarts,
                PlayerIndex = scoia.Index,
                SourceCardId = Guid.Empty
            };
            match.Phase = MatchPhase.Mulligan;
            match.AddLog($"{scoia.Name} (Scoia'tael) chooses who starts.");
            return;
        }

        match.ActivePlayer = match.StartingPlayer;
        match.Phase = MatchPhase.Playing;
        match.AddLog($"Match starts. {match.CurrentPlayer.Name} goes first.");
    }

    public static void Draw(PlayerState player, int count)
    {
        for (var i = 0; i < count && player.Deck.Count > 0; i++)
        {
            var card = player.Deck[0];
            player.Deck.RemoveAt(0);
            player.Hand.Add(card);
        }
    }

    public void Shuffle(List<CardInstance> cards)
    {
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    private void ShuffleDiscardIntoDeck(PlayerState player)
    {
        player.Deck.AddRange(player.Discard);
        player.Discard.Clear();
        Shuffle(player.Deck);
    }

    private static void EnsurePhase(MatchState match, MatchPhase phase)
    {
        if (match.Phase != phase)
            throw new InvalidOperationException($"Expected phase {phase}, got {match.Phase}.");
    }
}
