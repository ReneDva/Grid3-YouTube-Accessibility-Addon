[Setup]
AppName=YouTube Navigator System V6
AppVersion=6.9.2
DefaultDirName=C:\YouTube_Navigator_V6
DefaultGroupName=YouTube V6 System
OutputBaseFilename=YouTube_V6_Full_Installer
Compression=lzma
SolidCompression=yes
; הרשאות ניהול נדרשות עבור Firewall, Defender וכתיבה לתיקיית Windows
PrivilegesRequired=admin
SetupIconFile=C:\playwright-mcp\YouTube_Skip_App_V6\navi_small.ico
UninstallDisplayIcon={app}\navi_small.ico

[Files]
; --- קבצי הליבה (Exe) ---
Source: "C:\playwright-mcp\YouTube_Skip_App_V6\nav.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\playwright-mcp\YouTube_Skip_App_V6\skip_ads.exe"; DestDir: "{app}"; Flags: ignoreversion

; --- קבצי עזר (Scripts + Icon) ---
Source: "C:\playwright-mcp\YouTube_Skip_App_V6\Setup_System.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\playwright-mcp\YouTube_Skip_App_V6\navi_small.ico"; DestDir: "{app}"; Flags: ignoreversion

; --- העתקת send.vbs לתיקיית המערכת (לאפשר קיצור דרך פשוט ב-Grid 3) ---
Source: "C:\playwright-mcp\YouTube_Skip_App_V6\send.vbs"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\playwright-mcp\YouTube_Skip_App_V6\send.vbs"; DestDir: "{win}"; Flags: ignoreversion

; --- קבצי מקור לגיבוי ---
Source: "C:\playwright-mcp\YouTube_Skip_App_V6\youtube_navigator.js"; DestDir: "{app}\src"; Flags: ignoreversion
Source: "C:\playwright-mcp\YouTube_Skip_App_V6\skip_ads_cdp_V6.js"; DestDir: "{app}\src"; Flags: ignoreversion

[Dirs]
Name: "C:\YouTube_User_Data_V5"; Permissions: users-full
Name: "{app}"; Permissions: users-full

[Icons]
Name: "{userdesktop}\הפעלת מערכת יוטיוב V6"; Filename: "{app}\Setup_System.bat"; WorkingDir: "{app}"; IconFilename: "{app}\navi_small.ico"
Name: "{group}\כיבוי מערכת מלא"; Filename: "wscript.exe"; Parameters: "send.vbs exit"; WorkingDir: "{app}"; IconFilename: "{app}\navi_small.ico"

[Run]
; 1. החרגה מהאנטי-וירוס (Windows Defender)
Filename: "powershell.exe"; \
    Parameters: "-ExecutionPolicy Bypass -Command ""Add-MpPreference -ExclusionPath '{app}', 'C:\YouTube_User_Data_V5'"""; \
    Flags: runhidden; \
    StatusMsg: "מגדיר החרגות אבטחה ב-Windows Defender..."

; 2. פתיחת חוק בחומת האש (Windows Firewall) עבור nav.exe
Filename: "netsh"; \
    Parameters: "advfirewall firewall add rule name=""YouTube Navi Server"" dir=in action=allow program=""{app}\nav.exe"" enable=yes profile=any"; \
    Flags: runhidden; \
    StatusMsg: "מגדיר הרשאות רשת בחומת האש (Firewall)..."

; 3. הפעלה אוטומטית בסיום ההתקנה
Filename: "{app}\Setup_System.bat"; Description: "הפעל את המערכת כעת"; Flags: postinstall nowait skipifsilent

[UninstallRun]
; הסרת החוק מחומת האש בזמן הסרת התקנה
Filename: "netsh"; \
    Parameters: "advfirewall firewall delete rule name=""YouTube Navi Server"""; \
    Flags: runhidden

; סגירת כל התהליכים הקשורים
Filename: "taskkill"; Parameters: "/f /im nav.exe /im skip_ads.exe /im chrome.exe /t"; Flags: runhidden; RunOnceId: "StopServices"

[UninstallDelete]
Type: files; Name: "{win}\send.vbs"

[Code]
function InitializeSetup(): Boolean;
var
  CanaryPath: String;
begin
  CanaryPath := ExpandConstant('{localappdata}\Google\Chrome SxS\Application\chrome.exe');
  if not FileExists(CanaryPath) then
  begin
    if MsgBox('שימו לב: Chrome Canary לא נמצא.' #13#10 +
              'המערכת דורשת את גרסת Canary כדי לפעול.' #13#10 +
              'האם ברצונך להמשיך?', mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
      Exit;
    end;
  end;
  Result := True;
end;