@echo off
title YouTube System V6.9 - Final Stabilized Launcher
:: הגדרת נתיבים קבועים
set "BASE_DIR=C:\YouTube_Navigator_V6"
set "DATA_DIR=C:\YouTube_User_Data_V5"
set "CHROME_PATH=%LOCALAPPDATA%\Google\Chrome SxS\Application\chrome.exe"

:: מעבר לתיקיית העבודה
cd /d "%BASE_DIR%"

:: 1. ניקוי יסודי של תהליכים
echo [1/5] Cleaning up previous sessions...
taskkill /f /im nav.exe >nul 2>&1
taskkill /f /im skip_ads.exe >nul 2>&1
taskkill /f /im chrome.exe >nul 2>&1

:: בדיקה שכרום קיים
if not exist "%CHROME_PATH%" (
    powershell -Command "Add-Type -AssemblyName PresentationFramework; [System.Windows.MessageBox]::Show('שגיאה: Chrome Canary לא נמצא במחשב', 'Navi Error', 'OK', 'Error')"
    exit
)

:: 2. הפעלת כרום - נוסף דגל שחזור שקט
echo [2/5] Launching Chrome Canary (Port 15432)...
if not exist "%DATA_DIR%" mkdir "%DATA_DIR%"
start "" "%CHROME_PATH%" --remote-debugging-port=15432 --user-data-dir="%DATA_DIR%" --no-first-run --no-default-browser-check --autoplay-policy=no-user-gesture-required --disable-session-crashed-bubble --disable-infobars --restore-last-session "https://www.youtube.com"

:: קיצור המתנה לכרום ל-3 שניות
echo Waiting for Chrome (3s)...
timeout /t 3 >nul

:: 3. הפעלת השרת
echo [3/5] Activating Navigator Server...
if exist "nav.exe" (
    start /min "" "nav.exe"
    :: קיצור המתנה לשרת ל-2 שניות
    timeout /t 2 >nul
) else (
    powershell -Command "Add-Type -AssemblyName PresentationFramework; [System.Windows.MessageBox]::Show('שגיאה: הקובץ nav.exe חסר בתיקייה', 'Navi Error', 'OK', 'Error')"
    exit
)

:: 4. בדיקת האזנה בפורט 3000
echo [4/5] Verifying Port 3000...
netstat -ano | findstr :3000 >nul
if %errorlevel% neq 0 (
    echo [RETRYING] Port 3000 not ready yet, waiting 2 more seconds...
    timeout /t 2 >nul
    netstat -ano | findstr :3000 >nul
    if %errorlevel% neq 0 (
        echo [ERROR] Server is not listening on port 3000!
        powershell -Command "Add-Type -AssemblyName PresentationFramework; [System.Windows.MessageBox]::Show('השרת (nav.exe) עלה אך אינו מאזין בפורט 3000. ייתכן שחומת האש חוסמת אותו.', 'Navi Connection Error', 'OK', 'Warning')"
        pause
        exit
    )
)

:: 5. החזרת פוקוס לגריד ושליחת פקודת התחלה
echo [5/5] Finalizing...
wscript.exe "C:\Windows\send.vbs" home
powershell -command "$wshell = New-Object -ComObject WScript.Shell; $wshell.AppActivate('Grid 3')"

echo ======================================================
echo  SYSTEM V6.9 IS READY AND ACTIVE
echo ======================================================
timeout /t 1 >nul
exit