using System.Diagnostics;
using System.Runtime.Versioning;
using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Commons.IdentityHelpers;

/// <summary>
/// Launches a process under a Windows user account (impersonation via <c>CreateProcessWithLogonW</c>).
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="ProcessStartInfo"/> credential properties for the impersonated-launch pattern:
/// headless, no user profile, UPN logon.
/// </para>
/// <para>
/// <b>Contract constraint for spawned processes:</b> because <c>LoadUserProfile</c> is
/// <see langword="false"/>, the spawned process must not rely on user-profile environment
/// variables (<c>%APPDATA%</c>, <c>%LOCALAPPDATA%</c>, <c>%USERPROFILE%</c>,
/// user-scoped <c>%TEMP%</c>). System-scoped paths (<c>%ProgramData%</c>,
/// <c>%SystemRoot%</c>) are safe (ADR-021).
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
public static class ImpersonatedProcess
{
    /// <summary>
    /// Launches <paramref name="exePath"/> under the account described by
    /// <paramref name="credential"/> and returns the process ID.
    /// </summary>
    /// <param name="exePath">Full path to the executable.</param>
    /// <param name="arguments">CLI arguments to pass to the process.</param>
    /// <param name="password">Plain-text password for the account.</param>
    /// <param name="credential">Validated Windows credential (UPN format).</param>
    /// <param name="headless">
    /// When <see langword="true"/> (default) the process is started with no console window
    /// (production / service context).
    /// When <see langword="false"/> the process inherits the caller's console so that its
    /// output is visible — intended for CLI / dev / debug sessions.
    /// </param>
    public static Result<int> Start(
        string            exePath,
        string            arguments,
        string            password,
        WindowsCredential credential,
        bool              headless = true)
    {
        if (!File.Exists(exePath))
            return Result<int>.Fail($"Executable not found: '{exePath}'");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName            = exePath,
                Arguments           = arguments,
                UserName            = credential.Username,
                Domain              = credential.Domain,   // explicit domain/machinename — works for both AD and local accounts (ADR-021)
                PasswordInClearText = password,
                UseShellExecute     = false,               // required for credential-based launch
                CreateNoWindow      = headless,            // false = attach to caller's console (interactive/dev mode)
                LoadUserProfile     = false,               // technical user has no local profile (ADR-021)
            };

            var process = Process.Start(psi);

            if (process == null)
                return Result<int>.Fail($"Process.Start returned null for '{exePath}'.");

            return process.Id;
        }
        catch (Exception ex) { return ex; }
    }
}
