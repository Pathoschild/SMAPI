@echo off
setlocal enabledelayedexpansion

:: ดึง Default Gateway IP
for /f "tokens=3" %%B in ('netsh interface ip show config name^="Wi-Fi" ^| findstr /i "Default Gateway"') do (
    set gateway=%%B
)

:: แสดงผล
echo Gateway IP: %gateway%

:: ตรวจสอบว่า gateway เริ่มด้วย 192.168
echo %gateway% | findstr "^192\.168" >nul
if %errorlevel%==0 (
    echo ✅ Gateway IP valid: %gateway%
    echo Running: adb connect %gateway%
    adb connect %gateway%
) else (
    echo ❌ Gateway IP not in 192.168.x.x range. Skipping adb connect.
)

pause