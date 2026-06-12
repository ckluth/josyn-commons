@echo off
CHCP 1252
setlocal

set "NUGET_BASE=%USERPROFILE%\.nuget\packages"
set PACKAGES=josyn.commons.helpers

for %%P in (%PACKAGES%) do (
    if exist "%NUGET_BASE%\%%P" (
        echo Loesche: %NUGET_BASE%\%%P
        rd /s /q "%NUGET_BASE%\%%P"
        if errorlevel 1 ( echo   FEHLER ) else ( echo   OK )
    ) else (
        echo Nicht gefunden, uebersprungen: %NUGET_BASE%\%%P
    )
)

echo.
echo [OK] NuGet-Cache bereinigt.
if /i "%~1" neq "NOPAUSE" pause
