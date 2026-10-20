using WebGwent.Game.Engine;
using WebGwent.Game.Models;

namespace WebGwent.Game.Catalog;

public static class PresetDecks
{
    public static DeckList NorthernRealms() => new(
        "Northern Realms — Foltest",
        Faction.NorthernRealms,
        "nr_leader_foltest_siege",
        [
            "nr_vernon_roche", "nr_john_natalis", "nr_esterad", "nr_philippa",
            "nr_ves", "nr_yarpen", "nr_keira", "nr_dandelion",
            "nr_sile", "nr_prince_stennis", "nr_thaler", "nr_dun_banner",
            "nr_sabrina", "nr_sheldon",
            "nr_crinfrid1", "nr_crinfrid2", "nr_crinfrid3",
            "nr_blue_stripes1", "nr_blue_stripes2", "nr_blue_stripes3",
            "nr_catapult1", "nr_catapult2", "nr_ballista", "nr_trebuchet1", "nr_trebuchet2",
            "nr_siege_tower", "nr_poor_infantry1", "nr_poor_infantry2", "nr_poor_infantry3",
            "nr_kaedweni1", "nr_kaedweni2", "nr_kaedweni3",
            "n_geralt", "n_yennefer", "n_villentretenmerth",
            "n_decoy1", "n_horn1", "n_frost", "n_clear", "n_scorch"
        ]);

    public static DeckList Nilfgaard() => new(
        "Nilfgaard — Emhyr",
        Faction.Nilfgaard,
        "ng_leader_emhyr_imperial",
        [
            "ng_letho", "ng_menno", "ng_tibor", "ng_morvran",
            "ng_cahir", "ng_stefan", "ng_shilard", "ng_young_evelyn",
            "ng_vattier", "ng_assire", "ng_fringilla",
            "ng_etolian1", "ng_etolian2", "ng_rainfarn", "ng_vanhemar",
            "ng_rotten1", "ng_rotten2",
            "ng_nausicaa1", "ng_nausicaa2", "ng_nausicaa3",
            "ng_brigade1", "ng_brigade2", "ng_brigade3",
            "ng_archer1", "ng_archer2", "ng_siege1", "ng_siege2",
            "ng_combat1", "ng_combat2",
            "n_avallach", "n_ciri",
            "n_decoy1", "n_horn1", "n_fog", "n_rain", "n_clear", "n_scorch"
        ]);

    public static DeckList Scoiatael() => new(
        "Scoia'tael — Francesca",
        Faction.Scoiatael,
        "st_leader_francesca_daisy",
        [
            "st_eithne", "st_iorveth", "st_isengrim", "st_milva", "st_yaevinn",
            "st_dennis", "st_ida", "st_filavandrel", "st_schirru",
            "st_havekar_nurse1", "st_havekar_nurse2",
            "st_havekar_smuggler1", "st_havekar_smuggler2", "st_havekar_smuggler3",
            "st_dwarf1", "st_dwarf2", "st_dwarf3",
            "st_elf1", "st_elf2", "st_elf3",
            "st_vrihedd1", "st_vrihedd2", "st_vrihedd3", "st_ciarach1", "st_ciarach2",
            "st_dol1", "st_dol2", "st_mahakam1", "st_mahakam2",
            "n_zoltan", "n_olgierd",
            "n_decoy1", "n_horn1", "n_frost", "n_fog", "n_clear", "n_scorch"
        ]);

    public static DeckList Monsters() => new(
        "Monsters — Eredin",
        Faction.Monsters,
        "mo_leader_eredin_king",
        [
            "mo_draug", "mo_kayran", "mo_imlerith", "mo_leshen",
            "mo_arachas_behemoth", "mo_arachas1", "mo_arachas2", "mo_arachas3",
            "mo_nekker1", "mo_nekker2", "mo_nekker3",
            "mo_ghoul1", "mo_ghoul2", "mo_ghoul3",
            "mo_bruxa", "mo_ekimmara", "mo_fleder", "mo_garkain", "mo_katakan",
            "mo_crone_brewess", "mo_crone_weavess", "mo_crone_whispess",
            "mo_fiend", "mo_griffin", "mo_werewolf", "mo_fotterscar",
            "mo_celaeno", "mo_harpy1", "mo_earth_elemental",
            "n_gaunter", "n_gaunter_darkness1", "n_gaunter_darkness2", "n_gaunter_darkness3",
            "n_decoy1", "n_horn1", "n_frost", "n_fog", "n_rain"
        ]);

    public static DeckList Skellige() => new(
        "Skellige — Crach an Craite",
        Faction.Skellige,
        "sk_leader_crach",
        [
            "sk_hjalmar", "sk_cerys", "sk_ermion", "sk_birna", "sk_hemdall", "sk_kambi",
            "sk_uldalryk", "sk_olaf", "sk_donar", "sk_madman_lugos", "sk_blueboy",
            "sk_light_longship1", "sk_light_longship2", "sk_light_longship3",
            "sk_war_longship1", "sk_war_longship2",
            "sk_shieldmaiden1", "sk_shieldmaiden2", "sk_shieldmaiden3",
            "sk_berserker1", "sk_berserker2", "sk_young_berserker1", "sk_young_berserker2",
            "sk_clan_hunter1", "sk_clan_hunter2",
            "sk_clan_warrior1", "sk_clan_warrior2", "sk_clan_warrior3",
            "sk_coral", "sk_holger", "sk_skald",
            "n_cow", "n_bovine", "n_triss",
            "n_decoy1", "n_horn1", "n_mardroeme", "n_frost", "n_rain", "n_clear"
        ]);

    public static IReadOnlyList<DeckList> All() =>
    [
        NorthernRealms(), Nilfgaard(), Scoiatael(), Monsters(), Skellige()
    ];

    public static DeckList ForFaction(Faction faction) => faction switch
    {
        Faction.NorthernRealms => NorthernRealms(),
        Faction.Nilfgaard => Nilfgaard(),
        Faction.Scoiatael => Scoiatael(),
        Faction.Monsters => Monsters(),
        Faction.Skellige => Skellige(),
        _ => NorthernRealms()
    };
}
