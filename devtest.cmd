@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

where pwsh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
  pwsh -NoProfile -File "%SCRIPT_DIR%scripts\test.ps1" %*
  exit /b %ERRORLEVEL%
)

where powershell >nul 2>nul
if %ERRORLEVEL% EQU 0 (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\test.ps1" %*
  exit /b %ERRORLEVEL%
)

echo Unable to find pwsh or powershell on PATH.
exit /b 1
