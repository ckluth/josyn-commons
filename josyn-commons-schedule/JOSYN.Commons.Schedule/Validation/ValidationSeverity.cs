#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Classifies a <see cref="ValidationIssue"/> as a hard error or an advisory warning.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// A definite contradiction — the rule as written cannot fire correctly or will
    /// produce clearly wrong behaviour (e.g. a window where start ≥ end).
    /// </summary>
    Error,

    /// <summary>
    /// A suspicious but technically valid condition that is likely a modelling mistake
    /// and worth reviewing (e.g. an ordinal that is unreachable in most months).
    /// </summary>
    Warning,
}
