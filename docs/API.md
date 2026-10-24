# WebGwent — клиент и API

Решение разделено на два процесса:

| Проект | Роль | Порт (dev) |
|--------|------|------------|
| `WebGwent.Api` | REST + SignalR, аккаунты, матчи, колоды | `http://localhost:5180` |
| `WebGwent.Server` | Blazor UI (только клиент API) | `http://localhost:5097` |

`WebGwent.Game` — движок правил (библиотека). `WebGwent.Tests` — тесты движка.

## Запуск

В двух терминалах:

```bash
dotnet run --project WebGwent.Api
dotnet run --project WebGwent.Server
```

Откройте UI: http://localhost:5097  
Админ: `admin` / `admin123`

Базовый URL API для UI задаётся в `WebGwent.Server/appsettings.json` → `ApiBaseUrl`.

## Auth

- `POST /api/auth/login` → `{ token, account }` (JWT)
- `POST /api/auth/register`
- `GET /api/auth/me` (Bearer)

SignalR Hub: `/hubs/game?access_token=…`

## Основные REST-группы

- `/api/decks` — список / сохранение / удаление колод
- `/api/matches/bot` — матч против бота + ходы / bot-step
- `/api/plus/*` — демо-покупка и инвайты
- `/api/account/*` — профиль, история, аватар
