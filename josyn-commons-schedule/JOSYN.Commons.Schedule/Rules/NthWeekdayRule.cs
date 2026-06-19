#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Fire on the Nth — or last, or last-N — occurrence of a named weekday within a
/// calendar period.
/// </summary>
/// <remarks>
/// If the computed date falls on an excluded date, that occurrence is skipped entirely.
/// No near-miss adjustment is performed — operators who need a fallback define an
/// explicit <c>once</c> block.
/// </remarks>
/// <param name="Weekday">The day of the week to target.</param>
/// <param name="Nth">Which occurrence within the period.</param>
/// <param name="SchedulePeriod">The calendar period that scopes the ordinal lookup.</param>
/// <param name="Times">One or more fire times on the computed date.</param>
/// <param name="ActiveFrom">Inherited from <see cref="BoundedRule"/>.</param>
/// <param name="ActiveUntil">Inherited from <see cref="BoundedRule"/>.</param>
public sealed record NthWeekdayRule(
    DayOfWeek Weekday,
    Ordinal Nth,
    Period SchedulePeriod,
    IReadOnlyList<TimeOnly> Times,
    DateBound? ActiveFrom,
    DateBound? ActiveUntil) : BoundedRule(ActiveFrom, ActiveUntil);
