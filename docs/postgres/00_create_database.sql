-- Создать пустую БД (выполнить в pgAdmin Query Tool, подключённом к postgres)
-- Или: psql -U postgres -f 00_create_database.sql

SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'webgwent' AND pid <> pg_backend_pid();

DROP DATABASE IF EXISTS webgwent;

CREATE DATABASE webgwent
    WITH OWNER = postgres
         ENCODING = 'UTF8'
         TEMPLATE = template0;
