@echo off
CHCP 1252
cd /d "%~dp0.."
dotnet pack JOSYN.Commons.Schedule --output "..\..\local-packages"
if %ERRORLEVEL% neq 0 (
    echo [FEHLER] Pack JOSYN.Commons.Schedule fehlgeschlagen.
    exit /b %ERRORLEVEL%
)
echo.
echo [OK] Paket erfolgreich gepackt.
REM pause
