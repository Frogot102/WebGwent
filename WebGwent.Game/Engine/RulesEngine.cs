using WebGwent.Game.Models;

namespace WebGwent.Game.Engine;

public sealed class RulesEngine
{
    public static readonly CombatRow[] BoardRows = [CombatRow.Melee, CombatRow.Ranged, CombatRow.Siege];

    /// <summary>Row-targeted Scorch (Villentretenmerth, Schirrú, Foltest, Francesca) needs the enemy row to total this much.</summary>
    public const int RowScorchThreshold = 10;

    private readonly Random _rng;

    public RulesEngine(Random? rng = null) => _rng = rng ?? Random.Shared;

    /// <summary>
    /// Passive leaders act before the mulligan: White Flame cancels the opponent's leader,
    /// Daisy of the Valley draws an 11th card. Passive leaders cannot be activated later.
    /// </summary>
    public void ApplyLeaderPassives(MatchState match)
    {
        foreach (var p in match.Players.Where(p => p.Leader?.LeaderAbility == LeaderAbility.EmhyrWhiteFlame).ToList())
        {
            var opp = match.Players[1 - p.Index];
            opp.LeaderCancelled = true;
            opp.LeaderUsed = true;
            match.AddLog($"{p.Name}'s White Flame cancels {opp.Name}'s leader.");
        }

        foreach (var p in match.Players)
        {
            if (p.Leader is null || p.LeaderCancelled || !LeaderAbilities.IsPassive(p.Leader.LeaderAbility))
                continue;
            p.LeaderUsed = true;
            if (p.Leader.LeaderAbility == LeaderAbility.FrancescaDaisy)
            {
                Draw(p, 1);
                match.AddLog($"{p.Name} draws an extra card (Daisy of the Valley).");
            }
        }
    }

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
        BeginRound(match);
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

        player.Hand.Remove(card);
        match.LastPlayedRow = null;
        try
        {
            if (card.Type == CardType.Special)
                ResolveSpecial(match, player, card, rowOverride, targetId);
            else
                ResolveUnit(match, player, card, rowOverride);
        }
        catch
        {
            // Validation failed before anything happened — give the card back.
            if (FindCardAnywhere(match, card.InstanceId) is null)
                player.Hand.Add(card);
            throw;
        }

        match.LastRoundResult = null;
        match.LastPlayedCardId = card.InstanceId;
        match.LastActionPlayer = playerIndex;

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
        var enemy = match.Players[1 - playerIndex];

        if (targetId is null && row is null && !pending.CanSkip)
            throw new InvalidOperationException("A choice is required.");
        if (targetId is Guid t && !pending.ValidTargets.Contains(t))
            throw new InvalidOperationException("Invalid target.");

        switch (pending.Kind)
        {
            case PendingKind.ChooseAgileRow:
            {
                if (row is null || !pending.ValidRows.Contains(row.Value))
                    throw new InvalidOperationException("Invalid row.");
                var source = FindCardAnywhere(match, pending.SourceCardId)
                             ?? throw new InvalidOperationException("Source card missing.");
                player.Discard.RemoveAll(c => c.InstanceId == source.InstanceId);
                match.Pending = null;
                DeployUnit(match, player, source, row.Value);
                break;
            }

            case PendingKind.ChooseMedicTarget:
                match.Pending = null;
                if (targetId is null)
                    match.AddLog($"{player.Name} skipped medic revive.");
                else
                    ReviveMedic(match, player, targetId.Value);
                break;

            case PendingKind.ChooseDecoyTarget:
            {
                if (targetId is null)
                    throw new InvalidOperationException("Invalid decoy target.");
                var decoy = FindCardAnywhere(match, pending.SourceCardId)
                            ?? throw new InvalidOperationException("Source card missing.");
                match.Pending = null;
                ApplyDecoy(match, player, decoy, targetId.Value);
                break;
            }

            case PendingKind.ChooseHornRow:
                if (row is null || !pending.ValidRows.Contains(row.Value))
                    throw new InvalidOperationException("Invalid row.");
                match.Pending = null;
                ApplyHornCard(match, player, row.Value);
                break;

            case PendingKind.ChooseMardroemeRow:
                if (row is null || !pending.ValidRows.Contains(row.Value))
                    throw new InvalidOperationException("Invalid row.");
                match.Pending = null;
                ApplyMardroemeCard(match, player, row.Value);
                break;

            case PendingKind.ChooseOwnDiscardCard:
                match.Pending = null;
                if (targetId is Guid ownId)
                    TakeIntoHand(match, player, player.Discard, ownId, "from their discard pile");
                break;

            case PendingKind.ChooseEnemyDiscardCard:
                match.Pending = null;
                if (targetId is Guid enemyId)
                    TakeIntoHand(match, player, enemy.Discard, enemyId, "from the opponent's discard pile");
                break;

            case PendingKind.ChooseHandDiscard:
            {
                var card = player.Hand.First(c => c.InstanceId == targetId);
                player.Hand.Remove(card);
                MoveToDiscard(player, card);
                var remaining = pending.Remaining - 1;
                match.Pending = null;
                if (remaining > 0 && player.Hand.Count > 0)
                {
                    match.Pending = new PendingAction
                    {
                        Kind = PendingKind.ChooseHandDiscard,
                        PlayerIndex = player.Index,
                        Remaining = remaining,
                        ValidTargets = player.Hand.Select(c => c.InstanceId).ToList()
                    };
                }
                else if (player.Deck.Count > 0)
                {
                    match.Pending = new PendingAction
                    {
                        Kind = PendingKind.ChooseDeckCard,
                        PlayerIndex = player.Index,
                        ValidTargets = player.Deck.Select(c => c.InstanceId).ToList()
                    };
                }
                break;
            }

            case PendingKind.ChooseDeckCard:
                match.Pending = null;
                if (targetId is Guid deckId)
                    TakeIntoHand(match, player, player.Deck, deckId, "from their deck");
                break;

            case PendingKind.ChooseDeckWeather:
                match.Pending = null;
                if (targetId is Guid weatherId)
                {
                    var weather = player.Deck.First(c => c.InstanceId == weatherId);
                    player.Deck.Remove(weather);
                    PlayWeatherCard(match, player, weather);
                }
                break;

            case PendingKind.RevealOpponentCards:
                match.Pending = null;
                break;

            default:
                throw new InvalidOperationException("Unsupported pending kind.");
        }

        if (match.Pending == null)
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
        if (player.Leader is null || player.LeaderUsed)
            throw new InvalidOperationException("Leader already used or missing.");
        if (player.LeaderCancelled)
            throw new InvalidOperationException("Leader ability was cancelled.");
        if (LeaderAbilities.IsPassive(player.Leader.LeaderAbility))
            throw new InvalidOperationException("Leader ability is passive.");

        var ability = player.Leader.LeaderAbility;
        if (ability == LeaderAbility.EredinDestroyer && player.Hand.Count < 2)
            throw new InvalidOperationException("Need at least 2 cards in hand.");

        match.LastRoundResult = null;
        match.LastPlayedCardId = null;
        match.LastPlayedRow = null;
        match.LastActionPlayer = playerIndex;
        player.LeaderUsed = true;
        match.AddLog($"{player.Name} used leader ability.");
        ApplyLeader(match, player, ability);

        if (match.Pending == null)
            AfterAction(match, playerIndex);
    }

    private void ApplyLeader(MatchState match, PlayerState player, LeaderAbility ability)
    {
        var enemy = match.Players[1 - player.Index];
        switch (ability)
        {
            case LeaderAbility.FoltestKingOfTemeria:
                PlayWeatherFromDeck(match, player, Ability.WeatherFog);
                break;
            case LeaderAbility.FoltestLordCommander:
                ClearWeather(match);
                match.AddLog("Weather cleared.");
                break;
            case LeaderAbility.FoltestSiegemaster:
                AddLeaderHorn(match, player, CombatRow.Siege);
                break;
            case LeaderAbility.FoltestSteelForged:
                ScorchEnemyRowIfStrong(match, player, CombatRow.Siege);
                break;
            case LeaderAbility.FoltestSonOfMedell:
                ScorchEnemyRowIfStrong(match, player, CombatRow.Ranged);
                break;

            case LeaderAbility.EmhyrImperialMajesty:
                PlayWeatherFromDeck(match, player, Ability.WeatherRain);
                break;
            case LeaderAbility.EmhyrEmperor:
            {
                var shown = enemy.Hand.OrderBy(_ => _rng.Next()).Take(3).Select(c => c.InstanceId).ToList();
                if (shown.Count == 0)
                {
                    match.AddLog($"{enemy.Name} has no cards to look at.");
                    break;
                }
                match.Pending = new PendingAction
                {
                    Kind = PendingKind.RevealOpponentCards,
                    PlayerIndex = player.Index,
                    ValidTargets = shown
                };
                match.AddLog($"{player.Name} looks at {shown.Count} of {enemy.Name}'s cards.");
                break;
            }
            case LeaderAbility.EmhyrRelentless:
                OfferCards(match, player, PendingKind.ChooseEnemyDiscardCard, enemy.Discard, "The opponent's discard pile is empty.");
                break;

            case LeaderAbility.FrancescaPureblood:
                PlayWeatherFromDeck(match, player, Ability.WeatherFrost);
                break;
            case LeaderAbility.FrancescaBeautiful:
                AddLeaderHorn(match, player, CombatRow.Ranged);
                break;
            case LeaderAbility.FrancescaQueen:
                ScorchEnemyRowIfStrong(match, player, CombatRow.Melee);
                break;
            case LeaderAbility.FrancescaHope:
                MoveAgileUnitsToBestRow(match, player);
                break;

            case LeaderAbility.EredinCommander:
                AddLeaderHorn(match, player, CombatRow.Melee);
                break;
            case LeaderAbility.EredinBringer:
                OfferCards(match, player, PendingKind.ChooseOwnDiscardCard, player.Discard, "Your discard pile is empty.");
                break;
            case LeaderAbility.EredinDestroyer:
                match.Pending = new PendingAction
                {
                    Kind = PendingKind.ChooseHandDiscard,
                    PlayerIndex = player.Index,
                    Remaining = 2,
                    ValidTargets = player.Hand.Select(c => c.InstanceId).ToList()
                };
                break;
            case LeaderAbility.EredinKing:
                OfferCards(match, player, PendingKind.ChooseDeckWeather,
                    player.Deck.Where(IsWeatherCard).ToList(), "No weather cards left in the deck.");
                break;

            case LeaderAbility.CrachAnCraite:
                foreach (var p in match.Players)
                    ShuffleDiscardIntoDeck(p);
                match.AddLog("Both discard piles were shuffled back into the decks.");
                break;
        }
    }

    private static void OfferCards(MatchState match, PlayerState player, PendingKind kind, List<CardInstance> source, string emptyMessage)
    {
        if (source.Count == 0)
        {
            match.AddLog(emptyMessage);
            return;
        }

        match.Pending = new PendingAction
        {
            Kind = kind,
            PlayerIndex = player.Index,
            ValidTargets = source.Select(c => c.InstanceId).ToList()
        };
    }

    private static void AddLeaderHorn(MatchState match, PlayerState player, CombatRow row)
    {
        // Leader horns do nothing on a row that already has a Commander's Horn.
        if (player.HornRows.Add(row))
            match.AddLog($"{player.Name}'s leader sounds the horn on {row}.");
        else
            match.AddLog($"{row} already has a Commander's Horn.");
        match.LastPlayedRow = row;
    }

    private void MoveAgileUnitsToBestRow(MatchState match, PlayerState player)
    {
        var moved = 0;
        foreach (var card in player.Rows[CombatRow.Melee].Concat(player.Rows[CombatRow.Ranged])
                     .Where(c => c.Type == CardType.Unit && !c.IsHero && c.Abilities.Contains(Ability.Agile))
                     .ToList())
        {
            var from = card.PlacedRow;
            var to = from == CombatRow.Melee ? CombatRow.Ranged : CombatRow.Melee;
            var before = ScoreCalculator.GetTotalScore(player, match);
            MoveOnBoard(player, card, to);
            if (ScoreCalculator.GetTotalScore(player, match) > before)
                moved++;
            else
                MoveOnBoard(player, card, from);
        }

        match.AddLog($"{player.Name} moved {moved} Agile unit(s).");
    }

    private static void MoveOnBoard(PlayerState player, CardInstance card, CombatRow to)
    {
        player.Rows[card.PlacedRow].Remove(card);
        card.PlacedRow = to;
        player.Rows[to].Add(card);
    }

    private void ResolveUnit(MatchState match, PlayerState player, CardInstance card, CombatRow? rowOverride)
    {
        CombatRow row;
        if (card.Abilities.Contains(Ability.Agile))
        {
            if (rowOverride is not (CombatRow.Melee or CombatRow.Ranged))
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

            row = rowOverride.Value;
        }
        else
        {
            row = card.PlacedRow;
            if (row is CombatRow.None)
                throw new InvalidOperationException("Unit has no row.");
        }

        DeployUnit(match, player, card, row);
    }

    /// <summary>Puts a unit on the board and triggers everything it does on arrival.</summary>
    private void DeployUnit(MatchState match, PlayerState player, CardInstance card, CombatRow row)
    {
        var isSpy = card.Abilities.Contains(Ability.Spy);
        var boardOwner = isSpy ? match.Players[1 - player.Index] : player;
        PlaceUnit(match, player, card, row, isSpy);

        if (card.Abilities.Contains(Ability.Scorch))
            ScorchEnemyRowIfStrong(match, player, row);
        if (card.Abilities.Contains(Ability.ScorchBattlefield))
            GlobalScorch(match, spare: card);

        if (card.Abilities.Contains(Ability.Mardroeme))
            boardOwner.MardroemeRows.Add(row);
        TransformBerserkers(match, boardOwner);

        if (card.Abilities.Contains(Ability.Medic))
        {
            var reviveTargets = player.Discard
                .Where(c => c.Type == CardType.Unit && !c.IsHero)
                .Select(c => c.InstanceId)
                .ToList();
            if (reviveTargets.Count > 0 && LeaderAbilities.IsActiveAnywhere(match, LeaderAbility.EmhyrInvader))
            {
                // Emhyr: Invader of the North — the revived unit is picked at random.
                ReviveMedic(match, player, reviveTargets[_rng.Next(reviveTargets.Count)]);
            }
            else if (reviveTargets.Count > 0)
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

    private void ResolveSpecial(MatchState match, PlayerState player, CardInstance card, CombatRow? row, Guid? targetId)
    {
        if (IsWeatherCard(card))
        {
            PlayWeatherCard(match, player, card);
        }
        else if (card.Abilities.Contains(Ability.CommandersHorn))
        {
            var valid = BoardRows.Where(r => !player.HornRows.Contains(r)).ToList();
            if (valid.Count == 0)
                throw new InvalidOperationException("NoHornRows");
            player.Discard.Add(card);
            if (row is CombatRow chosen && valid.Contains(chosen))
                ApplyHornCard(match, player, chosen);
            else
                AskForRow(match, player, card, PendingKind.ChooseHornRow, valid, "Commander's Horn");
        }
        else if (card.Abilities.Contains(Ability.Mardroeme))
        {
            var valid = BoardRows.Where(r => !player.MardroemeRows.Contains(r)).ToList();
            if (valid.Count == 0)
                throw new InvalidOperationException("NoMardroemeRows");
            player.Discard.Add(card);
            if (row is CombatRow chosen && valid.Contains(chosen))
                ApplyMardroemeCard(match, player, chosen);
            else
                AskForRow(match, player, card, PendingKind.ChooseMardroemeRow, valid, "Mardroeme");
        }
        else if (card.Abilities.Contains(Ability.Scorch))
        {
            MoveToDiscard(player, card);
            match.AddLog($"{player.Name} played Scorch.");
            GlobalScorch(match);
        }
        else if (card.Abilities.Contains(Ability.Decoy))
        {
            // Own side only — includes enemy spies sitting on your rows. Heroes cannot be Decoyed (TW3).
            var targets = DecoyTargets(player);
            if (targets.Count == 0)
                throw new InvalidOperationException("NoDecoyTargets");

            if (targetId is Guid chosen && targets.Contains(chosen))
            {
                ApplyDecoy(match, player, card, chosen);
                return;
            }

            // Park the Decoy in discard until a target is picked.
            player.Discard.Add(card);
            match.Pending = new PendingAction
            {
                Kind = PendingKind.ChooseDecoyTarget,
                PlayerIndex = player.Index,
                SourceCardId = card.InstanceId,
                ValidTargets = targets
            };
            match.AddLog($"{player.Name} must choose a Decoy target.");
        }
        else
        {
            MoveToDiscard(player, card);
            match.AddLog($"{player.Name} played special {card.Name}.");
        }
    }

    public static List<Guid> DecoyTargets(PlayerState player) =>
        player.AllBoardCards()
            .Where(c => c.Type == CardType.Unit && !c.IsHero)
            .Select(c => c.InstanceId)
            .ToList();

    private static void AskForRow(MatchState match, PlayerState player, CardInstance card, PendingKind kind, List<CombatRow> rows, string what)
    {
        match.Pending = new PendingAction
        {
            Kind = kind,
            PlayerIndex = player.Index,
            SourceCardId = card.InstanceId,
            ValidRows = rows
        };
        match.AddLog($"{player.Name} must choose a row for {what}.");
    }

    private static void ApplyHornCard(MatchState match, PlayerState player, CombatRow row)
    {
        player.HornRows.Add(row);
        match.LastPlayedRow = row;
        match.AddLog($"{player.Name} played Commander's Horn on {row}.");
    }

    private void ApplyMardroemeCard(MatchState match, PlayerState player, CombatRow row)
    {
        player.MardroemeRows.Add(row);
        match.LastPlayedRow = row;
        match.AddLog($"{player.Name} played Mardroeme on {row}.");
        TransformBerserkers(match, player);
    }

    public static bool IsWeatherCard(CardInstance card) =>
        card.Abilities.Any(a => a is Ability.WeatherFrost or Ability.WeatherFog or Ability.WeatherRain
            or Ability.WeatherStorm or Ability.WeatherClear);

    private static void PlayWeatherCard(MatchState match, PlayerState player, CardInstance card)
    {
        if (card.Abilities.Contains(Ability.WeatherFrost))
        {
            match.MeleeWeather = WeatherEffect.Frost;
            match.LastPlayedRow = CombatRow.Melee;
        }
        else if (card.Abilities.Contains(Ability.WeatherFog))
        {
            match.RangedWeather = WeatherEffect.Fog;
            match.LastPlayedRow = CombatRow.Ranged;
        }
        else if (card.Abilities.Contains(Ability.WeatherRain))
        {
            match.SiegeWeather = WeatherEffect.Rain;
            match.LastPlayedRow = CombatRow.Siege;
        }
        else if (card.Abilities.Contains(Ability.WeatherStorm))
        {
            match.RangedWeather = WeatherEffect.Storm;
            match.SiegeWeather = WeatherEffect.Storm;
            match.LastPlayedRow = CombatRow.Ranged;
        }
        else
        {
            ClearWeather(match);
            match.LastPlayedRow = null;
        }

        MoveToDiscard(player, card);
        match.AddLog($"{player.Name} played {card.Name}.");
    }

    private static void PlayWeatherFromDeck(MatchState match, PlayerState player, Ability weather)
    {
        var card = player.Deck.FirstOrDefault(c => c.Abilities.Contains(weather));
        if (card is null)
        {
            match.AddLog($"No {weather} card left in {player.Name}'s deck.");
            return;
        }

        player.Deck.Remove(card);
        PlayWeatherCard(match, player, card);
    }

    private static void TakeIntoHand(MatchState match, PlayerState player, List<CardInstance> from, Guid id, string where)
    {
        var card = from.First(c => c.InstanceId == id);
        from.Remove(card);
        card.ResetToDefault();
        player.Hand.Add(card);
        match.AddLog($"{player.Name} took {card.Name} {where}.");
    }

    private static void ApplyDecoy(MatchState match, PlayerState player, CardInstance decoy, Guid targetId)
    {
        // Decoy may be parked in discard
        player.Discard.RemoveAll(c => c.InstanceId == decoy.InstanceId);

        var target = player.AllBoardCards().FirstOrDefault(c => c.InstanceId == targetId)
                     ?? throw new InvalidOperationException("Decoy target not found.");
        var row = target.PlacedRow;

        player.Rows[row].Remove(target);
        target.ResetToDefault();
        player.Hand.Add(target);
        decoy.PlacedRow = row;
        decoy.BaseStrength = 0;
        player.Rows[row].Add(decoy);
        match.LastPlayedRow = row;
        match.AddLog($"{player.Name} decoyed {target.Name} back to hand.");
    }

    private void ReviveMedic(MatchState match, PlayerState player, Guid targetId)
    {
        var card = player.Discard.FirstOrDefault(c => c.InstanceId == targetId)
                   ?? throw new InvalidOperationException("Revive target missing.");
        if (card.IsHero || card.Type != CardType.Unit)
            throw new InvalidOperationException("Cannot revive that card.");

        player.Discard.Remove(card);
        var row = card.Abilities.Contains(Ability.Agile)
            ? BestAgileRow(match, player, card)
            : card.PlacedRow == CombatRow.None ? CombatRow.Melee : card.PlacedRow;
        match.AddLog($"{player.Name} revived {card.Name}.");
        // The revived card is played as if from hand: spies draw, musters muster, medics chain.
        DeployUnit(match, player, card, row);
    }

    private static CombatRow BestAgileRow(MatchState match, PlayerState player, CardInstance card)
    {
        var best = CombatRow.Melee;
        var bestScore = int.MinValue;
        foreach (var row in new[] { CombatRow.Melee, CombatRow.Ranged })
        {
            card.PlacedRow = row;
            player.Rows[row].Add(card);
            var score = ScoreCalculator.GetTotalScore(player, match);
            player.Rows[row].Remove(card);
            if (score > bestScore)
            {
                bestScore = score;
                best = row;
            }
        }

        return best;
    }

    private void Muster(MatchState match, PlayerState owner, CardInstance card)
    {
        if (string.IsNullOrEmpty(card.MusterGroup))
            return;

        // TW3: Muster only pulls the matching cards from the deck — copies in hand stay there.
        var pulled = owner.Deck.Where(c => c.MusterGroup == card.MusterGroup).ToList();
        foreach (var c in pulled)
        {
            owner.Deck.Remove(c);
            var row = c.PlacedRow == CombatRow.None || c.Abilities.Contains(Ability.Agile)
                ? card.PlacedRow
                : c.PlacedRow;
            var target = c.Abilities.Contains(Ability.Spy) ? match.Players[1 - owner.Index] : owner;
            c.PlacedRow = row;
            target.Rows[row].Add(c);
        }

        if (pulled.Count > 0)
            match.AddLog($"Muster summoned {pulled.Count} more card(s).");
    }

    private static void SummonAvenger(MatchState match, PlayerState boardOwner, CardInstance card)
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

    /// <summary>
    /// Berserkers on a row with Mardroeme turn into Vildkaarls:
    /// Berserker → Vildkaarl (14, Morale Boost), Young Berserker → Young Vildkaarl (8, Tight Bond).
    /// </summary>
    private static void TransformBerserkers(MatchState match, PlayerState player)
    {
        foreach (var row in player.MardroemeRows)
        {
            foreach (var card in player.Rows[row].Where(c => c.Abilities.Contains(Ability.Berserker) && !c.Transformed))
            {
                var young = card.DefaultName.StartsWith("Young", StringComparison.Ordinal);
                card.Transformed = true;
                card.Name = young ? "Young Vildkaarl" : "Vildkaarl";
                card.BaseStrength = young ? 8 : 14;
                card.Abilities = young ? [Ability.TightBond] : [Ability.MoraleBoost];
                match.AddLog($"A Berserker transformed into {card.Name}.");
            }
        }
    }

    /// <summary>Destroys the strongest non-Hero unit(s) on the whole battlefield (<paramref name="spare"/> excluded).</summary>
    private void GlobalScorch(MatchState match, CardInstance? spare = null)
    {
        var all = match.Players.SelectMany(p => p.AllBoardCards().Select(c => (p, c)))
            .Where(x => !x.c.IsHero && x.c.Type == CardType.Unit && x.c.InstanceId != spare?.InstanceId)
            .ToList();
        if (all.Count == 0) return;

        var max = all.Max(x => ScoreCalculator.GetCardStrength(x.c, x.p, match));
        var victims = all.Where(x => ScoreCalculator.GetCardStrength(x.c, x.p, match) == max)
            .Select(x => x.c).ToList();
        DestroyUnits(match, victims);
    }

    /// <summary>
    /// Destroys the strongest non-Hero unit(s) in the enemy's row, but only if that row totals 10 or more.
    /// </summary>
    private void ScorchEnemyRowIfStrong(MatchState match, PlayerState player, CombatRow row)
    {
        var enemy = match.Players[1 - player.Index];
        if (ScoreCalculator.GetRowScore(enemy, row, match) < RowScorchThreshold)
        {
            match.AddLog($"Scorch on {row}: the enemy row is below {RowScorchThreshold}.");
            return;
        }

        var units = enemy.Rows[row].Where(c => c.Type == CardType.Unit && !c.IsHero).ToList();
        if (units.Count == 0) return;
        var max = units.Max(c => ScoreCalculator.GetCardStrength(c, enemy, match));
        if (max <= 0) return;
        DestroyUnits(match, units.Where(c => ScoreCalculator.GetCardStrength(c, enemy, match) == max).ToList());
        match.AddLog($"Scorch destroyed the strongest enemy {row} unit(s).");
    }

    private void DestroyUnits(MatchState match, List<CardInstance> units)
    {
        foreach (var unit in units)
        {
            var owner = FindOwner(match, unit);
            if (owner is null) continue;
            owner.Rows[unit.PlacedRow].Remove(unit);
            var row = unit.PlacedRow;
            MoveToDiscard(owner, unit);
            if (unit.Abilities.Contains(Ability.SummonAvenger))
            {
                unit.PlacedRow = row;
                SummonAvenger(match, owner, unit);
                unit.ResetToDefault();
            }
        }
    }

    private static void MoveToDiscard(PlayerState owner, CardInstance card)
    {
        card.ResetToDefault();
        owner.Discard.Add(card);
    }

    private static PlayerState? FindOwner(MatchState match, CardInstance card)
    {
        return match.Players.FirstOrDefault(p => p.AllBoardCards().Any(c => c.InstanceId == card.InstanceId));
    }

    public static CardInstance? FindCardAnywhere(MatchState match, Guid id)
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

    private static void ClearWeather(MatchState match)
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
        else if (match.Players[0].Faction == Faction.Nilfgaard && match.Players[1].Faction != Faction.Nilfgaard)
            roundWinner = 0; // Nilfgaard wins draws
        else if (match.Players[1].Faction == Faction.Nilfgaard && match.Players[0].Faction != Faction.Nilfgaard)
            roundWinner = 1;
        else
            roundWinner = null;

        if (roundWinner is int w)
        {
            match.Players[w].RoundWins++;
            match.Players[1 - w].Lives--;
            match.AddLog($"{match.Players[w].Name} wins the round.");
            if (match.Players[w].Faction == Faction.NorthernRealms)
            {
                Draw(match.Players[w], 1);
                match.AddLog($"{match.Players[w].Name} draws (Northern Realms).");
            }
        }
        else
        {
            // TW3: a drawn round costs both players a crystal.
            foreach (var p in match.Players)
                p.Lives--;
            match.AddLog("Round is a draw — both players lose a crystal.");
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

        var alive0 = match.Players[0].Lives > 0;
        var alive1 = match.Players[1].Lives > 0;
        if (!alive0 || !alive1)
        {
            match.Phase = MatchPhase.MatchEnd;
            match.Winner = alive0 ? 0 : alive1 ? 1 : null;
            match.AddLog(match.Winner is int mw
                ? $"{match.Players[mw].Name} wins the match!"
                : "The match ends in a draw.");
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
        BeginRound(match);
        match.AddLog($"Round {match.RoundNumber} begins. {match.CurrentPlayer.Name} starts.");
    }

    /// <summary>
    /// Skellige: two random units from the discard pile return to the battlefield at the start of round 3.
    /// </summary>
    private void SkelligeRevive(MatchState match, PlayerState player)
    {
        var units = player.Discard.Where(c => c.Type == CardType.Unit)
            .OrderBy(_ => _rng.Next())
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
        BeginRound(match);
        match.AddLog($"Match starts. {match.CurrentPlayer.Name} goes first.");
    }

    private static void BeginRound(MatchState match)
    {
        foreach (var p in match.Players)
            p.HandAtRoundStart = p.Hand.Count;
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
        foreach (var card in player.Discard)
            card.ResetToDefault();
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
