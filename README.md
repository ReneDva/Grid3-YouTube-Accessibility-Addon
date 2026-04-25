# Grid3-YouTube-Accessibility-Addon (V7)

Grid3-YouTube-Accessibility-Addon is a Windows background controller that connects Grid 3 commands to YouTube in Chrome via CDP.

V7 replaces the V6 JavaScript + VBScript bridge with a single .NET WinExe process model:
- Leader mode (resident background process)
- Messenger mode (short-lived command relay)
- Named Pipe IPC (no HTTP command server)
- In-process ad skipper task

Docs:
- Setup and caregiver workflow (V7): [docs/SETUP_V7.md](docs/SETUP_V7.md)
- Legacy setup guide (V6): [docs/SETUP_V6.md](docs/SETUP_V6.md)
- Runtime architecture: [ARCHITECTURE.md](ARCHITECTURE.md)
- Migration plan and stage status: [docs/Architecture_Design.V7-plan.md](docs/Architecture_Design.V7-plan.md)

---

## Current Runtime Model (V7)

| Component | Responsibility |
|---|---|
| `YouTubeControl.exe` (Leader) | Owns CDP session, executes commands, runs in-process ad-skipper, handles shutdown |
| `YouTubeControl.exe <action>` (Messenger) | Sends action over named pipe and exits quickly |
| Named Pipe (`YouTubeControlPipe`) | Low-latency local IPC between Messenger and Leader |
| Global Mutex | Single leader election (`Global\\YouTubeControl_Leader_Mutex`) |
| Chrome Canary / Chrome Stable | Browser target on debug port `15432` |

---

## Supported Actions

| Action | Behavior |
|---|---|
| `home` | Navigate to YouTube home and normalize page interaction state |
| `up` | Move focus up / previous item |
| `down` | Move focus down / next item |
| `enter` | Activate focused item |
| `back` | Navigate browser history back |
| `play_pause` | Toggle player play/pause |
| `fullscreen` | Toggle fullscreen |
| `toggle` | Alias command handled by fullscreen toggle path |
| `like` | Toggle Like action in supported player contexts |
| `search:<query>` | Open YouTube search for `<query>` |
| `open:<url>` | Open explicit URL |
| `refresh` | Reload active YouTube page |
| `exit` | Close browser and terminate leader |
| `stop` | Alias to `exit` |

---

## Full Build and Packaging Instructions (V7)

Run all commands from repository root.

### 1. Build (Debug)

```powershell
dotnet build src/YouTubeControl/YouTubeControl.csproj
```

### 2. Run Unit Tests

```powershell
dotnet test tests/YouTubeControl.Tests/YouTubeControl.Tests.csproj
```

### 3. Publish Production Binary (Release, Single File)

```powershell
dotnet publish src/YouTubeControl/YouTubeControl.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishAot=true
```

Expected output:

```text
src/YouTubeControl/bin/Release/net10.0-windows/win-x64/publish/YouTubeControl.exe
```

### 4. Build Installer (Inno Setup)

```powershell
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "src\inno_setup_v7.iss"
```

Expected output:

```text
Output/YouTube_V7_Full_Installer.exe
```

---

## Built-In Ad Skipper (V7, Implemented)

The V7 ad skipper is already implemented and active in the current codebase.
It runs inside Leader mode and no longer requires a separate `skip_ads.exe` process.

Behavior:
- Polls every `1500ms`
- Targets active YouTube `watch` / `shorts` pages
- Searches for visible skip/close-ad selectors
- Clicks skip target when found
- Logs only when a click is actually executed (keeps steady-state logs clean)
- Stops cleanly via `CancellationToken` when `exit` or `stop` is triggered

Implementation:
- `src/YouTubeControl/Actions/AdSkipperTask.cs`
- Integrated in `src/YouTubeControl/LeaderMode.cs`

---

## Automated Full Regression Script

Use the full-sequence regression runner:

`scripts/run_youtubecontrol_sequence.ps1`

What it does:
- Stops any existing `YouTubeControl.exe` process
- Starts Leader mode (no-args)
- Waits 15 seconds before the first action
- Executes full action sequence coverage
- Waits 7 seconds after `home`, otherwise 5 seconds
- Restarts and verifies leader after terminal actions when sequence continues

Run:

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/run_youtubecontrol_sequence.ps1
```

Stop on first failure:

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/run_youtubecontrol_sequence.ps1 -StopOnFailure
```

---

## Manual Full Command Validation (All Actions)

After installation, run this from CMD or PowerShell:

```powershell
cd C:\YouTube_Navigator_V7
```

### 1. Start Leader (no arguments)

```powershell
.\YouTubeControl.exe
```

Wait until Chrome opens and YouTube is ready.

### 2. Run full action coverage (Messenger commands)

```powershell
.\YouTubeControl.exe home
.\YouTubeControl.exe down
.\YouTubeControl.exe up
.\YouTubeControl.exe enter
.\YouTubeControl.exe back

.\YouTubeControl.exe play_pause
.\YouTubeControl.exe play_pause

.\YouTubeControl.exe fullscreen
.\YouTubeControl.exe fullscreen
.\YouTubeControl.exe toggle

.\YouTubeControl.exe like
.\YouTubeControl.exe like

.\YouTubeControl.exe search:disney songs
.\YouTubeControl.exe down
.\YouTubeControl.exe enter

.\YouTubeControl.exe open:https://www.youtube.com/shorts
.\YouTubeControl.exe down

.\YouTubeControl.exe refresh

.\YouTubeControl.exe stop
```

Notes:
- `stop` is an alias of `exit` and terminates Leader.
- If you prefer, end with `.\YouTubeControl.exe exit` instead of `stop`.

---

## Publish (Single File)

```powershell
dotnet publish src/YouTubeControl/YouTubeControl.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## V7 Source Layout

```text
src/
  YouTubeControl/
    Program.cs
    LeaderMode.cs
    MessengerMode.cs
    ChromeManager.cs
    Logger.cs
    Actions/
      NavigationActions.cs
      AdSkipperTask.cs
    Models/
      AppConfig.cs

scripts/
  run_youtubecontrol_sequence.ps1
```

---

## Notes

- V7 no longer uses port `3000` command HTTP flow.
- Grid 3 cells should call `YouTubeControl.exe <action>` directly.
- Legacy V6 assets may still exist in the repository for migration history; see cleanup guidance in ongoing migration tasks.
