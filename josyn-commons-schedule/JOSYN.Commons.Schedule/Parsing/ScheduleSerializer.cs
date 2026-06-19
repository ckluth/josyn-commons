using System.Globalization;
using System.Text;

using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Serializes a <see cref="ScheduleDefinition"/> to its INI text representation.
/// </summary>
/// <inheritdoc cref="IScheduleSerializer"/>
public static class ScheduleSerializer
{
    /// <inheritdoc cref="IScheduleSerializer.Serialize"/>
    public static Result<string> Serialize(ScheduleDefinition definition)
    {
        try
        {
            var sb = new StringBuilder();
            for (var i = 0; i < definition.Rules.Count; i++)
            {
                if (i > 0) sb.AppendLine(); // blank line between blocks
                sb.AppendLine(SerializeRule(definition.Rules[i]));
            }
            return Result<string>.Success(sb.ToString());
        }
        catch (Exception ex) { return ex; }
    }

    //
    // Rule dispatch
    //

    /// <summary>Used internally by <see cref="ScheduleValidator"/> for duplicate detection.</summary>
    internal static string SerializeRuleText(ScheduleRule rule) => SerializeRule(rule);

    private static string SerializeRule(ScheduleRule rule) => rule switch
    {
        IntervalRule     r => SerializeInterval(r),
        FixedRule        r => SerializeFixed(r),
        NthWeekdayRule   r => SerializeNthWeekday(r),
        MonthlyDateRule  r => SerializeMonthlyDate(r),
        WeekIntervalRule r => SerializeWeekInterval(r),
        OnceRule         r => SerializeOnce(r),
        ExcludeRule      r => SerializeExclude(r),
        _                  => throw new InvalidOperationException($"Unknown rule type: {rule.GetType().Name}"),
    };

    //
    // Per-rule-type serializers
    //
    private static string SerializeInterval(IntervalRule r)
    {
        var entries = new List<(string Key, string Value)>
        {
            ("type",  "interval"),
            ("days",  FormatDaySet(r.Days)),
            ("start", FormatTime(r.Start)),
            ("end",   FormatTime(r.End)),
            ("every", r.Every.ToString()),
        };
        AppendBounds(entries, r);
        return FormatBlock(entries);
    }

    private static string SerializeFixed(FixedRule r)
    {
        var entries = new List<(string Key, string Value)>
        {
            ("type", "fixed"),
            ("days", FormatDaySet(r.Days)),
            ("time", FormatTimes(r.Times)),
        };
        AppendBounds(entries, r);
        return FormatBlock(entries);
    }

    private static string SerializeNthWeekday(NthWeekdayRule r)
    {
        var entries = new List<(string Key, string Value)>
        {
            ("type",    "nth_weekday"),
            ("weekday", FormatWeekday(r.Weekday)),
            ("nth",     FormatOrdinal(r.Nth)),
            ("period",  FormatPeriod(r.SchedulePeriod)),
            ("time",    FormatTimes(r.Times)),
        };
        AppendBounds(entries, r);
        return FormatBlock(entries);
    }

    private static string SerializeMonthlyDate(MonthlyDateRule r)
    {
        var entries = new List<(string Key, string Value)>
        {
            ("type", "monthly_date"),
            ("day",  FormatMonthlyDay(r.Day)),
            ("time", FormatTimes(r.Times)),
        };
        if (r.Months is not null) entries.Add(("months", FormatMonthSet(r.Months)));
        AppendBounds(entries, r);
        return FormatBlock(entries);
    }

    private static string SerializeWeekInterval(WeekIntervalRule r)
    {
        var entries = new List<(string Key, string Value)>
        {
            ("type",   "week_interval"),
            ("days",   FormatDaySet(r.Days)),
            ("time",   FormatTimes(r.Times)),
            ("every",  r.Every.ToString()),
            ("anchor", r.Anchor.ToString("yyyy-MM-dd")),
        };
        AppendBounds(entries, r);
        return FormatBlock(entries);
    }

    private static string SerializeOnce(OnceRule r)
    {
        var entries = new List<(string Key, string Value)>
        {
            ("type",     "once"),
            ("datetime", r.FireAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
        };
        AppendBounds(entries, r);
        return FormatBlock(entries);
    }

    private static string SerializeExclude(ExcludeRule r)
    {
        var entries = new List<(string Key, string Value)>
        {
            ("type",  "exclude"),
            ("dates", FormatDateRanges(r.Dates)),
        };
        return FormatBlock(entries);
    }

    //
    // Block formatter
    //

    // Pad all keys to the same width so the '=' signs align — matches ADR-026 examples.
    private static string FormatBlock(List<(string Key, string Value)> entries)
    {
        var maxLen = entries.Max(e => e.Key.Length);
        return string.Join(
            Environment.NewLine,
            entries.Select(e => $"{e.Key.PadRight(maxLen)} = {e.Value}"));
    }

    // Append active_from and active_until to the entry list if present.
    private static void AppendBounds(List<(string Key, string Value)> entries, BoundedRule rule)
    {
        if (rule.ActiveFrom  is not null) entries.Add(("active_from",  FormatDateBound(rule.ActiveFrom)));
        if (rule.ActiveUntil is not null) entries.Add(("active_until", FormatDateBound(rule.ActiveUntil)));
    }

    //
    // Value formatters
    //

    private static string FormatDaySet(DaySet days)
    {
        if (days.Equals(DaySet.Weekdays)) return "weekdays";
        if (days.Equals(DaySet.Weekend))  return "weekend";
        if (days.Equals(DaySet.Daily))    return "daily";

        // Sort Mon→Sun (schedule order), compress consecutive runs of ≥ 3 into ranges.
        var sorted = days.Days
            .OrderBy(d => d == DayOfWeek.Sunday ? 6 : (int)d - 1)
            .ToList();

        return CompressDayList(sorted);

        //
        // nested helper
        //
        static string CompressDayList(List<DayOfWeek> sorted)
        {
            var parts = new List<string>();
            var i = 0;
            while (i < sorted.Count)
            {
                var runStart = i;
                // Extend run while days are consecutive in schedule order.
                while (i + 1 < sorted.Count && AreScheduleConsecutive(sorted[i], sorted[i + 1]))
                    i++;
                var runLen = i - runStart + 1;

                if (runLen >= 3)
                    parts.Add($"{DayAbbrev(sorted[runStart])}..{DayAbbrev(sorted[i])}");
                else
                    for (var j = runStart; j <= i; j++)
                        parts.Add(DayAbbrev(sorted[j]));

                i++;
            }
            return string.Join(", ", parts);
        }

        static bool AreScheduleConsecutive(DayOfWeek a, DayOfWeek b) =>
            ScheduleOrder(b) == ScheduleOrder(a) + 1;

        static int ScheduleOrder(DayOfWeek d) => d == DayOfWeek.Sunday ? 6 : (int)d - 1;
    }

    private static string FormatMonthSet(MonthSet months)
    {
        var sorted = months.Months.OrderBy(m => m).ToList();
        return CompressMonthList(sorted);

        //
        // nested helper
        //
        static string CompressMonthList(List<int> sorted)
        {
            var parts = new List<string>();
            var i = 0;
            while (i < sorted.Count)
            {
                var runStart = i;
                while (i + 1 < sorted.Count && sorted[i + 1] == sorted[i] + 1)
                    i++;
                var runLen = i - runStart + 1;

                if (runLen >= 3)
                    parts.Add($"{MonthAbbrev(sorted[runStart])}..{MonthAbbrev(sorted[i])}");
                else
                    for (var j = runStart; j <= i; j++)
                        parts.Add(MonthAbbrev(sorted[j]));

                i++;
            }
            return string.Join(", ", parts);
        }
    }

    private static string FormatTime(TimeOnly t) =>
        t.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string FormatTimes(IReadOnlyList<TimeOnly> times) =>
        string.Join(", ", times.Select(FormatTime));

    private static string FormatOrdinal(Ordinal ordinal) => ordinal switch
    {
        Ordinal.Numeric(var n)      => n.ToString(),
        Ordinal.Last                => "last",
        Ordinal.LastMinus(var o)    => $"last-{o}",
        _                           => throw new InvalidOperationException($"Unknown ordinal: {ordinal}"),
    };

    private static string FormatMonthlyDay(MonthlyDay day) => day switch
    {
        MonthlyDay.Numeric(var n) => n.ToString(),
        MonthlyDay.Last           => "last",
        MonthlyDay.LastBusiness   => "last_business",
        _                         => throw new InvalidOperationException($"Unknown monthly day: {day}"),
    };

    private static string FormatPeriod(Period period) => period switch
    {
        Period.Month   => "month",
        Period.Quarter => "quarter",
        Period.Year    => "year",
        _              => throw new InvalidOperationException($"Unknown period: {period}"),
    };

    private static string FormatDateBound(DateBound bound) => bound switch
    {
        DateBound.FullDate(var d)       => d.ToString("yyyy-MM-dd"),
        DateBound.Annual(var m, var d)  => $"{m:D2}-{d:D2}",
        _                               => throw new InvalidOperationException($"Unknown date bound: {bound}"),
    };

    private static string FormatDateRanges(IReadOnlyList<DateRange> ranges) =>
        string.Join(", ", ranges
            .OrderBy(r => r.Start)
            .Select(r => r.Start == r.End
                ? r.Start.ToString("yyyy-MM-dd")
                : $"{r.Start:yyyy-MM-dd}..{r.End:yyyy-MM-dd}"));

    private static string FormatWeekday(DayOfWeek d) => DayAbbrev(d);

    //
    // Vocabulary helpers
    //

    private static string DayAbbrev(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday    => "mon",
        DayOfWeek.Tuesday   => "tue",
        DayOfWeek.Wednesday => "wed",
        DayOfWeek.Thursday  => "thu",
        DayOfWeek.Friday    => "fri",
        DayOfWeek.Saturday  => "sat",
        DayOfWeek.Sunday    => "sun",
        _                   => throw new InvalidOperationException($"Unknown day: {d}"),
    };

    private static string MonthAbbrev(int month) => month switch
    {
        1  => "jan", 2  => "feb", 3  => "mar", 4  => "apr",
        5  => "may", 6  => "jun", 7  => "jul", 8  => "aug",
        9  => "sep", 10 => "oct", 11 => "nov", 12 => "dec",
        _  => throw new InvalidOperationException($"Invalid month number: {month}"),
    };
}
