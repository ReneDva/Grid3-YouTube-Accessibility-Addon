---
name: windows-native-integration
description: "Implement Windows native single-instance and IPC patterns for C# using Global Mutex and Named Pipes ACL/timeout handling. Use when building Leader/Messenger process coordination."
---

# Skill 01 — Windows Native Integration

**Project:** Grid3-YouTube-Accessibility-Addon V7  
**File scope:** `src/YouTubeControl/LeaderMode.cs`, `src/YouTubeControl/MessengerMode.cs`  
**Goal:** Ensure the application manages itself correctly inside the Windows OS — single-instance enforcement, low-latency IPC, and resilient pipe communication.

---

## 1. Mutex Management

**What to implement:**  
A Global-namespace Mutex that guarantees only one Leader process runs at a time, even across multiple Windows user sessions on the same machine.

**Rules:**

- Always use the `Global\` prefix in the Mutex name (e.g., `Global\YouTubeControl_Leader_Mutex`).  
  Without it, the Mutex is session-scoped and a second user on the same PC could start a second Leader.
- Grant `MutexRights.FullControl` to `WellKnownSidType.WorldSid` via `MutexSecurity` so the Mutex is accessible regardless of which user account created it.
- After calling `WaitOne()`, always wrap it in a `try/catch (AbandonedMutexException)`. An `AbandonedMutexException` means the previous Leader crashed without releasing — the OS transfers ownership, and the catch block must treat this as a successful acquisition.
- Release the Mutex in `Dispose()` — never rely on GC to release it.

**Key types:** `System.Threading.Mutex`, `System.Security.AccessControl.MutexSecurity`, `System.Security.AccessControl.MutexAccessRule`, `System.Security.Principal.SecurityIdentifier`

---

## 2. Named Pipes Patterns

**What to implement:**  
An async `NamedPipeServerStream` in the Leader and a `NamedPipeClientStream` in the Messenger.

**Server (Leader) rules:**

- Use `NamedPipeServerStreamAcl.Create()` (from the `System.IO.Pipes.AccessControl` NuGet package) instead of the plain constructor — this is the only way to pass a `PipeSecurity` object on .NET 5+.
- Grant `PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance` to `WorldSid` so Grid 3 (which may run as a different user) can connect.
- Use `PipeOptions.Asynchronous` and call `WaitForConnectionAsync(cancellationToken)` — never block the thread.
- After accepting one connection, immediately dispatch it to a separate `Task` and loop back to `WaitForConnectionAsync`, so the Leader can handle burst commands without queuing.

**Client (Messenger) rules:**

- Use a plain `NamedPipeClientStream` with `PipeDirection.Out`.
- Call `Connect(timeoutMilliseconds)` — **not** `ConnectAsync` without a timeout — so the Messenger process can never hang if the Leader crashed.

---

## 3. Reliability Checks

**Messenger timeout:**  
Set `ConnectionTimeoutMs = 2000`. If the Leader's pipe does not accept within 2 seconds, `Connect()` throws `TimeoutException` — catch it silently. The Messenger must **always exit**, never freeze, because a frozen Messenger means Grid 3's button press never completes.

**Message validation (Leader side):**  
Before executing any incoming pipe message, validate it:

1. Reject if the raw string is null, empty, or longer than 512 characters.
2. Parse into `action` (before the first `:`) and `query` (after).
3. Match `action` against an explicit whitelist: `home`, `up`, `down`, `enter`, `back`, `play_pause`, `fullscreen`, `toggle`, `like`, `search`, `open`, `exit`, `refresh`.
4. Log and discard any message that fails validation — never throw, never crash.

**Pipe accept timeout:**  
Wrap each `WaitForConnectionAsync` call with a `CancellationTokenSource.CreateLinkedTokenSource` that cancels after ~30 seconds. This keeps the server loop cycling even when no Messenger connects, preventing a stale handle from blocking shutdown.

---

## Checklist for the agent

- [ ] Mutex name starts with `Global\`
- [ ] `MutexSecurity` grants `WorldSid` full control
- [ ] `AbandonedMutexException` is caught and treated as successful acquisition
- [ ] Mutex is released in `Dispose()`
- [ ] Pipe server uses `NamedPipeServerStreamAcl.Create()` with `PipeSecurity`
- [ ] `PipeOptions.Asynchronous` is set on the server
- [ ] Each accepted connection is handled on a separate `Task`
- [ ] Messenger uses `Connect(int timeout)`, not an infinite connect
- [ ] Incoming messages are validated against the action whitelist before execution
- [ ] All failures are logged to `error_log.txt` and never shown as dialogs
