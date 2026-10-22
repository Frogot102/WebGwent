namespace WebGwent.Game.Models;

public enum Faction
{
    Neutral,
    NorthernRealms,
    Nilfgaard,
    Scoiatael,
    Monsters,
    Skellige
}

public enum CombatRow
{
    Melee,
    Ranged,
    Siege,
    None
}

public enum CardType
{
    Unit,
    Special,
    Leader
}

public enum Ability
{
    None,
    Hero,
    Spy,
    Medic,
    TightBond,
    MoraleBoost,
    Muster,
    Agile,
    Decoy,
    Scorch,
    CommandersHorn,
    WeatherFrost,
    WeatherFog,
    WeatherRain,
    WeatherClear,
    Berserker,
    Mardroeme,
    SummonAvenger,
    WeatherStorm,       // Skellige Storm: Ranged and Siege rows
    ScorchBattlefield   // unit Scorch on the whole battlefield (Clan Dimun Pirate)
}

public enum LeaderAbility
{
    None,

    // Northern Realms — Foltest
    FoltestKingOfTemeria,   // play Impenetrable Fog from the deck
    FoltestLordCommander,   // clear all weather
    FoltestSiegemaster,     // Commander's Horn on Siege
    FoltestSteelForged,     // destroy enemy's strongest Siege unit(s) if their Siege row is 10+
    FoltestSonOfMedell,     // destroy enemy's strongest Ranged unit(s) if their Ranged row is 10+

    // Nilfgaard — Emhyr
    EmhyrImperialMajesty,   // play Torrential Rain from the deck
    EmhyrEmperor,           // look at 3 random cards in the opponent's hand
    EmhyrWhiteFlame,        // passive: cancels the opponent's leader ability
    EmhyrRelentless,        // take a card from the opponent's discard pile
    EmhyrInvader,           // passive: Medics of both players revive a random unit

    // Scoia'tael — Francesca
    FrancescaPureblood,     // play Biting Frost from the deck
    FrancescaBeautiful,     // Commander's Horn on Ranged
    FrancescaDaisy,         // passive: draw an extra card at the start of the battle
    FrancescaQueen,         // destroy enemy's strongest Melee unit(s) if their Melee row is 10+
    FrancescaHope,          // move Agile units to the row where they are strongest

    // Monsters — Eredin
    EredinCommander,        // Commander's Horn on Melee
    EredinBringer,          // return a card from your discard pile to your hand
    EredinDestroyer,        // discard 2 cards, then take any card from your deck
    EredinKing,             // play any weather card from the deck
    EredinTreacherous,      // passive: spies of both players are twice as strong

    // Skellige
    CrachAnCraite,          // shuffle both discard piles back into the decks
    KingBran                // passive: weather only halves your units' strength
}

public static class LeaderAbilities
{
    public static bool IsPassive(LeaderAbility ability) =>
        ability is LeaderAbility.EmhyrWhiteFlame or LeaderAbility.FrancescaDaisy or LeaderAbility.KingBran
            or LeaderAbility.EmhyrInvader or LeaderAbility.EredinTreacherous;

    /// <summary>Passive leader effect that applies to both players while its owner's leader isn't cancelled.</summary>
    public static bool IsActiveAnywhere(MatchState match, LeaderAbility ability) =>
        match.Players.Any(p => p.ActiveLeaderAbility == ability);
}

public enum MatchPhase
{
    Mulligan,
    Playing,
    RoundEnd,
    MatchEnd
}

public enum WeatherEffect
{
    None,
    Frost,
    Fog,
    Rain,
    Storm
}

public enum GameMode
{
    Hotseat,
    VsBot,
    Online
}

/// <summary>
/// Bot strength. <see cref="High"/> is the current full AI (simulation + Monte Carlo look-ahead).
/// <see cref="DeathMarch"/> is the same pipeline with a larger look-ahead budget (Witcher 3 difficulty name).
/// </summary>
public enum BotDifficulty
{
    Easy,
    Medium,
    High,
    DeathMarch
}

public static class BotDifficultyRules
{
    public static bool RequiresPlus(BotDifficulty d) =>
        d is BotDifficulty.High or BotDifficulty.DeathMarch;
}
