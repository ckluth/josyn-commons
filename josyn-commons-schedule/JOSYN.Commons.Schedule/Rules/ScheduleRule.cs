#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Root of the schedule rule discriminated union.
/// Every rule block in a schedule file maps to a concrete subtype.
/// </summary>
public abstract record ScheduleRule;

/// <summary>
/// A rule that may carry optional activation window modifiers
/// (<c>active_from</c> / <c>active_until</c>).
/// Outside the declared window the rule is ignored entirely — it is not evaluated
/// and does not contribute to launches.
/// </summary>
/// <param name="ActiveFrom">
/// Inclusive window start. <see langword="null"/> means no lower bound.
/// </param>
/// <param name="ActiveUntil">
/// Inclusive window end. <see langword="null"/> means no upper bound.
/// </param>
public abstract record BoundedRule(
    DateBound? ActiveFrom,
    DateBound? ActiveUntil) : ScheduleRule;
