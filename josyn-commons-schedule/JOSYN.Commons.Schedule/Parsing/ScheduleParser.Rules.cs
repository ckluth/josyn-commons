using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

public static partial class ScheduleParser
{
    //
    // Rule dispatch
    //
    private static Result<ScheduleRule> ParseRule(IniBlock block)
    {
        var typeResult = block.Require("type");
        if (!typeResult.Succeeded) return typeResult.ToResult<ScheduleRule>();

        return typeResult.Value.Trim().ToLowerInvariant() switch
        {
            "interval" => ParseIntervalRule(block),
            "fixed" => ParseFixedRule(block),
            "nth_weekday" => ParseNthWeekdayRule(block),
            "monthly_date" => ParseMonthlyDateRule(block),
            "week_interval" => ParseWeekIntervalRule(block),
            "once" => ParseOnceRule(block),
            "exclude" => ParseExcludeRule(block),
            var t => Result.Error($"Block {block.BlockIndex + 1}: unknown rule type '{t}'."),
        };
    }

    //
    // Per-rule-type factories
    //

    private static Result<ScheduleRule> ParseIntervalRule(IniBlock block)
    {
        var daysRaw = block.Require("days"); if (!daysRaw.Succeeded) return daysRaw.ToResult<ScheduleRule>();
        var startRaw = block.Require("start"); if (!startRaw.Succeeded) return startRaw.ToResult<ScheduleRule>();
        var endRaw = block.Require("end"); if (!endRaw.Succeeded) return endRaw.ToResult<ScheduleRule>();
        var everyRaw = block.Require("every"); if (!everyRaw.Succeeded) return everyRaw.ToResult<ScheduleRule>();

        var days = ValueParsers.ParseDaySet(daysRaw.Value); if (!days.Succeeded) return days.ToResult<ScheduleRule>();
        var start = ValueParsers.ParseTimeOnly(startRaw.Value); if (!start.Succeeded) return start.ToResult<ScheduleRule>();
        var end = ValueParsers.ParseTimeOnly(endRaw.Value); if (!end.Succeeded) return end.ToResult<ScheduleRule>();
        var every = ValueParsers.ParseDuration(everyRaw.Value); if (!every.Succeeded) return every.ToResult<ScheduleRule>();
        var bounds = ParseActiveBounds(block); if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        return new IntervalRule(days.Value, start.Value, end.Value, every.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseFixedRule(IniBlock block)
    {
        var daysRaw = block.Require("days"); if (!daysRaw.Succeeded) return daysRaw.ToResult<ScheduleRule>();
        var timesRaw = block.Require("time"); if (!timesRaw.Succeeded) return timesRaw.ToResult<ScheduleRule>();

        var days = ValueParsers.ParseDaySet(daysRaw.Value); if (!days.Succeeded) return days.ToResult<ScheduleRule>();
        var times = ValueParsers.ParseTimes(timesRaw.Value); if (!times.Succeeded) return times.ToResult<ScheduleRule>();
        var bounds = ParseActiveBounds(block); if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        return new FixedRule(days.Value, times.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseNthWeekdayRule(IniBlock block)
    {
        var weekdayRaw = block.Require("weekday"); if (!weekdayRaw.Succeeded) return weekdayRaw.ToResult<ScheduleRule>();
        var nthRaw = block.Require("nth"); if (!nthRaw.Succeeded) return nthRaw.ToResult<ScheduleRule>();
        var periodRaw = block.Require("period"); if (!periodRaw.Succeeded) return periodRaw.ToResult<ScheduleRule>();
        var timesRaw = block.Require("time"); if (!timesRaw.Succeeded) return timesRaw.ToResult<ScheduleRule>();

        var weekday = ValueParsers.ParseWeekday(weekdayRaw.Value); if (!weekday.Succeeded) return weekday.ToResult<ScheduleRule>();
        var nth = ValueParsers.ParseOrdinal(nthRaw.Value); if (!nth.Succeeded) return nth.ToResult<ScheduleRule>();
        var period = ParsePeriod(periodRaw.Value); if (!period.Succeeded) return period.ToResult<ScheduleRule>();
        var times = ValueParsers.ParseTimes(timesRaw.Value); if (!times.Succeeded) return times.ToResult<ScheduleRule>();
        var bounds = ParseActiveBounds(block); if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        return new NthWeekdayRule(weekday.Value, nth.Value, period.Value, times.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseMonthlyDateRule(IniBlock block)
    {
        var dayRaw = block.Require("day"); if (!dayRaw.Succeeded) return dayRaw.ToResult<ScheduleRule>();
        var timesRaw = block.Require("time"); if (!timesRaw.Succeeded) return timesRaw.ToResult<ScheduleRule>();

        var day = ValueParsers.ParseMonthlyDay(dayRaw.Value); if (!day.Succeeded) return day.ToResult<ScheduleRule>();
        var times = ValueParsers.ParseTimes(timesRaw.Value); if (!times.Succeeded) return times.ToResult<ScheduleRule>();
        var bounds = ParseActiveBounds(block); if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        // months are optional — null means "every month"
        MonthSet? months = null;
        var monthsRaw = block.Optional("months");
        // ReSharper disable once InvertIf
        if (monthsRaw is not null)
        {
            var monthsParsed = ValueParsers.ParseMonthSet(monthsRaw);
            if (!monthsParsed.Succeeded) return monthsParsed.ToResult<ScheduleRule>();
            months = monthsParsed.Value;
        }

        return new MonthlyDateRule(day.Value, times.Value, months, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseWeekIntervalRule(IniBlock block)
    {
        var daysRaw = block.Require("days"); if (!daysRaw.Succeeded) return daysRaw.ToResult<ScheduleRule>();
        var timesRaw = block.Require("time"); if (!timesRaw.Succeeded) return timesRaw.ToResult<ScheduleRule>();
        var everyRaw = block.Require("every"); if (!everyRaw.Succeeded) return everyRaw.ToResult<ScheduleRule>();
        var anchorRaw = block.Require("anchor"); if (!anchorRaw.Succeeded) return anchorRaw.ToResult<ScheduleRule>();

        var days = ValueParsers.ParseDaySet(daysRaw.Value); if (!days.Succeeded) return days.ToResult<ScheduleRule>();
        var times = ValueParsers.ParseTimes(timesRaw.Value); if (!times.Succeeded) return times.ToResult<ScheduleRule>();
        var every = ValueParsers.ParsePositiveInt(everyRaw.Value); if (!every.Succeeded) return every.ToResult<ScheduleRule>();
        var anchor = ValueParsers.ParseDateOnly(anchorRaw.Value); if (!anchor.Succeeded) return anchor.ToResult<ScheduleRule>();
        var bounds = ParseActiveBounds(block); if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        return new WeekIntervalRule(days.Value, times.Value, every.Value, anchor.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseOnceRule(IniBlock block)
    {
        var dtRaw = block.Require("datetime"); if (!dtRaw.Succeeded) return dtRaw.ToResult<ScheduleRule>();
        var dt = ValueParsers.ParseDateTime(dtRaw.Value); if (!dt.Succeeded) return dt.ToResult<ScheduleRule>();
        var bounds = ParseActiveBounds(block); if (!bounds.Succeeded) return bounds.ToResult<ScheduleRule>();

        return new OnceRule(dt.Value, bounds.Value.From, bounds.Value.Until);
    }

    private static Result<ScheduleRule> ParseExcludeRule(IniBlock block)
    {
        var datesRaw = block.Require("dates"); if (!datesRaw.Succeeded) return datesRaw.ToResult<ScheduleRule>();
        var dates = ValueParsers.ParseDateRanges(datesRaw.Value); if (!dates.Succeeded) return dates.ToResult<ScheduleRule>();

        return new ExcludeRule(dates.Value);
    }

    //
    // Shared helpers
    //
    
    //
    // Parse the optional active_from / active_until pair present on any BoundedRule.
    //
    private static Result<(DateBound? From, DateBound? Until)> ParseActiveBounds(IniBlock block)
    {
        var from = ParseOptionalBound(block, "active_from");
        var until = ParseOptionalBound(block, "active_until");
        if (!from.Succeeded) return from.ToResult<(DateBound? From, DateBound? Until)>();
        if (!until.Succeeded) return until.ToResult<(DateBound? From, DateBound? Until)>();
        return (from.Value, until.Value);

        //
        // nested helper
        //
        static Result<DateBound?> ParseOptionalBound(IniBlock b, string key)
        {
            var raw = b.Optional(key);
            if (raw is null) return Result<DateBound?>.Success(null);
            var result = ValueParsers.ParseDateBound(raw);
            return result.Succeeded
                ? Result<DateBound?>.Success(result.Value)
                : result.ToResult<DateBound?>();
        }
    }

    private static Result<Period> ParsePeriod(string raw)
    {
        return raw.Trim().ToLowerInvariant() switch
        {
            "month" => Period.Month,
            "quarter" => Period.Quarter,
            "year" => Period.Year,
            var p => Result.Error($"Unknown period '{p}' — expected month, quarter, or year."),
        };
    }
}
