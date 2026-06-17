using System.Runtime.Versioning;
using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Commons.Helpers;

/// <summary>
/// Validated Windows credential in UPN format (<c>username@domain</c>).
/// </summary>
/// <remarks>
/// <para>
/// Wraps a UPN string and guarantees it is non-empty and contains exactly one <c>@</c>.
/// Construct via <see cref="Parse"/>.
/// </para>
/// <para>
/// <b>Bare usernames are not accepted.</b> Technical accounts in JOSYN are domain-qualified
/// by design; local accounts (no domain) offer no meaningful isolation boundary.
/// </para>
/// <para>
/// The UPN can be passed directly to <see cref="System.Diagnostics.ProcessStartInfo.UserName"/>
/// with <c>Domain = null</c> — <c>CreateProcessWithLogonW</c> resolves domain membership
/// from the UPN suffix.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
public readonly record struct WindowsCredential
{
    /// <summary>The UPN string: <c>username@domain</c>.</summary>
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

        // Bare username (no @) — local accounts are not supported (ADR-021).
        var atCount = upn.Count(c => c == '@');
        if (atCount == 0)
            return Result.Error($"TechnicalUserName '{upn}' is not in UPN format (username@domain). Local accounts are not supported.");

        if (atCount > 1)
            return Result.Error($"TechnicalUserName '{upn}' contains more than one '@' — not a valid UPN.");

        // Neither the local-part nor the domain portion may be empty.
        var at = upn.IndexOf('@');
        if (at == 0 || at == upn.Length - 1)
            return Result.Error($"TechnicalUserName '{upn}' has an empty username or domain portion.");

        return new WindowsCredential(upn);
    }
}
