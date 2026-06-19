using NUnit.Framework;

using JOSYN.Commons.Schedule;

namespace JOSYN.Commons.Schedule.Test;

/// <summary>
/// One test per semantic condition defined in <see cref="ScheduleValidator"/>.
/// </summary>
[TestFixture]
public sealed class ScheduleValidatorTests
{
    //
    // Errors
    //

    [Test]
    public void Validate_IntervalStartGeEnd_IsError()
    {
        // start == end
        var rule = new IntervalRule(
            Days: DaySet.Weekdays,
            Start: new TimeOnly(17, 0),
            End:   new TimeOnly(17, 0),
            Every: new Duration(30, DurationUnit.Minutes),
            ActiveFrom: null, ActiveUntil: null);

        var issues = ScheduleValidator.Validate(Definition(rule));

        Assert.That(issues, Has.One.Matches<ValidationIssue>(i =>
            i.Severity == ValidationSeverity.Error && i.RuleIndex == 0));
    }

    [Test]
    public void Validate_WeekIntervalAnchorNotInDays_IsError()
    {
        // days = fri, anchor = 2026-01-05 (a Monday) — anchor day not in days set
        var rule = new WeekIntervalRule(
            Days: new DaySet(new HashSet<DayOfWeek> { DayOfWeek.Friday }),
            Times: new[] { new TimeOnly(8, 0) }.ToList().AsReadOnly(),
            EveryWeeks: 2,
            Anchor: new DateOnly(2026, 1, 5),   // Monday
            ActiveFrom: null, ActiveUntil: null);

        var issues = ScheduleValidator.Validate(Definition(rule));

        Assert.That(issues, Has.One.Matches<ValidationIssue>(i =>
            i.Severity == ValidationSeverity.Error && i.RuleIndex == 0));
    }

    [Test]
    public void Validate_ExcludeRangeStartAfterEnd_IsError()
    {
        var rule = new ExcludeRule(
            Dates: new List<DateRange>
            {
                new DateRange(new DateOnly(2026, 12, 25), new DateOnly(2026, 12, 25)),
                new DateRange(new DateOnly(2026, 12, 31), new DateOnly(2026, 12, 27)), // start > end
            }.AsReadOnly());

        var issues = ScheduleValidator.Validate(Definition(rule));

        Assert.That(issues, Has.One.Matches<ValidationIssue>(i => i is { Severity: ValidationSeverity.Error, RuleIndex: 0 }));
    }

    [Test]
    public void Validate_ActiveFromAfterActiveUntil_FullDates_IsError()
    {
        var rule = new IntervalRule(
            Days: DaySet.Weekdays,
            Start: new TimeOnly(8, 0),
            End:   new TimeOnly(17, 0),
            Every: new Duration(30, DurationUnit.Minutes),
            ActiveFrom:  new DateBound.FullDate(new DateOnly(2026, 12, 31)),
            ActiveUntil: new DateBound.FullDate(new DateOnly(2026, 1, 1)));

        var issues = ScheduleValidator.Validate(Definition(rule));

        Assert.That(issues, Has.One.Matches<ValidationIssue>(i => i is { Severity: ValidationSeverity.Error, RuleIndex: 0 }));
    }

    //
    // Warnings
    //

    [Test]
    public void Validate_IntervalEveryGreaterThanWindow_IsWarning()
    {
        // 1h interval inside a 30m window
        var rule = new IntervalRule(
            Days: DaySet.Weekdays,
            Start: new TimeOnly(8, 0),
            End:   new TimeOnly(8, 30),
            Every: new Duration(60, DurationUnit.Minutes),
            ActiveFrom: null, ActiveUntil: null);

        var issues = ScheduleValidator.Validate(Definition(rule));

        Assert.That(issues, Has.One.Matches<ValidationIssue>(i => i is { Severity: ValidationSeverity.Warning, RuleIndex: 0 }));
    }

    [Test]
    public void Validate_NthWeekday_Nth5_Monthly_IsWarning()
    {
        var rule = new NthWeekdayRule(
            Weekday: DayOfWeek.Friday,
            Nth:     new Ordinal.Numeric(5),
            SchedulePeriod: Period.Month,
            Times: new[] { new TimeOnly(9, 0) }.ToList().AsReadOnly(),
            ActiveFrom: null, ActiveUntil: null);

        var issues = ScheduleValidator.Validate(Definition(rule));

        Assert.That(issues, Has.One.Matches<ValidationIssue>(i => i is { Severity: ValidationSeverity.Warning, RuleIndex: 0 }));
    }

    [Test]
    public void Validate_MonthlyDate_Day31_WithShortMonths_IsWarning()
    {
        // day=31, months includes feb — feb never has 31 days
        var rule = new MonthlyDateRule(
            Day:    new MonthlyDay.Numeric(31),
            Times:  new[] { new TimeOnly(8, 0) }.ToList().AsReadOnly(),
            Months: new MonthSet(new HashSet<int> { 1, 2, 12 }),
            ActiveFrom: null, ActiveUntil: null);

        var issues = ScheduleValidator.Validate(Definition(rule));

        Assert.That(issues, Has.One.Matches<ValidationIssue>(i => i is { Severity: ValidationSeverity.Warning, RuleIndex: 0 }));
    }

    [Test]
    public void Validate_OnceDateTimeInPast_IsWarning()
    {
        var rule = new OnceRule(
            FireAt: new DateTime(2000, 1, 1, 0, 0, 0),
            ActiveFrom: null, ActiveUntil: null);

        var issues = ScheduleValidator.Validate(Definition(rule));

        Assert.That(issues, Has.One.Matches<ValidationIssue>(i => i is { Severity: ValidationSeverity.Warning, RuleIndex: 0 }));
    }

    [Test]
    public void Validate_DuplicateBlocks_IsWarning()
    {
        var rule = new FixedRule(
            Days:  DaySet.Weekdays,
            Times: new[] { new TimeOnly(8, 0) }.ToList().AsReadOnly(),
            ActiveFrom: null, ActiveUntil: null);

        // Two identical rules
        var issues = ScheduleValidator.Validate(new ScheduleDefinition(new List<ScheduleRule> { rule, rule }.AsReadOnly()));

        Assert.That(issues.Count(i =>
            i.Severity == ValidationSeverity.Warning), Is.GreaterThanOrEqualTo(1));
    }

    // ── Clean definition ──────────────────────────────────────────────────────

    [Test]
    public void Validate_CleanDefinition_ReturnsNoIssues()
    {
        var rule = new IntervalRule(
            Days: DaySet.Weekdays,
            Start: new TimeOnly(8, 0),
            End:   new TimeOnly(17, 0),
            Every: new Duration(30, DurationUnit.Minutes),
            ActiveFrom: null, ActiveUntil: null);

        var issues = ScheduleValidator.Validate(Definition(rule));
        Assert.That(issues, Is.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ScheduleDefinition Definition(ScheduleRule rule) =>
        new(new List<ScheduleRule> { rule }.AsReadOnly());
}
