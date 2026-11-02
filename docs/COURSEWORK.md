# WebGwent — материалы к курсовой

## 1. Цель

Реализовать веб-приложение для игры в Гвинт (правила TW3) на языке C#, с игрой против бота и онлайн PvP (WebGwent Plus).

## 2. Архитектура

- **WebGwent.Game** — доменная логика без UI. `RulesEngine` применяет ходы к `MatchState`. Подсчёт силы — `ScoreCalculator`. Карты — embedded JSON (`Data/cards.json`) через `CardCatalog`. Кастомные фракции/карты и пассивы — overlay в `CustomContentStore`.
- **WebGwent.Api** — REST + SignalR (`GameHub`), JWT-аутентификация, аккаунты/история/инвайты в `App_Data/*.json`. Источник истины для онлайн и бот-матчей — `GameSessionManager` **на Api**.
- **WebGwent.Server** — Blazor Server UI. Ходит в Api по HTTP (`WebGwentApiClient`) и проксирует `/api/**` на Api, чтобы демо по одному URL (ngrok / Radmin). SignalR к хабу Api — с серверного circuit с JWT.
- **WebGwent.Tests** — механики TW3, бот, бенчмарк; интеграционные тесты Api.

```
Browser → Blazor Server (:5097)
            ├─ REST /api/* (proxy) → WebGwent.Api (:5180)
            └─ HubConnection → /hubs/game → GameHub → GameSessionManager → RulesEngine
```

Живые матчи/комнаты хранятся **в памяти** Api (после рестарта — reconnect по коду комнаты недоступен, пока матч не создан заново). Аккаунты, история, колоды, инвайты и custom-content — на диске в `App_Data`.

Режим `GameMode.Hotseat` остаётся для юнит-тестов; отдельной страницы Hotseat в UI нет (два игрока — через Online).

## 3. Игровой цикл

1. Создание матча из двух валидных колод, раздача по 10 карт.
2. Муллиган (до 2 замен). Scoia'tael (или кастом с пассивом Scoia'tael) выбирает, кто ходит первым.
3. Ходы: карта / пас / лидер; pending при Agile, Medic, Decoy, Роге, Mardroeme и т.д.
4. Оба паса → подсчёт → проигравший теряет кристалл (при ничьей — оба; Nilfgaard выигрывает ничьи); фракционные эффекты между раундами.
5. Скеллиге в начале 3-го раунда возвращает **двух сильнейших не-героев** из сброса.
6. У кого кончились кристаллы — проиграл; ничья возможна. Максимум 3 раунда.

## 4. Бот

`BotAI` + Monte Carlo look-ahead (`RolloutPolicy`). Сложности Easy / Medium / High / DeathMarch. High и DeathMarch требуют Plus.

## 5. Демонстрация на защите

См. [DEMO.md](DEMO.md). Кратко:

1. Vs Bot (Easy) — Tight Bond / Muster / погода.
2. Admin → custom faction + passive → колода → матч.
3. Plus invite → Online по коду комнаты, таймер хода.
4. История с очками раундов + лидерборд.
5. `dotnet test`.

## 6. Запуск

Нужны **два** процесса (или Docker Compose):

```bash
dotnet run --project WebGwent.Api
dotnet run --project WebGwent.Server
# UI http://localhost:5097  API http://localhost:5180

docker compose up --build
```

Секреты: `Jwt__Key` / `JWT_KEY`, `SeedAdmin__Password` — см. `.env.example`.

Тесты: `dotnet test`.
