#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Fire on a specific calendar day each month, optionally restricted to a set of months.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MonthlyDay.LastBusiness"/> scans backward from the last calendar day,
/// skipping weekends and any date covered by an <c>exclude</c> block, and fires on the
/// first valid day found. If no valid day exists in the month the rule is skipped silently.
/// </para>
/// <para>
/// If <see cref="Day"/> is a <see cref="MonthlyDay.Numeric"/> that exceeds the length of a
/// given month (e.g. day 31 in February), the rule fires on the last calendar day of
/// that month instead.
/// </para>
/// </remarks>
/// <param name="Day">Which calendar day within the month to target.</param>
/// <param name="Times">One or more fire times on the computed date.</param>
/// <param name="Months">
/// Restricts which months are active. <see langword="null"/> means every month.
/// </param>
/// <param name="ActiveFrom">Inherited from <see cref="BoundedRule"/>.</param>
/// <param name="ActiveUntil">Inherited from <see cref="BoundedRule"/>.</param>
public sealed record MonthlyDateRule(
    MonthlyDay Day,
    IReadOnlyList<TimeOnly> Times,
    MonthSet? Months,
    DateBound? ActiveFrom,
    DateBound? ActiveUntil) : BoundedRule(ActiveFrom, ActiveUntil);
