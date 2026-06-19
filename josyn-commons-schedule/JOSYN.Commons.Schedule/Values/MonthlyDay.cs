#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// The calendar day selector for a <c>monthly_date</c> rule — the <c>day</c> field.
/// </summary>
public abstract record MonthlyDay
{
    private MonthlyDay() { }

    /// <summary>
    /// A fixed calendar day number (1–31).
    /// If the value exceeds the length of a given month the rule fires on the last
    /// calendar day of that month instead.
    /// </summary>
    /// <param name="N">Day number. Valid range: 1–31.</param>
    public sealed record Numeric(int N) : MonthlyDay;

    /// <summary>The last calendar day of the applicable month.</summary>
    public sealed record Last : MonthlyDay;

    /// <summary>
    /// The last business day of the applicable month — scanned backward from the last
    /// calendar day, skipping weekends and any date covered by an <c>exclude</c> block.
    /// If no valid day exists in the month the rule fires are skipped silently.
    /// </summary>
    public sealed record LastBusiness : MonthlyDay;
}
