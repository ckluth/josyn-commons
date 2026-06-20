#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Evaluates a parsed <see cref="ScheduleDefinition"/> against a point in time to decide
/// whether the schedule is due.
/// </summary>
/// <remarks>
/// <para>
/// Multiple rules in one definition are OR'd: the schedule is due if any rule is
/// satisfied at the evaluation moment. Exclusions are applied after all positive rules
/// are evaluated and always win.
/// </para>
/// <para>
/// Time comparison is at minute granularity — seconds and sub-second parts of the
/// evaluation moment are ignored. This matches the expected invocation cadence of
/// <c>TimeScheduler</c> (one tick per minute).
/// </para>
/// </remarks>
public static partial class ScheduleEvaluator
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="definition"/> has at least one
    /// rule that is satisfied at <paramref name="now"/>, and no <c>exclude</c> rule blocks
    /// the date.
    /// </summary>
    /// <param name="definition">A valid parsed schedule definition.</param>
    /// <param name="now">The evaluation moment (seconds and below are ignored).</param>
    public static bool IsDue(ScheduleDefinition definition, DateTime now)
    {
        var today   = DateOnly.FromDateTime(now);
        var nowTime = new TimeOnly(now.Hour, now.Minute);

        var excludedDates = CollectExcludedDates(definition.Rules);

        // Exclusions always win — no positive rule can override them.
        if (excludedDates.Contains(today))
            return false;

        return definition.Rules
            .OfType<BoundedRule>()
            .Any(rule => IsRuleDue(rule, today, nowTime, excludedDates));
    }

    // ── dispatch ──────────────────────────────────────────────────────────────

    private static bool IsRuleDue(
        BoundedRule rule, DateOnly today, TimeOnly nowTime, HashSet<DateOnly> excludedDates)
    {
        if (!IsInActiveWindow(rule, today))
            return false;

        return rule switch
        {
            FixedRule r        => IsFixedDue(r, today, nowTime),
            IntervalRule r     => IsIntervalDue(r, today, nowTime),
            MonthlyDateRule r  => IsMonthlyDateDue(r, today, nowTime, excludedDates),
            NthWeekdayRule r   => IsNthWeekdayDue(r, today, nowTime, excludedDates),
            WeekIntervalRule r => IsWeekIntervalDue(r, today, nowTime),
            OnceRule r         => IsOnceDue(r, today, nowTime),
            _                  => false,
        };
    }

    // ── active window ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="false"/> when <paramref name="today"/> falls outside the
    /// <c>activeFrom</c> / <c>activeUntil</c> window of <paramref name="rule"/>.
    /// Annual bounds correctly wrap across the year boundary (ADR-026 § activeFrom/activeUntil).
    /// </summary>
    private static bool IsInActiveWindow(BoundedRule rule, DateOnly today)
    {
        if (rule.ActiveFrom == null && rule.ActiveUntil == null)
            return true;

        // Both Annual: year-wrap logic applies (e.g. "Nov 1 – Feb 28").
        if (rule.ActiveFrom is DateBound.Annual fromAnnual &&
            rule.ActiveUntil is DateBound.Annual untilAnnual)
            return IsInAnnualWindow(fromAnnual, untilAnnual, today);

        // One-sided or mixed Annual/FullDate: evaluate each bound independently.
        if (rule.ActiveFrom != null)
        {
            var from = ResolveBound(rule.ActiveFrom, today.Year);
            if (today < from)
                return false;
        }

        if (rule.ActiveUntil != null)
        {
            var until = ResolveBound(rule.ActiveUntil, today.Year);
            if (today > until)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Evaluates a double-Annual window, supporting year-boundary wrap.
    /// E.g. <c>activeFrom=11-01, activeUntil=02-28</c> means November through February.
    /// </summary>
    private static bool IsInAnnualWindow(DateBound.Annual from, DateBound.Annual until, DateOnly today)
    {
        var fromDate  = new DateOnly(today.Year, from.Month,  from.Day);
        var untilDate = new DateOnly(today.Year, until.Month, until.Day);

        // Normal window (no year-wrap): e.g. Apr 1 – Sep 30.
        if (fromDate <= untilDate)
            return today >= fromDate && today <= untilDate;

        // Year-wrapping window: active from fromDate to Dec 31 OR from Jan 1 to untilDate.
        return today >= fromDate || today <= untilDate;
    }

    private static DateOnly ResolveBound(DateBound bound, int year) => bound switch
    {
        DateBound.FullDate fd => fd.Date,
        DateBound.Annual ann  => new DateOnly(year, ann.Month, ann.Day),
        _                     => throw new ArgumentOutOfRangeException(nameof(bound)),
    };

    // ── exclusion collection ──────────────────────────────────────────────────

    private static HashSet<DateOnly> CollectExcludedDates(IReadOnlyList<ScheduleRule> rules)
    {
        var excluded = new HashSet<DateOnly>();

        foreach (var rule in rules.OfType<ExcludeRule>())
            foreach (var range in rule.Dates)
                for (var d = range.Start; d <= range.End; d = d.AddDays(1))
                    excluded.Add(d);

        return excluded;
    }
}
