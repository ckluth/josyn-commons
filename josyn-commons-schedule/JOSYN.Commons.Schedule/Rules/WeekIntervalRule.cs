#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Fire on specified days at specified times, repeating every N weeks.
/// </summary>
/// <remarks>
/// The <see cref="Anchor"/> key is a past ISO date that was a valid fire date
/// (i.e. a day whose weekday is a member of <see cref="Days"/>); it establishes the
/// week-parity phase. The scheduler fires on weeks where
/// <c>floor((today − Anchor) / 7) mod EveryWeeks == 0</c> and the weekday matches.
/// </remarks>
/// <param name="Days">Days of the week on which this rule fires in active weeks.</param>
/// <param name="Times">One or more fire times on each active day.</param>
/// <param name="EveryWeeks">Repetition interval in whole weeks. Must be ≥ 1.</param>
/// <param name="Anchor">
/// A reference date (must be a weekday member of <see cref="Days"/>) that defines
/// which weeks are active.
/// </param>
/// <param name="ActiveFrom">Inherited from <see cref="BoundedRule"/>.</param>
/// <param name="ActiveUntil">Inherited from <see cref="BoundedRule"/>.</param>
public sealed record WeekIntervalRule(
    DaySet Days,
    IReadOnlyList<TimeOnly> Times,
    int EveryWeeks,
    DateOnly Anchor,
    DateBound? ActiveFrom,
    DateBound? ActiveUntil) : BoundedRule(ActiveFrom, ActiveUntil);
