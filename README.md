# WebGwent

Курсовая работа: веб-порт **Гвинта** из *The Witcher 3: Wild Hunt* на **C# / Blazor Server**.

## Возможности

- Полный цикл матча: муллиган, 3 раунда, пас, победа в 2
- Механики: погода, Decoy, Scorch, Commander's Horn, Spy, Medic, Tight Bond, Morale Boost, Muster, Agile, Berserker, лидеры
- Фракционные пассивы: Northern Realms, Nilfgaard, Scoia'tael, Monsters, Skellige
- Режимы: **против бота**, **онлайн PvP** (SignalR, код комнаты)
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
| `WebGwent.Tests` | Юнит-тесты механик |

Подробности для пояснительной записки: [docs/COURSEWORK.md](docs/COURSEWORK.md).
