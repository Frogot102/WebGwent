# WebGwent

Курсовая работа: веб-порт **Гвинта** из *The Witcher 3: Wild Hunt* на **C# / Blazor Server**.

## Возможности

- Полный цикл матча по правилам TW3: муллиган, у каждого 2 кристалла, проигрыш или ничья в раунде стоит кристалл, максимум 3 раунда, возможна ничья в матче
- Все карты TW3 вместе с дополнениями «Каменные сердца» и «Кровь и вино», включая Шторм Скеллиге и 22 лидера
- Механики: погода, Decoy, Scorch, Commander's Horn (с выбором ряда), Spy, Medic, Tight Bond, Morale Boost, Muster, Agile, Berserker и Mardroeme
- Карты в руке и на рядах отсортированы по силе; бот отвечает с паузой после хода игрока
- Фракционные пассивы: Northern Realms, Nilfgaard, Scoia'tael, Monsters, Skellige
- Сложности бота: **Лёгкая**, **Средняя**, **Высокая** (полный ИИ), **На смерть** (усиленный look-ahead)
- **WebGwent Plus**: без рекламы, сложности Высокая / На смерть, онлайн PvP; демо-покупка и инвайт-коды в профиле
- Режимы: **против бота** (всем на Лёгкой/Средней), **онлайн PvP** (SignalR, только Plus)
- Справочник `/rules` с поиском, сортировкой и сворачиваемыми главами
- Пресет-колоды всех 5 фракций + валидация

Вшитый админ (роль Admin, Plus без подписки): логин **`admin`**, пароль **`admin123`**.  
Создаётся/обновляется при каждом запуске (`BuiltInAdmin` + `SeedAdmin` в `appsettings.json`). На странице входа есть подсказка.

## Запуск

Нужны **два** процесса — API и UI:

```bash
dotnet restore
dotnet run --project WebGwent.Api
dotnet run --project WebGwent.Server
```

UI: http://localhost:5097 · API: http://localhost:5180  
Как устроены endpoint’ы: [docs/API.md](docs/API.md).

Тесты:

```bash
dotnet test
```

## Структура

| Проект | Назначение |
|--------|------------|
| `WebGwent.Game` | Движок правил, каталог карт, бот, сессии |
| `WebGwent.Api` | REST + SignalR (JWT), аккаунты, матчи, колоды |
| `WebGwent.Server` | Blazor UI — клиент API |
| `WebGwent.Tests` | Юнит-тесты механик, поведения бота и бенчмарк бота |

Подробности для пояснительной записки: [docs/COURSEWORK.md](docs/COURSEWORK.md).
