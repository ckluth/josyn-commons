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
public sealed class Turnstile
{
    private string? lockFileName;
    private bool acquired;

    private Turnstile() { }

    // The logical id is hashed to a filename — callers never deal with filesystem constraints.
    private string LockFileName
    {
        // null-forgiving is safe: always assigned by TryGetAccess before any read path.
        get => this.lockFileName!;
        set => this.lockFileName = HashId(value);
    }

    private bool WasTimeout { get; set; }

    private FileStream? FileStream { get; set; }

    // On Linux /tmp is world-writable (sticky bit) so any user can create files there.
    // On Windows we manage ACLs on the folder ourselves.
    // The dot prefix hides the folder on Linux; Hidden attribute is set on Windows.
    private static string LockFolder =>
        OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ".turnstile-locks")
            : "/tmp/.turnstile-locks";

    private string FilePath => Path.Combine(LockFolder, this.LockFileName);

    // --- Public API ---

    /// <summary>
    /// Acquires the named lock, executes <paramref name="worker"/>, releases the lock.
    /// </summary>
    /// <param name="id">Logical name for the critical section (e.g. <c>"AppLog"</c>).</param>
    /// <param name="worker">Action to execute while the lock is held.</param>
    /// <param name="timeOutMilliSeconds">
    /// How long to spin-wait for the lock before giving up. Default: 180 000 ms (3 min).
    /// </param>
    /// <returns>
    /// <see cref="Result.Success"/> — lock acquired, worker completed without throwing.<br/>
    /// <c>Result.Fail</c> — timeout elapsed before the lock was acquired.<br/>
    /// <c>Result.Fail</c> — worker threw an exception.
    /// </returns>
    public static Result Run(string id, Action worker, int timeOutMilliSeconds = 180_000)
    {
        var ts = new Turnstile();
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
            ts.Release();
        }
    }

    // --- Private logic ---

    private void TryGetAccess(string id, int timeOutMilliseconds)
    {
        this.LockFileName = id;
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

    private void Release()
    {
        if (!this.acquired) return;

        try { this.FileStream?.Dispose(); } catch { /* ignore */ }
        try { File.Delete(this.FilePath); } catch { /* ignore */ }
    }

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
        catch
        {
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

    private static string HashId(string id)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(id));
        return BitConverter.ToString(hash);
    }
}
