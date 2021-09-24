@echo off

SET installerDir=%~dp0

REM make sure we're not running within a zip folder
echo %installerDir% | findstr /C:"%TEMP%" 1>nul
if %ERRORLEVEL% EQU 0 (
    echo Oops! It looks like you're running the installer from inside a zip file. Make sure you unzip the download first.
    echo.
    pause
    exit
)

REM make sure .NET 5 is installed
WHERE dotnet /q
if %ERRORLEVEL% NEQ 0 (
    echo Oops! You must have .NET 5 ^(desktop x64^) installed to use SMAPI: https://dotnet.microsoft.com/download/dotnet/5.0/runtime
    echo.
    pause
    exit
)

REM make sure an antivirus hasn't deleted the installer DLL
if not exist "%installerDir%internal\windows\SMAPI.Installer.dll" (
    echo Oops! SMAPI is missing one of its files. Your antivirus might have deleted it.
    echo Missing file: %installerDir%internal\windows\SMAPI.Installer.dll
    echo.
    pause
    exit
)

REM start installer
dotnet internal\windows\SMAPI.Installer.dll
