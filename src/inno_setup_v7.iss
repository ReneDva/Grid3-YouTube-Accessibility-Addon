#define MyAppName "YouTube Navigator System V7"
#define MyAppVersion "7.0.0"
#define MyAppPublisher "Grid3 YouTube Accessibility"
#define MyAppExeName "YouTubeControl.exe"
#define PublishExe "..\\src\\YouTubeControl\\bin\\Release\\net10.0-windows\\win-x64\\publish\\YouTubeControl.exe"
#define ChromeSetupBundle "..\\Output\\ChromeSetup.exe"
; Source design icon (SVG) for V7 branding.
#define AppIconSourceSvg "..\\docs\\icon_combined_v3.svg"
; Inno Setup requires .ico for SetupIconFile / IconFilename / UninstallDisplayIcon.
; TODO: Regenerate icon_v7.ico from AppIconSourceSvg whenever the SVG is updated.
#define AppIcon "..\\docs\\icon_v7.ico"
#define UserDataDir "C:\YouTube_User_Data"
#define LegacyUserDataDir "C:\Grid3_YouTube_Accessibility_Addon_User_Data"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName=C:\YouTube_Navigator_V7
DefaultGroupName=YouTube V7 System
OutputDir=..\Output
OutputBaseFilename=YouTube_V7_Full_Installer
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
SetupIconFile={#AppIcon}
UninstallDisplayIcon={app}\icon_v7.ico
WizardStyle=modern

[Files]
Source: "{#PublishExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppIcon}"; DestDir: "{app}"; DestName: "icon_v7.ico"; Flags: ignoreversion

; Bundled Chrome Canary installer for prerequisite install when missing.
Source: "{#ChromeSetupBundle}"; DestDir: "{tmp}"; DestName: "ChromeSetup.exe"; Flags: ignoreversion deleteafterinstall

[Dirs]
Name: "{#UserDataDir}"; Permissions: users-full
Name: "{app}"; Permissions: users-full

[Icons]
Name: "{userdesktop}\YouTube Navigator V7"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\icon_v7.ico"
Name: "{group}\YouTube Navigator V7"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\icon_v7.ico"

[Run]
; Install Chrome Canary silently only if it is not already installed.
Filename: "{tmp}\ChromeSetup.exe"; \
    Parameters: "/silent /install"; \
    Flags: runhidden waituntilterminated; \
    StatusMsg: "Installing Chrome Canary prerequisite..."; \
    Check: not IsCanaryInstalled

; Configure Windows Defender exclusions for app and data directory.
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -Command ""Add-MpPreference -ExclusionPath '{app}', '{#UserDataDir}'"""; \
    Flags: runhidden waituntilterminated; \
    StatusMsg: "Configuring Windows Defender exclusions..."

; Optional first launch after setup.
Filename: "{app}\{#MyAppExeName}"; Description: "Launch YouTube Navigator V7 now"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im YouTubeControl.exe /im chrome.exe /t"; Flags: runhidden; RunOnceId: "StopYouTubeControl"

[Code]
function CanaryPath: string;
begin
  Result := ExpandConstant('{localappdata}\Google\Chrome SxS\Application\chrome.exe');
end;

function IsCanaryInstalled: Boolean;
begin
  Result := FileExists(CanaryPath);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;

function HasProfileData(BaseDir: string): Boolean;
var
  DefaultDir: string;
begin
  DefaultDir := AddBackslash(BaseDir) + 'Default';
  Result := FileExists(AddBackslash(DefaultDir) + 'Login Data') or
            FileExists(AddBackslash(DefaultDir) + 'Preferences');
end;

function NeedsManualSignIn(): Boolean;
begin
  Result := not HasProfileData('{#UserDataDir}') and
            not HasProfileData('{#LegacyUserDataDir}');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and not WizardSilent and NeedsManualSignIn() then
  begin
    MsgBox(
      'Initial startup must be performed by a teacher or therapist.' + #13#10 +
      'A manual sign-in to the user''s Chrome profile account is required for first-time setup only.',
      mbInformation,
      MB_OK);
  end;
end;
