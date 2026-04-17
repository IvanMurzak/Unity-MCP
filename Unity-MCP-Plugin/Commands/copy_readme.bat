@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\..\"
set "SOURCE_README=%REPO_ROOT%README.md"
set "PLUGIN_README=%REPO_ROOT%Unity-MCP-Plugin\Packages\com.ivanmurzak.unity.mcp\README.md"
set "INSTALLER_README=%REPO_ROOT%Installer\Assets\com.IvanMurzak\AI Game Dev Installer\README.md"

if not exist "%SOURCE_README%" (
    echo Source README not found: "%SOURCE_README%"
    exit /b 1
)

copy /Y "%SOURCE_README%" "%PLUGIN_README%" >nul
if errorlevel 1 exit /b 1

copy /Y "%SOURCE_README%" "%INSTALLER_README%" >nul
if errorlevel 1 exit /b 1
