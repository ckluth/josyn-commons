#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Validates the semantics of a parsed <see cref="ScheduleDefinition"/>.
/// Applied after a successful parse; never fails structurally — always returns a list
/// (possibly empty).
/// </summary>
public static class ScheduleValidator
{
    /// <inheritdoc cref="IScheduleValidator.Validate"/>
    public static IReadOnlyList<ValidationIssue> Validate(ScheduleDefinition definition)
    {
        var issues = new List<ValidationIssue>();

        CheckIndividualRules(definition.Rules, issues);
        CheckDuplicates(definition.Rules, issues);

        return issues;
    }

    //
    // Per-rule checks
    //

    private static void CheckIndividualRules(IReadOnlyList<ScheduleRule> rules, List<ValidationIssue> issues)
    {
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];

        if (rule is BoundedRule br)  CheckActiveBounds(br, i, issues);

            switch (rule)
            {
                case IntervalRule     r: CheckInterval(r, i, issues);     break;
                case WeekIntervalRule r: CheckWeekInterval(r, i, issues); break;
                case NthWeekdayRule   r: CheckNthWeekday(r, i, issues);   break;
                case MonthlyDateRule  r: CheckMonthlyDate(r, i, issues);  break;
                case OnceRule         r: CheckOnce(r, i, issues);         break;
                case ExcludeRule      r: CheckExclude(r, i, issues);      break;
            }
        }
    }

    private static void CheckInterval(IntervalRule r, int i, List<ValidationIssue> issues)
    {
        if (r.Start >= r.End)
        {
            issues.Add(Error(i,
                $"interval rule (rule {i + 1}): \"start\" ({r.Start:HH:mm}) must be earlier than \"end\" ({r.End:HH:mm})."));
            return; // window check below is meaningless without a valid window
        }

        var window   = r.End.ToTimeSpan() - r.Start.ToTimeSpan();
        var interval = r.Every.ToTimeSpan();
        if (interval > window)
            issues.Add(Warning(i,
                $"interval rule (rule {i + 1}): \"every\" ({r.Every}) exceeds the time window " +
                $"({r.Start:HH:mm}–{r.End:HH:mm}). Only the start time will fire."));
    }

    private static void CheckWeekInterval(WeekIntervalRule r, int i, List<ValidationIssue> issues)
    {
        if (!r.Days.Contains(r.Anchor.DayOfWeek))
            issues.Add(Error(i,
                $"week_interval rule (rule {i + 1}): anchor date {r.Anchor:yyyy-MM-dd} " +
                $"is a {r.Anchor.DayOfWeek} which is not in the \"days\" set. " +
                "The phase calculation would never produce a matching week."));
    }

    private static void CheckNthWeekday(NthWeekdayRule r, int i, List<ValidationIssue> issues)
    {
        // A 5th weekday occurrence within a month exists in fewer than half of all months.
        if (r.Nth is Ordinal.Numeric(5) && r.SchedulePeriod == Period.Month)
            issues.Add(Warning(i,
                $"nth_weekday rule (rule {i + 1}): nth=5 with period=month — most months have " +
                "no 5th weekday occurrence. The rule will be silently skipped for those months."));
    }

    private static void CheckMonthlyDate(MonthlyDateRule r, int i, List<ValidationIssue> issues)
    {
        if (r.Day is not MonthlyDay.Numeric(var n)) return;

        // Months where n exceeds the minimum day count (using a non-leap year for Feb=28).
        var monthsToCheck = (IEnumerable<int>)(r.Months?.Months ?? Enumerable.Range(1, 12));
        var tooShort = monthsToCheck.Where(m => n > DateTime.DaysInMonth(2001, m)).ToList();

        if (tooShort.Count > 0)
            issues.Add(Warning(i,
                $"monthly_date rule (rule {i + 1}): day={n} exceeds the length of " +
                $"{FormatMonthList(tooShort)}. The rule will silently fire on the last day of " +
                "those months instead (ADR-026 clamping behaviour)."));
    }

    private static void CheckOnce(OnceRule r, int i, List<ValidationIssue> issues)
    {
        if (r.FireAt < DateTime.Now)
            issues.Add(Warning(i,
                $"once rule (rule {i + 1}): datetime {r.FireAt:yyyy-MM-dd HH:mm} is in the past."));
    }

    private static void CheckExclude(ExcludeRule r, int i, List<ValidationIssue> issues)
    {
        issues.AddRange(r.Dates.Where(range => range.Start > range.End)
            .Select(range => Error(i, $"exclude rule (rule {i + 1}): date range {range.Start:yyyy-MM-dd}" + $"..{range.End:yyyy-MM-dd} has start after end.")));
    }

    private static void CheckActiveBounds(BoundedRule rule, int i, List<ValidationIssue> issues)
    {
        // Only flag when both sides are full dates — annual MM-DD windows can legitimately
        // wrap across the year boundary (e.g. active_from=11-01, active_until=02-28).
        if (rule.ActiveFrom  is DateBound.FullDate(var from) &&
            rule.ActiveUntil is DateBound.FullDate(var until) &&
            from > until)
        {
            issues.Add(Error(i,
                $"Rule {i + 1}: \"activeFrom\" ({from:yyyy-MM-dd}) is after " +
                $"\"activeUntil\" ({until:yyyy-MM-dd})."));
        }
    }

    //
    // Cross-rule checks
    //

    private static void CheckDuplicates(IReadOnlyList<ScheduleRule> rules, List<ValidationIssue> issues)
    {
        // Serialise each rule to its canonical text and flag any identical pair.
        // Using the serializer avoids implementing deep equality on every rule type.
        var seen = new Dictionary<string, int>();
        for (var i = 0; i < rules.Count; i++)
        {
            var text = ScheduleSerializer.SerializeRuleText(rules[i]);
            if (seen.TryGetValue(text, out var firstIndex))
                issues.Add(Warning(i,
                    $"Rule {i + 1} is identical to rule {firstIndex + 1} — duplicate rule."));
            else
                seen[text] = i;
        }
    }

    //
    // Helpers
    //

    private static ValidationIssue Error(int index, string message) => new(ValidationSeverity.Error, message, index);

    private static ValidationIssue Warning(int index, string message) => new(ValidationSeverity.Warning, message, index);

    private static string FormatMonthList(List<int> months)
    {
        string[] abbrevs = ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];
        return string.Join(", ", months.Select(m => abbrevs[m - 1]));
    }
}
