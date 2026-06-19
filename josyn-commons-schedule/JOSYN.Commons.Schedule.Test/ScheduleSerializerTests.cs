using NUnit.Framework;

using JOSYN.Commons.Schedule;

namespace JOSYN.Commons.Schedule.Test;

/// <summary>
/// Round-trip tests: parse → serialize → parse → serialize; the two serialized texts
/// must be identical (proves the serializer is a left-inverse of the parser).
/// </summary>
[TestFixture]
public sealed class ScheduleSerializerTests
{
    //
    // Round-trip per rule type
    //

    [Test]
    public void RoundTrip_IntervalRule_IsStable()   => AssertRoundTrip(Snippets.Interval);

    [Test]
    public void RoundTrip_FixedRule_IsStable()      => AssertRoundTrip(Snippets.Fixed);

    [Test]
    public void RoundTrip_NthWeekdayRule_IsStable() => AssertRoundTrip(Snippets.NthWeekday);

    [Test]
    public void RoundTrip_MonthlyDateRule_IsStable() => AssertRoundTrip(Snippets.MonthlyDate);

    [Test]
    public void RoundTrip_WeekIntervalRule_IsStable() => AssertRoundTrip(Snippets.WeekInterval);

    [Test]
    public void RoundTrip_OnceRule_IsStable()       => AssertRoundTrip(Snippets.Once);

    [Test]
    public void RoundTrip_ExcludeRule_IsStable()    => AssertRoundTrip(Snippets.Exclude);

    [Test]
    public void RoundTrip_WithActiveBounds_IsStable() => AssertRoundTrip(Snippets.IntervalWithBounds);

    [Test]
    public void RoundTrip_FullExample_IsStable()    => AssertRoundTrip(Snippets.Full);

    //
    // Keyword detection
    //

    [Test]
    public void Serialize_DaySet_MonToFri_EmitsWeekdaysKeyword()
    {
        // "mon..fri" parses to the same DaySet as "weekdays" — serializer must emit "weekdays".
        const string ini = """
            type  = interval
            days  = mon..fri
            start = 08:00
            end   = 17:00
            every = 30m
            """;

        var parsed = ScheduleParser.Parse(ini);
        Assert.That(parsed.Succeeded, Is.True);

        var serialized = ScheduleSerializer.Serialize(parsed.Value!);
        Assert.That(serialized.Succeeded, Is.True);
        Assert.That(serialized.Value, Does.Contain("weekdays"));
    }

    [Test]
    public void Serialize_MonthSet_Range_EmitsRangeNotation()
    {
        // "jan, feb, mar" should compress to "jan..mar" in serialized output.
        const string ini = """
            type   = monthly_date
            day    = 1
            time   = 09:00
            months = jan, feb, mar
            """;

        var parsed = ScheduleParser.Parse(ini);
        Assert.That(parsed.Succeeded, Is.True);

        var serialized = ScheduleSerializer.Serialize(parsed.Value);
        Assert.That(serialized.Succeeded, Is.True);
        Assert.That(serialized.Value, Does.Contain("jan..mar"));
    }

    //
    // Helpers
    //

    private static void AssertRoundTrip(string ini)
    {
        var parse1     = ScheduleParser.Parse(ini);
        Assert.That(parse1.Succeeded, Is.True, $"First parse failed: {parse1.ErrorMessage}");

        var serialize1 = ScheduleSerializer.Serialize(parse1.Value);
        Assert.That(serialize1.Succeeded, Is.True);

        var parse2     = ScheduleParser.Parse(serialize1.Value);
        Assert.That(parse2.Succeeded, Is.True, $"Second parse failed: {parse2.ErrorMessage}");

        var serialize2 = ScheduleSerializer.Serialize(parse2.Value);
        Assert.That(serialize2.Succeeded, Is.True);

        Assert.That(serialize2.Value, Is.EqualTo(serialize1.Value),
            "Serializer is not idempotent — second pass produced different output.");
    }

    //
    // INI snippets (one per rule type + a full example)
    //

    private static class Snippets
    {
        public const string Interval = """
            type  = interval
            days  = weekdays
            start = 08:00
            end   = 17:00
            every = 30m
            """;

        public const string IntervalWithBounds =
            "type         = interval\n" +
            "days         = weekdays\n" +
            "start        = 08:00\n" +
            "end          = 17:00\n" +
            "every        = 30m\n" +
            "active_from  = 04-01\n" +
            "active_until = 09-30\n";

        public const string Fixed = """
            type = fixed
            days = mon, wed, fri
            time = 06:00, 06:30
            """;

        public const string NthWeekday = """
            type    = nth_weekday
            weekday = tue
            nth     = 2
            period  = month
            time    = 09:00
            """;

        public const string MonthlyDate = """
            type   = monthly_date
            day    = 1
            time   = 09:00
            months = jan, jul
            """;

        public const string WeekInterval = """
            type   = week_interval
            days   = fri
            time   = 08:00
            every  = 2
            anchor = 2026-01-02
            """;

        public const string Once = """
            type     = once
            datetime = 2026-12-26 10:00
            """;

        public const string Exclude =
            "type  = exclude\n" +
            "dates = 2026-12-24, 2026-12-27..2026-12-31\n";

        public const string Full =
            "type  = interval\n" +
            "days  = weekdays\n" +
            "start = 08:00\n" +
            "end   = 17:00\n" +
            "every = 30m\n" +
            "\n" +
            "type = fixed\n" +
            "days = mon, wed, fri\n" +
            "time = 06:00, 06:30\n" +
            "\n" +
            "type    = nth_weekday\n" +
            "weekday = fri\n" +
            "nth     = last\n" +
            "period  = month\n" +
            "time    = 16:00\n" +
            "\n" +
            "type = monthly_date\n" +
            "day  = last_business\n" +
            "time = 17:00\n" +
            "\n" +
            "type   = week_interval\n" +
            "days   = fri\n" +
            "time   = 08:00\n" +
            "every  = 2\n" +
            "anchor = 2026-01-02\n" +
            "\n" +
            "type     = once\n" +
            "datetime = 2026-12-26 10:00\n" +
            "\n" +
            "type  = exclude\n" +
            "dates = 2026-12-24, 2026-12-25, 2026-12-27..2026-12-31\n";
    }
}
