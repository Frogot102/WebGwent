using WebGwent.Game.Models;
using WebGwent.Game.Services;
using WebGwent.Server.Localization;

namespace WebGwent.Server.Services;

/// <summary>
/// Card pictures in wwwroot/img/cards: The Witcher 3 Gwent card faces, scaled to 260 px and shown
/// whole — English faces from the Witcher Wiki (witcher.fandom.com) and, in img/cards/ru under the
/// same names, the Russian faces from the Russian Witcher Wiki (witcher.fandom.com/ru). Coral and
/// Nilfgaardian Knight have no TW3 face, so they use their standalone Gwent art in both languages.
/// Custom cards have no picture and keep the plain frame.
/// </summary>
public static class CardArt
{
    private const string Folder = "img/cards/";
    private const string RussianFolder = "img/cards/ru/";

    /// <summary>
    /// Cards with no Russian TW3 face on the Russian wiki (the wiki's Siegemaster page shows the
    /// Steel-Forged face): they keep the English face with the Russian name laid over it.
    /// </summary>
    private static readonly HashSet<string> EnglishOnly = ["schirru", "foltest-the-siegemaster"];

    /// <summary>
    /// Russian faces made before TW3 patch 4.04, which still print the card's old name (Василиск,
    /// Лучник Бурой Хоругви, Вампиры: брукса, the swapped Eredin titles …): the current name is laid
    /// over the printed one.
    /// </summary>
    private static readonly HashSet<string> RenamedInRussian =
    [
        "black-infantry-archer-1", "black-infantry-archer-2", "bovine-defense-force", "clan-dimun-pirate",
        "cockatrice", "crinfrid-reavers-dragon-hunter", "eredin-bringer-of-death",
        "eredin-commander-of-the-red-riders", "eredin-destroyer-of-worlds", "eredin-king-of-the-wild-hunt",
        "vampire-bruxa", "vampire-ekimmara", "vampire-fleder", "vampire-garkain", "vampire-katakan",
        "vrihedd-brigade-recruit", "vrihedd-brigade-veteran-1", "vrihedd-brigade-veteran-2"
    ];

    /// <summary>Plain illustrations: no TW3 frame, so no printed strength either.</summary>
    private static readonly HashSet<string> FullArt = ["coral", "nilfgaardian-knight"];

    private static readonly Dictionary<string, string> ByDefinitionId = new(StringComparer.Ordinal)
    {
        ["n_decoy1"] = "decoy",
        ["n_decoy2"] = "decoy",
        ["n_decoy3"] = "decoy",
        ["n_horn1"] = "commanders-horn",
        ["n_horn2"] = "commanders-horn",
        ["n_horn3"] = "commanders-horn",
        ["n_scorch"] = "scorch",
        ["n_scorch2"] = "scorch",
        ["n_scorch3"] = "scorch",
        ["n_frost"] = "biting-frost",
        ["n_frost2"] = "biting-frost",
        ["n_frost3"] = "biting-frost",
        ["n_fog"] = "impenetrable-fog",
        ["n_fog2"] = "impenetrable-fog",
        ["n_fog3"] = "impenetrable-fog",
        ["n_rain"] = "torrential-rain",
        ["n_rain2"] = "torrential-rain",
        ["n_rain3"] = "torrential-rain",
        ["n_clear"] = "clear-weather",
        ["n_clear2"] = "clear-weather",
        ["n_clear3"] = "clear-weather",
        ["n_mardroeme"] = "mardroeme",
        ["n_mardroeme2"] = "mardroeme",
        ["n_mardroeme3"] = "mardroeme",
        ["n_storm1"] = "skellige-storm",
        ["n_storm2"] = "skellige-storm",
        ["n_storm3"] = "skellige-storm",
        ["n_geralt"] = "geralt",
        ["n_ciri"] = "cirilla",
        ["n_yennefer"] = "yennefer",
        ["n_triss"] = "triss",
        ["n_villentretenmerth"] = "villentretenmerth",
        ["n_avallach"] = "mysterious-elf",
        ["n_zoltan"] = "zoltan",
        ["n_vesemir"] = "vesemir",
        ["n_regis"] = "emiel-regis",
        ["n_olgierd"] = "olgierd-von-everec",
        ["n_gaunter"] = "gaunt-odimm",
        ["n_gaunter_darkness1"] = "gaunter-odimm-darkness",
        ["n_gaunter_darkness2"] = "gaunter-odimm-darkness",
        ["n_gaunter_darkness3"] = "gaunter-odimm-darkness",
        ["n_cow"] = "cow",
        ["n_bovine"] = "bovine-defense-force",
        ["nr_leader_foltest_siege"] = "foltest-the-siegemaster",
        ["nr_leader_foltest_king"] = "foltest-king-of-temeria",
        ["nr_leader_foltest_lord"] = "foltest-lord-commander-of-the-north",
        ["nr_leader_foltest_son"] = "foltest-son-of-medell",
        ["nr_leader_foltest_iron"] = "foltest-the-steel-forged",
        ["nr_vernon_roche"] = "vernon-roche",
        ["nr_john_natalis"] = "john-natalis",
        ["nr_esterad"] = "esterad-thyssen",
        ["nr_philippa"] = "philippa-eilhart",
        ["nr_ves"] = "ves",
        ["nr_siegfried"] = "siegfried-of-denesle",
        ["nr_yarpen"] = "yarpen-zigrin",
        ["nr_keira"] = "keira-metz",
        ["nr_dandelion"] = "dandelion",
        ["nr_sile"] = "sile-de-tansarville",
        ["nr_prince_stennis"] = "prince-stennis",
        ["nr_crinfrid1"] = "crinfrid-reavers-dragon-hunter",
        ["nr_crinfrid2"] = "crinfrid-reavers-dragon-hunter",
        ["nr_crinfrid3"] = "crinfrid-reavers-dragon-hunter",
        ["nr_blue_stripes1"] = "blue-stripes-commando-1",
        ["nr_blue_stripes2"] = "blue-stripes-commando-1",
        ["nr_blue_stripes3"] = "blue-stripes-commando-1",
        ["nr_catapult1"] = "catapult-1",
        ["nr_catapult2"] = "catapult-1",
        ["nr_ballista"] = "ballista",
        ["nr_ballista2"] = "ballista-2",
        ["nr_trebuchet1"] = "trebuchet-1",
        ["nr_trebuchet2"] = "trebuchet-2",
        ["nr_siege_tower"] = "siege-tower",
        ["nr_poor_infantry1"] = "poor-fucking-infantry",
        ["nr_poor_infantry2"] = "poor-fucking-infantry",
        ["nr_poor_infantry3"] = "poor-fucking-infantry",
        ["nr_poor_infantry4"] = "poor-fucking-infantry",
        ["nr_redanian1"] = "redanian-foot-soldier-1",
        ["nr_redanian2"] = "redanian-foot-soldier-2",
        ["nr_kaedweni1"] = "kaedweni-siege-expert-1",
        ["nr_kaedweni2"] = "kaedweni-siege-expert-1",
        ["nr_kaedweni3"] = "kaedweni-siege-expert-1",
        ["nr_thaler"] = "thaler",
        ["nr_dun_banner"] = "dun-banner-medic",
        ["nr_sabrina"] = "sabrina-glevissig",
        ["nr_sheldon"] = "sheldon-skaggs",
        ["nr_dethmold"] = "dethmold",
        ["nr_dijkstra"] = "sigismund-dijkstra",
        ["ng_leader_emhyr_imperial"] = "emhyr-var-emreis-his-imperial-majesty",
        ["ng_leader_emhyr_white"] = "emhyr-var-emreis-the-white-flame",
        ["ng_leader_emhyr_invader"] = "emhyr-var-emreis-invader-of-the-north",
        ["ng_leader_emhyr_relentless"] = "emhyr-var-emreis-the-relentless",
        ["ng_leader_emhyr_emperor"] = "emhyr-var-emreis-emperor-of-nilfgaard",
        ["ng_letho"] = "letho-of-gulet",
        ["ng_menno"] = "menno-coehoorn",
        ["ng_cahir"] = "cahir-mawr-dyffryn-aep-ceallach",
        ["ng_stefan"] = "stefan-skellen",
        ["ng_shilard"] = "shilard-fitz-oesterlen",
        ["ng_young_evelyn"] = "young-emmisary-1",
        ["ng_young_emissary2"] = "young-emmisary-2",
        ["ng_vattier"] = "vattier-de-rideaux",
        ["ng_albrich"] = "albrich",
        ["ng_sweers"] = "sweers",
        ["ng_assire"] = "assire-var-anahid",
        ["ng_fringilla"] = "fringilla-vigo",
        ["ng_cynthia"] = "cynthia",
        ["ng_renuald"] = "renuald-aep-matsen",
        ["ng_rotten1"] = "rotten-mangonel",
        ["ng_rotten2"] = "rotten-mangonel",
        ["ng_nausicaa1"] = "nausicaa-cavalry-rider-1",
        ["ng_nausicaa2"] = "nausicaa-cavalry-rider-1",
        ["ng_nausicaa3"] = "nausicaa-cavalry-rider-1",
        ["ng_brigade1"] = "impera-brigade-guard-1",
        ["ng_brigade2"] = "impera-brigade-guard-1",
        ["ng_brigade3"] = "impera-brigade-guard-1",
        ["ng_brigade4"] = "impera-brigade-guard-1",
        ["ng_archer1"] = "black-infantry-archer-1",
        ["ng_archer2"] = "black-infantry-archer-2",
        ["ng_siege1"] = "siege-engineer",
        ["ng_siege2"] = "heavy-zerrikanian-fire-scorpion",
        ["ng_combat1"] = "nilfgaardian-knight",
        ["ng_combat2"] = "nilfgaardian-knight",
        ["ng_combat3"] = "puttkamer",
        ["ng_tibor"] = "tibor-eggebracht",
        ["ng_morvran"] = "morvran-voorhis",
        ["ng_etolian1"] = "etolian-auxiliary-archers-1",
        ["ng_etolian2"] = "etolian-auxiliary-archers-2",
        ["ng_rainfarn"] = "rainfarn",
        ["ng_vanhemar"] = "vanhemar",
        ["ng_morteisen"] = "morteisen",
        ["ng_vreemde"] = "vreemde",
        ["ng_siege_technician"] = "siege-technician",
        ["ng_zerrikanian_scorpion"] = "zerrikanian-fire-scorpion",
        ["st_leader_francesca_daisy"] = "francesca-findabair-daisy-of-the-valley",
        ["st_leader_francesca_hope"] = "francesca-findabair-hope-of-the-aen-seidhe",
        ["st_leader_francesca_queen"] = "francesca-findabair-queen-of-dol-blathanna",
        ["st_leader_francesca_pureblood"] = "francesca-findabair-pureblood-elf",
        ["st_leader_francesca_beautiful"] = "francesca-findabair-the-beautiful",
        ["st_eithne"] = "eithne",
        ["st_iorveth"] = "iorveth",
        ["st_isengrim"] = "isengrim-faoiltiarna",
        ["st_milva"] = "milva",
        ["st_yaevinn"] = "yaevinn",
        ["st_dennis"] = "dennis-cranmer",
        ["st_ida"] = "ida-emean-aep-sivney",
        ["st_filavandrel"] = "filavandrel-aen-fidhail",
        ["st_toruviel"] = "toruviel",
        ["st_riordain"] = "riordain",
        ["st_havekar_nurse1"] = "havekar-healer-1",
        ["st_havekar_nurse2"] = "havekar-healer-2",
        ["st_havekar_nurse3"] = "havekar-healer-3",
        ["st_havekar_smuggler1"] = "havekar-smuggler-1",
        ["st_havekar_smuggler2"] = "havekar-smuggler-2",
        ["st_havekar_smuggler3"] = "havekar-smuggler-3",
        ["st_dwarf1"] = "dwarevn-skirmisher-1",
        ["st_dwarf2"] = "dwarevn-skirmisher-2",
        ["st_dwarf3"] = "dwarevn-skirmisher-3",
        ["st_elf1"] = "elven-skirmisher-1",
        ["st_elf2"] = "elven-skirmisher-2",
        ["st_elf3"] = "elven-skirmisher-3",
        ["st_vrihedd1"] = "vrihedd-brigade-veteran-1",
        ["st_vrihedd3"] = "vrihedd-brigade-veteran-2",
        ["st_vrihedd2"] = "vrihedd-brigade-recruit",
        ["st_ciarach1"] = "ciaran-aep-easnillien",
        ["st_ciarach2"] = "barclay-els",
        ["st_dol1"] = "dol-blathanna-archer",
        ["st_dol2"] = "dol-blathanna-scout-1",
        ["st_dol_scout2"] = "dol-blathanna-scout-2",
        ["st_dol_scout3"] = "dol-blathanna-scout-3",
        ["st_schirru"] = "schirru",
        ["st_mahakam1"] = "mahakaman-defender-1",
        ["st_mahakam2"] = "mahakaman-defender-2",
        ["st_mahakam3"] = "mahakaman-defender-3",
        ["st_mahakam4"] = "mahakaman-defender-4",
        ["st_mahakam5"] = "mahakaman-defender-5",
        ["st_saesenthessis"] = "saesenthessis",
        ["mo_leader_eredin_king"] = "eredin-king-of-the-wild-hunt",
        ["mo_leader_eredin_cmd"] = "eredin-commander-of-the-red-riders",
        ["mo_leader_eredin_destroyer"] = "eredin-destroyer-of-worlds",
        ["mo_leader_eredin_bringer"] = "eredin-bringer-of-death",
        ["mo_leader_eredin_treacherous"] = "eredin-the-treacherous",
        ["mo_draug"] = "draug",
        ["mo_kayran"] = "kayran",
        ["mo_imlerith"] = "imlerith",
        ["mo_leshen"] = "leshen",
        ["mo_forktail"] = "forktail",
        ["mo_earth_elemental"] = "earth-elemental",
        ["mo_fiend"] = "fiend",
        ["mo_plague_maiden"] = "plague-maiden",
        ["mo_griffin"] = "griffin",
        ["mo_werewolf"] = "werewolf",
        ["mo_fotterscar"] = "frightener",
        ["mo_arachas1"] = "arachas-1",
        ["mo_arachas2"] = "arachas-2",
        ["mo_arachas3"] = "arachas-3",
        ["mo_nekker1"] = "nekker-1",
        ["mo_nekker2"] = "nekker-2",
        ["mo_nekker3"] = "nekker-3",
        ["mo_ghoul1"] = "ghoul-1",
        ["mo_ghoul2"] = "ghoul-2",
        ["mo_ghoul3"] = "ghoul-3",
        ["mo_celaeno"] = "celaeno-harpy",
        ["mo_harpy1"] = "harpy",
        ["mo_harpy2"] = "harpy",
        ["mo_wyvern"] = "wyvern",
        ["mo_foglet1"] = "foglet",
        ["mo_foglet2"] = "foglet",
        ["mo_botchling"] = "botchling",
        ["mo_bruxa"] = "vampire-bruxa",
        ["mo_ekimmara"] = "vampire-ekimmara",
        ["mo_fleder"] = "vampire-fleder",
        ["mo_garkain"] = "vampire-garkain",
        ["mo_katakan"] = "vampire-katakan",
        ["mo_crone_brewess"] = "crone-brewess",
        ["mo_crone_weavess"] = "crone-weavess",
        ["mo_crone_whispess"] = "crone-whispess",
        ["mo_arachas_behemoth"] = "arachas-behemoth",
        ["mo_endrega"] = "endrega",
        ["mo_cockatrice"] = "cockatrice",
        ["mo_gargoyle"] = "gargoyle",
        ["mo_grave_hag"] = "grave-hag",
        ["mo_ice_giant"] = "ice-giant",
        ["mo_fire_elemental"] = "fire-elemental",
        ["mo_toad"] = "toad",
        ["sk_leader_crach"] = "crach-an-craite",
        ["sk_leader_bran"] = "king-bran",
        ["sk_hjalmar"] = "hjalmar",
        ["sk_cerys"] = "cerys",
        ["sk_ermion"] = "ermion",
        ["sk_birna"] = "birna-bran",
        ["sk_uldalryk"] = "udalryk",
        ["sk_olaf"] = "olaf",
        ["sk_donar"] = "donar-an-hindar",
        ["sk_madman_lugos"] = "madman-lugos",
        ["sk_blueboy"] = "blueboy-lugos",
        ["sk_svanrige"] = "svanrige",
        ["sk_light_longship1"] = "light-longship",
        ["sk_light_longship2"] = "light-longship",
        ["sk_light_longship3"] = "light-longship",
        ["sk_war_longship1"] = "war-longship",
        ["sk_war_longship2"] = "war-longship",
        ["sk_war_longship3"] = "war-longship",
        ["sk_shieldmaiden1"] = "clan-drummond-shield-maiden-1",
        ["sk_shieldmaiden2"] = "clan-drummond-shield-maiden-2",
        ["sk_shieldmaiden3"] = "clan-drummond-shield-maiden-3",
        ["sk_berserker1"] = "berserker",
        ["sk_berserker2"] = "berserker",
        ["sk_young_berserker1"] = "young-berserker",
        ["sk_young_berserker2"] = "young-berserker",
        ["sk_young_berserker3"] = "young-berserker",
        ["sk_clan_hunter1"] = "clan-dimun-pirate",
        ["sk_clan_hunter2"] = "clan-dimun-pirate",
        ["sk_clan_warrior1"] = "clan-an-craite-warrior",
        ["sk_clan_warrior2"] = "clan-an-craite-warrior",
        ["sk_clan_warrior3"] = "clan-an-craite-warrior",
        ["sk_drummond1"] = "clan-brokvar-archer",
        ["sk_drummond2"] = "clan-brokvar-archer",
        ["sk_kambi"] = "kambi",
        ["sk_hemdall"] = "hemdall",
        ["sk_coral"] = "coral",
        ["sk_holger"] = "holger-blackhand",
        ["sk_skald"] = "clan-heymaey-skald",
        ["sk_tordarroch"] = "clan-tordarroch-armorsmith",
        ["sk_draig"] = "draig-bon-dhu",
    };

    private static string? Key(string definitionId, string name) => name switch
    {
        // Berserkers transformed by Mardroeme keep their definition id but change face.
        "Vildkaarl" => "vildkaarl",
        "Young Vildkaarl" => "young-vildkaarl",
        _ => ByDefinitionId.GetValueOrDefault(definitionId)
    };

    /// <summary>The card face in the site's language: the Russian TW3 face in Russian, the English one otherwise.</summary>
    public static string? Url(string definitionId, string name, AppLanguage language) =>
        Key(definitionId, name) is not { } key ? null
        : language == AppLanguage.Ru && !EnglishOnly.Contains(key) && !FullArt.Contains(key) ? RussianFolder + key + ".webp"
        : Folder + key + ".webp";

    /// <summary>
    /// CSS classes of the name plate drawn on the picture, or null when the picture already prints the
    /// card's current name in the site's language: "" for a bare illustration (a caption at the
    /// bottom), "over-print" over a face printed in the other language, "over-print renamed" over the
    /// old name printed on a Russian face.
    /// </summary>
    public static string? NamePlate(string definitionId, string name, AppLanguage language) =>
        Key(definitionId, name) switch
        {
            null => null,
            string key when FullArt.Contains(key) => "",
            _ when language != AppLanguage.Ru => null,
            string key when EnglishOnly.Contains(key) => "over-print",
            string key when RenamedInRussian.Contains(key) => "over-print renamed",
            _ => null
        };

    /// <summary>True for a plain illustration: no printed frame, strength or name on the picture.</summary>
    public static bool IsPlainArt(string definitionId, string name) =>
        Key(definitionId, name) is { } key && FullArt.Contains(key);

    /// <summary>
    /// Whether the live strength is drawn over the picture. TW3 faces print the card's own strength;
    /// it is redrawn so weather, horns and bonds show on the card. A Hero's emblem is left alone
    /// unless its strength really changed.
    /// </summary>
    public static bool ShowsStrength(CardView card) =>
        card.Type == CardType.Unit
        && (!card.IsHero || card.CurrentStrength != card.BaseStrength || IsPlainArt(card.DefinitionId, card.Name));

    /// <summary>"buffed" / "debuffed" when the live strength differs from the card's own.</summary>
    public static string StrengthTrend(CardView card) =>
        card.CurrentStrength > card.BaseStrength ? "buffed"
        : card.CurrentStrength < card.BaseStrength ? "debuffed"
        : "";

    /// <summary>
    /// The colour a card glows with (a CSS class): its combat row for units — gold for Agile,
    /// which goes to either row — and its kind for special cards.
    /// </summary>
    public static string GlowClass(CardView card) => card.Type switch
    {
        CardType.Leader => "leader",
        CardType.Unit when card.Abilities.Contains(Ability.Agile) => "row-agile",
        CardType.Unit => card.Row switch
        {
            CombatRow.Ranged => "row-ranged",
            CombatRow.Siege => "row-siege",
            _ => "row-melee"
        },
        _ when IsWeather(card.Abilities) => "special-weather",
        _ when card.Abilities.Contains(Ability.Decoy) => "special-decoy",
        _ when card.Abilities.Contains(Ability.CommandersHorn) => "special-horn",
        _ when card.Abilities.Any(a => a is Ability.Scorch or Ability.ScorchBattlefield) => "special-scorch",
        _ => "special-artifact"
    };

    public static bool IsWeather(IEnumerable<Ability> abilities) =>
        abilities.Any(a => a is Ability.WeatherFrost or Ability.WeatherFog or Ability.WeatherRain
            or Ability.WeatherClear or Ability.WeatherStorm);
}
