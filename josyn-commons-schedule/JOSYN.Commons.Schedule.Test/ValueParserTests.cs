using NUnit.Framework;

using JOSYN.Commons.Schedule;

namespace JOSYN.Commons.Schedule.Test;

/// <summary>
/// Tests for the individual value-type parsers in <see cref="ValueParsers"/>.
/// These are internal and exercised here via InternalsVisibleTo.
/// </summary>
[TestFixture]
public sealed class ValueParserTests
{
    // ── DaySet ────────────────────────────────────────────────────────────────

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

    [Test]
    public void ParseDaySet_Range_MonToFri_EqualsWeekdays()
    {
        var r = ValueParsers.ParseDaySet("mon..fri");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Equals(DaySet.Weekdays), Is.True);
    }

    [Test]
    public void ParseDaySet_CommaList_ContainsExactDays()
    {
        var r = ValueParsers.ParseDaySet("mon, wed, fri");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Days, Is.EquivalentTo([DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]));
    }

    [Test]
    public void ParseDaySet_MixedRangeAndSingle_Succeeds()
    {
        var r = ValueParsers.ParseDaySet("mon..wed, fri");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Days, Is.EquivalentTo([DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday]));
    }

    [TestCase("xyz")]
    [TestCase("mon, xyz")]
    public void ParseDaySet_InvalidAbbreviation_Fails(string input)
    {
        var r = ValueParsers.ParseDaySet(input);
        Assert.That(r.Succeeded, Is.False);
    }

    // ── MonthSet ──────────────────────────────────────────────────────────────

    [Test]
    public void ParseMonthSet_Range_JanToJun_ContainsSixMonths()
    {
        var r = ValueParsers.ParseMonthSet("jan..jun");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Months, Is.EquivalentTo(Enumerable.Range(1, 6)));
    }

    [Test]
    public void ParseMonthSet_CommaList_ContainsCorrectMonths()
    {
        var r = ValueParsers.ParseMonthSet("jan, jul");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Months, Is.EquivalentTo([1, 7]));
    }

    [TestCase("xyz")]
    public void ParseMonthSet_Invalid_Fails(string input)
    {
        var r = ValueParsers.ParseMonthSet(input);
        Assert.That(r.Succeeded, Is.False);
    }

    // ── TimeOnly ──────────────────────────────────────────────────────────────

    [TestCase("08:00", 8, 0)]
    [TestCase("17:30", 17, 30)]
    [TestCase("00:00", 0, 0)]
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
        var r = ValueParsers.ParseTimeOnly(input);
        Assert.That(r.Succeeded, Is.False);
    }

    // ── Duration ──────────────────────────────────────────────────────────────

    [TestCase("30m", 30, DurationUnit.Minutes)]
    [TestCase("2h",   2, DurationUnit.Hours)]
    [TestCase("90m", 90, DurationUnit.Minutes)]
    public void ParseDuration_ValidInput_ReturnsCorrectDuration(string input, int amount, DurationUnit unit)
    {
        var r = ValueParsers.ParseDuration(input);
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value!.Amount, Is.EqualTo(amount));
        Assert.That(r.Value.Unit, Is.EqualTo(unit));
    }

    [TestCase("1h30m")]
    [TestCase("0m")]
    [TestCase("abc")]
    [TestCase("30")]
    public void ParseDuration_InvalidInput_Fails(string input)
    {
        var r = ValueParsers.ParseDuration(input);
        Assert.That(r.Succeeded, Is.False);
    }

    // ── Ordinal ───────────────────────────────────────────────────────────────

    [Test]
    public void ParseOrdinal_Numeric_ReturnsNumeric()
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
        var r = ValueParsers.ParseOrdinal(input);
        Assert.That(r.Succeeded, Is.False);
    }

    // ── MonthlyDay ────────────────────────────────────────────────────────────

    [Test]
    public void ParseMonthlyDay_Numeric_ReturnsNumeric()
    {
        var r = ValueParsers.ParseMonthlyDay("15");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Is.InstanceOf<MonthlyDay.Numeric>());
        Assert.That(((MonthlyDay.Numeric)r.Value!).N, Is.EqualTo(15));
    }

    [Test] public void ParseMonthlyDay_Last_Succeeds()         => Assert.That(ValueParsers.ParseMonthlyDay("last").Succeeded, Is.True);
    [Test] public void ParseMonthlyDay_LastBusiness_Succeeds() => Assert.That(ValueParsers.ParseMonthlyDay("last_business").Succeeded, Is.True);

    [TestCase("0")]
    [TestCase("32")]
    [TestCase("abc")]
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
        Assert.That(annual.Day, Is.EqualTo(1));
    }

    [TestCase("2026-13-01")]
    [TestCase("13-01")]
    [TestCase("abc")]
    public void ParseDateBound_InvalidInput_Fails(string input)
    {
        Assert.That(ValueParsers.ParseDateBound(input).Succeeded, Is.False);
    }

    // ── DateRanges ────────────────────────────────────────────────────────────

    [Test]
    public void ParseDateRanges_SingleDate_ReturnsSingleRangeWithEqualBounds()
    {
        var r = ValueParsers.ParseDateRanges("2026-12-24");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Has.Count.EqualTo(1));
        Assert.That(r.Value![0].Start, Is.EqualTo(new DateOnly(2026, 12, 24)));
        Assert.That(r.Value[0].End,   Is.EqualTo(new DateOnly(2026, 12, 24)));
    }

    [Test]
    public void ParseDateRanges_RangeSyntax_ReturnsBounds()
    {
        var r = ValueParsers.ParseDateRanges("2026-12-27..2026-12-31");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value![0].Start, Is.EqualTo(new DateOnly(2026, 12, 27)));
        Assert.That(r.Value[0].End,   Is.EqualTo(new DateOnly(2026, 12, 31)));
    }

    [Test]
    public void ParseDateRanges_Mixed_ReturnsBothEntries()
    {
        var r = ValueParsers.ParseDateRanges("2026-12-24, 2026-12-27..2026-12-31");
        Assert.That(r.Succeeded, Is.True);
        Assert.That(r.Value, Has.Count.EqualTo(2));
    }
}
