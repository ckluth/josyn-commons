@echo off
CHCP 1252
cd /d "%~dp0.."
dotnet test
REM pause
