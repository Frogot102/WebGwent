# Сценарии демо к защите

## Подготовка

```bash
dotnet run --project WebGwent.Api
dotnet run --project WebGwent.Server
# или: docker compose up --build
```

UI: http://localhost:5097 · вшитый админ `admin` / `admin123`.

## 1. Локальный матч с ботом (2–3 мин)

1. Меню → **Vs Bot** → Northern Realms vs Monsters → Easy → Start  
2. Муллиган → Ready  
3. Tight Bond / Muster / погода → пас → гемы ◆◇  
4. Звук/вспышка при погоде и Scorch (клик по странице разблокирует AudioContext)

## 2. Admin-контент + пассив

1. Войти как admin → профиль → Admin content  
2. Добавить фракцию, выбрать **Passive** (например Skellige) → Save  
3. Добавить карту этой фракции → колода в Deck Builder → Vs Bot

## 3. Plus → Online PvP

1. Второй аккаунт: профиль → Buy Plus → checkout stub → запрос инвайта  
2. Admin: approve → пользователь redeem / получает код  
3. Браузер A: Online → Create room → код  
4. Браузер B: Join → тот же код  
5. Показать таймер хода (3 мин, красный &lt; 30 с)  
6. После матча: History — очки по раундам; Leaderboard — Elo

## 4. Владение колодами

1. Два аккаунта сохраняют свои колоды  
2. Каждый в списке видит только пресеты + свои (не чужие)

## 5. Тесты

```bash
dotnet test
```

Показать `RulesEngine.SkelligeRevive` (два сильнейших не-героя) и `docs/COURSEWORK.md`.
