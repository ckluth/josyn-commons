#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Repeat every fixed duration within a bounded time window on specified days.
/// </summary>
/// <remarks>
/// The first fire is at <see cref="Start"/>. Subsequent fires are at
/// <c>Start + N × Every</c>. A computed slot that falls after <see cref="End"/>
/// is dropped silently — the window is never overrun. <see cref="End"/> itself is
/// a permitted fire time if it aligns exactly.
/// </remarks>
/// <param name="Days">Days of the week on which this rule is active.</param>
/// <param name="Start">Window open time (inclusive).</param>
/// <param name="End">Window close time (inclusive if exactly aligned).</param>
/// <param name="Every">Repetition interval.</param>
/// <param name="ActiveFrom">Inherited from <see cref="BoundedRule"/>.</param>
/// <param name="ActiveUntil">Inherited from <see cref="BoundedRule"/>.</param>
public sealed record IntervalRule(
    DaySet Days,
    TimeOnly Start,
    TimeOnly End,
    Duration Every,
    DateBound? ActiveFrom,
    DateBound? ActiveUntil) : BoundedRule(ActiveFrom, ActiveUntil);
