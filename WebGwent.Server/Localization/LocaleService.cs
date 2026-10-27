using WebGwent.Server.Services;

namespace WebGwent.Server.Localization;

public enum AppLanguage
{
    En,
    Ru
}

public sealed class LocaleService(CustomFactionRegistry customFactions)
{
    public AppLanguage Language { get; private set; } = AppLanguage.Ru;

    public event Action? Changed;

    public void SetLanguage(AppLanguage language)
    {
        if (Language == language) return;
        Language = language;
        Changed?.Invoke();
    }

    public void Toggle() =>
        SetLanguage(Language == AppLanguage.Ru ? AppLanguage.En : AppLanguage.Ru);

    public string T(string key) =>
        UiStrings.Get(key, Language);

    public string T(string key, params object[] args) =>
        string.Format(UiStrings.Get(key, Language), args);

    public string Faction(FactionLabel faction) => faction switch
    {
        FactionLabel.NorthernRealms => T("faction.nr"),
        FactionLabel.Nilfgaard => T("faction.ng"),
        FactionLabel.Scoiatael => T("faction.st"),
        FactionLabel.Monsters => T("faction.mo"),
        FactionLabel.Skellige => T("faction.sk"),
        _ => T("faction.neutral")
    };

    public string FactionName(WebGwent.Game.Models.Faction faction)
    {
        var custom = customFactions.DisplayName(faction, Language == AppLanguage.Ru);
        if (!string.IsNullOrWhiteSpace(custom))
            return custom;
        return faction switch
        {
            WebGwent.Game.Models.Faction.NorthernRealms => T("faction.nr"),
            WebGwent.Game.Models.Faction.Nilfgaard => T("faction.ng"),
            WebGwent.Game.Models.Faction.Scoiatael => T("faction.st"),
            WebGwent.Game.Models.Faction.Monsters => T("faction.mo"),
            WebGwent.Game.Models.Faction.Skellige => T("faction.sk"),
            WebGwent.Game.Models.Faction.Custom1 or WebGwent.Game.Models.Faction.Custom2
                or WebGwent.Game.Models.Faction.Custom3 or WebGwent.Game.Models.Faction.Custom4
                or WebGwent.Game.Models.Faction.Custom5 => T("faction.custom") + " " + ((int)faction - (int)WebGwent.Game.Models.Faction.Custom1 + 1),
            _ => T("faction.neutral")
        };
    }

    public string Row(WebGwent.Game.Models.CombatRow row) => row switch
    {
        WebGwent.Game.Models.CombatRow.Melee => T("row.melee"),
        WebGwent.Game.Models.CombatRow.Ranged => T("row.ranged"),
        WebGwent.Game.Models.CombatRow.Siege => T("row.siege"),
        _ => ""
    };

    public string CardName(string englishName) =>
        CardLocalizer.CardName(englishName, Language);

    public string Ability(WebGwent.Game.Models.Ability ability) =>
        CardLocalizer.AbilityName(ability, Language);

    public string Abilities(IEnumerable<WebGwent.Game.Models.Ability> abilities) =>
        CardLocalizer.FormatAbilities(abilities, Language);

    public string AbilityDescription(WebGwent.Game.Models.Ability ability) =>
        CardLocalizer.AbilityDescription(ability, Language);

    public string CardDescription(string? catalogDescription, IEnumerable<WebGwent.Game.Models.Ability> abilities) =>
        CardLocalizer.CardDescription(catalogDescription, abilities, Language);

    public string? ExtraInspectText(string? catalogDescription, IEnumerable<WebGwent.Game.Models.Ability> abilities) =>
        CardLocalizer.ExtraInspectText(catalogDescription, abilities, Language);
}

public enum FactionLabel
{
    Neutral,
    NorthernRealms,
    Nilfgaard,
    Scoiatael,
    Monsters,
    Skellige
}
