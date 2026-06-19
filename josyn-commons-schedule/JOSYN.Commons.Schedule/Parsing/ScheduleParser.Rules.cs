using System.Text.Json;

using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

public static partial class ScheduleParser
{
    // ── Rule dispatch ─────────────────────────────────────────────────────────

    private static Result<ScheduleRule> ParseRule(JsonElement obj, int index)
    {
        if (!obj.TryGetProperty("type", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
            return Result.Error($"Rule {index + 1}: required string property \"type\" is missing.");

        return typeProp.GetString()!.ToLowerInvariant() switch
        {
            "interval"      => ParseIntervalRule(obj, index),
            "fixed"         => ParseFixedRule(obj, index),
            "nth_weekday"   => ParseNthWeekdayRule(obj, index),
            "monthly_date"  => ParseMonthlyDateRule(obj, index),
            "week_interval" => ParseWeekIntervalRule(obj, index),
            "once"          => ParseOnceRule(obj, index),
            "exclude"       => ParseExcludeRule(obj, index),
            var t           => Result.Error($"Rule {index + 1}: unknown rule type \"{t}\"."),
        };
    }

    // ── Per-rule-type factories ───────────────────────────────────────────────

    private static Result<ScheduleRule> ParseIntervalRule(JsonElement obj, int index)
    {
        var days   = ParseDaysField(obj, index);               if (!days.Succeeded)   return days.ToResult<ScheduleRule>();
        var start  = RequireTimeString(obj, index, "start");   if (!start.Succeeded)  return start.ToResult<ScheduleRule>();
        var end    = RequireTimeString(obj, index, "end");     if (!end.Succeeded)    return end.ToResult<ScheduleRule>();
        var everyS = RequireString(obj, index, "every");       if (!everyS.Succeeded) return everyS.ToResult<ScheduleRule>();
        var bounds = ParseActiveBoundsJson(obj, index);        if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        var every = ValueParsers.ParseDuration(everyS.Value);
        if (!every.Succeeded) return every.ToResult<ScheduleRule>();

        return new IntervalRule(days.Value, start.Value, end.Value, every.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseFixedRule(JsonElement obj, int index)
    {
        var days   = ParseDaysField(obj, index);          if (!days.Succeeded)   return days.ToResult<ScheduleRule>();
        var times  = ParseTimesField(obj, index);         if (!times.Succeeded)  return times.ToResult<ScheduleRule>();
        var bounds = ParseActiveBoundsJson(obj, index);   if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        return new FixedRule(days.Value, times.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseNthWeekdayRule(JsonElement obj, int index)
    {
        var weekdayS = RequireString(obj, index, "weekday"); if (!weekdayS.Succeeded) return weekdayS.ToResult<ScheduleRule>();
        var periodS  = RequireString(obj, index, "period");  if (!periodS.Succeeded)  return periodS.ToResult<ScheduleRule>();
        var times    = ParseTimesField(obj, index);          if (!times.Succeeded)    return times.ToResult<ScheduleRule>();
        var nth      = ParseNthField(obj, index);            if (!nth.Succeeded)      return nth.ToResult<ScheduleRule>();
        var bounds   = ParseActiveBoundsJson(obj, index);    if (!bounds.Succeeded)   return bounds.ToResult<ScheduleRule>();

        var weekday = ValueParsers.ParseWeekday(weekdayS.Value); if (!weekday.Succeeded) return weekday.ToResult<ScheduleRule>();
        var period  = ParsePeriod(periodS.Value);                if (!period.Succeeded)  return period.ToResult<ScheduleRule>();

        return new NthWeekdayRule(weekday.Value, nth.Value, period.Value, times.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseMonthlyDateRule(JsonElement obj, int index)
    {
        var day    = ParseDayField(obj, index);            if (!day.Succeeded)    return day.ToResult<ScheduleRule>();
        var times  = ParseTimesField(obj, index);          if (!times.Succeeded)  return times.ToResult<ScheduleRule>();
        var months = ParseOptionalMonthsField(obj, index); if (!months.Succeeded) return months.ToResult<ScheduleRule>();
        var bounds = ParseActiveBoundsJson(obj, index);    if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        return new MonthlyDateRule(day.Value, times.Value, months.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseWeekIntervalRule(JsonElement obj, int index)
    {
        var days       = ParseDaysField(obj, index);                   if (!days.Succeeded)       return days.ToResult<ScheduleRule>();
        var times      = ParseTimesField(obj, index);                  if (!times.Succeeded)      return times.ToResult<ScheduleRule>();
        var everyWeeks = RequirePositiveInt(obj, index, "everyWeeks"); if (!everyWeeks.Succeeded) return everyWeeks.ToResult<ScheduleRule>();
        var anchorS    = RequireString(obj, index, "anchor");          if (!anchorS.Succeeded)    return anchorS.ToResult<ScheduleRule>();
        var bounds     = ParseActiveBoundsJson(obj, index);            if (!bounds.Succeeded)     return bounds.ToResult<ScheduleRule>();

        var anchor = ValueParsers.ParseDateOnly(anchorS.Value);
        if (!anchor.Succeeded) return anchor.ToResult<ScheduleRule>();

        return new WeekIntervalRule(days.Value, times.Value, everyWeeks.Value, anchor.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseOnceRule(JsonElement obj, int index)
    {
        var dtS    = RequireString(obj, index, "datetime"); if (!dtS.Succeeded)    return dtS.ToResult<ScheduleRule>();
        var bounds = ParseActiveBoundsJson(obj, index);     if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        var dt = ValueParsers.ParseDateTime(dtS.Value);
        if (!dt.Succeeded) return dt.ToResult<ScheduleRule>();

        return new OnceRule(dt.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseExcludeRule(JsonElement obj, int index)
    {
        var dates = ParseDatesField(obj, index);
        if (!dates.Succeeded) return dates.ToResult<ScheduleRule>();
        return new ExcludeRule(dates.Value);
    }

    // ── Field helpers ─────────────────────────────────────────────────────────

    private static Result<DaySet> ParseDaysField(JsonElement obj, int index)
    {
        if (!obj.TryGetProperty("days", out var prop))
            return Result.Error($"Rule {index + 1}: required property \"days\" is missing.");

        if (prop.ValueKind == JsonValueKind.String)
            return ValueParsers.ParseDaySet(prop.GetString()!);

        if (prop.ValueKind == JsonValueKind.Array)
        {
            var days = new HashSet<DayOfWeek>();
            foreach (var el in prop.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.String)
                    return Result.Error($"Rule {index + 1}: \"days\" array elements must be strings.");
                var day = ValueParsers.ParseWeekday(el.GetString()!);
                if (!day.Succeeded) return day.ToResult<DaySet>();
                days.Add(day.Value);
            }
            return days.Count > 0
                ? new DaySet(days)
                : Result.Error($"Rule {index + 1}: \"days\" array must not be empty.");
        }

        return Result.Error($"Rule {index + 1}: \"days\" must be a shorthand string (\"weekdays\", \"weekend\", \"daily\") or a JSON array of day abbreviations.");
    }

    private static Result<IReadOnlyList<TimeOnly>> ParseTimesField(JsonElement obj, int index)
    {
        if (!obj.TryGetProperty("times", out var prop) || prop.ValueKind != JsonValueKind.Array)
            return Result.Error($"Rule {index + 1}: required array property \"times\" is missing.");

        var times = new List<TimeOnly>();
        foreach (var el in prop.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.String)
                return Result.Error($"Rule {index + 1}: \"times\" array elements must be strings.");
            var t = ValueParsers.ParseTimeOnly(el.GetString()!);
            if (!t.Succeeded) return t.ToResult<IReadOnlyList<TimeOnly>>();
            times.Add(t.Value);
        }

        return times.Count > 0
            ? Result<IReadOnlyList<TimeOnly>>.Success(times)
            : Result.Error($"Rule {index + 1}: \"times\" array must not be empty.");
    }

    private static Result<IReadOnlyList<DateRange>> ParseDatesField(JsonElement obj, int index)
    {
        if (!obj.TryGetProperty("dates", out var prop) || prop.ValueKind != JsonValueKind.Array)
            return Result.Error($"Rule {index + 1}: required array property \"dates\" is missing.");

        var ranges = new List<DateRange>();
        foreach (var el in prop.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.String)
            {
                var d = ValueParsers.ParseDateOnly(el.GetString()!);
                if (!d.Succeeded) return d.ToResult<IReadOnlyList<DateRange>>();
                ranges.Add(new DateRange(d.Value, d.Value));
            }
            else if (el.ValueKind == JsonValueKind.Object)
            {
                // { "from": "YYYY-MM-DD", "to": "YYYY-MM-DD" }
                if (!el.TryGetProperty("from", out var fromEl) || fromEl.ValueKind != JsonValueKind.String)
                    return Result.Error($"Rule {index + 1}: date range object is missing a \"from\" string property.");
                if (!el.TryGetProperty("to", out var toEl) || toEl.ValueKind != JsonValueKind.String)
                    return Result.Error($"Rule {index + 1}: date range object is missing a \"to\" string property.");

                var from = ValueParsers.ParseDateOnly(fromEl.GetString()!);
                var to   = ValueParsers.ParseDateOnly(toEl.GetString()!);
                if (!from.Succeeded) return from.ToResult<IReadOnlyList<DateRange>>();
                if (!to.Succeeded)   return to.ToResult<IReadOnlyList<DateRange>>();
                ranges.Add(new DateRange(from.Value, to.Value));
            }
            else
            {
                return Result.Error($"Rule {index + 1}: \"dates\" entries must be ISO date strings or {{\"from\":...,\"to\":...}} objects.");
            }
        }

        return ranges.Count > 0
            ? Result<IReadOnlyList<DateRange>>.Success(ranges)
            : Result.Error($"Rule {index + 1}: \"dates\" array must not be empty.");
    }

    private static Result<MonthSet?> ParseOptionalMonthsField(JsonElement obj, int index)
    {
        if (!obj.TryGetProperty("months", out var prop))
            return Result<MonthSet?>.Success(null);

        if (prop.ValueKind != JsonValueKind.Array)
            return Result.Error($"Rule {index + 1}: \"months\" must be an array of month abbreviations.");

        var months = new HashSet<int>();
        foreach (var el in prop.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.String)
                return Result.Error($"Rule {index + 1}: \"months\" array elements must be strings.");
            var m = ValueParsers.ParseSingleMonth(el.GetString()!);
            if (!m.Succeeded) return m.ToResult<MonthSet?>();
            months.Add(m.Value);
        }

        return months.Count > 0
            ? Result<MonthSet?>.Success(new MonthSet(months))
            : Result.Error($"Rule {index + 1}: \"months\" array must not be empty.");
    }

    private static Result<Ordinal> ParseNthField(JsonElement obj, int index)
    {
        if (!obj.TryGetProperty("nth", out var prop))
            return Result.Error($"Rule {index + 1}: required property \"nth\" is missing.");

        if (prop.ValueKind == JsonValueKind.Number)
        {
            if (!prop.TryGetInt32(out var n) || n < 1 || n > 5)
                return Result.Error($"Rule {index + 1}: \"nth\" as an integer must be 1–5.");
            return new Ordinal.Numeric(n);
        }

        if (prop.ValueKind == JsonValueKind.String)
            return ValueParsers.ParseOrdinal(prop.GetString()!);

        return Result.Error($"Rule {index + 1}: \"nth\" must be an integer (1–5) or a string (\"last\", \"last-N\").");
    }

    private static Result<MonthlyDay> ParseDayField(JsonElement obj, int index)
    {
        if (!obj.TryGetProperty("day", out var prop))
            return Result.Error($"Rule {index + 1}: required property \"day\" is missing.");

        if (prop.ValueKind == JsonValueKind.Number)
        {
            if (!prop.TryGetInt32(out var n) || n < 1 || n > 31)
                return Result.Error($"Rule {index + 1}: \"day\" as an integer must be 1–31.");
            return new MonthlyDay.Numeric(n);
        }

        if (prop.ValueKind == JsonValueKind.String)
            return ValueParsers.ParseMonthlyDay(prop.GetString()!);

        return Result.Error($"Rule {index + 1}: \"day\" must be an integer (1–31) or a string (\"last\", \"last_business\").");
    }

    private static Result<(DateBound? From, DateBound? Until)> ParseActiveBoundsJson(JsonElement obj, int index)
    {
        var from  = ParseOptionalDateBound(obj, index, "activeFrom");
        var until = ParseOptionalDateBound(obj, index, "activeUntil");
        if (!from.Succeeded)  return from.ToResult<(DateBound?, DateBound?)>();
        if (!until.Succeeded) return until.ToResult<(DateBound?, DateBound?)>();
        return (from.Value, until.Value);

        //
        // nested helper
        //
        static Result<DateBound?> ParseOptionalDateBound(JsonElement o, int i, string key)
        {
            if (!o.TryGetProperty(key, out var prop) || prop.ValueKind == JsonValueKind.Null)
                return Result<DateBound?>.Success(null);
            if (prop.ValueKind != JsonValueKind.String)
                return Result.Error($"Rule {i + 1}: \"{key}\" must be a string.");
            var result = ValueParsers.ParseDateBound(prop.GetString()!);
            return result.Succeeded
                ? Result<DateBound?>.Success(result.Value)
                : result.ToResult<DateBound?>();
        }
    }

    private static Result<string> RequireString(JsonElement obj, int index, string key)
    {
        if (!obj.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.String)
            return Result.Error($"Rule {index + 1}: required string property \"{key}\" is missing.");
        return prop.GetString()!;
    }

    private static Result<TimeOnly> RequireTimeString(JsonElement obj, int index, string key)
    {
        var raw = RequireString(obj, index, key);
        if (!raw.Succeeded) return raw.ToResult<TimeOnly>();
        return ValueParsers.ParseTimeOnly(raw.Value);
    }

    private static Result<int> RequirePositiveInt(JsonElement obj, int index, string key)
    {
        if (!obj.TryGetProperty(key, out var prop))
            return Result.Error($"Rule {index + 1}: required property \"{key}\" is missing.");
        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out var n) || n < 1)
            return Result.Error($"Rule {index + 1}: \"{key}\" must be a positive integer.");
        return n;
    }

    private static Result<Period> ParsePeriod(string raw) =>
        raw.Trim().ToLowerInvariant() switch
        {
            "month"   => Period.Month,
            "quarter" => Period.Quarter,
            "year"    => Period.Year,
            var p     => Result.Error($"Unknown period \"{p}\" — expected \"month\", \"quarter\", or \"year\"."),
        };
}
