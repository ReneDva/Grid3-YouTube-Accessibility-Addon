# System State (Current, src)

This document captures the currently implemented runtime state for the code under src, based on the V7 C# implementation.

## Scope

- Repository area covered: src/YouTubeControl
- Runtime style: single WinExe running in dual mode (Leader or Messenger)
- IPC: local named pipe (YouTubeControlPipe)
- Leader election: global mutex (Global\\YouTubeControl_Leader_Mutex)
- Browser control: Chrome DevTools Protocol via PuppeteerSharp on port 15432

## Main Runtime Components

- Program.cs
- Process startup, global exception handlers, leader election, and shutdown token wiring.
- If mutex is not acquired, process acts as Messenger and forwards command to Leader.
- If mutex is acquired, process starts LeaderMode and stays resident.

- MessengerMode.cs
- Combines CLI args into one command line.
- Sends one line over named pipe and exits quickly.

- LeaderMode.cs
- Owns the active browser session and command dispatch pipeline.
- Runs three concurrent loops:
  - Pipe server loop (receive and dispatch commands)
  - CDP recovery loop (reattach when session drops)
  - Ad skipper loop (polls every 1500 ms)
- Supports actions:
  - home, up, down, enter, back, play_pause, fullscreen, toggle, like, search, open, refresh, exit, stop
- stop is normalized to exit.

- ChromeManager.cs
- Resolves Chrome binary path (Canary fallback to Stable).
- Uses UserDataDirectoryPolicy to resolve the runtime profile directory.
- Launches Chrome with remote debugging port 15432 and a canonical user data directory:
  - C:\\YouTube_User_Data
- Restores foreground window after launch using retry + stability checks and a final-pass fallback.

- UserDataDirectoryPolicy.cs
- Selects preferred profile directory with this order:
  - Use canonical path directly when existing profile data is found.
  - Migrate legacy profile from `%LOCALAPPDATA%\YouTubeControl` if it exists.
  - Bootstrap first install at C:\\YouTube_User_Data when no profile data exists.

- Actions/NavigationActions.cs
- Provides the browser-side JavaScript script used for navigation, focus highlighting, media controls, and activation.

- Actions/AdSkipperTask.cs
- Executes browser-side selector checks for skip/close-ad targets.
- Clicks the target when found on watch/shorts pages.

- Logger.cs
- Thread-safe file logger with fallback log file behavior.

- Models/AppConfig.cs
- Exists and can load config.json defaults.
- Current runtime state: not referenced by startup/ChromeManager flow in the active code path.

## Current Command Lifecycle

1. Grid 3 (or any caller) starts YouTubeControl.exe with or without args.
2. Program attempts mutex acquisition.
3. If leader already exists, process runs MessengerMode and sends command to named pipe.
4. If Leader needs a browser launch, it resolves the user-data path via UserDataDirectoryPolicy, launches Chrome, then runs foreground-restore verification.
5. Leader receives command, validates action, resolves page, executes command via CDP.
6. Messenger exits immediately; Leader continues resident until exit/stop or shutdown.

## Mermaid Flowchart

```mermaid
flowchart TD
  A["Process Start: Program.Main(args)"] --> B{"Acquire Global Mutex?"}

  B -- "No: leader exists" --> C["MessengerMode.BuildCommand"]
  C --> D["NamedPipeClientStream to YouTubeControlPipe"]
  D --> E["Messenger exits"]

  B -- "Yes: this process is leader" --> F["Init logging and global exception handlers"]
  F --> G["Create CancellationTokenSource"]
  G --> H["LeaderMode.RunAsync"]

  H --> I["EnsureBrowserConnectedAsync"]
  I --> J{"CDP attached?"}
  J -- "Yes" --> K["Start 3 parallel leader loops"]
  J -- "No, launch allowed" --> L["UserDataDirectoryPolicy.Resolve + ChromeManager.Launch"]
  L --> L2["Foreground restore retries + stability verification"]
  L2 --> I
  J -- "No, launch not allowed" --> M["Command cannot execute"]

  K --> N["Pipe Server Loop"]
  K --> O["CDP Recovery Loop"]
  K --> P["Ad Skipper Loop (1500ms)"]

  N --> Q["DispatchCommandAsync"]
  Q --> R{"Action type"}

  R -- "home" --> H1["Go to YouTube home"]
  H1 --> H2["Run open-focus behavior"]
  H2 --> FX1["Red navigation frame on first item"]

  R -- "search:query" --> S1["Open search results URL"]
  S1 --> S2["Focus reset"]
  S2 --> FX1

  R -- "open:url" --> O1["Open explicit URL"]
  O1 --> O2["Focus reset"]
  O2 --> FX1

  R -- "back" --> B1["Browser go-back"]
  B1 --> B2["Focus reset"]
  B2 --> FX1

  R -- "down (next)" --> D1["Move navIndex +1"]
  D1 --> D2["Red frame moves to next item"]

  R -- "up (prev)" --> U1["Move navIndex -1"]
  U1 --> U2["Red frame moves to previous item"]

  R -- "enter (choose current video)" --> E1["Click focused link"]
  E1 --> E2["Navigate to selected video/page"]

  R -- "play_pause" --> P1["Toggle video or Shorts playback"]

  R -- "like" --> L1["Click like button when found"]

  R -- "fullscreen or toggle" --> F1["Toggle fullscreen via trusted keyboard path"]

  R -- "refresh" --> R1["Reload active YouTube page"]

  R -- "exit or stop" --> V["CloseBrowserAsync"]
  V --> W["Raise ShutdownRequested"]
  W --> X["Cancel token, stop loops, release mutex, leader exits"]

  P --> Y["AdSkipperTask.TrySkipAsync"]
  Y --> Z{"Skip target found?"}
  Z -- "Yes" --> AA["Mouse click on skip or close-ad target"]
  Z -- "No" --> AB["No-op and continue polling"]
```

## User Actions and Visible Effects

- home: opens YouTube home and applies focus reset with red navigation frame on the first navigable item.
- search:query: opens search results and resets focus with red navigation frame.
- open:url: opens the given URL and resets focus with red navigation frame when a navigable list is available.
- back: browser back navigation and focus reset with red navigation frame.
- down (next): moves to next item in list navigation.
- up (prev): moves to previous item in list navigation.
- enter (choose current video): clicks the currently focused item (thumbnail/title link).
- play_pause: toggles play/pause in standard video or Shorts context.
- like: clicks like action when selector is found.
- fullscreen and toggle: toggles fullscreen state.
- refresh: reloads active YouTube page.
- exit and stop: closes browser and requests leader shutdown.

Navigation frame details:
- Implemented by browser-side highlight styling in NavigationActions (8px red outline).
- Frame is reset/cleared and reapplied on focus-changing actions.

## Operational Notes (Current State)

- Leader is single-instance by mutex; Messenger is intentionally short-lived.
- Named pipe is the only command transport (no runtime HTTP command server in V7).
- Profile policy uses canonical path C:\\YouTube_User_Data with legacy migration support.
- Browser reconnect/retry logic is present for transient CDP session failures.
- Foreground restore is launch-time hardened with retries, stability checks, and final-pass attempt logging.
- Exit flow closes tabs/window and requests graceful shutdown.
- Logs are written to logs.txt in the app base directory, with temp fallback if needed.

## End-to-end flow: Grid3 click → V8 injection (diagram and locations)

Below is the updated command/runtime map with explicit execution boundaries between the LeaderMode host process and the Browser/V8 environment reached through CDP.

```mermaid
flowchart TD
  %% Trigger and IPC ingress
  subgraph Grid3Space["Grid3 (external app)"]
    G["User switch press"]
  end

  subgraph IPCPath["Windows IPC path"]
    M["MessengerMode builds command"]
    P["Named Pipe\nYouTubeControlPipe"]
  end

  %% Host-side execution (C#)
  subgraph Host["LeaderMode host application (.NET / C#)"]
    L0["Leader pipe read"]
    L1["TryParseCommand(raw)"]
    L2["DispatchCommandAsync(action, query)"]
    L3["GetYouTubePageAsync()"]
    L4["TryBringToFrontAsync(page)"]
    LR{"Action category?"}

    A0["Category A\nup/down/enter"]
    B0["Category B\nhome/search/back/open/refresh"]
    C0["Category C\nplay_pause/like"]
    D0["Category D\nfullscreen/exit"]

    B1["GoToAsync / ReloadAsync\n(back uses history navigation)"]
    D1["Keyboard.PressAsync or CloseAsync"]
    Log["Logger.Log result"]

    AD0["Ad skipper loop\n(background, no Grid3 trigger)"]
    AD1["Resolve skippable YouTube page"]
  end

  %% CDP boundary and browser-side execution
  CDP["CDP transport boundary"]

  subgraph Browser["Chrome page runtime (Renderer / V8)"]
    Eval["EvaluateExpressionAsync(...)\n(script runs in page context)"]
    DOM["DOM interaction / navigation state"]
    NavIdx["[MEM-JS] window.navIndex\nvolatile in-page state"]
    NativeClick["Mouse.ClickAsync(x,y)"]
  end

  %% Persistent state
  Store[("[STORE] C:\\YouTube_User_Data\nChrome profile on disk")]
  BrowserMem["[MEM-C#] _browser (IBrowser)\nLeader process memory"]

  %% Shared flow
  G --> M --> P --> L0 --> L1 --> L2 --> L3 --> L4 --> LR
  L3 -. "uses" .-> BrowserMem
  BrowserMem -. "session via" .-> CDP
  CDP -. "connect/attach" .-> Browser

  %% Category branches
  LR --> A0 --> Eval
  LR --> B0 --> B1 --> Eval
  LR --> C0 --> Eval
  LR --> D0 --> D1 --> Log

  Eval --> DOM --> NavIdx
  Eval --> Log

  %% Background ad-skip flow
  AD0 --> AD1 --> Eval
  AD1 --> NativeClick --> Log

  %% Storage relation
  Browser -. "profile read/write" .-> Store

  %% Styling
  classDef hostNode fill:#eef6ff,stroke:#1a73e8,stroke-width:1px
  classDef runtimeNode fill:#e6fff2,stroke:#1e7f4f,stroke-width:1px
  classDef memNode fill:#fff3cd,stroke:#8a6d3b,stroke-width:2px,stroke-dasharray: 4 2
  classDef storeNode fill:#e8f4ff,stroke:#005a9e,stroke-width:2px

  class L0,L1,L2,L3,L4,LR,A0,B0,C0,D0,B1,D1,Log,AD0,AD1 hostNode
  class Eval,DOM,NavIdx,NativeClick runtimeNode
  class BrowserMem,NavIdx memNode
  class Store storeNode
```

Location map and execution boundaries:
- Shared flow for all actions:
  - Grid3 trigger enters YouTubeControl through `MessengerMode` and `YouTubeControlPipe` (named pipe transport).
  - Parsing and routing occur in `LeaderMode.cs` via `TryParseCommand(...)` then `DispatchCommandAsync(...)`.
  - Active-tab resolution and focusing occur in `LeaderMode.cs` via `GetYouTubePageAsync(...)` and `TryBringToFrontAsync(...)`.
  - Command outcomes are logged from the host (`Logger.Log(...)`) after each execution path.
- Category A: Stateful DOM navigation (`up`, `down`, `enter`):
  - Host builds route and invokes `page.EvaluateExpressionAsync<string>(...)`.
  - Script executes inside V8/page context and updates DOM state (including `window.navIndex`).
- Category B: URL navigation (`home`, `search`, `back`, `open`, `refresh`):
  - Host-side navigation uses `page.GoToAsync(...)` and `page.ReloadAsync()`.
  - `back` uses history navigation (`page.GoBackAsync()`), then action script continues through `EvaluateExpressionAsync(...)` for focus/normalization behavior.
  - Resulting script/DOM work still runs inside V8.
- Category C: Stateless page actions (`play_pause`, `like`):
  - Host invokes `EvaluateExpressionAsync(...)` only.
  - Player/element interaction executes entirely in the V8 page context.
- Category D: System-level actions (`fullscreen`, `exit`):
  - `fullscreen` uses browser-level input through `page.Keyboard.PressAsync(...)` (with state check around fullscreen state).
  - `exit` closes page/browser via `page.CloseAsync()` and `browser.CloseAsync()`.
  - These are host-initiated browser control operations, not DOM-nav script paths.
- Background automated action (no Grid3 trigger):
  - Ad skip loop runs independently from command intake.
  - `AdSkipperTask.TrySkipAsync(...)` uses `EvaluateExpressionAsync(...)` to detect a skip/close target and `page.Mouse.ClickAsync(...)` to perform the click.
- State locations:
  - `[MEM-C#] _browser` is in Leader process memory.
  - `[MEM-JS] window.navIndex` is in renderer/V8 memory.
  - `[STORE] C:\YouTube_User_Data` is persistent Chrome profile storage on disk.


