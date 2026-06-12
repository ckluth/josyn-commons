@echo off
CHCP 1252
setlocal

:: -------------------------------------------------------
:: publish-for-linux.cmd [RID]
:: Publishes IntegrationRunner + IntegrationWorker as
:: self-contained Linux binaries into C:\DevGit\temp.
::
:: RID examples: linux-x64 (default), linux-arm64, linux-arm
:: Check target architecture with: uname -m
:: -------------------------------------------------------

set "RUNTIME=%~1"
if not defined RUNTIME set "RUNTIME=linux-x64"

set "OUTPUT=C:\DevGit\temp\linux-turnstile-integrationtest"
set "ROOT=%~dp0.."

if not exist "%OUTPUT%" mkdir "%OUTPUT%"

echo [INFO] Runtime : %RUNTIME%
echo [INFO] Output  : %OUTPUT%
echo.

dotnet publish "%ROOT%\JOSYN.Commons.Helpers.Turnstile.IntegrationRunner" ^
    --configuration Release ^
    --runtime %RUNTIME% ^
    --self-contained true ^
    --output "%OUTPUT%"
if %ERRORLEVEL% neq 0 (
    echo.
    echo [FEHLER] Publish IntegrationRunner fehlgeschlagen.
    exit /b %ERRORLEVEL%
)

dotnet publish "%ROOT%\JOSYN.Commons.Helpers.Turnstile.IntegrationWorker" ^
    --configuration Release ^
    --runtime %RUNTIME% ^
    --self-contained true ^
    --output "%OUTPUT%"
if %ERRORLEVEL% neq 0 (
    echo.
    echo [FEHLER] Publish IntegrationWorker fehlgeschlagen.
    exit /b %ERRORLEVEL%
)

echo.
echo [OK] Publish abgeschlossen: %OUTPUT% ^(%RUNTIME%^)
exit /b 0
