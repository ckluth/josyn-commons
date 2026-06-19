using NUnit.Framework;

using JOSYN.Commons.Schedule;

namespace JOSYN.Commons.Schedule.Test;

/// <summary>
/// Integration-level tests for <see cref="ScheduleParser.Parse"/>.
/// </summary>
[TestFixture]
public sealed class ScheduleParserTests
{
    //
    // Happy path — one test per rule type
    //

    [Test]
    public void Parse_IntervalRule_Succeeds()
    {
        const string ini = """
            type  = interval
            days  = weekdays
            start = 08:00
            end   = 17:00
            every = 30m
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Rules, Has.Count.EqualTo(1));

        var rule = (IntervalRule)r.Value.Rules[0];
        Assert.That(rule.Days.Equals(DaySet.Weekdays), Is.True);
        Assert.That(rule.Start, Is.EqualTo(new TimeOnly(8, 0)));
        Assert.That(rule.End,   Is.EqualTo(new TimeOnly(17, 0)));
        Assert.That(rule.Every, Is.EqualTo(new Duration(30, DurationUnit.Minutes)));
        Assert.That(rule.ActiveFrom,  Is.Null);
        Assert.That(rule.ActiveUntil, Is.Null);
    }

    [Test]
    public void Parse_FixedRule_Succeeds()
    {
        const string ini = """
            type = fixed
            days = mon, wed, fri
            time = 06:00, 06:30
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (FixedRule)r.Value!.Rules[0];
        Assert.That(rule.Days.Days, Is.EquivalentTo(new[]
            { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }));
        Assert.That(rule.Times, Has.Count.EqualTo(2));
        Assert.That(rule.Times[0], Is.EqualTo(new TimeOnly(6, 0)));
        Assert.That(rule.Times[1], Is.EqualTo(new TimeOnly(6, 30)));
    }

    [Test]
    public void Parse_NthWeekdayRule_Succeeds()
    {
        const string ini = """
            type    = nth_weekday
            weekday = tue
            nth     = 2
            period  = month
            time    = 09:00
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (NthWeekdayRule)r.Value!.Rules[0];
        Assert.That(rule.Weekday,        Is.EqualTo(DayOfWeek.Tuesday));
        Assert.That(rule.Nth,            Is.EqualTo(new Ordinal.Numeric(2)));
        Assert.That(rule.SchedulePeriod, Is.EqualTo(Period.Month));
        Assert.That(rule.Times[0],       Is.EqualTo(new TimeOnly(9, 0)));
    }

    [Test]
    public void Parse_NthWeekdayRule_LastOrdinal_Succeeds()
    {
        const string ini = """
            type    = nth_weekday
            weekday = fri
            nth     = last
            period  = month
            time    = 16:00
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(((NthWeekdayRule)r.Value!.Rules[0]).Nth, Is.InstanceOf<Ordinal.Last>());
    }

    [Test]
    public void Parse_MonthlyDateRule_WithoutMonths_Succeeds()
    {
        const string ini = """
            type = monthly_date
            day  = 15
            time = 08:00
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (MonthlyDateRule)r.Value!.Rules[0];
        Assert.That(rule.Day,    Is.EqualTo(new MonthlyDay.Numeric(15)));
        Assert.That(rule.Months, Is.Null);
    }

    [Test]
    public void Parse_MonthlyDateRule_WithMonths_ParsesMonthSet()
    {
        const string ini = """
            type   = monthly_date
            day    = 1
            time   = 09:00
            months = jan, jul
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (MonthlyDateRule)r.Value!.Rules[0];
        Assert.That(rule.Months, Is.Not.Null);
        Assert.That(rule.Months!.Months, Is.EquivalentTo([1, 7]));
    }

    [Test]
    public void Parse_WeekIntervalRule_Succeeds()
    {
        const string ini = """
            type   = week_interval
            days   = fri
            time   = 08:00
            every  = 2
            anchor = 2026-01-02
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (WeekIntervalRule)r.Value!.Rules[0];
        Assert.That(rule.Every,  Is.EqualTo(2));
        Assert.That(rule.Anchor, Is.EqualTo(new DateOnly(2026, 1, 2)));
    }

    [Test]
    public void Parse_OnceRule_Succeeds()
    {
        const string ini = """
            type     = once
            datetime = 2026-12-26 10:00
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (OnceRule)r.Value!.Rules[0];
        Assert.That(rule.FireAt, Is.EqualTo(new DateTime(2026, 12, 26, 10, 0, 0)));
    }

    [Test]
    public void Parse_ExcludeRule_Succeeds()
    {
        const string ini = """
            type  = exclude
            dates = 2026-12-24, 2026-12-25, 2026-12-27..2026-12-31
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (ExcludeRule)r.Value!.Rules[0];
        Assert.That(rule.Dates, Has.Count.EqualTo(3));
    }

    //
    // Active bounds
    //

    [Test]
    public void Parse_ActiveBounds_FullDates_Parsed()
    {
        const string ini = """
            type         = interval
            days         = weekdays
            start        = 08:00
            end          = 17:00
            every        = 30m
            active_from  = 2026-04-01
            active_until = 2026-09-30
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (IntervalRule)r.Value!.Rules[0];
        Assert.That(rule.ActiveFrom,  Is.InstanceOf<DateBound.FullDate>());
        Assert.That(rule.ActiveUntil, Is.InstanceOf<DateBound.FullDate>());
    }

    [Test]
    public void Parse_ActiveBounds_AnnualDates_Parsed()
    {
        const string ini = """
            type         = interval
            days         = weekdays
            start        = 08:00
            end          = 17:00
            every        = 30m
            active_from  = 04-01
            active_until = 09-30
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (IntervalRule)r.Value!.Rules[0];
        Assert.That(rule.ActiveFrom,  Is.InstanceOf<DateBound.Annual>());
        Assert.That(rule.ActiveUntil, Is.InstanceOf<DateBound.Annual>());
    }

    //
    // Preprocessing
    //

    [Test]
    public void Parse_HashComments_Stripped()
    {
        const string ini = """
            # This is a comment
            type  = interval  # inline comment
            days  = weekdays
            start = 08:00
            end   = 17:00
            every = 30m
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Rules, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_SemicolonComments_Stripped()
    {
        const string ini = """
            type  = interval ; inline comment
            days  = weekdays
            start = 08:00
            end   = 17:00
            every = 30m
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);
    }

    [Test]
    public void Parse_MultiLineContinuation_JoinsValue()
    {
        // The dates value wraps onto a continuation line.
        const string ini =
            "type  = exclude\n" +
            "dates = 2026-12-24, 2026-12-25,\n" +
            "        2026-12-27..2026-12-31\n";

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);

        var rule = (ExcludeRule)r.Value!.Rules[0];
        Assert.That(rule.Dates, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_MultipleBlocks_ReturnsAllRules()
    {
        const string ini = """
            type = fixed
            days = mon
            time = 09:00

            type = fixed
            days = tue
            time = 10:00

            type = exclude
            dates = 2026-12-25
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Rules, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_EmptyInput_ReturnsEmptyDefinition()
    {
        var r = ScheduleParser.Parse(string.Empty);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Rules, Is.Empty);
    }

    //
    // Error handling
    //

    [Test]
    public void Parse_MissingTypeKey_Fails()
    {
        const string ini = """
            days = weekdays
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_UnknownType_Fails()
    {
        const string ini = """
            type = unknown_type
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_MissingRequiredKey_Fails()
    {
        // interval without 'end'
        const string ini = """
            type  = interval
            days  = weekdays
            start = 08:00
            every = 30m
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_MultipleBlockErrors_AllReportedInOneMessage()
    {
        // Two broken blocks — the error message should mention both.
        const string ini = """
            type = unknown_a

            type = unknown_b
            """;

        var r = ScheduleParser.Parse(ini);
        Assert.That(r.Succeeded, Is.False);
        Assert.That(r.ErrorMessage, Does.Contain("unknown_a"));
        Assert.That(r.ErrorMessage, Does.Contain("unknown_b"));
    }
}
