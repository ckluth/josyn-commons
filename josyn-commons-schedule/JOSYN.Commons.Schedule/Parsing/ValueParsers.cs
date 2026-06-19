using System.Globalization;

using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Per-value-type parsers for schedule field values.
/// Each method returns a typed <see cref="Result{T}"/> so failures compose cleanly.
/// </summary>
internal static class ValueParsers
{
    //
    // Vocabulary tables
    //
    private static readonly Dictionary<string, DayOfWeek> DayAbbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["mon"] = DayOfWeek.Monday,
            ["tue"] = DayOfWeek.Tuesday,
            ["wed"] = DayOfWeek.Wednesday,
            ["thu"] = DayOfWeek.Thursday,
            ["fri"] = DayOfWeek.Friday,
            ["sat"] = DayOfWeek.Saturday,
            ["sun"] = DayOfWeek.Sunday,
        };

    private static readonly Dictionary<string, int> MonthAbbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["jan"] = 1,  ["feb"] = 2,  ["mar"] = 3,  ["apr"] = 4,
            ["may"] = 5,  ["jun"] = 6,  ["jul"] = 7,  ["aug"] = 8,
            ["sep"] = 9,  ["oct"] = 10, ["nov"] = 11, ["dec"] = 12,
        };

    //
    // Day set
    //
    internal static Result<DaySet> ParseDaySet(string raw)
    {
        var s = raw.Trim();

        // Named keyword shortcuts
        if (s.Equals("weekdays", StringComparison.OrdinalIgnoreCase)) return DaySet.Weekdays;
        if (s.Equals("weekend",  StringComparison.OrdinalIgnoreCase)) return DaySet.Weekend;
        if (s.Equals("daily",    StringComparison.OrdinalIgnoreCase)) return DaySet.Daily;

        var days = new HashSet<DayOfWeek>();
        foreach (var part in s.Split(','))
        {
            var p = part.Trim();
            if (p.Contains(".."))
            {
                var range = ParseDayRange(p);
                if (!range.Succeeded) return range.ToResult<DaySet>();
                foreach (var d in range.Value) days.Add(d);
            }
            else
            {
                var day = ParseSingleDay(p);
                if (!day.Succeeded) return day.ToResult<DaySet>();
                days.Add(day.Value);
            }
        }

        return days.Count > 0
            ? new DaySet(days)
            : Result.Error($"Day expression '{raw}' produced no days.");

        //
        // nested helpers
        //
        static Result<IEnumerable<DayOfWeek>> ParseDayRange(string expr)
        {
            var parts = expr.Split("..");
            if (parts.Length != 2) return Result.Error($"Invalid day range '{expr}'.");

            var from = ParseSingleDay(parts[0].Trim());
            var to   = ParseSingleDay(parts[1].Trim());
            if (!from.Succeeded) return from.ToResult<IEnumerable<DayOfWeek>>();
            return !to.Succeeded 
                ? to.ToResult<IEnumerable<DayOfWeek>>() 
                : Result<IEnumerable<DayOfWeek>>.Success(ExpandDayRange(from.Value, to.Value));
        }

        static IEnumerable<DayOfWeek> ExpandDayRange(DayOfWeek from, DayOfWeek to)
        {
            // Map .NET DayOfWeek (Sun=0) to schedule order (Mon=0 … Sun=6) so
            // "mon..fri" and "sat..sun" expand in natural left-to-right order.
            var start = ToScheduleOrder(from);
            var end   = ToScheduleOrder(to);
            for (var i = start; i <= end; i++)
                yield return FromScheduleOrder(i);
        }

        static int ToScheduleOrder(DayOfWeek d) =>
            d == DayOfWeek.Sunday ? 6 : (int)d - 1;

        static DayOfWeek FromScheduleOrder(int i) =>
            i == 6 ? DayOfWeek.Sunday : (DayOfWeek)(i + 1);
    }

    internal static Result<DayOfWeek> ParseWeekday(string raw) =>
        DayAbbreviations.TryGetValue(raw.Trim(), out var day)
            ? day
            : Result.Error($"Unknown weekday abbreviation '{raw.Trim()}' — expected mon, tue, wed, thu, fri, sat, or sun.");

    private static Result<DayOfWeek> ParseSingleDay(string raw) =>
        DayAbbreviations.TryGetValue(raw.Trim(), out var day)
            ? day
            : Result.Error($"Unknown day abbreviation '{raw.Trim()}'.");

    //
    // Month set
    //
    internal static Result<MonthSet> ParseMonthSet(string raw)
    {
        var months = new HashSet<int>();
        foreach (var part in raw.Trim().Split(','))
        {
            var p = part.Trim();
            if (p.Contains(".."))
            {
                var range = ParseMonthRange(p);
                if (!range.Succeeded) return range.ToResult<MonthSet>();
                foreach (var m in range.Value) months.Add(m);
            }
            else
            {
                var month = ParseSingleMonth(p);
                if (!month.Succeeded) return month.ToResult<MonthSet>();
                months.Add(month.Value);
            }
        }

        return months.Count > 0
            ? new MonthSet(months)
            : Result.Error($"Month expression '{raw}' produced no months.");

        //
        // nested helpers
        //
        static Result<IEnumerable<int>> ParseMonthRange(string expr)
        {
            var parts = expr.Split("..");
            if (parts.Length != 2) return Result.Error($"Invalid month range '{expr}'.");

            var from = ParseSingleMonth(parts[0].Trim());
            var to   = ParseSingleMonth(parts[1].Trim());
            if (!from.Succeeded) return from.ToResult<IEnumerable<int>>();
            if (!to.Succeeded)   return to.ToResult<IEnumerable<int>>();

            return Result<IEnumerable<int>>.Success(
                Enumerable.Range(from.Value, to.Value - from.Value + 1));
        }
    }

    private static Result<int> ParseSingleMonth(string raw) => MonthAbbreviations.TryGetValue(raw.Trim(), out var month) ? month : Result.Error($"Unknown month abbreviation '{raw.Trim()}'.");

    //
    // Time values
    //
    internal static Result<TimeOnly> ParseTimeOnly(string raw)
    {
        var s = raw.Trim();
        return TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            ? t
            : Result.Error($"Invalid time '{s}' — expected HH:mm in 24-hour format.");
    }

    internal static Result<IReadOnlyList<TimeOnly>> ParseTimes(string raw)
    {
        var times = new List<TimeOnly>();
        foreach (var part in raw.Split(','))
        {
            var t = ParseTimeOnly(part.Trim());
            if (!t.Succeeded) return t.ToResult<IReadOnlyList<TimeOnly>>();
            times.Add(t.Value);
        }

        return times.Count > 0
            ? Result<IReadOnlyList<TimeOnly>>.Success(times)
            : Result.Error($"Time list is empty: '{raw}'.");
    }

    //
    // Duration
    //
    internal static Result<Duration> ParseDuration(string raw)
    {
        var s = raw.Trim();

        if (s.EndsWith('m') && int.TryParse(s[..^1], out var mins) && mins >= 1)
            return new Duration(mins, DurationUnit.Minutes);

        if (s.EndsWith('h') && int.TryParse(s[..^1], out var hrs) && hrs >= 1)
            return new Duration(hrs, DurationUnit.Hours);

        return Result.Error($"Invalid duration '{s}' — expected a positive integer followed by 'm' or 'h' " + "(e.g. '30m', '2h'). Compound forms like '1h30m' are not accepted.");
    }

    //
    // Ordinal (nth field)
    //

    internal static Result<Ordinal> ParseOrdinal(string raw)
    {
        var s = raw.Trim();

        if (s.Equals("last", StringComparison.OrdinalIgnoreCase))
            return new Ordinal.Last();

        if (s.StartsWith("last-", StringComparison.OrdinalIgnoreCase))
        {
            var offsetStr = s["last-".Length..];
            return int.TryParse(offsetStr, out var offset) && offset >= 1
                ? new Ordinal.LastMinus(offset)
                : Result.Error($"Invalid ordinal '{s}' — 'last-N' requires a positive integer offset.");
        }

        return int.TryParse(s, out var n) && n is >= 1 and <= 5
            ? new Ordinal.Numeric(n)
            : Result.Error($"Invalid ordinal '{s}' — expected 1–5, 'last', or 'last-N'.");
    }

    //
    // Monthly day (day field)
    //

    internal static Result<MonthlyDay> ParseMonthlyDay(string raw)
    {
        var s = raw.Trim();

        if (s.Equals("last",          StringComparison.OrdinalIgnoreCase)) return new MonthlyDay.Last();
        if (s.Equals("last_business", StringComparison.OrdinalIgnoreCase)) return new MonthlyDay.LastBusiness();

        return int.TryParse(s, out var n) && n >= 1 && n <= 31
            ? new MonthlyDay.Numeric(n)
            : Result.Error($"Invalid day value '{s}' — expected 1–31, 'last', or 'last_business'.");
    }

    //
    // Date boundary (active_from / active_until)
    //

    internal static Result<DateBound> ParseDateBound(string raw)
    {
        var s = raw.Trim();

        if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var full))
            return new DateBound.FullDate(full);

        // MM-DD form: exactly 5 chars, hyphen at position 2
        if (s is [_, _, '-', _, _]
            && int.TryParse(s[..2], out var month) && month >= 1 && month <= 12
            && int.TryParse(s[3..], out var day) && day   >= 1 && day   <= 31)
            return new DateBound.Annual(month, day);

        return Result.Error(
            $"Invalid date boundary '{s}' — expected YYYY-MM-DD (one-time) or MM-DD (annual).");
    }

    //
    // Plain date
    //

    internal static Result<DateOnly> ParseDateOnly(string raw)
    {
        var s = raw.Trim();
        return DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : Result.Error($"Invalid date '{s}' — expected YYYY-MM-DD.");
    }

    //
    // DateTime (once.datetime)
    //

    internal static Result<DateTime> ParseDateTime(string raw)
    {
        var s = raw.Trim();
        return DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : Result.Error($"Invalid datetime '{s}' — expected YYYY-MM-DD HH:mm.");
    }

    //
    // Date ranges (exclude.dates)
    //

    internal static Result<IReadOnlyList<DateRange>> ParseDateRanges(string raw)
    {
        var ranges = new List<DateRange>();
        foreach (var part in raw.Split(','))
        {
            var p = part.Trim();
            if (string.IsNullOrEmpty(p)) continue;

            if (p.Contains(".."))
            {
                var halves = p.Split("..");
                if (halves.Length != 2) return Result.Error($"Invalid date range '{p}'.");

                var from = ParseDateOnly(halves[0].Trim());
                var to   = ParseDateOnly(halves[1].Trim());
                if (!from.Succeeded) return from.ToResult<IReadOnlyList<DateRange>>();
                if (!to.Succeeded)   return to.ToResult<IReadOnlyList<DateRange>>();

                ranges.Add(new DateRange(from.Value, to.Value));
            }
            else
            {
                var d = ParseDateOnly(p);
                if (!d.Succeeded) return d.ToResult<IReadOnlyList<DateRange>>();
                ranges.Add(new DateRange(d.Value, d.Value));
            }
        }

        return ranges.Count > 0
            ? Result<IReadOnlyList<DateRange>>.Success(ranges)
            : Result.Error($"Date range list is empty: '{raw}'.");
    }

    //
    // Positive integer (week_interval.every)
    //

    internal static Result<int> ParsePositiveInt(string raw)
    {
        var s = raw.Trim();
        return int.TryParse(s, out var n) && n >= 1
            ? n
            : Result.Error($"Invalid integer '{s}' — expected a positive integer.");
    }
}
