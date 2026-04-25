# Skill 03 — Stealth Background Execution

**Project:** Grid3-YouTube-Accessibility-Addon V7  
**File scope:** `src/YouTubeControl/YouTubeControl.csproj`, `src/YouTubeControl/Program.cs`, `src/YouTubeControl/LeaderMode.cs`  
**Goal:** The Leader process must be completely invisible — no black window, no taskbar entry, no system tray icon in normal mode — and must recover from errors silently without ever blocking an eye-gaze user with a popup.

---

## 1. WinExe Lifecycle

**What to set in the `.csproj`:**

```xml
<OutputType>WinExe</OutputType>
<UseWindowsForms>true</UseWindowsForms>
```

`WinExe` tells the linker not to allocate a console subsystem. The result: no black terminal window flashes when Grid 3 launches either the Leader or the Messenger.  
`Console` output type must **never** be used — it would create a visible black window on every Grid 3 button press.

**Why WinForms (not WPF or a bare Win32 app):**  
`System.Windows.Forms.Application.Run(ApplicationContext)` provides a Windows message loop required for IPC pump-and-wait semantics, without requiring any visible Form.

---

## 2. Headless Startup

**What to implement:**  
Run the Leader as a `ApplicationContext` subclass with no `MainForm` assigned.

**Pattern:**
```
Application.Run(new LeaderMode(config));   // no Form argument
```

`LeaderMode` inherits from `ApplicationContext`. Because `MainForm` is never set, the message loop runs indefinitely with zero UI. The process stays alive purely to serve the Named Pipe and maintain the CDP connection.

**What NOT to do:**

- Do not call `Application.Run(new SomeForm())` — this shows a window.
- Do not call `Application.Run()` with no argument if you need to be able to call `ExitThread()` from within the context.
- Do not use `Thread.Sleep` in a loop as a substitute — it blocks and prevents Windows messages from being processed.

**Tray icon:**  
Normal operation requires **no** tray icon. The Leader is fully invisible. A tray icon is only acceptable as a debug aid during development, and must be removed before release.

---

## 3. Quiet Recovery

**What to implement:**  
Two layers of error interception that ensure a crash or unhandled exception never surfaces a Windows error dialog.

### Layer 1 — WinForms thread exceptions

```csharp
Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
Application.ThreadException += (_, e) => HandleCriticalError(e.Exception);
```

Must be set **before** `Application.Run()`.

### Layer 2 — AppDomain unhandled exceptions

```csharp
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    HandleCriticalError(e.ExceptionObject as Exception ?? new Exception("Unknown"));
```

### `HandleCriticalError` behaviour

1. Write to `error_log.txt` (append, timestamp, exception type + message).
2. Call `Application.Exit()`.
3. **Never** call `MessageBox.Show()`. Never re-throw. Never let the OS show its own "application has stopped working" dialog.

### File-write failure

The log write itself must be wrapped in its own `try/catch` with an empty catch body. If the disk is full or the file is locked, the process must still exit cleanly rather than crashing inside the error handler.

### Restart vs. exit

For a critical startup failure (e.g., Chrome not found), the correct behavior is:

- Log the error.
- Exit silently.

Do **not** attempt an automatic restart loop for startup failures — that would spin indefinitely if the root cause (missing Chrome) is not fixed. Auto-restart is only appropriate inside the CDP reconnect loop for transient disconnects.

---

## Checklist for the agent

- [ ] `.csproj` has `<OutputType>WinExe</OutputType>`
- [ ] `.csproj` has `<UseWindowsForms>true</UseWindowsForms>`
- [ ] `Application.Run()` receives an `ApplicationContext` instance, not a `Form`
- [ ] `LeaderMode` inherits `ApplicationContext` and never sets `MainForm`
- [ ] `Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)` is called before `Application.Run()`
- [ ] `Application.ThreadException` handler is registered before `Application.Run()`
- [ ] `AppDomain.CurrentDomain.UnhandledException` handler is registered before `Application.Run()`
- [ ] `HandleCriticalError` writes to `error_log.txt` and calls `Application.Exit()`
- [ ] `HandleCriticalError` never calls `MessageBox.Show()`
- [ ] Log file write is wrapped in its own `try/catch` with empty body
- [ ] No `Console.WriteLine` calls remain in the release build
