#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Fire at one or more explicit times on specified days.
/// Each time value is an independent fire trigger; order within the list is irrelevant.
/// </summary>
/// <param name="Days">Days of the week on which this rule is active.</param>
/// <param name="Times">One or more fire times.</param>
/// <param name="ActiveFrom">Inherited from <see cref="BoundedRule"/>.</param>
/// <param name="ActiveUntil">Inherited from <see cref="BoundedRule"/>.</param>
public sealed record FixedRule(
    DaySet Days,
    IReadOnlyList<TimeOnly> Times,
    DateBound? ActiveFrom,
    DateBound? ActiveUntil) : BoundedRule(ActiveFrom, ActiveUntil);
