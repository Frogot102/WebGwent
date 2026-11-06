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
        ["Scorch"] = "Казнь",
        ["Biting Frost"] = "Мороз",
        ["Impenetrable Fog"] = "Мгла",
        ["Torrential Rain"] = "Ливень",
        ["Clear Weather"] = "Ясное небо",
        ["Mardroeme"] = "Мардрём",
        ["Geralt of Rivia"] = "Геральт из Ривии",
        ["Cirilla Fiona Elen Riannon"] = "Цирилла",
        ["Yennefer of Vengerberg"] = "Йеннифэр из Венгерберга",
        ["Triss Merigold"] = "Трисс Меригольд",
        ["Villentretenmerth"] = "Виллентретенмерт",
        ["Avallac'h"] = "Таинственный эльф",
        ["Zoltan Chivay"] = "Золтан Хивай",
        ["Vesemir"] = "Весемир",
        ["Emiel Regis Rohellec Terzieff"] = "Эмиель Регис",
        ["Olgierd von Everec"] = "Ольгерд фон Эверек",
        ["Gaunter O'Dimm"] = "Гюнтер о’Дим",
        ["Gaunter O'Dimm: Darkness"] = "Гюнтер о’Дим: Тьма",
        ["Cow"] = "Корова",
        ["Bovine Defense Force"] = "Рогатое сопротивление",

        ["Foltest: Lord Commander of the North"] = "Фольтест: Предводитель Севера",
        ["Foltest: The Siegemaster"] = "Фольтест: Завоеватель",
        ["Foltest: King of Temeria"] = "Фольтест: Король Темерии",
        ["Foltest: Son of Medell"] = "Фольтест: Сын Меделла",
        ["Foltest: The Steel-Forged"] = "Фольтест: Железный Владыка",
        ["Vernon Roche"] = "Вернон Роше",
        ["John Natalis"] = "Ян Наталис",
        ["Esterad Thyssen"] = "Эстерад Тиссен",
        ["Philippa Eilhart"] = "Филиппа Эйльхарт",
        ["Ves"] = "Бьянка",
        ["Siegfried of Denesle"] = "Зигфрид из Денесле",
        ["Yarpen Zigrin"] = "Ярпен Зигрин",
        ["Keira Metz"] = "Кейра Мец",
        ["Dandelion"] = "Лютик",
        ["Síle de Tansarville"] = "Шеала де Тансервилль",
        ["Prince Stennis"] = "Принц Стеннис",
        ["Crinfrid Reavers Dragon Hunter"] = "Рубайла из Кринфрида",
        ["Blue Stripes Commando"] = "Боец Синих Полосок",
        ["Catapult"] = "Катапульта",
        ["Ballista"] = "Баллиста",
        ["Trebuchet"] = "Требушет",
        ["Siege Tower"] = "Осадная башня",
        ["Poor Fucking Infantry"] = "Грёбаная пехтура",
        ["Redanian Foot Soldier"] = "Реданский пехотинец",
        ["Kaedweni Siege Expert"] = "Каэдвенский осадный мастер",
        ["Thaler"] = "Талер",
        ["Dun Banner Medic"] = "Лекарь Бурой Хоругви",
        ["Sabrina Glevissig"] = "Сабрина Глевиссиг",
        ["Sheldon Skaggs"] = "Шелдон Скаггс",

        ["Emhyr var Emreis: His Imperial Majesty"] = "Эмгыр вар Эмрейс: Йож из Эрленвальда",
        ["Emhyr var Emreis: The White Flame"] = "Эмгыр вар Эмрейс: Белое Пламя",
        ["Emhyr var Emreis: The Relentless"] = "Эмгыр вар Эмрейс: Властелин Юга",
        ["Emhyr var Emreis: Emperor of Nilfgaard"] = "Эмгыр вар Эмрейс: Император Нильфгаарда",
        ["Letho of Gulet"] = "Лето из Гулеты",
        ["Menno Coehoorn"] = "Мэнно Коегоорн",
        ["Cahir Mawr Dyffryn aep Ceallach"] = "Кагыр Маур аэп Кеаллах",
        ["Stefan Skellen"] = "Стефан Скеллен",
        ["Shilard Fitz-Oesterlen"] = "Шилярд Фиц-Эстерлен",
        ["Young Emissary"] = "Молодой посланник",
        ["Vattier de Rideaux"] = "Ваттье де Ридо",
        ["Albrich"] = "Альбрых",
        ["Sweers"] = "Свирс",
        ["Assire var Anahid"] = "Ассирэ вар Анагыд",
        ["Fringilla Vigo"] = "Фрингилья Виго",
        ["Cynthia"] = "Цинтия",
        ["Renuald aep Matsen"] = "Ренуальд аэп Матсен",
        ["Rotten Mangonel"] = "Сгнившая петрария",
        ["Nausicaa Cavalry Rider"] = "Кавалерия «Наузикаа»",
        ["Impera Brigade Guard"] = "Бригада «Импера»",
        ["Black Infantry Archer"] = "Лучник чёрной пехоты",
        ["Siege Engineer"] = "Сапёр",
        ["Heavy Zerrikanian Fire Scorpion"] = "Большой огненный скорпион",
        ["Nilfgaardian Knight"] = "Нильфгаардский рыцарь",
        ["Puttkammer"] = "Путткаммер",
        ["Tibor Eggebracht"] = "Тибор Эггебрахт",
        ["Morvran Voorhis"] = "Морвран Воорхис",
        ["Etolian Auxiliary Archers"] = "Лучная поддержка",
        ["Rainfarn"] = "Раинфарн",
        ["Vanhemar"] = "Вангемар",

        ["Francesca Findabair: Daisy of the Valley"] = "Францеска Финдабаир: Маргаритка из Долин",
        ["Francesca Findabair: Hope of the Aen Seidhe"] = "Францеска Финдабаир: Надежда Aen Seidhe",
        ["Francesca Findabair: Queen of Dol Blathanna"] = "Францеска Финдабаир: Королева Дол Блатанны",
        ["Francesca Findabair: Pureblood Elf"] = "Францеска Финдабаир: Истинная эльфка",
        ["Francesca Findabair: The Beautiful"] = "Францеска Финдабаир: Прекраснейшая",
        ["Eithné"] = "Эитнэ",
        ["Iorveth"] = "Иорвет",
        ["Isengrim Faoiltiarna"] = "Изенгрим Фаоильтиарна",
        ["Milva"] = "Мильва",
        ["Yaevinn"] = "Яевинн",
        ["Dennis Cranmer"] = "Деннис Кранмер",
        ["Ida Emean aep Sivney"] = "Ида Эмеан",
        ["Filavandrel aén Fidháil"] = "Филавандрель",
        ["Toruviel"] = "Торувьель",
        ["Riordain"] = "Риордаин",
        ["Havekar Healer"] = "Лекарь-гавенкар",
        ["Havekar Smuggler"] = "Поддержка гавенкаров",
        ["Dwarven Skirmisher"] = "Краснолюд-застрельщик",
        ["Elven Skirmisher"] = "Эльфский застрельщик",
        ["Vrihedd Brigade Veteran"] = "Бригада «Врихедд»",
        ["Vrihedd Brigade Recruit"] = "Кадет «Врихедд»",
        ["Ciaran aep Easnillien"] = "Киаран аэп Эасниллен",
        ["Barclay Els"] = "Барклай Эльс",
        ["Dol Blathanna Archer"] = "Лучник из Дол Блатанны",
        ["Dol Blathanna Scout"] = "Следопыт из Дол Блатанны",
        ["Schirrú"] = "Ширру",
        ["Mahakaman Defender"] = "Махакамский доброволец",

        ["Eredin: King of the Wild Hunt"] = "Эредин Бреакк Глас: Король Aen Elle",
        ["Eredin: Commander of the Red Riders"] = "Эредин Бреакк Глас: Командир Дикой Охоты",
        ["Eredin: Destroyer of Worlds"] = "Эредин Бреакк Глас: Владыка Тир на Лиа",
        ["Eredin: Bringer of Death"] = "Эредин Бреакк Глас: Убийца Оберона",
        ["Draug"] = "Драуг",
        ["Kayran"] = "Кейран",
        ["Imlerith"] = "Имлерих",
        ["Leshen"] = "Леший",
        ["Forktail"] = "Вилохвост",
        ["Earth Elemental"] = "Элементаль земли",
        ["Fiend"] = "Бес",
        ["Plague Maiden"] = "Моровая дева",
        ["Griffin"] = "Грифон",
        ["Werewolf"] = "Волколак",
        ["Frightener"] = "Пугач",
        ["Arachas"] = "Главоглаз",
        ["Nekker"] = "Накер",
        ["Ghoul"] = "Гуль",
        ["Celaeno Harpy"] = "Гарпия келено",
        ["Harpy"] = "Гарпия",
        ["Wyvern"] = "Виверна",
        ["Foglet"] = "Туманник",
        ["Botchling"] = "Игоша",
        ["Vampire: Bruxa"] = "Вампир: брукса",
        ["Vampire: Ekimmara"] = "Вампир: экимма",
        ["Vampire: Fleder"] = "Вампир: фледер",
        ["Vampire: Garkain"] = "Вампир: гаркаин",
        ["Vampire: Katakan"] = "Вампир: катакан",
        ["Crone: Brewess"] = "Ведьма: Кухарка",
        ["Crone: Weavess"] = "Ведьма: Пряха",
        ["Crone: Whispess"] = "Ведьма: Шептуха",
        ["Arachas Behemoth"] = "Огромный главоглаз",

        ["Crach an Craite"] = "Крах ан Крайт",
        ["King Bran"] = "Король Бран",
        ["Hjalmar"] = "Хьялмар",
        ["Cerys"] = "Керис",
        ["Ermion"] = "Мышовур",
        ["Birna Bran"] = "Бирна Бран",
        ["Udalryk"] = "Удальрик",
        ["Olaf"] = "Олаф",
        ["Donar an Hindar"] = "Донар ан Хиндар",
        ["Madman Lugos"] = "Лугос Безумный",
        ["Blueboy Lugos"] = "Лугос Синий",
        ["Svanrige"] = "Сванриге",
        ["Light Longship"] = "Лёгкий драккар",
        ["War Longship"] = "Военный драккар",
        ["Clan Drummond Shield Maiden"] = "Воительница клана Друммонд",
        ["Berserker"] = "Берсерк",
        ["Young Berserker"] = "Молодой берсерк",
        ["Vildkaarl"] = "Обратившийся вильдкаарл",
        ["Young Vildkaarl"] = "Обратившийся молодой вильдкаарл",
        ["Clan Dimun Pirate"] = "Пират клана Димун",
        ["Clan an Craite Warrior"] = "Воин клана ан Крайт",
        ["Clan Brokva Archer"] = "Лучник клана Броквар",
        ["Kambi"] = "Камби",
        ["Hemdall"] = "Хеймдалль",
        ["Coral"] = "Коралл",
        ["Holger Blackhand"] = "Хольгер Чернорукий",
        ["Clan Heymaey Skald"] = "Скальд клана Хэймэй",

        // Hearts of Stone / Blood and Wine and missing base cards
        ["Skellige Storm"] = "Шторм со Скеллиге",
        ["Dethmold"] = "Детмольд",
        ["Sigismund Dijkstra"] = "Сигизмунд Дийкстра",
        ["Morteisen"] = "Мортейсен",
        ["Vreemde"] = "Вреемде",
        ["Siege Technician"] = "Осадная поддержка",
        ["Zerrikanian Fire Scorpion"] = "Огненный скорпион",
        ["Emhyr var Emreis: Invader of the North"] = "Эмгыр вар Эмрейс: Завоеватель Севера",
        ["Saesenthessis"] = "Саэсентессис",
        ["Endrega"] = "Эндриага",
        ["Cockatrice"] = "Куролиск",
        ["Gargoyle"] = "Гаргулья",
        ["Grave Hag"] = "Кладбищенская баба",
        ["Ice Giant"] = "Ледяной великан",
        ["Fire Elemental"] = "Элементаль огня",
        ["Toad"] = "Принц-жаба",
        ["Eredin Bréacc Glas: The Treacherous"] = "Эредин Бреакк Глас: Вероломный",
        ["Clan Tordarroch Armorsmith"] = "Кузнец клана Тордаррох",
        ["Draig Bon-Dhu"] = "Драйг Бон-Ду",
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
        [Ability.Scorch] = ("Scorch", "Казнь"),
        [Ability.CommandersHorn] = ("Commander's Horn", "Командирский рог"),
        [Ability.WeatherFrost] = ("Biting Frost", "Мороз"),
        [Ability.WeatherFog] = ("Impenetrable Fog", "Мгла"),
        [Ability.WeatherRain] = ("Torrential Rain", "Ливень"),
        [Ability.WeatherClear] = ("Clear Weather", "Ясное небо"),
        [Ability.Berserker] = ("Berserker", "Берсерк"),
        [Ability.Mardroeme] = ("Mardroeme", "Мардрём"),
        [Ability.SummonAvenger] = ("Avenger", "Мститель"),
        [Ability.WeatherStorm] = ("Skellige Storm", "Шторм со Скеллиге"),
        [Ability.ScorchBattlefield] = ("Scorch", "Казнь"),
    };

    private static readonly Dictionary<Ability, (string En, string Ru)> AbilityDescriptions = new()
    {
        [Ability.Hero] = (
            "Not affected by weather, Commander's Horn, Morale Boost or Scorch. Cannot be taken back by a Decoy or revived by a Medic.",
            "На героя не действуют погода, Командирский рог, Моральный подъём и Казнь. Его нельзя забрать Чучелом или воскресить Медиком."),
        [Ability.Spy] = (
            "Goes to your opponent's side of the battlefield, where its strength counts for them, and you draw 2 cards from your deck.",
            "Выходит на сторону противника — его сила идёт в счёт соперника, — а вы берёте 2 карты из своей колоды."),
        [Ability.Medic] = (
            "When played, choose a unit from your discard pile and play it at once. Heroes and special cards cannot be revived.",
            "При розыгрыше выберите отряд из своего сброса и сразу сыграйте его. Героев и особые карты воскресить нельзя."),
        [Ability.TightBond] = (
            "Next to cards with the same name, the strength of each is multiplied by the number of such cards in the row.",
            "Рядом с картами того же имени сила каждой умножается на число таких карт в ряду."),
        [Ability.MoraleBoost] = (
            "Adds +1 to every other unit in its row. Heroes are not affected.",
            "Даёт +1 всем остальным отрядам в своём ряду. На героев не действует."),
        [Ability.Muster] = (
            "When played, every card of the same group in your deck comes out onto the battlefield too.",
            "При розыгрыше все карты той же группы из вашей колоды тоже выходят на поле."),
        [Ability.Agile] = (
            "Can be played in either the Close Combat or the Ranged Combat row.",
            "Можно сыграть в ближний или дальний ряд — на выбор."),
        [Ability.Decoy] = (
            "Swap with a non-Hero unit on your side: the unit returns to your hand and the Decoy takes its place.",
            "Меняется местами с не-героем на вашей стороне: отряд возвращается в руку, а Чучело встаёт на его место."),
        [Ability.Scorch] = (
            "Destroys the strongest non-Hero unit on the battlefield, on both sides — every one of them if several share that strength.",
            "Уничтожает сильнейший отряд-не-героя на поле, на обеих сторонах, — всех, если у нескольких одинаковая сила."),
        [Ability.CommandersHorn] = (
            "Doubles the strength of all non-Hero units in the chosen row. Several horns in one row do not stack.",
            "Удваивает силу всех не-героев в выбранном ряду. Несколько рогов в одном ряду не складываются."),
        [Ability.WeatherFrost] = (
            "Sets the strength of every Close Combat unit on both sides to 1.",
            "Сила всех отрядов ближнего боя у обоих игроков становится 1."),
        [Ability.WeatherFog] = (
            "Sets the strength of every Ranged Combat unit on both sides to 1.",
            "Сила всех отрядов дальнего боя у обоих игроков становится 1."),
        [Ability.WeatherRain] = (
            "Sets the strength of every Siege unit on both sides to 1.",
            "Сила всех осадных отрядов у обоих игроков становится 1."),
        [Ability.WeatherClear] = (
            "Removes every weather effect from the battlefield.",
            "Убирает с поля все погодные эффекты."),
        [Ability.Berserker] = (
            "Transforms into a stronger unit when a Mardroeme affects its row.",
            "Превращается в более сильного бойца, когда на его ряд действует Мардрём."),
        [Ability.Mardroeme] = (
            "Choose a row: every Berserker in it transforms.",
            "Выберите ряд: все берсерки в нём превращаются."),
        [Ability.SummonAvenger] = (
            "When this card leaves the battlefield, a stronger replacement takes its place.",
            "Когда карта покидает поле, на её место приходит более сильная замена."),
        [Ability.WeatherStorm] = (
            "Sets the strength of every Ranged Combat and Siege unit on both sides to 1.",
            "Сила всех отрядов дальнего боя и осадных отрядов у обоих игроков становится 1."),
        [Ability.ScorchBattlefield] = (
            "When played, destroys the strongest non-Hero unit(s) on the battlefield.",
            "При розыгрыше уничтожает сильнейший отряд-не-героя на поле (или нескольких с равной силой)."),
    };

    private static readonly Dictionary<string, string> DescriptionsRu = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Return a non-Hero unit to hand; Decoy takes its place."] =
            "Верните не-героя в руку; Чучело занимает его место.",
        ["Destroy the enemy's strongest Ranged unit(s) if their Ranged row totals 10 or more."] =
            "Уничтожает сильнейших юнитов дальнего боя врага, если сила его дальнего ряда ≥ 10.",
        ["Scorch: destroys the strongest non-Hero unit(s) on the battlefield."] =
            "Казнь: уничтожает сильнейшего не-героя (или нескольких) на поле.",
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
            "Мардрём: берсерки в его ряду превращаются в вильдкаарлов.",
        ["Transforms into Vildkaarl (14, Morale Boost) when Mardroeme affects its row."] =
            "Превращается в обратившегося вильдкаарла (14, Моральный подъём), когда на его ряд действует Мардрём.",
        ["Transforms into Young Vildkaarl (8, Tight Bond) when Mardroeme affects its row."] =
            "Превращается в обратившегося молодого вильдкаарла (8, Сплочённость), когда на его ряд действует Мардрём.",

        // Leaders
        ["Doubles the strength of all your Siege units (unless a Commander's Horn is already in that row)."] =
            "Удваивает силу ваших осадных юнитов (если в ряду ещё нет Командирского рога).",
        ["Doubles the strength of all your Ranged units (unless a Commander's Horn is already in that row)."] =
            "Удваивает силу ваших юнитов дальнего боя (если в ряду ещё нет Командирского рога).",
        ["Doubles the strength of all your Close Combat units (unless a Commander's Horn is already in that row)."] =
            "Удваивает силу ваших юнитов ближнего боя (если в ряду ещё нет Командирского рога).",
        ["Pick an Impenetrable Fog card from your deck and play it instantly."] =
            "Достаёт из колоды Мглу и сразу играет его.",
        ["Pick a Torrential Rain card from your deck and play it instantly."] =
            "Достаёт из колоды Ливень и сразу играет его.",
        ["Pick a Biting Frost card from your deck and play it instantly."] =
            "Достаёт из колоды Мороз и сразу играет его.",
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

    /// <summary>Abilities whose general text a card's own catalog line replaces (row Scorch, horn units, avengers…).</summary>
    private static readonly HashSet<Ability> CardSpecificAbilities =
    [
        Ability.Scorch, Ability.ScorchBattlefield, Ability.CommandersHorn, Ability.SummonAvenger,
        Ability.Decoy, Ability.Mardroeme, Ability.Berserker
    ];

    /// <summary>
    /// Everything the inspect panel says about a card: one line per ability, titled with its name.
    /// The card's own catalog line describes how this particular card uses its ability
    /// (Villentretenmerth only scorches the enemy's Close Combat row), so it replaces that ability's
    /// general text; any other catalog line is added untitled unless it repeats an ability.
    /// </summary>
    public static IReadOnlyList<(string Title, string Text)> InspectLines(
        string? catalogDescription, IEnumerable<Ability> abilities, AppLanguage language)
    {
        var catalog = string.IsNullOrWhiteSpace(catalogDescription) ? null
            : language == AppLanguage.Ru && DescriptionsRu.TryGetValue(catalogDescription.Trim(), out var ru) ? ru
            : catalogDescription.Trim();

        var lines = new List<(string Title, string Text)>();
        var catalogUsed = false;
        foreach (var ability in abilities.Where(a => a != Ability.None).Distinct())
        {
            var title = AbilityName(ability, language);
            var text = AbilityDescription(ability, language);
            if (catalog is not null && !catalogUsed && CardSpecificAbilities.Contains(ability))
            {
                text = WithoutTitle(catalog, title);
                catalogUsed = true;
            }

            if (text.Length > 0)
                lines.Add((title, text));
        }

        if (catalog is not null && !catalogUsed && !lines.Any(l => IsNearDuplicate(catalog, l.Text)))
            lines.Add(("", catalog));
        return lines;
    }

    /// <summary>"Commander's Horn: doubles…" → "doubles…" (capitalised) when shown under that title.</summary>
    private static string WithoutTitle(string text, string title)
    {
        var prefix = title + ":";
        if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return text;
        var rest = text[prefix.Length..].TrimStart();
        return rest.Length == 0 ? text : char.ToUpper(rest[0]) + rest[1..];
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
