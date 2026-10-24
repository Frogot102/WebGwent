@echo off
setlocal
cd /d "%~dp0"

echo Stopping previous WebGwent processes (if any)...
taskkill /IM WebGwent.Server.exe /F >nul 2>&1
taskkill /IM WebGwent.Api.exe /F >nul 2>&1
for /f "tokens=5" %%P in ('netstat -ano ^| findstr ":5097 " ^| findstr LISTENING') do taskkill /PID %%P /F >nul 2>&1
for /f "tokens=5" %%P in ('netstat -ano ^| findstr ":5180 " ^| findstr LISTENING') do taskkill /PID %%P /F >nul 2>&1
timeout /t 1 /nobreak >nul

echo Building...
dotnet build WebGwent.slnx -v q
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo Starting WebGwent.Api on http://localhost:5180 ...
start "WebGwent.Api" cmd /k "dotnet run --project WebGwent.Api --no-build --urls http://localhost:5180"

timeout /t 2 /nobreak >nul

echo Starting WebGwent.Server on http://localhost:5097 ...
start "WebGwent.Server" cmd /k "dotnet run --project WebGwent.Server --no-build --urls http://localhost:5097"

timeout /t 3 /nobreak >nul
start "" "http://localhost:5097"

echo.
echo Api:    http://localhost:5180
echo Client: http://localhost:5097
echo Close the two console windows to stop.
endlocal
