# Architecture Design: C# Background Relay & Profile Manager
**Project:** Grid3-YouTube-Accessibility-Addon (Upgrade to V7)  
**Objective:** Replace VBScript and Node.js with a single-instance C# (.NET) Wrapper to eliminate visual flicker and simplify user profile management.

---

## 1. System Overview

The new architecture shifts from a script-based bridge to a **Resident Background Application**. Instead of launching a new environment (Node.js) and a script (VBScript) for every action, a single C# executable manages the entire lifecycle of the accessibility tool.

### Core Components

| Component | Role |
|---|---|
| **The Leader** | Primary process — owns the CDP connection, listens for commands via Named Pipe |
| **The Messenger** | Short-lived instance — triggered by Grid 3, relays command to Leader, exits immediately |
| **IPC (Named Pipes)** | `System.IO.Pipes` — near-zero latency, no firewall ports |
| **Profile Manager** | First-run UI to capture and save the Chrome profile name |

### Architecture Diagram (V7)

```
                    On grid set open
+-------------+ -----------------------> +------------------------+
|   Grid 3    |                          |  YouTubeControl.exe    |
| (YouTube    |                          |  (Leader Mode)         |
|  grid set)  |                          |  - Launches Chrome     |
+-------------+                          |  - Owns CDP connection |
      |                                  |  - Runs ad-skip loop   |
      | Cell press:                      +------------------------+
      | YouTubeControl.exe "search:q"           ^
      v                                         | Named Pipe
+------------------------+                      |
| YouTubeControl.exe     | -------------------> |
| (Messenger Mode)       |  "search:q"
| Exits in milliseconds  |
+------------------------+
```

---

## 2. The New Workflow

### Phase A: Installation & First Run (The "Smart Setup")
1. On first execution, the Wrapper detects no `config.json` exists.
2. **Profile Selection:** Chrome Canary launches in generic mode (no profile flag) showing the Profile Picker.
3. **User Interaction:** The installer (parent/therapist) selects or logs into the desired Google/YouTube account.
4. **Automatic Detection:** The Wrapper identifies the most-recently-modified `Profile*` directory under Chrome's `User Data` folder.
5. **Save:** Profile name written to `config.json` alongside the EXE.
6. **Initialization:** Chrome restarts with the specific profile and `--remote-debugging-port=15432`.

### Phase B: Daily Use (The Resident Mode)
1. **Grid 3 Startup:** The YouTube grid opening runs `YouTubeControl.exe` (no arguments).
2. **Leader Election:** Process checks for the global named Mutex. Not held → becomes **Leader**.
3. **Silent Operation:** Leader runs with no visible window. Opens Chrome, connects to CDP, starts ad-skipper background task.

### Phase C: Executing Commands (The Relay)
1. **Grid 3 Action:** User presses a button (e.g., "Search"). Grid 3 runs: `YouTubeControl.exe "search:disney"`
2. **Relay Logic:** New instance detects the **Leader** holds the Mutex → becomes **Messenger**. Writes `"search:disney"` to the Named Pipe.
3. **Instant Exit:** Messenger closes in milliseconds. **No black window or flicker.**
4. **Execution:** Leader receives the pipe message and injects JavaScript into Chrome via the active CDP connection.

---

## 3. Structural Migration Report: V6 → V7

### 3.1 Components to RETAIN (Injectable JS Strings)

These blocks run *inside Chrome* via CDP injection. They are language-agnostic — port verbatim as `const string` literals in C#.

| JS Source | File & Lines | What to Retain |
|---|---|---|
| `navScript` | `youtube_navigator.js:122-215` | Entire block: `getItems()`, `highlight()`, `clearHighlights()`, `navIndex` tracking, Shorts navigation selectors, `play_pause` / `like` / `enter` handlers. All CSS selectors. |
| `browserSideScript` | `skip_ads_cdp_V6.js:59-110` | Entire block: `isVisible()` check, skip-button selector array, `getBoundingClientRect()` coordinate return. |
| Action vocabulary | `ARCHITECTURE.md` | All 10 action names: `home`, `up`, `down`, `enter`, `back`, `play_pause`, `fullscreen`, `like`, `search:q`, `open:url`, `exit` — identical in V7. |
| CDP port | `youtube_navigator.js:12` | `15432` — keep unchanged. |
| Skip-ads timing | `skip_ads_cdp_V6.js:164` | `1500ms` poll interval, `3000ms` reconnect delay — keep unchanged. |

---

### 3.2 Components to MODIFY (Adapt / Translate)

#### HTTP Server → Named Pipe Listener

| V6 (Node.js) | V7 (C#) | .NET Library |
|---|---|---|
| `http.createServer` on port `3000` | `NamedPipeServerStream("YouTubeControlPipe", PipeDirection.In)` | `System.IO.Pipes` |
| URL path parsing (`req.url`, `decodeURIComponent`) | `args[0]` command-line argument parsing in `Main(string[] args)` | `System` |
| `send.vbs` firing `XMLHTTP GET` | `NamedPipeClientStream` writes `args[0]` and exits | `System.IO.Pipes` |
| `wscript.exe send.vbs home` | `YouTubeControl.exe home` | — (Grid 3 direct) |

> **Note:** UTF-8 encoding is handled natively by .NET strings. The `UTF8EncodeForUrl()` function in `send.vbs` becomes unnecessary.

#### Chrome Launch (Logic Refactor)

| V6 | V7 | .NET Library |
|---|---|---|
| `Setup_System.bat` `start "" chrome.exe ...` | `Process.Start(new ProcessStartInfo { FileName = chromePath, Arguments = flags })` | `System.Diagnostics.Process` |
| Hardcoded `--user-data-dir=C:\YouTube_User_Data_V5` | `userDataDir` read from `config.json` | `System.Text.Json` |
| No `--profile-directory` flag | `--profile-directory="{savedProfile}"` added from `config.json` | `System.Diagnostics.Process` |

#### `ensureSkipperRunning()` → In-Process Ad-Skipper

| V6 | V7 | .NET Library |
|---|---|---|
| `exec('tasklist')` + `spawn('skip_ads.exe')` | Ad-skipper runs as a `Task` inside the Leader process — no second EXE | `System.Threading.Tasks` |
| `taskkill /F /IM skip_ads.exe` on `exit` | `CancellationToken` cancels the background task | `System.Threading` |

#### CDP Client Translation

| V6 (Node.js) | V7 (C#) | .NET Library |
|---|---|---|
| `require('chrome-remote-interface')` | `Puppeteer.ConnectAsync(...)` | `PuppeteerSharp` (NuGet) |
| `CDP.List(config)` | `browser.Targets()` | `PuppeteerSharp` |
| `CDP({ target })` | `target.CreateCDPSessionAsync()` | `PuppeteerSharp` |
| `Runtime.evaluate({ expression: navScript })` | `cdpSession.SendAsync("Runtime.evaluate", new { expression = navScript })` | `PuppeteerSharp` |
| `Page.navigate({ url })` | `page.GoToAsync(url)` | `PuppeteerSharp` |
| `Input.dispatchMouseEvent(...)` | `cdpSession.SendAsync("Input.dispatchMouseEvent", ...)` | `PuppeteerSharp` |
| `Browser.close()` | `browser.CloseAsync()` | `PuppeteerSharp` |

---

### 3.3 Components to ADD (New Infrastructure)

#### Single-Instance Manager (Leader Election)

- **Mechanism:** Global named Mutex `"YouTubeControl_Leader_Mutex"`.
- **Library:** `System.Threading.Mutex`

#### Named Pipe IPC

- **Pipe name:** `"YouTubeControlPipe"` (local, single-machine).
- **Library:** `System.IO.Pipes`

#### First-Run Profile UI (Visual Picker)

**Trigger:** `config.json` absent on Leader startup.

**Flow:**
1. **JSON Parsing:** Access `%LOCALAPPDATA%\Google\Chrome SxS\User Data\Local State` and parse `profile.info_cache` to retrieve `name`, `gaia_name`, and `avatar_icon`.
2. **UI Rendering:** Show a high-contrast WinForms window with a flow-layout of profile cards. Each card shows the Chrome profile name and avatar.
3. **Selection Logic:** On profile click, store both the technical folder key (for example `Profile 2`) and display name into `config.json`.
4. **Initialization:** Restart Chrome with the selected `--profile-directory` and `--remote-debugging-port=15432`.

**Libraries:** `System.Windows.Forms`, `System.Text.Json`, `System.IO`

#### Windowless Background Process

- **Project type:** WinForms (`.csproj` with `<OutputType>WinExe</OutputType>`).
- **Operation:** Call `Application.Run()` with no main form so the Leader stays alive without a visible window.

#### CDP Connection Recovery Loop

- **Mechanism:** `while (true)` loop with `WebSocketException` handling to auto-reconnect when Chrome is manually closed and reopened.
- **Libraries:** `PuppeteerSharp`, `System.Net.WebSockets`

---

## 4. Files Eliminated in V7

| V6 File | Reason Removed |
|---|---|
| `send.vbs` | Replaced by Messenger mode of `YouTubeControl.exe` |
| `Setup_System.bat` | Chrome launch and startup logic moves into Leader startup code |
| `skip_ads.exe` (separate binary) | Ad-skipper runs as an in-process background `Task` in the Leader |
| `nav.exe` (Node.js HTTP server) | Replaced by the Leader's Named Pipe server |

---

## 5. New File Structure (V7)

```
src/
└── YouTubeControl/
    ├── YouTubeControl.csproj              (.NET 8, WinExe, x64, single-file publish)
    ├── Program.cs                         (Main: Mutex election → Leader or Messenger)
    ├── LeaderMode.cs                      (Pipe server loop, CDP connection, ad-skipper task)
    ├── MessengerMode.cs                   (Pipe client: write args[0], exit)
    ├── ChromeManager.cs                   (Launch Chrome, read/write config.json, recovery loop)
    ├── ProfileSetupForm.cs                (First-run WinForms UI — profile detection)
    ├── Actions/
    │   ├── NavigationActions.cs           (navScript JS string + ExecuteAction dispatcher)
    │   └── AdSkipperTask.cs              (browserSideScript JS string + 1500ms polling loop)
    └── Models/
        └── AppConfig.cs                  (config.json schema: profileDirectory, userDataDir)

Output/
└── YouTubeControl.exe                     (single-file, self-contained, .NET 8 runtime)
```

---

## 6. Grid 3 Integration (V6 → V7 Mapping)

| | V6 | V7 |
|---|---|---|
| **Application** | `wscript.exe` | `C:\YouTube_Navigator_V7\YouTubeControl.exe` |
| **Arguments** | `"C:\...\send.vbs" search:disney` | `search:disney` |
| **Startup cell** | Runs `Setup_System.bat` | Runs `YouTubeControl.exe` (no args → Leader) |
| **Command cell** | Runs `wscript.exe send.vbs <action>` | Runs `YouTubeControl.exe <action>` |
| **Visible window** | Black cmd/wscript window flickers | No window — Messenger exits in milliseconds |

---

## 7. Technical Specifications

### Inter-Process Communication (IPC)
- **Technology:** `System.IO.Pipes` (Named Pipes)
- **Reason:** Native Windows mechanism, faster than HTTP, no firewall ports required

### Chrome Management
- **Startup flags:**
  - `--remote-debugging-port=15432`
  - `--profile-directory="{savedProfile}"`
  - `--user-data-dir="{userDataDir}"`
  - `--no-first-run`
  - `--no-default-browser-check`
  - `--autoplay-policy=no-user-gesture-required`
  - `--disable-session-crashed-bubble`
- **Initial navigation:** `https://www.youtube.com`

### Stealth & UX
- **Normal mode:** Completely invisible — no taskbar entry, no window, no tray icon
- **Setup mode:** Minimal high-contrast WinForms dialog for profile selection only

---

## 8. Verification Plan

| Step | Test | Expected Result |
|---|---|---|
| **Build** | `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` | Single `YouTubeControl.exe` generated |
| **Leader startup** | Run `YouTubeControl.exe` (no args) | Chrome opens, no window visible, YouTube loads |
| **Messenger relay** | Run `YouTubeControl.exe down` in a second terminal | No black window; red highlight moves down in Chrome |
| **First-run** | Delete `config.json`, run EXE | Profile Setup form appears; `config.json` written after "Done" |
| **Ad-skip** | Start a YouTube video with a pre-roll ad | Skip button clicked automatically within 1500ms |
| **Connection recovery** | Kill Chrome manually; reopen Chrome | Leader detects disconnect and reconnects |
| **Exit** | Run `YouTubeControl.exe exit` | Chrome closes cleanly; Leader process terminates |
| **Hebrew search** | Run `YouTubeControl.exe "search:דיסני"` | YouTube search results for "דיסני" load correctly |

---

## 9. Development Roadmap

1. **Project Setup:** Create `.NET 8` WinForms project; add `PuppeteerSharp` NuGet package.
2. **Single-Instance Logic:** Implement Mutex-based Leader/Messenger election in `Program.cs`.
3. **Named Pipe IPC:** Build `NamedPipeServerStream` listener (Leader) and `NamedPipeClientStream` writer (Messenger).
4. **Chrome Integration:** Port CDP logic to C# using `PuppeteerSharp`; migrate `navScript` and `browserSideScript` as string constants.
5. **In-Process Ad-Skipper:** Run `AdSkipperTask.cs` polling loop as a cancellable background `Task`.
6. **Profile Configuration UI:** Build `ProfileSetupForm.cs` with first-run detection, Chrome profile scan, and `config.json` write.
7. **Connection Recovery:** Wrap CDP calls in retry loop with `WebSocketException` handling.
8. **Packaging:** Configure single-file self-contained publish; update Grid 3 cell configurations.

---

### Notes for Implementation

- **English Only:** All code comments, logs, and documentation must be in English.
- **UI Simplicity:** The Profile Setup form must be usable by non-technical parents and therapists — minimal controls, large text, high contrast.
- **Stability:** The Leader process must survive Chrome restarts — connection recovery is non-optional.
- **No Port 3000:** The HTTP server and all references to port 3000 are fully removed in V7.
