-- Связи таблиц WebGwent → accounts
-- Сначала чистим «висячие» ссылки, потом ставим FK.

-- Колода с OwnerAccountId, которого нет в accounts → привязать к admin (если есть) или NULL
UPDATE decks d
SET "OwnerAccountId" = (SELECT "Id" FROM accounts WHERE "Username" = 'admin' LIMIT 1)
WHERE d."OwnerAccountId" IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM accounts a WHERE a."Id" = d."OwnerAccountId");

UPDATE match_history m
SET "PlayerAccountId" = NULL
WHERE m."PlayerAccountId" IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM accounts a WHERE a."Id" = m."PlayerAccountId");

UPDATE match_history m
SET "OpponentAccountId" = NULL
WHERE m."OpponentAccountId" IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM accounts a WHERE a."Id" = m."OpponentAccountId");

UPDATE invite_requests r
SET "ResolvedByAccountId" = NULL
WHERE r."ResolvedByAccountId" IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM accounts a WHERE a."Id" = r."ResolvedByAccountId");

-- Удалить заявки без аккаунта (если вдруг есть)
DELETE FROM invite_requests r
WHERE NOT EXISTS (SELECT 1 FROM accounts a WHERE a."Id" = r."AccountId");

DELETE FROM invites i
WHERE NOT EXISTS (SELECT 1 FROM accounts a WHERE a."Id" = i."CreatedByAccountId");

-- FK
ALTER TABLE decks
  DROP CONSTRAINT IF EXISTS fk_decks_owner,
  ADD CONSTRAINT fk_decks_owner
    FOREIGN KEY ("OwnerAccountId") REFERENCES accounts ("Id")
    ON DELETE SET NULL;

ALTER TABLE match_history
  DROP CONSTRAINT IF EXISTS fk_match_player,
  ADD CONSTRAINT fk_match_player
    FOREIGN KEY ("PlayerAccountId") REFERENCES accounts ("Id")
    ON DELETE SET NULL;

ALTER TABLE match_history
  DROP CONSTRAINT IF EXISTS fk_match_opponent,
  ADD CONSTRAINT fk_match_opponent
    FOREIGN KEY ("OpponentAccountId") REFERENCES accounts ("Id")
    ON DELETE SET NULL;

ALTER TABLE invites
  DROP CONSTRAINT IF EXISTS fk_invites_created_by,
  ADD CONSTRAINT fk_invites_created_by
    FOREIGN KEY ("CreatedByAccountId") REFERENCES accounts ("Id")
    ON DELETE CASCADE;

ALTER TABLE invite_requests
  DROP CONSTRAINT IF EXISTS fk_invite_requests_account,
  ADD CONSTRAINT fk_invite_requests_account
    FOREIGN KEY ("AccountId") REFERENCES accounts ("Id")
    ON DELETE CASCADE;

ALTER TABLE invite_requests
  DROP CONSTRAINT IF EXISTS fk_invite_requests_resolved_by,
  ADD CONSTRAINT fk_invite_requests_resolved_by
    FOREIGN KEY ("ResolvedByAccountId") REFERENCES accounts ("Id")
    ON DELETE SET NULL;
