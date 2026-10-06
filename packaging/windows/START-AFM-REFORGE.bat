@echo off
setlocal
cd /d "%~dp0"

start "AFM Reforge" "%~dp0AFMReforge.exe"

timeout /t 2 /nobreak >nul
start "" "http://localhost:5180/"

endlocal
