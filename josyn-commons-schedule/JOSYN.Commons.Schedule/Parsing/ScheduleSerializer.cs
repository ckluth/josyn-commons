using System.Globalization;
using System.Text;
using System.Text.Json;

using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Serializes a <see cref="ScheduleDefinition"/> to its JSON text representation.
/// Comments are operator-authored and are not round-tripped — the output is standard JSON.
/// </summary>
/// <inheritdoc cref="IScheduleSerializer"/>
public static class ScheduleSerializer
{
    /// <inheritdoc cref="IScheduleSerializer.Serialize"/>
    public static Result<string> Serialize(ScheduleDefinition definition)
    {
        try
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartArray();
                foreach (var rule in definition.Rules)
                    WriteRule(writer, rule);
                writer.WriteEndArray();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (Exception ex) { return ex; }
    }

    /// <summary>Used internally by <see cref="ScheduleValidator"/> for duplicate detection.</summary>
    internal static string SerializeRuleText(ScheduleRule rule)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            WriteRule(writer, rule);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    // ── Rule dispatch ─────────────────────────────────────────────────────────

    private static void WriteRule(Utf8JsonWriter writer, ScheduleRule rule)
    {
        writer.WriteStartObject();
        switch (rule)
        {
            case IntervalRule     r: WriteInterval(writer, r);     break;
            case FixedRule        r: WriteFixed(writer, r);        break;
            case NthWeekdayRule   r: WriteNthWeekday(writer, r);   break;
            case MonthlyDateRule  r: WriteMonthlyDate(writer, r);  break;
            case WeekIntervalRule r: WriteWeekInterval(writer, r); break;
            case OnceRule         r: WriteOnce(writer, r);         break;
            case ExcludeRule      r: WriteExclude(writer, r);      break;
            default: throw new InvalidOperationException($"Unknown rule type: {rule.GetType().Name}");
        }
        writer.WriteEndObject();
    }

    // ── Per-rule-type writers ─────────────────────────────────────────────────

    private static void WriteInterval(Utf8JsonWriter writer, IntervalRule r)
    {
        writer.WriteString("type",  "interval");
        WriteDaysField(writer, r.Days);
        writer.WriteString("start", FormatTime(r.Start));
        writer.WriteString("end",   FormatTime(r.End));
        writer.WriteString("every", r.Every.ToString());
        WriteBounds(writer, r);
    }

    private static void WriteFixed(Utf8JsonWriter writer, FixedRule r)
    {
        writer.WriteString("type", "fixed");
        WriteDaysField(writer, r.Days);
        WriteTimesArray(writer, r.Times);
        WriteBounds(writer, r);
    }

    private static void WriteNthWeekday(Utf8JsonWriter writer, NthWeekdayRule r)
    {
        writer.WriteString("type",    "nth_weekday");
        writer.WriteString("weekday", DayAbbrev(r.Weekday));
        WriteNthField(writer, r.Nth);
        writer.WriteString("period",  FormatPeriod(r.SchedulePeriod));
        WriteTimesArray(writer, r.Times);
        WriteBounds(writer, r);
    }

    private static void WriteMonthlyDate(Utf8JsonWriter writer, MonthlyDateRule r)
    {
        writer.WriteString("type", "monthly_date");
        WriteDayField(writer, r.Day);
        WriteTimesArray(writer, r.Times);
        if (r.Months is not null) WriteMonthsArray(writer, r.Months);
        WriteBounds(writer, r);
    }

    private static void WriteWeekInterval(Utf8JsonWriter writer, WeekIntervalRule r)
    {
        writer.WriteString("type", "week_interval");
        WriteDaysField(writer, r.Days);
        WriteTimesArray(writer, r.Times);
        writer.WriteNumber("everyWeeks", r.EveryWeeks);
        writer.WriteString("anchor",     r.Anchor.ToString("yyyy-MM-dd"));
        WriteBounds(writer, r);
    }

    private static void WriteOnce(Utf8JsonWriter writer, OnceRule r)
    {
        writer.WriteString("type",     "once");
        writer.WriteString("datetime", r.FireAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        WriteBounds(writer, r);
    }

    private static void WriteExclude(Utf8JsonWriter writer, ExcludeRule r)
    {
        writer.WriteString("type", "exclude");
        writer.WriteStartArray("dates");
        foreach (var range in r.Dates.OrderBy(d => d.Start))
        {
            if (range.Start == range.End)
                writer.WriteStringValue(range.Start.ToString("yyyy-MM-dd"));
            else
            {
                writer.WriteStartObject();
                writer.WriteString("from", range.Start.ToString("yyyy-MM-dd"));
                writer.WriteString("to",   range.End.ToString("yyyy-MM-dd"));
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();
    }

    // ── Field writers ─────────────────────────────────────────────────────────

    private static void WriteDaysField(Utf8JsonWriter writer, DaySet days)
    {
        // Emit the named shorthand when the set matches exactly.
        if (days.Equals(DaySet.Weekdays)) { writer.WriteString("days", "weekdays"); return; }
        if (days.Equals(DaySet.Weekend))  { writer.WriteString("days", "weekend");  return; }
        if (days.Equals(DaySet.Daily))    { writer.WriteString("days", "daily");    return; }

        // Custom set: sorted Mon→Sun.
        writer.WriteStartArray("days");
        foreach (var day in days.Days.OrderBy(d => d == DayOfWeek.Sunday ? 6 : (int)d - 1))
            writer.WriteStringValue(DayAbbrev(day));
        writer.WriteEndArray();
    }

    private static void WriteTimesArray(Utf8JsonWriter writer, IReadOnlyList<TimeOnly> times)
    {
        writer.WriteStartArray("times");
        foreach (var t in times)
            writer.WriteStringValue(FormatTime(t));
        writer.WriteEndArray();
    }

    private static void WriteMonthsArray(Utf8JsonWriter writer, MonthSet months)
    {
        writer.WriteStartArray("months");
        foreach (var m in months.Months.OrderBy(x => x))
            writer.WriteStringValue(MonthAbbrev(m));
        writer.WriteEndArray();
    }

    private static void WriteNthField(Utf8JsonWriter writer, Ordinal ordinal)
    {
        switch (ordinal)
        {
            case Ordinal.Numeric(var n):   writer.WriteNumber("nth", n);             break;
            case Ordinal.Last:             writer.WriteString("nth", "last");        break;
            case Ordinal.LastMinus(var o): writer.WriteString("nth", $"last-{o}");  break;
            default: throw new InvalidOperationException($"Unknown ordinal: {ordinal}");
        }
    }

    private static void WriteDayField(Utf8JsonWriter writer, MonthlyDay day)
    {
        switch (day)
        {
            case MonthlyDay.Numeric(var n): writer.WriteNumber("day", n);                break;
            case MonthlyDay.Last:           writer.WriteString("day", "last");           break;
            case MonthlyDay.LastBusiness:   writer.WriteString("day", "last_business");  break;
            default: throw new InvalidOperationException($"Unknown monthly day: {day}");
        }
    }

    private static void WriteBounds(Utf8JsonWriter writer, BoundedRule rule)
    {
        if (rule.ActiveFrom  is not null) writer.WriteString("activeFrom",  FormatDateBound(rule.ActiveFrom));
        if (rule.ActiveUntil is not null) writer.WriteString("activeUntil", FormatDateBound(rule.ActiveUntil));
    }

    // ── Value formatters ──────────────────────────────────────────────────────

    private static string FormatTime(TimeOnly t) =>
        t.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string FormatPeriod(Period period) => period switch
    {
        Period.Month   => "month",
        Period.Quarter => "quarter",
        Period.Year    => "year",
        _              => throw new InvalidOperationException($"Unknown period: {period}"),
    };

    private static string FormatDateBound(DateBound bound) => bound switch
    {
        DateBound.FullDate(var d)      => d.ToString("yyyy-MM-dd"),
        DateBound.Annual(var m, var d) => $"{m:D2}-{d:D2}",
        _                              => throw new InvalidOperationException($"Unknown date bound: {bound}"),
    };

    // ── Vocabulary helpers ────────────────────────────────────────────────────

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
