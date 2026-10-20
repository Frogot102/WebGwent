# Сценарии демо к защите

## 1. Локальный матч с ботом (2–3 мин)

1. `dotnet run --project WebGwent.Server`
2. Меню → **Vs Bot** → Northern Realms vs Monsters → Start
3. Муллиган → Ready
4. Сыграть **Blue Stripes** / **Crinfrid** (Tight Bond) или юнит с высокой силой
5. Показать пас и конец раунда / гемы ◆◇

## 2. Механики (по желанию в том же матче)

- **Погода**: Biting Frost / Fog / Rain → сила ряда = 1 у не-героев
- **Clear Weather** — сброс погоды
- **Spy** (Prince Stennis и т.п.) — карта на стороне врага, +2 в руку
- **Leader** — кнопка Leader (один раз за матч)

## 3. Онлайн PvP

1. Браузер A: **Online PvP** → Create room → скопировать код
2. Браузер B (инкогнито): Join + тот же код
3. Оба проходят муллиган и делают по ходу
4. Обрыв сети: перезагрузка вкладки + тот же код через переподключение Hub (automatic reconnect)

## 4. Тесты и код

```bash
dotnet test
```

Показать `WebGwent.Game/Engine/RulesEngine.cs` и `docs/COURSEWORK.md`.
