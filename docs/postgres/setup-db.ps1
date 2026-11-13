# Поднять БД webgwent на Windows (нужен psql в PATH или PostgreSQL\bin)
param(
    [string]$User = "postgres",
    [string]$HostName = "localhost",
    [string]$Psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
)

$ErrorActionPreference = "Stop"
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path $Psql)) {
    $Psql = "psql"
}

Write-Host "Password for user '$User' will be prompted (or set env PGPASSWORD)."
& $Psql -U $User -h $HostName -f (Join-Path $dir "00_create_database.sql")
& $Psql -U $User -h $HostName -d webgwent -f (Join-Path $dir "01_schema.sql")
& $Psql -U $User -h $HostName -d webgwent -f (Join-Path $dir "02_seed_data.sql")
Write-Host "Done. Database webgwent is ready."
