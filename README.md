# WebGwent

Курсовая работа: веб-порт **Гвинта** из *The Witcher 3: Wild Hunt* на **C# / Blazor Server**.

## Возможности

- Полный цикл матча по правилам TW3: муллиган, у каждого 2 кристалла, проигрыш или ничья в раунде стоит кристалл, максимум 3 раунда, возможна ничья в матче
- Механики: погода, Decoy, Scorch, Commander's Horn (с выбором ряда), Spy, Medic, Tight Bond, Morale Boost, Muster, Agile, Berserker и Mardroeme, все 20 лидеров TW3
- Фракционные пассивы: Northern Realms, Nilfgaard, Scoia'tael, Monsters, Skellige
- Режимы: **против бота**, **онлайн PvP** (SignalR, код комнаты)
- Бот планирует раунды, оценивает каждый ход на копии матча и проверяет лучшие ходы доигрыванием партии до конца (метод Монте-Карло)
- Пресет-колоды всех 5 фракций + валидация

## Запуск

```bash
dotnet restore
dotnet run --project WebGwent.Server
```

Откройте URL из консоли (обычно `https://localhost:7xxx`).

Тесты:

```bash
dotnet test
```

## Структура

| Проект | Назначение |
|--------|------------|
| `WebGwent.Game` | Движок правил, каталог карт, бот, сессии |
| `WebGwent.Server` | Blazor UI + SignalR Hub |
| `WebGwent.Tests` | Юнит-тесты механик, поведения бота и бенчмарк бота |

Подробности для пояснительной записки: [docs/COURSEWORK.md](docs/COURSEWORK.md).
