using NUnit.Framework;

using JOSYN.Commons.Schedule;

namespace JOSYN.Commons.Schedule.Test;

/// <summary>
/// One test per behavioural condition of <see cref="ScheduleEvaluator.IsDue"/>.
/// Each test fixes <c>now</c> to a known moment so the assertion is deterministic.
/// </summary>
/// <remarks>
/// Reference calendar used throughout:
/// <list type="bullet">
///   <item>2026-06-15 (Monday) — a normal weekday</item>
///   <item>2026-06-19 (Friday)</item>
///   <item>2026-06-20 (Saturday) — weekend</item>
///   <item>2026-01-02 (Friday)  — anchor date used in week_interval tests</item>
/// </list>
/// </remarks>
[TestFixture]
public sealed class ScheduleEvaluatorTests
{
    // ── fixed ─────────────────────────────────────────────────────────────────

    [Test]
    public void Fixed_DayAndTimeMatch_IsTrue()
    {
        var rule = Fixed(DaySet.Weekdays, new TimeOnly(8, 0));
        var now  = new DateTime(2026, 6, 15, 8, 0, 0); // Monday 08:00

        Assert.That(IsDue(rule, now), Is.True);
    }

    [Test]
    public void Fixed_DayMatchesButTimeDiffers_IsFalse()
    {
        var rule = Fixed(DaySet.Weekdays, new TimeOnly(8, 0));
        var now  = new DateTime(2026, 6, 15, 8, 1, 0); // Monday 08:01

        Assert.That(IsDue(rule, now), Is.False);
    }

    [Test]
    public void Fixed_WeekendDayWithWeekdaysRule_IsFalse()
    {
        var rule = Fixed(DaySet.Weekdays, new TimeOnly(8, 0));
        var now  = new DateTime(2026, 6, 20, 8, 0, 0); // Saturday

        Assert.That(IsDue(rule, now), Is.False);
    }

    [Test]
    public void Fixed_MultipleTimesInRule_SecondTimeMatches_IsTrue()
    {
        var rule = new FixedRule(
            Days:       DaySet.Weekdays,
            Times:      new[] { new TimeOnly(8, 0), new TimeOnly(12, 0) }.ToList().AsReadOnly(),
            ActiveFrom: null, ActiveUntil: null);
        var now = new DateTime(2026, 6, 15, 12, 0, 0); // Monday 12:00

        Assert.That(IsDue(rule, now), Is.True);
    }

    [Test]
    public void Fixed_SecondsIgnored_StillMatches()
    {
        // Seconds are stripped — 08:00:59 should still match a fire time of 08:00.
        var rule = Fixed(DaySet.Weekdays, new TimeOnly(8, 0));
        var now  = new DateTime(2026, 6, 15, 8, 0, 59);

        Assert.That(IsDue(rule, now), Is.True);
    }

    // ── interval ─────────────────────────────────────────────────────────────

    [Test]
    public void Interval_TimeAlignedOnSlot_IsTrue()
    {
        // start=08:00, every=30m → slots: 08:00, 08:30, 09:00 …
        var rule = new IntervalRule(
            Days: DaySet.Weekdays,
            Start: new TimeOnly(8, 0),
            End:   new TimeOnly(17, 0),
            Every: new Duration(30, DurationUnit.Minutes),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8,  0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8, 30, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 17, 0, 0)), Is.True);
    }

    [Test]
    public void Interval_TimeBetweenSlots_IsFalse()
    {
        var rule = new IntervalRule(
            Days: DaySet.Weekdays,
            Start: new TimeOnly(8, 0),
            End:   new TimeOnly(17, 0),
            Every: new Duration(30, DurationUnit.Minutes),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8, 15, 0)), Is.False);
    }

    [Test]
    public void Interval_TimeBeforeStart_IsFalse()
    {
        var rule = new IntervalRule(
            Days: DaySet.Weekdays,
            Start: new TimeOnly(8, 0),
            End:   new TimeOnly(17, 0),
            Every: new Duration(30, DurationUnit.Minutes),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 7, 59, 0)), Is.False);
    }

    [Test]
    public void Interval_TimeAfterEnd_IsFalse()
    {
        var rule = new IntervalRule(
            Days: DaySet.Weekdays,
            Start: new TimeOnly(8, 0),
            End:   new TimeOnly(17, 0),
            Every: new Duration(30, DurationUnit.Minutes),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 17, 30, 0)), Is.False);
    }

    [Test]
    public void Interval_HourlyInterval_CorrectSlots()
    {
        var rule = new IntervalRule(
            Days: DaySet.Daily,
            Start: new TimeOnly(6, 0),
            End:   new TimeOnly(18, 0),
            Every: new Duration(2, DurationUnit.Hours),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 6,  0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8,  0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 7,  0, 0)), Is.False);
    }

    // ── monthly_date ──────────────────────────────────────────────────────────

    [Test]
    public void MonthlyDate_NumericDay_OnTargetDate_IsTrue()
    {
        var rule = new MonthlyDateRule(
            Day:    new MonthlyDay.Numeric(15),
            Times:  Times(8, 0),
            Months: null,
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8, 0, 0)), Is.True);
    }

    [Test]
    public void MonthlyDate_NumericDay_WrongDate_IsFalse()
    {
        var rule = new MonthlyDateRule(
            Day:    new MonthlyDay.Numeric(15),
            Times:  Times(8, 0),
            Months: null,
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 14, 8, 0, 0)), Is.False);
    }

    [Test]
    public void MonthlyDate_Day31_InFebruary_ClampedToLastDay_IsTrue()
    {
        // Feb 2026 has 28 days — day=31 should clamp to Feb 28.
        var rule = new MonthlyDateRule(
            Day:    new MonthlyDay.Numeric(31),
            Times:  Times(8, 0),
            Months: null,
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 2, 28, 8, 0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 2, 27, 8, 0, 0)), Is.False);
    }

    [Test]
    public void MonthlyDate_Last_OnLastCalendarDay_IsTrue()
    {
        var rule = new MonthlyDateRule(
            Day:    new MonthlyDay.Last(),
            Times:  Times(17, 30),
            Months: null,
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 30, 17, 30, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 6, 29, 17, 30, 0)), Is.False);
    }

    [Test]
    public void MonthlyDate_LastBusiness_SkipsWeekend_IsTrue()
    {
        // June 2026: last day is June 30 (Tuesday) — it is itself a business day.
        var rule = new MonthlyDateRule(
            Day:    new MonthlyDay.LastBusiness(),
            Times:  Times(17, 0),
            Months: null,
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 30, 17, 0, 0)), Is.True);
    }

    [Test]
    public void MonthlyDate_LastBusiness_LastDayIsWeekend_FindsPreviousWeekday()
    {
        // January 2026: Jan 31 is Saturday → last business day is Jan 30 (Friday).
        var rule = new MonthlyDateRule(
            Day:    new MonthlyDay.LastBusiness(),
            Times:  Times(17, 0),
            Months: null,
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 1, 30, 17, 0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 1, 31, 17, 0, 0)), Is.False);
    }

    [Test]
    public void MonthlyDate_MonthFilter_SkipsNonMatchingMonth_IsFalse()
    {
        // months = [jan, jul] — June must be skipped.
        var rule = new MonthlyDateRule(
            Day:    new MonthlyDay.Numeric(1),
            Times:  Times(9, 0),
            Months: new MonthSet(new HashSet<int> { 1, 7 }),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 1, 9, 0, 0)), Is.False);
        Assert.That(IsDue(rule, new DateTime(2026, 7, 1, 9, 0, 0)), Is.True);
    }

    // ── nth_weekday ───────────────────────────────────────────────────────────

    [Test]
    public void NthWeekday_SecondTuesdayOfMonth_OnCorrectDate_IsTrue()
    {
        // June 2026: Tuesdays are 2, 9, 16, 23, 30 → 2nd is June 9.
        var rule = new NthWeekdayRule(
            Weekday:        DayOfWeek.Tuesday,
            Nth:            new Ordinal.Numeric(2),
            SchedulePeriod: Period.Month,
            Times:          Times(9, 0),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 9, 9, 0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 6, 2, 9, 0, 0)), Is.False);
    }

    [Test]
    public void NthWeekday_LastFridayOfMonth_IsTrue()
    {
        // June 2026: Fridays are 5, 12, 19, 26 → last is June 26.
        var rule = new NthWeekdayRule(
            Weekday:        DayOfWeek.Friday,
            Nth:            new Ordinal.Last(),
            SchedulePeriod: Period.Month,
            Times:          Times(16, 0),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 26, 16, 0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 6, 19, 16, 0, 0)), Is.False);
    }

    [Test]
    public void NthWeekday_SecondToLastFriday_IsTrue()
    {
        // June 2026: last-1 Friday = June 19.
        var rule = new NthWeekdayRule(
            Weekday:        DayOfWeek.Friday,
            Nth:            new Ordinal.LastMinus(1),
            SchedulePeriod: Period.Month,
            Times:          Times(9, 0),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 6, 19, 9, 0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 6, 26, 9, 0, 0)), Is.False);
    }

    [Test]
    public void NthWeekday_FirstMondayOfQuarter_Q2_IsTrue()
    {
        // ADR-026: quarter means first month of quarter.
        // Q2 starts April 2026. First Monday of April 2026 = April 6.
        var rule = new NthWeekdayRule(
            Weekday:        DayOfWeek.Monday,
            Nth:            new Ordinal.Numeric(1),
            SchedulePeriod: Period.Quarter,
            Times:          Times(12, 0),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 4, 6, 12, 0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 4, 13, 12, 0, 0)), Is.False);
    }

    [Test]
    public void NthWeekday_SecondMondayOfYear_IsTrue()
    {
        // Period=Year: first month of "year period" = January.
        // First Monday in Jan 2026 = Jan 5; second = Jan 12.
        var rule = new NthWeekdayRule(
            Weekday:        DayOfWeek.Monday,
            Nth:            new Ordinal.Numeric(2),
            SchedulePeriod: Period.Year,
            Times:          Times(9, 0),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 1, 12, 9, 0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 1,  5, 9, 0, 0)), Is.False);
    }

    [Test]
    public void NthWeekday_TargetDateExcluded_IsFalse()
    {
        // 2nd Tuesday June 9 is excluded — rule must be silently skipped.
        var nthRule = new NthWeekdayRule(
            Weekday:        DayOfWeek.Tuesday,
            Nth:            new Ordinal.Numeric(2),
            SchedulePeriod: Period.Month,
            Times:          Times(9, 0),
            ActiveFrom: null, ActiveUntil: null);

        var excludeRule = new ExcludeRule(
            Dates: new[] { new DateRange(new DateOnly(2026, 6, 9), new DateOnly(2026, 6, 9)) }
                       .ToList().AsReadOnly());

        Assert.That(IsDue(Definition(nthRule, excludeRule), new DateTime(2026, 6, 9, 9, 0, 0)),
            Is.False);
    }

    // ── week_interval ─────────────────────────────────────────────────────────

    [Test]
    public void WeekInterval_EveryOtherFriday_OnAnchorWeek_IsTrue()
    {
        // anchor=2026-01-02 (Friday), everyWeeks=2.
        // Week 0 (anchor week): Jan 2 → due.
        var rule = WeekInt(DayOfWeek.Friday, 2, new DateOnly(2026, 1, 2), new TimeOnly(8, 0));

        Assert.That(IsDue(rule, new DateTime(2026, 1, 2, 8, 0, 0)), Is.True);
    }

    [Test]
    public void WeekInterval_EveryOtherFriday_SkipWeek_IsFalse()
    {
        // Week 1 (Jan 9): not due.
        var rule = WeekInt(DayOfWeek.Friday, 2, new DateOnly(2026, 1, 2), new TimeOnly(8, 0));

        Assert.That(IsDue(rule, new DateTime(2026, 1, 9, 8, 0, 0)), Is.False);
    }

    [Test]
    public void WeekInterval_EveryOtherFriday_NextActiveWeek_IsTrue()
    {
        // Week 2 (Jan 16): due again.
        var rule = WeekInt(DayOfWeek.Friday, 2, new DateOnly(2026, 1, 2), new TimeOnly(8, 0));

        Assert.That(IsDue(rule, new DateTime(2026, 1, 16, 8, 0, 0)), Is.True);
    }

    [Test]
    public void WeekInterval_BeforeAnchorDate_IsFalse()
    {
        var rule = WeekInt(DayOfWeek.Friday, 2, new DateOnly(2026, 6, 5), new TimeOnly(8, 0));

        Assert.That(IsDue(rule, new DateTime(2026, 5, 29, 8, 0, 0)), Is.False);
    }

    // ── once ──────────────────────────────────────────────────────────────────

    [Test]
    public void Once_ExactMinuteMatch_IsTrue()
    {
        var rule = new OnceRule(
            FireAt:     new DateTime(2026, 12, 26, 10, 0, 0),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 12, 26, 10, 0, 0)), Is.True);
    }

    [Test]
    public void Once_OneMinuteLate_IsFalse()
    {
        var rule = new OnceRule(
            FireAt:     new DateTime(2026, 12, 26, 10, 0, 0),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 12, 26, 10, 1, 0)), Is.False);
    }

    [Test]
    public void Once_WrongDate_IsFalse()
    {
        var rule = new OnceRule(
            FireAt:     new DateTime(2026, 12, 26, 10, 0, 0),
            ActiveFrom: null, ActiveUntil: null);

        Assert.That(IsDue(rule, new DateTime(2026, 12, 27, 10, 0, 0)), Is.False);
    }

    // ── exclude ───────────────────────────────────────────────────────────────

    [Test]
    public void Exclude_ExcludedDate_BlocksOtherwiseDueRule()
    {
        // fixed rule would fire on Mon 08:00 — but June 15 is excluded.
        var fixedRule   = Fixed(DaySet.Weekdays, new TimeOnly(8, 0));
        var excludeRule = new ExcludeRule(
            Dates: new[] { new DateRange(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 15)) }
                       .ToList().AsReadOnly());

        Assert.That(IsDue(Definition(fixedRule, excludeRule), new DateTime(2026, 6, 15, 8, 0, 0)),
            Is.False);
    }

    [Test]
    public void Exclude_ExcludeRange_BlocksEntireRange()
    {
        var fixedRule   = Fixed(DaySet.Daily, new TimeOnly(8, 0));
        var excludeRule = new ExcludeRule(
            Dates: new[] { new DateRange(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 19)) }
                       .ToList().AsReadOnly());

        var definition = Definition(fixedRule, excludeRule);

        // All dates in range are blocked.
        for (var day = 15; day <= 19; day++)
            Assert.That(IsDue(definition, new DateTime(2026, 6, day, 8, 0, 0)), Is.False,
                $"Expected false for June {day}");

        // Date outside range still fires.
        Assert.That(IsDue(definition, new DateTime(2026, 6, 20, 8, 0, 0)), Is.True);
    }

    [Test]
    public void Exclude_MultipleExcludeBlocks_AreMerged()
    {
        var fixedRule    = Fixed(DaySet.Daily, new TimeOnly(8, 0));
        var excludeRule1 = new ExcludeRule(
            Dates: new[] { new DateRange(new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 25)) }
                       .ToList().AsReadOnly());
        var excludeRule2 = new ExcludeRule(
            Dates: new[] { new DateRange(new DateOnly(2026, 12, 26), new DateOnly(2026, 12, 26)) }
                       .ToList().AsReadOnly());

        var definition = Definition(fixedRule, excludeRule1, excludeRule2);

        Assert.That(IsDue(definition, new DateTime(2026, 12, 24, 8, 0, 0)), Is.False);
        Assert.That(IsDue(definition, new DateTime(2026, 12, 25, 8, 0, 0)), Is.False);
        Assert.That(IsDue(definition, new DateTime(2026, 12, 26, 8, 0, 0)), Is.False);
        Assert.That(IsDue(definition, new DateTime(2026, 12, 23, 8, 0, 0)), Is.True);
    }

    // ── activeFrom / activeUntil ──────────────────────────────────────────────

    [Test]
    public void ActiveWindow_FullDate_InsideWindow_IsTrue()
    {
        var rule = Fixed(DaySet.Weekdays, new TimeOnly(8, 0),
            activeFrom:  new DateBound.FullDate(new DateOnly(2026, 1, 1)),
            activeUntil: new DateBound.FullDate(new DateOnly(2026, 12, 31)));

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8, 0, 0)), Is.True);
    }

    [Test]
    public void ActiveWindow_FullDate_BeforeWindowStart_IsFalse()
    {
        var rule = Fixed(DaySet.Weekdays, new TimeOnly(8, 0),
            activeFrom:  new DateBound.FullDate(new DateOnly(2026, 7, 1)),
            activeUntil: new DateBound.FullDate(new DateOnly(2026, 12, 31)));

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8, 0, 0)), Is.False);
    }

    [Test]
    public void ActiveWindow_FullDate_AfterWindowEnd_IsFalse()
    {
        var rule = Fixed(DaySet.Weekdays, new TimeOnly(8, 0),
            activeFrom:  new DateBound.FullDate(new DateOnly(2026, 1, 1)),
            activeUntil: new DateBound.FullDate(new DateOnly(2026, 3, 31)));

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8, 0, 0)), Is.False);
    }

    [Test]
    public void ActiveWindow_Annual_NormalWindow_InsideIsTrue()
    {
        // activeFrom=04-01, activeUntil=09-30 — summer window.
        var rule = Fixed(DaySet.Daily, new TimeOnly(8, 0),
            activeFrom:  new DateBound.Annual(4,  1),
            activeUntil: new DateBound.Annual(9, 30));

        Assert.That(IsDue(rule, new DateTime(2026, 6, 15, 8, 0, 0)), Is.True);
        Assert.That(IsDue(rule, new DateTime(2026, 3, 31, 8, 0, 0)), Is.False);
        Assert.That(IsDue(rule, new DateTime(2026, 10, 1, 8, 0, 0)), Is.False);
    }

    [Test]
    public void ActiveWindow_Annual_WrappingWindow_InsideIsTrue()
    {
        // activeFrom=11-01, activeUntil=02-28 — winter window crossing year boundary.
        var rule = Fixed(DaySet.Daily, new TimeOnly(8, 0),
            activeFrom:  new DateBound.Annual(11, 1),
            activeUntil: new DateBound.Annual(2, 28));

        Assert.That(IsDue(rule, new DateTime(2026, 12, 1,  8, 0, 0)), Is.True,  "December in");
        Assert.That(IsDue(rule, new DateTime(2026, 1,  15, 8, 0, 0)), Is.True,  "January in");
        Assert.That(IsDue(rule, new DateTime(2026, 3,  1,  8, 0, 0)), Is.False, "March out");
        Assert.That(IsDue(rule, new DateTime(2026, 10, 31, 8, 0, 0)), Is.False, "October out");
    }

    // ── empty definition ──────────────────────────────────────────────────────

    [Test]
    public void EmptyDefinition_IsNeverDue()
    {
        var definition = new ScheduleDefinition(new List<ScheduleRule>().AsReadOnly());
        Assert.That(ScheduleEvaluator.IsDue(definition, new DateTime(2026, 6, 15, 8, 0, 0)),
            Is.False);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static bool IsDue(ScheduleRule rule, DateTime now)
        => ScheduleEvaluator.IsDue(Definition(rule), now);

    private static bool IsDue(ScheduleDefinition definition, DateTime now)
        => ScheduleEvaluator.IsDue(definition, now);

    private static ScheduleDefinition Definition(params ScheduleRule[] rules)
        => new(rules.ToList().AsReadOnly());

    private static FixedRule Fixed(
        DaySet days, TimeOnly time,
        DateBound? activeFrom = null, DateBound? activeUntil = null)
        => new(days, new[] { time }.ToList().AsReadOnly(), activeFrom, activeUntil);

    private static IReadOnlyList<TimeOnly> Times(int hour, int minute)
        => new[] { new TimeOnly(hour, minute) }.ToList().AsReadOnly();

    private static WeekIntervalRule WeekInt(
        DayOfWeek day, int everyWeeks, DateOnly anchor, TimeOnly time)
        => new WeekIntervalRule(
            Days:       new DaySet(new HashSet<DayOfWeek> { day }),
            Times:      new[] { time }.ToList().AsReadOnly(),
            EveryWeeks: everyWeeks,
            Anchor:     anchor,
            ActiveFrom: null, ActiveUntil: null);
}
