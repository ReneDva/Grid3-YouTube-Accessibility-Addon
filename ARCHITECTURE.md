# Architecture — Grid3-YouTube-Accessibility-Addon V7

This document describes the active V7 architecture implemented in the repository.

---

## 1. System Overview

V7 is a resident C# controller that replaces the V6 chain (`send.vbs` -> HTTP -> `nav.exe`) with a single executable in dual mode.

```text
                        Grid 3 startup cell
+-------------+ -------------------------------> +--------------------------+
|   Grid 3    |                                   | YouTubeControl.exe       |
| (YouTube    |                                   | Leader mode              |
|  grid set)  |                                   | - Owns CDP connection    |
+-------------+                                   | - Executes actions       |
      |                                           | - Runs ad skipper task   |
      | Grid command cell                         +--------------------------+
      | YouTubeControl.exe "search:q"                       ^
      v                                                      | Named Pipe
+--------------------------+                                 |
| YouTubeControl.exe       | --------------------------------+
| Messenger mode           |   writes command, exits fast
+--------------------------+
```

---

## 2. Core Runtime Components

### 2.1 Program

File: `src/YouTubeControl/Program.cs`

Responsibilities:
- Initializes process-wide logging and exception handlers
- Performs mutex-based leader election (`Global\\YouTubeControl_Leader_Mutex`)
- Routes no-arg instance to Leader mode
- Routes arg-based instance to Messenger mode
- Coordinates graceful shutdown through cancellation token

### 2.2 LeaderMode

File: `src/YouTubeControl/LeaderMode.cs`

Responsibilities:
- Hosts named pipe server loop (`YouTubeControlPipe`)
- Parses, validates, and dispatches supported actions
- Maintains Chrome/CDP connectivity and reconnect loop
- Runs in-process ad-skipper polling loop (1500ms)
- Handles explicit exit/stop lifecycle and browser close

Parallel loops in Leader:
- Pipe server loop
- CDP recovery loop
- Ad-skipper loop

### 2.3 MessengerMode

File: `src/YouTubeControl/MessengerMode.cs`

Responsibilities:
- Connects to named pipe
- Sends single command payload
- Exits immediately

### 2.4 ChromeManager

File: `src/YouTubeControl/ChromeManager.cs`

Responsibilities:
- Resolves Chrome binary path (Canary first, then Stable fallback)
- Launches Chrome with debug port `15432`
- Uses fixed user-data directory (`C:\YouTube_User_Data`)

### 2.5 NavigationActions

File: `src/YouTubeControl/Actions/NavigationActions.cs`

Responsibilities:
- Provides browser-side JS action logic for navigation and interaction
- Supports Home, search/open flows, list navigation, select, player actions

### 2.6 AdSkipperTask

File: `src/YouTubeControl/Actions/AdSkipperTask.cs`

Responsibilities:
- Ports V6 browser-side ad-skip script into C# constant
- Detects visible skip/close-ad controls on active YouTube watch/shorts pages
- Clicks target when found
- Logs only when click is executed

---

## 3. Command Flow

1. Grid 3 sends a command by launching `YouTubeControl.exe <action>`.
2. New process checks mutex.
3. If leader exists, instance runs Messenger mode and writes to named pipe.
4. Leader pipe loop receives command and dispatches action via CDP.
5. Messenger exits in milliseconds, avoiding visible console flicker.

---

## 4. Supported Action Vocabulary

Implemented action set:
- `home`
- `up`
- `down`
- `enter`
- `back`
- `play_pause`
- `fullscreen`
- `toggle`
- `like`
- `search:<query>`
- `open:<url>`
- `refresh`
- `exit`
- `stop` (mapped to `exit`)

---

## 5. In-Process Ad-Skipper Design

Loop behavior:
- Runs as background task inside Leader mode
- Poll interval: `1500ms`
- Checks active browser pages for YouTube `watch` / `shorts` URLs
- Executes skip/close click only when visible selector is found
- Uses cancellation token for clean shutdown on `exit`/`stop`
- Remains silent when no action is needed to reduce log noise

---

## 6. Connectivity and Recovery

- CDP endpoint: `http://127.0.0.1:15432`
- Leader attempts attach-first and launches Chrome only when required
- Recovery loop keeps trying to reattach when browser session drops
- Transient session failures are retried for command dispatch

---

## 7. Shutdown Semantics

On `exit` / `stop`:
- Leader marks exit requested
- Browser tabs and window are closed
- Browser handle is invalidated
- Leader raises shutdown event
- Program cancellation token is cancelled
- Leader loops stop gracefully

---

## 8. Ports and IPC

| Channel | Value | Purpose |
|---|---|---|
| CDP | `15432` | Chrome remote debugging |
| Named Pipe | `YouTubeControlPipe` | Messenger -> Leader command relay |
| HTTP command port | Not used in V7 | V6-only path removed from runtime |

---

## 9. V7 Repository Structure (Active)

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
```

---

## 10. Legacy V6 Components (Replaced in Runtime)

The following runtime roles were replaced by V7:
- `send.vbs` command bridge -> Messenger mode
- `nav.exe` HTTP server -> Leader pipe server
- `skip_ads.exe` external process -> `AdSkipperTask` in-process loop
- `Setup_System.bat` orchestration -> startup/lifecycle handled in C#

Legacy files may still exist in the repo for migration reference, but they are no longer part of the active V7 runtime design.
