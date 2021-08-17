@echo off
echo "%~dp0" | findstr /C:"%TEMP%" 1>nul
if not errorlevel 1 (
   echo Oops! It looks like you're running the installer from inside a zip file. Make sure you unzip the download first.
   pause
) else (
   start /WAIT /B internal\windows\install.exe
)
