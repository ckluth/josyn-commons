using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Commons.Helpers;

/// <summary>
/// Cross-process, cross-user named mutex backed by an exclusive file lock.
/// </summary>
/// <remarks>
/// <para>
/// Solves the problem where two or more separate OS processes — potentially running under
/// different user accounts — must not execute the same named critical section simultaneously.
/// Typical use: serialising writes to a shared log file between the scheduler and job processes.
/// </para>
/// <para>
/// The lock is a <see cref="FileStream"/> opened with <see cref="FileShare.None"/>.
/// The OS kernel enforces exclusivity; the lock is automatically released when the holding
/// process exits or crashes — there is no abandoned-mutex problem as with
/// <see cref="System.Threading.Mutex"/>.
/// </para>
/// <para>
/// <b>Platform support:</b> Windows and Linux.
/// On Windows the lock folder (<c>%ProgramData%\.josyn-locks</c>) is created with an
/// Everyone-FullControl ACL so processes running at different elevation levels can all participate.
/// On Linux the lock folder is <c>/tmp/.josyn-locks</c> (world-writable by OS convention);
/// each lock file is chmod 666 immediately after creation so a process running as a different
/// uid can open it on its next retry attempt.
/// </para>
/// <para>
/// <b>Thread safety within one process:</b> this class serialises between OS processes via the
/// file lock but does not add an in-process <see cref="System.Threading.Monitor"/> layer.
/// Multiple threads competing for the same lock will spin concurrently. That is correct but
/// wasteful; add your own in-process gate when needed.
/// </para>
/// </remarks>
public sealed class Turnstile : IDisposable
{
    private string? turnstileId;
    private bool makeMd5 = true;
    private bool acquired;

    /// <summary>
    /// Optional sink for diagnostic messages produced while waiting for the lock.
    /// Each failed acquisition attempt writes the causing exception message here.
    /// Assign before calling <see cref="Run"/> or <see cref="TryGetAccess"/>.
    /// </summary>
    public static Action<string>? Add2DebugLog { get; set; }

    /// <summary>
    /// The effective lock identifier used as the lock-file name.
    /// Normally the MD5 hash of the <c>id</c> string passed to <see cref="Run"/> or
    /// <see cref="TryGetAccess"/>; pass <c>keepPlaintextId = true</c> to retain the raw string
    /// (useful for debugging — ensure the string is a valid filename).
    /// </summary>
    public string TurnstileId
    {
        // null-forgiving is safe here: turnstileId is always assigned by TryGetAccess() before this getter
        // is reachable through any code path (Run or direct TryGetAccess call).
        get => this.turnstileId!;
        private set => this.turnstileId = this.makeMd5 ? GetMd5(value) : value;
    }

    /// <summary>
    /// <see langword="true"/> if the last <see cref="TryGetAccess"/> call exited due to the
    /// timeout elapsing rather than successfully acquiring the lock.
    /// When using <see cref="Run"/>, prefer checking <c>result.Succeeded</c> — timeout is
    /// reported as <c>Result.Fail</c> there. This property is for callers using
    /// <see cref="TryGetAccess"/> directly.
    /// </summary>
    public bool WasTimeout { get; private set; }

    private FileStream? FileStream { get; set; }

    // On Linux /tmp is world-writable (sticky bit) so any user can create files there.
    // On Windows we manage ACLs on the folder ourselves.
    // The dot prefix hides the folder on Linux; Hidden attribute is set on Windows.
    private static string LockFolder =>
        OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ".josyn-locks")
            : "/tmp/.josyn-locks";

    private string FilePath => Path.Combine(LockFolder, this.TurnstileId);

    // --- Public API ---

    /// <summary>
    /// Acquires the named lock, executes <paramref name="worker"/>, releases the lock.
    /// This is the preferred high-level entry point.
    /// </summary>
    /// <param name="id">
    /// Logical name for the critical section (e.g. <c>"AppLog"</c>).
    /// Hashed to an MD5 filename by default; see <paramref name="keepPlaintextId"/>.
    /// </param>
    /// <param name="worker">Action to execute while the lock is held.</param>
    /// <param name="timeOutMilliSeconds">
    /// How long to spin-wait for the lock before giving up. Default: 180 000 ms (3 min).
    /// </param>
    /// <param name="keepPlaintextId">
    /// When <see langword="true"/>, <paramref name="id"/> is used as-is for the lock-file name.
    /// </param>
    /// <returns>
    /// <see cref="Result.Success"/> — lock acquired, worker completed without throwing.<br/>
    /// <see cref="Result.Fail(string)"/> — timeout elapsed before the lock was acquired.<br/>
    /// <see cref="Result.Fail(Exception)"/> — worker threw an exception.
    /// </returns>
    public static Result Run(string id, Action worker, int timeOutMilliSeconds = 180_000, bool keepPlaintextId = false)
    {
        var ts = new Turnstile { makeMd5 = !keepPlaintextId };
        try
        {
            ts.TryGetAccess(id, timeOutMilliSeconds);

            if (ts.WasTimeout)
                return Result.Fail($"Turnstile timeout: lock '{id}' was not acquired within {timeOutMilliSeconds}ms.");

            worker();
            return Result.Success;
        }
        catch (Exception e)
        {
            return Result.Fail(e);
        }
        finally
        {
            ts.Dispose();
        }
    }

    /// <summary>
    /// Low-level API: spin-waits until the named lock is acquired or the timeout elapses.
    /// Check <see cref="WasTimeout"/> after this call. Caller is responsible for
    /// calling <see cref="Dispose"/> to release the lock.
    /// Prefer <see cref="Run"/> for typical use.
    /// </summary>
    /// <param name="id">Logical lock name — hashed to a filename (see <see cref="TurnstileId"/>).</param>
    /// <param name="timeOutMilliseconds">Spin timeout in milliseconds.</param>
    public void TryGetAccess(string id, int timeOutMilliseconds)
    {
        this.TurnstileId = id;
        var sw = Stopwatch.StartNew();
        while (true)
        {
            if (this.TryOnceGetAccess()) break;

            Thread.Sleep(500);

            if (!(sw.Elapsed.TotalMilliseconds > timeOutMilliseconds)) continue;
            this.WasTimeout = true;
            break;
        }
    }

    /// <summary>
    /// Releases the lock: closes the file handle and deletes the lock file.
    /// No-op if this instance never acquired the lock (e.g. after a timeout).
    /// </summary>
    public void Dispose()
    {
        // Only clean up if we actually hold the lock.
        if (!this.acquired) return;

        try { this.FileStream?.Dispose(); } catch { /* ignore */ }
        try { File.Delete(this.FilePath); } catch { /* ignore */ }
    }

    // --- Private logic ---

    private bool TryOnceGetAccess()
    {
        try
        {
            EnsureFolder(LockFolder);

            // OpenOrCreate + FileShare.None = cross-process exclusive file lock.
            //   - File doesn't exist  → created, exclusive lock acquired.
            //   - File exists, held   → IOException (flock EWOULDBLOCK on Linux,
            //                           sharing violation on Windows) → caller retries.
            //   - File exists, orphan → opened, exclusive lock acquired (handles crash of prior holder).
            this.FileStream = new FileStream(
                this.FilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);

            // Linux: chmod 666 so a process running under a different user account can
            // open this file on its next retry attempt (to race for the flock).
            // On Windows the folder ACL already grants Everyone full control.
            SetWorldWritable(this.FilePath);

            this.acquired = true;
            return true;
        }
        catch (Exception ex)
        {
            Add2DebugLog?.Invoke(ex.Message);
            return false;
        }
    }

    // --- Folder setup ---

    private static void EnsureFolder(string folder)
    {
        if (Directory.Exists(folder)) return;

        Directory.CreateDirectory(folder);

        if (OperatingSystem.IsWindows())
            ApplyWindowsWorldWritableAcl(folder);
        // On Linux /tmp already carries the sticky bit; no extra setup needed.
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyWindowsWorldWritableAcl(string folder)
    {
        try
        {
            var di = new DirectoryInfo(folder);
            var security = di.GetAccessControl();
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            security.AddAccessRule(new FileSystemAccessRule(
                account,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.InheritOnly,
                AccessControlType.Allow));
            di.SetAccessControl(security);
            di.Attributes = FileAttributes.Hidden;
        }
        catch { /* best-effort */ }
    }

    private static void SetWorldWritable(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        try
        {
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead  | UnixFileMode.UserWrite  |
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                UnixFileMode.OtherRead | UnixFileMode.OtherWrite);
        }
        catch { /* best-effort */ }
    }

    // --- Helpers ---

    private static string GetMd5(string text)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hash);
    }
}
