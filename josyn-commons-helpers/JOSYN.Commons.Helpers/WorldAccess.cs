using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace JOSYN.Commons.Helpers;

/// <summary>
/// Cross-platform helpers for granting world-accessible permissions to filesystem paths.
/// </summary>
/// <remarks>
/// <para>
/// On Windows, a folder created with <see cref="EnsureFolder"/> receives an
/// Everyone-FullControl ACL so that processes running under different user accounts can
/// all read and write inside it. New files inherit that ACL automatically.
/// </para>
/// <para>
/// On Linux, <see cref="SetFileWorldWritable"/> applies chmod 666 to a single file
/// immediately after creation, making it accessible to processes running as a different
/// uid on their next access attempt.
/// </para>
/// </remarks>
public static class WorldAccess
{
    /// <summary>
    /// Creates <paramref name="folder"/> if it does not exist, then applies world-writable
    /// permissions: Everyone-FullControl ACL on Windows (with <c>Hidden</c> attribute),
    /// no extra step on Linux (<c>/tmp</c> and similar directories are world-writable by
    /// OS convention).
    /// </summary>
    public static void EnsureFolder(string folder)
    {
        if (Directory.Exists(folder)) return;

        Directory.CreateDirectory(folder);

        if (OperatingSystem.IsWindows())
            ApplyWindowsWorldWritableAcl(folder);
    }

    /// <summary>
    /// On Linux: applies chmod 666 to <paramref name="path"/> so a process running as a
    /// different uid can open the file on its next access attempt.<br/>
    /// On Windows: no-op — the folder ACL already propagates to newly created files via
    /// inheritance.
    /// </summary>
    public static void SetFileWorldWritable(string path)
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

    // ── private ──────────────────────────────────────────────────────────────

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
}
