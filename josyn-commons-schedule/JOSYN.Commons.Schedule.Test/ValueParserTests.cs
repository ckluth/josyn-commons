using NUnit.Framework;

using JOSYN.Commons.Schedule;

namespace JOSYN.Commons.Schedule.Test;

/// <summary>
/// Tests for the individual leaf-value parsers in <see cref="ValueParsers"/>.
/// These are internal and exercised here via InternalsVisibleTo.
/// </summary>
[TestFixture]
public sealed class ValueParserTests
{
    // ── DaySet (shorthand strings only) ───────────────────────────────────────

    [TestCase("weekdays")]
    [TestCase("WEEKDAYS")]
    public void ParseDaySet_WeekdaysKeyword_ReturnsWeekdays(string input)
    {
        var r = ValueParsers.ParseDaySet(input);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Equals(DaySet.Weekdays), Is.True);
    }

    [Test]
    public void ParseDaySet_WeekendKeyword_ReturnsWeekend()
    {
        var r = ValueParsers.ParseDaySet("weekend");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Equals(DaySet.Weekend), Is.True);
    }

    [Test]
    public void ParseDaySet_DailyKeyword_ReturnsDaily()
    {
        var r = ValueParsers.ParseDaySet("daily");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Equals(DaySet.Daily), Is.True);
    }

    [TestCase("xyz")]
    [TestCase("fridays")]
    [TestCase("mon")]        // single abbreviations are not valid as day-field strings; use array form
    public void ParseDaySet_UnknownShorthand_Fails(string input)
    {
        var r = ValueParsers.ParseDaySet(input);
        Assert.That(r.Succeeded, Is.False);
    }

    // ── Weekday abbreviations ─────────────────────────────────────────────────

    [TestCase("mon", DayOfWeek.Monday)]
    [TestCase("tue", DayOfWeek.Tuesday)]
    [TestCase("fri", DayOfWeek.Friday)]
    [TestCase("SUN", DayOfWeek.Sunday)]
    public void ParseWeekday_ValidAbbreviation_ReturnsCorrectDay(string input, DayOfWeek expected)
    {
        var r = ValueParsers.ParseWeekday(input);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.EqualTo(expected));
    }

    [TestCase("xyz")]
    [TestCase("monday")]
    public void ParseWeekday_InvalidAbbreviation_Fails(string input)
    {
        Assert.That(ValueParsers.ParseWeekday(input).Succeeded, Is.False);
    }

    // ── Month abbreviations ───────────────────────────────────────────────────

    [TestCase("jan", 1)]
    [TestCase("JUL", 7)]
    [TestCase("dec", 12)]
    public void ParseSingleMonth_ValidAbbreviation_ReturnsCorrectNumber(string input, int expected)
    {
        var r = ValueParsers.ParseSingleMonth(input);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.EqualTo(expected));
    }

    [TestCase("xyz")]
    [TestCase("january")]
    public void ParseSingleMonth_Invalid_Fails(string input)
    {
        Assert.That(ValueParsers.ParseSingleMonth(input).Succeeded, Is.False);
    }

    // ── TimeOnly ──────────────────────────────────────────────────────────────

    [TestCase("08:00",  8,  0)]
    [TestCase("17:30", 17, 30)]
    [TestCase("00:00",  0,  0)]
    [TestCase("23:59", 23, 59)]
    public void ParseTimeOnly_ValidInput_ReturnsCorrectTime(string input, int hour, int minute)
    {
        var r = ValueParsers.ParseTimeOnly(input);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.EqualTo(new TimeOnly(hour, minute)));
    }

    [TestCase("24:00")]
    [TestCase("8:00")]
    [TestCase("08:60")]
    [TestCase("abc")]
    public void ParseTimeOnly_InvalidInput_Fails(string input)
    {
        Assert.That(ValueParsers.ParseTimeOnly(input).Succeeded, Is.False);
    }

    // ── Duration ──────────────────────────────────────────────────────────────

    [TestCase("30m",  30, DurationUnit.Minutes)]
    [TestCase("2h",    2, DurationUnit.Hours)]
    [TestCase("90m",  90, DurationUnit.Minutes)]
    public void ParseDuration_ValidInput_ReturnsCorrectDuration(string input, int amount, DurationUnit unit)
    {
        var r = ValueParsers.ParseDuration(input);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Amount, Is.EqualTo(amount));
        Assert.That(r.Value.Unit,    Is.EqualTo(unit));
    }

    [TestCase("1h30m")]
    [TestCase("0m")]
    [TestCase("abc")]
    [TestCase("30")]
    public void ParseDuration_InvalidInput_Fails(string input)
    {
        Assert.That(ValueParsers.ParseDuration(input).Succeeded, Is.False);
    }

    // ── Ordinal ───────────────────────────────────────────────────────────────

    [Test]
    public void ParseOrdinal_NumericString_ReturnsNumeric()
    {
        var r = ValueParsers.ParseOrdinal("2");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.InstanceOf<Ordinal.Numeric>());
        Assert.That(((Ordinal.Numeric)r.Value!).N, Is.EqualTo(2));
    }

    [Test]
    public void ParseOrdinal_Last_ReturnsLast()
    {
        var r = ValueParsers.ParseOrdinal("last");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.InstanceOf<Ordinal.Last>());
    }

    [Test]
    public void ParseOrdinal_LastMinus_ReturnsLastMinus()
    {
        var r = ValueParsers.ParseOrdinal("last-1");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.InstanceOf<Ordinal.LastMinus>());
        Assert.That(((Ordinal.LastMinus)r.Value!).Offset, Is.EqualTo(1));
    }

    [TestCase("0")]
    [TestCase("6")]
    [TestCase("last-0")]
    [TestCase("abc")]
    public void ParseOrdinal_InvalidInput_Fails(string input)
    {
        Assert.That(ValueParsers.ParseOrdinal(input).Succeeded, Is.False);
    }

    // ── MonthlyDay (string form) ──────────────────────────────────────────────

    [Test] public void ParseMonthlyDay_Last_Succeeds()         => Assert.That(ValueParsers.ParseMonthlyDay("last").Succeeded,          Is.True);
    [Test] public void ParseMonthlyDay_LastBusiness_Succeeds() => Assert.That(ValueParsers.ParseMonthlyDay("last_business").Succeeded, Is.True);

    [TestCase("abc")]
    [TestCase("15")]   // numeric strings are not accepted by the string parser — integers come from JSON
    public void ParseMonthlyDay_InvalidInput_Fails(string input)
    {
        Assert.That(ValueParsers.ParseMonthlyDay(input).Succeeded, Is.False);
    }

    // ── DateBound ─────────────────────────────────────────────────────────────

    [Test]
    public void ParseDateBound_FullDate_ReturnsFullDate()
    {
        var r = ValueParsers.ParseDateBound("2026-04-01");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.InstanceOf<DateBound.FullDate>());
        Assert.That(((DateBound.FullDate)r.Value!).Date, Is.EqualTo(new DateOnly(2026, 4, 1)));
    }

    [Test]
    public void ParseDateBound_Annual_ReturnsAnnual()
    {
        var r = ValueParsers.ParseDateBound("04-01");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.InstanceOf<DateBound.Annual>());
        var annual = (DateBound.Annual)r.Value!;
        Assert.That(annual.Month, Is.EqualTo(4));
        Assert.That(annual.Day,   Is.EqualTo(1));
    }

    [TestCase("2026-13-01")]
    [TestCase("13-01")]
    [TestCase("abc")]
    public void ParseDateBound_InvalidInput_Fails(string input)
    {
        Assert.That(ValueParsers.ParseDateBound(input).Succeeded, Is.False);
    }

    // ── ParseDateOnly ─────────────────────────────────────────────────────────

    [Test]
    public void ParseDateOnly_ValidDate_Succeeds()
    {
        var r = ValueParsers.ParseDateOnly("2026-12-24");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.EqualTo(new DateOnly(2026, 12, 24)));
    }

    [TestCase("24-12-2026")]
    [TestCase("abc")]
    public void ParseDateOnly_InvalidInput_Fails(string input)
    {
        Assert.That(ValueParsers.ParseDateOnly(input).Succeeded, Is.False);
    }
}
