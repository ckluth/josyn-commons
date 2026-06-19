#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// A single semantic issue found by <see cref="ScheduleValidator"/>.
/// </summary>
/// <param name="Severity">Whether the issue is a hard error or an advisory warning.</param>
/// <param name="Message">Human-readable description of the issue.</param>
/// <param name="RuleIndex">
/// Zero-based index of the offending rule in <see cref="ScheduleDefinition.Rules"/>,
/// or <see langword="null"/> for cross-rule issues (e.g. duplicates reference the second occurrence).
/// Add 1 for display purposes.
/// </param>
public sealed record ValidationIssue(
    ValidationSeverity Severity,
    string Message,
    int? RuleIndex);
