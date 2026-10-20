# WebGwent — материалы к курсовой

## 1. Цель

Реализовать веб-приложение для игры в Гвинт (правила TW3) на языке C#, с возможностью игры против бота, вдвоём на одном устройстве и онлайн.

## 2. Архитектура

- **WebGwent.Game** — доменная логика без UI. `RulesEngine` применяет ходы к `MatchState`. Подсчёт силы — `ScoreCalculator`. Карты загружаются из embedded JSON (`Data/cards.json`) через `CardCatalog`.
- **WebGwent.Server** — Blazor Server (интерактивный рендер). Локальные режимы вызывают `GameSessionManager` напрямую. Онлайн — `GameHub` (SignalR), источник истины на сервере; клиентам уходит `MatchView` (без чужой руки).
- **WebGwent.Tests** — проверка каталога, валидации колод, погоды, Tight Bond, Spy, ничьей Nilfgaard и т.д.

```
Browser → Blazor / SignalR → GameSessionManager → RulesEngine → MatchState
```

## 3. Игровой цикл

1. Создание матча из двух валидных колод, раздача по 10 карт.
2. Муллиган (до 2 замен). Scoia'tael выбирает, кто ходит первым.
3. Ходы: сыграть карту / пас / способность лидера; при необходимости pending-выбор (Agile, Medic, Decoy).
4. Оба паса → подсчёт → гем; фракционные эффекты между раундами.
5. Два гема — конец матча.

## 4. Демонстрация на защите

1. Vs Bot: Northern Realms vs Monsters — показать Tight Bond / Muster.
2. Сыграть погоду и Clear Weather.
3. Spy + добор карт.
4. Online: создать комнату, второй браузер (или инкогнито) — Join по коду.
5. `dotnet test` — зелёные тесты.

## 5. Запуск и требования

- .NET 10 SDK
- Браузер с поддержкой WebSockets (для онлайн)

```bash
dotnet run --project WebGwent.Server
dotnet test
```
