# JOSYN.Commons.Helpers

Generic cross-cutting helpers for JOSYN processes.

## Contents

### `Turnstile`

A cross-process, cross-user named mutex backed by an exclusive file lock.

Ensures only one OS process at a time executes a named critical section — even when processes
run under different user accounts or elevation levels.

Works on **Windows** and **Linux**.

#### Usage

```csharp
var result = Turnstile.Run("MyLock", () =>
{
    // only one process at a time reaches here
    File.AppendAllText(sharedLogPath, line);
});

if (!result.Succeeded)
    Console.Error.WriteLine(result.ErrorMessage);
```

#### Lock folder

| Platform | Path |
|----------|------|
| Windows  | `%ProgramData%\.josyn-locks` (Everyone FullControl ACL, Hidden) |
| Linux    | `/tmp/.josyn-locks` (world-writable via chmod 666 per lock file) |
