@echo off
setlocal
cd /d "%~dp0"
echo.
echo [Backpack Config] Starte Konfiguration...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0apply_config.ps1"
if errorlevel 1 (
    echo.
    echo [Backpack Config] Die Konfiguration konnte nicht angewendet werden.
) else (
    echo.
    echo [Backpack Config] Die Konfiguration wurde erfolgreich angewendet.
)
echo.
pause
