# Architecture — Grid3-YouTube-Accessibility-Addon V6

This document describes the system architecture, component responsibilities, data flow, and how all parts work together.

---

## System Overview

The system connects **Grid 3** (AAC software for eye-tracking / switch-access users) with **YouTube running inside Chrome Canary**. When the student opens the YouTube grid set, Grid 3 automatically runs `Setup_System.bat`, which starts all three processes. Cell presses in Grid 3 send HTTP commands to `nav.exe`, which controls the browser via CDP. `skip_ads.exe` runs alongside it, independently monitoring Chrome Canary and clicking Skip Ad the moment it appears.

```
                       On grid set open
+-------------+ -----------------------------> +--------------------+
|   Grid 3    |                                |  Setup_System.bat  |
| (YouTube    |                                |  Starts:           |
|  grid set)  |                                |  [1] Chrome Canary |
+-------------+                                |  [2] nav.exe       |
      |                                        |  [3] skip_ads.exe  |
      | Cell press: wscript.exe + send.vbs     +--------------------+
      v
+-----------+  HTTP GET :3000  +-----------+   CDP :15432   +----------------+
|  send.vbs | ---------------> |  nav.exe  | <----------->  | Chrome Canary  |
+-----------+                  | port 3000 |  Controls JS   | YouTube.com    |
                               +-----------+  in browser    | port 15432     |
                                     |                      +----------------+
                                     | kills on exit               ^ |
                                     v                    CDP :15432 | | CDP :15432
                               +-----------+  ---------------------> | | Polls page,
                               | skip_ads  | <--------------------- -+ v clicks
                               |   .exe    |                           Skip Ad
                               |(background|                           button
                               | process)  |
                               +-----------+
```

---

## Component Descriptions

### 1. `nav.exe` — Central Navigator Server

**Source:** `src/youtube_navigator.js`  
**Port:** Listens on `http://localhost:3000`  
**Built with:**
```bash
cd src
npx pkg youtube_navigator.js --target node18-win-x64 --output nav.exe --public
```

**Responsibilities:**
- Starts an HTTP server that accepts action commands from `send.vbs`.
- Connects to Chrome Canary via CDP on port `15432`.
- Executes JavaScript in the YouTube page to navigate, scroll, highlight items, search, like, and toggle fullscreen.
- Automatically ensures `skip_ads.exe` is running in the background.
- Handles graceful system shutdown when the `exit` command is received.
- Decodes UTF-8 encoded URLs to support searches in both **English and Hebrew**.

**Key functions in `youtube_navigator.js`:**

| Function | Description |
|---|---|
| `ensureSkipperRunning()` | Checks `tasklist` for `skip_ads.exe`; starts it if not running |
| `navigateYouTube(action, query)` | Main dispatcher: connects to CDP, runs JS in the page, handles all navigation logic |
| `delay(ms)` | Promise-based pause utility used between page loads and interactions |
| HTTP Server `createServer` | Listens on port 3000, decodes the URL path to extract action and query |

**Supported actions:**

| Action | Behavior |
|---|---|
| `home` | Navigate to `https://www.youtube.com` and reset focus |
| `up` | Move red highlight to the previous item |
| `down` | Move red highlight to the next item |
| `enter` | Click the currently highlighted item |
| `back` | `window.history.back()` |
| `play_pause` | Toggle video play/pause (works on both video pages and Shorts) |
| `fullscreen` | Simulate pressing `F` to toggle fullscreen |
| `like` | Click the Like button (works on both video pages and Shorts) |
| `search:<query>` | Navigate to YouTube search results for the given query |
| `open:<url>` | Navigate directly to a specific YouTube URL |
| `exit` | Close the browser gracefully and kill `skip_ads.exe` |

---

### 2. `skip_ads.exe` — Background Ad-Skipper

**Source:** `src/skip_ads_cdp_V6.js`  
**Built with:**
```bash
cd src
npx pkg skip_ads_cdp_V6.js --target node18-win-x64 --output skip_ads.exe --public
```

**Responsibilities:**
- Started by `Setup_System.bat` alongside `nav.exe` when the grid set opens — runs as a hidden background process.
- Runs **in parallel with `nav.exe`**, not under it: both independently connect to Chrome Canary via CDP.
- Connects directly to Chrome Canary on port `15432` — it listens to the browser and acts on it; Chrome does not call it.
- Continuously polls the active YouTube tab for a visible "Skip Ad" button.
- Clicks the button the instant it appears, preventing any delay or need for user interaction.
- Stopped by `nav.exe` when the `exit` command is received.
- `nav.exe` also includes `ensureSkipperRunning()` as a safety net in case `skip_ads.exe` was not yet running.
- Writes activity to `debug_log_skipper.txt` for diagnostics.
- Avoids causing Play/Pause stutter during ads (stabilized in V6.9.2).

**Key functions in `skip_ads_cdp_V6.js`:**

| Function | Description |
|---|---|
| `logger(message, forceConsole)` | Deduplicating logger: writes to file, optionally to console |
| `CHROME_CONFIG` | CDP connection config (`127.0.0.1:15432`) |

---

### 3. `send.vbs` — Grid 3 Command Bridge

**Source:** `src/send.vbs`  

**Responsibilities:**
- Called by Grid 3 via `wscript.exe` with a single argument (the action name).
- Encodes the action string as a UTF-8 URL (supports Hebrew search queries).
- Sends an HTTP GET request to `http://localhost:3000/<encoded_action>`.
- Exits silently — no window, no console output.

**Key functions in `send.vbs`:**

| Function | Description |
|---|---|
| `UTF8EncodeForUrl(sStr)` | Encodes any string (including Hebrew) to a percent-encoded UTF-8 URL segment |
| Main body | Reads `WScript.Arguments(0)`, encodes it, fires an HTTP GET via `MSXML2.XMLHTTP` |

**How Grid 3 calls it:**
- **Application/File:** `wscript.exe`
- **Arguments:** `"C:\YouTube_Navigator_V6\send.vbs" <action>`

---

### 4. `Setup_System.bat` — System Launcher

**Source:** `src/Setup_System.bat`  

**Responsibilities:**
- Acts as the master launcher for the entire system.
- Kills any leftover `nav.exe`, `skip_ads.exe`, and `chrome.exe` processes from previous sessions.
- Verifies Chrome Canary is installed; shows an error dialog if not.
- Launches Chrome Canary with `--remote-debugging-port=15432` and a dedicated user data directory.
- Starts `nav.exe` in a minimized window.
- Waits briefly for Chrome and `nav.exe` to initialize, then sends a `home` command via `send.vbs`.

---

### 5. `inno_setup_v6.iss` — Installer Script

**Source:** `src/inno_setup_v6.iss`  
**Tool:** [Inno Setup Compiler](https://jrsoftware.org/isinfo.php)  
**Output:** `Output/YouTube_V6_Full_Installer.exe`

**Responsibilities:**
- Defines all files to bundle (`nav.exe`, `skip_ads.exe`, `Setup_System.bat`, `send.vbs`, icon).
- Copies `send.vbs` to both the app directory and the Windows system directory for universal accessibility.
- Creates required directories with full user permissions.
- Adds a desktop shortcut to launch the system.
- Adds a Start Menu shortcut to exit the system.
- Configures Windows Defender exclusions on install to prevent false positives.

---

## Data Flow Example: User Presses "Search for Disney"

1. Grid 3 cell triggers: `wscript.exe "C:\YouTube_Navigator_V6\send.vbs" "search:disney"`
2. `send.vbs` encodes `search:disney` → `search%3Adisney`, sends `GET http://localhost:3000/search%3Adisney`
3. `nav.exe` receives the request, decodes to `action=search`, `query=disney`
4. `nav.exe` connects to Chrome Canary CDP, navigates to `https://www.youtube.com/results?search_query=disney`
5. After page load, `nav.exe` injects JS to highlight the first search result with a red outline
6. User presses "Down/Up/Enter" cells in Grid 3 to browse and select a video

> **Note:** YouTube search supports both English and Hebrew queries. Hebrew text is UTF-8 encoded by `send.vbs` before being sent to the server.

---

## Port Reference

| Port | Used By | Purpose |
|---|---|---|
| `15432` | Chrome Canary | Remote debugging / CDP endpoint |
| `3000` | `nav.exe` | Receives commands from `send.vbs` |

---

## File Structure

```
src/
├── youtube_navigator.js   → Source for nav.exe (HTTP server + CDP controller)
├── skip_ads_cdp_V6.js     → Source for skip_ads.exe (background ad-skipper)
├── send.vbs               → Grid 3 bridge script (VBScript HTTP client)
├── Setup_System.bat       → System launcher (process manager + Chrome starter)
├── inno_setup_v6.iss      → Inno Setup installer script
└── debug_log_skipper.txt  → Runtime log from skip_ads.exe

Output/
└── YouTube_V6_Full_Installer.exe  → Ready-to-install executable (built by Inno Setup)
```
