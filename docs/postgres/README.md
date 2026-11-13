# PostgreSQL для WebGwent (pgAdmin 4 / другой ПК)

Файлы в этой папке позволяют поднять ту же БД, что используется Api.

| Файл | Назначение |
|------|------------|
| `00_create_database.sql` | Создать пустую БД `webgwent` |
| `01_schema.sql` | Таблицы, индексы, внешние ключи |
| `02_seed_data.sql` | Данные (аккаунты, матчи, инвайты, колода) |
| `add_foreign_keys.sql` | Только FK (если схема уже есть без связей) |

## Вариант A: pgAdmin 4

1. Установить PostgreSQL + pgAdmin 4.
2. Подключиться к серверу (логин `postgres`, свой пароль).
3. **Query Tool** на базе `postgres` → открыть и выполнить `00_create_database.sql`.
4. В Object Explorer: Refresh → выбрать базу **`webgwent`**.
5. Query Tool уже на `webgwent` → выполнить `01_schema.sql`.
6. Затем выполнить `02_seed_data.sql`.
7. Refresh → Tables: должны быть `accounts`, `match_history`, `decks`, `invites`, `invite_requests`.

## Вариант B: командная строка

```bash
psql -U postgres -f docs/postgres/00_create_database.sql
psql -U postgres -d webgwent -f docs/postgres/01_schema.sql
psql -U postgres -d webgwent -f docs/postgres/02_seed_data.sql
```

## Подключение Api

В `WebGwent.Api/appsettings.json` (или лучше `appsettings.Development.json`):

```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=webgwent;Username=postgres;Password=ВАШ_ПАРОЛЬ"
}
```

Образец без секрета: `WebGwent.Api/appsettings.Example.json`.

Запуск:

```bash
dotnet run --project WebGwent.Api
```

Логин по умолчанию после сида / SeedAdmin: **admin / admin123**.

## Без ручного SQL

Если БД `webgwent` уже создана, а таблиц нет — при старте Api сработает `EnsureCreated` (пустые таблицы). Данные можно не заливать: зарегистрировать пользователей заново или выполнить только `02_seed_data.sql` после схемы.

Повторный импорт из старых JSON (если файлы лежат в `App_Data`):

```bash
dotnet run --project WebGwent.Api -- --force-import-json
```
