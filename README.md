# Grid3-YouTube-Accessibility-Addon (V7)

_If you are a parent, teacher, therapist, or installer, start with the V7 user guide: [docs/SETUP_V7.md](docs/SETUP_V7.md)._

## Overview

Grid3-YouTube-Accessibility-Addon V7 is a Windows background controller that connects Grid 3 command cells to YouTube in Chrome through CDP (Chrome DevTools Protocol).

V7 replaced the old V6 script chain (`send.vbs` + HTTP + `nav.exe`) with a single C# executable that runs in two modes:

- Leader mode: resident controller process
- Messenger mode: short-lived command relay process

This removes command-window flicker, removes HTTP command transport from runtime, and centralizes lifecycle control in one process.

---

## Documentation Map

Use this map based on your current task.

| Developer Journey | Start Here | Then Continue To |
|---|---|---|
| Discovery | [Overview](#overview) | [Core Concepts and Architecture](#core-concepts-and-architecture) |
| Evaluation | [Technology Stack](#technology-stack) | [Supported Command Reference](#supported-command-reference) |
| First Implementation | [Getting Started](#getting-started) | [Build, Test, and Packaging](#build-test-and-packaging) |
| Troubleshooting | [Troubleshooting](#troubleshooting) | [Validation and Advanced Operations](#validation-and-advanced-operations) |
| Advanced Usage | [Validation and Advanced Operations](#validation-and-advanced-operations) | [Release Notes and Branch History](#implementation-notes-release-notes-and-branch-history) |

---

## Getting Started

### Prerequisites

- Windows 10/11
- .NET 10 SDK
- Chrome Canary (preferred runtime target; stable fallback logic exists)
- Inno Setup 6 (required only to build installer)

### Quick Start (Developer)

Run from repository root.

```powershell
dotnet build src/YouTubeControl/YouTubeControl.csproj
dotnet test tests/YouTubeControl.Tests/YouTubeControl.Tests.csproj
dotnet publish src/YouTubeControl/YouTubeControl.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishAot=true
```

Published binary:

```text
src/YouTubeControl/bin/Release/net10.0-windows/win-x64/publish/YouTubeControl.exe
```

### First Runtime Check

```powershell
cd C:\YouTube_Navigator_V7
.\YouTubeControl.exe
.\YouTubeControl.exe home
.\YouTubeControl.exe stop
```

---

## Core Concepts and Architecture

### Core Technical References

- Active runtime architecture document: [ARCHITECTURE.md](ARCHITECTURE.md)
- Runtime flow and behavior reference: [src/SYSTEM_STATE.md](src/SYSTEM_STATE.md)
- V7 migration design and rollout notes: [docs/Architecture_Design.V7-plan.md](docs/Architecture_Design.V7-plan.md)

### Runtime Model (V7)

| Component | Responsibility |
|---|---|
| YouTubeControl.exe (Leader) | Owns CDP session, receives commands, executes actions, runs ad-skipper, controls shutdown |
| YouTubeControl.exe <action> (Messenger) | Sends one command to Leader over named pipe and exits quickly |
| Named Pipe (YouTubeControlPipe) | Local low-latency command transport |
| Global Mutex | Single-leader election (`Global\\YouTubeControl_Leader_Mutex`) |
| ChromeManager | Chrome discovery/launch, debug port wiring, user-data directory handling |

---

## Technology Stack

| Area | Technology | Version / Notes |
|---|---|---|
| Primary language | C# | .NET 10 (`net10.0-windows`) |
| Runtime model | WinExe + WinForms capability | Headless background process with Windows app manifest |
| Browser automation | PuppeteerSharp | NuGet: `PuppeteerSharp` `24.40.0` |
| IPC | Named Pipes | `System.IO.Pipes`, pipe name `YouTubeControlPipe` |
| Browser protocol | CDP | Debug endpoint on port `15432` |
| Ad skipping | In-process background task | Poll interval `1500ms` |
| Build / packaging | .NET CLI + Inno Setup | Installer script: `src/inno_setup_v7.iss` |
| Test framework | xUnit + Microsoft.NET.Test.Sdk + Coverlet | Tests in `tests/YouTubeControl.Tests` |
| Utility scripting | PowerShell | Regression sequence runner in `scripts/` |

### Languages and Artifacts

- C# (.NET): runtime, IPC server/client, command dispatch, browser lifecycle, logging
- JavaScript (embedded/injected): browser-side navigation and ad-skip logic via CDP evaluation
- PowerShell: sequence-based validation workflows
- Inno Setup script: Windows installer generation

---

## Supported Command Reference

| Command | Description |
|---|---|
| home | Navigate to YouTube home and normalize interaction state |
| up | Move highlight to previous item |
| down | Move highlight to next item |
| enter | Activate highlighted item |
| back | Browser history back |
| play_pause | Toggle player state |
| fullscreen | Toggle fullscreen mode |
| toggle | Alias for fullscreen path |
| like | Toggle like action where supported |
| search:<query> | Open YouTube search results |
| open:<url> | Open explicit URL |
| refresh | Reload active page |
| exit | Close browser and terminate leader |
| stop | Alias for exit |

---

## Build, Test, and Packaging

### Build

```powershell
dotnet build src/YouTubeControl/YouTubeControl.csproj
```

### Unit Tests

```powershell
dotnet test tests/YouTubeControl.Tests/YouTubeControl.Tests.csproj
```

### Publish Release Binary (single-file AOT)

```powershell
dotnet publish src/YouTubeControl/YouTubeControl.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishAot=true
```

Expected output:

```text
src/YouTubeControl/bin/Release/net10.0-windows/win-x64/publish/YouTubeControl.exe
```

### Build Installer

```powershell
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "src\inno_setup_v7.iss"
```

Expected output:

```text
Output/YouTube_V7_Full_Installer.exe
```

---

## Repository Layout

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

tests/
  YouTubeControl.Tests/

scripts/
  run_youtubecontrol_sequence.ps1

docs/
  SETUP_V7.md
  SETUP_V6.md
  Architecture_Design.V7-plan.md
```

---

## Troubleshooting

| Problem | What to check |
|---|---|
| Commands do nothing | Confirm Leader is running first (`YouTubeControl.exe` with no args) |
| Browser does not open | Verify Chrome Canary exists and launch path is available |
| Command cell fails in Grid 3 | Ensure each cell runs `YouTubeControl.exe <action>` |
| Search/open command fails | Validate command format: `search:<query>` or `open:<url>` |
| Shutdown fails | Use `exit` or `stop` explicitly |

---

## Validation and Advanced Operations

### Automated full-sequence validation

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/run_youtubecontrol_sequence.ps1
```

Stop on first failure:

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/run_youtubecontrol_sequence.ps1 -StopOnFailure
```

### Manual runtime smoke test (full command coverage)

```powershell
cd C:\YouTube_Navigator_V7
.\YouTubeControl.exe
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

---

## Implementation Notes, Release Notes, and Branch History

Implementation notes:

- V7 runtime does not use the old V6 HTTP command-server model
- Grid 3 command cells should call `YouTubeControl.exe <action>` directly
- Ad skipping is integrated in-process in `AdSkipperTask` (no external `skip_ads.exe` dependency)
- Legacy V6 assets may exist for migration/reference purposes only

Branch references:

- Current version branch (active/default): [main](https://github.com/ReneDva/Grid3-YouTube-Accessibility-Addon/tree/main)
- Older V6 baseline snapshot branch: [backup/v6-script-http-baseline](https://github.com/ReneDva/Grid3-YouTube-Accessibility-Addon/tree/backup/v6-script-http-baseline)