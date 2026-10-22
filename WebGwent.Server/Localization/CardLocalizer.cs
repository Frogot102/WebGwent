using WebGwent.Game.Models;

namespace WebGwent.Server.Localization;

/// <summary>
/// Display-only Russian names. Game logic keeps English canonical names.
/// </summary>
public static class CardLocalizer
{
    private static readonly Dictionary<string, string> NamesRu = new(StringComparer.Ordinal)
    {
        ["Decoy"] = "Чучело",
        ["Commander's Horn"] = "Командирский рог",
        ["Scorch"] = "Испепеление",
        ["Biting Frost"] = "Лютый мороз",
        ["Impenetrable Fog"] = "Непроглядный туман",
        ["Torrential Rain"] = "Ливень",
        ["Clear Weather"] = "Ясная погода",
        ["Mardroeme"] = "Мардроеме",
        ["Geralt of Rivia"] = "Геральт из Ривии",
        ["Cirilla Fiona Elen Riannon"] = "Цирилла Фиона Элен Рианнон",
        ["Yennefer of Vengerberg"] = "Йеннифэр из Венгерберга",
        ["Triss Merigold"] = "Трисс Меригольд",
        ["Villentretenmerth"] = "Виллентретенмерт",
        ["Avallac'h"] = "Аваллак'х",
        ["Zoltan Chivay"] = "Золтан Хивай",
        ["Vesemir"] = "Весемир",
        ["Emiel Regis Rohellec Terzieff"] = "Эмиль Регис Рохеллек Терзиефф",
        ["Olgierd von Everec"] = "Ольгерд фон Эверек",
        ["Gaunter O'Dimm"] = "Гюнтер о'Димм",
        ["Gaunter O'Dimm: Darkness"] = "Гюнтер о'Димм: Тьма",
        ["Cow"] = "Корова",
        ["Bovine Defense Force"] = "Бычья сила",

        ["Foltest: Lord Commander of the North"] = "Фольтест: Верховный главнокомандующий Севера",
        ["Foltest: The Siegemaster"] = "Фольтест: Мастер осады",
        ["Foltest: King of Temeria"] = "Фольтест: Король Темерии",
        ["Foltest: Son of Medell"] = "Фольтест: Сын Меделла",
        ["Foltest: The Steel-Forged"] = "Фольтест: Стальной",
        ["Vernon Roche"] = "Вернон Роше",
        ["John Natalis"] = "Джон Наталис",
        ["Esterad Thyssen"] = "Эстерад Тиссен",
        ["Philippa Eilhart"] = "Филиппа Эйльхарт",
        ["Ves"] = "Вес",
        ["Siegfried of Denesle"] = "Зигфрид из Денесле",
        ["Yarpen Zigrin"] = "Ярпен Зигрин",
        ["Keira Metz"] = "Кейра Мец",
        ["Dandelion"] = "Лютик",
        ["Síle de Tansarville"] = "Шиля де Тансервилль",
        ["Prince Stennis"] = "Принц Стеннис",
        ["Crinfrid Reavers Dragon Hunter"] = "Охотник на драконов из Кринфрида",
        ["Blue Stripes Commando"] = "Командос Синих Полосок",
        ["Catapult"] = "Катапульта",
        ["Ballista"] = "Баллиста",
        ["Trebuchet"] = "Требюше",
        ["Siege Tower"] = "Осадная башня",
        ["Poor Fucking Infantry"] = "Чёртова пехота",
        ["Redanian Foot Soldier"] = "Реданский пехотинец",
        ["Kaedweni Siege Expert"] = "Каэдвенский осадный эксперт",
        ["Thaler"] = "Талер",
        ["Dun Banner Medic"] = "Медик Алого знамени",
        ["Sabrina Glevissig"] = "Сабрина Глевиссиг",
        ["Sheldon Skaggs"] = "Шелдон Скаггс",

        ["Emhyr var Emreis: His Imperial Majesty"] = "Эмгыр вар Эмрейс: Его Императорское Величество",
        ["Emhyr var Emreis: The White Flame"] = "Эмгыр вар Эмрейс: Белое Пламя",
        ["Emhyr var Emreis: The Relentless"] = "Эмгыр вар Эмрейс: Неумолимый",
        ["Emhyr var Emreis: Emperor of Nilfgaard"] = "Эмгыр вар Эмрейс: Император Нильфгаарда",
        ["Letho of Gulet"] = "Лето из Гулеты",
        ["Menno Coehoorn"] = "Менно Коегоорн",
        ["Cahir Mawr Dyffryn aep Ceallach"] = "Кагыр Мавр Диффрин аэп Кеаллах",
        ["Stefan Skellen"] = "Стефан Скеллен",
        ["Shilard Fitz-Oesterlen"] = "Шилярд Фиц-Оестерлен",
        ["Young Emissary"] = "Юный эмиссар",
        ["Vattier de Rideaux"] = "Ваттье де Ридео",
        ["Albrich"] = "Альбрих",
        ["Sweers"] = "Свирс",
        ["Assire var Anahid"] = "Ассире вар Анагид",
        ["Fringilla Vigo"] = "Фрингилла Виго",
        ["Cynthia"] = "Цинтия",
        ["Renuald aep Matsen"] = "Ренуальд аэп Матсен",
        ["Rotten Mangonel"] = "Гнилая мангонель",
        ["Nausicaa Cavalry Rider"] = "Всадник из кавалерии Навсикаи",
        ["Impera Brigade Guard"] = "Гвардия бригады Импера",
        ["Black Infantry Archer"] = "Лучник чёрной пехоты",
        ["Siege Engineer"] = "Осадный инженер",
        ["Heavy Zerrikanian Fire Scorpion"] = "Тяжёлый зерриканский огненный скорпион",
        ["Nilfgaardian Knight"] = "Нильфгаардский рыцарь",
        ["Puttkammer"] = "Путткаммер",
        ["Tibor Eggebracht"] = "Тибор Эггебрахт",
        ["Morvran Voorhis"] = "Морвран Воорхис",
        ["Etolian Auxiliary Archers"] = "Этолийские вспомогательные лучники",
        ["Rainfarn"] = "Райнфарн",
        ["Vanhemar"] = "Ванхемар",

        ["Francesca Findabair: Daisy of the Valley"] = "Францеска Финдабаир: Маргаритка долины",
        ["Francesca Findabair: Hope of the Aen Seidhe"] = "Францеска Финдабаир: Надежда Aen Seidhe",
        ["Francesca Findabair: Queen of Dol Blathanna"] = "Францеска Финдабаир: Королева Доль Блатанны",
        ["Francesca Findabair: Pureblood Elf"] = "Францеска Финдабаир: Чистокровная эльфийка",
        ["Francesca Findabair: The Beautiful"] = "Францеска Финдабаир: Прекраснейшая",
        ["Eithné"] = "Эйтнэ",
        ["Iorveth"] = "Йорвет",
        ["Isengrim Faoiltiarna"] = "Изенгрим Фаоильтиарна",
        ["Milva"] = "Мильва",
        ["Yaevinn"] = "Яевинн",
        ["Dennis Cranmer"] = "Деннис Кранмер",
        ["Ida Emean aep Sivney"] = "Ида Эмеан аэп Сивни",
        ["Filavandrel aén Fidháil"] = "Филавандрель аэн Фидаил",
        ["Toruviel"] = "Торувиэль",
        ["Riordain"] = "Риордайн",
        ["Havekar Healer"] = "Хавекарский целитель",
        ["Havekar Smuggler"] = "Хавекарский контрабандист",
        ["Dwarven Skirmisher"] = "Гномий застрельщик",
        ["Elven Skirmisher"] = "Эльфийский застрельщик",
        ["Vrihedd Brigade Veteran"] = "Ветеран бригады Врихедд",
        ["Vrihedd Brigade Recruit"] = "Рекрут бригады Врихедд",
        ["Ciaran aep Easnillien"] = "Киаран аэп Эаснилен",
        ["Barclay Els"] = "Баркли Элс",
        ["Dol Blathanna Archer"] = "Лучник Доль Блатанны",
        ["Dol Blathanna Scout"] = "Разведчик Доль Блатанны",
        ["Schirrú"] = "Ширру",
        ["Mahakaman Defender"] = "Махакамский защитник",

        ["Eredin: King of the Wild Hunt"] = "Эредин: Король Дикой Охоты",
        ["Eredin: Commander of the Red Riders"] = "Эредин: Командир Красных Всадников",
        ["Eredin: Destroyer of Worlds"] = "Эредин: Разрушитель миров",
        ["Eredin: Bringer of Death"] = "Эредин: Вестник Кары",
        ["Draug"] = "Драуг",
        ["Kayran"] = "Кайран",
        ["Imlerith"] = "Имлерих",
        ["Leshen"] = "Леший",
        ["Forktail"] = "Вилохвост",
        ["Earth Elemental"] = "Элементаль земли",
        ["Fiend"] = "Бес",
        ["Plague Maiden"] = "Дева чумы",
        ["Griffin"] = "Грифон",
        ["Werewolf"] = "Волколак",
        ["Frightener"] = "Пугач",
        ["Arachas"] = "Арахас",
        ["Nekker"] = "Наккер",
        ["Ghoul"] = "Утопец",
        ["Celaeno Harpy"] = "Гарпия Келено",
        ["Harpy"] = "Гарпия",
        ["Wyvern"] = "Виверна",
        ["Foglet"] = "Туманник",
        ["Botchling"] = "Поросль",
        ["Vampire: Bruxa"] = "Вампир: Брукса",
        ["Vampire: Ekimmara"] = "Вампир: Экиммара",
        ["Vampire: Fleder"] = "Вампир: Фледер",
        ["Vampire: Garkain"] = "Вампир: Гаркаин",
        ["Vampire: Katakan"] = "Вампир: Катакан",
        ["Crone: Brewess"] = "Ведьма: Пивоварка",
        ["Crone: Weavess"] = "Ведьма: Ткачиха",
        ["Crone: Whispess"] = "Ведьма: Шептуха",
        ["Arachas Behemoth"] = "Арахас-бегемот",

        ["Crach an Craite"] = "Крах ан Крайт",
        ["King Bran"] = "Король Бран",
        ["Hjalmar"] = "Хьяльмар",
        ["Cerys"] = "Керис",
        ["Ermion"] = "Эрмион",
        ["Birna Bran"] = "Бирна Бран",
        ["Udalryk"] = "Удальрик",
        ["Olaf"] = "Олаф",
        ["Donar an Hindar"] = "Донар ан Гиндар",
        ["Madman Lugos"] = "Безумный Лугос",
        ["Blueboy Lugos"] = "Синеглазый Лугос",
        ["Svanrige"] = "Сванриге",
        ["Light Longship"] = "Лёгкий драккар",
        ["War Longship"] = "Боевой драккар",
        ["Clan Drummond Shield Maiden"] = "Щитоносица клана Драммонд",
        ["Berserker"] = "Берсерк",
        ["Young Berserker"] = "Юный берсерк",
        ["Vildkaarl"] = "Вильдкаарл",
        ["Young Vildkaarl"] = "Юный вильдкаарл",
        ["Clan Dimun Pirate"] = "Пират клана Димун",
        ["Clan an Craite Warrior"] = "Воин клана ан Крайт",
        ["Clan Brokva Archer"] = "Лучник клана Брокви",
        ["Kambi"] = "Камби",
        ["Hemdall"] = "Хемдалль",
        ["Coral"] = "Коралл",
        ["Holger Blackhand"] = "Хольгер Чёрная Рука",
        ["Clan Heymaey Skald"] = "Скальд клана Хеймай",

        // Hearts of Stone / Blood and Wine and missing base cards
        ["Skellige Storm"] = "Шторм Скеллиге",
        ["Dethmold"] = "Детмольд",
        ["Sigismund Dijkstra"] = "Сигизмунд Дийкстра",
        ["Morteisen"] = "Мортейзен",
        ["Vreemde"] = "Вреемде",
        ["Siege Technician"] = "Осадный техник",
        ["Zerrikanian Fire Scorpion"] = "Зерриканский огненный скорпион",
        ["Emhyr var Emreis: Invader of the North"] = "Эмгыр вар Эмрейс: Захватчик Севера",
        ["Saesenthessis"] = "Саэсентессис",
        ["Endrega"] = "Эндриага",
        ["Cockatrice"] = "Кокатрикс",
        ["Gargoyle"] = "Гаргулья",
        ["Grave Hag"] = "Кладбищенская баба",
        ["Ice Giant"] = "Ледяной великан",
        ["Fire Elemental"] = "Огненный элементаль",
        ["Toad"] = "Жаба",
        ["Eredin Bréacc Glas: The Treacherous"] = "Эредин Бреакк Глас: Предатель",
        ["Clan Tordarroch Armorsmith"] = "Бронник клана Тордаррох",
        ["Draig Bon-Dhu"] = "Драйг Бон-Дху",
    };

    private static readonly Dictionary<Ability, (string En, string Ru)> Abilities = new()
    {
        [Ability.None] = ("", ""),
        [Ability.Hero] = ("Hero", "Герой"),
        [Ability.Spy] = ("Spy", "Шпион"),
        [Ability.Medic] = ("Medic", "Медик"),
        [Ability.TightBond] = ("Tight Bond", "Сплочённость"),
        [Ability.MoraleBoost] = ("Morale Boost", "Моральный подъём"),
        [Ability.Muster] = ("Muster", "Сбор"),
        [Ability.Agile] = ("Agile", "Ловкость"),
        [Ability.Decoy] = ("Decoy", "Чучело"),
        [Ability.Scorch] = ("Scorch", "Испепеление"),
        [Ability.CommandersHorn] = ("Commander's Horn", "Командирский рог"),
        [Ability.WeatherFrost] = ("Biting Frost", "Лютый мороз"),
        [Ability.WeatherFog] = ("Impenetrable Fog", "Непроглядный туман"),
        [Ability.WeatherRain] = ("Torrential Rain", "Ливень"),
        [Ability.WeatherClear] = ("Clear Weather", "Ясная погода"),
        [Ability.Berserker] = ("Berserker", "Берсерк"),
        [Ability.Mardroeme] = ("Mardroeme", "Мардроеме"),
        [Ability.SummonAvenger] = ("Avenger", "Мститель"),
        [Ability.WeatherStorm] = ("Skellige Storm", "Шторм Скеллиге"),
        [Ability.ScorchBattlefield] = ("Scorch", "Испепеление"),
    };

    private static readonly Dictionary<Ability, (string En, string Ru)> AbilityDescriptions = new()
    {
        [Ability.Hero] = (
            "Not affected by special cards or abilities.",
            "Не подвержен действию особых карт и способностей."),
        [Ability.Spy] = (
            "Place on your opponent's battlefield (counts for their score) and draw 2 cards.",
            "Кладётся на поле противника (идёт в его счёт); вы берёте 2 карты."),
        [Ability.Medic] = (
            "Choose one non-Hero card from your discard pile and play it instantly.",
            "Выберите не-героя из своего сброса и сразу сыграйте его."),
        [Ability.TightBond] = (
            "Place next to a card with the same name to double the strength of both.",
            "Рядом с картой того же имени удваивает силу обеих."),
        [Ability.MoraleBoost] = (
            "Adds +1 to all units in the row (excluding itself).",
            "+1 ко всем юнитам в ряду (кроме себя)."),
        [Ability.Muster] = (
            "Find any cards with the same name in your deck and play them instantly.",
            "Достаёт из колоды карты с тем же именем и сразу играет их."),
        [Ability.Agile] = (
            "Can be placed in either the Close Combat or Ranged Combat row.",
            "Можно положить в ближний или дальний ряд."),
        [Ability.Decoy] = (
            "Swap with a non-Hero unit on your side of the battlefield.",
            "Меняется местами с не-героем на вашей стороне поля."),
        [Ability.Scorch] = (
            "Destroy the strongest non-Hero unit(s) on the battlefield.",
            "Уничтожает сильнейшего не-героя (или нескольких с равной силой) на поле."),
        [Ability.CommandersHorn] = (
            "Double the strength of all non-Hero units in the selected row.",
            "Удваивает силу всех не-героев в выбранном ряду."),
        [Ability.WeatherFrost] = (
            "Sets the strength of all Close Combat units to 1.",
            "Сила всех юнитов ближнего ряда становится 1."),
        [Ability.WeatherFog] = (
            "Sets the strength of all Ranged Combat units to 1.",
            "Сила всех юнитов дальнего ряда становится 1."),
        [Ability.WeatherRain] = (
            "Sets the strength of all Siege units to 1.",
            "Сила всех осадных юнитов становится 1."),
        [Ability.WeatherClear] = (
            "Clears all weather effects.",
            "Снимает все эффекты погоды."),
        [Ability.Berserker] = (
            "Transforms into a stronger unit when a Mardroeme is played.",
            "Превращается в более сильного юнита, когда сыгран Мардроеме."),
        [Ability.Mardroeme] = (
            "Triggers Berserker transformations on the board.",
            "Запускает превращение берсерков на поле."),
        [Ability.SummonAvenger] = (
            "When this card is removed from the battlefield, summon a replacement.",
            "Когда карта покидает поле, призывает замену."),
        [Ability.WeatherStorm] = (
            "Sets the strength of all Ranged Combat and Siege units to 1.",
            "Сила всех юнитов дальнего и осадного рядов становится 1."),
        [Ability.ScorchBattlefield] = (
            "Destroys the strongest non-Hero unit(s) on the battlefield when played.",
            "При розыгрыше уничтожает сильнейшего не-героя (или нескольких) на поле."),
    };

    private static readonly Dictionary<string, string> DescriptionsRu = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Return a non-Hero unit to hand; Decoy takes its place."] =
            "Верните не-героя в руку; Чучело занимает его место.",
        ["Destroy the enemy's strongest Ranged unit(s) if their Ranged row totals 10 or more."] =
            "Уничтожает сильнейших юнитов дальнего боя врага, если сила его дальнего ряда ≥ 10.",
        ["Scorch: destroys the strongest non-Hero unit(s) on the battlefield."] =
            "Испепеление: уничтожает сильнейшего не-героя (или нескольких) на поле.",
        ["Passive: abilities that restore a unit to the battlefield restore a randomly-chosen unit. Affects both players."] =
            "Пассивно: способности, возвращающие отряд на поле, возвращают случайный отряд. Действует на обоих игроков.",
        ["Passive: doubles the strength of all spy cards. Affects both players."] =
            "Пассивно: удваивает силу всех шпионов. Действует на обоих игроков.",
        ["Double the strength of all non-Hero units in a row."] =
            "Удваивает силу всех не-героев в ряду.",
        ["Destroy the strongest non-Hero unit(s) on the board."] =
            "Уничтожает сильнейшего не-героя (или нескольких с равной силой) на поле.",
        ["Choose a row: Berserkers there transform into Vildkaarls."] =
            "Выберите ряд: берсерки в нём превращаются в вильдкаарлов.",
        ["Destroy the enemy's strongest Close Combat unit(s) if their Close Combat row totals 10 or more."] =
            "Уничтожает сильнейших юнитов ближнего боя врага, если сила его ближнего ряда ≥ 10.",
        ["Destroy the enemy's strongest Siege unit(s) if their Siege row totals 10 or more."] =
            "Уничтожает сильнейших осадных юнитов врага, если сила его осадного ряда ≥ 10.",
        ["Destroy the enemy's strongest Ranged unit(s) if their Ranged row totals 10 or more."] =
            "Уничтожает сильнейших юнитов дальнего боя врага, если сила его дальнего ряда ≥ 10.",
        ["When destroyed, summon Bovine Defense Force."] =
            "При уничтожении призывает Бычью силу.",
        ["When destroyed, summon Hemdall."] =
            "При уничтожении призывает Хемдалля.",
        ["Commander's Horn: doubles the other non-Hero units in his row."] =
            "Командирский рог: удваивает силу остальных не-героев в его ряду.",
        ["Mardroeme: Berserkers in his row transform into Vildkaarls."] =
            "Мардроеме: берсерки в его ряду превращаются в вильдкаарлов.",
        ["Transforms into Vildkaarl (14, Morale Boost) when Mardroeme affects its row."] =
            "Превращается в Вильдкаарла (14, Моральный подъём), когда на его ряд действует Мардроеме.",
        ["Transforms into Young Vildkaarl (8, Tight Bond) when Mardroeme affects its row."] =
            "Превращается в Юного вильдкаарла (8, Сплочённость), когда на его ряд действует Мардроеме.",

        // Leaders
        ["Doubles the strength of all your Siege units (unless a Commander's Horn is already in that row)."] =
            "Удваивает силу ваших осадных юнитов (если в ряду ещё нет Командирского рога).",
        ["Doubles the strength of all your Ranged units (unless a Commander's Horn is already in that row)."] =
            "Удваивает силу ваших юнитов дальнего боя (если в ряду ещё нет Командирского рога).",
        ["Doubles the strength of all your Close Combat units (unless a Commander's Horn is already in that row)."] =
            "Удваивает силу ваших юнитов ближнего боя (если в ряду ещё нет Командирского рога).",
        ["Pick an Impenetrable Fog card from your deck and play it instantly."] =
            "Достаёт из колоды Непроглядный туман и сразу играет его.",
        ["Pick a Torrential Rain card from your deck and play it instantly."] =
            "Достаёт из колоды Ливень и сразу играет его.",
        ["Pick a Biting Frost card from your deck and play it instantly."] =
            "Достаёт из колоды Лютый мороз и сразу играет его.",
        ["Pick any weather card from your deck and play it instantly."] =
            "Достаёт из колоды любую карту погоды и сразу играет её.",
        ["Clear any weather effects in play."] =
            "Снимает все эффекты погоды.",
        ["Passive: cancels your opponent's Leader Ability."] =
            "Пассивно: отменяет способность лидера соперника.",
        ["Draw a card from your opponent's discard pile."] =
            "Берёт в руку карту из сброса соперника.",
        ["Look at 3 random cards from your opponent's hand."] =
            "Показывает 3 случайные карты из руки соперника.",
        ["Passive: draw an extra card at the beginning of the battle."] =
            "Пассивно: в начале битвы вы берёте дополнительную карту.",
        ["Move Agile units to whichever valid row maximizes their strength."] =
            "Переставляет ловких юнитов в ряд, где их сила больше.",
        ["Discard 2 cards and draw 1 card of your choice from your deck."] =
            "Сбросьте 2 карты и возьмите из колоды 1 карту на выбор.",
        ["Restore a card from your discard pile to your hand."] =
            "Возвращает в руку карту из вашего сброса.",
        ["Shuffle all cards from each player's discard pile back into their decks."] =
            "Замешивает сбросы обоих игроков обратно в их колоды.",
        ["Passive: your units only lose half their strength in bad weather."] =
            "Пассивно: в плохую погоду ваши юниты теряют только половину силы.",
    };

    public static string CardName(string englishName, AppLanguage language)
    {
        if (language != AppLanguage.Ru)
            return englishName;
        // Strip runtime suffixes like " (Transformed)"
        var baseName = englishName;
        var suffix = "";
        var idx = englishName.IndexOf(" (", StringComparison.Ordinal);
        if (idx >= 0)
        {
            baseName = englishName[..idx];
            suffix = language == AppLanguage.Ru && englishName.EndsWith("(Transformed)", StringComparison.Ordinal)
                ? " (Преображён)"
                : englishName[idx..];
        }

        return NamesRu.TryGetValue(baseName, out var ru) ? ru + suffix : englishName;
    }

    public static string AbilityName(Ability ability, AppLanguage language)
    {
        if (!Abilities.TryGetValue(ability, out var pair))
            return ability.ToString();
        return language == AppLanguage.Ru ? pair.Ru : pair.En;
    }

    public static string FormatAbilities(IEnumerable<Ability> abilities, AppLanguage language) =>
        string.Join(", ", abilities
            .Where(a => a != Ability.None)
            .Select(a => AbilityName(a, language))
            .Where(s => s.Length > 0));

    public static string AbilityDescription(Ability ability, AppLanguage language)
    {
        if (!AbilityDescriptions.TryGetValue(ability, out var pair))
            return "";
        return language == AppLanguage.Ru ? pair.Ru : pair.En;
    }

    public static string CardDescription(string? catalogDescription, IEnumerable<Ability> abilities, AppLanguage language)
    {
        var list = abilities.ToList();
        if (!string.IsNullOrWhiteSpace(catalogDescription))
        {
            var desc = catalogDescription.Trim();
            if (language != AppLanguage.Ru)
                return desc;
            if (DescriptionsRu.TryGetValue(desc, out var ru))
                return ru;
        }

        foreach (var a in list.Where(x => x is not Ability.None and not Ability.Hero))
        {
            var d = AbilityDescription(a, language);
            if (d.Length > 0) return d;
        }

        if (list.Contains(Ability.Hero))
            return AbilityDescription(Ability.Hero, language);

        return catalogDescription?.Trim() ?? "";
    }

    /// <summary>
    /// Catalog/flavor line for the inspect panel — omitted when it repeats an ability blurb.
    /// </summary>
    public static string? ExtraInspectText(string? catalogDescription, IEnumerable<Ability> abilities, AppLanguage language)
    {
        var abilityTexts = abilities
            .Where(a => a != Ability.None)
            .Select(a => AbilityDescription(a, language))
            .Where(s => s.Length > 0)
            .ToList();

        string? catalogText = null;
        if (!string.IsNullOrWhiteSpace(catalogDescription))
        {
            var raw = catalogDescription.Trim();
            catalogText = language == AppLanguage.Ru && DescriptionsRu.TryGetValue(raw, out var ru)
                ? ru
                : raw;
        }

        if (abilityTexts.Count == 0)
            return string.IsNullOrEmpty(catalogText) ? null : catalogText;

        if (string.IsNullOrEmpty(catalogText))
            return null;

        if (abilityTexts.Any(a => IsNearDuplicate(catalogText, a)))
            return null;

        return catalogText;
    }

    private static bool IsNearDuplicate(string a, string b)
    {
        var na = Compact(a);
        var nb = Compact(b);
        if (na.Length == 0 || nb.Length == 0) return false;
        if (na.Equals(nb, StringComparison.Ordinal)) return true;
        if (na.Contains(nb, StringComparison.Ordinal) || nb.Contains(na, StringComparison.Ordinal))
            return true;

        var wa = SignificantWords(a);
        var wb = SignificantWords(b);
        if (wa.Count == 0 || wb.Count == 0) return false;
        var shared = wa.Count(w => wb.Contains(w));
        var min = Math.Min(wa.Count, wb.Count);
        return shared >= Math.Max(3, (int)Math.Ceiling(min * 0.75));
    }

    private static string Compact(string s) =>
        new string(s.ToLowerInvariant().Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray());

    private static HashSet<string> SignificantWords(string s)
    {
        string[] stop = ["в", "и", "на", "из", "для", "при", "the", "a", "an", "of", "to", "in", "on", "all", "всех", "или"];
        return s.ToLowerInvariant()
            .Split([' ', ',', '.', ';', ':', '—', '-', '·', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray()))
            .Where(w => w.Length >= 3 && !stop.Contains(w, StringComparer.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
    }
}
