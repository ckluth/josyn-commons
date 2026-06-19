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
        const string json = """
            [
              {
                "type": "interval",
                "days": "weekdays",
                "start": "08:00",
                "end": "17:00",
                "every": "30m"
              }
            ]
            """;

        var r = ScheduleParser.Parse(json);
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
    public void Parse_FixedRule_DaysAsArray_Succeeds()
    {
        const string json = """
            [{ "type": "fixed", "days": ["mon", "wed", "fri"], "times": ["06:00", "06:30"] }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (FixedRule)r.Value!.Rules[0];
        Assert.That(rule.Days.Days, Is.EquivalentTo(new[]
            { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }));
        Assert.That(rule.Times, Has.Count.EqualTo(2));
        Assert.That(rule.Times[0], Is.EqualTo(new TimeOnly(6, 0)));
        Assert.That(rule.Times[1], Is.EqualTo(new TimeOnly(6, 30)));
    }

    [Test]
    public void Parse_NthWeekdayRule_IntegerNth_Succeeds()
    {
        const string json = """
            [{
              "type": "nth_weekday",
              "weekday": "tue",
              "nth": 2,
              "period": "month",
              "times": ["09:00"]
            }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (NthWeekdayRule)r.Value!.Rules[0];
        Assert.That(rule.Weekday,        Is.EqualTo(DayOfWeek.Tuesday));
        Assert.That(rule.Nth,            Is.EqualTo(new Ordinal.Numeric(2)));
        Assert.That(rule.SchedulePeriod, Is.EqualTo(Period.Month));
        Assert.That(rule.Times[0],       Is.EqualTo(new TimeOnly(9, 0)));
    }

    [Test]
    public void Parse_NthWeekdayRule_StringNthLast_Succeeds()
    {
        const string json = """
            [{
              "type": "nth_weekday",
              "weekday": "fri",
              "nth": "last",
              "period": "month",
              "times": ["16:00"]
            }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(((NthWeekdayRule)r.Value!.Rules[0]).Nth, Is.InstanceOf<Ordinal.Last>());
    }

    [Test]
    public void Parse_NthWeekdayRule_StringNthLastMinus_Succeeds()
    {
        const string json = """
            [{
              "type": "nth_weekday",
              "weekday": "fri",
              "nth": "last-1",
              "period": "month",
              "times": ["09:00"]
            }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(((NthWeekdayRule)r.Value!.Rules[0]).Nth, Is.InstanceOf<Ordinal.LastMinus>());
    }

    [Test]
    public void Parse_MonthlyDateRule_NumericDay_NoMonths_Succeeds()
    {
        const string json = """
            [{ "type": "monthly_date", "day": 15, "times": ["08:00"] }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (MonthlyDateRule)r.Value!.Rules[0];
        Assert.That(rule.Day,    Is.EqualTo(new MonthlyDay.Numeric(15)));
        Assert.That(rule.Months, Is.Null);
    }

    [Test]
    public void Parse_MonthlyDateRule_WithMonthsArray_ParsesMonthSet()
    {
        const string json = """
            [{ "type": "monthly_date", "day": 1, "times": ["09:00"], "months": ["jan", "jul"] }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (MonthlyDateRule)r.Value!.Rules[0];
        Assert.That(rule.Months, Is.Not.Null);
        Assert.That(rule.Months!.Months, Is.EquivalentTo(new[] { 1, 7 }));
    }

    [Test]
    public void Parse_MonthlyDateRule_StringDayLastBusiness_Succeeds()
    {
        const string json = """
            [{ "type": "monthly_date", "day": "last_business", "times": ["17:00"] }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(((MonthlyDateRule)r.Value!.Rules[0]).Day, Is.InstanceOf<MonthlyDay.LastBusiness>());
    }

    [Test]
    public void Parse_WeekIntervalRule_Succeeds()
    {
        const string json = """
            [{
              "type": "week_interval",
              "days": ["fri"],
              "times": ["08:00"],
              "everyWeeks": 2,
              "anchor": "2026-01-02"
            }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (WeekIntervalRule)r.Value!.Rules[0];
        Assert.That(rule.EveryWeeks, Is.EqualTo(2));
        Assert.That(rule.Anchor,     Is.EqualTo(new DateOnly(2026, 1, 2)));
    }

    [Test]
    public void Parse_OnceRule_Succeeds()
    {
        const string json = """
            [{ "type": "once", "datetime": "2026-12-26 10:00" }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (OnceRule)r.Value!.Rules[0];
        Assert.That(rule.FireAt, Is.EqualTo(new DateTime(2026, 12, 26, 10, 0, 0)));
    }

    [Test]
    public void Parse_ExcludeRule_StringsAndRangeObjects_Succeeds()
    {
        const string json = """
            [{
              "type": "exclude",
              "dates": [
                "2026-12-24",
                "2026-12-25",
                { "from": "2026-12-27", "to": "2026-12-31" }
              ]
            }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (ExcludeRule)r.Value!.Rules[0];
        Assert.That(rule.Dates, Has.Count.EqualTo(3));
    }

    //
    // Days field variants
    //

    [Test]
    public void Parse_DaysShorthand_Weekend_Succeeds()
    {
        const string json = """
            [{ "type": "fixed", "days": "weekend", "times": ["10:00"] }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(((FixedRule)r.Value!.Rules[0]).Days.Equals(DaySet.Weekend), Is.True);
    }

    [Test]
    public void Parse_DaysShorthand_Daily_Succeeds()
    {
        const string json = """
            [{ "type": "fixed", "days": "daily", "times": ["06:00"] }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(((FixedRule)r.Value!.Rules[0]).Days.Equals(DaySet.Daily), Is.True);
    }

    //
    // activeFrom / activeUntil
    //

    [Test]
    public void Parse_ActiveBounds_FullDates_Parsed()
    {
        const string json = """
            [{
              "type": "interval",
              "days": "weekdays",
              "start": "08:00",
              "end": "17:00",
              "every": "30m",
              "activeFrom": "2026-04-01",
              "activeUntil": "2026-09-30"
            }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (IntervalRule)r.Value!.Rules[0];
        Assert.That(rule.ActiveFrom,  Is.InstanceOf<DateBound.FullDate>());
        Assert.That(rule.ActiveUntil, Is.InstanceOf<DateBound.FullDate>());
    }

    [Test]
    public void Parse_ActiveBounds_AnnualDates_Parsed()
    {
        const string json = """
            [{
              "type": "interval",
              "days": "weekdays",
              "start": "08:00",
              "end": "17:00",
              "every": "30m",
              "activeFrom": "04-01",
              "activeUntil": "09-30"
            }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);

        var rule = (IntervalRule)r.Value!.Rules[0];
        Assert.That(rule.ActiveFrom,  Is.InstanceOf<DateBound.Annual>());
        Assert.That(rule.ActiveUntil, Is.InstanceOf<DateBound.Annual>());
    }

    //
    // Comment handling (JSONC)
    //

    [Test]
    public void Parse_LineComments_Ignored()
    {
        const string jsonc = """
            [
              // Every 30 min during business hours
              {
                "type": "interval", // the type
                "days": "weekdays",
                "start": "08:00",
                "end": "17:00",
                "every": "30m"
              }
            ]
            """;

        var r = ScheduleParser.Parse(jsonc);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Rules, Has.Count.EqualTo(1));
    }

    //
    // Multiple rules
    //

    [Test]
    public void Parse_MultipleRules_ReturnsAll()
    {
        const string json = """
            [
              { "type": "fixed", "days": ["mon"], "times": ["09:00"] },
              { "type": "fixed", "days": ["tue"], "times": ["10:00"] },
              { "type": "exclude", "dates": ["2026-12-25"] }
            ]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Rules, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_EmptyArray_ReturnsEmptyDefinition()
    {
        var r = ScheduleParser.Parse("[]");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Rules, Is.Empty);
    }

    //
    // Error handling
    //

    [Test]
    public void Parse_NotAnArray_Fails()
    {
        var r = ScheduleParser.Parse("{}");
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_InvalidJson_Fails()
    {
        var r = ScheduleParser.Parse("not json at all");
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_MissingTypeProperty_Fails()
    {
        const string json = """[{ "days": "weekdays" }]""";
        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_UnknownType_Fails()
    {
        const string json = """[{ "type": "unknown_type" }]""";
        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_MissingRequiredProperty_Fails()
    {
        // interval without "end"
        const string json = """
            [{
              "type": "interval",
              "days": "weekdays",
              "start": "08:00",
              "every": "30m"
            }]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_MultipleRuleErrors_AllReportedInOneMessage()
    {
        const string json = """
            [
              { "type": "unknown_a" },
              { "type": "unknown_b" }
            ]
            """;

        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.False);
        Assert.That(r.ErrorMessage, Does.Contain("unknown_a"));
        Assert.That(r.ErrorMessage, Does.Contain("unknown_b"));
    }

    [Test]
    public void Parse_InvalidDayShorthand_Fails()
    {
        const string json = """[{ "type": "fixed", "days": "fridays", "times": ["09:00"] }]""";
        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.False);
    }

    [Test]
    public void Parse_InvalidDayInArray_Fails()
    {
        const string json = """[{ "type": "fixed", "days": ["mon", "xyz"], "times": ["09:00"] }]""";
        var r = ScheduleParser.Parse(json);
        Assert.That(r.Succeeded, Is.False);
    }
}
