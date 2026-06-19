using System.Text.Json;
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

    [Test] public void RoundTrip_IntervalRule_IsStable()    => AssertRoundTrip(Snippets.Interval);
    [Test] public void RoundTrip_FixedRule_IsStable()       => AssertRoundTrip(Snippets.Fixed);
    [Test] public void RoundTrip_NthWeekdayRule_IsStable()  => AssertRoundTrip(Snippets.NthWeekday);
    [Test] public void RoundTrip_MonthlyDateRule_IsStable() => AssertRoundTrip(Snippets.MonthlyDate);
    [Test] public void RoundTrip_WeekIntervalRule_IsStable()=> AssertRoundTrip(Snippets.WeekInterval);
    [Test] public void RoundTrip_OnceRule_IsStable()        => AssertRoundTrip(Snippets.Once);
    [Test] public void RoundTrip_ExcludeRule_IsStable()     => AssertRoundTrip(Snippets.Exclude);
    [Test] public void RoundTrip_WithActiveBounds_IsStable()=> AssertRoundTrip(Snippets.IntervalWithBounds);
    [Test] public void RoundTrip_FullExample_IsStable()     => AssertRoundTrip(Snippets.Full);

    //
    // Serialized output is valid standard JSON (parseable without comment handling)
    //

    [Test]
    public void Serialize_Output_IsValidJson()
    {
        var parsed = ScheduleParser.Parse(Snippets.Full);
        Assert.That(parsed.Succeeded, Is.True);

        var serialized = ScheduleSerializer.Serialize(parsed.Value!);
        Assert.That(serialized.Succeeded, Is.True);

        // Should parse without comment handling — output is plain JSON, not JSONC.
        Assert.DoesNotThrow(() => JsonDocument.Parse(serialized.Value!));
    }

    //
    // Named shorthand detection
    //

    [Test]
    public void Serialize_DaySet_AllWeekdays_EmitsWeekdaysKeyword()
    {
        // All five weekdays as an explicit array should serialize back to the "weekdays" shorthand.
        const string json = """
            [{ "type": "fixed", "days": ["mon", "tue", "wed", "thu", "fri"], "times": ["09:00"] }]
            """;

        var parsed = ScheduleParser.Parse(json);
        Assert.That(parsed.Succeeded, Is.True);

        var serialized = ScheduleSerializer.Serialize(parsed.Value!);
        Assert.That(serialized.Succeeded, Is.True);
        Assert.That(serialized.Value, Does.Contain("\"weekdays\""));
    }

    [Test]
    public void Serialize_DaySet_Weekend_EmitsWeekendKeyword()
    {
        const string json = """[{ "type": "fixed", "days": ["sat", "sun"], "times": ["10:00"] }]""";

        var parsed = ScheduleParser.Parse(json);
        Assert.That(parsed.Succeeded, Is.True);

        var serialized = ScheduleSerializer.Serialize(parsed.Value!);
        Assert.That(serialized.Succeeded, Is.True);
        Assert.That(serialized.Value, Does.Contain("\"weekend\""));
    }

    //
    // everyWeeks serialized as a JSON number (not a string)
    //

    [Test]
    public void Serialize_WeekInterval_EveryWeeks_IsJsonNumber()
    {
        var parsed = ScheduleParser.Parse(Snippets.WeekInterval);
        Assert.That(parsed.Succeeded, Is.True);

        var serialized = ScheduleSerializer.Serialize(parsed.Value!);
        Assert.That(serialized.Succeeded, Is.True);

        using var doc = JsonDocument.Parse(serialized.Value!);
        var everyWeeks = doc.RootElement[0].GetProperty("everyWeeks");
        Assert.That(everyWeeks.ValueKind, Is.EqualTo(JsonValueKind.Number));
        Assert.That(everyWeeks.GetInt32(), Is.EqualTo(2));
    }

    //
    // Helpers
    //

    private static void AssertRoundTrip(string input)
    {
        var parse1     = ScheduleParser.Parse(input);
        Assert.That(parse1.Succeeded, Is.True, $"First parse failed: {parse1.ErrorMessage}");

        var serialize1 = ScheduleSerializer.Serialize(parse1.Value!);
        Assert.That(serialize1.Succeeded, Is.True);

        var parse2     = ScheduleParser.Parse(serialize1.Value!);
        Assert.That(parse2.Succeeded, Is.True, $"Second parse failed: {parse2.ErrorMessage}");

        var serialize2 = ScheduleSerializer.Serialize(parse2.Value!);
        Assert.That(serialize2.Succeeded, Is.True);

        Assert.That(serialize2.Value, Is.EqualTo(serialize1.Value),
            "Serializer is not idempotent — second pass produced different output.");
    }

    //
    // JSONC snippets (one per rule type + a full example)
    //

    private static class Snippets
    {
        public const string Interval = """
            [{ "type": "interval", "days": "weekdays", "start": "08:00", "end": "17:00", "every": "30m" }]
            """;

        public const string IntervalWithBounds = """
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

        public const string Fixed = """
            [{ "type": "fixed", "days": ["mon", "wed", "fri"], "times": ["06:00", "06:30"] }]
            """;

        public const string NthWeekday = """
            [{ "type": "nth_weekday", "weekday": "tue", "nth": 2, "period": "month", "times": ["09:00"] }]
            """;

        public const string MonthlyDate = """
            [{ "type": "monthly_date", "day": 1, "times": ["09:00"], "months": ["jan", "jul"] }]
            """;

        public const string WeekInterval = """
            [{ "type": "week_interval", "days": ["fri"], "times": ["08:00"], "everyWeeks": 2, "anchor": "2026-01-02" }]
            """;

        public const string Once = """
            [{ "type": "once", "datetime": "2026-12-26 10:00" }]
            """;

        public const string Exclude = """
            [{
              "type": "exclude",
              "dates": ["2026-12-24", { "from": "2026-12-27", "to": "2026-12-31" }]
            }]
            """;

        public const string Full = """
            [
              { "type": "interval", "days": "weekdays", "start": "08:00", "end": "17:00", "every": "30m" },
              { "type": "fixed", "days": ["mon", "wed", "fri"], "times": ["06:00", "06:30"] },
              { "type": "nth_weekday", "weekday": "fri", "nth": "last", "period": "month", "times": ["16:00"] },
              { "type": "monthly_date", "day": "last_business", "times": ["17:00"] },
              { "type": "week_interval", "days": ["fri"], "times": ["08:00"], "everyWeeks": 2, "anchor": "2026-01-02" },
              { "type": "once", "datetime": "2026-12-26 10:00" },
              { "type": "exclude", "dates": ["2026-12-24", "2026-12-25", { "from": "2026-12-27", "to": "2026-12-31" }] }
            ]
            """;
    }
}
