#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

public static partial class ScheduleEvaluator
{
    // ── fixed ─────────────────────────────────────────────────────────────────

    private static bool IsFixedDue(FixedRule rule, DateOnly today, TimeOnly nowTime)
        => rule.Days.Contains(today.DayOfWeek) && rule.Times.Contains(nowTime);

    // ── interval ──────────────────────────────────────────────────────────────

    private static bool IsIntervalDue(IntervalRule rule, DateOnly today, TimeOnly nowTime)
    {
        if (!rule.Days.Contains(today.DayOfWeek))
            return false;

        if (nowTime < rule.Start || nowTime > rule.End)
            return false;

        // nowTime aligns with a slot when the elapsed minutes from Start are an
        // exact multiple of the interval. Use total-minutes comparison to stay
        // in integer arithmetic and avoid floating-point rounding issues.
        var elapsed      = (nowTime - rule.Start).TotalMinutes;
        var intervalMins = rule.Every.ToTimeSpan().TotalMinutes;
        return elapsed % intervalMins == 0;
    }

    // ── monthly_date ──────────────────────────────────────────────────────────

    private static bool IsMonthlyDateDue(
        MonthlyDateRule rule, DateOnly today, TimeOnly nowTime, HashSet<DateOnly> excludedDates)
    {
        if (rule.Months != null && !rule.Months.Contains(today.Month))
            return false;

        var targetDate = ResolveMonthlyDate(rule.Day, today.Year, today.Month, excludedDates);
        if (targetDate == null)
            return false;

        return today == targetDate.Value && rule.Times.Contains(nowTime);
    }

    private static DateOnly? ResolveMonthlyDate(
        MonthlyDay day, int year, int month, HashSet<DateOnly> excludedDates) => day switch
    {
        MonthlyDay.Numeric n   => ClampToMonth(n.N, year, month),
        MonthlyDay.Last        => LastDayOfMonth(year, month),
        MonthlyDay.LastBusiness => LastBusinessDayOfMonth(year, month, excludedDates),
        _                      => null,
    };

    private static DateOnly ClampToMonth(int dayNum, int year, int month)
        => new(year, month, Math.Min(dayNum, DateTime.DaysInMonth(year, month)));

    private static DateOnly LastDayOfMonth(int year, int month)
        => new(year, month, DateTime.DaysInMonth(year, month));

    /// <summary>
    /// Scans backward from the last calendar day, skipping weekends and any date in
    /// <paramref name="excludedDates"/>. Returns <see langword="null"/> if no valid day
    /// exists (the rule is silently skipped for that month).
    /// </summary>
    private static DateOnly? LastBusinessDayOfMonth(
        int year, int month, HashSet<DateOnly> excludedDates)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        for (var d = lastDay; d >= 1; d--)
        {
            var date = new DateOnly(year, month, d);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            if (excludedDates.Contains(date))
                continue;
            return date;
        }
        return null;
    }

    // ── nth_weekday ───────────────────────────────────────────────────────────

    private static bool IsNthWeekdayDue(
        NthWeekdayRule rule, DateOnly today, TimeOnly nowTime, HashSet<DateOnly> excludedDates)
    {
        var targetDate = ResolveNthWeekday(rule.Weekday, rule.Nth, rule.SchedulePeriod, today);
        if (targetDate == null)
            return false;

        // Exclusion collision → skip silently; no near-miss adjustment (ADR-026).
        if (excludedDates.Contains(targetDate.Value))
            return false;

        return today == targetDate.Value && rule.Times.Contains(nowTime);
    }

    private static DateOnly? ResolveNthWeekday(
        DayOfWeek weekday, Ordinal nth, Period period, DateOnly today)
    {
        var (periodStart, periodEnd) = GetPeriodBounds(period, today);
        var occurrences = GetWeekdayOccurrencesInRange(weekday, periodStart, periodEnd);

        if (occurrences.Count == 0)
            return null;

        return nth switch
        {
            Ordinal.Numeric n  => n.N <= occurrences.Count ? occurrences[n.N - 1] : null,
            Ordinal.Last       => occurrences[^1],
            Ordinal.LastMinus lm => lm.Offset < occurrences.Count
                                        ? occurrences[^(lm.Offset + 1)]
                                        : null,
            _ => null,
        };
    }

    private static (DateOnly start, DateOnly end) GetPeriodBounds(Period period, DateOnly today)
        => period switch
        {
            Period.Month => (
                new DateOnly(today.Year, today.Month, 1),
                new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))),

            Period.Quarter => GetQuarterBounds(today),

            Period.Year => (
                new DateOnly(today.Year, 1, 1),
                new DateOnly(today.Year, 12, 31)),

            _ => throw new ArgumentOutOfRangeException(nameof(period)),
        };

    /// <summary>
    /// Returns the bounds of the first month of the quarter containing <paramref name="today"/>.
    /// ADR-026: "Nth weekday of a quarter" means the Nth occurrence in the first month of that quarter.
    /// </summary>
    private static (DateOnly start, DateOnly end) GetQuarterBounds(DateOnly today)
    {
        var quarterFirstMonth = ((today.Month - 1) / 3) * 3 + 1;
        var year = today.Year;
        return (
            new DateOnly(year, quarterFirstMonth, 1),
            new DateOnly(year, quarterFirstMonth, DateTime.DaysInMonth(year, quarterFirstMonth)));
    }

    private static List<DateOnly> GetWeekdayOccurrencesInRange(
        DayOfWeek weekday, DateOnly start, DateOnly end)
    {
        var result = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek == weekday)
                result.Add(d);
        return result;
    }

    // ── week_interval ─────────────────────────────────────────────────────────

    private static bool IsWeekIntervalDue(WeekIntervalRule rule, DateOnly today, TimeOnly nowTime)
    {
        if (!rule.Days.Contains(today.DayOfWeek))
            return false;

        var daysDiff = today.DayNumber - rule.Anchor.DayNumber;

        // Only fire on or after the anchor date, and only on an active week.
        if (daysDiff < 0)
            return false;

        var weeksDiff = daysDiff / 7;
        if (weeksDiff % rule.EveryWeeks != 0)
            return false;

        return rule.Times.Contains(nowTime);
    }

    // ── once ──────────────────────────────────────────────────────────────────

    // Consumed-state tracking is deferred (ADR-027 open question / plan §2).
    // Until a sidecar table is implemented, the rule fires every tick at the specified
    // minute — the caller is responsible for idempotent handling.
    private static bool IsOnceDue(OnceRule rule, DateOnly today, TimeOnly nowTime)
    {
        var fireDate = DateOnly.FromDateTime(rule.FireAt);
        var fireTime = new TimeOnly(rule.FireAt.Hour, rule.FireAt.Minute);
        return today == fireDate && nowTime == fireTime;
    }
}
