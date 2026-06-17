# JOSYN.Commons.IdentityHelpers

Windows identity utilities for JOSYN processes.

## Contents

### `WindowsCredential`

A validated value type that wraps a Windows UPN string (`username@domain`).

Accepts both Active Directory domain accounts (`svc_job@corp.local`) and local machine
accounts (`svc_job@MACHINENAME`). Bare usernames without a domain are rejected — all
JOSYN technical users are domain-qualified (ADR-021).

```csharp
var credential = WindowsCredential.Parse("svc_job@corp.local");
if (!credential.Succeeded)
    // handle error
```

---

### `ImpersonatedProcess`

Launches a process under a Windows domain account via `CreateProcessWithLogonW`.

```csharp
var result = ImpersonatedProcess.Start(
    exePath:    @"C:\jobs\MyJob.exe",
    arguments:  "JOSYN-IPC ...",
    password:   resolvedPassword,
    credential: credential.Value);

if (!result.Succeeded)
    // handle error
int pid = result.Value;
```

#### `headless` parameter

| Value | `CreateNoWindow` | Use case |
|-------|-----------------|----------|
| `true` (default) | `true` | Production / service context — no console window |
| `false` | `false` | CLI / dev / debug — job output visible in operator's terminal (ADR-022) |

#### Constraints

- `LoadUserProfile = false` — the technical user account has no local profile.
  Spawned processes must not use `%APPDATA%`, `%LOCALAPPDATA%`, `%USERPROFILE%`,
  or user-scoped `%TEMP%`. System-scoped paths (`%ProgramData%`, `%SystemRoot%`) are safe.
- Windows only (`[SupportedOSPlatform("windows")]`).
- The calling service account requires `SeAssignPrimaryTokenPrivilege` and
  `SeIncreaseQuotaPrivilege`; the technical user requires `SeBatchLogonPrivilege`.
  See ADR-021 for the full privilege matrix.
