using System.Globalization;

using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Leaf string parsers for schedule field values.
/// Compound-value parsing (arrays, rule objects) is handled by the JSON layer in
/// <see cref="ScheduleParser"/>. Each method here parses a single atomic string token.
/// </summary>
internal static class ValueParsers
{
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

    // ── Day field ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses the string form of the <c>days</c> field: the three named shortcuts
    /// ("weekdays", "weekend", "daily"). For a custom set, the caller iterates a
    /// JSON array and calls <see cref="ParseWeekday"/> per element.
    /// </summary>
    internal static Result<DaySet> ParseDaySet(string raw)
    {
        var s = raw.Trim();
        if (s.Equals("weekdays", StringComparison.OrdinalIgnoreCase)) return DaySet.Weekdays;
        if (s.Equals("weekend",  StringComparison.OrdinalIgnoreCase)) return DaySet.Weekend;
        if (s.Equals("daily",    StringComparison.OrdinalIgnoreCase)) return DaySet.Daily;
        return Result.Error(
            $"Unknown day shorthand \"{s}\" — expected \"weekdays\", \"weekend\", or \"daily\". " +
            "For a custom day set use a JSON array: [\"mon\", \"wed\", \"fri\"].");
    }

    internal static Result<DayOfWeek> ParseWeekday(string raw) =>
        DayAbbreviations.TryGetValue(raw.Trim(), out var day)
            ? day
            : Result.Error($"Unknown weekday abbreviation \"{raw.Trim()}\" — expected mon, tue, wed, thu, fri, sat, or sun.");

    internal static Result<int> ParseSingleMonth(string raw) =>
        MonthAbbreviations.TryGetValue(raw.Trim(), out var month)
            ? month
            : Result.Error($"Unknown month abbreviation \"{raw.Trim()}\".");

    // ── Time values ───────────────────────────────────────────────────────────

    internal static Result<TimeOnly> ParseTimeOnly(string raw)
    {
        var s = raw.Trim();
        return TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            ? t
            : Result.Error($"Invalid time \"{s}\" — expected HH:mm in 24-hour format.");
    }

    // ── Duration ──────────────────────────────────────────────────────────────

    internal static Result<Duration> ParseDuration(string raw)
    {
        var s = raw.Trim();

        if (s.EndsWith('m') && int.TryParse(s[..^1], out var mins) && mins >= 1)
            return new Duration(mins, DurationUnit.Minutes);

        if (s.EndsWith('h') && int.TryParse(s[..^1], out var hrs) && hrs >= 1)
            return new Duration(hrs, DurationUnit.Hours);

        return Result.Error(
            $"Invalid duration \"{s}\" — expected a positive integer followed by 'm' or 'h' " +
            "(e.g. \"30m\", \"2h\"). Compound forms like \"1h30m\" are not accepted.");
    }

    // ── Ordinal (nth field — string form) ─────────────────────────────────────

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
                : Result.Error($"Invalid ordinal \"{s}\" — 'last-N' requires a positive integer offset.");
        }

        // Accept numeric strings for robustness (e.g. "2" in addition to the integer form).
        return int.TryParse(s, out var n) && n is >= 1 and <= 5
            ? new Ordinal.Numeric(n)
            : Result.Error($"Invalid ordinal \"{s}\" — expected 1–5, \"last\", or \"last-N\".");
    }

    // ── Monthly day (day field — string form) ─────────────────────────────────

    internal static Result<MonthlyDay> ParseMonthlyDay(string raw)
    {
        var s = raw.Trim();
        if (s.Equals("last",          StringComparison.OrdinalIgnoreCase)) return new MonthlyDay.Last();
        if (s.Equals("last_business", StringComparison.OrdinalIgnoreCase)) return new MonthlyDay.LastBusiness();
        return Result.Error($"Invalid day string \"{s}\" — expected \"last\" or \"last_business\".");
    }

    // ── Date boundary (activeFrom / activeUntil) ──────────────────────────────

    internal static Result<DateBound> ParseDateBound(string raw)
    {
        var s = raw.Trim();

        if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var full))
            return new DateBound.FullDate(full);

        // MM-DD form: exactly 5 chars, hyphen at position 2
        if (s is [_, _, '-', _, _]
            && int.TryParse(s[..2], out var month) && month >= 1 && month <= 12
            && int.TryParse(s[3..], out var day)   && day   >= 1 && day   <= 31)
            return new DateBound.Annual(month, day);

        return Result.Error(
            $"Invalid date boundary \"{s}\" — expected YYYY-MM-DD (one-time) or MM-DD (annual).");
    }

    // ── Plain date ────────────────────────────────────────────────────────────

    internal static Result<DateOnly> ParseDateOnly(string raw)
    {
        var s = raw.Trim();
        return DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : Result.Error($"Invalid date \"{s}\" — expected YYYY-MM-DD.");
    }

    // ── DateTime (once.datetime) ──────────────────────────────────────────────

    internal static Result<DateTime> ParseDateTime(string raw)
    {
        var s = raw.Trim();
        return DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : Result.Error($"Invalid datetime \"{s}\" — expected YYYY-MM-DD HH:mm.");
    }

    // ── Positive integer (everyWeeks) ─────────────────────────────────────────

    internal static Result<int> ParsePositiveInt(string raw)
    {
        var s = raw.Trim();
        return int.TryParse(s, out var n) && n >= 1
            ? n
            : Result.Error($"Invalid integer \"{s}\" — expected a positive integer.");
    }
}
