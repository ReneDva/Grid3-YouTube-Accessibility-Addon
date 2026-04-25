# Grid3-YouTube-Accessibility-Addon (V7)

Grid3-YouTube-Accessibility-Addon is a Windows background controller that connects Grid 3 commands to YouTube in Chrome via CDP.

V7 replaces the V6 JavaScript + VBScript bridge with a single .NET WinExe process model:
- Leader mode (resident background process)
- Messenger mode (short-lived command relay)
- Named Pipe IPC (no HTTP command server)
- In-process ad skipper task

Docs:
- Setup and caregiver workflow: [docs/SETUP.md](docs/SETUP.md)
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

## Build and Run

### Build

```powershell
dotnet build src/YouTubeControl/YouTubeControl.csproj
```

### Test

```powershell
dotnet test tests/YouTubeControl.Tests/YouTubeControl.Tests.csproj
```

### Start Leader

```powershell
src/YouTubeControl/bin/Debug/net10.0-windows/YouTubeControl.exe
```

### Send Commands (Messenger Mode)

```powershell
src/YouTubeControl/bin/Debug/net10.0-windows/YouTubeControl.exe home
src/YouTubeControl/bin/Debug/net10.0-windows/YouTubeControl.exe down
src/YouTubeControl/bin/Debug/net10.0-windows/YouTubeControl.exe search:disney songs
```

---

## In-Process Ad Skipper (V7)

V7 ad skipping runs inside Leader mode and no longer requires a separate `skip_ads.exe` process.

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
