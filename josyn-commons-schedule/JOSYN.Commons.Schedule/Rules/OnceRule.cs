#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Fire exactly once at a specified date and time.
/// </summary>
/// <remarks>
/// State tracking (marking the rule as consumed after it fires) is deferred to the
/// <c>TimeScheduler</c> implementation and is not a concern of this model.
/// </remarks>
/// <param name="FireAt">The exact date and time at which the rule fires.</param>
/// <param name="ActiveFrom">Inherited from <see cref="BoundedRule"/>.</param>
/// <param name="ActiveUntil">Inherited from <see cref="BoundedRule"/>.</param>
public sealed record OnceRule(
    DateTime FireAt,
    DateBound? ActiveFrom,
    DateBound? ActiveUntil) : BoundedRule(ActiveFrom, ActiveUntil);
