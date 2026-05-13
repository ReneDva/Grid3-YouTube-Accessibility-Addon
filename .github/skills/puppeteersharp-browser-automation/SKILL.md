---
name: puppeteersharp-browser-automation
description: "Implement resilient PuppeteerSharp automation with attach-first launch strategy, dynamic Chrome profile binding, and reconnect recovery loops. Use for CDP lifecycle reliability."
---

# Skill 02 — PuppeteerSharp Browser Automation

**Project:** Grid3-YouTube-Accessibility-Addon V7  
**File scope:** `src/YouTubeControl/ChromeManager.cs`  
**Goal:** Reliably control Chrome without connection failures — distinguish between launching a new instance and attaching to an existing one, bind the correct profile dynamically, and recover automatically from WebSocket drops and page crashes.

---

## 1. Launch and Connect Strategy

**What to implement:**  
A two-step strategy that first tries to attach to a running Chrome, and only launches a new one if that fails.

**Attach path:**  
Call `Puppeteer.ConnectAsync(new ConnectOptions { BrowserURL = "http://127.0.0.1:<debugPort>" })`.  
This connects to Chrome via its DevTools HTTP endpoint without spawning a new process, preserving the user's open tabs and session.  
If it throws (Chrome not running), catch silently and fall through to the launch path.

**Launch path:**  
Call `Puppeteer.LaunchAsync(new LaunchOptions { ExecutablePath = ..., Args = [...] })`.  
This starts a fresh Chrome process with the exact flags required for this session.

**Decision order:**  
Always try Attach first. Only Launch if Attach fails. This prevents opening duplicate Chrome windows when the user restarts the addon mid-session.

---

## 2. Profile Binding

**What to implement:**  
Inject `--profile-directory` and `--user-data-dir` flags dynamically from `config.json` — never hardcode them.

**Rules:**

- Read both values from `AppConfig` at runtime. The values are written by Skill 04 (Chrome Profile Management) during first-run setup.
- Always include `--user-data-dir=<userDataDir>` so Chrome uses the correct data directory.
- Only add `--profile-directory=<profileDirectory>` if the value in config is non-empty — omitting it on first run lets Chrome show its own Profile Picker.
- Include these standard stability flags alongside the profile flags:
  - `--no-first-run`
  - `--no-default-browser-check`
  - `--autoplay-policy=no-user-gesture-required`
  - `--disable-session-crashed-bubble`

---

## 3. Resilience Patterns

### Exponential Backoff

Wrap all `ConnectAsync` / `LaunchAsync` calls in a retry loop with exponential backoff.

**Pattern:**
```
attempt 0 -> wait  500 ms on failure
attempt 1 -> wait 1000 ms on failure
attempt 2 -> wait 2000 ms on failure
attempt 3 -> wait 4000 ms on failure
attempt 4 -> give up, return null
```

Use `Math.Pow(2, attemptIndex) * baseDelayMs` to compute the delay.  
Always check `CancellationToken.IsCancellationRequested` before each retry — if the Leader is shutting down, stop immediately.

### Target Disconnected detection

`TargetClosedException` is thrown when the page you hold a reference to has been navigated away from, refreshed, or closed by the user.

**Handling:**

- In `GetYouTubePageAsync`: catch `TargetClosedException`, set `_browser = null`, and return `null`. The caller will retry on the next command.
- In `AdSkipperTask.PollPageAsync` (inner loop): catch `TargetClosedException`, log "Tab disconnected", break out of the inner polling loop, and let the outer loop find a new page.

### Reconnect recovery loop

Run a background `Task` in the Leader that checks `_browser.IsConnected` every 3 seconds.  
If disconnected (Chrome was closed and reopened), call `GetBrowserWithRetryAsync()` again.  
After reconnecting, pass the new `IBrowser` reference to `AdSkipperTask.UpdateBrowser()` so the ad-skipper also uses the fresh connection.

---

## Checklist for the agent

- [ ] `TryAttachAsync` is attempted before `LaunchAsync`
- [ ] `TryAttachAsync` failure is caught silently, not re-thrown
- [ ] `--profile-directory` comes from `config.json`, not hardcoded
- [ ] `--user-data-dir` comes from `config.json`, not hardcoded
- [ ] `--profile-directory` is only added when the config value is non-empty
- [ ] Retry loop uses `BaseDelayMs * 2^i` (Exponential Backoff)
- [ ] `CancellationToken` is checked before every retry delay
- [ ] `TargetClosedException` in `GetYouTubePageAsync` sets `_browser = null` and returns `null`
- [ ] `TargetClosedException` in the ad-skipper inner loop breaks to the outer reconnect loop
- [ ] Recovery loop runs every 3 seconds and calls `UpdateBrowser()` after reconnect
