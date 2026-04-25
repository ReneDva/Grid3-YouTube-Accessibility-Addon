# Architecture Design: C# Background Relay & Profile Manager
**Project:** Grid3-YouTube-Accessibility-Addon (Upgrade to V7)  
**Objective:** Replace VBScript and Node.js with a single-instance C# (.NET) Wrapper to eliminate visual flicker and simplify user profile management.

---

## 1. System Overview
The new architecture shifts from a script-based bridge to a **Resident Background Application**. Instead of launching a new environment (Node.js) and a script (VBScript) for every action, a single C# executable will manage the entire lifecycle of the accessibility tool.

### Core Components:
* **The Leader (Background Instance):** The primary process that maintains the CDP connection to Chrome and listens for commands.
* **The Messenger (Short-lived Instance):** Triggered by Grid 3, it sends arguments to the Leader and exits immediately.
* **IPC (Inter-Process Communication):** Named Pipes will be used for near-zero latency communication between instances.
* **Profile Manager:** A first-run logic to handle Chrome profile selection.

---

## 2. The New Workflow

### Phase A: Installation & First Run (The "Smart Setup")
1.  On the very first execution after installation, the C# Wrapper detects no `config.json`.
2.  **Profile Selection:** The Wrapper launches Chrome Canary in a generic "Profile Picker" mode or opens the Settings page.
3.  **User Interaction:** The installer (parent/therapist) selects or logs into the desired Google/YouTube account.
4.  **Automatic Detection:** The Wrapper identifies the selected `Profile Directory` name and saves it to a local configuration file.
5.  **Initialization:** Chrome is restarted with the specific profile and the `--remote-debugging-port=15432` flag.

### Phase B: Daily Use (The Resident Mode)
1.  **Grid 3 Startup:** When the YouTube grid opens, it launches `YouTubeControl.exe`.
2.  **Leader Election:** The process checks if another instance is running. Since it's the first, it becomes the **Leader**.
3.  **Silent Operation:** The Leader runs with no window (`WindowStyle.Hidden`). It opens Chrome and connects to the CDP.

### Phase C: Executing Commands (The Relay)
1.  **Grid 3 Action:** The user presses a button (e.g., "Search"). Grid 3 runs:
    `YouTubeControl.exe "search:disney"`
2.  **Relay Logic:** This new instance detects the **Leader**. Instead of opening Chrome, it sends the string `"search:disney"` through a **Named Pipe**.
3.  **Instant Exit:** The second instance closes in milliseconds. **No black window or flicker appears.**
4.  **Execution:** The Leader receives the pipe message and injects the JavaScript into Chrome via the active CDP connection.

---

## 3. Technical Specifications

### Inter-Process Communication (IPC)
* **Technology:** `System.IO.Pipes` (Named Pipes).
* **Reason:** It is a native Windows mechanism, faster than HTTP, and does not require opening firewall ports.

### Chrome Management
* **Startup Flags:**
    * `--remote-debugging-port=15432`
    * `--profile-directory="{Saved_Profile_Name}"`
    * `--user-data-dir="{Standard_Chrome_Path}"`
* **Navigation:** Initial launch will navigate directly to `https://www.youtube.com`.

### Stealth & UX
* **Application Type:** Windows Forms or WPF (but started without a Main Window).
* **Visuals:** * **Normal Mode:** Invisible.
    * **Setup Mode:** A simple, high-contrast GUI for the installer to select the profile.

---

## 4. Grid 3 Integration (Command Structure)
The integration within Grid 3 becomes much simpler. You no longer need `wscript.exe`.

**Current (Old):**
* **Application:** `wscript.exe`
* **Arguments:** `"C:\YouTube_Navigator_V6\send.vbs" home`

**New (Upgraded):**
* **Application:** `C:\YouTube_Navigator_V7\YouTubeControl.exe`
* **Arguments:** `home` (or `up`, `down`, `search:query`)

---

## 5. Development Roadmap (Plan for Claude)
1.  **Project Setup:** Create a .NET 6/8 Console/WinForms app.
2.  **Single-Instance Logic:** Implement `Mutex` or `Process.GetProcessesByName` to detect existing instances.
3.  **Named Pipes Implementation:** Create the Pipe Server (Leader) and Pipe Client (Messenger).
4.  **Chrome Integration:** Port the CDP WebSocket logic to C# (using `PuppeteerSharp` or `System.Net.WebSockets`).
5.  **Profile Configuration UI:** Build the "First-Run" logic to capture the Chrome profile name.
6.  **Packaging:** Configure a "Single File" publish to generate one standalone EXE.

---

### Instructions for the AI (Claude):
* **Focus:** Maintain the English-only documentation constraint.
* **UI:** Keep the Setup UI extremely simple for non-technical users.
* **Stability:** Ensure the Leader process can gracefully recover the CDP connection if Chrome is closed and reopened.

---
