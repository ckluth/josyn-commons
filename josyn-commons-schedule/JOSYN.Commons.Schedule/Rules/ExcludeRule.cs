#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Declares dates on which no rule in the file may fire.
/// </summary>
/// <remarks>
/// Exclusions always win — no other rule type can override them. Multiple
/// <c>exclude</c> blocks in one file are permitted; their date sets are merged.
/// <c>exclude</c> blocks do not support <c>active_from</c> / <c>active_until</c>.
/// </remarks>
/// <param name="Dates">
/// One or more date ranges. A range where <see cref="DateRange.Start"/> equals
/// <see cref="DateRange.End"/> represents a single excluded date.
/// </param>
public sealed record ExcludeRule(IReadOnlyList<DateRange> Dates) : ScheduleRule;
