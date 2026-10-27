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

echo Starting WebGwent.Api on http://127.0.0.1:5180 (local only)...
start "WebGwent.Api" cmd /k "dotnet run --project WebGwent.Api --no-build --urls http://127.0.0.1:5180"

timeout /t 2 /nobreak >nul

echo Starting WebGwent.Server on http://0.0.0.0:5097 (LAN / Radmin / ngrok)...
start "WebGwent.Server" cmd /k "dotnet run --project WebGwent.Server --no-build --urls http://0.0.0.0:5097"

timeout /t 3 /nobreak >nul
start "" "http://localhost:5097"

echo.
echo ============================================================
echo  Local:   http://localhost:5097
echo  LAN:     http://YOUR_PC_IP:5097   (Radmin VPN IP works too)
echo  Public:  ngrok http 5097
echo ============================================================
echo  Friend opens ONLY the Server URL above — API stays on this PC.
echo  Close the two console windows to stop.
echo.
endlocal
