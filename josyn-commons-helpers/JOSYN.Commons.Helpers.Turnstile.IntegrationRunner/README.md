# Turnstile Integration Test

A standalone, manually executed test scenario that verifies `Turnstile` correctly enforces
mutual exclusion across concurrent OS processes. Not a unit test — spawns real child processes.
Runs on **Windows** and **Linux** (including headless machines without an IDE).

---

## What the test does

1. Creates a temporary file.
2. Spawns one **IntegrationWorker** process per logical CPU core, all in parallel.
3. Each worker acquires the `Turnstile` lock and appends one uniquely identifiable line
   (`pid=<pid> iter=<n>`) to the shared file — 10 times in sequence, with a random delay
   before each attempt to increase contention.
4. After all workers exit, the runner asserts:
   - All processes exited with code **0** (no Turnstile failure or exception).
   - The file contains exactly **`ProcessorCount × 10`** non-empty lines.
   - There are **no duplicate lines** (concurrent write corruption would produce them).

The runner exits `0` (pass) or `1` (fail) and prints a clear `[PASS]` / `[FAIL]` summary.

---

## Projects

| Project | Role |
|---------|------|
| `JOSYN.Commons.Helpers.Turnstile.IntegrationRunner` | Orchestrator — this executable |
| `JOSYN.Commons.Helpers.Turnstile.IntegrationWorker` | Worker — spawned by the runner |

Both must be built (or published) before running.

---

## Prerequisites

- .NET 10 SDK installed.
- Both projects built — the runner locates the worker by path and does not build it at runtime.

---

## How to run on Windows (dev machine)

```cmd
cd josyn-commons-helpers
.local-build\build.cmd Debug
.\JOSYN.Commons.Helpers.Turnstile.IntegrationRunner\bin\Debug\JOSYN.Commons.Helpers.Turnstile.IntegrationRunner.exe
```

---

## How to run on Linux (dev machine)

```bash
cd josyn-commons-helpers
dotnet build --configuration Debug
./JOSYN.Commons.Helpers.Turnstile.IntegrationRunner/bin/Debug/JOSYN.Commons.Helpers.Turnstile.IntegrationRunner
```

---

## How to run on a headless machine (e.g. Raspberry Pi)

### Step 1 — Publish on the dev machine

First check your Pi's architecture (see Step 3 → "Connect and check architecture") and pick
the matching `--runtime`:

| Pi model / OS | `uname -m` | `--runtime` |
|---------------|-----------|-------------|
| Pi 4 / 5 with 64-bit OS | `aarch64` | `linux-arm64` |
| Pi 2 / 3, or Pi 4 with 32-bit OS | `armv7l` | `linux-arm` |

Publish both projects as self-contained binaries into a single output folder
(replace `linux-arm` with `linux-arm64` if your Pi is 64-bit):

```powershell
cd C:\DevGit\josyn-commons\josyn-commons-helpers

dotnet publish JOSYN.Commons.Helpers.Turnstile.IntegrationRunner --configuration Release --runtime linux-arm --self-contained true --output publish\pi

dotnet publish JOSYN.Commons.Helpers.Turnstile.IntegrationWorker --configuration Release --runtime linux-arm --self-contained true --output publish\pi
```

After this, `publish\pi\` contains both executables and all their dependencies
(shared libraries, `*.so` files, `runtimeconfig.json`, etc. — around 70–80 files total).

### Step 2 — Prepare and copy to the Pi

#### Verify the Pi is reachable

```bash
ping raspberrypi.local
```

If that doesn't resolve, use the Pi's IP address instead of `raspberrypi.local` in all
commands below. Find it with `hostname -I` on the Pi, or check your router's DHCP table.

#### Establish an SSH connection

SSH lets you run commands on the Pi from your dev machine. The default Raspberry Pi OS
credentials are username `pi` and password `raspberry` (change this if you haven't already).

```bash
ssh pi@raspberrypi.local
```

You will be prompted for the password:

```
pi@raspberrypi.local's password: _
```

Type the password and press Enter. The password is not echoed — the cursor will not move.
On success you land in the Pi's shell:

```
pi@raspberrypi:~ $
```

Type `exit` to close the SSH session and return to your dev machine.

> **On Windows:** use the built-in `ssh` command in PowerShell or Command Prompt (available
> since Windows 10). Or use [PuTTY](https://www.putty.org) if you prefer a GUI.

#### Create the target folder on the Pi

```cmd
ssh pi@192.168.178.42 "mkdir -p ~/josyn-integration"
```

This runs the `mkdir` command on the Pi **without opening an interactive session**.
You will be prompted for the password once.

#### Copy all files

Run this in PowerShell or CMD on your **Windows dev machine** — not inside an SSH session.
If you are currently SSHed into the Pi, type `exit` first.

```cmd
scp -r "C:\DevGit\josyn-commons\josyn-commons-helpers\publish\pi\." pi@192.168.178.42:~/josyn-integration/
```

The quotes around the source path are required on Windows — without them `scp` misreads
the `:` in `C:` as a host separator and fails.

You will be prompted for the Pi's password once, then all files copy over:

```
pi@192.168.178.42's password: _
JOSYN.Commons.Helpers.Turnstile.IntegrationRunner          100%  ...
JOSYN.Commons.Helpers.Turnstile.IntegrationWorker          100%  ...
JOSYN.Commons.Helpers.dll                                  100%  ...
...
```

#### Verify the copy (optional)

```bash
ssh pi@raspberrypi.local "ls ~/josyn-integration/"
```

You should see both executables among the listed files:

```
JOSYN.Commons.Helpers.Turnstile.IntegrationRunner
JOSYN.Commons.Helpers.Turnstile.IntegrationWorker
JOSYN.Commons.Helpers.dll
...
```

#### Re-deploy after changes

Repeat Step 1 (publish) and then run the `scp` command again. `scp` overwrites existing
files. No need to delete the folder first.

> **If you are switching architectures** (e.g. from `linux-arm` to `linux-arm64`), delete
> the folder on the target machine first and re-copy — otherwise stale binaries from the
> previous architecture will remain and cause `Exec format error`:
> ```bash
> ssh pi@192.168.178.42 "rm -rf ~/josyn-integration && mkdir ~/josyn-integration"
> ```

For frequent re-deploys, `rsync` is faster (copies only changed files):

```bash
rsync -av --delete publish/pi/. pi@raspberrypi.local:~/josyn-integration/
```

### Step 3 — Run on the Pi

#### Connect and check architecture

```bash
ssh pi@192.168.178.42
uname -m
```

| Output | Meaning | Correct `--runtime` for Step 1 |
|--------|---------|-------------------------------|
| `aarch64` | 64-bit ARM (Pi 4/5 with 64-bit OS) | `linux-arm64` |
| `armv7l` | 32-bit ARM (Pi 2/3, or Pi 4 with 32-bit OS) | `linux-arm` |

If your output doesn't match what you published in Step 1, go back and re-publish with the
correct runtime identifier, then re-copy.

#### Navigate and mark executables

```bash
cd ~/josyn-integration

# Required once — Linux does not preserve execute bits from scp.
chmod +x JOSYN.Commons.Helpers.Turnstile.IntegrationRunner
chmod +x JOSYN.Commons.Helpers.Turnstile.IntegrationWorker
```

> You only need to `chmod` once per deploy. After that the bits are set on the Pi's filesystem.

#### Run

```bash
./JOSYN.Commons.Helpers.Turnstile.IntegrationRunner
echo "Exit code: $?"
```

The runner spawns the worker automatically — do **not** run the worker directly.
The worker binary must be in the **same directory** as the runner.

#### What you will see

```
=== Turnstile Integration Test ===
Processes : 4  (one per logical CPU core)
Iterations: 10  per process
Expected  : 40  lines
Lock file : /tmp/josyn-turnstile-<guid>.txt

Worker    : /home/pi/josyn-integration/JOSYN.Commons.Helpers.Turnstile.IntegrationWorker

Waiting for 4 worker(s).... done.

[PASS] 40 lines, no duplicates. Mutual exclusion held.
Exit code: 0
```

The lock folder `/tmp/.josyn-locks` is created automatically on first run and removed
automatically after. The temp file is cleaned up regardless of pass or fail.

Duration is typically 3–10 seconds on a Pi 4 (4 cores × 10 iterations × up to 100 ms
random delay per iteration).

---

## Where the worker binary is expected

The runner searches in order:

1. **Same directory as the runner** (publish scenario) — worker `.dll` must also be present.
2. **Sibling project output** `../JOSYN.Commons.Helpers.Turnstile.IntegrationWorker/bin/<Debug|Release>/` (dev build scenario).

If neither is found, the runner exits immediately with `[FAIL]` and a descriptive message.

---

## Interpreting failures

| Symptom | Likely cause |
|---------|--------------|
| `[FAIL]` worker binary not found | Not built yet — run `build.cmd` or `dotnet build` first |
| `Exec format error` | Wrong architecture — published for `linux-arm` but machine is `x86_64`, or vice versa. Run `uname -m` on the target, re-publish with the correct `--runtime`, delete the remote folder, re-copy. |
| `GLIBC_2.34 not found` | Target OS is too old for .NET 10 (requires Debian 11 Bullseye or newer). Raspberry Pi OS Buster (2022 kernel) is affected — update the OS. |
| Worker process(es) exited non-zero | `Turnstile.Run` returned `Fail` — check `C:\ProgramData\.josyn-locks` or `/tmp/.josyn-locks` for leftover lock files |
| Wrong line count | A worker timed out waiting for the lock; increase the default timeout in `Turnstile.Run` |
| Duplicate lines detected | Mutual exclusion failed — this is a critical bug in `Turnstile` |

