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
    SummonAvenger
}

public enum LeaderAbility
{
    None,
    ClearWeather,
    FoltestStorm,
    FoltestFog,
    FoltestRain,
    FoltestSiegeMaster,
    FoltestSonOfMedell,
    EmhyrSpy,
    EmhyrLookout,
    EmhyrImperial,
    EmhyrWhiteFlame,
    EmhyrRelentless,
    FrancescaHope,
    FrancescaDaisy,
    FrancescaQueen,
    EredinKing,
    EredinCommander,
    EredinDestroyer,
    CrachAnCraite,
    KingBran
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
    Rain
}

public enum GameMode
{
    Hotseat,
    VsBot,
    Online
}
