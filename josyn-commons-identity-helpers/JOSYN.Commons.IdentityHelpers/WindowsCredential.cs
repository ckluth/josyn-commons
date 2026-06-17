using System.Runtime.Versioning;
using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Commons.IdentityHelpers;

/// <summary>
/// Validated Windows credential in UPN format (<c>username@domain</c>).
/// </summary>
/// <remarks>
/// <para>
/// Wraps a UPN string and guarantees it is non-empty and contains exactly one <c>@</c>.
/// Construct via <see cref="Parse"/>.
/// </para>
/// <para>
/// Works for both domain accounts (<c>svc_job@corp.local</c>) and local machine accounts
/// (<c>svc_job@MACHINENAME</c>). The UPN is split into username and domain internally
/// when needed — callers always deal with the whole UPN string.
/// </para>
/// <para>
/// <b>Bare usernames are not accepted.</b> Always qualify with <c>@domain</c> or
/// <c>@machinename</c> for local accounts.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
public readonly record struct WindowsCredential
{
    /// <summary>The full UPN string: <c>username@domain</c>.</summary>
    public string Upn { get; }

    private WindowsCredential(string upn) => Upn = upn;

    /// <summary>
    /// Parses and validates a UPN string.
    /// </summary>
    /// <param name="upn">Must be non-empty and contain exactly one <c>@</c> character.</param>
    public static Result<WindowsCredential> Parse(string upn)
    {
        if (string.IsNullOrWhiteSpace(upn))
            return Result.Error("TechnicalUserName must not be empty.");

        var atCount = upn.Count(c => c == '@');
        if (atCount == 0)
            return Result.Error($"TechnicalUserName '{upn}' is not in UPN format (username@domain). Bare usernames are not supported.");

        if (atCount > 1)
            return Result.Error($"TechnicalUserName '{upn}' contains more than one '@' — not a valid UPN.");

        var at = upn.IndexOf('@');
        if (at == 0 || at == upn.Length - 1)
            return Result.Error($"TechnicalUserName '{upn}' has an empty username or domain portion.");

        return new WindowsCredential(upn);
    }

    // ── internal helpers ─────────────────────────────────────────────
    // Split only at the ProcessStartInfo boundary — not part of the public contract.
    internal string Username => Upn[..Upn.IndexOf('@')];
    internal string Domain   => Upn[(Upn.IndexOf('@') + 1)..];
}
