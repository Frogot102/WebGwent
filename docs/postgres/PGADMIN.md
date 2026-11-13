# PostgreSQL + pgAdmin 4 для WebGwent

## Уже сделано

База **`webgwent`** создана на PostgreSQL 18. Таблицы и данные из JSON перенесены:

| Таблица | Содержимое |
|--------|------------|
| `accounts` | 5 аккаунтов (admin, aaa, Fgt1, Fgt2, fgt3) |
| `match_history` | 9 записей |
| `invites` | 1 инвайт |
| `invite_requests` | 1 заявка |
| `decks` | 1 колода Mine |

В pgAdmin 4: **Refresh** у Databases → открой `webgwent` → Schemas → public → Tables.

Connection string в `appsettings.json`:
`Host=localhost;Port=5432;Database=webgwent;Username=postgres;Password=frogot1`

## Запуск

```bash
dotnet run --project WebGwent.Api
```

Повторный импорт из JSON (если нужно заново):
```bash
dotnet run --project WebGwent.Api -- --force-import-json
```
или `"Postgres": { "ForceJsonImport": true }` в appsettings.

## Что ещё в файлах

- `App_Data/custom-content.json` - кастом-контент (если появится)
- `App_Data/avatars` - картинки аватаров
- Живые комнаты матчей - в RAM Api

Логин как раньше: **admin / admin123**.
