@echo off
CHCP 1252
cd /d "%~dp0.."
dotnet pack JOSYN.Commons.Log --output "..\..\local-packages"
if %ERRORLEVEL% neq 0 (
    echo [FEHLER] Pack JOSYN.Commons.Log fehlgeschlagen.
    exit /b %ERRORLEVEL%
)
echo.
echo [OK] Paket erfolgreich gepackt.
REM pause
